// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Chronicle.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Chronicle.Api.Controllers.Api;

/// <summary>Who is signed in: the user's name, and whether through the Keycloak login, so the UI offers Sign out.</summary>
public sealed record MeView(string? User, bool SignedIn);

/// <summary>Signing in and out with Keycloak, and who is signed in.</summary>
[Route("api")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AuthController(IOptions<LoginOptions> login) : ApiControllerBase
{
    /// <summary>GET /api/me — who is signed in. Without a login configured, nobody, and that's fine.</summary>
    [HttpGet("me")]
    public MeView Me() => new(login.Value.UserOf(User), login.Value.Active);

    /// <summary>
    /// GET /api/auth/login?returnUrl= — a browser navigation, not a call: off to Keycloak, then back to returnUrl signed
    /// in. Without a login configured, straight back.
    /// </summary>
    [HttpGet("auth/login")]
    [AllowAnonymous]
    public IActionResult SignIn(string? returnUrl)
    {
        var target = Login.LocalPath(returnUrl);
        return login.Value.Active
            ? Challenge(new AuthenticationProperties { RedirectUri = target }, OpenIdConnectDefaults.AuthenticationScheme)
            : LocalRedirect(target);
    }

    /// <summary>POST /api/auth/logout — a form post from the top bar: ends the session here and at Keycloak, then back to the list.</summary>
    [HttpPost("auth/logout")]
    public IActionResult Logout() =>
        login.Value.Active
            ? SignOut(new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme)
            : LocalRedirect("/");
}
