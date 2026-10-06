using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

[TestClass]
public sealed class PaletteWorkflowTextsTests
{
    [TestMethod]
    public void TheEnglishCatalogCoversEveryIdAndFormatsWithFourArguments()
    {
        foreach (var id in Enum.GetValues<PaletteWorkflowStringId>())
            Assert.IsFalse(string.IsNullOrWhiteSpace(PaletteWorkflowTexts.English.Get(id, "a", "b", "c", "d")), id.ToString());
    }

    [TestMethod]
    public void AnIncompleteReplacementCatalogIsRefused()
    {
        var partial = new Dictionary<PaletteWorkflowStringId, string> { [PaletteWorkflowStringId.Unsorted] = "Unsorted" };

        Assert.ThrowsExactly<ArgumentException>(() => new PaletteWorkflowTexts(partial));
    }
}
