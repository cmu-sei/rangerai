// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Chronicle.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Chronicle.Api.Tests;

/// <summary>With Authorization:Authority set: a person's session or an app's token, or nothing.</summary>
public class LoginTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Issuer = "https://keycloak.invalid/realms/test";
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes("a-test-signing-key-that-is-long-enough-for-hs256"));

    /// <summary>
    /// The API with a login configured. Bearer tokens are checked for real, against a test key in place of Keycloak's;
    /// the browser session is faked by X-Test-User in place of the cookie.
    /// </summary>
    private WebApplicationFactory<Program> WithLogin() =>
        factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Authorization:Authority", Issuer);
            b.ConfigureServices(services =>
            {
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestSession>(TestSession.Scheme, null);
                services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
                    o => o.ForwardAuthenticate = TestSession.Scheme);
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    o.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                    o.TokenValidationParameters.ValidIssuer = Issuer;
                    o.TokenValidationParameters.IssuerSigningKey = Key;
                });
            });
        });

    private static string Token(string audience) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Audience = audience,
        Subject = new ClaimsIdentity([new Claim("preferred_username", "service-account-scenario-builder.service")]),
        Expires = DateTime.UtcNow.AddMinutes(5),
        SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256)
    });

    private static HttpClient Client(WebApplicationFactory<Program> app) =>
        app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Without_a_session_or_token_calls_are_refused_but_the_switcher_version_and_health_still_answer()
    {
        using var app = WithLogin();
        var client = Client(app);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/chronicles")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/apps")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/version")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task An_app_calls_with_a_token_for_the_chronicle_audience()
    {
        using var app = WithLogin();
        var client = Client(app);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("chronicle"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/chronicles")).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("scenario-builder"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/chronicles")).StatusCode);
    }

    [Fact]
    public async Task A_signed_in_person_is_named_and_another_sites_page_cant_write_with_their_session()
    {
        using var app = WithLogin();
        var client = Client(app);
        client.DefaultRequestHeaders.Add("X-Test-User", "lee");

        var me = JsonNode.Parse(await client.GetStringAsync("/api/me"))!;
        Assert.Equal("lee", (string)me["user"]!);
        Assert.True((bool)me["signedIn"]!);

        var cross = new HttpRequestMessage(HttpMethod.Put, "/api/chronicles/harbor-ttx/archive")
        {
            Content = new StringContent("{\"archived\":false}", Encoding.UTF8, "application/json")
        };
        cross.Headers.Add("Sec-Fetch-Site", "cross-site");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(cross)).StatusCode);

        var own = new HttpRequestMessage(HttpMethod.Put, "/api/chronicles/harbor-ttx/archive")
        {
            Content = new StringContent("{\"archived\":false}", Encoding.UTF8, "application/json")
        };
        own.Headers.Add("Sec-Fetch-Site", "same-origin");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(own)).StatusCode);
    }

    [Fact]
    public async Task Without_a_login_the_api_is_open_and_sign_in_goes_straight_back()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/chronicles")).StatusCode);
        Assert.False((bool)JsonNode.Parse(await client.GetStringAsync("/api/me"))!["signedIn"]!);
        var response = await client.GetAsync("/api/auth/login?returnUrl=%2Fchronicles%2Fharbor-ttx");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/chronicles/harbor-ttx", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Swagger_signs_in_with_Keycloak_as_the_apis_own_client()
    {
        using var app = WithLogin();
        var client = Client(app);
        var document = JsonNode.Parse(await client.GetStringAsync("/openapi/v1.json"))!;
        var flow = document["components"]!["securitySchemes"]!["keycloak"]!["flows"]!["authorizationCode"]!;
        Assert.Equal($"{Issuer}/protocol/openid-connect/auth", (string)flow["authorizationUrl"]!);
        Assert.Equal($"{Issuer}/protocol/openid-connect/token", (string)flow["tokenUrl"]!);
        Assert.NotNull(document["security"]![0]!["keycloak"]);
        Assert.Contains("chronicle.api", await client.GetStringAsync("/swagger/index.js"));
    }

    [Fact]
    public async Task Enabled_false_turns_the_login_off_with_an_authority_set()
    {
        using var app = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Authorization:Authority", Issuer);
            b.UseSetting("Authorization:Enabled", "false");
        });
        var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/chronicles")).StatusCode);
        Assert.False((bool)JsonNode.Parse(await client.GetStringAsync("/api/me"))!["signedIn"]!);
    }

    [Fact]
    public void Enabled_true_without_an_authority_stops_at_startup()
    {
        using var app = factory.WithWebHostBuilder(b => b.UseSetting("Authorization:Enabled", "true"));
        var error = Assert.Throws<InvalidOperationException>(() => app.CreateClient());
        Assert.Contains("Authorization:Authority", error.Message);
    }

    [Theory]
    [InlineData("/chronicles/x?audience=public", "/chronicles/x?audience=public")]
    [InlineData(null, "/")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("//evil.example/", "/")]
    [InlineData("/\\evil.example/", "/")]
    public void Sign_in_only_returns_to_this_site(string? returnUrl, string expected) =>
        Assert.Equal(expected, Login.LocalPath(returnUrl));

    /// <summary>A browser session, as the cookie handler would make it.</summary>
    private sealed class TestSession(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Scheme = "TestSession";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var user = Request.Headers["X-Test-User"].ToString();
            if (user.Length == 0)
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim("preferred_username", user)], CookieAuthenticationDefaults.AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
        }
    }
}
