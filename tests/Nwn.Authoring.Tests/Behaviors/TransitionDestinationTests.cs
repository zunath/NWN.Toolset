using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Behaviors;

namespace Nwn.Authoring.Tests.Behaviors;

[TestClass]
public sealed class TransitionDestinationTests
{
    [TestMethod]
    public void TheDestinationFlagsNameADoorAWaypointOrNothing()
    {
        Assert.AreEqual(BehaviorTagScope.Door, TransitionDestinationFlags.ScopeOf(1));
        Assert.AreEqual(BehaviorTagScope.Waypoint, TransitionDestinationFlags.ScopeOf(2));
        Assert.AreEqual(BehaviorTagScope.None, TransitionDestinationFlags.ScopeOf(0));
        Assert.AreEqual(BehaviorTagScope.None, TransitionDestinationFlags.ScopeOf(3));
        Assert.AreEqual(BehaviorTagScope.None, TransitionDestinationFlags.ScopeOf(null));
    }

    [TestMethod]
    public void AResultIsGoodOnlyWhenItIsResolvedOrDeliberatelyNone()
    {
        Assert.IsTrue(TransitionDestinationResult.Resolved("door in area").IsGood);
        Assert.IsTrue(TransitionDestinationResult.TypeNone.IsGood);
        Assert.IsFalse(TransitionDestinationResult.NotFound.IsGood);
        Assert.IsFalse(TransitionDestinationResult.Ambiguous(2).IsGood);
        Assert.IsFalse(TransitionDestinationResult.WrongType(BehaviorTagScope.Door).IsGood);
        Assert.IsFalse(TransitionDestinationResult.CatalogIncomplete.IsGood);
        Assert.IsFalse(TransitionDestinationResult.TypeUnset.IsGood);
    }

    [TestMethod]
    public void TheFactoriesCarryTheDetailsEachStatusIsDescribedWith()
    {
        Assert.AreEqual(TransitionDestinationStatus.NotFound, TransitionDestinationResult.FromLocation(null).Status);
        var located = TransitionDestinationResult.FromLocation("door in area");
        Assert.AreEqual(TransitionDestinationStatus.Resolved, located.Status);
        Assert.AreEqual("door in area", located.Description);
        Assert.AreEqual(2, TransitionDestinationResult.Ambiguous(2).MatchCount);
        Assert.AreEqual(BehaviorTagScope.Door, TransitionDestinationResult.WrongType(BehaviorTagScope.Door).FoundAs);
    }
}
