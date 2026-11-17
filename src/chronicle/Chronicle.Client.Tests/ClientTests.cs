// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Chronicle.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Chronicle.Client.Tests;

/// <summary>The Chronicle API, in memory, on a fresh database, without the samples.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string db = Path.Combine(Path.GetTempPath(), $"chronicle-client-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Chronicle", $"Data Source={db};Pooling=false");
        builder.UseSetting("Samples:Seed", "false");
    }

    /// <summary>A client on the in-memory API, optionally through <paramref name="through"/>.</summary>
    public ChronicleClient Client(DelegatingHandler? through = null, Action<ChronicleClientOptions>? configure = null, TimeSpan? timeout = null)
    {
        HttpMessageHandler handler = Server.CreateHandler();
        if (through is not null)
        {
            through.InnerHandler = handler;
            handler = through;
        }
        var options = new ChronicleClientOptions { Url = "http://localhost", App = "test", RetryDelay = TimeSpan.FromMilliseconds(20) };
        configure?.Invoke(options);
        return new ChronicleClient(new HttpClient(handler) { Timeout = timeout ?? TimeSpan.FromSeconds(30) }, options);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        File.Delete(db);
    }
}

public class ClientTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);

    private async Task<string> NewChronicle(ChronicleClient client)
    {
        var id = $"t-{Guid.NewGuid():N}";
        await client.CreateAsync(ChronicleRecord.Start(id, "Test", EventKind.Training, DateTimeOffset.UtcNow));
        return id;
    }

    private static Fact Fact(string id, string text, Audience visibility = Audience.Participant, params string[] knownBy) =>
        new() { Id = id, Text = text, Visibility = visibility, KnownBy = knownBy };

    private static Subscription All(string id, Audience audience = Audience.Participant, params string[] viewers) =>
        new() { ChronicleId = id, Audience = audience, Viewers = viewers };

    /// <summary>The first <paramref name="count"/> entries a subscription delivers.</summary>
    private static async Task<List<T>> Take<T>(IAsyncEnumerable<T> stream, int count, CancellationTokenSource cts)
    {
        var got = new List<T>();
        await foreach (var item in stream.WithCancellation(cts.Token))
        {
            got.Add(item);
            if (got.Count == count)
                break;
        }
        return got;
    }

    [Fact]
    public async Task A_subscriber_hears_the_history_then_each_new_entry()
    {
        var client = api.Client();
        var id = await NewChronicle(client);
        await client.AddAsync(id, Part.World, Fact("f-1", "Before"));

        using var cts = new CancellationTokenSource(Patience);
        var listening = Take(client.SubscribeAsync(All(id), cts.Token), 2, cts);
        await Task.Delay(200);
        await client.AddAsync(id, Part.World, Fact("f-2", "After"));

        var got = await listening;
        Assert.Equal(["f-1", "f-2"], got.Select(e => e.ItemId));
        Assert.Equal([1L, 2L], got.Select(e => e.Sequence!.Value));
        Assert.Equal("After", ((Fact)got[1].Item!).Text);
    }

    [Fact]
    public async Task Each_audience_hears_only_what_it_may_see()
    {
        var client = api.Client();
        var id = await NewChronicle(client);
        await client.AddAsync(id, Part.World, new Persona { Id = "persona-clerk", Name = "Bank clerk", Origin = PersonaOrigin.Authored, Visibility = Audience.Participant });
        await client.AddAsync(id, Part.World, Fact("f-truth", "The twist", Audience.Excon));
        await client.AddAsync(id, Part.World, Fact("f-bank", "The clerk's bank", Audience.Excon, "persona-clerk"));
        await client.AddAsync(id, Part.World, Fact("f-public", "Everyone knows"));

        Assert.Equal(["persona-clerk", "f-public"], (await client.GetEntriesAsync(All(id))).Select(e => e.ItemId));
        Assert.Equal(["persona-clerk", "f-bank", "f-public"], (await client.GetEntriesAsync(All(id, Audience.Participant, "persona-clerk"))).Select(e => e.ItemId));
        Assert.Equal(4, (await client.GetEntriesAsync(All(id, Audience.Excon))).Count);

        using var cts = new CancellationTokenSource(Patience);
        var heard = await Take(client.SubscribeAsync(All(id, Audience.Participant, "persona-clerk"), cts.Token), 3, cts);
        Assert.Equal(["persona-clerk", "f-bank", "f-public"], heard.Select(e => e.ItemId));
    }

    [Fact]
    public async Task Publishing_the_same_entry_twice_stores_it_once()
    {
        var client = api.Client();
        var id = await NewChronicle(client);
        var entry = Entry.Add(id, Part.World, Fact("f-1", "Once"), new Author { App = "test", Via = Via.App }, DateTimeOffset.UtcNow);

        var first = await client.PublishAsync(entry);
        var again = await client.PublishAsync(entry);

        Assert.True(first.Created);
        Assert.False(again.Created);
        Assert.Equal(first.Sequence, again.Sequence);
        Assert.Single(await client.GetEntriesAsync(All(id)));
    }

    [Fact]
    public async Task Revising_a_replaced_version_is_refused_as_stale()
    {
        var client = api.Client();
        var id = await NewChronicle(client);
        var added = await client.AddAsync(id, Part.World, Fact("f-1", "v1"));
        await client.ReviseAsync(id, Part.World, Fact("f-1", "v2"), added.Entry.Id);

        var e = await Assert.ThrowsAsync<ChronicleApiException>(() => client.ReviseAsync(id, Part.World, Fact("f-1", "v2 again"), added.Entry.Id));
        Assert.True(e.IsConflict);
        Assert.Equal("STALE", e.Code);
    }

    [Fact]
    public async Task A_missing_Chronicle_reads_as_null_and_refuses_a_subscription()
    {
        var client = api.Client();
        Assert.Null(await client.GetAsync("no-such-chronicle"));
        Assert.Null(await client.GetJsonAsync("no-such-chronicle"));

        var e = await Assert.ThrowsAsync<ChronicleApiException>(async () =>
        {
            await foreach (var _ in client.SubscribeAsync(All("no-such-chronicle")))
            {
            }
        });
        Assert.True(e.IsNotFound);
    }

    [Fact]
    public async Task A_dropped_subscription_reconnects_and_resumes_without_repeating()
    {
        var client = api.Client(new DropFirstStream());
        var id = await NewChronicle(client);
        await client.AddAsync(id, Part.World, Fact("f-1", "Before the drop"));

        using var cts = new CancellationTokenSource(Patience);
        var listening = Take(client.SubscribeAsync(All(id), cts.Token), 2, cts);
        await Task.Delay(300);
        await client.AddAsync(id, Part.World, Fact("f-2", "After the drop"));

        Assert.Equal(["f-1", "f-2"], (await listening).Select(e => e.ItemId));
    }

    [Fact]
    public async Task A_subscription_waits_for_an_API_that_is_not_up_yet()
    {
        var refusing = new RefuseFirst(3);
        var client = api.Client(refusing);
        var id = await NewChronicle(api.Client());
        await client.AddAsync(id, Part.World, Fact("f-1", "Eventually"));

        using var cts = new CancellationTokenSource(Patience);
        var got = await Take(client.SubscribeAsync(All(id), cts.Token), 1, cts);
        Assert.Equal("f-1", got[0].ItemId);
        Assert.True(refusing.Refused >= 3);
    }

    [Fact]
    public async Task A_quiet_subscription_outlives_the_HttpClient_timeout()
    {
        var client = api.Client(timeout: TimeSpan.FromSeconds(1));
        var id = await NewChronicle(client);

        using var cts = new CancellationTokenSource(Patience);
        var listening = Take(client.SubscribeAsync(All(id), cts.Token), 1, cts);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await client.AddAsync(id, Part.World, Fact("f-late", "After a quiet spell"));

        Assert.Equal("f-late", (await listening)[0].ItemId);
    }

    [Fact]
    public async Task A_silent_connection_is_replaced_after_the_idle_timeout()
    {
        var client = api.Client(configure: o => o.StreamIdleTimeout = TimeSpan.FromMilliseconds(300));
        var id = await NewChronicle(client);

        using var cts = new CancellationTokenSource(Patience);
        var listening = Take(client.SubscribeAsync(All(id), cts.Token), 1, cts);
        await Task.Delay(TimeSpan.FromSeconds(1));   // several idle reconnects
        await client.AddAsync(id, Part.World, Fact("f-1", "Still heard"));

        Assert.Equal("f-1", (await listening)[0].ItemId);
    }

    [Fact]
    public async Task An_entry_of_an_unknown_kind_is_skipped_typed_and_delivered_raw()
    {
        var stub = new CannedStream(
            """{"id":"0190a1b2-0000-7000-8000-000000000001","chronicleId":"c","sequence":1,"recordedAt":"2026-11-17T00:00:00Z","part":"world","kind":"hologram","op":"add","itemId":"h-1","item":{"kind":"hologram","id":"h-1"},"author":{"app":"future","via":"app"}}""",
            """{"id":"0190a1b2-0000-7000-8000-000000000002","chronicleId":"c","sequence":2,"recordedAt":"2026-11-17T00:00:00Z","part":"world","kind":"fact","op":"add","itemId":"f-1","item":{"kind":"fact","id":"f-1","visibility":"participant","text":"Known"},"author":{"app":"test","via":"app"}}""");
        var client = new ChronicleClient(new HttpClient(stub), new ChronicleClientOptions { Url = "http://stub", RetryDelay = TimeSpan.FromMilliseconds(20) });

        using var cts = new CancellationTokenSource(Patience);
        var typed = await Take(client.SubscribeAsync(All("c"), cts.Token), 1, cts);
        Assert.Equal("f-1", typed[0].ItemId);

        var raw = await Take(client.SubscribeRawAsync(All("c"), cts.Token), 2, cts);
        Assert.Equal(["hologram", "fact"], raw.Select(r => r.Kind));
        Assert.False(raw[0].TryRead(out _, out _));

        var strict = new ChronicleClient(new HttpClient(stub), new ChronicleClientOptions { Url = "http://stub", UnreadableEntries = UnreadableEntries.Throw });
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => Take(strict.SubscribeAsync(All("c"), cts.Token), 1, cts));
    }

    [Fact]
    public async Task Notable_activity_stores_its_entry()
    {
        var client = api.Client();
        var id = await NewChronicle(client);
        var entry = Entry.Add(id, Part.AsRun,
            new ParticipantAction { Id = "act-1", At = DateTimeOffset.UtcNow, By = "blue team", Text = "Isolated the host", Visibility = Audience.Participant },
            new Author { App = "ghosts", Via = Via.App }, DateTimeOffset.UtcNow);

        var browsed = await client.ReportActivityAsync(id, new ActivityReport { Kind = "browse", Actor = "persona-clerk", Text = "Read the news", Visibility = Audience.Participant });
        var notable = await client.ReportActivityAsync(id, new ActivityReport { Kind = "chat", Text = "Told the team", Entry = entry });

        Assert.Equal("test", browsed.Source);
        Assert.Null(browsed.Sequence);
        Assert.Equal(1, notable.Sequence);
        Assert.Contains(await client.GetActivityAsync(id, ChronicleView.Participant), a => a.Kind == "browse");
        Assert.Equal("act-1", Assert.Single(await client.GetEntriesAsync(All(id))).ItemId);
    }

    [Fact]
    public async Task An_app_registers_as_a_subscriber_and_reads_its_context()
    {
        var client = api.Client();
        var id = await NewChronicle(client);
        await client.AddAsync(id, Part.World, Fact("f-public", "Everyone knows"));
        await client.AddAsync(id, Part.World, Fact("f-truth", "The twist", Audience.Excon));

        var registered = await client.RegisterSubscriberAsync(id, "ghosts-clerk",
            new SubscriberRegistration { Name = "GHOSTS · Clerk", Audience = Audience.Participant, Parts = [Part.World] });
        Assert.Equal("test", registered.App);
        Assert.Contains(await client.GetSubscribersAsync(id, ChronicleView.Excon), s => s.Id == "ghosts-clerk");

        var context = await client.GetContextAsync(new Subscription { ChronicleId = id, Audience = Audience.Participant, Parts = [Part.World] });
        Assert.Equal(1, context.Items);
        Assert.Equal(1, context.Withheld);
        Assert.Contains("Everyone knows", context.Text);

        Assert.True(await client.UnregisterSubscriberAsync(id, "ghosts-clerk"));
        Assert.False(await client.UnregisterSubscriberAsync(id, "ghosts-clerk"));
    }

    [Fact]
    public async Task An_unreachable_API_throws_unavailable_after_its_retries()
    {
        var client = new ChronicleClient(new HttpClient(), new ChronicleClientOptions
        {
            Url = "http://127.0.0.1:9", Retries = 2, RetryDelay = TimeSpan.FromMilliseconds(10)
        });
        var e = await Assert.ThrowsAsync<ChronicleApiException>(() => client.ListAsync());
        Assert.True(e.IsUnavailable);
        Assert.Null(e.Status);
    }

    /// <summary>Cuts the first subscription off after its first read, as a dropped connection would.</summary>
    private sealed class DropFirstStream : DelegatingHandler
    {
        private int streams;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = await base.SendAsync(request, ct);
            if (!request.RequestUri!.AbsolutePath.EndsWith("/entries/stream") || Interlocked.Increment(ref streams) > 1)
                return response;
            var cut = new StreamContent(new BreaksAfterOneRead(await response.Content.ReadAsStreamAsync(ct)));
            foreach (var header in response.Content.Headers)
                cut.Headers.TryAddWithoutValidation(header.Key, header.Value);
            response.Content = cut;
            return response;
        }
    }

    private sealed class BreaksAfterOneRead(Stream inner) : Stream
    {
        private int reads;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
            ++reads > 1 ? throw new IOException("The connection dropped.") : await inner.ReadAsync(buffer, ct);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                inner.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>Refuses the first few subscriptions as an API that isn't up yet would.</summary>
    private sealed class RefuseFirst(int count) : DelegatingHandler
    {
        public int Refused;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/entries/stream") && Interlocked.Increment(ref Refused) <= count)
                throw new HttpRequestException("Connection refused");
            return base.SendAsync(request, ct);
        }
    }

    /// <summary>A stream that serves the same entries, then ends.</summary>
    private sealed class CannedStream(params string[] entries) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var text = new StringBuilder();
            for (var i = 0; i < entries.Length; i++)
                text.Append($"event: entry\nid: {i + 1}\ndata: {entries[i]}\n\n");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(text.ToString(), Encoding.UTF8, "text/event-stream")
            });
        }
    }
}
