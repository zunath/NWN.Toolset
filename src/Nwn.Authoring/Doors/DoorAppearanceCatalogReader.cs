using System.Globalization;
using Nwn.Authoring.Resources;
using Nwn.Formats.TwoDa;

namespace Nwn.Authoring.Doors;

/// <summary>Reads host-supplied native door tables into validated, positional appearance options.</summary>
public static class DoorAppearanceCatalogReader
{
    private const string LabelColumn = "Label";
    private const string SpecificModelColumn = "Model";
    private const string GenericModelColumn = "ModelName";
    private const string VisibleModelColumn = "VisibleModel";
    private const string SpecificStringRefColumn = "StringRefGame";
    private const string GenericNameStringRefColumn = "Name";
    private const string GenericStringRefColumn = "StrRef";

    /// <summary>Returns generic rows followed by specific rows, each retaining its physical row index.</summary>
    public static IReadOnlyList<DoorAppearanceOption> Read(
        TwoDaTable? doortypes,
        TwoDaTable? genericdoors)
    {
        var result = new List<DoorAppearanceOption>();
        ReadGeneric(genericdoors, result);
        ReadSpecific(doortypes, result);
        return result.AsReadOnly();
    }

    private static void ReadGeneric(TwoDaTable? table, ICollection<DoorAppearanceOption> result)
    {
        if (!HasColumns(table, LabelColumn, GenericModelColumn, VisibleModelColumn))
            return;

        for (var row = 0; row < table!.Rows.Count; row++)
        {
            var label = table.GetValue(row, LabelColumn);
            var model = table.GetValue(row, GenericModelColumn);
            var visibleModel = ReadInteger(table.GetValue(row, VisibleModelColumn));
            if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                !TwoDaChoicePolicy.IsSelectableLabel(model) ||
                visibleModel is null)
            {
                continue;
            }

            var stringRef = ReadOptionalInteger(table, row, GenericNameStringRefColumn) ??
                            ReadOptionalInteger(table, row, GenericStringRefColumn);
            result.Add(new DoorAppearanceOption(
                DoorAppearanceKind.Generic,
                row,
                label!,
                model!,
                visibleModel.Value != 0,
                stringRef));
        }
    }

    private static void ReadSpecific(TwoDaTable? table, ICollection<DoorAppearanceOption> result)
    {
        if (!HasColumns(table, LabelColumn, SpecificModelColumn, SpecificStringRefColumn, VisibleModelColumn))
            return;

        for (var row = 0; row < table!.Rows.Count; row++)
        {
            var label = table.GetValue(row, LabelColumn);
            var model = table.GetValue(row, SpecificModelColumn);
            var stringRefText = table.GetValue(row, SpecificStringRefColumn);
            var visibleModel = ReadInteger(table.GetValue(row, VisibleModelColumn));
            if (!TwoDaChoicePolicy.IsSelectableLabel(label) ||
                !TwoDaChoicePolicy.IsSelectableLabel(model) ||
                !TwoDaChoicePolicy.IsSelectableLabel(stringRefText) ||
                visibleModel is null)
            {
                continue;
            }

            result.Add(new DoorAppearanceOption(
                DoorAppearanceKind.Specific,
                row,
                label!,
                model!,
                visibleModel.Value != 0,
                ReadInteger(stringRefText)));
        }
    }

    private static int? ReadOptionalInteger(TwoDaTable table, int row, string column) =>
        table.Columns.Contains(column, StringComparer.OrdinalIgnoreCase)
            ? ReadInteger(table.GetValue(row, column))
            : null;

    private static bool HasColumns(TwoDaTable? table, params string[] requiredColumns) =>
        table is not null && requiredColumns.All(column =>
            table.Columns.Contains(column, StringComparer.OrdinalIgnoreCase));

    private static int? ReadInteger(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
}
