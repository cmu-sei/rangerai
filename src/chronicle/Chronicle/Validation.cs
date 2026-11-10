// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Chronicle;

/// <summary>How much a finding matters.</summary>
public enum Severity
{
    /// <summary>The record breaks the spec.</summary>
    Error,

    /// <summary>The record is valid but probably not what its authors meant.</summary>
    Warning
}

/// <summary>One thing wrong with a record.</summary>
/// <param name="Severity">How much it matters.</param>
/// <param name="Code">Which rule, such as DUPLICATE_ID.</param>
/// <param name="Message">What is wrong, in words.</param>
/// <param name="ItemId">The item it is about, if any.</param>
public sealed record Finding(Severity Severity, string Code, string Message, string? ItemId = null);

/// <summary>
/// The rules the JSON Schema can't state: ids unique across parts, kinds in their parts, references that resolve, and
/// parts that match the history. A record that passes the schema and has no error findings is a valid Chronicle.
/// </summary>
public static partial class Validation
{
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._:-]*$")]
    private static partial Regex ItemIdPattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex ChronicleIdPattern();

    /// <summary>
    /// Checks <paramref name="record"/>. Pass <paramref name="isView"/> for an audience's view, whose references may
    /// name items it can't see and whose history can't be replayed.
    /// </summary>
    public static IReadOnlyList<Finding> Check(ChronicleRecord record, bool isView = false)
    {
        var findings = new List<Finding>();
        void Error(string code, string message, string? id = null) => findings.Add(new(Severity.Error, code, message, id));
        void Warn(string code, string message, string? id = null) => findings.Add(new(Severity.Warning, code, message, id));

        if (Major(record.SchemaVersion) != Major(ChronicleRecord.CurrentSchemaVersion))
            Error("SCHEMA_VERSION", $"Schema {record.SchemaVersion} isn't readable as {ChronicleRecord.CurrentSchemaVersion}.");
        if (!ChronicleIdPattern().IsMatch(record.Id))
            Error("ID_FORMAT", $"'{record.Id}' isn't a valid Chronicle id.");

        var ids = new HashSet<string>();
        foreach (var (part, item) in record.Items)
        {
            if (!ItemIdPattern().IsMatch(item.Id))
                Error("ID_FORMAT", $"'{item.Id}' isn't a valid item id.", item.Id);
            if (!ids.Add(item.Id))
                Error("DUPLICATE_ID", $"'{item.Id}' is used more than once.", item.Id);
            if (!item.Parts.Contains(part))
                Error("KIND_NOT_IN_PART", $"A {item.Kind} belongs in {string.Join(" or ", item.Parts)}, not {part}.", item.Id);
            CheckKind(item, Warn);
        }

        if (!isView)
        {
            foreach (var (_, item) in record.Items)
                foreach (var missing in item.References().Where(r => !ids.Contains(r)).Distinct())
                    Error("UNRESOLVED_REFERENCE", $"'{item.Id}' names '{missing}', which isn't in the Chronicle.", item.Id);

            if (record.History.Count > 0)
                CheckHistory(record, Error);
        }

        return findings;
    }

    private static void CheckKind(Item item, Action<string, string, string?> warn)
    {
        switch (item)
        {
            case PlannedEvent { Inject: null, Cyber: null, Influence: null } p:
                warn("PLANNED_EVENT_EMPTY", $"'{p.Id}' has no inject, cyber event or influence activity.", p.Id);
                break;
            case PlannedEvent { Offset: null, After.Count: 0, Condition: null } p:
                warn("PLANNED_EVENT_UNTIMED", $"'{p.Id}' has no offset, nothing it follows, and no condition.", p.Id);
                break;
            case PersonaPool { Count: < 1 } p:
                warn("POOL_EMPTY", $"'{p.Id}' has no people.", p.Id);
                break;
        }

        var cyber = item switch
        {
            PlannedEvent p => p.Cyber,
            CyberActivity c => c.Event,
            _ => null
        };
        if (cyber is not null && !CyberDem.EventClass.All.Contains(cyber.Class))
            warn("UNKNOWN_EVENT_CLASS", $"'{cyber.Class}' isn't a Cyber DEM event class.", item.Id);
    }

    private static void CheckHistory(ChronicleRecord record, Action<string, string, string?> error)
    {
        ChronicleRecord rebuilt;
        try
        {
            rebuilt = Projection.Rebuild(record);
        }
        catch (ChronicleException e)
        {
            error($"HISTORY_{e.Code}", e.Message, e.Entry?.ItemId);
            return;
        }

        foreach (var part in Enum.GetValues<Part>())
        {
            var want = JsonSerializer.Serialize(rebuilt[part], ChronicleJson.Options);
            var have = JsonSerializer.Serialize(record[part], ChronicleJson.Options);
            if (want != have)
                error("PARTS_DIFFER_FROM_HISTORY", $"The {part} part isn't what its history makes it.", null);
        }
    }

    private static string Major(string version) => version.Split('.')[0];
}
