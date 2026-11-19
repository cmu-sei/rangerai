// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace ScenarioBuilder.Api.Endpoints;

/// <summary>One of the Ranger apps, for the top bar's switcher; unlinked without a URL.</summary>
public record RangerApp(string Id, string Name, string? Url);

/// <summary>GET /apps — the Ranger apps, for the top bar's switcher, as <c>Apps</c> in appsettings.json lists them.</summary>
public class AppsEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapGet("/apps", (IConfiguration config) => config.GetSection("Apps").Get<List<RangerApp>>() ?? [])
            .AllowAnonymous()
            .WithTags("Apps");
}
