// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Microsoft.Extensions.Options;
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Endpoints;

/// <summary>SignedIn: through the Keycloak login, so the UI offers Sign out.</summary>
public record MeView(string User, bool Admin, bool SignedIn);

/// <summary>GET /me — who is calling, whether they're an admin and signed in, so the UI knows whether to offer Admin and Sign out.</summary>
public class MeEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapGet("/me", (HttpContext http, IConfiguration config, IOptions<LoginOptions> login) =>
            {
                var caller = Caller.From(http);
                return new MeView(caller.User, Caller.IsAdmin(caller, config), login.Value.Active);
            })
            .WithTags("Me");
}
