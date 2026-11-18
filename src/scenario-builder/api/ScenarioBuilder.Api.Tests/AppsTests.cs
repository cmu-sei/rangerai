// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;

namespace ScenarioBuilder.Api.Tests;

/// <summary>The Ranger apps, for the top bar's switcher.</summary>
public class AppsTests
{
    [Fact]
    public async Task The_Ranger_apps_are_listed_as_appsettings_lists_them()
    {
        using var factory = new ApiFactory();
        var apps = JsonNode.Parse(await factory.Client().GetStringAsync("/api/apps"))!.AsArray();
        Assert.Equal(["scenario-builder", "grayspace", "chronicle", "ghosts"], apps.Select(a => (string)a!["id"]!));
        Assert.Equal("http://localhost:4320", (string)apps[2]!["url"]!);
        Assert.Null(apps[3]!["url"]);
    }
}
