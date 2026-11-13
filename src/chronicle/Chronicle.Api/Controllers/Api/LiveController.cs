// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Chronicle.Api.Models;
using Chronicle.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Controllers.Api;

/// <summary>
/// The live side of a Chronicle: activity as apps report it, the subscribers that read it, their views as text, and a
/// WebSocket that carries all of it, plus each new entry, to an audience as it happens.
/// </summary>
[Route("api/chronicles/{chronicleId}")]
public sealed class LiveController(
    ChronicleStore store, LiveHub hub, SubscriberStore subscribers, ActivityService activity, AgentRunner runner) : ApiControllerBase
{
    /// <summary>
    /// The live feed, as a WebSocket. It opens with <c>hello</c> (the subscribers and recent activity), then sends
    /// <c>entry</c>, <c>activity</c>, <c>subscriber</c> and <c>token</c> messages as they happen, each only if the audience
    /// may see it.
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Live(string chronicleId, [FromQuery] ViewQuery view, CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
            return Problem(StatusCodes.Status400BadRequest, "NOT_WEBSOCKET", "Connect with a WebSocket.");
        if (store.Subscribe(chronicleId) is not { } hearing)
            return ChronicleNotFound(chronicleId);

        var audience = view.AudienceOrDefault;
        var viewers = view.Viewers;
        using var stopEntries = hearing.Stop;
        var (listener, stopLive) = hub.Listen(chronicleId, audience, viewers);
        using var _ = stopLive;
        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        using var closed = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var subscription = new Subscription { ChronicleId = chronicleId, Audience = audience, Viewers = viewers };
        var hello = LiveHub.Serialize(new
        {
            type = "hello",
            chronicleId,
            audience,
            subscribers = Visible(chronicleId, audience),
            activity = hub.Recent(chronicleId, audience, viewers)
        });
        await Send(socket, hello, closed.Token);

        // Entries newer than the record taken as hearing began go on the same queue as live messages.
        var sent = hearing.Record.History.Count;
        var entries = Task.Run(async () =>
        {
            await foreach (var stored in hearing.Reader.ReadAllAsync(closed.Token))
                if (stored.Entry.Sequence > sent && subscription.Wants(stored.Entry) && AudienceView.Sees(stored.Subject, audience, viewers))
                    listener.Queue.Writer.TryWrite(LiveHub.Serialize(new { type = "entry", entry = stored.Entry }));
        }, closed.Token);
        var reading = Task.Run(async () =>
        {
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open)
                if ((await socket.ReceiveAsync(buffer, closed.Token)).MessageType == WebSocketMessageType.Close)
                    break;
            await closed.CancelAsync();
        }, closed.Token);

        try
        {
            await foreach (var json in listener.Queue.Reader.ReadAllAsync(closed.Token))
                await Send(socket, json, closed.Token);
        }
        catch (Exception e) when (e is OperationCanceledException or WebSocketException) { }
        finally
        {
            await closed.CancelAsync();
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
            await Task.WhenAll(entries, reading).ContinueWith(_ => { }, CancellationToken.None);
        }
        return new EmptyResult();
    }

    private static Task Send(WebSocket socket, string json, CancellationToken ct) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, ct);

    private IEnumerable<Subscriber> Visible(string chronicleId, Audience audience) =>
        subscribers.For(chronicleId).Where(s => s.Audience <= audience).Select(s => SubscriberStore.ForAudience(s, audience));

    /// <summary>
    /// Reports activity. With an entry it is notable: the entry is stored first (as POST /entries would) and the
    /// activity carries its sequence. Visibility defaults to excon, or for a notable one to its item's visibility.
    /// </summary>
    [HttpPost("activity")]
    [ProducesResponseType<Activity>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Activity>> Report(string chronicleId, [FromBody] ActivityPost post, CancellationToken ct)
    {
        if (store.Get(chronicleId) is null)
            return ChronicleNotFound(chronicleId);
        if (post.Entry is { } entry && entry.ChronicleId != chronicleId)
            return Problem(StatusCodes.Status400BadRequest, "WRONG_CHRONICLE", $"The entry is for Chronicle '{entry.ChronicleId}', not '{chronicleId}'.");
        var stored = await activity.Report(chronicleId, post, ct);
        return StatusCode(StatusCodes.Status201Created, stored);
    }

    /// <summary>The recent activity an audience may see, oldest first (the last 1,000 are kept, in memory).</summary>
    [HttpGet("activity")]
    [ProducesResponseType<IEnumerable<Activity>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<Activity>> Recent(string chronicleId, [FromQuery] ViewQuery view, [FromQuery] int limit = 200) =>
        store.Get(chronicleId) is null ? ChronicleNotFound(chronicleId) : Ok(hub.Recent(chronicleId, view.AudienceOrDefault, view.Viewers, limit));

    /// <summary>The subscribers an audience may see: those reading at its level or below.</summary>
    [HttpGet("subscribers")]
    [ProducesResponseType<IEnumerable<Subscriber>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<Subscriber>> Subscribers(string chronicleId, [FromQuery] ViewQuery view) =>
        store.Get(chronicleId) is null ? ChronicleNotFound(chronicleId) : Ok(Visible(chronicleId, view.AudienceOrDefault).ToList());

    /// <summary>Registers or replaces a subscriber. With an agent, the Chronicle can run it.</summary>
    [HttpPut("subscribers/{subscriberId}")]
    [ProducesResponseType<Subscriber>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<Subscriber> Register(string chronicleId, string subscriberId, [FromBody] SubscriberPut body)
    {
        if (store.Get(chronicleId) is null)
            return ChronicleNotFound(chronicleId);
        var subscriber = subscribers.Put(chronicleId, subscriberId, body);
        activity.Announce(subscriber);
        return subscriber;
    }

    [HttpDelete("subscribers/{subscriberId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Unregister(string chronicleId, string subscriberId) =>
        subscribers.Delete(chronicleId, subscriberId)
            ? NoContent()
            : Problem(StatusCodes.Status404NotFound, "NOT_FOUND", $"Chronicle '{chronicleId}' has no subscriber '{subscriberId}'.");

    /// <summary>Runs a subscriber's agent in the background; follow it on the live feed.</summary>
    [HttpPost("subscribers/{subscriberId}/run")]
    [ProducesResponseType<RunStarted>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<RunStarted> Run(string chronicleId, string subscriberId)
    {
        if (subscribers.Get(chronicleId, subscriberId) is not { } subscriber)
            return Problem(StatusCodes.Status404NotFound, "NOT_FOUND", $"Chronicle '{chronicleId}' has no subscriber '{subscriberId}'.");
        return runner.Start(subscriber) is { } started
            ? Accepted(started)
            : Problem(StatusCodes.Status409Conflict, "NO_AGENT", $"Subscriber '{subscriberId}' has no agent to run.");
    }

    /// <summary>A view as text a model can read, with how much of the record it withholds.</summary>
    [HttpGet("context")]
    [ProducesResponseType<ContextView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<ContextView> Context(string chronicleId, [FromQuery] EntryQuery query) =>
        store.Get(chronicleId) is { } record
            ? ContextBuilder.Build(record, query.AudienceOrDefault, query.Viewers, query.Parts)
            : ChronicleNotFound(chronicleId);
}
