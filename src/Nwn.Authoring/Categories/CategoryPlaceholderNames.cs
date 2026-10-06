using System.Globalization;
using System.Text.RegularExpressions;

namespace Nwn.Authoring.Categories
{
    /// <summary>
    /// The "Category N" text a palette category receives when its TLK name could not be resolved at import.
    /// </summary>
    /// <remarks>
    /// The text only carries the string reference back out of a folder already marked
    /// <see cref="CategoryFolder.IsUnresolvedPlaceholder"/>. It never decides whether a folder is a
    /// placeholder: a builder can deliberately name a folder "Category 7".
    /// </remarks>
    public static partial class CategoryPlaceholderNames
    {
        /// <summary>The placeholder for an unresolved string reference.</summary>
        public static string For(uint strRef) =>
            string.Create(CultureInfo.InvariantCulture, $"Category {strRef}");

        /// <summary>Recovers the string reference from placeholder text, or false when the text has another shape.</summary>
        public static bool TryParse(string? name, out uint strRef)
        {
            strRef = 0;
            if (name is null)
                return false;

            var match = PlaceholderPattern().Match(name);
            return match.Success &&
                   uint.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out strRef);
        }

        [GeneratedRegex(@"^Category (\d+)$")]
        private static partial Regex PlaceholderPattern();
    }
}
