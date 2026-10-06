#nullable disable
using Nwn.Authoring.Areas.Generation.Decoration;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Areas.Generation.Drafting
{
    public static class AreaGenerationLayoutSolver
    {
        public const int DefaultRetryCount = AreaLayoutSolver.DefaultRetryCount;

        public static LayoutSolverResult Solve(
            MacroLayoutParameters baseParameters,
            TilesetModel tileset,
            int width,
            int height,
            int seed,
            string openTerrainOverride = "",
            int retryCount = DefaultRetryCount,
            Action<AreaLayoutDiagnostic> diagnosticSink = null)
        {
            var request = new LayoutSolveRequest(width, height, seed, openTerrainOverride, retryCount);
            var options = new LayoutSolveOptions
            {
                ProtectedFeatureCellsProvider = macro => BuildProtectedFeatureCells(tileset, macro),
                DiagnosticSink = diagnosticSink
            };
            return AreaLayoutSolver.Solve(baseParameters, tileset, request, options);
        }

        private static IReadOnlyCollection<(int X, int Y)> BuildProtectedFeatureCells(TilesetModel tileset, MacroLayout macro)
        {
            if (macro.FeatureTiles.Count == 0 || macro.Rooms.Count == 0)
                return Array.Empty<(int X, int Y)>();

            var surfaceLayout = new ResolvedLayout
            {
                Width = macro.Corners.Width,
                Height = macro.Corners.Height,
                Rooms = macro.Rooms,
                Transitions = macro.Transitions,
                CornerTerrains = macro.Corners,
                OpenTerrain = macro.OpenTerrain,
                SecondaryOpenTerrain = macro.SecondaryOpenTerrain,
                Crossers = macro.Crossers,
                StampedStructureTiles = macro.StampedOpenSetPieceFootprints.SelectMany(footprint => footprint).ToHashSet()
            };
            var surface = Decoration.DecorationPlacementSafety.BuildOpenSurface(surfaceLayout);
            var protectedCells = new HashSet<(int X, int Y)>();
            foreach (var route in Decoration.DecorationPlacementSafety.BuildRoutes(surfaceLayout, surface, string.Empty))
            {
                protectedCells.Add(((int)MathF.Floor(route.Start.X / 10), (int)MathF.Floor(route.Start.Y / 10)));
                protectedCells.Add(((int)MathF.Floor(route.End.X / 10), (int)MathF.Floor(route.End.Y / 10)));
            }
            return protectedCells;
        }
    }
}
