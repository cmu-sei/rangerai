// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

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
    /// <summary>Whether a new scenario gets its Chronicle when it's created, so the Chronicle lists it from the start.</summary>
    public bool PublishOnCreate { get; set; } = true;
}

/// <summary>A Chronicle call that didn't succeed: its status, and the API's own answer.</summary>
public sealed class ChronicleException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

/// <summary>
/// The Chronicle API, as Scenario Builder publishes to it: records whole, entries one at a time. A thin layer over the
/// Chronicle.Client library, which does the calling and retrying; this keeps Scenario Builder's JSON-shaped use of it
/// and its errors in Scenario Builder's terms.
/// </summary>
public sealed class ChronicleClient(global::Chronicle.Client.ChronicleClient api, IOptions<ChronicleOptions> options)
{
    /// <summary>The HttpClient's name, as the Chronicle.Client library registers it.</summary>
    public const string Client = global::Chronicle.Client.ChronicleClient.HttpClientName;

    /// <summary>The app Scenario Builder publishes as, by default.</summary>
    public const string App = "scenario-builder";

    public string UiLink(string chronicleId) => $"{options.Value.UiUrl.TrimEnd('/')}/events/{Uri.EscapeDataString(chronicleId)}";

    /// <summary>The whole record as the designer sees it, history included; null when there's no such Chronicle.</summary>
    public Task<JsonObject?> Get(string chronicleId, CancellationToken ct) =>
        Call(() => api.GetJsonAsync(chronicleId, global::Chronicle.Client.ChronicleView.Designer, ct));

    public Task Create(JsonObject record, CancellationToken ct) => Call(() => api.CreateJsonAsync(record, ct));

    /// <summary>Appends one entry; returns it as stored, with its sequence.</summary>
    public Task<JsonObject?> Append(string chronicleId, JsonObject entry, CancellationToken ct) =>
        Call(() => api.PublishJsonAsync(chronicleId, entry, ct));

    /// <summary>Registers (or updates) one downstream agent; false when this Chronicle API has no subscribers yet (an older one answers 404).</summary>
    public async Task<bool> PutSubscriber(string chronicleId, string subscriberId, JsonObject body, CancellationToken ct)
    {
        try
        {
            await Call(() => api.RegisterSubscriberJsonAsync(chronicleId, subscriberId, body, ct));
            return true;
        }
        catch (ChronicleException e) when (e.Status is StatusCodes.Status404NotFound or StatusCodes.Status405MethodNotAllowed)
        {
            return false;
        }
    }

    public string AgentModel => options.Value.AgentModel;

    public bool PublishOnCreate => options.Value.PublishOnCreate;

    /// <summary>Makes the call, turning the library's errors into Scenario Builder's: 502 when the Chronicle API is down or failed.</summary>
    private async Task<T> Call<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (global::Chronicle.Client.ChronicleApiException e)
        {
            throw e.IsUnavailable
                ? new ChronicleException(StatusCodes.Status502BadGateway,
                    e.Status is null ? $"{e.Message} Check Chronicle:Url in the API's settings." : e.Message)
                : new ChronicleException((int)e.Status!.Value, e.Message);
        }
    }
}
