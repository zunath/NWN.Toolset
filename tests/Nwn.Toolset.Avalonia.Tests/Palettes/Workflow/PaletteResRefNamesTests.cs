using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Palettes.Workflow;

[TestClass]
public sealed class PaletteResRefNamesTests
{
    [TestMethod]
    [DataRow("Big Crate", "big_crate")]
    [DataRow("  Crate -- Large__Red  ", "crate_large_red")]
    [DataRow("Ölfass 2", "lfass_2")]
    [DataRow("!!!", "")]
    [DataRow("An Extremely Long Blueprint Name", "an_extremely_lon")]
    public void DisplayNamesReduceToLegalResRefs(string name, string expected)
    {
        Assert.AreEqual(expected, PaletteResRefNames.FromDisplayName(name));
    }
}
