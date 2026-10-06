using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;

namespace Nwn.Authoring.Tests.Categories;

[TestClass]
public sealed class CategoryHierarchyTests
{
    [TestMethod]
    public void UnsortedAndFolderCountsUseOnlyExistingMemberships()
    {
        var section = new CategorySection();
        var interiors = section.AddFolder("Interiors");
        var consoles = interiors.AddChild("Consoles");
        consoles.AddMember("item_a");
        interiors.AddMember("item_b");
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "item_a", "item_b", "item_c" };

        CollectionAssert.AreEqual(new[] { "item_c" }, section.UnsortedResRefs(existing).ToArray());
        Assert.AreEqual(2, section.CountIn(interiors, existing));
        Assert.AreEqual(1, section.CountIn(consoles, existing));
    }

    [TestMethod]
    public void NestedRenameRepathsPinsAndSiblingNamesRemainScoped()
    {
        var section = new CategorySection();
        var weapons = section.AddFolder("Weapons");
        var melee = weapons.AddChild("Melee");
        var rare = melee.AddChild("Rare");
        section.Pin(section.PathKey(rare));
        var props = section.AddFolder("Props");
        props.AddChild("Rare");

        Assert.IsTrue(section.TryRenameFolder(melee, "Blades"));

        CollectionAssert.AreEqual(new[] { "Weapons/Blades/Rare" }, section.Pinned.ToArray());
        var resolved = section.FindByPathKey("Weapons/Blades/Rare");
        Assert.IsNotNull(resolved);
        Assert.AreSame(rare, resolved);
        Assert.ThrowsExactly<ArgumentException>(() => weapons.AddChild("blades"));
    }

    [TestMethod]
    public void FolderNameRepairAndAutomaticGroupingKeepTheirRules()
    {
        Assert.AreEqual("Skin-Hide", CategoryFolder.Sanitize(" Skin/Hide "));
        Assert.IsNull(CategoryFolder.Sanitize("  "));
        Assert.IsNotNull(CategoryFolder.NameProblem("Weapons/Melee"));
        Assert.IsNull(CategoryFolder.NameProblem("Weapons"));
        Assert.AreEqual("Viscara", AutomaticGrouping.GroupNameFor("Viscara - Veles - Plaza"));
        Assert.AreEqual("Veles - Plaza", AutomaticGrouping.LeafLabelFor("Viscara - Veles - Plaza"));
        Assert.IsNull(AutomaticGrouping.GroupNameFor("CZ-220"));
    }
}
