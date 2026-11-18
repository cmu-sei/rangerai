// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle.Api.Models;

/// <summary>One of the Ranger apps, for the top bar's switcher (<c>Apps</c> in appsettings.json); unlinked without a URL.</summary>
public sealed record RangerApp
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Url { get; init; }
}
