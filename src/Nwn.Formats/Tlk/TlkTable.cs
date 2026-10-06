namespace Nwn.Formats.Tlk;

/// <summary>An in-memory TLK table: a language ID plus an ordered, zero-based list of entries.
/// Entry index <c>i</c> corresponds to a game strref of <c>i</c> in this table, or, when used as the
/// module's alternate/custom tlk, to the engine strref <c>0x01000000 + i</c> -- see
/// <see cref="CustomStrRefBase"/>.</summary>
public sealed class TlkTable(uint languageId)
{
    /// <summary>The engine reads a strref with this bit set as "look up (strref - this) in the
    /// module's custom tlk" instead of dialog.tlk.</summary>
    public const uint CustomStrRefBase = 0x01000000;

    public uint LanguageId { get; } = languageId;
    public List<TlkEntry> Entries { get; } = [];

    public static uint ToCustomStrRef(int entryIndex) => CustomStrRefBase + (uint)entryIndex;

    public static int FromCustomStrRef(uint strRef)
    {
        if (strRef < CustomStrRefBase)
        {
            throw new ArgumentOutOfRangeException(nameof(strRef), strRef, "Not a custom-tlk strref.");
        }

        return (int)(strRef - CustomStrRefBase);
    }
}
