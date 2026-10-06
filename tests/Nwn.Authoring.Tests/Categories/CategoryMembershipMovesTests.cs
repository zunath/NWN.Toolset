using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;

namespace Nwn.Authoring.Tests.Categories;

[TestClass]
public sealed class CategoryMembershipMovesTests
{
    [TestMethod]
    public void AMoveTakesTheResourceOutOfEveryFolderAndIntoExactlyOne()
    {
        var section = new CategorySection();
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        var nested = first.AddChild("Nested");
        first.AddMember("one");
        nested.AddMember("one");

        CollectionAssert.AreEqual(
            new[] { "First", "First/Nested" },
            CategoryMembershipMoves.FolderPathsContaining(section, "one").ToArray());

        Assert.IsTrue(CategoryMembershipMoves.MoveTo(section, "one", second));
        CollectionAssert.AreEqual(new[] { "Second" }, section.FoldersContaining("one").Select(section.PathKey).ToArray());

        Assert.IsTrue(CategoryMembershipMoves.MoveTo(section, "one", null));
        Assert.IsFalse(section.FoldersContaining("one").Any());
        Assert.IsFalse(CategoryMembershipMoves.MoveTo(section, "one", null), "nothing changed the second time");
    }

    [TestMethod]
    public void ResolvingReportsTheFirstFolderThatNoLongerExists()
    {
        var section = new CategorySection();
        section.AddFolder("First").AddChild("Nested");

        Assert.IsTrue(CategoryMembershipMoves.TryResolve(section, new[] { "First/Nested" }, out var folders, out var missing));
        Assert.AreEqual(1, folders.Count);
        Assert.IsNull(missing);

        Assert.IsFalse(CategoryMembershipMoves.TryResolve(section, new[] { "First", "Gone", "Also Gone" }, out folders, out missing));
        Assert.AreEqual(0, folders.Count);
        Assert.AreEqual("Gone", missing);
    }

    [TestMethod]
    public void ApplyRestoresSeveralMembershipsAtOnce()
    {
        var section = new CategorySection();
        var first = section.AddFolder("First");
        var second = section.AddFolder("Second");
        var third = section.AddFolder("Third");
        second.AddMember("one");

        CategoryMembershipMoves.Apply(section, "one", new[] { first, third });

        CollectionAssert.AreEqual(new[] { "First", "Third" }, section.FoldersContaining("one").Select(folder => folder.Name).ToArray());
    }
}
