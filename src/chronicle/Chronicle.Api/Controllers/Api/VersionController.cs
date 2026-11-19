// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chronicle.Api.Controllers.Api;

/// <summary>The API's release number.</summary>
public sealed record VersionView(string Version);

/// <summary>
/// GET /api/version — the API's release number (<c>Version</c> in the csproj), shown in the UI's footer, as Scenario
/// Builder's is. Open, so the footer shows it before sign-in.
/// </summary>
[Route("api/version")]
[AllowAnonymous]
public sealed class VersionController : ApiControllerBase
{
    // The informational version carries "+<commit>" from source link; the footer wants the number alone.
    private static readonly string Number =
        (typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
            .Split('+')[0];

    [HttpGet]
    [ProducesResponseType<VersionView>(StatusCodes.Status200OK)]
    public VersionView Get() => new(Number);
}
