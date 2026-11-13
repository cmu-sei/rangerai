// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ScenarioBuilder.Api.Domain;

namespace ScenarioBuilder.Api.Infrastructure;

/// <summary>
/// Relays what GraySpace builds to the scenario's Chronicle, until GraySpace publishes it itself: a deployment when an
/// instance is asked for (building), then, once the instance job has succeeded, one realization per manifest host
/// (what it became: hostname, addresses, name) and the deployment revised to ready. Each item is published once, so
/// following the job again changes nothing. A scenario that hasn't been published to a Chronicle is skipped, and a
/// Chronicle that can't be reached never fails the GraySpace call it rides on.
/// </summary>
public sealed partial class AsBuiltRelay(ChronicleClient chronicle, ILogger<AsBuiltRelay> log)
{
    private static readonly JsonObject Author = new() { ["app"] = "grayspace", ["via"] = "app" };

    public static string DeploymentId(string enclaveSlug) => $"dep-grayspace-{WorldParser.SlugOf(enclaveSlug, 50)}";

    /// <summary>An instance was asked for: the deployment, building.</summary>
    public async Task Building(string chronicleId, JsonObject spec, string? version, string? instanceId, CancellationToken ct)
    {
        try
        {
            if (await chronicle.Get(chronicleId, ct) is not { } record)
                return;
            var slug = (string?)spec["slug"] ?? chronicleId;
            var items = Items(record);
            if (items.ContainsKey(DeploymentId(slug)))
                return;
            await chronicle.Append(chronicleId, ChronicleRecordBuilder.Entry(chronicleId, "asBuilt", Deployment(slug, version, "building", []), DateTime.UtcNow, null,
                Author.DeepClone().AsObject(), $"GraySpace is building instance {instanceId ?? (string?)spec["prefix"]} of enclave {slug}."), ct);
        }
        catch (ChronicleException e)
        {
            log.LogWarning("As Built not relayed to Chronicle {Chronicle}: {Message}", chronicleId, e.Message);
        }
    }

    /// <summary>What GraySpace says of the instance and its job; once the create job has succeeded, what it built.</summary>
    public async Task Followed(string chronicleId, JsonObject spec, string? version, JsonNode? instance, JsonNode? job, CancellationToken ct)
    {
        if ((string?)job?["kind"] != "create_instance" || (string?)job?["state"] != "succeeded" || instance?["hosts"] is not JsonArray { Count: > 0 } hosts)
            return;
        try
        {
            if (await chronicle.Get(chronicleId, ct) is not { } record)
                return;
            var slug = (string?)spec["slug"] ?? chronicleId;
            var items = Items(record);
            var latest = Latest(record);
            var depId = DeploymentId(slug);
            var builds = new List<string>();
            foreach (var host in hosts.OfType<JsonObject>())
            {
                var realization = Realization(host, spec, depId);
                if (realization is null)
                    continue;
                if (!builds.Contains((string)realization["of"]!))
                    builds.Add((string)realization["of"]!);
                if (items.ContainsKey((string)realization["id"]!))
                    continue;
                await chronicle.Append(chronicleId, ChronicleRecordBuilder.Entry(chronicleId, "asBuilt", realization, DateTime.UtcNow, null, Author.DeepClone().AsObject(), null), ct);
            }
            var ready = Deployment(slug, version ?? (string?)instance?["slice"]?["version"] ?? (string?)job?["slice"]?["version"], "ready", builds);
            ready["at"] = (string?)job?["finished"] ?? DateTime.UtcNow.ToString("O");
            if (!items.TryGetValue(depId, out var current))
                await chronicle.Append(chronicleId, ChronicleRecordBuilder.Entry(chronicleId, "asBuilt", ready, DateTime.UtcNow, null, Author.DeepClone().AsObject(),
                    $"Instance {(string?)instance?["id"]} is ready: {hosts.Count} hosts running."), ct);
            else if ((string?)current["state"] != "ready")
                await chronicle.Append(chronicleId, ChronicleRecordBuilder.Entry(chronicleId, "asBuilt", ready, DateTime.UtcNow, null, Author.DeepClone().AsObject(),
                    $"Instance {(string?)instance?["id"]} is ready: {hosts.Count} hosts running.", "revise", latest.GetValueOrDefault(depId)), ct);
        }
        catch (ChronicleException e)
        {
            log.LogWarning("As Built not relayed to Chronicle {Chronicle}: {Message}", chronicleId, e.Message);
        }
    }

    private static JsonObject Deployment(string slug, string? version, string state, List<string> builds)
    {
        var item = new JsonObject
        {
            ["kind"] = "deployment", ["id"] = DeploymentId(slug), ["builder"] = "grayspace", ["reference"] = slug, ["state"] = state, ["visibility"] = "excon"
        };
        if (version is not null)
            item["version"] = version;
        if (builds.Count > 0)
            item["builds"] = new JsonArray(builds.Select(b => (JsonNode)b).ToArray());
        return item;
    }

    /// <summary>One manifest host as what it became; null when nothing says which World item it builds.</summary>
    public static JsonObject? Realization(JsonObject host, JsonObject spec, string deploymentId)
    {
        var name = (string?)host["name"];
        if (name is null)
            return null;
        var specHosts = (spec["hosts"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var copy = Copy().Match(name);
        var specHost = specHosts.FirstOrDefault(h => (string?)h["name"] == name)
            ?? (copy.Success ? specHosts.FirstOrDefault(h => (string?)h["name"] == copy.Groups[1].Value) : null);
        var of = (string?)host["labels"]?["chronicle.of"] ?? (string?)specHost?["labels"]?["chronicle.of"];
        if (of is null)
            return null;
        var identifiers = new JsonObject();
        void Put(string key, string? value) { if (!string.IsNullOrEmpty(value)) identifiers[key] = value; }
        Put("hostname", (string?)host["hostname"]);
        Put("ip", (string?)host["ip"]);
        Put("publicIp", (string?)host["public_ip"]);
        Put("fqdn", (host["fqdns"] as JsonArray)?.Select(f => (string?)f).FirstOrDefault(f => f is not null));
        Put("site", (string?)host["site"]);
        Put("segment", (string?)host["segment"]);
        Put("image", (string?)host["image"]);
        if (copy.Success && specHost is not null && (string?)specHost["name"] != name)
            Put("copy", copy.Groups[2].Value);
        return new JsonObject
        {
            ["kind"] = "realization", ["id"] = $"rz-{WorldParser.SlugOf(name, 50)}", ["of"] = of, ["deploymentId"] = deploymentId,
            ["identifiers"] = identifiers, ["visibility"] = "participant"
        };
    }

    private static Dictionary<string, JsonObject> Items(JsonObject record) =>
        ChronicleRecordBuilder.ItemsOf(record).GroupBy(x => (string)x.Item["id"]!).ToDictionary(g => g.Key, g => g.Last().Item);

    private static Dictionary<string, string> Latest(JsonObject record)
    {
        var latest = new Dictionary<string, string>();
        foreach (var e in (record["history"] as JsonArray ?? []).OfType<JsonObject>())
            if ((string?)e["itemId"] is { } itemId && (string?)e["id"] is { } entryId)
                latest[itemId] = entryId;
        return latest;
    }

    [GeneratedRegex(@"^(.+)-(\d{2,3})$")]
    private static partial Regex Copy();
}
