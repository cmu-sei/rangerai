// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

// The As Run: what happened. Every As Run item says when it happened and, if it carried out the plan, which planned
// event; an after-action analyst compares the two.

/// <summary>What every As Run item has: when it happened, and the planned event it carried out, if any.</summary>
public abstract record Happening : Item
{
    /// <summary>When it happened, by the wall clock.</summary>
    public required DateTimeOffset At { get; init; }

    /// <summary>The planned event it carried out; absent when it wasn't planned.</summary>
    public string? PlannedEventId { get; init; }

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(new[] { PlannedEventId }.OfType<string>());
}

/// <summary>A point in the event's life: start, pause, resume, end.</summary>
[Parts(Part.AsRun)]
public sealed record Milestone : Happening
{
    /// <summary>The milestone.</summary>
    public required MilestoneType Type { get; init; }

    /// <summary>Why, such as the reason for a pause.</summary>
    public string? Note { get; init; }
}

/// <summary>A point in the event's life.</summary>
public enum MilestoneType { Start, Pause, Resume, End }

/// <summary>A cyber event that happened on the range or in a simulation, in the Cyber DEM's terms.</summary>
[Parts(Part.AsRun)]
public sealed record CyberActivity : Happening
{
    /// <summary>The event.</summary>
    public required CyberEvent Event { get; init; }

    /// <summary>The app or sensor that saw it.</summary>
    public string? ObservedBy { get; init; }

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(Event.References());
}

/// <summary>Influence activity that happened: a post, comment, reaction, share or message.</summary>
[Parts(Part.AsRun)]
public sealed record InfluenceActivity : Happening
{
    /// <summary>The activity.</summary>
    public required Influence Activity { get; init; }

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(Activity.References());
}

/// <summary>An inject that reached people.</summary>
[Parts(Part.AsRun)]
public sealed record InjectDelivery : Happening
{
    /// <summary>Who it reached.</summary>
    public IReadOnlyList<string> To { get; init; } = [];

    /// <summary>How it went, such as delivered late or acknowledged.</summary>
    public string? Note { get; init; }
}

/// <summary>A decision or action by participants.</summary>
[Parts(Part.AsRun)]
public sealed record ParticipantAction : Happening
{
    /// <summary>Who acted: a team, a role or a person, in the event's terms.</summary>
    public required string By { get; init; }

    /// <summary>What they decided or did.</summary>
    public required string Text { get; init; }

    /// <summary>The objectives it bears on.</summary>
    public IReadOnlyList<string> ObjectiveIds { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(ObjectiveIds);
}

/// <summary>Something a controller, evaluator or app noticed.</summary>
[Parts(Part.AsRun)]
public sealed record Observation : Happening
{
    /// <summary>Who noticed it.</summary>
    public required string By { get; init; }

    /// <summary>What they noticed.</summary>
    public required string Text { get; init; }

    /// <summary>The objectives it bears on.</summary>
    public IReadOnlyList<string> ObjectiveIds { get; init; } = [];

    /// <summary>The items it is about.</summary>
    public IReadOnlyList<string> About { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(ObjectiveIds).Concat(About);
}
