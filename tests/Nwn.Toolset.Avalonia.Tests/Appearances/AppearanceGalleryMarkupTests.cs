using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Appearances;

/// <summary>Loading and placeholder rules of the shared appearance gallery markup.</summary>
[TestClass]
public sealed class AppearanceGalleryMarkupTests
{
    [TestMethod]
    public void TilesLoadPreviewsAsTheyScrollIntoViewAndReplaceThePlaceholder()
    {
        var view = ToolsetSourceFiles.Read("Appearances", "Views", "AppearanceGalleryView.axaml");

        StringAssert.Contains(view, "<controls:VirtualizingWrapPanel />");
        StringAssert.Contains(view, "Loaded=\"OnTileLoaded\"",
            "appearance previews follow the palette's viewport-driven loading pattern");
        StringAssert.Contains(view, "IsVisible=\"{Binding !HasPreview}\"",
            "the letter is only a temporary placeholder and must not remain behind real artwork");
        StringAssert.Contains(view, "IsVisible=\"{Binding HasPreview}\"",
            "the rendered model replaces rather than overlays the fallback letter");
    }
}
