using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Tests.Categories;

[TestClass]
public sealed class CategoryMembershipHistoryTests
{
    private static CategoryMembershipEdit Edit(string resRef) =>
        new(ModuleResourceType.Nss, resRef, resRef, new[] { "First" }, new[] { "Second" });

    [TestMethod]
    public void AnEditLeavesItsStackOnlyWhenTheReplayIsCompleted()
    {
        var history = new CategoryMembershipHistory();
        history.Record(Edit("one"));

        Assert.IsTrue(history.TryPeekUndo(out var peeked));
        Assert.AreEqual("one", peeked.ResRef);
        Assert.IsTrue(history.CanUndo, "peeking must not consume the edit; a refused save retries it");
        Assert.IsFalse(history.CanRedo);

        history.CompleteUndo();
        Assert.IsFalse(history.CanUndo);
        Assert.IsTrue(history.CanRedo);

        history.CompleteRedo();
        Assert.IsTrue(history.CanUndo);
        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void ANewMoveDiscardsWhatCouldHaveBeenRedone()
    {
        var history = new CategoryMembershipHistory();
        history.Record(Edit("one"));
        history.CompleteUndo();

        history.Record(Edit("two"));

        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void InvolvesMatchesEitherStackIgnoringCase()
    {
        var history = new CategoryMembershipHistory();
        history.Record(Edit("one"));
        history.Record(Edit("two"));
        history.CompleteUndo();

        Assert.IsTrue(history.Involves(ModuleResourceType.Nss, "ONE"));
        Assert.IsTrue(history.Involves(ModuleResourceType.Nss, "two"));
        Assert.IsFalse(history.Involves(ModuleResourceType.Area, "one"));
    }

    [TestMethod]
    public void ClearingAnEmptyHistoryRaisesNothing()
    {
        var history = new CategoryMembershipHistory();
        var raised = 0;
        history.Changed += () => raised++;

        history.Clear();
        Assert.AreEqual(0, raised);

        history.Record(Edit("one"));
        history.Clear();
        Assert.AreEqual(2, raised);
        Assert.IsFalse(history.CanUndo);
    }
}
