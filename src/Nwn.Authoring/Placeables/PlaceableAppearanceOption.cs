namespace Nwn.Authoring.Placeables;

/// <summary>A selectable physical row from the native placeables appearance table.</summary>
public sealed record PlaceableAppearanceOption(
    int RowIndex,
    string ModelName,
    string? Label,
    int? StringRef)
{
    public bool HasLabel => !string.IsNullOrWhiteSpace(Label);
}
