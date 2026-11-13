// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;

namespace Chronicle.Api.Models;

/// <summary>
/// Something an app did during the event, as it happened: an NPC browsing, an email sent, an agent at work. Activity is
/// the live feed; it is kept in memory, not in the record. A notable activity also stored an entry, and names it.
/// </summary>
public sealed record Activity
{
    public required string Id { get; init; }
    public required string ChronicleId { get; init; }

    /// <summary>The app that reported it, such as ghosts.</summary>
    public required string Source { get; init; }

    /// <summary>The subscriber it belongs to, when it reported as one.</summary>
    public string? SubscriberId { get; init; }

    /// <summary>Who did it: an item id, such as a persona.</summary>
    public string? Actor { get; init; }

    /// <summary>What kind of thing happened: browse, email, file, chat, login, inject, agent.started, agent.published…</summary>
    public required string Kind { get; init; }

    public string? Text { get; init; }

    /// <summary>The range host it happened on.</summary>
    public string? Host { get; init; }

    public required DateTimeOffset At { get; init; }

    /// <summary>The lowest audience that may see it.</summary>
    public required Audience Visibility { get; init; }

    public JsonObject? Data { get; init; }

    /// <summary>For a notable activity, the sequence of the entry it stored.</summary>
    public long? Sequence { get; init; }

    /// <summary>Whether <paramref name="audience"/>, speaking for <paramref name="viewers"/>, may see it: its visibility, or its actor.</summary>
    public bool SeenBy(Audience audience, IReadOnlyCollection<string> viewers) =>
        Visibility <= audience || (Actor is not null && viewers.Contains(Actor));
}

/// <summary>An activity as an app reports it. With an entry, it is notable: the entry is stored first.</summary>
public sealed record ActivityPost
{
    public required string Source { get; init; }
    public string? SubscriberId { get; init; }
    public string? Actor { get; init; }
    public required string Kind { get; init; }
    public string? Text { get; init; }
    public string? Host { get; init; }
    public DateTimeOffset? At { get; init; }

    /// <summary>Absent means excon, so an unmarked report fails closed; for a notable one, the entry's item's visibility.</summary>
    public Audience? Visibility { get; init; }

    public JsonObject? Data { get; init; }

    /// <summary>An entry to store, exactly as POST /entries takes it.</summary>
    public Entry? Entry { get; init; }
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

/// <summary>An agent the Chronicle runs for a subscriber: it reads the subscriber's view, writes with a model, publishes.</summary>
public sealed record AgentSpec
{
    public required string System { get; init; }

    /// <summary>The job; {{context}} is replaced by the subscriber's view as text, which is otherwise appended.</summary>
    public required string Task { get; init; }

    public int? MaxTokens { get; init; }
    public required AgentOutput Output { get; init; }
}

/// <summary>A subscriber as registered.</summary>
public sealed record SubscriberPut
{
    public required string App { get; init; }
    public required string Name { get; init; }
    public string? Role { get; init; }
    public required Audience Audience { get; init; }
    public List<string> Viewers { get; init; } = [];
    public List<Part> Parts { get; init; } = [];
    public List<string> Kinds { get; init; } = [];
    public string? Model { get; init; }
    public AgentSpec? Agent { get; init; }
}

/// <summary>An app or agent that reads a Chronicle through its own view, with what it has done lately.</summary>
public sealed record Subscriber
{
    public required string Id { get; init; }
    public required string ChronicleId { get; init; }
    public required string App { get; init; }
    public required string Name { get; init; }
    public string? Role { get; init; }
    public required Audience Audience { get; init; }
    public List<string> Viewers { get; init; } = [];
    public List<Part> Parts { get; init; } = [];
    public List<string> Kinds { get; init; } = [];
    public string? Model { get; init; }

    /// <summary>The agent, shown to exercise control and designers only.</summary>
    public AgentSpec? Agent { get; init; }

    public bool HasAgent { get; init; }

    /// <summary>The read it subscribes with, e.g. GET /api/chronicles/x?audience=participant&amp;viewers=p.</summary>
    public required string Query { get; init; }

    public required DateTimeOffset Registered { get; init; }
    public DateTimeOffset? LastSeen { get; init; }
    public string? LastActivity { get; init; }
    public int ActivityCount { get; init; }
    public int Published { get; init; }
    public long? LastSequence { get; init; }
}

/// <summary>A view of a Chronicle, as text a model can read.</summary>
public sealed record ContextView(string Query, int Items, int Total, int Withheld, int Tokens, string Text, IReadOnlyList<string> Sample);

/// <summary>An agent run, started.</summary>
public sealed record RunStarted(string RunId, string SubscriberId);
