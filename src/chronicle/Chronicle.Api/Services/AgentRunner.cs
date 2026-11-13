// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Chronicle.Api.Models;

namespace Chronicle.Api.Services;

/// <summary>The model a subscriber's agent writes with: Ollama's own API (thinking off), or any OpenAI-compatible server.</summary>
public sealed class AgentOptions
{
    public string Provider { get; set; } = "ollama";
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "gemma4:26b-32k";
    public string? ApiKey { get; set; }
    public int MaxTokens { get; set; } = 400;
}

/// <summary>
/// Runs a subscriber's agent in the background: it reads the subscriber's view, streams the model's words to live
/// listeners who may see that view, then publishes what it wrote as an entry from the agent's template.
/// </summary>
public sealed partial class AgentRunner(
    ChronicleStore store, LiveHub hub, ActivityService activity,
    IHttpClientFactory clients, Microsoft.Extensions.Options.IOptions<AgentOptions> options,
    IHostApplicationLifetime lifetime, ILogger<AgentRunner> log)
{
    public const string Client = "agents";

    /// <summary>Starts a run; null when the subscriber has no agent.</summary>
    public RunStarted? Start(Subscriber subscriber)
    {
        if (subscriber.Agent is null)
            return null;
        var runId = Guid.CreateVersion7().ToString("N")[..12];
        _ = Task.Run(() => Run(subscriber, subscriber.Agent, runId, lifetime.ApplicationStopping));
        return new RunStarted(runId, subscriber.Id);
    }

    private async Task Run(Subscriber s, AgentSpec agent, string runId, CancellationToken ct)
    {
        var chronicleId = s.ChronicleId;
        Task<Activity> Report(string kind, string text, JsonObject? data = null, Entry? entry = null) =>
            activity.Report(chronicleId, new ActivityPost
            {
                Source = s.App, SubscriberId = s.Id, Kind = kind, Text = text, Visibility = s.Audience, Data = data, Entry = entry
            }, ct);
        try
        {
            var record = store.Get(chronicleId) ?? throw new KeyNotFoundException(chronicleId);
            var context = ContextBuilder.Build(record, s.Audience, s.Viewers, s.Parts);
            await Report("agent.started", $"Reading {context.Items} items ({context.Withheld} withheld)", new JsonObject
            {
                ["runId"] = runId, ["items"] = context.Items, ["total"] = context.Total, ["withheld"] = context.Withheld,
                ["tokens"] = context.Tokens, ["query"] = context.Query, ["sample"] = new JsonArray([.. context.Sample.Select(x => (JsonNode)x)])
            });

            var task = agent.Task.Contains("{{context}}") ? agent.Task.Replace("{{context}}", context.Text) : $"{agent.Task}\n\n{context.Text}";
            var text = new StringBuilder();
            await foreach (var piece in Stream(agent.System, task, s.Model, agent.MaxTokens ?? options.Value.MaxTokens, ct))
            {
                text.Append(piece);
                hub.Send(chronicleId, new LiveHub.Message(LiveHub.Serialize(new { type = "token", subscriberId = s.Id, runId, text = piece }), s.Audience));
            }

            var written = Clean(text.ToString());
            var entry = Publishable(chronicleId, agent.Output, written, runId);
            var published = await Report("agent.published", $"Published {entry.Kind} to {Words(entry.Part)}",
                new JsonObject { ["runId"] = runId, ["part"] = JsonNamingPolicy.CamelCase.ConvertName(entry.Part.ToString()), ["kind"] = entry.Kind, ["itemId"] = entry.ItemId, ["text"] = written },
                entry);
            log.LogInformation("Agent {Subscriber} on {Chronicle} published entry {Sequence}", s.Id, chronicleId, published.Sequence);
        }
        catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            log.LogWarning(e, "Agent {Subscriber} on {Chronicle} failed", s.Id, chronicleId);
            await Report("agent.failed", e.Message, new JsonObject { ["runId"] = runId }).ConfigureAwait(false);
        }
    }

    /// <summary>The agent's output as an entry: an add, or a revise when its item is already in the record.</summary>
    private Entry Publishable(string chronicleId, AgentOutput output, string text, string runId)
    {
        var lines = text.Split('\n');
        var title = lines.FirstOrDefault(l => l.Trim().Length > 0)?.Trim() ?? "";
        var body = string.Join('\n', lines.SkipWhile(l => l.Trim().Length == 0).Skip(1)).Trim();
        var values = new Dictionary<string, string>
        {
            ["text"] = text, ["title"] = title, ["body"] = body, ["now"] = DateTimeOffset.UtcNow.ToString("O"), ["runId"] = runId
        };
        var json = Fill(output.Item.DeepClone(), values)!.ToJsonString();
        var item = ChronicleJson.Deserialize<Item>(json);
        var record = store.Get(chronicleId)!;
        var now = DateTimeOffset.UtcNow;
        var latest = record.History.LastOrDefault(e => e.ItemId == item.Id);
        return latest is null || latest.Op == Operation.Retract
            ? Entry.Add(chronicleId, output.Part, item, output.Author, now, output.Reason)
            : Entry.Revise(chronicleId, output.Part, item, latest.Id, output.Author, now, output.Reason);
    }

    private static JsonNode? Fill(JsonNode? node, IReadOnlyDictionary<string, string> values) => node switch
    {
        JsonObject o => new JsonObject(o.Select(p => KeyValuePair.Create(p.Key, Fill(p.Value?.DeepClone(), values)))),
        JsonArray a => new JsonArray([.. a.Select(x => Fill(x?.DeepClone(), values))]),
        JsonValue v when v.TryGetValue<string>(out var s) => JsonValue.Create(Placeholder().Replace(s, m => values.GetValueOrDefault(m.Groups[1].Value, m.Value))),
        _ => node
    };

    /// <summary>What the model wrote, without markdown: no bold, no heading marks, bullets as •.</summary>
    public static string Clean(string text) =>
        Bullet().Replace(Heading().Replace(text.Replace("**", ""), ""), "• ").Trim();

    private static string Words(Part part) => Regex.Replace(part.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

    private async IAsyncEnumerable<string> Stream(string system, string task, string? model, int maxTokens,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var o = options.Value;
        var openai = o.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase);
        var messages = new JsonArray(new JsonObject { ["role"] = "system", ["content"] = system }, new JsonObject { ["role"] = "user", ["content"] = task });
        var body = openai
            ? new JsonObject { ["model"] = model ?? o.Model, ["stream"] = true, ["max_tokens"] = maxTokens, ["messages"] = messages }
            : new JsonObject { ["model"] = model ?? o.Model, ["stream"] = true, ["think"] = false, ["messages"] = messages, ["options"] = new JsonObject { ["num_predict"] = maxTokens } };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{o.BaseUrl.TrimEnd('/')}/{(openai ? "v1/chat/completions" : "api/chat")}")
        {
            Content = JsonContent.Create(body)
        };
        if (!string.IsNullOrEmpty(o.ApiKey))
            request.Headers.Authorization = new("Bearer", o.ApiKey);
        using var response = await clients.CreateClient(Client).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"The model server answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (openai)
            {
                if (!line.StartsWith("data:")) continue;
                var data = line[5..].Trim();
                if (data == "[DONE]") yield break;
                if (JsonNode.Parse(data)?["choices"]?[0]?["delta"]?["content"]?.GetValue<string>() is { Length: > 0 } piece)
                    yield return piece;
            }
            else if (line.Trim().Length > 0)
            {
                var chunk = JsonNode.Parse(line);
                if (chunk?["error"] is { } error)
                    throw new InvalidOperationException($"The model server failed: {error}");
                if (chunk?["message"]?["content"]?.GetValue<string>() is { Length: > 0 } piece)
                    yield return piece;
                if (chunk?["done"]?.GetValue<bool>() == true) yield break;
            }
        }
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"^#+\s*", RegexOptions.Multiline)]
    private static partial Regex Heading();

    [GeneratedRegex(@"^[ \t]*[*-][ \t]+", RegexOptions.Multiline)]
    private static partial Regex Bullet();
}
