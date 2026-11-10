// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

/// <summary>
/// What an audience may see of a Chronicle: the same rule for people, apps and AI models. An item is seen by its
/// visibility's audience and every audience above it, and by the viewers it names in knownBy.
/// </summary>
public static class AudienceView
{
    /// <summary>Whether <paramref name="audience"/>, speaking for <paramref name="viewers"/>, may see <paramref name="item"/>.</summary>
    public static bool Sees(Item item, Audience audience, IReadOnlyCollection<string>? viewers = null) =>
        item.Visibility <= audience || (viewers is { Count: > 0 } && item.KnownBy.Any(viewers.Contains));

    /// <summary>
    /// The record as <paramref name="audience"/> may see it: the items it may see, and the entries about them. An entry is
    /// seen when the version it writes, or for a retract the version it withdraws, is seen. A view's history is for
    /// reading, not replaying: a reveal (a revise that lowers an item's visibility) arrives without the add before it.
    /// References to items the audience can't see are left as they are.
    /// </summary>
    public static ChronicleRecord For(ChronicleRecord record, Audience audience, IReadOnlyCollection<string>? viewers = null)
    {
        if (audience == Audience.Designer)
            return record;

        bool Keep(Item item) => Sees(item, audience, viewers);
        return record with
        {
            World = [.. record.World.Where(Keep)],
            Intent = [.. record.Intent.Where(Keep)],
            AsBuilt = [.. record.AsBuilt.Where(Keep)],
            AsRun = [.. record.AsRun.Where(Keep)],
            Lessons = [.. record.Lessons.Where(Keep)],
            History = [.. Projection.Subjects(record).Where(s => Keep(s.Subject)).Select(s => s.Entry)]
        };
    }
}
