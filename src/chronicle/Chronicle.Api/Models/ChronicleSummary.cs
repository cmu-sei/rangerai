// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle.Api.Models;

/// <summary>What a list of Chronicles shows for each.</summary>
public sealed record ChronicleSummary(string Id, string Name, EventKind Kind, ScenarioTypeRef? ScenarioType, DateTimeOffset Created, long Entries)
{
    public static ChronicleSummary From(ChronicleRecord record) =>
        new(record.Id, record.Name, record.Kind, record.ScenarioType, record.Created, record.History.Count);
}
