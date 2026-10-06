using System.Globalization;
using Avalonia.Data.Converters;
using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Decoration;
using Nwn.Authoring.Areas.Generation.Preview;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>Provides host-localized labels for enum-backed Area Generator choices.</summary>
public sealed class AreaGeneratorOptionLabelConverter : IValueConverter
{
    private readonly AreaGeneratorTexts _texts;

    public AreaGeneratorOptionLabelConverter(AreaGeneratorTexts texts)
    {
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
    }

    /// <summary>Maps generator enum choices to builder-facing labels, including the two decoration placement styles.</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            AreaPreviewMode.Schematic => _texts.Get(AreaGeneratorStringId.PreviewModeSchematic),
            AreaPreviewMode.MapGraphics => _texts.Get(AreaGeneratorStringId.PreviewModeMapGraphics),
            DecorationPlacementStyle.Spacious => _texts.Get(AreaGeneratorStringId.PlacementSpacious),
            DecorationPlacementStyle.Compact => _texts.Get(AreaGeneratorStringId.PlacementCompact),
            DungeonLayoutStyle.RoomsAndCorridors => _texts.Get(AreaGeneratorStringId.StyleRoomsAndCorridors),
            DungeonLayoutStyle.OrganicCave => _texts.Get(AreaGeneratorStringId.StyleOrganicCave),
            DungeonLayoutStyle.Warren => _texts.Get(AreaGeneratorStringId.StyleWarren),
            DungeonLayoutStyle.PackedRooms => _texts.Get(AreaGeneratorStringId.StylePackedRooms),
            DungeonLayoutStyle.Labyrinth => _texts.Get(AreaGeneratorStringId.StyleLabyrinth),
            _ => value?.ToString() ?? string.Empty
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
