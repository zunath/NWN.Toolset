using System.Text;
using Nwn.Formats.Resources;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>Derives a blueprint resref from the display name a builder typed.</summary>
public static class PaletteResRefNames
{
    /// <summary>
    /// Reduces a display name to a legal NWN resref: lowercase, alphanumerics and underscores, at most
    /// <see cref="ResourceReferenceRules.MaxLength"/> characters. Anything else is dropped rather than
    /// substituted, so the result stays readable. Empty when the name has no letters or digits.
    /// </summary>
    public static string FromDisplayName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (char.IsAsciiLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
            else if (character is ' ' or '_' or '-' && builder.Length > 0 && builder[^1] != '_')
                builder.Append('_');

            if (builder.Length == ResourceReferenceRules.MaxLength)
                break;
        }

        return builder.ToString().TrimEnd('_');
    }
}
