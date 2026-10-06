namespace Nwn.Authoring.Fields
{
    /// <summary>A titled group of fields rendered as a section.</summary>
    public sealed class FieldGroup
    {
        public required string Title { get; init; }
        public required IReadOnlyList<FieldDescriptor> Fields { get; init; }

        /// <summary>
        /// Which editor tab this group appears on. Groups sharing a tab keep their declared order
        /// within it, and tabs appear in the order their first group is declared.
        /// </summary>
        /// <remarks>
        /// Blank means the editor shows one unnamed page - the shape every schema had before
        /// placeables needed more than a single scroll, and still the right shape for the types
        /// whose fields fit one.
        /// </remarks>
        public string Tab { get; init; } = string.Empty;
    }
}
