// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

namespace Chronicle.Client;

/// <summary>
/// Where the Chronicle API is, and who this app is when it publishes. Bound from the <c>Chronicle</c> configuration
/// section by <see cref="ServiceCollectionExtensions.AddChronicleClient(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration)"/>.
/// </summary>
public sealed class ChronicleClientOptions
{
    /// <summary>The configuration section the options are usually bound from.</summary>
    public const string Section = "Chronicle";

    /// <summary>The Chronicle API's base address, such as http://localhost:5285.</summary>
    public string Url { get; set; } = "http://localhost:5285";

    /// <summary>
    /// This app's name, as entries' authors name it: ghosts, grayspace, pandora. The default author of what the
    /// publish helpers write, and the source of the activity this app reports.
    /// </summary>
    public string? App { get; set; }

    /// <summary>The default author's actor, when the app acts for someone; usually set per entry instead.</summary>
    public string? Actor { get; set; }

    /// <summary>How the default author acts. Absent means as an app, on its own.</summary>
    public Via Via { get; set; } = Via.App;

    /// <summary>
    /// How many times a call that didn't reach the API, or that the API failed with a 5xx, is tried again. Publishing is
    /// safe to retry: the API stores an entry once, by its id.
    /// </summary>
    public int Retries { get; set; } = 3;

    /// <summary>The wait before the first retry or reconnect; it doubles each time, up to <see cref="MaxRetryDelay"/>.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>The longest wait between retries or reconnects.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a subscription may go without an event before it reconnects, in case the connection died silently.
    /// The API sends a heartbeat every 15 seconds on a quiet stream, so only a dead connection goes this long.
    /// Reconnecting resumes after the last entry received, so nothing is lost or repeated. Zero or less never does.
    /// </summary>
    public TimeSpan StreamIdleTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// What a typed subscription does with an entry this library can't read, such as an item kind added to the spec
    /// after it was built: skip it (and log a warning), or throw. Raw subscriptions read every entry.
    /// </summary>
    public UnreadableEntries UnreadableEntries { get; set; } = UnreadableEntries.Skip;

    /// <summary>
    /// Called on every request before it is sent, for headers such as an access token. The Chronicle API has no
    /// authentication yet; this is where it will go.
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, ValueTask>? ConfigureRequest { get; set; }

    /// <summary>The author <see cref="App"/>, <see cref="Actor"/> and <see cref="Via"/> make.</summary>
    /// <exception cref="InvalidOperationException">No <see cref="App"/> is set.</exception>
    public Author DefaultAuthor() => new()
    {
        App = App ?? throw new InvalidOperationException(
            "Set ChronicleClientOptions.App (Chronicle:App in configuration) to this app's name, or pass an author."),
        Actor = Actor,
        Via = Via
    };
}

/// <summary>What a typed subscription does with an entry it can't read.</summary>
public enum UnreadableEntries
{
    /// <summary>Skip it, log a warning, and carry on.</summary>
    Skip,

    /// <summary>Throw a <see cref="System.Text.Json.JsonException"/>; the subscription ends.</summary>
    Throw
}
