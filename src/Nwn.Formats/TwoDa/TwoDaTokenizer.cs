namespace Nwn.Formats.TwoDa;

/// <summary>Splits one 2DA line into whitespace-delimited tokens, honoring double-quoted tokens
/// (which may contain spaces/tabs) the way the toolset's own values do.</summary>
internal static class TwoDaTokenizer
{
    public static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        var i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            if (i >= line.Length)
            {
                break;
            }

            if (line[i] == '"')
            {
                var end = line.IndexOf('"', i + 1);
                if (end < 0)
                {
                    throw new FormatException($"Unterminated quoted value in 2DA line: {line}");
                }

                tokens.Add(line[(i + 1)..end]);
                i = end + 1;
            }
            else
            {
                var start = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i]))
                {
                    i++;
                }

                tokens.Add(line[start..i]);
            }
        }
        return tokens;
    }

    /// <summary>Quotes a value only when necessary (it is empty or contains whitespace); leaves
    /// <c>****</c> alone since that already round-trips as a bare token.</summary>
    public static string Format(string? value)
    {
        if (value is null)
        {
            return "****";
        }

        if (value.Length > 0 && !value.Any(char.IsWhiteSpace))
        {
            return value;
        }

        return $"\"{value}\"";
    }
}
