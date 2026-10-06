namespace Nwn.Formats.Key;

/// <summary>
/// A parsed BIF V1 file: the actual resource bytes for one KEY file table entry. A BIF's variable
/// resource table entries are looked up by their own <c>Index</c> field (the low 20 bits of the
/// entry's ID -- see <see cref="BifResourceEntry"/>), not by their position in the table; the two
/// need not coincide, so <see cref="Find"/> always searches by index rather than indexing the list
/// directly.
/// </summary>
public sealed class BifFile
{
    public required IReadOnlyList<BifResourceEntry> Resources { get; init; }
    internal long IndexBudgetBytes => checked((long)Resources.Count * BifReader.ResourceIndexByteBudget);

    public BifResourceEntry? Find(int index) => Resources.FirstOrDefault(r => r.Index == index);
}
