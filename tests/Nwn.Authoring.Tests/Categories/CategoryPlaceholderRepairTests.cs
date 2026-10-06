using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;

namespace Nwn.Authoring.Tests.Categories;

[TestClass]
public sealed class CategoryPlaceholderRepairTests
{
    [TestMethod]
    public void MarkedPlaceholdersAreRenamedAndTheirPinsMove()
    {
        var section = new CategorySection();
        var placeholder = section.AddFolder(CategoryPlaceholderNames.For(6782));
        placeholder.IsUnresolvedPlaceholder = true;
        placeholder.AddChild("Inner");
        section.Pin(section.PathKey(placeholder.Children[0]));

        var repaired = CategoryPlaceholderRepair.Repair(section, strRef => strRef == 6782 ? "Doors/Gates" : null);

        Assert.AreEqual(1, repaired);
        Assert.AreEqual("Doors-Gates", placeholder.Name, "TLK names are sanitized like imported names.");
        Assert.IsFalse(placeholder.IsUnresolvedPlaceholder);
        CollectionAssert.AreEqual(new[] { "Doors-Gates/Inner" }, section.Pinned.ToArray());
    }

    [TestMethod]
    public void DeliberateNamesAndUnresolvableReferencesAreLeftAlone()
    {
        var section = new CategorySection();
        var deliberate = section.AddFolder("Category 7");
        var unresolved = section.AddFolder(CategoryPlaceholderNames.For(9));
        unresolved.IsUnresolvedPlaceholder = true;

        var repaired = CategoryPlaceholderRepair.Repair(section, strRef => strRef == 7 ? "Seven" : null);

        Assert.AreEqual(0, repaired);
        Assert.AreEqual("Category 7", deliberate.Name, "Only folders the importer marked are placeholders.");
        Assert.AreEqual("Category 9", unresolved.Name);
        Assert.IsTrue(unresolved.IsUnresolvedPlaceholder);
    }

    [TestMethod]
    public void PlaceholderTextRoundTrips()
    {
        Assert.IsTrue(CategoryPlaceholderNames.TryParse(CategoryPlaceholderNames.For(16810847), out var strRef));
        Assert.AreEqual(16810847u, strRef);
        Assert.IsFalse(CategoryPlaceholderNames.TryParse("Category seven", out _));
        Assert.IsFalse(CategoryPlaceholderNames.TryParse(null, out _));
    }
}
