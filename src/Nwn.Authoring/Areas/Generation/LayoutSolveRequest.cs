#nullable enable
namespace Nwn.Authoring.Areas.Generation
{
    public sealed record LayoutSolveRequest(
        int Width,
        int Height,
        int Seed,
        string OpenTerrainOverride = "",
        int RetryCount = AreaLayoutSolver.DefaultRetryCount);
}

