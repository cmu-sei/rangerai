using System.Globalization;
using System.Text.RegularExpressions;

namespace RangerAi.Handlers;

public class Time : HandlerBase
{
    protected override Regex MatchPattern { get; } = new(
        "^time$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    protected override List<string> Responses { get; } =
    [
        DateTime.Now.ToString(CultureInfo.InvariantCulture)
    ];
}
