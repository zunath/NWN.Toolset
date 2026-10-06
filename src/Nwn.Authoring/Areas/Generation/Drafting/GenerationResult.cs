#nullable disable
using Nwn.Authoring.Areas.Generation.Decoration;
using Nwn.Authoring.Areas.Generation.Tilesets;

namespace Nwn.Authoring.Areas.Generation.Drafting
{
    /// <summary>Outcome of one <see cref="GenerationEngine.Generate"/> call.</summary>
    public sealed class GenerationResult
    {
        public bool Success { get; init; }
        public MacroLayout Layout { get; init; }
        public MacroLayoutParameters Parameters { get; init; }
        public TilesetModel Tileset { get; init; }
        public ResolvedLayout Resolved { get; init; }
        public int AttemptSeed { get; init; }
        public string FailureReason { get; init; }
        public IReadOnlyList<PlannedDecoration> PlannedDecorations { get; init; } = Array.Empty<PlannedDecoration>();
        public int PlannedDecorationCount => PlannedDecorations.Count;
        public DecorationPlacementReport DecorationPlacementReport { get; init; }
    }
}
