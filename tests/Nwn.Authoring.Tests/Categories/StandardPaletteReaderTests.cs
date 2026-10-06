using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;
using Nwn.Formats.Gff;

namespace Nwn.Authoring.Tests.Categories;

[TestClass]
public sealed class StandardPaletteReaderTests
{
    [TestMethod]
    public void MembershipIsNarrowedToBlueprintsTheHostCanResolve()
    {
        var category = new GffStruct(0);
        category.Add(GffField.Dword("STRREF", 100));
        category.Add(GffField.List("LIST", new[] { Leaf("present", 200), Leaf("cut_content", 201) }));
        var root = new GffStruct();
        root.Add(GffField.List("MAIN", new[] { category }));
        var bytes = GffWriter.Write(new GffDocument { FileType = "ITP ", Root = root });

        var palette = StandardPaletteReader.Read(
            bytes,
            resRef => resRef == "present",
            strRef => strRef switch { 100 => "Containers", 200 => "Crate", _ => null });

        Assert.IsFalse(palette.IsEmpty);
        CollectionAssert.AreEquivalent(new[] { "present" }, palette.ResRefs.ToArray());
        Assert.AreEqual("Containers", palette.Section.Folders[0].Name);
        CollectionAssert.AreEquivalent(
            new[] { "present", "cut_content" },
            palette.Section.Folders[0].Members.ToArray(),
            "The section keeps the palette's arrangement; ResRefs decides which tiles exist.");
        Assert.AreEqual("Crate", palette.Names["present"]);
    }

    [TestMethod]
    public void UnreadableBytesAreAFormatError()
    {
        Assert.ThrowsExactly<FormatException>(() => StandardPaletteReader.Read(new byte[] { 1, 2, 3 }, _ => true));
    }

    [TestMethod]
    public void TheEmptyPaletteHasNothingToShow()
    {
        Assert.IsTrue(StandardPalette.Empty.IsEmpty);
    }

    private static GffStruct Leaf(string resRef, uint strRef)
    {
        var leaf = new GffStruct(0);
        leaf.Add(GffField.Dword("STRREF", strRef));
        leaf.Add(GffField.ResRef("RESREF", resRef));
        return leaf;
    }
}
