// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

/// <summary>A scenario type, by the id and version the Scenario Builder gives it, such as crucible-tabletop 0.3.0.</summary>
public sealed record ScenarioTypeRef
{
    /// <summary>The scenario type's id.</summary>
    public required string Id { get; init; }

    /// <summary>The version of the scenario type the scenario was built from.</summary>
    public string? Version { get; init; }
}

/// <summary>
/// One Chronicle: the whole record of one event. Its parts are what is true now; its history is every entry that
/// made them so, in order. The parts can always be rebuilt from the history, so the history is the record and the parts
/// are its current view.
/// </summary>
public sealed record ChronicleRecord
{
    /// <summary>The schema version this library writes and reads.</summary>
    public const string CurrentSchemaVersion = "0.3.0";

    /// <summary>The schema version the record follows, as semantic versioning: a change that alters an existing field's meaning is a major version.</summary>
    public required string SchemaVersion { get; init; }

    /// <summary>The Chronicle's id. Letters, digits, '.', '_' and '-'; it never changes.</summary>
    public required string Id { get; init; }

    /// <summary>The event's name.</summary>
    public required string Name { get; init; }

    /// <summary>What kind of event it is.</summary>
    public required EventKind Kind { get; init; }

    /// <summary>The scenario type the event was designed from, as the Scenario Builder names it. Absent if it had none.</summary>
    public ScenarioTypeRef? ScenarioType { get; init; }

    /// <summary>The event in a sentence or two.</summary>
    public string? Description { get; init; }

    /// <summary>When the Chronicle was started.</summary>
    public required DateTimeOffset Created { get; init; }

    /// <summary>Earlier Chronicles whose lessons seeded this one.</summary>
    public IReadOnlyList<string> SeededFrom { get; init; } = [];

    /// <summary>What the world looks like.</summary>
    public IReadOnlyList<Item> World { get; init; } = [];

    /// <summary>What the event must achieve, and how it is planned to unfold.</summary>
    public IReadOnlyList<Item> Intent { get; init; } = [];

    /// <summary>What was built.</summary>
    public IReadOnlyList<Item> AsBuilt { get; init; } = [];

    /// <summary>What happened.</summary>
    public IReadOnlyList<Item> AsRun { get; init; } = [];

    /// <summary>What was learned.</summary>
    public IReadOnlyList<Item> Lessons { get; init; } = [];

    /// <summary>Every entry, in sequence order. Empty in an export that carries only the current view.</summary>
    public IReadOnlyList<Entry> History { get; init; } = [];

    /// <summary>The items in one part.</summary>
    public IReadOnlyList<Item> this[Part part] => part switch
    {
        Part.World => World,
        Part.Intent => Intent,
        Part.AsBuilt => AsBuilt,
        Part.AsRun => AsRun,
        Part.Lessons => Lessons,
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, null)
    };

    /// <summary>Every item, with its part.</summary>
    public IEnumerable<(Part Part, Item Item)> Items =>
        Enum.GetValues<Part>().SelectMany(p => this[p].Select(i => (p, i)));

    /// <summary>A new, empty Chronicle.</summary>
    public static ChronicleRecord Start(string id, string name, EventKind kind, DateTimeOffset created) => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        Id = id,
        Name = name,
        Kind = kind,
        Created = created
    };
}

/// <summary>
/// One change to a Chronicle, and what apps publish. An entry adds, revises or retracts one item in one part, and says
/// who did it, when and why. Entries are never changed or removed once stored.
/// </summary>
public sealed record Entry
{
    /// <summary>The entry's id, chosen by the publisher; a UUID, version 7 recommended so ids sort by time.</summary>
    public required Guid Id { get; init; }

    /// <summary>The Chronicle it changes.</summary>
    public required string ChronicleId { get; init; }

    /// <summary>Its place in the Chronicle's history, from 1, assigned when it is stored. Absent on an entry not yet stored.</summary>
    public long? Sequence { get; init; }

    /// <summary>When the author made the change.</summary>
    public required DateTimeOffset RecordedAt { get; init; }

    /// <summary>The part it changes.</summary>
    public required Part Part { get; init; }

    /// <summary>The item's kind, so a subscriber can route the entry without reading the item, a retraction included.</summary>
    public required string Kind { get; init; }

    /// <summary>What it does.</summary>
    public required Operation Op { get; init; }

    /// <summary>The item's id.</summary>
    public required string ItemId { get; init; }

    /// <summary>The item, in full: the new item for an add, its new version for a revise. Absent for a retract.</summary>
    public Item? Item { get; init; }

    /// <summary>
    /// For a revise or a retract, the entry that wrote the version it replaces. A store refuses the entry if a later one
    /// has replaced that version, so two authors can't silently overwrite each other.
    /// </summary>
    public Guid? Revises { get; init; }

    /// <summary>Who made the change.</summary>
    public required Author Author { get; init; }

    /// <summary>Why, in words.</summary>
    public string? Reason { get; init; }

    /// <summary>An entry that adds <paramref name="item"/> to <paramref name="part"/>.</summary>
    public static Entry Add(string chronicleId, Part part, Item item, Author author, DateTimeOffset at, string? reason = null) => new()
    {
        Id = Guid.CreateVersion7(at),
        ChronicleId = chronicleId,
        RecordedAt = at,
        Part = part,
        Kind = item.Kind,
        Op = Operation.Add,
        ItemId = item.Id,
        Item = item,
        Author = author,
        Reason = reason
    };

    /// <summary>An entry that replaces the version <paramref name="revises"/> wrote with <paramref name="item"/>.</summary>
    public static Entry Revise(string chronicleId, Part part, Item item, Guid revises, Author author, DateTimeOffset at, string? reason = null) =>
        Add(chronicleId, part, item, author, at, reason) with { Op = Operation.Revise, Revises = revises };

    /// <summary>An entry that withdraws the item whose version <paramref name="revises"/> wrote.</summary>
    public static Entry Retract(string chronicleId, Part part, string kind, string itemId, Guid revises, Author author, DateTimeOffset at, string? reason = null) => new()
    {
        Id = Guid.CreateVersion7(at),
        ChronicleId = chronicleId,
        RecordedAt = at,
        Part = part,
        Kind = kind,
        Op = Operation.Retract,
        ItemId = itemId,
        Revises = revises,
        Author = author,
        Reason = reason
    };
}
