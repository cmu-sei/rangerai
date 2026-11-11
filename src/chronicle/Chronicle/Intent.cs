// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;

namespace Chronicle;

// The Intent: what the event must achieve, and how it is planned to unfold.

/// <summary>Something the event must achieve.</summary>
[Parts(Part.Intent)]
public sealed record Objective : Item
{
    /// <summary>The objective.</summary>
    public required string Text { get; init; }

    /// <summary>How achieving it is measured.</summary>
    public IReadOnlyList<string> Measures { get; init; } = [];

    /// <summary>The objective this one serves.</summary>
    public string? ParentId { get; init; }

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(new[] { ParentId }.OfType<string>());
}

/// <summary>Something participants should take away.</summary>
[Parts(Part.Intent)]
public sealed record TeachingPoint : Item
{
    /// <summary>The teaching point.</summary>
    public required string Text { get; init; }

    /// <summary>The objectives it serves.</summary>
    public IReadOnlyList<string> ObjectiveIds { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(ObjectiveIds);
}

/// <summary>A rule of play: scope, rules of engagement, safety, what is out of bounds.</summary>
[Parts(Part.Intent)]
public sealed record Rule : Item
{
    /// <summary>The rule.</summary>
    public required string Text { get; init; }

    /// <summary>What kind of rule, such as scope, engagement or safety.</summary>
    public string? Category { get; init; }
}

/// <summary>A word or name the event's writing must use, or must avoid.</summary>
[Parts(Part.Intent)]
public sealed record Term : Item
{
    /// <summary>The word or name.</summary>
    public required string Text { get; init; }

    /// <summary>Whether it must be used as written, or must not be used.</summary>
    public required TermUsage Usage { get; init; }

    /// <summary>What it means, or what to use instead.</summary>
    public string? Note { get; init; }
}

/// <summary>
/// The event in a few paragraphs, such as an executive summary. Its visibility says who it is written for, so an
/// event can hold one for exercise control and another for participants.
/// </summary>
[Parts(Part.Intent)]
public sealed record Summary : Item
{
    /// <summary>What it is called, such as Executive summary.</summary>
    public string? Title { get; init; }

    /// <summary>The summary.</summary>
    public required string Text { get; init; }
}

/// <summary>How a term is used.</summary>
public enum TermUsage { Fixed, Banned }

/// <summary>
/// One planned event: a row of the master scenario events list. It happens at an offset from the start, or after
/// other planned events, and carries an inject for people, a cyber event for simulations and the range, influence
/// activity, or several of these.
/// </summary>
[Parts(Part.Intent)]
public sealed record PlannedEvent : Item
{
    /// <summary>A short title.</summary>
    public required string Title { get; init; }

    /// <summary>When it is planned, as an offset from the start of the event, such as 01:40:00.</summary>
    public TimeSpan? Offset { get; init; }

    /// <summary>The planned events it waits on (the Cyber DEM's SourceIDs, when they are cyber events).</summary>
    public IReadOnlyList<string> After { get; init; } = [];

    /// <summary>Any other condition it waits on, in words.</summary>
    public string? Condition { get; init; }

    /// <summary>The objectives it serves.</summary>
    public IReadOnlyList<string> ObjectiveIds { get; init; } = [];

    /// <summary>The inject for people, if it has one.</summary>
    public Inject? Inject { get; init; }

    /// <summary>The cyber event, if it has one.</summary>
    public CyberEvent? Cyber { get; init; }

    /// <summary>The influence activity, if it has one.</summary>
    public Influence? Influence { get; init; }

    /// <summary>What the participants are expected to do.</summary>
    public string? ExpectedResponse { get; init; }

    /// <summary>What exercise control may do if play goes off course.</summary>
    public IReadOnlyList<string> Escalations { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References()
        .Concat(After).Concat(ObjectiveIds)
        .Concat(Cyber?.References() ?? []).Concat(Influence?.References() ?? []);
}

/// <summary>A message for people: an email, a call, a news report, a note handed over. The Cyber DEM doesn't cover these.</summary>
public sealed record Inject
{
    /// <summary>How it arrives, such as email, phone, chat, news, document or in person.</summary>
    public required string Channel { get; init; }

    /// <summary>Who it is from: a persona or organization in the world, or a description.</summary>
    public string? From { get; init; }

    /// <summary>Who it is for: sides, organizations or personas, or a description such as "the SOC team".</summary>
    public IReadOnlyList<string> To { get; init; } = [];

    /// <summary>What it says.</summary>
    public required string Content { get; init; }
}

/// <summary>
/// A cyber event, in the Cyber DEM's terms (SISO-STD-025.2-2023): its class, phase, actors, targets and sources. ATT&amp;CK
/// ids are supporting detail, after the class (BONES §5, pattern 6). Cyber events target cyber objects only (pattern 3).
/// </summary>
public sealed record CyberEvent
{
    /// <summary>The Cyber DEM event class, the most specific that fits, such as PhishingAttack or Degrade.</summary>
    public required string Class { get; init; }

    /// <summary>The event's phase: Start, Suspend, Continue, ContinueWithChanges, End or Cancel.</summary>
    [SisoEnumeration(679, "Cyber Cyber Event Phase Type")]
    public string? Phase { get; init; }

    /// <summary>Who carries it out: personas, devices or other cyber objects.</summary>
    public IReadOnlyList<string> ActorIds { get; init; } = [];

    /// <summary>The cyber objects it targets.</summary>
    public IReadOnlyList<string> TargetIds { get; init; } = [];

    /// <summary>Conditions that must all hold for a target to count.</summary>
    public IReadOnlyList<string> TargetModifiers { get; init; } = [];

    /// <summary>The cyber events it follows from (SourceIDs): planned events in the Intent, cyber activity in the As Run.</summary>
    public IReadOnlyList<string> SourceIds { get; init; } = [];

    /// <summary>How long it lasts.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>ATT&amp;CK techniques and subtechniques, for an attack.</summary>
    public IReadOnlyList<string> MitreTechniqueIds { get; init; } = [];

    /// <summary>ATT&amp;CK mitigations, for a defense.</summary>
    public IReadOnlyList<string> MitreMitigationIds { get; init; } = [];

    /// <summary>ATT&amp;CK detections, for recon or detection.</summary>
    public IReadOnlyList<string> MitreDetectionIds { get; init; } = [];

    /// <summary>The class's own attributes, or detail no class models yet (BONES §5, pattern 4).</summary>
    public JsonElement? Details { get; init; }

    internal IEnumerable<string> References() => ActorIds.Concat(TargetIds).Concat(SourceIds);
}

/// <summary>
/// Influence activity: a post, comment, reaction, share or direct message. The Cyber DEM has only PhishingAttack;
/// these are the events the GHOSTS team proposes.
/// </summary>
public sealed record Influence
{
    /// <summary>What the persona does.</summary>
    public required InfluenceAction Action { get; init; }

    /// <summary>The persona, or for planned activity a pool, that acts.</summary>
    public required string ActorId { get; init; }

    /// <summary>The service it happens on, such as a social media platform.</summary>
    public string? ServiceId { get; init; }

    /// <summary>The content it posts or sends, as an item, when the content is in the Chronicle.</summary>
    public string? ContentId { get; init; }

    /// <summary>The content in words, when it isn't an item.</summary>
    public string? Text { get; init; }

    /// <summary>What it replies or reacts to.</summary>
    public string? InReplyTo { get; init; }

    internal IEnumerable<string> References() =>
        new[] { ActorId, ServiceId, ContentId }.OfType<string>();
}

/// <summary>What a persona does in the information environment.</summary>
public enum InfluenceAction { Post, Comment, React, Share, Message }
