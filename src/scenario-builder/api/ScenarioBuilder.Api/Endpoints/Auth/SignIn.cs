// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Endpoints.Auth;

/// <summary>GET /auth/login?returnUrl= — a browser navigation, not a call: off to Keycloak, then back to returnUrl signed in. Without a login configured, straight back.</summary>
public class SignInEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapGet("/auth/login", (string? returnUrl, IOptions<LoginOptions> login) =>
            {
                var target = Login.LocalPath(returnUrl);
                return login.Value.Active
                    ? Results.Challenge(new AuthenticationProperties { RedirectUri = target }, [OpenIdConnectDefaults.AuthenticationScheme])
                    : Results.LocalRedirect(target);
            })
            .AllowAnonymous()
            .ExcludeFromDescription();
}
