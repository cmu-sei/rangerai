// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Reflection;

namespace ScenarioBuilder.Api.Endpoints;

public record VersionView(string Version);

/// <summary>GET /version — the API's release number (<c>Version</c> in the csproj), shown in the UI's footer.</summary>
public class VersionEndpoint : IEndpoint
{
    // The informational version carries "+<commit>" from source link; the footer wants the number alone.
    private static readonly string Number =
        (typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
            .Split('+')[0];

    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapGet("/version", () => new VersionView(Number))
            .WithTags("Version");
}
