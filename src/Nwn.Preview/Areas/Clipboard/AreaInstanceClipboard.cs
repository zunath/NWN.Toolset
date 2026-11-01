// SPDX-License-Identifier: MIT

using Nwn.Authoring.Areas.Editing;

namespace Nwn.Preview.Areas.Clipboard;

/// <summary>The toolset-session clipboard for one placed area object.</summary>
/// <remarks>
/// The payload retains its native GFF instance, paired GIC comment and render preview.
/// Sharing one clipboard between area editors permits placement in another area of the
/// same module without converting the native values into operating-system clipboard text.
/// </remarks>
public sealed class AreaInstanceClipboard
{
    public AreaInstanceClipboardEntry? Content => Contents.FirstOrDefault();
    public IReadOnlyList<AreaInstanceClipboardEntry> Contents { get; private set; } = Array.Empty<AreaInstanceClipboardEntry>();
    public AreaTileClipboardEntry? Tiles { get; private set; }

    public void Set(AreaInstanceClipboardEntry content) => Set(new[] { content });

    public void Set(IReadOnlyList<AreaInstanceClipboardEntry> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Contents = Array.AsReadOnly(contents.ToArray());
        Tiles = null;
    }

    public void SetTiles(AreaTileClipboardEntry tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        Tiles = tiles;
        Contents = Array.Empty<AreaInstanceClipboardEntry>();
    }
}
