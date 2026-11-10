// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

// The As Built: what the builders made from the world and intent. Cyber objects a builder adds that the world didn't
// plan (a jump box, a log server) are ordinary cyber objects in this part.

/// <summary>One build of the range, or of part of it, by one builder.</summary>
[Parts(Part.AsBuilt)]
public sealed record Deployment : Item
{
    /// <summary>The builder, such as grayspace, ghosts or pandora.</summary>
    public required string Builder { get; init; }

    /// <summary>What the builder calls the build, such as a GraySpace enclave's slug.</summary>
    public string? Reference { get; init; }

    /// <summary>The version of the build, in the builder's terms.</summary>
    public string? Version { get; init; }

    /// <summary>Where the build stands.</summary>
    public required DeploymentState State { get; init; }

    /// <summary>When the build reached its state.</summary>
    public DateTimeOffset? At { get; init; }

    /// <summary>The World and Intent items the build was made from.</summary>
    public IReadOnlyList<string> Builds { get; init; } = [];

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Concat(Builds);
}

/// <summary>Where a build stands.</summary>
public enum DeploymentState { Planned, Building, Ready, Failed, Destroyed }

/// <summary>
/// What one planned item became on the range: the identifiers a builder gave it. A world device "mail server" becomes
/// a VM with an id, a hostname and an address.
/// </summary>
[Parts(Part.AsBuilt)]
public sealed record Realization : Item
{
    /// <summary>The planned item.</summary>
    public required string Of { get; init; }

    /// <summary>The deployment that made it.</summary>
    public required string DeploymentId { get; init; }

    /// <summary>The builder's identifiers for it, such as vmId, hostname or address.</summary>
    public IReadOnlyDictionary<string, string> Identifiers { get; init; } = new Dictionary<string, string>();

    /// <summary>How the built item differs from the plan, if it does.</summary>
    public string? Deviation { get; init; }

    /// <inheritdoc />
    public override IEnumerable<string> References() => base.References().Append(Of).Append(DeploymentId);
}

/// <summary>
/// A piece of content made for the event: a news article, a post, an email, a file. Content made before play is in the
/// As Built; content made during play is in the As Run.
/// </summary>
[Parts(Part.AsBuilt, Part.AsRun)]
public sealed record Content : Item
{
    /// <summary>What kind of content, such as article, post, email or file.</summary>
    public required string Format { get; init; }

    /// <summary>The title or subject.</summary>
    public string? Title { get; init; }

    /// <summary>The content itself, when it is text.</summary>
    public string? Body { get; init; }

    /// <summary>Where the content is, when it isn't held here.</summary>
    public string? Uri { get; init; }

    /// <summary>The persona or organization it is attributed to.</summary>
    public string? AttributedTo { get; init; }

    /// <summary>The service it is published on.</summary>
    public string? ServiceId { get; init; }

    /// <summary>The planned event it was made for.</summary>
    public string? PlannedEventId { get; init; }

    /// <inheritdoc />
    public override IEnumerable<string> References() =>
        base.References().Concat(new[] { AttributedTo, ServiceId, PlannedEventId }.OfType<string>());
}
