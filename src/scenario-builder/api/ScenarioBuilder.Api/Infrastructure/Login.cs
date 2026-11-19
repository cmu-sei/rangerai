// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using ScenarioBuilder.Api.Endpoints;

namespace ScenarioBuilder.Api.Infrastructure;

/// <summary>
/// Keycloak (or any OpenID Connect) login, as <c>Authorization</c> in appsettings.json. Unset <c>Authority</c> (or set
/// <c>Enabled</c> to false) and there's no login: the user is the auth proxy's <c>X-Forwarded-User</c>, or <c>dev</c>.
/// </summary>
public class LoginOptions
{
    public const string Section = "Authorization";

    /// <summary>The realm, e.g. https://localhost:8443/realms/crucible. Set, every /api call needs a signed-in user.</summary>
    public string? Authority { get; set; }

    /// <summary>A public client with PKCE whose redirect URIs cover the UI's origin; the API signs in through the UI's /api proxy.</summary>
    public string ClientId { get; set; } = "scenario-builder.ui";

    /// <summary>The audience a bearer token must carry: Swagger UI's, or another app's calling the API.</summary>
    public string Audience { get; set; } = "scenario-builder";

    /// <summary>The client Swagger UI signs in as; its redirect URIs cover the API's own origin.</summary>
    public string SwaggerClientId { get; set; } = "scenario-builder.api";

    /// <summary>Keycloak's authorization endpoint, for Swagger UI; under Authority unless set.</summary>
    public string? AuthorizationUrl { get; set; }

    /// <summary>Keycloak's token endpoint, for Swagger UI; under Authority unless set.</summary>
    public string? TokenUrl { get; set; }

    public string AuthorizationUrlOrDefault => AuthorizationUrl ?? $"{Authority!.TrimEnd('/')}/protocol/openid-connect/auth";

    public string TokenUrlOrDefault => TokenUrl ?? $"{Authority!.TrimEnd('/')}/protocol/openid-connect/token";

    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>The ID token claim that names the user: what scenarios record as Owner and Admins lists.</summary>
    public string UserClaim { get; set; } = "preferred_username";

    public int SessionHours { get; set; } = 8;

    /// <summary>
    /// Whether to require the login. Unset, it's on exactly when <see cref="Authority"/> is set; false turns it off with an
    /// Authority configured (Authorization__Enabled=false), and true without one is a startup error.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>The login is on: <see cref="Enabled"/>, or an <see cref="Authority"/> when that's unset.</summary>
    public bool Active => Enabled ?? !string.IsNullOrWhiteSpace(Authority);

    public string? UserOf(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true
            ? principal.FindFirst(UserClaim)?.Value ?? principal.FindFirst("sub")?.Value
            : null;
}

/// <summary>
/// The browser signs in once with Keycloak and then carries a session cookie, so plain links (document previews, downloads)
/// and the agent's own fetches are signed in too, with no token in the page. Swagger UI, which calls the API straight rather
/// than through the UI's proxy, sends a Keycloak bearer token for the <c>scenario-builder</c> audience instead. Calls with
/// neither get 401; the UI sends the person to <c>/api/auth/login</c>.
/// </summary>
public static class Login
{
    public const string CallbackPath = "/api/auth/callback";
    public const string SignedOutPath = "/api/auth/signed-out";
    private const string Either = "CookieOrBearer";

    /// <summary>Registers the login when it's <see cref="LoginOptions.Active"/>, and says whether it did.</summary>
    public static bool AddLogin(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(LoginOptions.Section);
        builder.Services.Configure<LoginOptions>(section);
        var options = section.Get<LoginOptions>() ?? new LoginOptions();
        if (!options.Active)
            return false;
        if (string.IsNullOrWhiteSpace(options.Authority))
            throw new InvalidOperationException(
                "Authorization:Enabled is true but Authorization:Authority isn't set. Set it to the Keycloak realm, " +
                "e.g. https://localhost:8443/realms/crucible, or set Authorization:Enabled to false to run without a login.");

        builder.Services.AddAuthentication(o =>
            {
                o.DefaultScheme = Either;
                o.DefaultChallengeScheme = Either;
            })
            // A bearer token means Swagger or an app calling; anything else is a browser with (or without) its session.
            .AddPolicyScheme(Either, Either, o => o.ForwardDefaultSelector = context =>
                context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? JwtBearerDefaults.AuthenticationScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = options.Authority;
                o.Audience = options.Audience;
                o.RequireHttpsMetadata = options.RequireHttpsMetadata;
                o.MapInboundClaims = false;
                o.TokenValidationParameters.NameClaimType = options.UserClaim;
            })
            .AddCookie(o =>
            {
                o.Cookie.Name = "scenario-builder";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.ExpireTimeSpan = TimeSpan.FromHours(options.SessionHours);
                o.SlidingExpiration = true;
                // An API answers, it doesn't redirect: the UI sends the person to sign in.
                o.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                o.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(o =>
            {
                o.Authority = options.Authority;
                o.ClientId = options.ClientId;
                o.RequireHttpsMetadata = options.RequireHttpsMetadata;
                o.ResponseType = "code";
                o.UsePkce = true;
                // Query, not form_post: Keycloak's cross-site POST back wouldn't carry the Lax correlation cookie over http.
                o.ResponseMode = "query";
                o.CallbackPath = CallbackPath;
                o.SignedOutCallbackPath = SignedOutPath;
                o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.Scope.Clear();
                o.Scope.Add("openid");
                o.Scope.Add("profile");
                o.Scope.Add("email");
                // Keeps only the ID token, for Keycloak's end-session, so signing out doesn't stop at a confirmation page.
                // Not the access and refresh tokens too: browsers send every localhost app's cookies to every port, and
                // a few apps' sessions holding all three tokens pass the dev server's 16 KB header limit (HTTP 431).
                o.SaveTokens = false;
                o.Events.OnTokenValidated = context =>
                {
                    if ((context.TokenEndpointResponse?.IdToken ?? context.ProtocolMessage.IdToken) is { Length: > 0 } idToken)
                        context.Properties!.StoreTokens([new AuthenticationToken { Name = "id_token", Value = idToken }]);
                    return Task.CompletedTask;
                };
                o.MapInboundClaims = false;
                o.TokenValidationParameters.NameClaimType = options.UserClaim;
                o.CorrelationCookie.SameSite = SameSiteMode.Lax;
                o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.NonceCookie.SameSite = SameSiteMode.Lax;
                o.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
        builder.Services.AddAuthorization();
        return true;
    }

    public static WebApplication UseLogin(this WebApplication app)
    {
        app.UseAuthentication();
        app.Use(RefuseCrossSiteWrites);
        app.UseAuthorization();
        return app;
    }

    /// <summary>
    /// The session is a cookie, so another site's page could send it along with a write. Browsers say where a request came
    /// from in Sec-Fetch-Site; the UI's own calls are same-origin. A bearer token isn't sent on its own, so it's left alone.
    /// </summary>
    private static async Task RefuseCrossSiteWrites(HttpContext context, RequestDelegate next)
    {
        var method = context.Request.Method;
        if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) && !HttpMethods.IsOptions(method)
            && context.User.Identity?.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme
            && context.Request.Headers["Sec-Fetch-Site"].ToString() is { Length: > 0 } site
            && site is not ("same-origin" or "none"))
        {
            await Results.Json(new ApiError("CROSS_SITE", "Changes come from Scenario Builder's own pages."), statusCode: StatusCodes.Status403Forbidden)
                .ExecuteAsync(context);
            return;
        }
        await next(context);
    }

    /// <summary>A path on this site to go back to, or the scenario list: never somewhere else.</summary>
    public static string LocalPath(string? returnUrl) =>
        returnUrl is { Length: > 0 } url && url[0] == '/' && !url.StartsWith("//") && !url.StartsWith("/\\") && !url.Any(char.IsControl)
            ? url
            : "/";
}
