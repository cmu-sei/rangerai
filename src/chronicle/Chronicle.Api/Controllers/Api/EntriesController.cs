// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Chronicle.Api.Models;
using Chronicle.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Controllers.Api;

/// <summary>
/// A Chronicle's entries: what apps publish, and what subscribers hear. Entries are never changed or removed, so this
/// resource takes no PUT or DELETE.
/// </summary>
[Route("api/chronicles/{chronicleId}/entries")]
public sealed class EntriesController(ChronicleStore store) : ApiControllerBase
{
    /// <summary>The history, filtered by audience, parts, kinds and position, as a subscription would be.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<Entry>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<Entry>> List(string chronicleId, [FromQuery] EntryQuery query) =>
        store.Get(chronicleId) is { } record
            ? Ok(Visible(record, query.ToSubscription(chronicleId)).Select(s => s.Entry).ToList())
            : ChronicleNotFound(chronicleId);

    /// <summary>One entry, by its sequence number, if the audience may see it.</summary>
    [HttpGet("{sequence:long}")]
    [ProducesResponseType<Entry>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<Entry> Get(string chronicleId, long sequence, [FromQuery] ViewQuery view)
    {
        if (store.Get(chronicleId) is not { } record)
            return ChronicleNotFound(chronicleId);
        var found = Projection.Subjects(record).FirstOrDefault(s => s.Entry.Sequence == sequence);
        return found.Entry is not null && AudienceView.Sees(found.Subject, view.AudienceOrDefault, view.Viewers)
            ? found.Entry
            : Problem(StatusCodes.Status404NotFound, "NOT_FOUND", $"Chronicle '{chronicleId}' has no entry {sequence} this audience may see.");
    }

    /// <summary>
    /// Publishes an entry. 201 when it is stored; 200 with the stored copy when the same entry was stored before, so a
    /// publisher can retry safely; 409 STALE or ID_TAKEN when it conflicts with what others wrote; 422 for any other rule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<Entry>(StatusCodes.Status201Created)]
    [ProducesResponseType<Entry>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Entry>> Publish(string chronicleId, [FromBody] Entry entry, CancellationToken ct)
    {
        if (store.Get(chronicleId) is null)
            return ChronicleNotFound(chronicleId);
        if (entry.ChronicleId != chronicleId)
            return Problem(StatusCodes.Status400BadRequest, "WRONG_CHRONICLE", $"The entry is for Chronicle '{entry.ChronicleId}', not '{chronicleId}'.");

        // A broken rule throws ChronicleException; ChronicleExceptionHandler answers it.
        var (stored, created) = await store.Append(chronicleId, entry, ct);
        return created
            ? CreatedAtAction(nameof(Get), new { chronicleId, sequence = stored.Sequence }, stored)
            : Ok(stored);
    }

    /// <summary>
    /// Subscribes, as server-sent events: the history after <c>after</c>, then each new entry as it is stored. Each event
    /// is an entry, with its sequence as the event id; a reconnecting client's Last-Event-ID resumes from there.
    /// </summary>
    [HttpGet("stream")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IResult Stream(string chronicleId, [FromQuery] EntryQuery query,
        [FromHeader(Name = "Last-Event-ID")] long? lastEventId, CancellationToken ct)
    {
        if (store.Subscribe(chronicleId) is not { } hearing)
            return Results.Problem($"There is no Chronicle '{chronicleId}'.", statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["code"] = "NOT_FOUND" });
        var subscription = query.ToSubscription(chronicleId, lastEventId);
        return TypedResults.ServerSentEvents(Events(hearing.Record, hearing.Reader, hearing.Stop, subscription, ct));
    }

    private static async IAsyncEnumerable<SseItem<string>> Events(ChronicleRecord record, ChannelReader<Stored> reader,
        IDisposable stop, Subscription subscription, [EnumeratorCancellation] CancellationToken ct)
    {
        using (stop)
        {
            foreach (var stored in Visible(record, subscription))
                yield return Event(stored.Entry);

            // The record was taken as hearing began, so anything newer arrives on the channel; skip what it already held.
            var sent = Math.Max(subscription.AfterSequence, record.History.Count);
            await foreach (var stored in reader.ReadAllAsync(ct))
                if (stored.Entry.Sequence > sent && Wants(subscription, stored))
                    yield return Event(stored.Entry);
        }
    }

    private static IEnumerable<Stored> Visible(ChronicleRecord record, Subscription subscription) =>
        Projection.Subjects(record).Select(s => new Stored(s.Entry, s.Subject)).Where(s => Wants(subscription, s));

    private static bool Wants(Subscription subscription, Stored stored) =>
        subscription.Wants(stored.Entry) && AudienceView.Sees(stored.Subject, subscription.Audience, [.. subscription.Viewers]);

    private static SseItem<string> Event(Entry entry) =>
        new(ChronicleJson.Serialize(entry, indented: false), "entry") { EventId = entry.Sequence!.Value.ToString() };
}
