namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// Bounds on a palette tree <see cref="ItpCategoryImporter"/> will walk before refusing it as malformed.
    /// </summary>
    /// <remarks>
    /// The defaults sit far above anything a real palette holds - the largest shipped or module palette is
    /// a few levels deep with tens of thousands of nodes - so they only ever stop a corrupt or hostile file.
    /// </remarks>
    public sealed record ItpImportLimits(int MaximumDepth, int MaximumNodes)
    {
        /// <summary>128 levels and 500,000 nodes.</summary>
        public static ItpImportLimits Default { get; } = new(128, 500_000);
    }
}
