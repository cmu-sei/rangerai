// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ScenarioBuilder.Api.Infrastructure;

/// <summary>
/// How Scenario Builder signs in to Chronicle as itself, as <c>Chronicle:Token</c> in appsettings.json: a Keycloak client
/// with a service account, whose tokens carry the <c>chronicle</c> audience. Unset <c>ClientId</c> and calls go without a
/// token, as they must to a Chronicle with no login.
/// </summary>
public class ChronicleTokenOptions
{
    public const string Section = "Chronicle:Token";

    /// <summary>The realm's token endpoint; <c>Authorization:Authority</c>'s unless set.</summary>
    public string? TokenUrl { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId);
}

/// <summary>Gets Scenario Builder's client credentials token, and keeps it until a minute before it runs out.</summary>
public sealed class ChronicleToken(IHttpClientFactory http, IOptions<ChronicleTokenOptions> options, IOptions<LoginOptions> login)
{
    public const string Client = "chronicle-token";

    private readonly SemaphoreSlim gate = new(1, 1);
    private string? token;
    private DateTime renew;

    public async Task<string> Get(CancellationToken ct)
    {
        if (token is not null && DateTime.UtcNow < renew)
            return token;
        await gate.WaitAsync(ct);
        try
        {
            if (token is not null && DateTime.UtcNow < renew)
                return token;
            var settings = options.Value;
            var url = settings.TokenUrl
                ?? (login.Value.Active ? login.Value.TokenUrlOrDefault : null)
                ?? throw new InvalidOperationException("Set Chronicle:Token:TokenUrl, or Authorization:Authority, for Scenario Builder to sign in to Chronicle.");
            var response = await http.CreateClient(Client).PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId!,
                ["client_secret"] = settings.ClientSecret ?? ""
            }), ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Keycloak refused Scenario Builder's sign-in to Chronicle as {settings.ClientId}: HTTP {(int)response.StatusCode} {await response.Content.ReadAsStringAsync(ct)}");
            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
                ?? throw new HttpRequestException("Keycloak answered Scenario Builder's sign-in to Chronicle with no token.");
            token = body.AccessToken;
            renew = DateTime.UtcNow.AddSeconds(Math.Max(body.ExpiresIn - 60, 0));
            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

/// <summary>Puts the token on every call the Chronicle client makes.</summary>
public sealed class ChronicleTokenHandler(ChronicleToken token) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await token.Get(ct));
        return await base.SendAsync(request, ct);
    }
}
