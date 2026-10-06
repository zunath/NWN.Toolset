// SPDX-License-Identifier: MIT

namespace Nwn.Preview.Areas.Clipboard;

/// <summary>The toolset-session clipboard for one placed area object.</summary>
/// <remarks>
/// The payload retains its native GFF instance, paired GIC comment and render preview.
/// Sharing one clipboard between area editors permits placement in another area of the
/// same module without converting the native values into operating-system clipboard text.
/// </remarks>
public sealed class AreaInstanceClipboard
{
    public AreaInstanceClipboardEntry? Content { get; private set; }

    public void Set(AreaInstanceClipboardEntry content)
    {
        ArgumentNullException.ThrowIfNull(content);
        Content = content;
    }
}
