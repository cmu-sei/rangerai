// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;
using ScenarioBuilder.Api.Domain;

namespace ScenarioBuilder.Api.Infrastructure;

/// <summary>What a publish did: the Chronicle, how many entries it holds now, its World and Intent counts, and how many entries this publish added.</summary>
public sealed record PublishResult(string ChronicleId, int Entries, int World, int Intent, int Published, int Subscribers, string Url);

/// <summary>
/// Publishes a scenario's World and Intent to its Chronicle, whose id is the scenario's slug. The first publish
/// creates the record with its whole history; after that only what changed is published: an add for each new item,
/// a revise (naming the entry it replaces) for each changed one. Nothing is retracted.
/// </summary>
public sealed class ChroniclePublisher(ChronicleClient chronicle)
{
    public async Task<PublishResult> Publish(JsonObject doc, string chronicleId, string owner, DateTime created, DateTime updated, CancellationToken ct)
    {
        var record = ChronicleRecordBuilder.Build(doc, chronicleId, owner, created, updated);
        var existing = await chronicle.Get(chronicleId, ct);
        if (existing is null)
        {
            await chronicle.Create(record, ct);
            return Result(record, record["history"]!.AsArray().Count, await Register(record, doc, ct));
        }

        var have = ChronicleRecordBuilder.ItemsOf(existing).ToDictionary(x => (string)x.Item["id"]!, x => x.Item);
        var latest = new Dictionary<string, string>();
        foreach (var e in (existing["history"] as JsonArray ?? []).OfType<JsonObject>())
            if ((string?)e["itemId"] is { } itemId && (string?)e["id"] is { } entryId)
                latest[itemId] = entryId;

        var published = 0;
        foreach (var built in record["history"]!.AsArray().OfType<JsonObject>())
        {
            var item = built["item"]!.AsObject();
            var id = (string)item["id"]!;
            var author = built["author"]!.DeepClone().AsObject();
            var reason = (string?)built["reason"];
            JsonObject? entry = null;
            if (!have.TryGetValue(id, out var current))
                entry = ChronicleRecordBuilder.Entry(chronicleId, (string)built["part"]!, item, DateTime.UtcNow, null, author, reason);
            else if (!ChronicleRecordBuilder.Same(current, item))
                entry = ChronicleRecordBuilder.Entry(chronicleId, (string)built["part"]!, item, DateTime.UtcNow, null, author, reason, "revise",
                    latest.GetValueOrDefault(id));
            if (entry is null)
                continue;
            await chronicle.Append(chronicleId, entry, ct);
            published++;
        }
        return Result(await chronicle.Get(chronicleId, ct) ?? existing, published, await Register(record, doc, ct));
    }

    /// <summary>Registers the downstream agents the record calls for; none when the Chronicle API has no subscribers.</summary>
    private async Task<int> Register(JsonObject record, JsonObject doc, CancellationToken ct)
    {
        var registered = 0;
        foreach (var subscriber in ChronicleSubscribers.Derive(record, doc, chronicle.AgentModel))
        {
            try
            {
                if (!await chronicle.PutSubscriber((string)record["id"]!, subscriber.Id, subscriber.Body, ct))
                    return registered;
            }
            catch (ChronicleException)
            {
                return registered;   // the record is published; agents can be registered on the next publish
            }
            registered++;
        }
        return registered;
    }

    private PublishResult Result(JsonObject record, int published, int subscribers) => new(
        (string)record["id"]!,
        (record["history"] as JsonArray)?.Count ?? 0,
        (record["world"] as JsonArray)?.Count ?? 0,
        (record["intent"] as JsonArray)?.Count ?? 0,
        published,
        subscribers,
        chronicle.UiLink((string)record["id"]!));
}
