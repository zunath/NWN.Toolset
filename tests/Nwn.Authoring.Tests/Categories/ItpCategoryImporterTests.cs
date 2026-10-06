using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Documents.Native;
using Nwn.Formats.Gff;

namespace Nwn.Authoring.Tests.Categories;

[TestClass]
public sealed class ItpCategoryImporterTests
{
    private const string RealisticPaletteJson = """
        {
          "__data_type": "ITP ",
          "MAIN": { "type": "list", "value": [
            { "__struct_id": 0, "LIST": { "type": "list", "value": [
              { "__struct_id": 0,
                "STRREF": { "type": "dword", "value": 6688 },
                "LIST": { "type": "list", "value": [
                  { "__struct_id": 0,
                    "NAME": { "type": "cexostring", "value": "Jump to Creature" },
                    "RESREF": { "type": "resref", "value": "_mdrn_dt_jumpto" } }
                ] } },
              { "__struct_id": 0,
                "NAME": { "type": "cexostring", "value": "Skin/Hide" },
                "LIST": { "type": "list", "value": [
                  { "__struct_id": 0,
                    "STRREF": { "type": "dword", "value": 77 },
                    "RESREF": { "type": "resref", "value": "door_a" } },
                  { "__struct_id": 0,
                    "DELETE_ME": { "type": "byte", "value": 1 },
                    "RESREF": { "type": "resref", "value": "door_gone" } },
                  { "__struct_id": 0,
                    "NAME": { "type": "cexostring", "value": "Narrow" },
                    "LIST": { "type": "list", "value": [
                      { "__struct_id": 0, "RESREF": { "type": "resref", "value": " door_b " } }
                    ] } },
                  { "__struct_id": 0,
                    "NAME": { "type": "cexostring", "value": "Hollow" },
                    "LIST": { "type": "list", "value": [] } }
                ] } }
            ] } }
          ] }
        }
        """;

    [TestMethod]
    public void WrappersAreHoistedPlaceholdersAreMarkedAndEmptyOrDeletedNodesAreDropped()
    {
        var section = ItpCategoryImporter.Import(Parse(RealisticPaletteJson), out var names);

        CollectionAssert.AreEqual(
            new[] { "Category 6688", "Skin-Hide" },
            section.Folders.Select(folder => folder.Name).ToArray(),
            "The unnamed root wrapper is hoisted, and a separator in a base-game name is sanitized.");
        Assert.IsTrue(section.Folders[0].IsUnresolvedPlaceholder);
        Assert.IsFalse(section.Folders[1].IsUnresolvedPlaceholder);

        var doors = section.Folders[1];
        CollectionAssert.AreEqual(new[] { "door_a" }, doors.Members.ToArray());
        CollectionAssert.AreEqual(new[] { "Narrow" }, doors.Children.Select(child => child.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "door_b" }, doors.Children[0].Members.ToArray());
        Assert.AreEqual("Jump to Creature", names["_mdrn_dt_jumpto"]);
        Assert.IsFalse(names.ContainsKey("door_gone"));
        Assert.AreEqual("Category 77", names["door_a"], "A leaf name falls back to the same placeholder text.");
    }

    [TestMethod]
    public void TlkResolutionNamesStrRefCategoriesAndLeaves()
    {
        var section = ItpCategoryImporter.Import(
            Parse(RealisticPaletteJson),
            out var names,
            strRef => strRef switch { 6688 => "Doors", 77 => "Oak Door", _ => null });

        Assert.AreEqual("Doors", section.Folders[0].Name);
        Assert.IsFalse(section.Folders[0].IsUnresolvedPlaceholder);
        Assert.AreEqual("Oak Door", names["door_a"]);
    }

    [TestMethod]
    public void NativeDocumentsImportTheSameTreeAsJsonDocuments()
    {
        var root = new GffStruct();
        root.Add(GffField.List("MAIN", new[]
        {
            Category("Furniture", Leaf("chair"), Category("Tables", Leaf("table"))),
            Leaf("loose")
        }));
        var bytes = GffWriter.Write(new GffDocument { FileType = "ITP ", Root = root });

        var section = ItpCategoryImporter.Import(GffReader.Read(bytes), out var names);

        Assert.AreEqual(1, section.Folders.Count);
        CollectionAssert.AreEqual(new[] { "chair" }, section.Folders[0].Members.ToArray());
        CollectionAssert.AreEqual(new[] { "table" }, section.Find("Furniture", "Tables")!.Members.ToArray());
        Assert.AreEqual("chair", names["chair"]);
    }

    [TestMethod]
    public void NativeDocumentsOfAnotherTypeAreRefused()
    {
        var document = new GffDocument { FileType = "UTP ", Root = new GffStruct() };

        Assert.ThrowsExactly<FormatException>(() => ItpCategoryImporter.Import(document, out _));
    }

    [TestMethod]
    public void TreesBeyondTheLimitsAreRefused()
    {
        var document = Parse(RealisticPaletteJson);

        Assert.ThrowsExactly<FormatException>(() =>
            ItpCategoryImporter.Import(document, out _, limits: new ItpImportLimits(1, 1000)));
        Assert.ThrowsExactly<FormatException>(() =>
            ItpCategoryImporter.Import(document, out _, limits: new ItpImportLimits(128, 3)));
    }

    private static ItpDocument Parse(string json) => ItpDocument.Parse(Encoding.UTF8.GetBytes(json));

    private static GffStruct Leaf(string resRef)
    {
        var leaf = new GffStruct(0);
        leaf.Add(GffField.String("NAME", resRef));
        leaf.Add(GffField.ResRef("RESREF", resRef));
        return leaf;
    }

    private static GffStruct Category(string name, params GffStruct[] children)
    {
        var category = new GffStruct(0);
        category.Add(GffField.String("NAME", name));
        category.Add(GffField.List("LIST", children));
        return category;
    }
}
