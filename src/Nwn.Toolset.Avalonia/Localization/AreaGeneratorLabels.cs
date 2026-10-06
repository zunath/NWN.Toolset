namespace Nwn.Toolset.Avalonia.Localization;

/// <summary>The Area Generator view's static labels, bound by name from its markup.</summary>
public sealed class AreaGeneratorLabels
{
    private readonly AreaGeneratorTexts _texts;

    public AreaGeneratorLabels(AreaGeneratorTexts texts)
    {
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
    }

    public string PageTitle => _texts.Get(AreaGeneratorStringId.PageTitle);
    public string PageSubtitle => _texts.Get(AreaGeneratorStringId.PageSubtitle);
    public string CompositionTitle => _texts.Get(AreaGeneratorStringId.CompositionTitle);
    public string Theme => _texts.Get(AreaGeneratorStringId.Theme);
    public string Tier => _texts.Get(AreaGeneratorStringId.Tier);
    public string TilesetProfile => _texts.Get(AreaGeneratorStringId.TilesetProfile);
    public string LayoutProfile => _texts.Get(AreaGeneratorStringId.LayoutProfile);
    public string AreaTitle => _texts.Get(AreaGeneratorStringId.AreaTitle);
    public string Width => _texts.Get(AreaGeneratorStringId.Width);
    public string Height => _texts.Get(AreaGeneratorStringId.Height);
    public string Seed => _texts.Get(AreaGeneratorStringId.Seed);
    public string Randomize => _texts.Get(AreaGeneratorStringId.Randomize);
    public string RandomizeTip => _texts.Get(AreaGeneratorStringId.RandomizeTip);
    public string ResRef => _texts.Get(AreaGeneratorStringId.ResRef);
    public string ResRefWatermark => _texts.Get(AreaGeneratorStringId.ResRefWatermark);
    public string DisplayName => _texts.Get(AreaGeneratorStringId.DisplayName);
    public string AdvancedLayout => _texts.Get(AreaGeneratorStringId.AdvancedLayout);
    public string Style => _texts.Get(AreaGeneratorStringId.Style);
    public string MinimumRooms => _texts.Get(AreaGeneratorStringId.MinimumRooms);
    public string MaximumRooms => _texts.Get(AreaGeneratorStringId.MaximumRooms);
    public string MinimumRoomSize => _texts.Get(AreaGeneratorStringId.MinimumRoomSize);
    public string MaximumRoomSize => _texts.Get(AreaGeneratorStringId.MaximumRoomSize);
    public string CorridorWidth => _texts.Get(AreaGeneratorStringId.CorridorWidth);
    public string LoopFactor => _texts.Get(AreaGeneratorStringId.LoopFactor);
    public string OrganicFill => _texts.Get(AreaGeneratorStringId.OrganicFill);
    public string Entrances => _texts.Get(AreaGeneratorStringId.Entrances);
    public string Exits => _texts.Get(AreaGeneratorStringId.Exits);
    public string DoorTransitions => _texts.Get(AreaGeneratorStringId.DoorTransitions);
    public string AccentEnabled => _texts.Get(AreaGeneratorStringId.AccentEnabled);
    public string AccentDensity => _texts.Get(AreaGeneratorStringId.AccentDensity);
    public string FeatureDensity => _texts.Get(AreaGeneratorStringId.FeatureDensity);
    public string HeightRegions => _texts.Get(AreaGeneratorStringId.HeightRegions);
    public string DecorationsTitle => _texts.Get(AreaGeneratorStringId.DecorationsTitle);
    public string PlaceDecorations => _texts.Get(AreaGeneratorStringId.PlaceDecorations);
    public string Palette => _texts.Get(AreaGeneratorStringId.Palette);
    public string Placement => _texts.Get(AreaGeneratorStringId.Placement);
    public string PlacementHint => _texts.Get(AreaGeneratorStringId.PlacementHint);
    public string Density => _texts.Get(AreaGeneratorStringId.Density);
    public string DensityHint => _texts.Get(AreaGeneratorStringId.DensityHint);
    public string RefreshPreview => _texts.Get(AreaGeneratorStringId.RefreshPreview);
    public string ShowRooms => _texts.Get(AreaGeneratorStringId.ShowRooms);
    public string ShowTransitions => _texts.Get(AreaGeneratorStringId.ShowTransitions);
    public string ShowFootprints => _texts.Get(AreaGeneratorStringId.ShowFootprints);
    public string ShowFootprintsTip => _texts.Get(AreaGeneratorStringId.ShowFootprintsTip);
    public string ShowRoutes => _texts.Get(AreaGeneratorStringId.ShowRoutes);
    public string ShowRoutesTip => _texts.Get(AreaGeneratorStringId.ShowRoutesTip);
    public string PreviewPlaceholder => _texts.Get(AreaGeneratorStringId.PreviewPlaceholder);
    public string Close => _texts.Get(AreaGeneratorStringId.Close);
    public string CreateArea => _texts.Get(AreaGeneratorStringId.CreateArea);
}
