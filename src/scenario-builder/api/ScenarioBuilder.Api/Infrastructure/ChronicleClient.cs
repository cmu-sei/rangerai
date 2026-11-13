// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace ScenarioBuilder.Api.Infrastructure;

/// <summary>Where the Chronicle API is (<c>Chronicle:Url</c>), and its UI, for links (<c>Chronicle:UiUrl</c>).</summary>
public sealed class ChronicleOptions
{
    public const string Section = "Chronicle";
    public string Url { get; set; } = "http://localhost:5285";
    public string UiUrl { get; set; } = "http://localhost:4320";
    /// <summary>The model the registered agents name, for display; the Chronicle's agent runner picks the one it calls.</summary>
    public string AgentModel { get; set; } = "gemma4:26b";
}

/// <summary>A Chronicle call that didn't succeed: its status, and the API's own answer.</summary>
public sealed class ChronicleException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

/// <summary>The Chronicle API, as Scenario Builder publishes to it: records whole, entries one at a time.</summary>
public sealed class ChronicleClient(IHttpClientFactory clients, IOptions<ChronicleOptions> options)
{
    public const string Client = "chronicle";

    public string UiLink(string chronicleId) => $"{options.Value.UiUrl.TrimEnd('/')}/events/{Uri.EscapeDataString(chronicleId)}";

    /// <summary>The whole record as the designer sees it, history included; null when there's no such Chronicle.</summary>
    public async Task<JsonObject?> Get(string chronicleId, CancellationToken ct)
    {
        var (status, body) = await Send(HttpMethod.Get, $"api/chronicles/{Uri.EscapeDataString(chronicleId)}?audience=designer", null, ct);
        return status == HttpStatusCode.NotFound ? null : Ok(status, body) as JsonObject;
    }

    public async Task Create(JsonObject record, CancellationToken ct)
    {
        var (status, body) = await Send(HttpMethod.Post, "api/chronicles", record, ct);
        Ok(status, body);
    }

    /// <summary>Appends one entry; returns it as stored, with its sequence.</summary>
    public async Task<JsonObject?> Append(string chronicleId, JsonObject entry, CancellationToken ct)
    {
        var (status, body) = await Send(HttpMethod.Post, $"api/chronicles/{Uri.EscapeDataString(chronicleId)}/entries", entry, ct);
        return Ok(status, body) as JsonObject;
    }

    /// <summary>Registers (or updates) one downstream agent; false when this Chronicle API has no subscribers yet (an older one answers 404).</summary>
    public async Task<bool> PutSubscriber(string chronicleId, string subscriberId, JsonObject body, CancellationToken ct)
    {
        var (status, text) = await Send(HttpMethod.Put,
            $"api/chronicles/{Uri.EscapeDataString(chronicleId)}/subscribers/{Uri.EscapeDataString(subscriberId)}", body, ct);
        if (status is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
            return false;
        Ok(status, text);
        return true;
    }

    public string AgentModel => options.Value.AgentModel;

    private async Task<(HttpStatusCode, string)> Send(HttpMethod method, string path, JsonNode? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, $"{options.Value.Url.TrimEnd('/')}/{path}");
        if (body is not null)
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        try
        {
            using var response = await clients.CreateClient(Client).SendAsync(request, ct);
            return (response.StatusCode, await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception e) when (e is HttpRequestException || (e is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new ChronicleException(StatusCodes.Status502BadGateway,
                $"The Chronicle API at {options.Value.Url} didn't answer ({e.Message}). Check Chronicle:Url in the API's settings.");
        }
    }

    private static JsonNode? Ok(HttpStatusCode status, string body)
    {
        if ((int)status is >= 200 and < 300)
            return body.Length == 0 ? null : JsonNode.Parse(body);
        throw new ChronicleException((int)status >= 500 ? StatusCodes.Status502BadGateway : (int)status,
            $"The Chronicle API refused it (HTTP {(int)status}): {(body.Length > 600 ? body[..600] : body)}");
    }
}
