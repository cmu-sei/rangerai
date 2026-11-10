using System.Text.RegularExpressions;

namespace RangerAi.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string ToCondensedLowerCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var startUnderscores = Regex.Match(input, @"^+");
        return startUnderscores + Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1$2").ToLower();
    }

    public static string Capitalize(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }
}
