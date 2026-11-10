// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle.Api.Models;

/// <summary>
/// Who is reading: an audience, and the personas, pools, sides or organizations it speaks for. The audience is taken on
/// trust; this API has no authentication yet.
/// </summary>
public class ViewQuery
{
    /// <summary>participant, excon or designer; absent means participant.</summary>
    public Audience? Audience { get; init; }

    /// <summary>Ids the reader speaks for, matched against each item's knownBy. Repeat the parameter for several.</summary>
    public List<string> Viewers { get; init; } = [];

    /// <summary>The audience, defaulting to participant so an unmarked read fails closed.</summary>
    public Audience AudienceOrDefault => Audience ?? Chronicle.Audience.Participant;
}

/// <summary>A view, narrowed to parts, kinds and a position in the history, as a subscription is.</summary>
public sealed class EntryQuery : ViewQuery
{
    /// <summary>Parts wanted, such as world; repeat for several; absent means all.</summary>
    public List<Part> Parts { get; init; } = [];

    /// <summary>Kinds wanted, such as device; repeat for several; absent means all.</summary>
    public List<string> Kinds { get; init; } = [];

    /// <summary>Only entries after this sequence number.</summary>
    public long? After { get; init; }

    /// <summary>The query as the spec's subscription; a reconnecting client's last event id moves it on.</summary>
    public Subscription ToSubscription(string chronicleId, long? lastEventId = null) => new()
    {
        ChronicleId = chronicleId,
        Audience = AudienceOrDefault,
        Viewers = Viewers,
        Parts = Parts,
        Kinds = Kinds,
        AfterSequence = Math.Max(After ?? 0, lastEventId ?? 0)
    };
}
