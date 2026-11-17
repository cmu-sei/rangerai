// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net;
using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Chronicle.Client;

/// <summary>
/// The Chronicle API, for an app: subscribe to a Chronicle's entries, publish your own, report activity, register as a
/// subscriber. The app decides what to do with what it hears; this client only carries it.
/// <para>
/// Subscriptions reconnect on their own and resume after the last entry received. Calls that don't reach the API, or
/// that it fails with a 5xx, are tried again (<see cref="ChronicleClientOptions.Retries"/>); anything else the API
/// refuses throws a <see cref="ChronicleApiException"/>.
/// </para>
/// </summary>
public sealed class ChronicleClient
{
    /// <summary>The name of the HttpClient <see cref="ServiceCollectionExtensions.AddChronicleClient(IServiceCollection, Action{ChronicleClientOptions})"/> registers.</summary>
    public const string HttpClientName = "chronicle";

    // The spec's own types (records, entries) are read strictly, as the spec says. The API's other answers are read
    // leniently, so a newer API that adds a field doesn't break an older app.
    private static readonly JsonSerializerOptions Lenient = new(ChronicleJson.Compact) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip };

    private readonly HttpClient http;
    private readonly ChronicleClientOptions options;
    private readonly ILogger log;

    /// <summary>A client for dependency injection; see <see cref="ServiceCollectionExtensions"/>.</summary>
    [ActivatorUtilitiesConstructor]
    public ChronicleClient(HttpClient http, IOptions<ChronicleClientOptions> options, ILogger<ChronicleClient>? log = null)
        : this(http, options.Value, log)
    {
    }

    /// <summary>A client on <paramref name="http"/>, without dependency injection.</summary>
    public ChronicleClient(HttpClient http, ChronicleClientOptions options, ILogger? log = null)
    {
        this.http = http;
        this.options = options;
        this.log = log ?? NullLogger.Instance;
    }

    /// <summary>A client for the API at <paramref name="url"/>, publishing as <paramref name="app"/>, with its own HttpClient.</summary>
    public static ChronicleClient Create(string url, string app, ILogger? log = null) =>
        new(new HttpClient(), new ChronicleClientOptions { Url = url, App = app }, log);

    /// <summary>The options it was made with.</summary>
    public ChronicleClientOptions Options => options;

    // ---- Chronicles ----

    /// <summary>Every Chronicle, newest first, archived ones marked.</summary>
    public async Task<IReadOnlyList<ChronicleSummary>> ListAsync(CancellationToken ct = default) =>
        Read<List<ChronicleSummary>>(await Send(HttpMethod.Get, "api/chronicles", null, ct), Lenient) ?? [];

    /// <summary>One Chronicle as <paramref name="view"/> (participant when absent) may see it; null when there's none.</summary>
    /// <exception cref="JsonException">The record has something this library can't read; <see cref="GetJsonAsync"/> reads anything.</exception>
    public async Task<ChronicleRecord?> GetAsync(string chronicleId, ChronicleView? view = null, CancellationToken ct = default) =>
        Read<ChronicleRecord>(await SendOrNotFound(HttpMethod.Get, $"{Path(chronicleId)}{Query(view)}", null, ct), ChronicleJson.Options);

    /// <summary>One Chronicle as JSON, as <paramref name="view"/> (participant when absent) may see it; null when there's none.</summary>
    public async Task<JsonObject?> GetJsonAsync(string chronicleId, ChronicleView? view = null, CancellationToken ct = default) =>
        Json(await SendOrNotFound(HttpMethod.Get, $"{Path(chronicleId)}{Query(view)}", null, ct));

    /// <summary>Starts a Chronicle, or imports one with its history. Returns it as stored.</summary>
    /// <exception cref="ChronicleApiException">409 CHRONICLE_EXISTS, or 422 with the spec's findings.</exception>
    public async Task<ChronicleRecord> CreateAsync(ChronicleRecord record, CancellationToken ct = default) =>
        Read<ChronicleRecord>(await Send(HttpMethod.Post, "api/chronicles", ChronicleJson.Serialize(record, indented: false), ct), ChronicleJson.Options)!;

    /// <summary>Starts a Chronicle from JSON, or imports one with its history. Returns it as stored.</summary>
    public async Task<JsonObject?> CreateJsonAsync(JsonObject record, CancellationToken ct = default) =>
        Json(await Send(HttpMethod.Post, "api/chronicles", record.ToJsonString(), ct));

    /// <summary>Archives a Chronicle (hides it from the list) or brings it back.</summary>
    public async Task ArchiveAsync(string chronicleId, bool archived = true, CancellationToken ct = default) =>
        await Send(HttpMethod.Put, $"{Path(chronicleId)}/archive", JsonSerializer.Serialize(new { archived }), ct);

    /// <summary>What the spec's validation finds in a stored Chronicle.</summary>
    public async Task<IReadOnlyList<Finding>> GetFindingsAsync(string chronicleId, CancellationToken ct = default) =>
        Read<List<Finding>>(await Send(HttpMethod.Get, $"{Path(chronicleId)}/findings", null, ct), Lenient) ?? [];

    /// <summary>Checks a record against the spec; stores nothing.</summary>
    public async Task<IReadOnlyList<Finding>> ValidateAsync(ChronicleRecord record, CancellationToken ct = default) =>
        Read<List<Finding>>(await Send(HttpMethod.Post, "api/chronicles/validate", ChronicleJson.Serialize(record, indented: false), ct), Lenient) ?? [];

    /// <summary>A view as text a model can read: the subscription's audience, viewers and parts.</summary>
    public async Task<ContextView> GetContextAsync(Subscription subscription, CancellationToken ct = default) =>
        Read<ContextView>(await Send(HttpMethod.Get, $"{Path(subscription.ChronicleId)}/context{Query(subscription, includePosition: false)}", null, ct), Lenient)!;

    // ---- Entries: reading ----

    /// <summary>The history the subscription asks for, as it stands now. Entries this library can't read follow <see cref="ChronicleClientOptions.UnreadableEntries"/>.</summary>
    public async Task<IReadOnlyList<Entry>> GetEntriesAsync(Subscription subscription, CancellationToken ct = default) =>
        [.. (await GetRawEntriesAsync(subscription, ct)).Select(Typed).OfType<Entry>()];

    /// <summary>The history the subscription asks for, as it stands now, raw.</summary>
    public async Task<IReadOnlyList<RawEntry>> GetRawEntriesAsync(Subscription subscription, CancellationToken ct = default)
    {
        var body = await Send(HttpMethod.Get, $"{Path(subscription.ChronicleId)}/entries{Query(subscription)}", null, ct);
        using var doc = JsonDocument.Parse(body);
        return [.. doc.RootElement.EnumerateArray().Select(e => RawEntry.Parse(e.GetRawText()))];
    }

    /// <summary>One entry by its sequence, if <paramref name="view"/> may see it; null when not.</summary>
    public async Task<Entry?> GetEntryAsync(string chronicleId, long sequence, ChronicleView? view = null, CancellationToken ct = default) =>
        Read<Entry>(await SendOrNotFound(HttpMethod.Get, $"{Path(chronicleId)}/entries/{sequence}{Query(view)}", null, ct), ChronicleJson.Options);

    // ---- Entries: subscribing ----

    /// <summary>
    /// Subscribes: the history after <see cref="Subscription.AfterSequence"/>, then each new entry as it is stored,
    /// until <paramref name="ct"/> is cancelled. It reconnects whenever the connection drops and resumes after the last
    /// entry it delivered, so each entry arrives once per call. To resume after a restart, keep the last
    /// <see cref="Entry.Sequence"/> handled and pass it as <see cref="Subscription.AfterSequence"/>.
    /// </summary>
    /// <exception cref="ChronicleApiException">The API refused the subscription, such as 404 for no such Chronicle.</exception>
    public async IAsyncEnumerable<Entry> SubscribeAsync(Subscription subscription, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var raw in SubscribeRawAsync(subscription, ct))
            if (Typed(raw) is { } entry)
                yield return entry;
    }

    /// <summary>
    /// Subscribes, as <see cref="SubscribeAsync"/> does, delivering each entry raw: an app that must never miss an
    /// entry, even of a kind added to the spec after this library was built, reads these.
    /// </summary>
    public async IAsyncEnumerable<RawEntry> SubscribeRawAsync(Subscription subscription, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var after = subscription.AfterSequence;
        var failures = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            HttpResponseMessage? response = null;
            try
            {
                response = await OpenStream(subscription, after, ct);
            }
            catch (ChronicleApiException e) when (e.IsUnavailable)
            {
                log.LogWarning("Chronicle {Chronicle}: can't subscribe yet ({Message}); trying again.", subscription.ChronicleId, e.Message);
            }

            if (response is not null)
            {
                using (response)
                {
                    await using var body = await response.Content.ReadAsStreamAsync(ct);
                    using var idle = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    var events = SseParser.Create(body).EnumerateAsync(idle.Token).GetAsyncEnumerator(idle.Token);
                    try
                    {
                        while (true)
                        {
                            if (options.StreamIdleTimeout > TimeSpan.Zero)
                                idle.CancelAfter(options.StreamIdleTimeout);
                            bool more;
                            try
                            {
                                more = await events.MoveNextAsync();
                            }
                            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                            {
                                log.LogInformation("Chronicle {Chronicle}: nothing heard for {Idle}; reconnecting.", subscription.ChronicleId, options.StreamIdleTimeout);
                                break;
                            }
                            catch (Exception e) when (e is IOException or HttpRequestException)
                            {
                                log.LogWarning("Chronicle {Chronicle}: the subscription dropped ({Message}); reconnecting.", subscription.ChronicleId, e.Message);
                                break;
                            }
                            idle.CancelAfter(Timeout.InfiniteTimeSpan);
                            if (!more)
                                break;
                            failures = 0;
                            if (events.Current.EventType != "entry")
                                continue;
                            var raw = RawEntry.Parse(events.Current.Data);
                            if (raw.Sequence <= after)
                                continue;
                            after = raw.Sequence;
                            yield return raw;
                        }
                    }
                    finally
                    {
                        await events.DisposeAsync();
                    }
                }
            }
            await Backoff(failures++, ct);
        }
    }

    private async Task<HttpResponseMessage> OpenStream(Subscription subscription, long after, CancellationToken ct)
    {
        using var request = await Request(HttpMethod.Get, $"{Path(subscription.ChronicleId)}/entries/stream{Query(subscription with { AfterSequence = after })}", null, ct);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception e) when (Unreachable(e, ct))
        {
            throw Unavailable(e);
        }
        if (response.IsSuccessStatusCode)
            return response;
        using (response)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception e) when (e is IOException or HttpRequestException)
            {
                body = "";
            }
            throw Refused(response.StatusCode, body);
        }
    }

    // ---- Entries: publishing ----

    /// <summary>
    /// Publishes an entry. Safe to retry: an entry already stored (by its id) comes back with <see cref="Published.Created"/> false.
    /// </summary>
    /// <exception cref="ChronicleApiException">
    /// 409 STALE when a later entry already replaced the version it revises, or ID_TAKEN; 422 for any other rule; 404 for no such Chronicle.
    /// </exception>
    public async Task<Published> PublishAsync(Entry entry, CancellationToken ct = default)
    {
        var (status, body) = await SendWithStatus(HttpMethod.Post, $"{Path(entry.ChronicleId)}/entries", ChronicleJson.Serialize(entry, indented: false), ct);
        return new Published(Read<Entry>(body, ChronicleJson.Options)!, status == HttpStatusCode.Created);
    }

    /// <summary>Publishes an entry written as JSON; returns it as stored, with its sequence.</summary>
    public async Task<JsonObject?> PublishJsonAsync(string chronicleId, JsonObject entry, CancellationToken ct = default) =>
        Json(await Send(HttpMethod.Post, $"{Path(chronicleId)}/entries", entry.ToJsonString(), ct));

    /// <summary>Adds <paramref name="item"/> to <paramref name="part"/>, as <paramref name="author"/> (or this app).</summary>
    public Task<Published> AddAsync(string chronicleId, Part part, Item item, string? reason = null, Author? author = null, CancellationToken ct = default) =>
        PublishAsync(Entry.Add(chronicleId, part, item, author ?? options.DefaultAuthor(), DateTimeOffset.UtcNow, reason), ct);

    /// <summary>
    /// Replaces the version entry <paramref name="revises"/> wrote with <paramref name="item"/>. A 409 STALE means
    /// someone revised it since: read it again, and decide.
    /// </summary>
    public Task<Published> ReviseAsync(string chronicleId, Part part, Item item, Guid revises, string? reason = null, Author? author = null, CancellationToken ct = default) =>
        PublishAsync(Entry.Revise(chronicleId, part, item, revises, author ?? options.DefaultAuthor(), DateTimeOffset.UtcNow, reason), ct);

    /// <summary>Withdraws the item whose current version entry <paramref name="revises"/> wrote.</summary>
    public Task<Published> RetractAsync(string chronicleId, Part part, string kind, string itemId, Guid revises, string? reason = null, Author? author = null, CancellationToken ct = default) =>
        PublishAsync(Entry.Retract(chronicleId, part, kind, itemId, revises, author ?? options.DefaultAuthor(), DateTimeOffset.UtcNow, reason), ct);

    // ---- Activity ----

    /// <summary>
    /// Reports what this app did, for the live feed. With an entry it is notable: the entry is stored first, and the
    /// activity carries its sequence. Not retried, so a dropped answer can't report an activity twice.
    /// </summary>
    public async Task<Activity> ReportActivityAsync(string chronicleId, ActivityReport report, CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["source"] = report.Source ?? options.App ?? throw new InvalidOperationException(
                "Set ChronicleClientOptions.App (Chronicle:App in configuration), or the report's Source."),
            ["kind"] = report.Kind
        };
        void Put(string name, JsonNode? value) { if (value is not null) body[name] = value; }
        Put("subscriberId", report.SubscriberId);
        Put("actor", report.Actor);
        Put("text", report.Text);
        Put("host", report.Host);
        Put("at", report.At is { } at ? JsonSerializer.SerializeToNode(at) : null);
        Put("visibility", report.Visibility is { } v ? JsonSerializer.SerializeToNode(v, ChronicleJson.Compact) : null);
        Put("data", report.Data?.DeepClone());
        Put("entry", report.Entry is { } e ? JsonSerializer.SerializeToNode(e, ChronicleJson.Compact) : null);
        return Read<Activity>(await Send(HttpMethod.Post, $"{Path(chronicleId)}/activity", body.ToJsonString(), ct, retry: false), Lenient)!;
    }

    /// <summary>The recent activity <paramref name="view"/> may see, oldest first.</summary>
    public async Task<IReadOnlyList<Activity>> GetActivityAsync(string chronicleId, ChronicleView? view = null, int limit = 200, CancellationToken ct = default) =>
        Read<List<Activity>>(await Send(HttpMethod.Get, $"{Path(chronicleId)}/activity{Query(view)}{(view is null ? "?" : "&")}limit={limit}", null, ct), Lenient) ?? [];

    // ---- Subscribers ----

    /// <summary>Registers this app, or one of its parts, as a subscriber, so the Chronicle and its UI show who reads what.</summary>
    public async Task<Subscriber> RegisterSubscriberAsync(string chronicleId, string subscriberId, SubscriberRegistration registration, CancellationToken ct = default)
    {
        var body = JsonSerializer.SerializeToNode(registration, ChronicleJson.Compact)!.AsObject();
        body["app"] = registration.App ?? options.App ?? throw new InvalidOperationException(
            "Set ChronicleClientOptions.App (Chronicle:App in configuration), or the registration's App.");
        return Read<Subscriber>(await Send(HttpMethod.Put, $"{Path(chronicleId)}/subscribers/{Uri.EscapeDataString(subscriberId)}", body.ToJsonString(), ct), Lenient)!;
    }

    /// <summary>Registers a subscriber from the JSON body the API takes.</summary>
    public async Task<JsonObject?> RegisterSubscriberJsonAsync(string chronicleId, string subscriberId, JsonObject body, CancellationToken ct = default) =>
        Json(await Send(HttpMethod.Put, $"{Path(chronicleId)}/subscribers/{Uri.EscapeDataString(subscriberId)}", body.ToJsonString(), ct));

    /// <summary>Removes a subscriber; false when there was none.</summary>
    public async Task<bool> UnregisterSubscriberAsync(string chronicleId, string subscriberId, CancellationToken ct = default) =>
        await SendOrNotFound(HttpMethod.Delete, $"{Path(chronicleId)}/subscribers/{Uri.EscapeDataString(subscriberId)}", null, ct) is not null;

    /// <summary>The subscribers <paramref name="view"/> may see.</summary>
    public async Task<IReadOnlyList<Subscriber>> GetSubscribersAsync(string chronicleId, ChronicleView? view = null, CancellationToken ct = default) =>
        Read<List<Subscriber>>(await Send(HttpMethod.Get, $"{Path(chronicleId)}/subscribers{Query(view)}", null, ct), Lenient) ?? [];

    /// <summary>Runs a subscriber's agent in the background (one registered with an <see cref="AgentSpec"/>).</summary>
    public async Task<RunStarted> RunSubscriberAsync(string chronicleId, string subscriberId, CancellationToken ct = default) =>
        Read<RunStarted>(await Send(HttpMethod.Post, $"{Path(chronicleId)}/subscribers/{Uri.EscapeDataString(subscriberId)}/run", null, ct, retry: false), Lenient)!;

    // ---- Plumbing ----

    private Entry? Typed(RawEntry raw)
    {
        if (raw.TryRead(out var entry, out var error))
            return entry;
        if (options.UnreadableEntries == UnreadableEntries.Throw)
            throw error!;
        log.LogWarning("Chronicle {Chronicle}: skipped entry {Sequence} ({Part} {Kind}), which this library can't read: {Message}",
            raw.ChronicleId, raw.Sequence, raw.Part, raw.Kind, error!.Message);
        return null;
    }

    private static string Path(string chronicleId) => $"api/chronicles/{Uri.EscapeDataString(chronicleId)}";

    private static string Query(ChronicleView? view) =>
        view is null ? "" : Query([("audience", Name(view.Audience)), .. (view.Viewers ?? []).Select(v => ("viewers", v))]);

    private static string Query(Subscription s, bool includePosition = true) => Query([
        ("audience", Name(s.Audience)),
        .. s.Viewers.Select(v => ("viewers", v)),
        .. s.Parts.Select(p => ("parts", Topics.PartName(p))),
        .. s.Kinds.Select(k => ("kinds", k)),
        .. includePosition && s.AfterSequence > 0 ? [("after", s.AfterSequence.ToString())] : Array.Empty<(string, string)>()
    ]);

    private static string Query(IEnumerable<(string Key, string Value)> pairs)
    {
        var text = string.Join('&', pairs.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return text.Length == 0 ? "" : $"?{text}";
    }

    private static string Name(Audience audience) => JsonNamingPolicy.CamelCase.ConvertName(audience.ToString());

    private async Task<HttpRequestMessage> Request(HttpMethod method, string path, string? body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, $"{options.Url.TrimEnd('/')}/{path}");
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        if (options.ConfigureRequest is { } configure)
            await configure(request, ct);
        return request;
    }

    private async Task<string> Send(HttpMethod method, string path, string? body, CancellationToken ct, bool retry = true) =>
        (await SendWithStatus(method, path, body, ct, retry)).Body;

    /// <summary>The body, or null when the API answers 404.</summary>
    private async Task<string?> SendOrNotFound(HttpMethod method, string path, string? body, CancellationToken ct)
    {
        try
        {
            return await Send(method, path, body, ct);
        }
        catch (ChronicleApiException e) when (e.IsNotFound)
        {
            return null;
        }
    }

    private async Task<(HttpStatusCode Status, string Body)> SendWithStatus(HttpMethod method, string path, string? body, CancellationToken ct, bool retry = true)
    {
        for (var attempt = 0; ; attempt++)
        {
            var last = !retry || attempt >= options.Retries;
            HttpStatusCode status;
            string text;
            try
            {
                using var request = await Request(method, path, body, ct);
                using var response = await http.SendAsync(request, ct);
                status = response.StatusCode;
                text = await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception e) when (Unreachable(e, ct))
            {
                if (last)
                    throw Unavailable(e);
                log.LogDebug("Chronicle API unreachable ({Message}); retrying.", e.Message);
                await Backoff(attempt, ct);
                continue;
            }
            if ((int)status is >= 200 and < 300)
                return (status, text);
            if ((int)status < 500 || last)
                throw Refused(status, text);
            await Backoff(attempt, ct);
        }
    }

    private static bool Unreachable(Exception e, CancellationToken ct) =>
        e is HttpRequestException || (e is OperationCanceledException && !ct.IsCancellationRequested);

    private ChronicleApiException Unavailable(Exception e) =>
        new(null, "UNAVAILABLE", $"The Chronicle API at {options.Url} didn't answer ({e.Message}).", e);

    private static ChronicleApiException Refused(HttpStatusCode status, string body)
    {
        string? code = null, detail = null;
        try
        {
            if (JsonNode.Parse(body) is JsonObject problem)
            {
                code = (string?)problem["code"];
                detail = (string?)problem["detail"] ?? (string?)problem["title"];
                if (problem["findings"] is JsonArray findings)
                    detail = string.Join(" ", findings.Select(f => $"{(string?)f?["code"]}: {(string?)f?["message"]}"));
            }
        }
        catch (JsonException)
        {
        }
        detail ??= body.Length > 600 ? body[..600] : body;
        return new ChronicleApiException(status, code, $"The Chronicle API refused it (HTTP {(int)status}{(code is null ? "" : $" {code}")}): {detail}");
    }

    private async Task Backoff(int attempt, CancellationToken ct)
    {
        var delay = options.RetryDelay * Math.Pow(2, Math.Min(attempt, 16));
        if (delay > options.MaxRetryDelay)
            delay = options.MaxRetryDelay;
        await Task.Delay(delay * (0.8 + Random.Shared.NextDouble() * 0.4), ct);
    }

    private static T? Read<T>(string? body, JsonSerializerOptions json) =>
        string.IsNullOrEmpty(body) ? default : JsonSerializer.Deserialize<T>(body, json);

    private static JsonObject? Json(string? body) => string.IsNullOrEmpty(body) ? null : JsonNode.Parse(body) as JsonObject;
}
