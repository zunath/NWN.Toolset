using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Explorer;

[TestClass]
public sealed class ModuleExplorerTextsTests
{
    [TestMethod]
    public void TheEnglishCatalogCoversEveryIdAndFormatsWithFourArguments()
    {
        foreach (var id in Enum.GetValues<ModuleExplorerStringId>())
            Assert.IsFalse(string.IsNullOrEmpty(ModuleExplorerTexts.English.Get(id, "a", "b", "c", "d")), id.ToString());
    }

    [TestMethod]
    public void AnIncompleteReplacementCatalogIsRefused()
    {
        var partial = new Dictionary<ModuleExplorerStringId, string> { [ModuleExplorerStringId.Unsorted] = "Unsorted" };

        Assert.ThrowsExactly<ArgumentException>(() => new ModuleExplorerTexts(partial));
    }
}
