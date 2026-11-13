// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ScenarioBuilder.Api.Domain;

/// <summary>
/// A scenario's World and Intent as a Chronicle record (src/chronicle, schema 0.3.0), built from the pull document
/// (<see cref="ScenarioDocument"/>). Each item carries the decision that set it: its time, who made it, person or
/// agent, whether it is still proposed, and the reason. Devices, services and personas come from the Hosts, Services
/// and Named Simulated Users answers (<see cref="WorldParser"/>).
/// </summary>
/// <remarks>
/// Who knows what: a planned event whose text names an organization or a persona gets <c>knownBy</c> that item, so the
/// newspaper knows the story it runs and the operator knows the message she sends; nothing else is inferred.
/// </remarks>
public static partial class ChronicleRecordBuilder
{
    public const string SchemaVersion = "0.3.0";

    private sealed record Pending(string Part, JsonObject Item, JsonObject? Decision);

    public static JsonObject Build(JsonObject doc, string chronicleId, string owner, DateTime created, DateTime updated)
    {
        var decisions = new Dictionary<string, JsonObject>();
        foreach (var d in (doc["decisions"] as JsonArray ?? []).OfType<JsonObject>())
            decisions[(string)d["question"]!] = d;
        var items = new List<Pending>();
        var ids = new HashSet<string>();

        string Add(string part, string kind, string baseId, string question, params (string Key, JsonNode? Value)[] fields)
        {
            var id = baseId;
            for (var n = 2; !ids.Add(id); n++)
                id = $"{baseId}-{n}";
            var item = new JsonObject { ["kind"] = kind, ["id"] = id };
            foreach (var (key, value) in fields)
                if (!Empty(value))
                    item[key] = value;
            item["visibility"] ??= "participant";
            decisions.TryGetValue(question, out var decision);
            item["standing"] = (string?)decision?["who"] == "proposed" ? "proposed" : "stated";
            items.Add(new Pending(part, item, decision));
            return id;
        }
        string? Fact(string topic, string? text, string question, string visibility = "participant", JsonArray? knownBy = null) =>
            string.IsNullOrWhiteSpace(text) ? null
                : Add("world", "fact", WorldParser.SlugOf(Camel().Replace(topic, "-")), question, ("topic", topic), ("text", text), ("visibility", visibility), ("knownBy", knownBy));

        var eng = doc["engagement"] as JsonObject ?? [];
        var sc = doc["scenario"] as JsonObject ?? [];
        var net = doc["network"] as JsonObject ?? [];
        var names = doc["names"] as JsonObject ?? [];
        var vocab = doc["vocabulary"] as JsonObject ?? [];

        // ---- World ----
        var sides = new Dictionary<string, string>();
        var orgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Texts(names["organizations"]))
        {
            var (name, stance) = Split(entry);
            string? key = stance?.ToLowerInvariant() switch { "friendly" => "blue", "adversary" or "hostile" => "red", _ => null };
            if (key is not null && !sides.ContainsKey(key))
                sides[key] = Add("world", "side", key, "organizations", ("name", key == "blue" ? "Friendly" : "Adversary"), ("visibility", key == "blue" ? "participant" : "excon"));
            orgs[name] = Add("world", "organization", $"org-{WorldParser.SlugOf(name)}", "organizations",
                ("name", name), ("sideId", key is null ? null : sides[key]), ("visibility", key == "red" ? "excon" : "participant"));
        }
        if (sides.ContainsKey("blue") && sides.ContainsKey("red"))
            foreach (var p in items.Where(i => (string?)i.Item["kind"] == "side"))
                p.Item["relations"] = new JsonArray(new JsonObject { ["sideId"] = (string)p.Item["id"]! == "blue" ? "red" : "blue", ["stance"] = "hostile" });

        Fact("situation", Text(sc["placeAndTime"]), "placeAndTime");
        Fact("audience", Text(eng["unit"]), "unit");
        Fact("audience", Text(eng["teamSize"]), "teamSize");
        Fact("audience", Text(eng["proficiency"]), "proficiency");
        Fact("network", Text(net["connectivity"]), "connectivity");
        Fact("identity", Text(net["identity"]), "identity");
        Fact("simulatedUsers", Text(net["simulatedUsers"]), "simulatedUsers", "excon");
        foreach (var e in Items(sc["groundTruth"]).Concat(Items(doc["groundTruth"])))
            Add("world", "fact", $"gt-{WorldParser.SlugOf(e.Id ?? e.Text)}", "groundTruth", ("topic", "groundTruth"), ("text", e.Text), ("visibility", e.Visibility));
        Fact("groundTruth", Text(sc["adversary"]), "adversary", "excon");
        foreach (var e in Items(sc["attackTechniques"]))
            Fact("attackTechnique", e.Text, "attackTechniques", e.Visibility);
        foreach (var e in Items(net["planted"]))
            Fact("planted", e.Text, "planted", e.Visibility);
        foreach (var e in Texts(net["monitoring"]))
            Add("world", "defense", $"def-{WorldParser.SlugOf(e, 30)}", "monitoring", ("description", e));

        var segments = new List<string>();
        foreach (var e in Texts(net["segments"]))
        {
            var (name, about) = Split(e);
            segments.Add(name);
            Add("world", "network", $"net-{WorldParser.SlugOf(name)}", "segments", ("name", name), ("description", about));
        }
        foreach (var h in WorldParser.Hosts(Texts(net["hosts"]), segments))
            Add("world", "device", h.Id, "hosts", ("name", h.Name), ("description", Sentence(h.Role)), ("role", Sentence(h.Role)),
                ("deviceTypes", new JsonArray("ComputerNode")), ("isVirtual", true),
                ("relatedObjects", h.Segment is null || !segments.Contains(h.Segment) ? null
                    : new JsonArray(new JsonObject { ["objectId"] = $"net-{WorldParser.SlugOf(h.Segment)}", ["relationship"] = "connectedTo" })));
        var services = WorldParser.Services(Texts(net["services"]));
        foreach (var s in services)
            Add("world", "service", s.Id, "services", ("name", s.Name), ("serviceType", s.ServiceType), ("address", s.Address));
        var mail = services.FirstOrDefault(s => s.ServiceType == "Email");
        var personas = WorldParser.Personas(Texts(net["personas"]), orgs.Keys.ToList());
        foreach (var p in personas)
            Add("world", "persona", p.Id, "personas", ("name", p.Name), ("description", p.Description), ("role", p.Role),
                ("organizationId", p.Organization is null ? null : orgs[p.Organization]), ("origin", "authored"),
                ("accounts", mail is null ? null : new JsonArray(new JsonObject { ["serviceId"] = mail.Id, ["username"] = WorldParser.Username(p.Name) })));

        // ---- Intent ----
        var measures = Texts(sc["standards"]).ToList();
        var objectives = (doc["objectives"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        for (var i = 0; i < objectives.Count; i++)
            Add("intent", "objective", $"obj-{WorldParser.SlugOf((string?)objectives[i]["id"] ?? (i + 1).ToString())}", "tasks",
                ("text", (string?)objectives[i]["text"]), ("measures", measures.Count == objectives.Count ? new JsonArray(measures[i]) : null));
        foreach (var e in Items(doc["teachingPoints"]))
            Add("intent", "teachingPoint", $"tp-{WorldParser.SlugOf(e.Text, 30)}", "teachingPoints", ("text", e.Text), ("visibility", e.Visibility));
        foreach (var (field, category) in new[] { ("rulesOfEngagement", "conduct"), ("escalationCeiling", "scope"), ("deadline", "timing") })
            if ((Text(eng[field]) ?? Text(sc[field])) is { } value)
                Add("intent", "rule", $"rule-{category}", field, ("text", value), ("category", category), ("visibility", field == "escalationCeiling" ? "excon" : "participant"));
        foreach (var (usage, question) in new[] { ("fixed", "fixedTerms"), ("banned", "bannedTerms") })
            foreach (var term in Texts(vocab[usage]).Where(t => t.Trim().Length > 0 && !t.Trim().Equals("none", StringComparison.OrdinalIgnoreCase)))
                Add("intent", "term", $"term-{WorldParser.SlugOf(term, 30)}", question, ("text", term), ("usage", usage));
        var knowers = orgs.Select(o => (Name: o.Key, Id: o.Value)).Concat(personas.Select(p => (p.Name, p.Id))).ToList();
        string? previous = null;
        foreach (var e in Items(sc["keyEvents"]))
        {
            var knownBy = new JsonArray(knowers.Where(k => e.Text.Contains(k.Name, StringComparison.OrdinalIgnoreCase)).Select(k => (JsonNode)k.Id).ToArray());
            previous = Add("intent", "plannedEvent", $"pe-{WorldParser.SlugOf(e.Id ?? e.Text)}", "keyEvents", ("title", e.Text), ("visibility", e.Visibility),
                ("after", previous is null ? null : new JsonArray(previous)), ("knownBy", knownBy));
        }
        var steps = Items(sc["escalationSteps"]).ToList();
        for (var i = 0; i < steps.Count; i++)
            Add("intent", "summary", $"esc-{i + 1}", "escalationSteps", ("title", $"Escalation step {i + 1}"), ("text", steps[i].Text), ("visibility", steps[i].Visibility));
        if (Text(sc["branching"]) is { } branching)
            Add("intent", "summary", "branching", "branching", ("title", "Branching"), ("text", branching), ("visibility", "excon"));
        if (Text(sc["description"]) is { } description)
            Add("intent", "summary", "participant-description", "description", ("title", "What the team is told"), ("text", description));
        if (Text(sc["executiveSummary"]) is { } summary)
            Add("intent", "summary", "executive-summary", "executiveSummary", ("title", "Executive summary"), ("text", summary), ("visibility", "excon"));

        // ---- History: each item in the order its decision was made ----
        var history = new JsonArray();
        var ordered = items.Select((p, i) => (p, i, at: Time(p.Decision) ?? updated)).OrderBy(x => x.at).ThenBy(x => x.i).ToList();
        for (var n = 0; n < ordered.Count; n++)
        {
            var (p, _, at) = ordered[n];
            history.Add(Entry(chronicleId, p.Part, p.Item, at, n + 1, Author(p.Decision, owner), (string?)p.Decision?["reason"]));
        }
        JsonArray Part(string name) => new(history.OfType<JsonObject>().Where(e => (string?)e["part"] == name).Select(e => e["item"]!.DeepClone()).ToArray());

        var record = new JsonObject
        {
            ["schemaVersion"] = SchemaVersion,
            ["id"] = chronicleId,
            ["name"] = (string?)doc["name"],
            ["kind"] = "exercise",
            ["scenarioType"] = doc["scenarioType"]?.DeepClone(),
            ["created"] = created.ToUniversalTime().ToString("O"),
            ["world"] = Part("world"),
            ["intent"] = Part("intent"),
            ["asBuilt"] = new JsonArray(),
            ["asRun"] = new JsonArray(),
            ["lessons"] = new JsonArray(),
            ["history"] = history
        };
        if ((Text(sc["description"]) ?? Text(eng["intent"])) is { } summaryText)
            record["description"] = summaryText;
        return record;
    }

    /// <summary>One entry adding <paramref name="item"/>; with no sequence, as an app publishes it.</summary>
    public static JsonObject Entry(string chronicleId, string part, JsonObject item, DateTime at, long? sequence, JsonObject author, string? reason,
        string op = "add", string? revises = null)
    {
        var entry = new JsonObject
        {
            ["id"] = Guid.CreateVersion7(new DateTimeOffset(at.ToUniversalTime())).ToString(),
            ["chronicleId"] = chronicleId,
            ["recordedAt"] = at.ToUniversalTime().ToString("O"),
            ["part"] = part,
            ["kind"] = (string?)item["kind"],
            ["op"] = op,
            ["itemId"] = (string?)item["id"],
            ["item"] = item.DeepClone(),
            ["author"] = author
        };
        if (sequence is { } s)
            entry["sequence"] = s;
        if (revises is not null)
            entry["revises"] = revises;
        if (!string.IsNullOrWhiteSpace(reason))
            entry["reason"] = reason;
        return entry;
    }

    /// <summary>The record's items by id, with the part each is in.</summary>
    public static IEnumerable<(string Part, JsonObject Item)> ItemsOf(JsonObject record) =>
        new[] { "world", "intent", "asBuilt", "asRun", "lessons" }
            .SelectMany(p => (record[p] as JsonArray ?? []).OfType<JsonObject>().Select(i => (p, i)));

    /// <summary>Two versions of an item are the same when they say the same thing, whatever the order and the defaults left out.</summary>
    public static bool Same(JsonNode? a, JsonNode? b) => Canonical(a) == Canonical(b);

    private static string Canonical(JsonNode? node) => node switch
    {
        null => "null",
        JsonObject o => "{" + string.Join(",", o.Where(p => !Empty(p.Value) && !(p.Key == "standing" && (string?)p.Value == "stated"))
            .OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{JsonSerializer.Serialize(p.Key)}:{Canonical(p.Value)}")) + "}",
        JsonArray a => "[" + string.Join(",", a.Select(Canonical)) + "]",
        _ => node.ToJsonString()
    };

    private static bool Empty(JsonNode? value) => value switch
    {
        null => true,
        JsonValue v when v.TryGetValue(out string? s) => string.IsNullOrWhiteSpace(s),
        JsonArray a => a.Count == 0,
        JsonObject o => o.Count == 0,
        _ => false
    };

    private static JsonObject Author(JsonObject? decision, string owner)
    {
        var author = new JsonObject { ["app"] = "scenario-builder", ["actor"] = (string?)decision?["by"] ?? owner };
        author["via"] = (string?)decision?["via"] switch { "agent" => "agent", "person" => "person", _ => "app" };
        return author;
    }

    private static DateTime? Time(JsonObject? decision) =>
        decision?["time"] is JsonValue v && v.TryGetValue(out DateTime t) ? t : decision?["time"] is JsonValue s && DateTime.TryParse(s.ToString(), out var p) ? p : null;

    private static string? Text(JsonNode? node) => node switch
    {
        JsonValue v when v.TryGetValue(out string? s) && !string.IsNullOrWhiteSpace(s) => s,
        JsonObject o => (string?)o["text"],
        _ => null
    };

    private static IEnumerable<string> Texts(JsonNode? node) => Items(node).Select(i => i.Text);

    private sealed record ListItem(string? Id, string Text, string Visibility);

    private static IEnumerable<ListItem> Items(JsonNode? node)
    {
        if (node is JsonValue single && single.TryGetValue(out string? one) && !string.IsNullOrWhiteSpace(one))
        {
            yield return new ListItem(null, one, "participant");
            yield break;
        }
        if (node is not JsonArray list)
            yield break;
        foreach (var e in list)
        {
            if (e is JsonObject o && (string?)o["text"] is { Length: > 0 } text)
                yield return new ListItem((string?)o["id"], text, (string?)o["visibility"] ?? "participant");
            else if (e is JsonValue v && v.TryGetValue(out string? s) && !string.IsNullOrWhiteSpace(s))
                yield return new ListItem(null, s, "participant");
        }
    }

    private static (string Name, string? About) Split(string text)
    {
        var colon = text.IndexOf(':');
        return colon < 0 ? (text.Trim(), null) : (text[..colon].Trim(), text[(colon + 1)..].Trim() is { Length: > 0 } r ? r : null);
    }

    private static string Sentence(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex Camel();
}
