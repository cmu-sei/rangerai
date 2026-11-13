// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using Chronicle.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chronicle.Api.Tests;

/// <summary>The live side: activity, subscribers, context, and the WebSocket that carries them.</summary>
public class LiveTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly Author Ghosts = new() { App = "ghosts", Via = Via.App };
    private readonly HttpClient http = factory.CreateClient();

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
    private static StringContent Json<T>(T value) => Json(ChronicleJson.Serialize(value));

    private async Task<string> NewChronicle()
    {
        var id = $"t-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await http.PostAsync("/api/chronicles", Json(ChronicleRecord.Start(id, "Live", EventKind.Exercise, DateTimeOffset.UtcNow)))).StatusCode);
        return id;
    }

    private async Task<JsonNode> Report(string id, string body)
    {
        var response = await http.PostAsync($"/api/chronicles/{id}/activity", Json(body));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    private async Task<WebSocket> Connect(string id, string audience)
    {
        var client = factory.Server.CreateWebSocketClient();
        return await client.ConnectAsync(new Uri($"ws://localhost/api/chronicles/{id}/live?audience={audience}"), CancellationToken.None);
    }

    private static async Task<JsonNode?> Next(WebSocket socket, int ms = 1500)
    {
        var buffer = new byte[1 << 16];
        using var timeout = new CancellationTokenSource(ms);
        try
        {
            var text = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, timeout.Token);
                text.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);
            return JsonNode.Parse(text.ToString());
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    [Fact]
    public async Task Activity_is_excon_unless_marked_and_a_participant_never_sees_excon_activity()
    {
        var id = await NewChronicle();
        await Report(id, """{"source":"ghosts","kind":"browse","text":"secret browse","host":"ws-01"}""");
        await Report(id, """{"source":"ghosts","kind":"email","text":"open mail","visibility":"participant"}""");

        var participant = JsonNode.Parse(await http.GetStringAsync($"/api/chronicles/{id}/activity?audience=participant"))!.AsArray();
        var excon = JsonNode.Parse(await http.GetStringAsync($"/api/chronicles/{id}/activity?audience=excon"))!.AsArray();
        Assert.Equal(["open mail"], participant.Select(a => (string)a!["text"]!));
        Assert.Equal(2, excon.Count);
    }

    [Fact]
    public async Task The_live_feed_carries_only_what_its_audience_may_see_tokens_included()
    {
        var id = await NewChronicle();
        using var participant = await Connect(id, "participant");
        using var excon = await Connect(id, "excon");
        Assert.Equal("hello", (string)(await Next(participant))!["type"]!);
        Assert.Equal("hello", (string)(await Next(excon))!["type"]!);

        await Report(id, """{"source":"ghosts","kind":"login","text":"excon only"}""");
        var hub = factory.Services.GetRequiredService<LiveHub>();
        hub.Send(id, new LiveHub.Message("""{"type":"token","subscriberId":"x","runId":"r","text":"secret"}""", Audience.Excon));
        await Report(id, """{"source":"ghosts","kind":"chat","text":"everyone","visibility":"participant"}""");

        var heard = new List<JsonNode>();
        while (await Next(participant) is { } m) heard.Add(m);
        Assert.Equal(["everyone"], heard.Select(m => (string)m["activity"]!["text"]!));

        var exconHeard = new List<string>();
        while (await Next(excon) is { } m) exconHeard.Add((string)m["type"]!);
        Assert.Equal(["activity", "token", "activity"], exconHeard);
    }

    [Fact]
    public async Task A_notable_activity_stores_its_entry_and_names_it_and_the_feed_hears_the_entry()
    {
        var id = await NewChronicle();
        using var socket = await Connect(id, "participant");
        await Next(socket);
        var entry = Entry.Add(id, Part.AsRun, new Observation { Id = "o-1", At = DateTimeOffset.UtcNow, By = "dana", Text = "Setpoint moved", Visibility = Audience.Participant }, Ghosts, DateTimeOffset.UtcNow);
        var body = new JsonObject { ["source"] = "ghosts", ["kind"] = "chat", ["text"] = "Dana messaged IT", ["entry"] = JsonNode.Parse(ChronicleJson.Serialize(entry)) };

        var activity = await Report(id, body.ToJsonString());

        Assert.Equal(1, (long)activity["sequence"]!);
        Assert.Equal("participant", (string)activity["visibility"]!);   // the item's visibility, as none was given
        var record = JsonNode.Parse(await http.GetStringAsync($"/api/chronicles/{id}"))!;
        Assert.Equal("o-1", (string)record["asRun"]![0]!["id"]!);
        var types = new List<string>();
        while (await Next(socket) is { } m) types.Add((string)m["type"]!);
        Assert.Contains("entry", types);
        Assert.Contains("activity", types);
    }

    [Fact]
    public async Task Subscribers_persist_and_a_participant_never_sees_an_excon_subscriber_or_any_prompt()
    {
        var id = await NewChronicle();
        var agent = """{"system":"s","task":"t {{context}}","output":{"part":"asRun","item":{"kind":"observation","id":"o","at":"{{now}}","by":"x","text":"{{text}}"},"author":{"app":"excon-assistant","via":"agent"}}}""";
        Assert.Equal(HttpStatusCode.OK, (await http.PutAsync($"/api/chronicles/{id}/subscribers/excon", Json($$"""{"app":"excon-assistant","name":"Excon","audience":"excon","agent":{{agent}}}"""))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.PutAsync($"/api/chronicles/{id}/subscribers/dana", Json($$"""{"app":"ghosts","name":"Dana","audience":"participant","viewers":["persona-dana"],"agent":{{agent}}}"""))).StatusCode);

        var reloaded = new SubscriberStore(factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Chronicle")!);
        reloaded.Load();
        Assert.Equal(["excon", "dana"], reloaded.For(id).Select(s => s.Id));
        Assert.Equal($"GET /api/chronicles/{id}?audience=participant&viewers=persona-dana", reloaded.Get(id, "dana")!.Query);

        var participant = JsonNode.Parse(await http.GetStringAsync($"/api/chronicles/{id}/subscribers?audience=participant"))!.AsArray();
        var dana = Assert.Single(participant)!;
        Assert.Null(dana["agent"]);
        Assert.True((bool)dana["hasAgent"]!);
        Assert.Equal(2, JsonNode.Parse(await http.GetStringAsync($"/api/chronicles/{id}/subscribers?audience=excon"))!.AsArray().Count);
    }

    [Fact]
    public async Task Context_counts_what_a_view_withholds()
    {
        var participant = JsonNode.Parse(await http.GetStringAsync("/api/chronicles/harbor-ttx/context?audience=participant"))!;
        var designer = JsonNode.Parse(await http.GetStringAsync("/api/chronicles/harbor-ttx/context?audience=designer"))!;
        Assert.Equal((int)designer["items"]!, (int)participant["total"]!);
        Assert.Equal((int)participant["total"]! - (int)participant["items"]!, (int)participant["withheld"]!);
        Assert.True((int)participant["withheld"]! > 0);
        Assert.Equal(0, (int)designer["withheld"]!);
        Assert.DoesNotContain("patient-zero", (string)participant["text"]!);
        Assert.StartsWith("GET /api/chronicles/harbor-ttx?audience=participant", (string)participant["query"]!);
    }

    [Fact]
    public void Agent_output_loses_its_markdown()
    {
        Assert.Equal("Brief\n• one\n• two", AgentRunner.Clean("## **Brief**\n* one\n- two\n"));
    }
}
