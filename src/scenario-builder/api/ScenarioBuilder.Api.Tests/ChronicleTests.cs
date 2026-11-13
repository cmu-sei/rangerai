// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ScenarioBuilder.Api.Domain;
using static ScenarioBuilder.Api.Tests.TestApi;

namespace ScenarioBuilder.Api.Tests;

/// <summary>Publishing to the Chronicle, drafting the enclave from the network answers, and relaying what GraySpace built.</summary>
public class ChronicleTests
{
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    private static readonly JsonObject Salt = JsonNode.Parse(Fixture("salt-lantern.answers.json"))!.AsObject();
    private static JsonObject Capabilities() => JsonNode.Parse(Fixture("grayspace-capabilities.json"))!.AsObject();
    private static JsonArray AttachmentPoints() => JsonNode.Parse(Fixture("grayspace-attachment-points.json"))!.AsArray();

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>Stands in for the Chronicle API: records in memory, entries applied to their part, subscribers kept.</summary>
    private sealed class FakeChronicle : HttpMessageHandler
    {
        public Dictionary<string, JsonObject> Records { get; } = [];
        public Dictionary<string, JsonObject> Subscribers { get; } = [];
        public bool HasSubscribers { get; set; } = true;
        public List<string> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);   // api chronicles {id} ...
            Calls.Add($"{request.Method} {request.RequestUri.AbsolutePath}");
            var body = request.Content is null ? null : JsonNode.Parse(await request.Content.ReadAsStringAsync(ct)) as JsonObject;
            switch (request.Method.Method, path.Length)
            {
                case ("POST", 2):
                    var id = (string)body!["id"]!;
                    if (Records.ContainsKey(id))
                        return Json("""{"title":"exists"}""", HttpStatusCode.Conflict);
                    Records[id] = body;
                    return Json(body.ToJsonString(), HttpStatusCode.Created);
                case ("GET", 3):
                    return Records.TryGetValue(path[2], out var record) ? Json(record.ToJsonString()) : Json("{}", HttpStatusCode.NotFound);
                case ("POST", 4) when path[3] == "entries":
                    var target = Records[path[2]];
                    var history = target["history"]!.AsArray();
                    body!["sequence"] = history.Count + 1;
                    history.Add(body.DeepClone());
                    var part = target[(string)body["part"]!]!.AsArray();
                    var item = body["item"]!.AsObject();
                    var at = part.Select((x, i) => (x, i)).FirstOrDefault(p => (string?)p.x!["id"] == (string?)item["id"]).i;
                    if (part.Any(x => (string?)x!["id"] == (string?)item["id"]))
                        part[at] = item.DeepClone();
                    else
                        part.Add(item.DeepClone());
                    return Json(body.ToJsonString(), HttpStatusCode.Created);
                case ("PUT", 5) when path[3] == "subscribers":
                    if (!HasSubscribers)
                        return Json("{}", HttpStatusCode.NotFound);
                    Subscribers[path[4]] = body!;
                    return Json(body!.ToJsonString());
            }
            return Json("{}", HttpStatusCode.NotFound);
        }

        public List<JsonObject> Entries(string id) => Records[id]["history"]!.AsArray().OfType<JsonObject>().ToList();
        public JsonObject Item(string id, string itemId) =>
            new[] { "world", "intent", "asBuilt", "asRun" }.SelectMany(p => Records[id][p]!.AsArray().OfType<JsonObject>()).Single(i => (string?)i["id"] == itemId);
    }

    /// <summary>Stands in for GraySpace at http://gs.test:8000/deployments/abc: answers "METHOD /path?query" as told.</summary>
    private sealed class FakeGraySpace : HttpMessageHandler
    {
        public const string Base = "/deployments/abc/api/v1";
        public Dictionary<string, Func<HttpResponseMessage>> Routes { get; } = new()
        {
            ["GET /api/v1/deployments/abc"] = () => Json("""{"id":"abc"}"""),
            [$"GET {Base}/capabilities"] = () => Json(Capabilities().ToJsonString()),
            [$"GET {Base}/attachment-points"] = () => Json(AttachmentPoints().ToJsonString())
        };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(Routes.TryGetValue($"{request.Method.Method} {request.RequestUri!.PathAndQuery}", out var answer) ? answer() : Json("""{"detail":"Not Found"}""", HttpStatusCode.NotFound));
    }

    private static WebApplicationFactory<Program> With(ApiFactory factory, FakeChronicle chronicle, FakeGraySpace? grayspace = null) =>
        factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddHttpClient("chronicle").ConfigurePrimaryHttpMessageHandler(() => chronicle);
            s.AddHttpClient("grayspace").ConfigurePrimaryHttpMessageHandler(() => grayspace ?? new FakeGraySpace());
        }));

    private static HttpClient Client(WebApplicationFactory<Program> app, bool agent = false)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-User", "dev");
        if (agent)
            client.DefaultRequestHeaders.Add("X-Via", "agent");
        return client;
    }

    /// <summary>SALT LANTERN, every question answered as the designer, the summary drafted, and GraySpace connected.</summary>
    private static async Task<(HttpClient Client, int Scenario)> SaltLantern(ApiFactory factory, WebApplicationFactory<Program> app)
    {
        var client = Client(app);
        (await client.PutAsJsonAsync("/api/grayspace", new { url = "http://gs.test:8000/deployments/abc", token = "t" })).EnsureSuccessStatusCode();
        var id = await CreateScenario(client, "SALT LANTERN", SmallUnitType);
        SetStage(factory, id, 4);
        foreach (var (question, value) in Salt["answers"]!.AsObject())
            if (question != "exerciseName")
                (await Put(client, id, question, new { value = value!.DeepClone() })).EnsureSuccessStatusCode();
        (await Draft(client, id, (string)Salt["executiveSummary"]!)).EnsureSuccessStatusCode();
        return (client, id);
    }

    [Fact]
    public void Hosts_services_and_personas_parse_from_the_answers_forms()
    {
        var hosts = WorldParser.Hosts(Salt["answers"]!["hosts"]!.AsArray().Select(h => (string)h!), ["corp-lan", "ot-control", "vendor-dmz"]);
        Assert.Equal(["dc01", "files01", "mail01", "www", "staff-ws", "hmi01", "historian01", "eng-ws01", "jump01"], hosts.Select(h => h.Hostname));
        Assert.Equal(("Mail", "The public website"), (Cap(hosts.Single(h => h.Hostname == "mail01").Role), Cap(hosts.Single(h => h.Hostname == "www").Role)));
        var staff = hosts.Single(h => h.Hostname == "staff-ws");
        Assert.Equal(("host-staff-ws", "Staff workstations", 4, "corp-lan"), (staff.Id, staff.Name, staff.Count, staff.Segment));
        Assert.True(hosts.Single(h => h.Hostname == "hmi01").Virtual);
        Assert.False(hosts.Single(h => h.Hostname == "dc01").Virtual);

        var services = WorldParser.Services(Salt["answers"]!["services"]!.AsArray().Select(s => (string)s!));
        Assert.Contains(services, s => s is { ServiceType: "News website", Address: "www.kessickledger.com", Host: "ledger" });
        Assert.Contains(services, s => s is { ServiceType: "Email", Address: "kbwa.org", Host: "mail01" });
        Assert.Contains(services, s => s is { ServiceType: "Remote access", Address: "remote.kbwa.org", Host: "jump01" });
        Assert.Contains(services, s => s is { ServiceType: "File share", Name: "File share plant-engineering" });

        var dana = WorldParser.Personas(Salt["answers"]!["personas"]!.AsArray().Select(p => (string)p!), ["Kessick Bay Water Authority", "VOLT HERON"]).Single();
        Assert.Equal(("persona-dana-whitlock", "Dana Whitlock", "Night-shift plant operator", "Kessick Bay Water Authority"), (dana.Id, dana.Name, dana.Role, dana.Organization));
        Assert.Equal("dana.whitlock", WorldParser.Username(dana.Name));

        // Free text that follows no form still becomes one host per line.
        Assert.Equal(["a-big-database-box"], WorldParser.Hosts(["a big database box"], []).Select(h => h.Hostname));

        static string Cap(string s) => char.ToUpperInvariant(s[0]) + s[1..];
    }

    [Fact]
    public async Task Publishing_creates_the_Chronicle_with_the_World_from_the_answers_then_only_what_changed()
    {
        using var factory = new ApiFactory();
        var chronicle = new FakeChronicle();
        using var app = With(factory, chronicle);
        var (client, id) = await SaltLantern(factory, app);

        Assert.Equal(HttpStatusCode.Forbidden, (await Client(app, agent: true).PostAsync($"/api/scenarios/{id}/chronicle", null)).StatusCode);
        var first = await Body(await client.PostAsync($"/api/scenarios/{id}/chronicle", null));

        Assert.Equal("salt-lantern", (string)first["chronicleId"]!);
        Assert.Equal("http://localhost:4320/events/salt-lantern", (string)first["url"]!);
        var entries = (int)first["entries"]!;
        Assert.Equal(chronicle.Entries("salt-lantern").Count, entries);
        Assert.Equal(3, (int)first["subscribers"]!);
        var record = chronicle.Records["salt-lantern"];
        Assert.Equal((int)first["parts"]!["world"]!, record["world"]!.AsArray().Count);
        var dc01 = chronicle.Item("salt-lantern", "host-dc01");
        Assert.Equal(("device", "net-corp-lan"), ((string)dc01["kind"]!, (string)dc01["relatedObjects"]![0]!["objectId"]!));
        var dana = chronicle.Item("salt-lantern", "persona-dana-whitlock");
        Assert.Equal(("org-kessick-bay-water-authority", "dana.whitlock"), ((string)dana["organizationId"]!, (string)dana["accounts"]![0]!["username"]!));
        Assert.Equal(["persona-dana-whitlock"], chronicle.Item("salt-lantern", "pe-e1")["knownBy"]!.AsArray().Select(k => (string)k!));
        Assert.Equal(["org-kessick-valley-ledger"], chronicle.Item("salt-lantern", "pe-e3")["knownBy"]!.AsArray().Select(k => (string)k!));
        Assert.Equal("excon", (string)chronicle.Item("salt-lantern", "gt-gt1")["visibility"]!);
        Assert.All(chronicle.Entries("salt-lantern"), e => Assert.Equal("scenario-builder", (string)e["author"]!["app"]!));

        // Again, unchanged: nothing new. Then one answer changes: one revise, naming the entry it replaces.
        var again = await Body(await client.PostAsync($"/api/scenarios/{id}/chronicle", null));
        Assert.Equal(entries, (int)again["entries"]!);
        var replaced = chronicle.Entries("salt-lantern").Last(e => (string)e["itemId"]! == "audience-3");
        (await Put(client, id, "proficiency", new { value = "New to OT." })).EnsureSuccessStatusCode();
        var changed = await Body(await client.PostAsync($"/api/scenarios/{id}/chronicle", null));

        Assert.Equal(entries + 1, (int)changed["entries"]!);
        var revise = chronicle.Entries("salt-lantern").Last();
        Assert.Equal(("revise", "audience-3", (string)replaced["id"]!), ((string)revise["op"]!, (string)revise["itemId"]!, (string)revise["revises"]!));
        Assert.Equal("New to OT.", (string)chronicle.Item("salt-lantern", "audience-3")["text"]!);
    }

    [Fact]
    public async Task Publishing_registers_an_agent_per_persona_and_newsroom_and_the_exercise_control_assistant()
    {
        using var factory = new ApiFactory();
        var chronicle = new FakeChronicle();
        using var app = With(factory, chronicle);
        var (client, id) = await SaltLantern(factory, app);
        (await client.PostAsync($"/api/scenarios/{id}/chronicle", null)).EnsureSuccessStatusCode();

        Assert.Equal(["excon-assistant", "ghosts-dana-whitlock", "pandora-kessick-valley-ledger"], chronicle.Subscribers.Keys.Order());
        var ghosts = chronicle.Subscribers["ghosts-dana-whitlock"];
        Assert.Equal(("ghosts", "GHOSTS · Dana Whitlock", "participant"), ((string)ghosts["app"]!, (string)ghosts["name"]!, (string)ghosts["audience"]!));
        Assert.Equal(["persona-dana-whitlock"], ghosts["viewers"]!.AsArray().Select(v => (string)v!));
        var delivery = ghosts["agent"]!["output"]!["item"]!;
        Assert.Equal(("injectDelivery", "pe-e1", "{{text}}"), ((string)delivery["kind"]!, (string)delivery["plannedEventId"]!, (string)delivery["note"]!));
        Assert.Equal("Mission Element HARBOR, 41st Cyber Protection Team", (string)delivery["to"]![0]!);
        Assert.Contains("{{context}}", (string)ghosts["agent"]!["task"]!);

        var news = chronicle.Subscribers["pandora-kessick-valley-ledger"];
        Assert.Equal(["org-kessick-valley-ledger"], news["viewers"]!.AsArray().Select(v => (string)v!));
        var story = news["agent"]!["output"]!["item"]!;
        Assert.Equal(("content", "pe-e3", "Kessick Valley Ledger", "svc-news-site-www-kessickledger-com"),
            ((string)story["kind"]!, (string)story["plannedEventId"]!, (string)story["attributedTo"]!, (string)story["serviceId"]!));

        var excon = chronicle.Subscribers["excon-assistant"];
        Assert.Equal(("excon", 4), ((string)excon["audience"]!, excon["parts"]!.AsArray().Count));
        Assert.Equal("excon", (string)excon["agent"]!["output"]!["item"]!["visibility"]!);
    }

    [Fact]
    public async Task A_Chronicle_API_without_subscribers_still_takes_the_publish()
    {
        using var factory = new ApiFactory();
        var chronicle = new FakeChronicle { HasSubscribers = false };
        using var app = With(factory, chronicle);
        var (client, id) = await SaltLantern(factory, app);

        var published = await Body(await client.PostAsync($"/api/scenarios/{id}/chronicle", null));

        Assert.Equal((0, true), ((int)published["subscribers"]!, chronicle.Records.ContainsKey("salt-lantern")));
    }

    [Fact]
    public async Task The_enclave_is_drafted_from_the_network_answers_and_what_GraySpace_offers()
    {
        using var factory = new ApiFactory();
        using var app = With(factory, new FakeChronicle());
        var (client, id) = await SaltLantern(factory, app);

        var spec = (await Body(await client.PostAsync($"/api/scenarios/{id}/enclave/draft", null)))["spec"]!.AsObject();

        Assert.Equal("salt-lantern", (string)spec["slug"]!);
        var sites = spec["sites"]!.AsArray();
        Assert.Equal(["kbwa-corp", "kbwa-plant"], sites.Select(s => (string)s!["name"]!));
        Assert.Equal(["ot-control", "vendor-dmz"], sites[1]!["segments"]!.AsArray().Select(g => (string)g!["name"]!));
        Assert.Equal("com-por", (string)sites[0]!["attach"]!["router"]!);   // Portland, for the Pacific Northwest
        Assert.NotEqual(Isp((string)sites[0]!["attach"]!["router"]!), Isp((string)sites[1]!["attach"]!["router"]!));

        var hosts = spec["hosts"]!.AsArray().OfType<JsonObject>().ToDictionary(h => (string)h["name"]!);
        Assert.Equal(["dc01", "files01", "mail01", "www", "staff-ws", "hmi01", "historian01", "eng-ws01", "jump01", "ledger", "tidewell-portal"], hosts.Keys);
        Assert.Equal(["ad-dc", "dns"], hosts["dc01"]["roles"]!.AsArray().Select(r => (string)r!));
        Assert.Equal((4, "ghosts-client", "kbwa-staff-ws"), ((int)hosts["staff-ws"]["count"]!, (string)hosts["staff-ws"]["roles"]![0]!, (string)hosts["staff-ws"]["labels"]!["ghosts.pool"]!));
        Assert.Equal("ubuntu-24.04-vm", (string)hosts["hmi01"]["image"]!);
        Assert.Equal(["mail01", "www", "jump01"], hosts.Values.Where(h => (bool?)h["publish"] == true).Select(h => (string)h["name"]!));
        Assert.Equal(("ghosts-pandora", "svc-news-site-www-kessickledger-com"), ((string)hosts["ledger"]["image"]!, (string)hosts["ledger"]["labels"]!["chronicle.of"]!));
        Assert.NotNull(hosts["ledger"]["internet"]);
        Assert.All(hosts.Values, h => Assert.Equal("salt-lantern", (string)h["labels"]!["chronicle.id"]!));
        Assert.Equal("host-dc01", (string)hosts["dc01"]["labels"]!["chronicle.of"]!);

        Assert.Equal(["kbwa.org", "kessickledger.com", "tidewellcontrols.com"], spec["dns"]!["domains"]!.AsArray().Select(d => (string)d!["domain"]!));
        Assert.Contains(spec["dns"]!["records"]!.AsArray(), r => (string)r!["type"]! == "MX" && (string)r["name"]! == "kbwa.org");
        Assert.Contains(spec["dns"]!["records"]!.AsArray(), r => (string)r!["name"]! == "remote.kbwa.org" && (string)r["host"]! == "jump01");
        Assert.Equal(["www", "jump01", "ledger", "tidewell-portal"], spec["tls"]!.AsArray().Select(t => (string)t!["host"]!));
        Assert.Equal(("corp.kbwa.org", "KBWA"), ((string)spec["role_vars"]!["ad-dc"]!["realm"]!, (string)spec["role_vars"]!["ad-dc"]!["domain"]!));
        Assert.Equal("plant-engineering", (string)spec["role_vars"]!["file"]!["share_name"]!);

        // The draft is saved: the page reads it back.
        Assert.Equal(11, (await client.GetFromJsonAsync<JsonObject>($"/api/scenarios/{id}/enclave"))!["spec"]!["hosts"]!.AsArray().Count);

        static string Isp(string router) => (string)AttachmentPoints().Single(p => (string)p!["router"]! == router)!["isp"]!;
    }

    [Fact]
    public async Task With_no_network_answers_there_is_nothing_to_draft()
    {
        using var factory = new ApiFactory();
        using var app = With(factory, new FakeChronicle());
        var client = Client(app);
        (await client.PutAsJsonAsync("/api/grayspace", new { url = "http://gs.test:8000/deployments/abc", token = "t" })).EnsureSuccessStatusCode();
        var id = await CreateScenario(client, "Empty", SmallUnitType);

        var response = await client.PostAsync($"/api/scenarios/{id}/enclave/draft", null);

        Assert.Equal((HttpStatusCode.UnprocessableEntity, "NOTHING_TO_DRAFT"), (response.StatusCode, await Code(response)));
    }

    [Fact]
    public async Task What_GraySpace_builds_is_relayed_to_the_Chronicle_once()
    {
        using var factory = new ApiFactory();
        var chronicle = new FakeChronicle();
        var grayspace = new FakeGraySpace();
        using var app = With(factory, chronicle, grayspace);
        var (client, id) = await SaltLantern(factory, app);
        (await client.PostAsync($"/api/scenarios/{id}/chronicle", null)).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/scenarios/{id}/enclave/draft", null)).EnsureSuccessStatusCode();
        var published = chronicle.Entries("salt-lantern").Count;

        const string Base = FakeGraySpace.Base;
        grayspace.Routes[$"PUT {Base}/slices/salt-lantern"] = () => Json("""{"slug":"salt-lantern","version":"sha256:abc","created":true,"findings":[]}""");
        grayspace.Routes[$"POST {Base}/instances"] = () => Json("""{"instance":"saltlan","job":{"id":"j1","state":"queued"}}""", HttpStatusCode.Accepted);
        (await client.PostAsJsonAsync($"/api/scenarios/{id}/enclave/instance", new { })).EnsureSuccessStatusCode();

        var building = chronicle.Entries("salt-lantern").Last();
        Assert.Equal(("asBuilt", "dep-grayspace-salt-lantern", "building", "grayspace"),
            ((string)building["part"]!, (string)building["itemId"]!, (string)building["item"]!["state"]!, (string)building["author"]!["app"]!));

        // The job is still running: nothing more. Then it succeeds with the manifest.
        grayspace.Routes[$"GET {Base}/jobs/j1"] = () => Json("""{"id":"j1","kind":"create_instance","state":"running","stage":"apply"}""");
        grayspace.Routes[$"GET {Base}/jobs/j1/log?follow=false"] = () => Json("");
        grayspace.Routes[$"GET {Base}/instances/saltlan"] = () => Json("""{"id":"saltlan","state":"creating","hosts":[]}""");
        (await client.GetAsync($"/api/scenarios/{id}/enclave/instance")).EnsureSuccessStatusCode();
        Assert.Equal(published + 1, chronicle.Entries("salt-lantern").Count);

        grayspace.Routes[$"GET {Base}/jobs/j1"] = () => Json("""{"id":"j1","kind":"create_instance","state":"succeeded","finished":"2026-11-13T12:00:00Z","slice":{"slug":"salt-lantern","version":"sha256:abc"}}""");
        grayspace.Routes[$"GET {Base}/instances/saltlan"] = () => Json("""
            {"id":"saltlan","state":"ready","hosts":[
              {"name":"dc01","hostname":"saltlan-dc01","site":"kbwa-corp","segment":"corp-lan","ip":"192.168.0.10","public_ip":null,"fqdns":[],"labels":{"chronicle.of":"host-dc01"},"image":"ubuntu-22.04-lxc","status":"running"},
              {"name":"www","hostname":"saltlan-www","site":"kbwa-corp","segment":"corp-lan","ip":"192.168.0.13","public_ip":"98.245.128.4","fqdns":["www.kbwa.org"],"labels":{},"image":"ubuntu-22.04-lxc","status":"running"},
              {"name":"staff-ws-01","hostname":"saltlan-staff-ws-01","site":"kbwa-corp","segment":"corp-lan","ip":"192.168.0.14","fqdns":[],"labels":{},"image":"ubuntu-22.04-lxc","status":"running"}]}
            """);
        (await client.GetAsync($"/api/scenarios/{id}/enclave/instance")).EnsureSuccessStatusCode();

        var www = chronicle.Item("salt-lantern", "rz-www");
        Assert.Equal(("host-www", "saltlan-www", "98.245.128.4", "www.kbwa.org"),
            ((string)www["of"]!, (string)www["identifiers"]!["hostname"]!, (string)www["identifiers"]!["publicIp"]!, (string)www["identifiers"]!["fqdn"]!));
        Assert.Equal(("host-staff-ws", "01"), ((string)chronicle.Item("salt-lantern", "rz-staff-ws-01")["of"]!, (string)chronicle.Item("salt-lantern", "rz-staff-ws-01")["identifiers"]!["copy"]!));
        var ready = chronicle.Entries("salt-lantern").Last();
        Assert.Equal(("revise", "ready", (string)building["id"]!), ((string)ready["op"]!, (string)ready["item"]!["state"]!, (string)ready["revises"]!));
        Assert.Equal(["host-dc01", "host-www", "host-staff-ws"], ready["item"]!["builds"]!.AsArray().Select(b => (string)b!));

        // Following again publishes nothing more.
        var count = chronicle.Entries("salt-lantern").Count;
        (await client.GetAsync($"/api/scenarios/{id}/enclave/instance")).EnsureSuccessStatusCode();
        Assert.Equal(count, chronicle.Entries("salt-lantern").Count);
    }

    [Fact]
    public async Task A_scenario_not_published_yet_relays_nothing()
    {
        using var factory = new ApiFactory();
        var chronicle = new FakeChronicle();
        var grayspace = new FakeGraySpace();
        using var app = With(factory, chronicle, grayspace);
        var (client, id) = await SaltLantern(factory, app);
        (await client.PostAsync($"/api/scenarios/{id}/enclave/draft", null)).EnsureSuccessStatusCode();
        grayspace.Routes[$"PUT {FakeGraySpace.Base}/slices/salt-lantern"] = () => Json("""{"slug":"salt-lantern","version":"sha256:abc"}""");
        grayspace.Routes[$"POST {FakeGraySpace.Base}/instances"] = () => Json("""{"instance":"saltlan","job":{"id":"j1"}}""");

        (await client.PostAsJsonAsync($"/api/scenarios/{id}/enclave/instance", new { })).EnsureSuccessStatusCode();

        Assert.Empty(chronicle.Records);
    }
}
