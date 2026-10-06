#nullable disable
using System;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Areas.Generation
{
    /// <summary>Produces a resolved layout using only the supplied layout policy and native tileset data.</summary>
    public static class AreaLayoutSolver
    {
        public const int DefaultRetryCount = 6;

        public static LayoutSolverResult Solve(
            MacroLayoutParameters baseParameters,
            TilesetModel tileset,
            LayoutSolveRequest request,
            LayoutSolveOptions options = null)
        {
            if (baseParameters == null) throw new ArgumentNullException(nameof(baseParameters));
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));
            if (request == null) throw new ArgumentNullException(nameof(request));
            options ??= new LayoutSolveOptions();

            var lastFailure = "no attempts made";
            for (var attempt = 0; attempt < request.RetryCount; attempt++)
            {
                var trySeed = request.Seed + attempt;
                var random = new System.Random(trySeed);
                var parameters = baseParameters.Clone();
                parameters.Width = request.Width;
                parameters.Height = request.Height;
                if (string.IsNullOrEmpty(parameters.SolidTerrain))
                    parameters.SolidTerrain = tileset.DefaultTerrain;
                parameters.OpenTerrain = string.IsNullOrEmpty(request.OpenTerrainOverride)
                    ? tileset.FloorTerrain
                    : request.OpenTerrainOverride;

                MacroLayout macro;
                try
                {
                    macro = MacroLayoutGenerator.Generate(parameters, random, tileset);
                    macro.Seed = trySeed;
                }
                catch (InvalidOperationException ex)
                {
                    lastFailure = ex.Message;
                    continue;
                }

                if (TileResolver.TryResolve(
                        tileset,
                        macro,
                        random,
                        out var resolved,
                        out var failureReason,
                        options.ProtectedFeatureCellsProvider?.Invoke(macro),
                        options.DiagnosticSink))
                {
                    return new LayoutSolverResult
                    {
                        Success = true,
                        Layout = macro,
                        Parameters = parameters,
                        Resolved = resolved,
                        AttemptSeed = trySeed
                    };
                }

                lastFailure = failureReason;
            }

            return new LayoutSolverResult
            {
                Success = false,
                FailureReason = lastFailure,
                AttemptSeed = request.Seed
            };
        }
    }
}


