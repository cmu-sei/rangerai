// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ScenarioBuilder.Api.Domain;

/// <summary>One downstream agent to register on the Chronicle: its id and the body the Chronicle API's PUT takes.</summary>
public sealed record SubscriberDefinition(string Id, JsonObject Body);

/// <summary>
/// The downstream agents a scenario's Chronicle starts with, derived from what was published: a GHOSTS simulated user
/// for each named persona, a Pandora newsroom for each news organization, and always the exercise control assistant.
/// Each reads only its own view: a persona and a newsroom see what participants see plus what they're known to know
/// (<c>knownBy</c>); the assistant sees everything exercise control does.
/// </summary>
public static partial class ChronicleSubscribers
{
    public static List<SubscriberDefinition> Derive(JsonObject record, JsonObject doc, string model)
    {
        var world = (record["world"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var intent = (record["intent"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        string? KnownEvent(string id) => intent.FirstOrDefault(i => (string?)i["kind"] == "plannedEvent"
            && (i["knownBy"] as JsonArray ?? []).Any(k => (string?)k == id))?["id"]?.GetValue<string>();
        var unit = UnitName(doc);
        var subscribers = new List<SubscriberDefinition>();

        foreach (var persona in world.Where(i => (string?)i["kind"] == "persona"))
        {
            var id = (string)persona["id"]!;
            var slug = id.StartsWith("persona-") ? id["persona-".Length..] : id;
            var name = (string?)persona["name"] ?? slug;
            var role = (string?)persona["role"] ?? "a member of staff";
            var item = new JsonObject
            {
                ["kind"] = "injectDelivery", ["id"] = $"ir-{slug}", ["at"] = "{{now}}",
                ["to"] = new JsonArray(unit), ["note"] = "{{text}}", ["visibility"] = "participant"
            };
            if (KnownEvent(id) is { } plannedEvent)
                item["plannedEventId"] = plannedEvent;
            subscribers.Add(new($"ghosts-{slug}", Body("ghosts", $"GHOSTS · {name}", $"Simulated user: {role}", "participant", [id], ["world", "intent"], model,
                "You are a GHOSTS simulated user on a cyber range. Stay fully in character as the persona described. You only know what is in your context. Never mention exercises, simulations or AI.",
                $"Your persona and what you know:\n{{{{context}}}}\n\nYou are {name}. Write the message you send to {unit} at the start, about what you've noticed. 3 or 4 short sentences, plain, the way you'd really write it. Output only the message.",
                200, "asRun", item, new JsonObject { ["app"] = "ghosts", ["actor"] = id, ["via"] = "agent" },
                $"{name}'s first message, written in character from their own view.")));
        }

        var newsService = world.FirstOrDefault(i => (string?)i["kind"] == "service" && (string?)i["serviceType"] == "News website");
        foreach (var org in world.Where(i => (string?)i["kind"] == "organization" && News().IsMatch((string?)i["name"] ?? "")))
        {
            var id = (string)org["id"]!;
            var slug = id.StartsWith("org-") ? id["org-".Length..] : id;
            var name = (string?)org["name"] ?? slug;
            var item = new JsonObject
            {
                ["kind"] = "content", ["id"] = $"content-{slug}-story", ["format"] = "text/markdown",
                ["title"] = "{{title}}", ["body"] = "{{body}}", ["attributedTo"] = name, ["visibility"] = "participant"
            };
            if (newsService is not null)
                item["serviceId"] = (string)newsService["id"]!;
            if (KnownEvent(id) is { } plannedEvent)
                item["plannedEventId"] = plannedEvent;
            subscribers.Add(new($"pandora-{slug}", Body("pandora", $"Pandora · {name}", "Content: the news site on the range", "participant", [id], ["world", "intent"], model,
                $"You write news articles for {name}, a news website on a cyber range. Write as a careful local reporter: who, what, when, a quote from those involved. Only use facts in your context; never invent a cause the public could not know.",
                $"What the newsroom knows:\n{{{{context}}}}\n\nWrite the article {name} runs about the planned story in your context. Give a headline on the first line, then 3 short paragraphs, under 170 words in total. No markdown symbols.",
                360, "asBuilt", item, new JsonObject { ["app"] = "pandora", ["actor"] = id, ["via"] = "agent" },
                $"The story {name} runs, written from the newsroom's own view.")));
        }

        subscribers.Add(new("excon-assistant", Body("excon-assistant", "Exercise Control Assistant", "Works for exercise control: sees everything", "excon", [],
            ["world", "intent", "asBuilt", "asRun"], model,
            "You assist the exercise control cell of a cyber exercise. Be concise and specific: name hosts, accounts, times and injects from your context.",
            "The exercise record, as exercise control sees it:\n{{context}}\n\nThe exercise has just started (H+0:00). Write the controllers' brief for the first 90 minutes: what to watch for on the range, which built hosts and accounts the evidence is on, the next two injects and when, and the branch that escalates. 5 to 6 bullets, under 130 words. Plain text only: no markdown, no asterisks, no headings; start each bullet with '• '.",
            320, "asRun",
            new JsonObject { ["kind"] = "observation", ["id"] = "obs-excon-brief-h0", ["at"] = "{{now}}", ["by"] = "excon-assistant", ["text"] = "{{text}}", ["visibility"] = "excon" },
            new JsonObject { ["app"] = "excon-assistant", ["actor"] = "lead-controller", ["via"] = "agent" },
            "Controllers' brief for the first 90 minutes, from the full record.")));
        return subscribers;
    }

    /// <summary>The trained unit as the Unit answer names it: the words before its colon.</summary>
    public static string UnitName(JsonObject doc)
    {
        var unit = doc["engagement"]?["unit"] is JsonValue v && v.TryGetValue(out string? s) ? s.Trim() : "";
        var colon = unit.IndexOf(':');
        var name = colon > 0 ? unit[..colon].Trim() : unit;
        return name.Length == 0 ? "the team" : name;
    }

    private static JsonObject Body(string app, string name, string role, string audience, string[] viewers, string[] parts, string model,
        string system, string task, int maxTokens, string part, JsonObject item, JsonObject author, string reason) => new()
    {
        ["app"] = app,
        ["name"] = name,
        ["role"] = role,
        ["audience"] = audience,
        ["viewers"] = new JsonArray(viewers.Select(v => (JsonNode)v).ToArray()),
        ["parts"] = new JsonArray(parts.Select(p => (JsonNode)p).ToArray()),
        ["model"] = model,
        ["agent"] = new JsonObject
        {
            ["system"] = system,
            ["task"] = task,
            ["maxTokens"] = maxTokens,
            ["output"] = new JsonObject { ["part"] = part, ["item"] = item, ["author"] = author, ["reason"] = reason }
        }
    };

    [GeneratedRegex(@"\b(ledger|news|times|gazette|herald|post|tribune|journal|chronicle|press|daily|radio|tv|television|courier|observer|record)\b", RegexOptions.IgnoreCase)]
    private static partial Regex News();
}
