// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ScenarioBuilder.Api.Domain;

/// <summary>The GHOSTS API the drafted GHOSTS clients report to (<c>Enclave:Ghosts</c>).</summary>
public sealed class EnclaveDraftOptions
{
    public const string Section = "Enclave";
    public GhostsOptions Ghosts { get; set; } = new();

    public sealed class GhostsOptions
    {
        public string ApiUrl { get; set; } = "http://ghosts-api.range.local:5000/api";
        public string ClientUrl { get; set; } = "http://ghosts-api.range.local/client/ghosts-client-universal.tar.gz";
    }
}

/// <summary>
/// Drafts a GraySpace enclave (slice spec) from a scenario's stage 3 answers and what GraySpace offers.
/// <list type="bullet">
/// <item><b>Sites:</b> one NAT site per segment, except a DMZ (a segment named or described as one), which joins the
/// site of the segment it shares a word with (vendor-dmz, "remote access into the plant", joins ot-control, "the
/// treatment plant's control network"), else the site before it. A site is named for the first friendly organization's
/// initials and its segment (kbwa-corp), or the shared word when a DMZ joined it (kbwa-plant).</item>
/// <item><b>Attachments:</b> the first site attaches at the attachment point nearest the scenario's place (a city, or a
/// region such as the Pacific Northwest); each other site at the nearest point of an ISP not used yet. Internet hosts
/// attach at the point nearest the place.</item>
/// <item><b>Hosts:</b> each parsed host (<see cref="WorldParser.Hosts"/>) on its segment: a VM image for a VM, an
/// LXC image otherwise; roles from what it does and the services it runs; public when it serves a website, mail or
/// remote access at an address. A service whose host is on no segment becomes an internet host (a news site on the
/// Pandora image). Every host is labelled with the World item it builds (<c>chronicle.of</c>) and the Chronicle
/// (<c>chronicle.id</c>); GHOSTS hosts get a <c>ghosts.pool</c>.</item>
/// <item><b>DNS and TLS:</b> each service address' domain, hosted round robin on GraySpace's DNS hosting servers;
/// an A record per named address and MX for mail; a certificate for each public website and remote access.</item>
/// </list>
/// </summary>
public static partial class EnclaveDrafter
{
    public sealed class NothingToDraftException(string message) : Exception(message);

    private static readonly Dictionary<string, string[]> Regions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pacific northwest"] = ["Portland", "Seattle", "Vancouver"],
        ["west coast"] = ["San Francisco", "Los Angeles", "Portland"],
        ["california"] = ["San Francisco", "Los Angeles"],
        ["new england"] = ["New York", "Boston"],
        ["northeast"] = ["New York", "Ashburn"],
        ["midwest"] = ["Chicago", "Kansas City"],
        ["gulf coast"] = ["Houston"],
        ["texas"] = ["Houston", "Dallas"],
        ["mid-atlantic"] = ["Ashburn", "New York"],
        ["east coast"] = ["New York", "Ashburn"],
        ["europe"] = ["Frankfurt", "London", "Paris"],
        ["east asia"] = ["Tokyo", "Seoul", "Hong Kong"]
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "into", "from", "with", "network", "networks", "segment", "access", "remote", "corporate", "its", "for", "that", "this", "their", "where"
    };

    /// <param name="doc">The scenario's pull document.</param>
    /// <param name="current">The enclave as it is: its slug, prefix and description are kept.</param>
    /// <param name="capabilities">GraySpace's <c>GET /capabilities</c>.</param>
    /// <param name="attachmentPoints">GraySpace's <c>GET /attachment-points</c>.</param>
    public static JsonObject Draft(JsonObject doc, JsonObject current, JsonObject capabilities, JsonArray attachmentPoints, EnclaveDraftOptions options)
    {
        var net = doc["network"] as JsonObject ?? [];
        var segmentLines = Texts(net["segments"]).ToList();
        var parsedHosts = WorldParser.Hosts(Texts(net["hosts"]), segmentLines.Select(l => Split(l).Name).ToList());
        if (segmentLines.Count == 0 && parsedHosts.Count == 0)
            throw new NothingToDraftException("Answer Network Segments and Hosts in stage 3 first; the enclave is drafted from them.");
        var services = WorldParser.Services(Texts(net["services"]));
        var chronicleId = (string?)doc["slug"] ?? (string?)current["slug"] ?? "scenario";
        var org = FriendlyOrganization(doc);
        var orgTag = Initials(org);

        // ---- Sites ----
        var segments = segmentLines.Select(l => Split(l)).Where(s => s.Name.Length > 0).ToList();
        foreach (var h in parsedHosts.Where(h => h.Segment is not null && segments.All(s => !s.Name.Equals(h.Segment, StringComparison.OrdinalIgnoreCase))))
            segments.Add((h.Segment!, null));
        if (segments.Count == 0)
            segments.Add(("lan", null));
        var siteOf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var siteSegments = new List<List<string>>();
        var siteWords = new List<string?>();
        for (var i = 0; i < segments.Count; i++)
        {
            var (name, about) = segments[i];
            if (IsDmz(name, about) && siteSegments.Count > 0)
            {
                var words = Words($"{name} {about}");
                var match = Enumerable.Range(0, i).Where(j => !IsDmz(segments[j].Name, segments[j].About))
                    .Select(j => (j, shared: Words($"{segments[j].Name} {segments[j].About}").Intersect(words).FirstOrDefault()))
                    .FirstOrDefault(x => x.shared is not null);
                var site = match.shared is not null ? siteOf[segments[match.j].Name] : siteSegments.Count - 1;
                siteSegments[site].Add(name);
                siteWords[site] ??= match.shared;
                siteOf[name] = site;
                continue;
            }
            siteOf[name] = siteSegments.Count;
            siteSegments.Add([name]);
            siteWords.Add(null);
        }

        var points = attachmentPoints.OfType<JsonObject>().ToList();
        var origin = Place(doc, points);
        var usedIsps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sites = new JsonArray();
        var siteNames = new List<string>();
        var takenSiteNames = new HashSet<string>();
        for (var s = 0; s < siteSegments.Count; s++)
        {
            var word = siteWords[s] ?? SiteWord(siteSegments[s][0]);
            var siteName = Unique(orgTag is null ? word : $"{orgTag}-{word}", takenSiteNames);
            siteNames.Add(siteName);
            var point = Nearest(points, origin, p => !usedIsps.Contains((string?)p["isp"] ?? "")) ?? Nearest(points, origin, _ => true);
            if (point is not null)
                usedIsps.Add((string?)point["isp"] ?? "");
            sites.Add(new JsonObject
            {
                ["name"] = siteName,
                ["attach"] = point is null ? new JsonObject() : new JsonObject { ["router"] = (string?)point["router"] },
                ["routed"] = false,
                ["segments"] = new JsonArray(siteSegments[s].Select(n => (JsonNode)new JsonObject { ["name"] = n }).ToArray())
            });
        }
        var internetPoint = Nearest(points, origin, _ => true);

        // ---- Hosts ----
        var images = (capabilities["images"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var roles = (capabilities["roles"] as JsonArray ?? []).OfType<JsonObject>().ToDictionary(r => (string)r["name"]!, r => r);
        var lxc = images.FirstOrDefault(i => (string?)i["type"] == "lxc" && (string?)i["os_family"] == "ubuntu") ?? images.FirstOrDefault(i => (string?)i["type"] == "lxc");
        var vm = images.FirstOrDefault(i => (string?)i["type"] == "vm") ?? lxc;
        var pandora = images.FirstOrDefault(i => ((string?)i["name"] ?? "").Contains("pandora", StringComparison.OrdinalIgnoreCase));
        var hosts = new JsonArray();
        var hostByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedRoles = new HashSet<string>();

        JsonArray RolesFor(string text, JsonObject? image)
        {
            var family = (string?)image?["os_family"];
            var wanted = new List<string>();
            var t = text.ToLowerInvariant();
            if (t.Contains("domain controller") || t.Contains("active directory") || t.Contains("directory and dns")) wanted.AddRange(["ad-dc", "dns"]);
            if (Regex.IsMatch(t, @"\bdns\b")) wanted.Add("dns");
            if (t.Contains("file share") || t.Contains("file server")) wanted.Add("file");
            if (Regex.IsMatch(t, @"\bmail\b|email")) wanted.Add("mail");
            if (t.Contains("website") || t.Contains("web site") || t.Contains("web server") || t.Contains("hmi") || t.Contains("portal")) wanted.Add("web");
            if (t.Contains("ghosts")) wanted.Add("ghosts-client");
            var fits = wanted.Distinct().Where(r => roles.TryGetValue(r, out var role)
                && (family is null || (role["os_families"] as JsonArray ?? []).Count == 0 || (role["os_families"] as JsonArray)!.Any(f => (string?)f == family))).ToList();
            usedRoles.UnionWith(fits);
            return new JsonArray(fits.Select(r => (JsonNode)r).ToArray());
        }

        bool Public(IEnumerable<ParsedService> on) => on.Any(s => s.Address is not null && s.ServiceType is "Website" or "Email" or "Remote access" or "News website");

        foreach (var h in parsedHosts)
        {
            var on = services.Where(s => s.Host == h.Hostname).ToList();
            var image = h.Virtual || Regex.IsMatch(h.Role, @"\b(hmi|engineering workstation)\b", RegexOptions.IgnoreCase) ? vm : lxc;
            var hostRoles = RolesFor($"{h.Role} {string.Join(' ', on.Select(s => $"{s.Name} {s.ServiceType}"))}", image);
            var labels = new JsonObject { ["chronicle.of"] = h.Id, ["chronicle.id"] = chronicleId };
            if (hostRoles.Any(r => (string?)r == "ghosts-client"))
                labels["ghosts.pool"] = orgTag is null ? h.Hostname.TrimEnd("0123456789".ToCharArray()) : $"{orgTag}-{h.Hostname.TrimEnd("0123456789".ToCharArray()).TrimEnd('-')}";
            var host = new JsonObject
            {
                ["name"] = h.Hostname,
                ["segment"] = h.Segment ?? segments[0].Name,
                ["image"] = (string?)image?["name"],
                ["roles"] = hostRoles,
                ["labels"] = labels
            };
            if (h.Count > 1)
                host["count"] = h.Count;
            if (Public(on))
                host["publish"] = true;
            hosts.Add(host);
            hostByName[h.Hostname] = h.Hostname;
        }
        foreach (var s in services.Where(s => s.Host is not null && !hostByName.ContainsKey(s.Host)))
        {
            var news = s.ServiceType == "News website" && pandora is not null;
            var image = news ? pandora : lxc;
            var host = new JsonObject
            {
                ["name"] = s.Host,
                ["internet"] = internetPoint is null ? new JsonObject() : new JsonObject { ["router"] = (string?)internetPoint["router"] },
                ["image"] = (string?)image?["name"],
                ["roles"] = news ? new JsonArray() : RolesFor($"{s.Name} {s.ServiceType}", image),
                ["labels"] = new JsonObject { ["chronicle.of"] = s.Id, ["chronicle.id"] = chronicleId }
            };
            if (news)
                host["env"] = new JsonObject { ["PANDORA_SITE_NAME"] = SiteTitle(s), ["PANDORA_THEME"] = "news" };
            hosts.Add(host);
            hostByName[s.Host!] = s.Host!;
        }

        // ---- DNS and TLS ----
        var dnsCaps = capabilities["dns"] as JsonObject ?? [];
        var tlds = (dnsCaps["tlds"] as JsonArray ?? []).Select(t => (string?)t).ToHashSet();
        var hosting = (dnsCaps["hosting"] as JsonArray ?? []).OfType<JsonObject>().Select(h => (string)h["hostname"]!).ToList();
        var domains = new JsonArray();
        var domainNames = new List<string>();
        var records = new JsonArray();
        var tls = new JsonArray();
        var ca = (capabilities["cas"] as JsonArray ?? []).OfType<JsonObject>().Select(c => (string?)c["hostname"]).FirstOrDefault();
        foreach (var s in services.Where(s => s.Address is not null && s.Host is not null && hostByName.ContainsKey(s.Host)))
        {
            var domain = Registrable(s.Address!);
            if (domain is null || (tlds.Count > 0 && !tlds.Contains(domain[(domain.LastIndexOf('.') + 1)..])))
                continue;
            if (!domainNames.Contains(domain) && hosting.Count > 0)
            {
                domains.Add(new JsonObject { ["domain"] = domain, ["hosting_server"] = hosting[domainNames.Count % hosting.Count] });
                domainNames.Add(domain);
            }
            if (!domainNames.Contains(domain))
                continue;
            if (s.ServiceType == "Email")
            {
                records.Add(new JsonObject { ["name"] = $"mail.{domain}", ["type"] = "A", ["host"] = s.Host, ["ttl"] = 300 });
                records.Add(new JsonObject { ["name"] = domain, ["type"] = "MX", ["data"] = $"10 mail.{domain}.", ["ttl"] = 300 });
                continue;
            }
            if (s.Address != domain && s.ServiceType is "Website" or "News website" or "Remote access")
            {
                records.Add(new JsonObject { ["name"] = s.Address, ["type"] = "A", ["host"] = s.Host, ["ttl"] = 300 });
                if (ca is not null && (s.ServiceType is "Website" or "News website" or "Remote access"))
                    tls.Add(new JsonObject { ["host"] = s.Host, ["names"] = new JsonArray(s.Address), ["ca"] = ca });
            }
        }

        // ---- Role settings ----
        var roleVars = new JsonObject();
        var identity = Text(net["identity"]) ?? "";
        var realm = DomainToken().Match(identity) is { Success: true } m ? m.Value.ToLowerInvariant()
            : domainNames.FirstOrDefault() is { } d ? $"corp.{d}" : $"{chronicleId}.internal";
        var netbios = NetBios().Match(identity) is { Success: true } nb ? nb.Groups[1].Value : realm.Split('.')[^2].ToUpperInvariant();
        var secret = Secret(chronicleId);
        if (usedRoles.Contains("ad-dc"))
            roleVars["ad-dc"] = new JsonObject { ["realm"] = realm, ["domain"] = netbios.Length > 15 ? netbios[..15] : netbios, ["admin_password"] = secret };
        if (usedRoles.Contains("dns"))
            roleVars["dns"] = new JsonObject { ["zone"] = realm };
        if (usedRoles.Contains("file"))
        {
            var share = services.FirstOrDefault(s => s.ServiceType == "File share");
            var shareName = share is null ? "shared" : WorldParser.SlugOf(Regex.Replace(share.Name, @"^file share\s*", "", RegexOptions.IgnoreCase), 30);
            roleVars["file"] = new JsonObject { ["share_name"] = shareName == "x" ? "shared" : shareName, ["guest_ok"] = false };
        }
        if (usedRoles.Contains("mail"))
        {
            var mailDomain = services.Where(s => s.ServiceType == "Email" && s.Address is not null).Select(s => Registrable(s.Address!)).FirstOrDefault(x => x is not null) ?? domainNames.FirstOrDefault();
            var users = new JsonArray(WorldParser.Personas(Texts(net["personas"]), []).Select(p => (JsonNode)new JsonObject { ["name"] = WorldParser.Username(p.Name), ["password"] = secret }).ToArray());
            var mail = new JsonObject { ["users"] = users };
            if (mailDomain is not null)
                mail["domain"] = mailDomain;
            roleVars["mail"] = mail;
        }
        if (usedRoles.Contains("web") && org is not null)
            roleVars["web"] = new JsonObject { ["site_title"] = org };
        if (usedRoles.Contains("ghosts-client"))
            roleVars["ghosts-client"] = new JsonObject { ["client_url"] = options.Ghosts.ClientUrl, ["api_url"] = options.Ghosts.ApiUrl };

        return new JsonObject
        {
            ["apiVersion"] = (string?)current["apiVersion"] ?? "grayspace/v1",
            ["kind"] = (string?)current["kind"] ?? "Slice",
            ["slug"] = (string?)current["slug"],
            ["prefix"] = (string?)current["prefix"],
            ["description"] = $"{(string?)doc["name"]}: drafted from the scenario's network answers",
            ["owner"] = (string?)current["owner"] ?? "scenario-builder",
            ["sites"] = sites,
            ["hosts"] = hosts,
            ["dns"] = new JsonObject { ["domains"] = domains, ["records"] = records },
            ["tls"] = tls,
            ["conditions"] = new JsonObject { ["links"] = new JsonObject() },
            ["role_vars"] = roleVars
        };
    }

    /// <summary>The domain a name is registered under: its last two labels (www.kbwa.org → kbwa.org).</summary>
    public static string? Registrable(string address)
    {
        var labels = address.Trim('.').ToLowerInvariant().Split('.');
        return labels.Length < 2 ? null : $"{labels[^2]}.{labels[^1]}";
    }

    private static bool IsDmz(string name, string? about) => Regex.IsMatch($"{name} {about}", @"\bdmz\b", RegexOptions.IgnoreCase) || name.EndsWith("-dmz", StringComparison.OrdinalIgnoreCase);

    private static HashSet<string> Words(string text) =>
        Regex.Matches(text.ToLowerInvariant(), @"[a-z]+(?:'s)?").Select(w => w.Value.EndsWith("'s") ? w.Value[..^2] : w.Value)
            .Where(w => w.Length >= 4 && !StopWords.Contains(w)).ToHashSet();

    /// <summary>A site named for its segment: corp-lan → corp.</summary>
    private static string SiteWord(string segment)
    {
        var s = Regex.Replace(WorldParser.SlugOf(segment, 20), "-(lan|net|network|segment|control|zone)$", "");
        return s.Length == 0 ? "site" : s;
    }

    private static string? FriendlyOrganization(JsonObject doc)
    {
        var unit = Text(doc["engagement"]?["unit"]) ?? "";
        return Texts(doc["names"]?["organizations"])
            .Select(o => o.Split(':') is var p ? (Name: p[0].Trim(), Side: p.Length > 1 ? p[1].Trim().ToLowerInvariant() : "") : default)
            .Where(o => o.Side == "friendly" && !unit.Contains(o.Name, StringComparison.OrdinalIgnoreCase))
            .Select(o => o.Name).FirstOrDefault();
    }

    /// <summary>"Kessick Bay Water Authority" → kbwa.</summary>
    private static string? Initials(string? name)
    {
        if (name is null)
            return null;
        var words = Regex.Matches(name, "[A-Za-z0-9]+").Select(m => m.Value).Where(w => !StopWords.Contains(w) && w != "of").ToList();
        var tag = words.Count == 1 ? words[0].ToLowerInvariant() : string.Concat(words.Select(w => char.ToLowerInvariant(w[0])));
        return tag.Length == 0 ? null : tag.Length > 8 ? tag[..8] : tag;
    }

    private static string SiteTitle(ParsedService s) => Regex.Replace(s.Name, @"\b(news site|website|site)\b", "", RegexOptions.IgnoreCase).Trim() is { Length: > 0 } t && !t.Contains('.') ? t : s.Name;

    /// <summary>The scenario's place as a coordinate: a city an attachment point names, found in the place, setting or brief; else a region's cities.</summary>
    private static (double Lat, double Lon)? Place(JsonObject doc, List<JsonObject> points)
    {
        var text = string.Join(' ', new[] { Text(doc["scenario"]?["placeAndTime"]), Text(doc["scenario"]?["description"]), Text(doc["network"]?["connectivity"]) }.Where(t => t is not null));
        var cities = points.Select(p => (string?)p["location"]).Where(l => l is not null).Distinct().ToList();
        var city = cities.FirstOrDefault(c => text.Contains(c!, StringComparison.OrdinalIgnoreCase));
        city ??= Regions.Where(r => text.Contains(r.Key, StringComparison.OrdinalIgnoreCase)).SelectMany(r => r.Value)
            .FirstOrDefault(c => cities.Contains(c, StringComparer.OrdinalIgnoreCase));
        var at = points.FirstOrDefault(p => string.Equals((string?)p["location"], city, StringComparison.OrdinalIgnoreCase));
        return at?["latitude"] is JsonValue lat && at["longitude"] is JsonValue lon ? (lat.GetValue<double>(), lon.GetValue<double>()) : null;
    }

    private static JsonObject? Nearest(List<JsonObject> points, (double Lat, double Lon)? origin, Func<JsonObject, bool> allowed)
    {
        var candidates = points.Where(allowed).ToList();
        if (origin is not { } o)
            return candidates.FirstOrDefault();
        return candidates.OrderBy(p => p["latitude"] is JsonValue lat && p["longitude"] is JsonValue lon
            ? Math.Pow(lat.GetValue<double>() - o.Lat, 2) + Math.Pow((lon.GetValue<double>() - o.Lon) * Math.Cos(o.Lat * Math.PI / 180), 2)
            : double.MaxValue).FirstOrDefault();
    }

    /// <summary>A password for the drafted role settings, the same for the same scenario so redrafting doesn't make a new version.</summary>
    private static string Secret(string seed) =>
        "Rng-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"ranger-enclave:{seed}")))[..10].ToLowerInvariant() + "!";

    private static string Unique(string stem, HashSet<string> taken)
    {
        var c = stem;
        for (var n = 2; !taken.Add(c); n++)
            c = $"{stem}-{n}";
        return c;
    }

    private static (string Name, string? About) Split(string line)
    {
        var colon = line.IndexOf(':');
        return colon < 0 ? (line.Trim(), null) : (line[..colon].Trim(), line[(colon + 1)..].Trim());
    }

    private static string? Text(JsonNode? node) => node is JsonValue v && v.TryGetValue(out string? s) && s.Trim().Length > 0 ? s : (string?)(node as JsonObject)?["text"];

    private static IEnumerable<string> Texts(JsonNode? node) => node switch
    {
        JsonArray a => a.Select(e => e is JsonObject o ? (string?)o["text"] : e is JsonValue v && v.TryGetValue(out string? s) ? s : null).Where(s => !string.IsNullOrWhiteSpace(s))!,
        JsonValue v when v.TryGetValue(out string? s) && !string.IsNullOrWhiteSpace(s) => [s],
        _ => []
    };

    [GeneratedRegex(@"\b[a-z0-9-]+(?:\.[a-z0-9-]+)+\.(?:org|com|net|gov|mil|io|local|internal|lan)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DomainToken();

    [GeneratedRegex(@"\(([A-Z][A-Z0-9-]{1,14})\)")]
    private static partial Regex NetBios();
}
