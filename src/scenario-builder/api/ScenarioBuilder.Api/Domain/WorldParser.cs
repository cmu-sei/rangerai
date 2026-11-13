// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System.Globalization;
using System.Text.RegularExpressions;

namespace ScenarioBuilder.Api.Domain;

/// <summary>A host the Hosts answer names: its hostname, the segment it sits on, what it does, its OS and how many.</summary>
public sealed record ParsedHost(string Id, string Hostname, string Name, string? Segment, string Role, string? Os, int Count, bool Virtual);

/// <summary>A service the Services answer names: what it is, its address when it has one, and the host that runs it.</summary>
public sealed record ParsedService(string Id, string Name, string ServiceType, string? Address, string? Host);

/// <summary>A named simulated user: who they are, what they do, and the organization they work for.</summary>
public sealed record ParsedPersona(string Id, string Name, string? Role, string? Organization, string? Description);

/// <summary>
/// Reads the free-text network answers into typed World items. The answers follow the forms their questions ask for
/// ("corp-lan: dc01, domain controller, Ubuntu, 1"; "Email for kbwa.org: mail01"; "Dana Whitlock: night-shift plant
/// operator, Kessick Bay Water Authority, messages IT"), and anything that doesn't still becomes one item per line.
/// </summary>
public static partial class WorldParser
{
    public static string SlugOf(string text, int limit = 40)
    {
        var s = NotSlug().Replace(text.ToLowerInvariant(), "-").Trim('-');
        if (s.Length > limit)
            s = s[..limit].TrimEnd('-');
        return s.Length == 0 ? "x" : s;
    }

    /// <summary>"segment: name[ and name], role…, OS, count", one host or several per line.</summary>
    public static List<ParsedHost> Hosts(IEnumerable<string> lines, IReadOnlyCollection<string> segments)
    {
        var hosts = new List<ParsedHost>();
        var taken = new HashSet<string>();
        foreach (var raw in lines.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            string? segment = null;
            var body = raw;
            var colon = raw.IndexOf(':');
            if (colon > 0)
            {
                var head = raw[..colon].Trim();
                if (segments.Count == 0 || segments.Contains(head, StringComparer.OrdinalIgnoreCase) || !head.Contains(' '))
                {
                    segment = segments.FirstOrDefault(s => s.Equals(head, StringComparison.OrdinalIgnoreCase)) ?? head;
                    body = raw[(colon + 1)..].Trim();
                }
            }

            var parts = body.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            if (parts.Count == 0)
                continue;
            var count = 1;
            if (parts.Count > 1 && int.TryParse(parts[^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
            {
                count = n;
                parts.RemoveAt(parts.Count - 1);
            }
            string? os = null;
            if (parts.Count > 1 && OsWords().IsMatch(parts[^1]))
            {
                os = parts[^1];
                parts.RemoveAt(parts.Count - 1);
            }
            var virtualMachine = os is not null && VmWord().IsMatch(os);
            var namePart = parts[0];
            var role = string.Join(", ", parts.Skip(1));

            var names = NameSplit().Split(namePart).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (names.Count > 1 && names.All(IsHostname))
            {
                // "mail01 and www, mail and the public website": one role each when the role splits the same way.
                var roles = NameSplit().Split(role).Select(s => s.Trim()).ToList();
                for (var i = 0; i < names.Count; i++)
                {
                    var hostname = Unique(names[i].ToLowerInvariant(), taken);
                    hosts.Add(new ParsedHost($"host-{hostname}", hostname, names[i], segment,
                        roles.Count == names.Count ? Trim(roles[i]) : role, os, 1, virtualMachine));
                }
            }
            else if (IsHostname(namePart))
            {
                var hostname = Unique(namePart.ToLowerInvariant(), taken);
                hosts.Add(new ParsedHost($"host-{hostname}", hostname, namePart, segment, role, os, count, virtualMachine));
            }
            else
            {
                // A description, such as "staff workstations with GHOSTS clients": named from the words before "with".
                var shortName = WithClause().Split(namePart)[0].Trim();
                var hostname = Unique(Abbreviate(shortName), taken);
                var name = shortName.Length == 0 ? namePart : char.ToUpperInvariant(shortName[0]) + shortName[1..];
                hosts.Add(new ParsedHost($"host-{hostname}", hostname, name, segment, role.Length == 0 ? namePart : $"{namePart}, {role}", os, count, virtualMachine));
            }
        }
        return hosts;
    }

    /// <summary>"Label: host", such as "Website www.kbwa.org: www".</summary>
    public static List<ParsedService> Services(IEnumerable<string> lines)
    {
        var services = new List<ParsedService>();
        var taken = new HashSet<string>();
        foreach (var raw in lines.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            var colon = raw.LastIndexOf(':');
            var label = colon > 0 ? raw[..colon].Trim() : raw;
            var host = colon > 0 ? raw[(colon + 1)..].Trim().ToLowerInvariant() : null;
            if (host is not null && !IsHostname(host))
                host = null;
            var address = Domain().Match(label) is { Success: true } m ? m.Value.TrimEnd('.').ToLowerInvariant() : null;
            var name = Quoted().Replace(label, "$1").Trim();
            services.Add(new ParsedService($"svc-{Unique(SlugOf(name, 36), taken)}", name, TypeOf(label), address, host));
        }
        return services;
    }

    /// <summary>"Name: role, organization, what they do". The organization is whichever part names a known one.</summary>
    public static List<ParsedPersona> Personas(IEnumerable<string> lines, IReadOnlyCollection<string> organizations)
    {
        var personas = new List<ParsedPersona>();
        var taken = new HashSet<string>();
        foreach (var raw in lines.Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            var colon = raw.IndexOf(':');
            var name = (colon > 0 ? raw[..colon] : raw.Split(',')[0]).Trim();
            var rest = colon > 0 ? raw[(colon + 1)..] : string.Join(',', raw.Split(',').Skip(1));
            var parts = rest.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            string? organization = null;
            var at = parts.FindIndex(p => organizations.Any(o => o.Equals(p, StringComparison.OrdinalIgnoreCase)));
            if (at >= 0)
            {
                organization = organizations.First(o => o.Equals(parts[at], StringComparison.OrdinalIgnoreCase));
                parts.RemoveAt(at);
            }
            else if (organizations.FirstOrDefault(o => rest.Contains(o, StringComparison.OrdinalIgnoreCase)) is { } named)
            {
                organization = named;
            }
            var role = parts.Count > 0 ? parts[0] : null;
            var description = parts.Count > 1 ? Sentence(string.Join(", ", parts.Skip(1))) : null;
            personas.Add(new ParsedPersona($"persona-{Unique(SlugOf(name), taken)}", name, role is null ? null : Sentence(role).TrimEnd('.'), organization, description));
        }
        return personas;
    }

    /// <summary>A mailbox name for a persona: first.last.</summary>
    public static string Username(string name) =>
        string.Join('.', NotSlug().Replace(name.ToLowerInvariant(), " ").Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static string TypeOf(string label)
    {
        var l = label.ToLowerInvariant();
        if (l.Contains("news")) return "News website";
        if (l.Contains("mail")) return "Email";
        if (l.Contains("directory") || l.Contains("active directory")) return "Directory and DNS";
        if (l.Contains("dns")) return "DNS";
        if (l.Contains("file share") || l.Contains("share")) return "File share";
        if (l.Contains("remote access") || l.Contains("vpn") || l.Contains("jump")) return "Remote access";
        if (l.Contains("hmi") || l.Contains("scada")) return "HMI";
        if (l.Contains("historian")) return "Historian";
        if (l.Contains("portal")) return "Website";
        if (l.Contains("web") || l.Contains("site")) return "Website";
        if (l.Contains("chat")) return "Chat";
        return "Service";
    }

    public static bool IsHostname(string text) => Hostname().IsMatch(text.Trim());

    private static string Abbreviate(string words)
    {
        var map = new Dictionary<string, string> { ["workstations"] = "ws", ["workstation"] = "ws", ["servers"] = "srv", ["server"] = "srv", ["controllers"] = "ctl", ["controller"] = "ctl" };
        var parts = NotSlug().Replace(words.ToLowerInvariant(), " ").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => map.TryGetValue(w, out var a) ? a : w).ToList();
        var slug = string.Join('-', parts);
        return slug.Length == 0 ? "host" : slug.Length > 20 ? slug[..20].TrimEnd('-') : slug;
    }

    private static string Unique(string stem, HashSet<string> taken)
    {
        var candidate = stem;
        for (var n = 2; !taken.Add(candidate); n++)
            candidate = $"{stem}-{n}";
        return candidate;
    }

    private static string Trim(string text) => text.Trim().TrimEnd('.');

    private static string Sentence(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NotSlug();

    [GeneratedRegex(@"\b(ubuntu|windows|debian|linux|centos|rhel|alpine|kali|server \d{4}|vm|lxc|container|macos)\b", RegexOptions.IgnoreCase)]
    private static partial Regex OsWords();

    [GeneratedRegex(@"\b(vm|virtual machine|windows)\b", RegexOptions.IgnoreCase)]
    private static partial Regex VmWord();

    [GeneratedRegex(@"\s+(?:and|&)\s+|\s*/\s*")]
    private static partial Regex NameSplit();

    [GeneratedRegex(@"\s+with\s+", RegexOptions.IgnoreCase)]
    private static partial Regex WithClause();

    [GeneratedRegex("^[a-z][a-z0-9-]{0,30}[0-9a-z]$|^[a-z]$", RegexOptions.IgnoreCase)]
    private static partial Regex Hostname();

    [GeneratedRegex(@"\b[a-z0-9](?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)*\.(?:com|org|net|gov|mil|io|ai|edu|us|uk|internal|local|range)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Domain();

    [GeneratedRegex("['\"‘’“”]([^'\"‘’“”]+)['\"‘’“”]")]
    private static partial Regex Quoted();
}
