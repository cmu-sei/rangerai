// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Collections.Concurrent;
using Chronicle.Client.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chronicle.Client.Tests;

public class HostedSubscriberTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    /// <summary>What the handlers heard, by subscriber name.</summary>
    private sealed class Heard
    {
        public ConcurrentQueue<(string Subscriber, Entry Entry)> Entries { get; } = new();
        public int FailuresLeft;
    }

    private sealed class Recorder(Heard heard) : IChronicleHandler
    {
        public Task HandleAsync(Entry entry, CancellationToken ct)
        {
            if (Interlocked.Decrement(ref heard.FailuresLeft) >= 0)
                throw new InvalidOperationException("Not yet.");
            heard.Entries.Enqueue(("recorder", entry));
            return Task.CompletedTask;
        }
    }

    private IHost Host(Heard heard, string chronicleId, string? cursors = null, HandlerErrors onError = HandlerErrors.Retry,
        Action<ChronicleSubscriberOptions>? more = null)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(heard);
        builder.Services.AddChronicleClient(o =>
            {
                o.Url = "http://localhost";
                o.App = "ghosts";
                o.RetryDelay = TimeSpan.FromMilliseconds(20);
            })
            .ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler());
        if (cursors is not null)
            builder.Services.AddSingleton<ICursorStore>(new FileCursorStore(cursors));
        builder.Services.AddChronicleSubscriber<Recorder>("clerk", o =>
        {
            o.ChronicleId = chronicleId;
            o.Audience = Audience.Participant;
            o.Parts = [Part.World];
            o.OnError = onError;
            more?.Invoke(o);
        });
        return builder.Build();
    }

    private static async Task Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting.");
            await Task.Delay(50);
        }
    }

    private async Task<(ChronicleClient Client, string Id)> Chronicle()
    {
        var client = api.Client();
        var id = $"t-{Guid.NewGuid():N}";
        await client.CreateAsync(ChronicleRecord.Start(id, "Test", EventKind.Exercise, DateTimeOffset.UtcNow));
        return (client, id);
    }

    private static Fact Fact(string id) => new() { Id = id, Text = id, Visibility = Audience.Participant };

    [Fact]
    public async Task The_handler_hears_each_entry_and_a_restart_resumes_where_it_left_off()
    {
        var (client, id) = await Chronicle();
        var cursors = Path.Combine(Path.GetTempPath(), $"cursors-{Guid.NewGuid():N}");
        await client.AddAsync(id, Part.World, Fact("f-1"));
        await client.AddAsync(id, Part.Intent, new Objective { Id = "o-1", Text = "Not wanted", Visibility = Audience.Participant });

        var heard = new Heard();
        using (var host = Host(heard, id, cursors))
        {
            await host.StartAsync();
            await client.AddAsync(id, Part.World, Fact("f-2"));
            await Until(() => heard.Entries.Count == 2);
            await host.StopAsync();
        }
        Assert.Equal(["f-1", "f-2"], heard.Entries.Select(h => h.Entry.ItemId));

        await client.AddAsync(id, Part.World, Fact("f-3"));
        var after = new Heard();
        using (var host = Host(after, id, cursors))
        {
            await host.StartAsync();
            await Until(() => after.Entries.Count == 1);
            await Task.Delay(200);
            await host.StopAsync();
        }
        Assert.Equal(["f-3"], after.Entries.Select(h => h.Entry.ItemId));
        Directory.Delete(cursors, recursive: true);
    }

    [Fact]
    public async Task A_failing_handler_gets_the_same_entry_again()
    {
        var (client, id) = await Chronicle();
        await client.AddAsync(id, Part.World, Fact("f-1"));
        await client.AddAsync(id, Part.World, Fact("f-2"));

        var heard = new Heard { FailuresLeft = 2 };
        using var host = Host(heard, id);
        await host.StartAsync();
        await Until(() => heard.Entries.Count == 2);
        await host.StopAsync();

        Assert.Equal(["f-1", "f-2"], heard.Entries.Select(h => h.Entry.ItemId));
    }

    [Fact]
    public async Task A_skipping_subscriber_moves_past_a_failing_entry()
    {
        var (client, id) = await Chronicle();
        await client.AddAsync(id, Part.World, Fact("f-1"));
        await client.AddAsync(id, Part.World, Fact("f-2"));

        var heard = new Heard { FailuresLeft = 1 };
        using var host = Host(heard, id, onError: HandlerErrors.Skip);
        await host.StartAsync();
        await Until(() => heard.Entries.Count == 1);
        await Task.Delay(200);
        await host.StopAsync();

        Assert.Equal(["f-2"], heard.Entries.Select(h => h.Entry.ItemId));
    }

    [Fact]
    public async Task A_subscriber_with_an_id_registers_itself_on_the_Chronicle()
    {
        var (client, id) = await Chronicle();
        var heard = new Heard();
        using var host = Host(heard, id, more: o =>
        {
            o.SubscriberId = "ghosts-clerk";
            o.DisplayName = "GHOSTS · Clerk";
        });
        await host.StartAsync();
        await Until(() => client.GetSubscribersAsync(id, ChronicleView.Excon).Result.Any(s => s.Id == "ghosts-clerk"));
        await host.StopAsync();

        var subscriber = (await client.GetSubscribersAsync(id, ChronicleView.Excon)).Single(s => s.Id == "ghosts-clerk");
        Assert.Equal("ghosts", subscriber.App);
        Assert.Equal("GHOSTS · Clerk", subscriber.Name);
        Assert.Equal([Part.World], subscriber.Parts);
    }
}
