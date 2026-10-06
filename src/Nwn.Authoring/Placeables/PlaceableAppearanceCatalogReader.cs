using System.Globalization;
using Nwn.Authoring.Resources;
using Nwn.Formats.TwoDa;

namespace Nwn.Authoring.Placeables;

/// <summary>Extracts drawable placeable appearance rows without applying host display policy.</summary>
public static class PlaceableAppearanceCatalogReader
{
    private const string LabelColumn = "Label";
    private const string StringRefColumn = "StrRef";
    private const string ModelNameColumn = "ModelName";

    /// <summary>Returns drawable rows using their physical table positions as native appearance IDs.</summary>
    public static IReadOnlyList<PlaceableAppearanceOption> Read(TwoDaTable? table)
    {
        if (table is null || !HasColumn(table, LabelColumn) || !HasColumn(table, ModelNameColumn))
            return Array.Empty<PlaceableAppearanceOption>();

        var options = new List<PlaceableAppearanceOption>();
        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var modelName = table.GetValue(rowIndex, ModelNameColumn);
            if (!TwoDaChoicePolicy.IsSelectableLabel(modelName))
                continue;

            var label = table.GetValue(rowIndex, LabelColumn);
            if (!string.IsNullOrWhiteSpace(label) && !TwoDaChoicePolicy.IsSelectableLabel(label))
                continue;

            options.Add(new PlaceableAppearanceOption(
                rowIndex,
                modelName!,
                label,
                string.IsNullOrWhiteSpace(label) ? null : ReadOptionalInteger(table, rowIndex, StringRefColumn)));
        }

        return options.AsReadOnly();
    }

    private static bool HasColumn(TwoDaTable table, string column) =>
        table.Columns.Contains(column, StringComparer.OrdinalIgnoreCase);

    private static int? ReadOptionalInteger(TwoDaTable table, int rowIndex, string column)
    {
        if (!HasColumn(table, column))
            return null;

        var raw = table.GetValue(rowIndex, column);
        if (raw is null)
            return null;
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;
        throw new FormatException($"Placeable appearance row {rowIndex} has an invalid StrRef value.");
    }
}
