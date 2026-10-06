namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// The default grouping rule: split a display name on its first " - " and use the leading
    /// segment as its folder. Remaining segments stay together as the leaf label.
    /// </summary>
    /// <remarks>
    /// A single predictable rule keeps names that do not match it available in
    /// <see cref="CategorySection.UnsortedFolderName"/>.
    ///
    /// The separator is space-dash-space rather than a bare dash on purpose: "CZ-220 - Hangar" must
    /// group under "CZ-220", not under "CZ".
    /// </remarks>
    public static class AutomaticGrouping
    {
        public const string Separator = " - ";

        /// <summary>User-facing explanation of the rule, so it can be shown next to the selector.</summary>
        public const string Description = "Grouped by the part of the name before the first dash.";

        /// <summary>The folder a display name belongs in, or null when the name has no separator.</summary>
        public static string? GroupNameFor(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            var index = displayName.IndexOf(Separator, StringComparison.Ordinal);
            if (index <= 0)
                return null;

            var group = displayName[..index].Trim();
            return group.Length == 0 ? null : group;
        }

        /// <summary>
        /// Returns the text after the first separator, or the whole name when it has no non-empty prefix.
        /// </summary>
        public static string LeafLabelFor(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            var index = displayName.IndexOf(Separator, StringComparison.Ordinal);
            if (index <= 0)
                return displayName.Trim();

            var remainder = displayName[(index + Separator.Length)..].Trim();
            return remainder.Length == 0 ? displayName.Trim() : remainder;
        }
    }
}
