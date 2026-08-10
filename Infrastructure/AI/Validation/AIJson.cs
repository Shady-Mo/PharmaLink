namespace Infrastructure.AI.Validation;

internal static class AIJson
{
    public static string ExtractJsonObject(string raw)
    {
        var json = raw.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = json.IndexOf('\n');
            if (firstNewline >= 0)
            {
                json = json[(firstNewline + 1)..];
            }

            if (json.EndsWith("```", StringComparison.Ordinal))
            {
                json = json[..^3].Trim();
            }
        }

        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');

        return start >= 0 && end >= start
            ? json[start..(end + 1)]
            : json;
    }
}
