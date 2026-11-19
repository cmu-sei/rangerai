// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace ScenarioBuilder.Api.Endpoints.Auth;

/// <summary>POST /auth/logout — a form post from the top bar: ends the session here and at Keycloak, then back to the scenario list.</summary>
public class SignOutEndpoint : IEndpoint
{
    public RouteHandlerBuilder Register(RouteGroupBuilder group, string tag) =>
        group.MapPost("/auth/logout", (IOptions<Infrastructure.LoginOptions> login) =>
                login.Value.Active
                    ? Results.SignOut(new AuthenticationProperties { RedirectUri = "/" },
                        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme])
                    : Results.LocalRedirect("/"))
            .ExcludeFromDescription();
}
