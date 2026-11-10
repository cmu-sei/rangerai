// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

/// <summary>The five parts of a Chronicle, in the order an event fills them.</summary>
public enum Part
{
    /// <summary>What the world looks like: sides, organizations, networks, devices, personas, facts.</summary>
    World,

    /// <summary>What the event must achieve, and how: objectives, rules of play, vocabulary, the planned events.</summary>
    Intent,

    /// <summary>What was built from the world and intent: deployments, what each planned item became, generated personas and content.</summary>
    AsBuilt,

    /// <summary>What happened during the event: milestones, cyber and influence activity, injects delivered, actions, observations.</summary>
    AsRun,

    /// <summary>What was learned: assessments against the objectives, and lessons for the next event.</summary>
    Lessons
}

/// <summary>What kind of event a Chronicle records.</summary>
public enum EventKind { Training, Exercise, Simulation }

/// <summary>
/// Who may see an item. The ladder is participant, then exercise control, then designer; an audience sees what is
/// marked at or below its own rung.
/// </summary>
public enum Audience
{
    /// <summary>The participants, and anyone playing beside them, such as simulated users.</summary>
    Participant,

    /// <summary>Exercise control (the White Cell): the ground truth and the plan.</summary>
    Excon,

    /// <summary>The designers: everything, including their notes.</summary>
    Designer
}

/// <summary>Whether a person stated an item, or it was proposed (by an agent or an app) and not yet confirmed.</summary>
public enum Standing { Stated, Proposed }

/// <summary>How an author acted: as a person, as a person's AI agent, or as an app on its own.</summary>
public enum Via { Person, Agent, App }

/// <summary>What an entry does to an item.</summary>
public enum Operation
{
    /// <summary>Adds an item the Chronicle doesn't have yet.</summary>
    Add,

    /// <summary>Replaces an item in full with a new version. The earlier versions stay in the history.</summary>
    Revise,

    /// <summary>Withdraws an item. It leaves the parts, and stays in the history.</summary>
    Retract
}

/// <summary>Who made a change.</summary>
public sealed record Author
{
    /// <summary>The app that published the entry, such as scenario-builder, grayspace or ghosts.</summary>
    public required string App { get; init; }

    /// <summary>The person, or the person whose agent acted, in the app's own terms; absent when the app acted on its own.</summary>
    public string? Actor { get; init; }

    /// <summary>How the author acted.</summary>
    public required Via Via { get; init; }
}

/// <summary>A place, in WGS 84 as C2SIM's geodetic coordinates are, or by name alone.</summary>
public sealed record Location
{
    /// <summary>The place's name, such as a city or a building.</summary>
    public string? Name { get; init; }

    /// <summary>Latitude in decimal degrees.</summary>
    public double? Latitude { get; init; }

    /// <summary>Longitude in decimal degrees.</summary>
    public double? Longitude { get; init; }
}
