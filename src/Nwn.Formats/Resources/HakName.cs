namespace Nwn.Formats.Resources;

/// <summary>
/// A hak's name, which is also its filename (<c>&lt;name&gt;.hak</c>) and the string a module's
/// <c>Mod_HakList</c> carries. The GFF field itself is a variable-length string, so nothing in the
/// format stops a longer name -- but the toolset and the tooling around it treat a hak name as a
/// resref, 16 characters of lowercase <c>[a-z0-9_]</c>, and a name outside that is a compatibility
/// problem waiting to happen rather than a working hak.
/// <para>
/// The limit binds tighter than it looks, which is why hak names use short slot prefixes:
/// <c>xm_part_shoulderl</c> is 17 characters and does not fit, <c>xm_pt_shoulderl</c> is 15 and does.
/// </para>
/// </summary>
public static class HakName
{
    public static bool TryValidate(string? name, out string? error)
    {
        if (!Resref.TryParse(name, out _, out var resrefError))
        {
            error = $"Hak name '{name}' is not usable: {resrefError} " +
                    "A hak name is its own filename and its Mod_HakList entry, so it follows the resref rules.";
            return false;
        }

        error = null;
        return true;
    }

    /// <param name="name">The candidate hak name.</param>
    /// <param name="source">Where the name came from, named in the error so the offender is
    /// findable -- a staged folder's path, say, rather than just the bad name.</param>
    public static void Validate(string? name, string source)
    {
        if (!TryValidate(name, out var error))
        {
            throw new FormatException($"{error} (from '{source}')");
        }
    }
}
