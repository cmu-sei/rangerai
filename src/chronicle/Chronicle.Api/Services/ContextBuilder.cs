// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Chronicle.Api.Models;

namespace Chronicle.Api.Services;

/// <summary>
/// A view of a Chronicle as text a model can read: one line per item the reader may see, so every agent is grounded the
/// same way. Also counts what the view withholds against the designer's.
/// </summary>
public static class ContextBuilder
{
    private static readonly Part[] DefaultParts = [Part.World, Part.Intent, Part.AsBuilt, Part.AsRun];

    public static ContextView Build(ChronicleRecord record, Audience audience, IReadOnlyCollection<string> viewers, IReadOnlyList<Part> parts)
    {
        var wanted = parts.Count > 0 ? parts : DefaultParts;
        var view = AudienceView.For(record, audience, viewers);
        var items = wanted.SelectMany(p => view[p]).ToList();
        var total = wanted.Sum(p => record[p].Count);
        var lines = items.Select(Line).ToList();
        var text = string.Join('\n', lines);
        return new ContextView(
            SubscriberStore.Query(record.Id, audience, [.. viewers], parts),
            items.Count, total, total - items.Count, (int)Math.Round(text.Length / 4.0), text,
            [.. items.Take(60).Select(Title)]);
    }

    private static JsonObject Json(Item item) => JsonNode.Parse(ChronicleJson.Serialize(item, indented: false))!.AsObject();

    private static string? Str(JsonObject o, string key) =>
        o[key] is JsonValue v && v.TryGetValue<string>(out var s) && s.Length > 0 ? s : null;

    public static string Title(Item item)
    {
        var o = Json(item);
        return Str(o, "name") ?? Str(o, "title") ?? Str(o, "text") ?? Str(o, "description") ?? item.Id;
    }

    /// <summary>"[kind:topic] title | role: … | text: … | k=v".</summary>
    public static string Line(Item item)
    {
        var o = Json(item);
        var title = Title(item);
        var line = new StringBuilder($"[{item.Kind}{(Str(o, "topic") is { } topic ? $":{topic}" : "")}] {title}");
        foreach (var key in new[] { "role", "description", "text", "note", "body", "address", "serviceType" })
            if (Str(o, key) is { } value && value != title)
                line.Append($" | {key}: {value}");
        if (o["identifiers"] is JsonObject ids && ids.Count > 0)
            line.Append(" | ").Append(string.Join(' ', ids.Select(p => $"{p.Key}={p.Value}")));
        return line.ToString();
    }
}
