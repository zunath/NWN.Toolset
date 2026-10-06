using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Doors;
using Nwn.Formats.TwoDa;

namespace Nwn.Authoring.Tests.Doors;

[TestClass]
public sealed class DoorAppearanceCatalogReaderTests
{
    [TestMethod]
    public void Read_UsesPhysicalRowsAndPreservesNativeMetadata()
    {
        var generic = new TwoDaTable(["Label", "ModelName", "VisibleModel", "Name", "StrRef"]);
        generic.AddRow("124", new Dictionary<string, string?>
        {
            ["Label"] = "Wood_strong", ["ModelName"] = "t_door01", ["VisibleModel"] = "1",
            ["Name"] = "****", ["StrRef"] = "63750"
        });
        generic.AddRow("1361", new Dictionary<string, string?>
        {
            ["Label"] = "Jabba_palace_door", ["ModelName"] = "ttd_door009", ["VisibleModel"] = "0",
            ["Name"] = "14445", ["StrRef"] = "5349"
        });

        var specific = new TwoDaTable(["Label", "Model", "StringRefGame", "VisibleModel"]);
        specific.AddRow("0", new Dictionary<string, string?>
        {
            ["Label"] = "Generic", ["Model"] = "door_zero", ["StringRefGame"] = "66717", ["VisibleModel"] = "1"
        });
        specific.AddRow("1362", new Dictionary<string, string?>
        {
            ["Label"] = "Astroport_door", ["Model"] = "ttd_door_010", ["StringRefGame"] = "63526",
            ["VisibleModel"] = "1"
        });

        var options = DoorAppearanceCatalogReader.Read(specific, generic);

        CollectionAssert.AreEqual(new long[] { 0, 1, 0, 1 }, options.Select(option => option.Id).ToArray());
        Assert.AreEqual(DoorAppearanceKind.Generic, options[0].Kind);
        Assert.AreEqual("Wood_strong", options[0].InternalLabel);
        Assert.AreEqual("t_door01", options[0].Model);
        Assert.AreEqual(63750, options[0].StringRef);
        Assert.AreEqual(14445, options[1].StringRef, "Name takes precedence over StrRef.");
        Assert.IsFalse(options[1].VisibleModel);
        Assert.AreEqual(DoorAppearanceKind.Specific, options[2].Kind);
        Assert.AreEqual("Generic", options[2].InternalLabel);
        Assert.AreEqual("door_zero", options[2].Model);
        Assert.AreEqual(DoorAppearanceKind.Specific, options[3].Kind);
        Assert.AreEqual("ttd_door_010", options[3].Model);
    }

    [TestMethod]
    public void Read_ExcludesPlaceholdersAndRowsMissingRequiredMetadata()
    {
        var generic = new TwoDaTable(["Label", "ModelName", "VisibleModel"]);
        Add(generic, "****", "door_a", "1");
        Add(generic, "unused_12", "door_b", "1");
        Add(generic, "Null_01", "door_c", "1");
        Add(generic, "Door", "****", "1");
        Add(generic, "Door", "door_d", "unknown");
        Add(generic, "Door", "door_e", null);
        Add(generic, "Door", "door_f", "0");

        var specific = new TwoDaTable(["Label", "Model", "StringRefGame", "VisibleModel"]);
        specific.AddRow("0", new Dictionary<string, string?>
        {
            ["Label"] = "Generic", ["Model"] = "door_zero", ["StringRefGame"] = "1", ["VisibleModel"] = "1"
        });
        specific.AddRow("1", new Dictionary<string, string?>
        {
            ["Label"] = "Reserved Door", ["Model"] = "door_reserved", ["StringRefGame"] = "1", ["VisibleModel"] = "1"
        });
        specific.AddRow("2", new Dictionary<string, string?>
        {
            ["Label"] = "Door", ["Model"] = "door_valid", ["StringRefGame"] = "****", ["VisibleModel"] = "1"
        });
        specific.AddRow("3", new Dictionary<string, string?>
        {
            ["Label"] = "Door", ["Model"] = "door_valid", ["StringRefGame"] = "1", ["VisibleModel"] = "no"
        });
        specific.AddRow("4", new Dictionary<string, string?>
        {
            ["Label"] = "Door", ["Model"] = "door_valid", ["StringRefGame"] = "1", ["VisibleModel"] = "2"
        });

        var options = DoorAppearanceCatalogReader.Read(specific, generic);

        CollectionAssert.AreEqual(new long[] { 6, 0, 4 }, options.Select(option => option.Id).ToArray());
        Assert.IsFalse(options[0].VisibleModel);
        Assert.AreEqual(DoorAppearanceKind.Specific, options[1].Kind);
        Assert.AreEqual("door_zero", options[1].Model);
        Assert.IsTrue(options[1].VisibleModel);
        Assert.AreEqual(DoorAppearanceKind.Specific, options[2].Kind);
        Assert.AreEqual("door_valid", options[2].Model);
        Assert.IsEmpty(DoorAppearanceCatalogReader.Read(
            new TwoDaTable(["Label", "Model", "VisibleModel"]), null));
    }

    private static void Add(TwoDaTable table, string label, string model, string? visibleModel) =>
        table.AddRow(label, new Dictionary<string, string?>
        {
            ["Label"] = label, ["ModelName"] = model, ["VisibleModel"] = visibleModel
        });
}
