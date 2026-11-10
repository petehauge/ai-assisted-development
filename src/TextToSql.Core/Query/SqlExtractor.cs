using System.Text.RegularExpressions;

namespace TextToSql.Core.Query;

/// <summary>Pulls the SQL statement out of a model reply, removing markdown fences and surrounding commentary.</summary>
public static partial class SqlExtractor
{
    // The first fenced block, with an optional language tag such as ```sql or ```tsql.
    [GeneratedRegex(@"```[A-Za-z-]*[ \t]*\r?\n?(?<sql>.*?)```", RegexOptions.Singleline)]
    private static partial Regex FencedBlock();

    // A line that starts a SQL statement; anything before it is treated as commentary.
    [GeneratedRegex(@"^\s*(SELECT|WITH|INSERT|UPDATE|DELETE|MERGE|DROP|CREATE|ALTER|TRUNCATE|EXEC|EXECUTE|DECLARE|SET)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex StatementStart();

    /// <returns>The SQL text, or an empty string when the reply contains none.</returns>
    public static string Extract(string? reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return string.Empty;
        }

        var fenced = FencedBlock().Match(reply);
        if (fenced.Success)
        {
            return fenced.Groups["sql"].Value.Trim();
        }

        var start = StatementStart().Match(reply);
        return start.Success ? reply[start.Index..].Trim() : reply.Trim();
    }
}
