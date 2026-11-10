using System.Text.RegularExpressions;

namespace RangerAi.Handlers;

public class About : HandlerBase
{
    protected override Regex MatchPattern { get; } = new(
        @"^(about|this|who)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    protected override List<string> Responses { get; } = new()
    {
        "What do you want to know specifically?",
        "I vastly prefer 8-bit games, we can start there",
        "Well, I grew up in Pineland..."
    };
}
