using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using RangerAi.Infrastructure;

namespace RangerAi.Handlers;

public class Alive : HandlerBase
{
    protected override Regex MatchPattern { get; } = new(
        @"^(alive|up|test)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    protected override List<string> Responses { get; } =
    [
        $"{GetStatus().GetAwaiter().GetResult()}"
    ];

    private static Task<string> GetStatus()
    {
        var sb = new StringBuilder();

        // 🛰️ Banner
        sb.AppendLine("🦊️ RANGER UP!");
        sb.AppendLine($"v{ApplicationDetails.Version} ({ApplicationDetails.VersionFile}). CERT © 2025. All Rights Reserved.");
        sb.AppendLine($"{DateTime.UtcNow}");

        return Task.FromResult(sb.ToString());
    }
}
