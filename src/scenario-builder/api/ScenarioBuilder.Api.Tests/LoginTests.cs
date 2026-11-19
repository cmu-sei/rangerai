// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
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
using ScenarioBuilder.Api.Infrastructure;

namespace ScenarioBuilder.Api.Tests;

/// <summary>With Authorization:Authority set: a session or nothing, and headers can't name the user.</summary>
public class LoginTests
{
    private const string Issuer = "https://keycloak.invalid/realms/test";
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes("a-test-signing-key-that-is-long-enough-for-hs256"));

    /// <summary>
    /// The API with a login configured. Bearer tokens are checked for real, against a test key in place of Keycloak's;
    /// the browser session is faked by X-Test-User in place of the cookie.
    /// </summary>
    private static WebApplicationFactory<Program> WithLogin(ApiFactory factory) =>
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

    private static string Token(string audience, string user) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Audience = audience,
        Subject = new ClaimsIdentity([new Claim("preferred_username", user)]),
        Expires = DateTime.UtcNow.AddMinutes(5),
        SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256)
    });

    private static HttpClient Client(WebApplicationFactory<Program> app, string? user)
    {
        var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (user is not null)
            client.DefaultRequestHeaders.Add("X-Test-User", user);
        return client;
    }

    [Fact]
    public async Task Without_a_session_calls_are_refused_but_the_footer_and_switcher_still_load()
    {
        using var factory = new ApiFactory();
        using var app = WithLogin(factory);
        var client = Client(app, null);
        client.DefaultRequestHeaders.Add("X-Forwarded-User", "dev");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/scenarios")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/version")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/apps")).StatusCode);
    }

    [Fact]
    public async Task The_user_is_the_sessions_whatever_X_Forwarded_User_says()
    {
        using var factory = new ApiFactory();
        using var app = WithLogin(factory);
        var client = Client(app, "lee");
        client.DefaultRequestHeaders.Add("X-Forwarded-User", "dev");

        var me = JsonNode.Parse(await client.GetStringAsync("/api/me"))!;
        Assert.Equal("lee", (string)me["user"]!);
        Assert.False((bool)me["admin"]!);
        Assert.True((bool)me["signedIn"]!);
    }

    [Fact]
    public async Task Another_sites_page_cant_make_changes_with_the_session()
    {
        using var factory = new ApiFactory();
        using var app = WithLogin(factory);
        var client = Client(app, "lee");
        var body = new { name = "Harbor", scenarioTypeId = "ttx" };

        var cross = new HttpRequestMessage(HttpMethod.Post, "/api/scenarios") { Content = JsonContent.Create(body) };
        cross.Headers.Add("Sec-Fetch-Site", "cross-site");
        var refused = await client.SendAsync(cross);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("CROSS_SITE", (string)JsonNode.Parse(await refused.Content.ReadAsStringAsync())!["code"]!);

        var own = new HttpRequestMessage(HttpMethod.Post, "/api/scenarios") { Content = JsonContent.Create(body) };
        own.Headers.Add("Sec-Fetch-Site", "same-origin");
        Assert.NotEqual(HttpStatusCode.Forbidden, (await client.SendAsync(own)).StatusCode);
    }

    [Fact]
    public async Task Swagger_calls_with_a_bearer_token_for_the_scenario_builder_audience()
    {
        using var factory = new ApiFactory();
        using var app = WithLogin(factory);
        var client = Client(app, null);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("scenario-builder", "lee"));
        Assert.Equal("lee", (string)JsonNode.Parse(await client.GetStringAsync("/api/me"))!["user"]!);

        // A cross-site header doesn't matter to a bearer token: a browser never sends one on its own.
        var write = new HttpRequestMessage(HttpMethod.Post, "/api/scenarios") { Content = JsonContent.Create(new { name = "x", scenarioTypeId = "nope" }) };
        write.Headers.Add("Sec-Fetch-Site", "cross-site");
        Assert.NotEqual(HttpStatusCode.Forbidden, (await client.SendAsync(write)).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("chronicle", "lee"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Swagger_signs_in_with_Keycloak_as_the_apis_own_client()
    {
        using var factory = new ApiFactory();
        using var app = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("Authorization:Authority", Issuer);
        });
        var client = Client(app, null);
        var document = JsonNode.Parse(await client.GetStringAsync("/openapi/v1.json"))!;
        var flow = document["components"]!["securitySchemes"]!["keycloak"]!["flows"]!["authorizationCode"]!;
        Assert.Equal($"{Issuer}/protocol/openid-connect/auth", (string)flow["authorizationUrl"]!);
        Assert.Equal($"{Issuer}/protocol/openid-connect/token", (string)flow["tokenUrl"]!);
        Assert.Contains("scenario-builder.api", await client.GetStringAsync("/swagger/index.js"));
    }

    [Fact]
    public async Task Calls_to_Chronicle_carry_Scenario_Builders_own_token_got_once()
    {
        var keycloak = new Recorder(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"sb-token\",\"expires_in\":300}", Encoding.UTF8, "application/json")
        });
        var chronicle = new Recorder(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var token = new ChronicleToken(new Factory(keycloak),
            Options.Create(new ChronicleTokenOptions { ClientId = "scenario-builder.service", ClientSecret = "s3cret" }),
            Options.Create(new LoginOptions { Authority = Issuer }));
        using var http = new HttpClient(new ChronicleTokenHandler(token) { InnerHandler = chronicle });

        await http.GetAsync("http://chronicle.invalid/api/chronicles");
        await http.GetAsync("http://chronicle.invalid/api/chronicles");

        Assert.All(chronicle.Requests, r => Assert.Equal("Bearer sb-token", r.Headers.Authorization!.ToString()));
        var asked = Assert.Single(keycloak.Requests);
        Assert.Equal($"{Issuer}/protocol/openid-connect/token", asked.RequestUri!.ToString());
        Assert.Contains("grant_type=client_credentials", keycloak.Bodies[0]);
        Assert.Contains("client_id=scenario-builder.service", keycloak.Bodies[0]);
    }

    private sealed class Recorder(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct));
            return answer(request);
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    [Fact]
    public async Task Without_a_login_sign_in_goes_straight_back_and_me_isnt_signed_in()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/auth/login?returnUrl=%2Fscenarios%2F3");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/scenarios/3", response.Headers.Location!.OriginalString);
        Assert.False((bool)JsonNode.Parse(await client.GetStringAsync("/api/me"))!["signedIn"]!);
    }

    [Fact]
    public async Task Enabled_false_turns_the_login_off_with_an_authority_set()
    {
        using var factory = new ApiFactory();
        using var app = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Authorization:Authority", Issuer);
            b.UseSetting("Authorization:Enabled", "false");
        });
        var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/scenarios")).StatusCode);
        Assert.False((bool)JsonNode.Parse(await client.GetStringAsync("/api/me"))!["signedIn"]!);
    }

    [Fact]
    public void Enabled_true_without_an_authority_stops_at_startup()
    {
        using var factory = new ApiFactory();
        using var app = factory.WithWebHostBuilder(b => b.UseSetting("Authorization:Enabled", "true"));
        var error = Assert.Throws<InvalidOperationException>(() => app.CreateClient());
        Assert.Contains("Authorization:Authority", error.Message);
    }

    [Theory]
    [InlineData("/scenarios/3?stage=2", "/scenarios/3?stage=2")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("//evil.example/", "/")]
    [InlineData("/\\evil.example/", "/")]
    [InlineData("scenarios", "/")]
    public void Sign_in_only_returns_to_this_site(string? returnUrl, string expected) =>
        Assert.Equal(expected, Login.LocalPath(returnUrl));

    private sealed class TestSession(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Scheme = "TestSession";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var user = Request.Headers["X-Test-User"].ToString();
            if (user.Length == 0)
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim("preferred_username", user), new Claim("sub", Guid.NewGuid().ToString())],
                CookieAuthenticationDefaults.AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
        }
    }
}
