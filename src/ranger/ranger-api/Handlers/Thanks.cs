using System.Text.RegularExpressions;

namespace RangerAi.Handlers;

public class Thanks : HandlerBase
{
    protected override Regex MatchPattern { get; } = new(
        @"^(thanks|thank you|merci)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    protected override List<string> Responses { get; } =
    [
        "I like you best",
        "You must wear a cape",
        "Thank you.",
        "You’re kind of a big deal now.",
        "set me = grateful",
        "Do you practice being so wonderful?",
        "You rule. (Note: Draw a ruler. You can do it.)",
        "You’re a spark plug for good!!",
        "a) A peach b) Bee’s knees c) Cat’s pajamas. d) All of the above.",
        "You’re what making a difference looks like. (Note: Draw a mirror. Up the fun factor.)",
        "Thanks for believing in robots.",
        "You can’t see but I’m totally doing a happy dance.",
        "I’m beginning to think you’re serious about this whole humanitarian thing...",
        "Even with amnesia, I’d remember to thank you.",
        "Aww shucks."
    ];
}
