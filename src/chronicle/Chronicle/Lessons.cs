// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

// The Lessons: what was learned. They seed the next event.

/// <summary>How well the event met one objective, and the evidence.</summary>
[Parts(Part.Lessons)]
public sealed record Assessment : Item
{
    /// <summary>The objective.</summary>
    public required string ObjectiveId { get; init; }

    /// <summary>How well it was met.</summary>
    public required Rating Rating { get; init; }

    /// <summary>Why.</summary>
    public string? Text { get; init; }

    /// <summary>The As Run items that show it.</summary>
    public IReadOnlyList<string> Evidence { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Append(ObjectiveId).Concat(Evidence);
}

/// <summary>How well an objective was met.</summary>
public enum Rating { Met, PartiallyMet, NotMet, NotAssessed }

/// <summary>Something learned, and what to do about it next time.</summary>
[Parts(Part.Lessons)]
public sealed record Lesson : Item
{
    /// <summary>What was learned.</summary>
    public required string Text { get; init; }

    /// <summary>What to do next time.</summary>
    public string? Recommendation { get; init; }

    /// <summary>Who should act on it: designers, exercise control, builders, participants' organization.</summary>
    public string? For { get; init; }

    /// <summary>The items that show it.</summary>
    public IReadOnlyList<string> Evidence { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(Evidence);
}
