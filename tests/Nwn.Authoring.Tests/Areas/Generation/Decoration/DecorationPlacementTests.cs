using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Decoration;
using Nwn.Authoring.Areas.Generation.Drafting;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Tests.Areas.Generation.Support;

namespace Nwn.Authoring.Tests.Areas.Generation.Decoration;

[TestClass]
public sealed class DecorationPlacementTests
{
    private static AreaGenerationDraft Generate(DecorationPlacementStyle style, int densityPercent, int seed = 4242)
    {
        var service = new AreaGenerationAuthoringService(GeneratorFixture.CreateTilesetSource(), GeneratorFixture.CreateCatalog(true));
        return service.Generate(new AreaGenerationSettings
        {
            ThemeKey = GeneratorFixture.ThemeKey,
            Width = 20,
            Height = 20,
            Seed = seed,
            Overrides = GeneratorFixture.CreateOverrides(style, densityPercent)
        });
    }

    [TestMethod]
    [DataRow(DecorationPlacementStyle.Spacious)]
    [DataRow(DecorationPlacementStyle.Compact)]
    public void PlacedPropsAccountForEveryProposalAndNeverOverlap(DecorationPlacementStyle style)
    {
        var draft = Generate(style, 200);

        Assert.IsTrue(draft.Result.Success, draft.Result.FailureReason);
        var report = draft.Result.DecorationPlacementReport!;
        Assert.AreEqual(report.ProposedCount,
            report.PlacedCount + report.UnsupportedCount + report.RouteConflictCount + report.OverlapCount);
        Assert.AreEqual(report.PlacedCount, draft.Result.PlannedDecorations.Count);
        var props = draft.Result.PlannedDecorations;
        Assert.IsTrue(props.Count > 0);
        for (var first = 0; first < props.Count; first++)
        for (var second = first + 1; second < props.Count; second++)
        {
            var dx = props[first].Position.X - props[second].Position.X;
            var dy = props[first].Position.Y - props[second].Position.Y;
            var required = props[first].FootprintRadius * props[first].VisualScale
                           + props[second].FootprintRadius * props[second].VisualScale;
            Assert.IsTrue(MathF.Sqrt(dx * dx + dy * dy) >= required - 0.001f,
                $"Props {first} and {second} overlap.");
        }
    }

    [TestMethod]
    public void DisablingDecorationsPlansNothingAndZeroDensityIsAccepted()
    {
        Assert.AreEqual(0, Generate(DecorationPlacementStyle.Spacious, 0).Result.PlannedDecorations.Count);
    }

    [TestMethod]
    public void EveryPlacedPropStandsOnTheOpenSurface()
    {
        var draft = Generate(DecorationPlacementStyle.Spacious, 100, seed: 77231);
        var layout = draft.Result.Resolved!;
        var surface = DecorationPlacementSafety.BuildOpenSurface(layout);

        foreach (var prop in draft.Result.PlannedDecorations)
        {
            Assert.IsTrue(
                DecorationPlacementSafety.HasSupport(
                    new System.Numerics.Vector2(prop.Position.X, prop.Position.Y), 0f, surface, layout, draft.Composition.Tileset),
                $"{prop.Resref} is not supported by an open tile.");
        }
    }

    [TestMethod]
    public void BuildingBoundsDetectOverlapAndCircleIntrusion()
    {
        var bounds = new DecorationBounds(0f, 0f, 10f, 10f);

        Assert.IsTrue(bounds.Overlaps(new DecorationBounds(9f, 9f, 20f, 20f)));
        Assert.IsFalse(bounds.Overlaps(new DecorationBounds(10f, 0f, 20f, 10f)), "Touching edges are not an overlap.");
        Assert.IsTrue(bounds.IntersectsCircle(11f, 5f, 1.5f));
        Assert.IsFalse(bounds.IntersectsCircle(12f, 5f, 1.5f));
    }

    [TestMethod]
    public void RouteWidthFollowsThePlacementStyle()
    {
        Assert.IsTrue(DecorationPlacementSafety.RouteRadius(DecorationPlacementStyle.Spacious)
                      > DecorationPlacementSafety.RouteRadius(DecorationPlacementStyle.Compact));
    }
}
