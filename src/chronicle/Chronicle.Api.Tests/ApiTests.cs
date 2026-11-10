// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Chronicle.Api.Tests;

/// <summary>The API on a fresh database, seeded with the sample Chronicles.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string db = Path.Combine(Path.GetTempPath(), $"chronicle-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Chronicle", $"Data Source={db};Pooling=false");
        builder.UseSetting("Samples:Directory", Path.Combine(Root(), "samples"));
        builder.UseSetting("Samples:Seed", "true");
    }

    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, ".."));

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        File.Delete(db);
    }
}

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly Author Me = new() { App = "test", Via = Via.App };
    private readonly HttpClient http = factory.CreateClient();

    private static StringContent Json<T>(T value) => new(ChronicleJson.Serialize(value), Encoding.UTF8, "application/json");

    private async Task<string> NewChronicle()
    {
        var id = $"t-{Guid.NewGuid():N}";
        var response = await http.PostAsync("/api/chronicles", Json(ChronicleRecord.Start(id, "Test", EventKind.Training, DateTimeOffset.UtcNow)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return id;
    }

    [Fact]
    public async Task The_sample_is_seeded_and_listed()
    {
        var list = JsonNode.Parse(await http.GetStringAsync("/api/chronicles"))!.AsArray();
        Assert.Contains(list, c => (string)c!["id"]! == "harbor-ttx");
    }

    [Fact]
    public async Task A_read_is_a_participant_view_unless_asked_otherwise()
    {
        var participant = await http.GetStringAsync("/api/chronicles/harbor-ttx");
        Assert.DoesNotContain("patient-zero", participant);
        Assert.Contains("patient-zero", await http.GetStringAsync("/api/chronicles/harbor-ttx?audience=excon"));
        Assert.Contains("harbor-master-away", await http.GetStringAsync("/api/chronicles/harbor-ttx?viewers=office-staff"));
    }

    [Fact]
    public async Task Entries_are_published_and_a_stale_revise_is_a_conflict()
    {
        var id = await NewChronicle();
        var add = Entry.Add(id, Part.World, new Fact { Id = "f", Text = "one", Visibility = Audience.Participant }, Me, DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.Created, (await http.PostAsync($"/api/chronicles/{id}/entries", Json(add))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"/api/chronicles/{id}/entries", Json(add))).StatusCode);

        var revise = Entry.Revise(id, Part.World, new Fact { Id = "f", Text = "two" }, add.Id, Me, DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.Created, (await http.PostAsync($"/api/chronicles/{id}/entries", Json(revise))).StatusCode);

        var stale = Entry.Revise(id, Part.World, new Fact { Id = "f", Text = "three" }, add.Id, Me, DateTimeOffset.UtcNow);
        var conflict = await http.PostAsync($"/api/chronicles/{id}/entries", Json(stale));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("STALE", (string)JsonNode.Parse(await conflict.Content.ReadAsStringAsync())!["code"]!);

        var history = JsonNode.Parse(await http.GetStringAsync($"/api/chronicles/{id}/entries?audience=designer"))!.AsArray();
        Assert.Equal([1L, 2L], history.Select(e => (long)e!["sequence"]!));
    }

    [Fact]
    public async Task A_bad_entry_is_refused_with_its_rule()
    {
        var id = await NewChronicle();
        var misplaced = Entry.Add(id, Part.Lessons, new Fact { Id = "f", Text = "x" }, Me, DateTimeOffset.UtcNow);
        var response = await http.PostAsync($"/api/chronicles/{id}/entries", Json(misplaced));
        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Contains("KIND_NOT_IN_PART", await response.Content.ReadAsStringAsync());

        var malformed = new StringContent("""{ "id": "nope" }""", Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsync($"/api/chronicles/{id}/entries", malformed)).StatusCode);
    }

    [Fact]
    public async Task An_import_must_match_its_history()
    {
        var sample = ChronicleJson.Deserialize<ChronicleRecord>(await http.GetStringAsync("/api/chronicles/harbor-ttx?audience=designer"));
        var tampered = sample with { Id = "tampered", History = [], };
        var response = await http.PostAsync("/api/chronicles", Json(tampered));
        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync("/api/chronicles", Json(sample))).StatusCode);
    }

    [Fact]
    public async Task A_subscriber_gets_the_history_then_live_entries_it_may_see()
    {
        var id = await NewChronicle();
        var secret = Entry.Add(id, Part.World, new Fact { Id = "secret", Text = "ground truth" }, Me, DateTimeOffset.UtcNow);
        await http.PostAsync($"/api/chronicles/{id}/entries", Json(secret));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        // The response's headers may wait for its first event, so publish while the subscription is opening.
        var opening = http.GetStreamAsync($"/api/chronicles/{id}/entries/stream?parts=world", cts.Token);
        await Task.Delay(300, cts.Token);
        var open = Entry.Add(id, Part.World, new Fact { Id = "open", Text = "public", Visibility = Audience.Participant }, Me, DateTimeOffset.UtcNow);
        await http.PostAsync($"/api/chronicles/{id}/entries", Json(open));
        using var stream = await opening;
        using var reader = new StreamReader(stream);

        string? data = null;
        while (data is null && await reader.ReadLineAsync(cts.Token) is { } line)
            if (line.StartsWith("data: "))
                data = line["data: ".Length..];
        var entry = ChronicleJson.Deserialize<Entry>(data!);
        Assert.Equal(("open", 2L), (entry.ItemId, entry.Sequence!.Value));
    }

    [Fact]
    public async Task A_published_entry_has_its_own_address_and_hidden_ones_are_not_found()
    {
        var id = await NewChronicle();
        var open = Entry.Add(id, Part.World, new Fact { Id = "open", Text = "x", Visibility = Audience.Participant }, Me, DateTimeOffset.UtcNow);
        var created = await http.PostAsync($"/api/chronicles/{id}/entries", Json(open));
        Assert.Equal($"/api/chronicles/{id}/entries/1", created.Headers.Location!.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync($"/api/chronicles/{id}/entries/1")).StatusCode);

        var secret = Entry.Add(id, Part.World, new Fact { Id = "secret", Text = "x" }, Me, DateTimeOffset.UtcNow);
        await http.PostAsync($"/api/chronicles/{id}/entries", Json(secret));
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/chronicles/{id}/entries/2")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync($"/api/chronicles/{id}/entries/2?audience=excon")).StatusCode);
    }

    [Fact]
    public async Task Entries_filter_by_repeated_parts_and_kinds()
    {
        var history = JsonNode.Parse(await http.GetStringAsync(
            "/api/chronicles/harbor-ttx/entries?audience=designer&parts=world&parts=intent&kinds=fact&kinds=rule"))!.AsArray();
        Assert.NotEmpty(history);
        Assert.All(history, e => Assert.Contains((string)e!["kind"]!, new[] { "fact", "rule" }));
    }

    [Fact]
    public async Task Validate_checks_without_storing()
    {
        var record = ChronicleRecord.Start("never-stored", "x", EventKind.Training, DateTimeOffset.UtcNow);
        var findings = JsonNode.Parse(await (await http.PostAsync("/api/chronicles/validate", Json(record))).Content.ReadAsStringAsync())!.AsArray();
        Assert.Empty(findings);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/chronicles/never-stored")).StatusCode);
    }

    [Fact]
    public async Task Unknown_chronicles_and_bad_audiences_are_refused()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/chronicles/nope")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/chronicles/harbor-ttx?audience=everyone")).StatusCode);
    }

    [Fact]
    public async Task The_openapi_document_lists_each_controller()
    {
        var paths = JsonNode.Parse(await http.GetStringAsync("/openapi/v1.json"))!["paths"]!.AsObject().Select(p => p.Key).ToList();
        Assert.Contains("/api/chronicles", paths);
        Assert.Contains("/api/chronicles/{chronicleId}/entries/stream", paths);
        Assert.Contains("/api/schema/record", paths);
    }

    [Fact]
    public async Task The_schemas_are_served()
    {
        var schema = JsonNode.Parse(await http.GetStringAsync("/api/schema/entry"))!;
        Assert.Equal("urn:ranger:chronicle:entry:v0", (string)schema["$id"]!);
    }
}
