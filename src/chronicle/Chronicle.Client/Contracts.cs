// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Chronicle.Client;

// What the Chronicle API takes and answers besides the spec's own types (records and entries). These are the API's,
// not the spec's: the client reads them leniently, so a newer API that adds a field doesn't break an older app.

/// <summary>Who is reading: an audience, and the personas, pools, sides or organizations it speaks for.</summary>
/// <param name="Audience">participant, excon or designer. Participant sees the least.</param>
/// <param name="Viewers">Ids matched against each item's <c>knownBy</c>, widening what the audience may see.</param>
public sealed record ChronicleView(Audience Audience, IReadOnlyList<string>? Viewers = null)
{
    /// <summary>What participants see.</summary>
    public static ChronicleView Participant { get; } = new(Audience.Participant);

    /// <summary>What exercise control sees: the ground truth and the plan.</summary>
    public static ChronicleView Excon { get; } = new(Audience.Excon);

    /// <summary>Everything, designers' notes too.</summary>
    public static ChronicleView Designer { get; } = new(Audience.Designer);
}

/// <summary>One Chronicle, as the API lists it.</summary>
public sealed record ChronicleSummary(string Id, string Name, EventKind Kind, ScenarioTypeRef? ScenarioType, DateTimeOffset Created, long Entries, bool Archived);

/// <summary>An entry as the API stored it, and whether this publish stored it (false: it was stored before, by this id).</summary>
public sealed record Published(Entry Entry, bool Created)
{
    /// <summary>Its place in the Chronicle's history.</summary>
    public long Sequence => Entry.Sequence ?? 0;
}

/// <summary>
/// An entry as it came off the wire, read only as far as routing needs; <see cref="Json"/> holds all of it. A raw
/// subscription delivers these, so an app keeps working when the spec adds a kind this library doesn't know yet.
/// </summary>
public sealed record RawEntry(long Sequence, Guid Id, string ChronicleId, string Part, string Kind, string Op, string ItemId, JsonElement Json)
{
    /// <summary>Reads the entry; false when this library can't, with why.</summary>
    public bool TryRead(out Entry? entry, out JsonException? error)
    {
        try
        {
            entry = Json.Deserialize<Entry>(ChronicleJson.Options) ?? throw new JsonException("The entry is null.");
            error = null;
            return true;
        }
        catch (JsonException e)
        {
            entry = null;
            error = e;
            return false;
        }
    }

    /// <summary>Reads the entry.</summary>
    /// <exception cref="JsonException">This library can't read it.</exception>
    public Entry Read() => TryRead(out var entry, out var error) ? entry! : throw error!;

    internal static RawEntry Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone();
        string Text(string name) => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()!
            : throw new JsonException($"The entry has no '{name}'.");
        var sequence = root.TryGetProperty("sequence", out var s) && s.TryGetInt64(out var n)
            ? n
            : throw new JsonException("The entry has no 'sequence'.");
        return new RawEntry(sequence, Guid.Parse(Text("id")), Text("chronicleId"), Text("part"), Text("kind"), Text("op"), Text("itemId"), root);
    }
}

/// <summary>
/// Something this app did during the event, as it happened: an NPC browsing, an email sent. Activity is the live feed;
/// the API keeps the latest in memory, not in the record. With an <see cref="Entry"/> it is notable: the entry is
/// stored first, and becomes part of the record.
/// </summary>
public sealed record ActivityReport
{
    /// <summary>What kind of thing happened: browse, email, file, chat, login, inject…</summary>
    public required string Kind { get; init; }

    /// <summary>The app that did it; absent means <see cref="ChronicleClientOptions.App"/>.</summary>
    public string? Source { get; init; }

    /// <summary>The subscriber it belongs to, when the app reports as one.</summary>
    public string? SubscriberId { get; init; }

    /// <summary>Who did it: an item id, such as a persona.</summary>
    public string? Actor { get; init; }

    /// <summary>What happened, in words.</summary>
    public string? Text { get; init; }

    /// <summary>The range host it happened on.</summary>
    public string? Host { get; init; }

    /// <summary>When; absent means now.</summary>
    public DateTimeOffset? At { get; init; }

    /// <summary>The lowest audience that may see it. Absent means excon, or for a notable one its item's visibility.</summary>
    public Audience? Visibility { get; init; }

    /// <summary>Anything else the app wants to show with it.</summary>
    public JsonObject? Data { get; init; }

    /// <summary>An entry to store with it, making it notable.</summary>
    public Entry? Entry { get; init; }
}

/// <summary>An activity as the API recorded it.</summary>
public sealed record Activity
{
    public required string Id { get; init; }
    public required string ChronicleId { get; init; }
    public required string Source { get; init; }
    public string? SubscriberId { get; init; }
    public string? Actor { get; init; }
    public required string Kind { get; init; }
    public string? Text { get; init; }
    public string? Host { get; init; }
    public required DateTimeOffset At { get; init; }
    public required Audience Visibility { get; init; }
    public JsonObject? Data { get; init; }

    /// <summary>For a notable activity, the sequence of the entry it stored.</summary>
    public long? Sequence { get; init; }
}

/// <summary>
/// A subscriber as an app registers it, so the Chronicle (and its UI) knows who reads what. Registering is optional:
/// subscribing works without it.
/// </summary>
public sealed record SubscriberRegistration
{
    /// <summary>The app, such as ghosts; absent means <see cref="ChronicleClientOptions.App"/>.</summary>
    public string? App { get; init; }

    /// <summary>What the Chronicle UI calls it, such as "GHOSTS · Dana Whitlock".</summary>
    public required string Name { get; init; }

    /// <summary>What it does, in a few words.</summary>
    public string? Role { get; init; }

    public required Audience Audience { get; init; }
    public IReadOnlyList<string> Viewers { get; init; } = [];
    public IReadOnlyList<Part> Parts { get; init; } = [];
    public IReadOnlyList<string> Kinds { get; init; } = [];

    /// <summary>The model it uses, for display.</summary>
    public string? Model { get; init; }

    /// <summary>An agent for the Chronicle to run on the subscriber's behalf; absent when the app does its own work.</summary>
    public AgentSpec? Agent { get; init; }
}

/// <summary>An agent the Chronicle runs for a subscriber: it reads the subscriber's view, writes with a model, publishes.</summary>
public sealed record AgentSpec
{
    public required string System { get; init; }

    /// <summary>The job; <c>{{context}}</c> is replaced by the subscriber's view as text.</summary>
    public required string Task { get; init; }

    public int? MaxTokens { get; init; }
    public required AgentOutput Output { get; init; }
}

/// <summary>What a subscriber's agent writes, and where: an item template and its author.</summary>
public sealed record AgentOutput
{
    public required Part Part { get; init; }

    /// <summary>An item; its strings may hold {{text}}, {{title}}, {{body}}, {{now}} and {{runId}}.</summary>
    public required JsonObject Item { get; init; }

    public required Author Author { get; init; }
    public string? Reason { get; init; }
}

/// <summary>A subscriber as the API holds it, with what it has done lately.</summary>
public sealed record Subscriber
{
    public required string Id { get; init; }
    public required string ChronicleId { get; init; }
    public required string App { get; init; }
    public required string Name { get; init; }
    public string? Role { get; init; }
    public required Audience Audience { get; init; }
    public IReadOnlyList<string> Viewers { get; init; } = [];
    public IReadOnlyList<Part> Parts { get; init; } = [];
    public IReadOnlyList<string> Kinds { get; init; } = [];
    public string? Model { get; init; }
    public AgentSpec? Agent { get; init; }
    public bool HasAgent { get; init; }
    public string? Query { get; init; }
    public DateTimeOffset Registered { get; init; }
    public DateTimeOffset? LastSeen { get; init; }
    public string? LastActivity { get; init; }
    public int ActivityCount { get; init; }
    public int Published { get; init; }
    public long? LastSequence { get; init; }
}

/// <summary>A view of a Chronicle as text a model can read, and how much of the record it withholds.</summary>
public sealed record ContextView(string Query, int Items, int Total, int Withheld, int Tokens, string Text, IReadOnlyList<string> Sample);

/// <summary>An agent run, started; follow it on the live feed.</summary>
public sealed record RunStarted(string RunId, string SubscriberId);
