using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Placeables;
using Nwn.Formats.TwoDa;

namespace Nwn.Authoring.Tests.Placeables;

[TestClass]
public sealed class PlaceableAppearanceCatalogReaderTests
{
    [TestMethod]
    public void ReadUsesPhysicalTablePositionAndKeepsDrawableUnlabelledRows()
    {
        var table = new TwoDaTable(["Label", "StrRef", "ModelName"]);
        table.AddRow("71", new Dictionary<string, string?>
        {
            ["Label"] = "Console", ["StrRef"] = "82", ["ModelName"] = "plc_console"
        });
        table.AddRow("83", new Dictionary<string, string?>
        {
            ["Label"] = null, ["StrRef"] = null, ["ModelName"] = "plc_unlabelled"
        });

        var options = PlaceableAppearanceCatalogReader.Read(table);

        CollectionAssert.AreEqual(new[] { 0, 1 }, options.Select(option => option.RowIndex).ToArray());
        Assert.AreEqual("Console", options[0].Label);
        Assert.AreEqual(82, options[0].StringRef);
        Assert.AreEqual("plc_unlabelled", options[1].ModelName);
        Assert.IsNull(options[1].StringRef, "Unlabelled rows use their model name and never parse StrRef.");
        Assert.IsFalse(options[1].HasLabel);
    }

    [TestMethod]
    public void ReadFromEngineCompatibleTextTreatsPrintedRowLabelsAsMetadataAndKeepsPhysicalPositions()
    {
        const string text = "2DA V2.0\r\n\r\nLabel StrRef ModelName\r\n"
            + "17 Console 82 plc_console\r\n"
            + "43 **** not-a-number plc_unlabelled\r\n"
            + "44 Reserved **** plc_reserved\r\n";
        var table = TwoDaReader.Read(Encoding.ASCII.GetBytes(text), TwoDaReadOptions.EngineCompatible);

        var options = PlaceableAppearanceCatalogReader.Read(table);

        CollectionAssert.AreEqual(new[] { 0, 1 }, options.Select(option => option.RowIndex).ToArray());
        Assert.AreEqual("Console", options[0].Label);
        Assert.AreEqual(82, options[0].StringRef);
        Assert.IsNull(options[1].Label);
        Assert.AreEqual("plc_unlabelled", options[1].ModelName);
        Assert.IsNull(options[1].StringRef, "Unlabelled rows use their model name and never parse StrRef.");
    }

    [TestMethod]
    public void ReadFiltersPlaceholderLabelsAndModelsButAllowsAValidModelWithNoLabel()
    {
        var table = new TwoDaTable(["Label", "StrRef", "ModelName"]);
        Add(table, "Console", "plc_console");
        Add(table, null, "plc_unlabelled");
        Add(table, "Reserved", "plc_reserved");
        Add(table, "Console", "unused_12");
        Add(table, "Console", "****");

        var options = PlaceableAppearanceCatalogReader.Read(table);

        CollectionAssert.AreEqual(new[] { 0, 1 }, options.Select(option => option.RowIndex).ToArray());
    }

    [TestMethod]
    public void ReadRejectsMalformedOptionalStringReference()
    {
        var table = new TwoDaTable(["Label", "StrRef", "ModelName"]);
        table.AddRow("0", new Dictionary<string, string?>
        {
            ["Label"] = "Console", ["StrRef"] = "not-a-number", ["ModelName"] = "plc_console"
        });

        Assert.ThrowsExactly<FormatException>(() => PlaceableAppearanceCatalogReader.Read(table));
    }

    [TestMethod]
    public void ReadFailsClosedWhenRequiredColumnsAreAbsent()
    {
        Assert.AreEqual(0, PlaceableAppearanceCatalogReader.Read(null).Count);
        Assert.AreEqual(0, PlaceableAppearanceCatalogReader.Read(new TwoDaTable(["Label", "StrRef"])).Count);
        Assert.AreEqual(0, PlaceableAppearanceCatalogReader.Read(new TwoDaTable(["ModelName", "StrRef"])).Count);
    }

    private static void Add(TwoDaTable table, string? label, string? model) =>
        table.AddRow("printed-row-label-is-not-the-native-id", new Dictionary<string, string?>
        {
            ["Label"] = label, ["ModelName"] = model, ["StrRef"] = null
        });
}
