// SPDX-License-Identifier: MIT
using System.Globalization;

namespace Nwn.Authoring.Appearances;

/// <summary>NWN body-part and equipment MDL naming.</summary>
internal static class ModelPartNames
{
    /// <summary>The default human mannequin skeletons used to dress an item for preview.</summary>
    internal const string MaleMannequinPrefix = "pmh0";

    internal const string FemaleMannequinPrefix = "pfh0";

    /// <summary>baseitems.2da's ItemClass for a cloak.</summary>
    internal const string CloakItemClass = "cloak";

    internal const string CloakMarker = "_cloak_";

    /// <summary>Body-part naming: <c>{prefix}_{partType}{number:D3}</c>, e.g. <c>pmh0_chest001</c>.</summary>
    internal static string Body(string prefix, string partType, int number) =>
        string.Create(CultureInfo.InvariantCulture, $"{prefix}_{partType}{number:D3}");

    /// <summary>Cloak naming, which spells an underscore before the number: <c>pmh0_cloak_001</c>.</summary>
    internal static string Cloak(string prefix, int number) =>
        string.Create(CultureInfo.InvariantCulture, $"{prefix}{CloakMarker}{number:D3}");

    /// <summary>The segmented-body prefix <c>p{gender}{race}{phenotype}</c>, e.g. <c>pmh0</c>.</summary>
    internal static string Creature(bool female, char race, int phenotype) =>
        string.Create(CultureInfo.InvariantCulture, $"p{(female ? 'f' : 'm')}{char.ToLowerInvariant(race)}{phenotype}");

    /// <summary>Simple-item model naming: <c>{itemClass}_{number:D3}</c>.</summary>
    internal static string Item(string itemClass, int number) =>
        string.Create(CultureInfo.InvariantCulture, $"{itemClass}_{number:D3}");

    /// <summary>Composite-item part naming: <c>{itemClass}_{b|m|t}_{number:D3}</c>.</summary>
    internal static string ItemSegment(string itemClass, char segment, int number) =>
        string.Create(CultureInfo.InvariantCulture, $"{itemClass}_{segment}_{number:D3}");

    /// <summary>Re-targets a cloak resref (or texture) to the wearer's body prefix, keeping its trailing <c>_cloak_NNN</c>.</summary>
    internal static string? RewriteCloakPrefix(string? resRef, string wearerPrefix)
    {
        var marker = resRef?.IndexOf(CloakMarker, StringComparison.OrdinalIgnoreCase) ?? -1;
        return marker >= 0 ? wearerPrefix + resRef![marker..] : resRef;
    }
}
