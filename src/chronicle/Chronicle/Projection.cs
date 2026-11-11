// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle;

/// <summary>An entry, or a history, that breaks the Chronicle's rules. <see cref="Code"/> says which rule.</summary>
public sealed class ChronicleException(string code, string message, Entry? entry = null) : Exception(message)
{
    /// <summary>The rule broken, such as STALE or ID_TAKEN.</summary>
    public string Code { get; } = code;

    /// <summary>The entry that broke it.</summary>
    public Entry? Entry { get; } = entry;
}

/// <summary>
/// How entries make the parts: the rules every store and every reader applies the same way. An add puts a new item at
/// the end of its part; a revise replaces it where it is; a retract removes it. An id names one item at a time: once
/// its item is retracted, a new item may be added under it, and the history keeps both. While an item is current it
/// never moves part or changes kind.
/// </summary>
public static class Projection
{
    /// <summary>
    /// Stores <paramref name="entry"/>: checks it against the full record, gives it the next sequence number, and
    /// returns the record with its parts updated and the entry at the end of its history.
    /// </summary>
    /// <exception cref="ChronicleException">The entry breaks a rule; the record is unchanged.</exception>
    public static ChronicleRecord Append(ChronicleRecord record, Entry entry)
    {
        var state = Replay(record);
        var stored = entry with { Sequence = state.LastSequence + 1 };
        state.Apply(stored);
        return state.Into(record) with { History = [.. record.History, stored] };
    }

    /// <summary>The record with its parts rebuilt from its history.</summary>
    /// <exception cref="ChronicleException">The history breaks a rule.</exception>
    public static ChronicleRecord Rebuild(ChronicleRecord record) => Replay(record).Into(record);

    /// <summary>Each entry in the history, with the item version it is about: the new version, or for a retract the one withdrawn.</summary>
    /// <exception cref="ChronicleException">The history breaks a rule.</exception>
    public static IEnumerable<(Entry Entry, Item Subject)> Subjects(ChronicleRecord record)
    {
        var state = new State(record.Id);
        foreach (var entry in record.History)
        {
            var before = state.Apply(entry);
            yield return (entry, entry.Item ?? before!);
        }
    }

    private static State Replay(ChronicleRecord record)
    {
        var state = new State(record.Id);
        foreach (var entry in record.History)
            state.Apply(entry);
        return state;
    }

    private sealed class State(string chronicleId)
    {
        private sealed record Track(Part Part, Guid LastEntry, Item? Current);

        private readonly Dictionary<Part, List<Item>> parts = Enum.GetValues<Part>().ToDictionary(p => p, _ => new List<Item>());
        private readonly Dictionary<string, Track> tracks = [];

        public long LastSequence { get; private set; }

        /// <summary>Applies one stored entry; returns the version it replaced or withdrew.</summary>
        public Item? Apply(Entry e)
        {
            if (e.ChronicleId != chronicleId)
                Fail("WRONG_CHRONICLE", $"The entry is for Chronicle '{e.ChronicleId}', not '{chronicleId}'.", e);
            if (e.Sequence != LastSequence + 1)
                Fail("SEQUENCE", $"The entry's sequence is {e.Sequence?.ToString() ?? "absent"}; the next is {LastSequence + 1}.", e);

            tracks.TryGetValue(e.ItemId, out var track);
            if (e.Op == Operation.Add)
            {
                CheckItem(e);
                // A retracted id is free again: an app that numbers its items by position can keep its own ids.
                if (track?.Current is not null)
                    Fail("ID_TAKEN", $"'{e.ItemId}' is already used by a current {track.Current.Kind}; revise it, or retract it first.", e);
                if (e.Revises is not null)
                    Fail("REVISES_ON_ADD", "An add revises nothing.", e);
            }
            else
            {
                if (track?.Current is null)
                    Fail("UNKNOWN_ITEM", $"There is no current item '{e.ItemId}' to {e.Op.ToString().ToLowerInvariant()}.", e);
                if (track!.Part != e.Part)
                    Fail("PART_CHANGED", $"'{e.ItemId}' is in {track.Part}, not {e.Part}.", e);
                if (track.Current!.Kind != e.Kind)
                    Fail("KIND_CHANGED", $"'{e.ItemId}' is a {track.Current.Kind}, not a {e.Kind}.", e);
                if (e.Revises != track.LastEntry)
                    Fail("STALE", $"'{e.ItemId}' was last written by entry {track.LastEntry}, not {e.Revises?.ToString() ?? "(none named)"}.", e);
                if (e.Op == Operation.Revise)
                    CheckItem(e);
                else if (e.Item is not null)
                    Fail("ITEM_ON_RETRACT", "A retract carries no item.", e);
            }

            var list = parts[e.Part];
            var before = track?.Current;
            switch (e.Op)
            {
                case Operation.Add: list.Add(e.Item!); break;
                case Operation.Revise: list[list.IndexOf(before!)] = e.Item!; break;
                case Operation.Retract: list.Remove(before!); break;
            }
            tracks[e.ItemId] = new Track(e.Part, e.Id, e.Item);
            LastSequence = e.Sequence!.Value;
            return before;
        }

        public ChronicleRecord Into(ChronicleRecord record) => record with
        {
            World = [.. parts[Part.World]],
            Intent = [.. parts[Part.Intent]],
            AsBuilt = [.. parts[Part.AsBuilt]],
            AsRun = [.. parts[Part.AsRun]],
            Lessons = [.. parts[Part.Lessons]]
        };

        private static void CheckItem(Entry e)
        {
            if (e.Item is null)
                Fail("ITEM_MISSING", $"An {e.Op.ToString().ToLowerInvariant()} carries the item in full.", e);
            if (e.Item!.Id != e.ItemId)
                Fail("ITEM_ID_MISMATCH", $"The entry names '{e.ItemId}'; its item is '{e.Item.Id}'.", e);
            if (e.Item.Kind != e.Kind)
                Fail("KIND_MISMATCH", $"The entry names kind '{e.Kind}'; its item is a {e.Item.Kind}.", e);
            if (!e.Item.Parts.Contains(e.Part))
                Fail("KIND_NOT_IN_PART", $"A {e.Kind} belongs in {string.Join(" or ", e.Item.Parts)}, not {e.Part}.", e);
        }

        private static void Fail(string code, string message, Entry e) =>
            throw new ChronicleException(code, $"Entry {e.Id}: {message}", e);
    }
}
