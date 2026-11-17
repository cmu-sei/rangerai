// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chronicle.Client.Hosting;

/// <summary>
/// What an app does with each entry it hears. Write one, and register it with
/// <c>services.AddChronicleSubscriber&lt;THandler&gt;(…)</c>; the library connects, reconnects,
/// resumes and calls it once per entry, in order. It is resolved in a fresh scope for each entry, so it may take
/// scoped services such as a DbContext.
/// <para>
/// Delivery is at least once: after a crash, or a failure the subscriber retries, an entry can arrive again. Make
/// handling the same entry twice harmless, for instance by remembering <see cref="Entry.Id"/>.
/// </para>
/// </summary>
public interface IChronicleHandler
{
    /// <summary>Handles one entry. Throwing follows <see cref="ChronicleSubscriberOptions.OnError"/>.</summary>
    Task HandleAsync(Entry entry, CancellationToken ct);
}

/// <summary>What a failing handler does to the subscription.</summary>
public enum HandlerErrors
{
    /// <summary>Wait, then deliver the same entry again, until the handler succeeds. Nothing is lost; a bad entry stalls the subscription.</summary>
    Retry,

    /// <summary>Log it and move on to the next entry. The subscription never stalls; the failed entry is not handled.</summary>
    Skip
}

/// <summary>One hosted subscription: what to subscribe to, and how.</summary>
public sealed class ChronicleSubscriberOptions
{
    /// <summary>The Chronicle to subscribe to.</summary>
    public string ChronicleId { get; set; } = "";

    /// <summary>The audience it reads as. Participant, the least, unless set.</summary>
    public Audience Audience { get; set; } = Audience.Participant;

    /// <summary>The personas, pools, sides or organizations it speaks for, widening what it may see.</summary>
    public List<string> Viewers { get; set; } = [];

    /// <summary>The parts it wants; empty means all.</summary>
    public List<Part> Parts { get; set; } = [];

    /// <summary>The kinds it wants; empty means all.</summary>
    public List<string> Kinds { get; set; } = [];

    /// <summary>What a handler that throws does: retry the entry (the default) or skip it.</summary>
    public HandlerErrors OnError { get; set; } = HandlerErrors.Retry;

    /// <summary>
    /// When set, the subscriber registers itself on the Chronicle under this id as it starts, so the Chronicle UI's
    /// Subscribers page shows it. Registering is best effort: a failure is logged, and the subscription goes on.
    /// </summary>
    public string? SubscriberId { get; set; }

    /// <summary>The name the Chronicle UI shows for a registered subscriber; absent means its id.</summary>
    public string? DisplayName { get; set; }

    /// <summary>What a registered subscriber does, in a few words.</summary>
    public string? Role { get; set; }

    /// <summary>The subscription these options describe, resuming after <paramref name="after"/>.</summary>
    public Subscription ToSubscription(long after) => new()
    {
        ChronicleId = ChronicleId,
        Audience = Audience,
        Viewers = [.. Viewers],
        Parts = [.. Parts],
        Kinds = [.. Kinds],
        AfterSequence = after
    };
}

/// <summary>
/// Where a hosted subscriber keeps the sequence of the last entry it handled, so a restart resumes there. Register one
/// in the container to use it; without one, a restart replays the history from the start.
/// </summary>
public interface ICursorStore
{
    /// <summary>The last sequence handled under <paramref name="key"/>; 0 when none.</summary>
    Task<long> LoadAsync(string key, CancellationToken ct);

    /// <summary>Records <paramref name="sequence"/> as the last handled under <paramref name="key"/>.</summary>
    Task SaveAsync(string key, long sequence, CancellationToken ct);
}

/// <summary>Cursors in memory: a restart replays the history. The default.</summary>
public sealed class InMemoryCursorStore : ICursorStore
{
    private readonly ConcurrentDictionary<string, long> cursors = new();

    /// <inheritdoc />
    public Task<long> LoadAsync(string key, CancellationToken ct) => Task.FromResult(cursors.GetValueOrDefault(key));

    /// <inheritdoc />
    public Task SaveAsync(string key, long sequence, CancellationToken ct)
    {
        cursors[key] = sequence;
        return Task.CompletedTask;
    }
}

/// <summary>Cursors as small files in a directory, one per subscription, so a restart resumes where it left off.</summary>
public sealed class FileCursorStore(string directory) : ICursorStore
{
    /// <inheritdoc />
    public async Task<long> LoadAsync(string key, CancellationToken ct)
    {
        var path = PathOf(key);
        return File.Exists(path) && long.TryParse((await File.ReadAllTextAsync(path, ct)).Trim(), out var sequence) ? sequence : 0;
    }

    /// <inheritdoc />
    public async Task SaveAsync(string key, long sequence, CancellationToken ct)
    {
        Directory.CreateDirectory(directory);
        var path = PathOf(key);
        var temp = $"{path}.tmp";
        await File.WriteAllTextAsync(temp, sequence.ToString(), ct);
        File.Move(temp, path, overwrite: true);
    }

    private string PathOf(string key) =>
        System.IO.Path.Combine(directory, string.Concat(key.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_')) + ".cursor");
}

/// <summary>Runs one hosted subscription: subscribes, calls the handler for each entry, keeps the cursor.</summary>
internal sealed class ChronicleSubscriberService(
    string name,
    IServiceProvider services,
    ChronicleClient client,
    IOptionsMonitor<ChronicleSubscriberOptions> monitor,
    ILogger<ChronicleSubscriberService> log,
    Type handlerType) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var options = monitor.Get(name);
        if (string.IsNullOrWhiteSpace(options.ChronicleId))
        {
            log.LogError("Chronicle subscriber {Name} has no ChronicleId; it won't run.", name);
            return;
        }
        var cursors = services.GetService<ICursorStore>() ?? new InMemoryCursorStore();
        var key = $"{name}.{options.ChronicleId}";
        await Register(options, stop);

        var failures = 0;
        while (!stop.IsCancellationRequested)
        {
            var after = await cursors.LoadAsync(key, stop);
            try
            {
                await foreach (var entry in client.SubscribeAsync(options.ToSubscription(after), stop))
                {
                    try
                    {
                        await using var scope = services.CreateAsyncScope();
                        var handler = (IChronicleHandler)scope.ServiceProvider.GetRequiredService(handlerType);
                        await handler.HandleAsync(entry, stop);
                        failures = 0;
                    }
                    catch (Exception e) when (!stop.IsCancellationRequested)
                    {
                        if (options.OnError == HandlerErrors.Retry)
                        {
                            log.LogError(e, "Chronicle subscriber {Name}: the handler failed on entry {Sequence}; it will get it again.", name, entry.Sequence);
                            throw new HandlerFailed();
                        }
                        log.LogError(e, "Chronicle subscriber {Name}: the handler failed on entry {Sequence}; skipped.", name, entry.Sequence);
                    }
                    await cursors.SaveAsync(key, entry.Sequence ?? after, stop);
                }
            }
            catch (Exception) when (stop.IsCancellationRequested)
            {
                return;   // stopping: whatever the cancellation broke doesn't matter now
            }
            catch (HandlerFailed)
            {
                await Wait(failures++, client.Options, stop);
            }
            catch (Exception e) when (!stop.IsCancellationRequested)
            {
                log.LogError(e, "Chronicle subscriber {Name}: the subscription failed; starting it again.", name);
                await Wait(failures++, client.Options, stop);
            }
        }
    }

    private async Task Register(ChronicleSubscriberOptions options, CancellationToken stop)
    {
        if (options.SubscriberId is not { } id)
            return;
        try
        {
            await client.RegisterSubscriberAsync(options.ChronicleId, id, new SubscriberRegistration
            {
                Name = options.DisplayName ?? id,
                Role = options.Role,
                Audience = options.Audience,
                Viewers = [.. options.Viewers],
                Parts = [.. options.Parts],
                Kinds = [.. options.Kinds]
            }, stop);
        }
        catch (Exception e) when (e is ChronicleApiException or InvalidOperationException)
        {
            log.LogWarning("Chronicle subscriber {Name} didn't register as {Id}: {Message}", name, id, e.Message);
        }
    }

    private static async Task Wait(int attempt, ChronicleClientOptions options, CancellationToken stop)
    {
        var delay = options.RetryDelay * Math.Pow(2, Math.Min(attempt, 16));
        try
        {
            await Task.Delay(delay > options.MaxRetryDelay ? options.MaxRetryDelay : delay, stop);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private sealed class HandlerFailed : Exception;
}
