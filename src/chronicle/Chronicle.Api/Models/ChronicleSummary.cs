// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle.Api.Models;

/// <summary>What a list of Chronicles shows for each. Archived ones are listed too, marked, for the list to hide.</summary>
public sealed record ChronicleSummary(string Id, string Name, EventKind Kind, ScenarioTypeRef? ScenarioType, DateTimeOffset Created, long Entries, bool Archived)
{
    public static ChronicleSummary From(ChronicleRecord record, bool archived) =>
        new(record.Id, record.Name, record.Kind, record.ScenarioType, record.Created, record.History.Count, archived);
}

/// <summary>The body of <c>PUT /api/chronicles/{id}/archive</c>: true archives the Chronicle, false brings it back.</summary>
public sealed record PutArchive(bool Archived);
