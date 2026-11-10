// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

/// <summary>
/// What a subscriber asks for: which Chronicle, which parts and kinds, as which audience, from where in the history.
/// The spec doesn't fix a transport; whatever carries entries (a message bus, server-sent events, a webhook) honours
/// these fields the same way.
/// </summary>
public sealed record Subscription
{
    /// <summary>The Chronicle.</summary>
    public required string ChronicleId { get; init; }

    /// <summary>The parts wanted; empty means all.</summary>
    public IReadOnlyList<Part> Parts { get; init; } = [];

    /// <summary>The kinds wanted; empty means all.</summary>
    public IReadOnlyList<string> Kinds { get; init; } = [];

    /// <summary>The audience the subscriber sees as. The carrier delivers only what that audience may see.</summary>
    public required Audience Audience { get; init; }

    /// <summary>
    /// Who the subscriber speaks for, when that widens what it may see: a simulated user's persona, pool and
    /// organization, matched against each item's knownBy.
    /// </summary>
    public IReadOnlyList<string> Viewers { get; init; } = [];

    /// <summary>Deliver entries after this sequence number; 0 replays the whole history.</summary>
    public long AfterSequence { get; init; }

    /// <summary>Whether the entry is in the parts and kinds asked for. Visibility is checked separately, by <see cref="AudienceView"/>.</summary>
    public bool Wants(Entry entry) =>
        entry.ChronicleId == ChronicleId
        && (Parts.Count == 0 || Parts.Contains(entry.Part))
        && (Kinds.Count == 0 || Kinds.Contains(entry.Kind))
        && (entry.Sequence ?? long.MaxValue) > AfterSequence;
}

/// <summary>
/// Topic names for carriers that route by topic: <c>chronicle.{chronicleId}.{part}.{kind}</c>, such as
/// <c>chronicle.harbor-ttx.world.device</c>. Parts are written as JSON writes them.
/// </summary>
public static class Topics
{
    /// <summary>The topic an entry is published on.</summary>
    public static string For(Entry entry) => $"chronicle.{entry.ChronicleId}.{PartName(entry.Part)}.{entry.Kind}";

    /// <summary>The topic for one part of a Chronicle, every kind, with the carrier's wildcard (such as NATS's '*').</summary>
    public static string For(string chronicleId, Part part, string wildcard = "*") =>
        $"chronicle.{chronicleId}.{PartName(part)}.{wildcard}";

    /// <summary>A part's name, as JSON writes it: world, intent, asBuilt, asRun, lessons.</summary>
    public static string PartName(Part part) => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(part.ToString());
}
