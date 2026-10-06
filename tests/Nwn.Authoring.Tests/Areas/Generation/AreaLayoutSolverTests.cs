using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Generation;

[TestClass]
public sealed class AreaLayoutSolverTests
{
    [TestMethod]
    public void SolveUsesRequestDimensionsSeedAndOverrideWithoutMutatingCallerParameters()
    {
        var tileset = CreateCompleteFlatCornerTileset();
        var parameters = new MacroLayoutParameters
        {
            Width = 16,
            Height = 16,
            SolidTerrain = "Solid",
            OpenTerrain = "Floor",
            MinRooms = 2,
            MaxRooms = 2,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 4,
            EntranceCount = 1,
            ExitCount = 1
        };
        var request = new LayoutSolveRequest(11, 12, 417, "Floor", RetryCount: 3);

        var first = AreaLayoutSolver.Solve(parameters, tileset, request);
        var second = AreaLayoutSolver.Solve(parameters, tileset, request);

        Assert.IsTrue(first.Success, first.FailureReason);
        Assert.IsTrue(second.Success, second.FailureReason);
        Assert.AreEqual(417, first.AttemptSeed);
        Assert.AreEqual(11, first.Resolved.Width);
        Assert.AreEqual(12, first.Resolved.Height);
        Assert.AreEqual("Floor", first.Parameters.OpenTerrain);
        CollectionAssert.AreEqual(
            first.Resolved.Tiles.Select(tile => (tile.TileId, tile.Orientation, tile.Height)).ToArray(),
            second.Resolved.Tiles.Select(tile => (tile.TileId, tile.Orientation, tile.Height)).ToArray());
        Assert.AreEqual(16, parameters.Width);
        Assert.AreEqual(16, parameters.Height);
        Assert.AreEqual("Floor", parameters.OpenTerrain);
    }

    [TestMethod]
    public void SolveInvokesHostProtectionProviderAndDiagnosticSink()
    {
        var tileset = CreateCompleteFlatCornerTileset();
        var parameters = new MacroLayoutParameters
        {
            SolidTerrain = "Solid",
            OpenTerrain = "Floor",
            MinRooms = 2,
            MaxRooms = 2,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 4,
            EntranceCount = 1,
            ExitCount = 1
        };
        var providerCalls = 0;
        AreaLayoutDiagnostic? diagnostic = null;
        var options = new LayoutSolveOptions
        {
            ProtectedFeatureCellsProvider = layout =>
            {
                providerCalls++;
                Assert.AreEqual(11, layout.Corners.Width);
                return Array.Empty<(int X, int Y)>();
            },
            DiagnosticSink = value => diagnostic = value
        };

        var result = AreaLayoutSolver.Solve(
            parameters,
            tileset,
            new LayoutSolveRequest(11, 11, 89, RetryCount: 1),
            options);

        Assert.IsTrue(result.Success, result.FailureReason);
        Assert.AreEqual(1, providerCalls);
        Assert.IsNotNull(diagnostic);
        Assert.AreEqual(AreaLayoutDiagnosticCode.DoorCandidatePoolUnavailable, diagnostic!.Code);
    }

    private static TilesetModel CreateCompleteFlatCornerTileset()
    {
        var model = new TilesetModel
        {
            Resref = "fixture",
            DefaultTerrain = "Solid",
            FloorTerrain = "Floor"
        };
        for (var mask = 0; mask < 16; mask++)
        {
            var corners = new string[4];
            for (var slot = 0; slot < corners.Length; slot++)
                corners[slot] = (mask & (1 << slot)) == 0 ? "Solid" : "Floor";
            model.Tiles.Add(new TileRecord
            {
                TileId = mask,
                Corners = corners,
                CornerHeights = [0, 0, 0, 0],
                Edges = ["", "", "", ""]
            });
        }
        return model;
    }
}

