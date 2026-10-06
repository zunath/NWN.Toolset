using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nwn.Authoring.Areas.Generation;
using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Decoration;
using Nwn.Authoring.Areas.Generation.Drafting;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Areas.Generation.Population;
using Nwn.Authoring.Areas.Generation.Preview;
using Nwn.Authoring.Areas.Generation.Tilesets;
using Nwn.Formats.Resources;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>The workflow for previewing and creating deterministic generated areas, driven entirely by an <see cref="AreaGeneratorHost"/>.</summary>
public partial class AreaGeneratorViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan AutomaticPreviewDelay = TimeSpan.FromMilliseconds(300);

    private enum StatusSource
    {
        Preview,
        Creation
    }

    private readonly AreaGeneratorHost _host;
    private readonly AreaGenerationAuthoringService _authoring;
    private readonly AreaGenerationPreviewRenderer _renderer;
    private readonly IGeneratedAreaBlueprintSource _blueprints;
    private readonly IAreaGenerationLog _log;
    private readonly AreaGeneratorTexts _texts;
    private readonly IAreaGeneratorBackgroundTaskRunner _backgroundTasks;
    private bool _loadingDefaults;
    private bool _adjustingRanges;
    private bool _automaticPreviewEnabled;
    private bool _showResRefValidation;
    private bool _disposed;
    private string _statusWithoutResRefValidation;
    private bool _statusWithoutResRefValidationIsError;
    private string _latestPreviewStatus;
    private bool _latestPreviewStatusIsError;
    private StatusSource _statusSource;
    private CancellationTokenSource? _automaticPreviewCancellation;
    private AreaGenerationDraft? _previewedDraft;
    private AreaGenerationDraft? _solvedDraft;
    private int _previewRevision;
    private bool _renderOnlyRequested;

    private static readonly HashSet<string> GenerationInputProperties = new(StringComparer.Ordinal)
    {
        nameof(SelectedTheme),
        nameof(SelectedTier),
        nameof(SelectedTilesetProfile),
        nameof(SelectedLayoutProfile),
        nameof(SelectedDecorationProfile),
        nameof(Width),
        nameof(Height),
        nameof(Seed),
        nameof(LayoutStyle),
        nameof(MinRooms),
        nameof(MaxRooms),
        nameof(MinRoomSize),
        nameof(MaxRoomSize),
        nameof(CorridorWidth),
        nameof(LoopFactorPercent),
        nameof(OpenFillPercent),
        nameof(EntranceCount),
        nameof(ExitCount),
        nameof(DoorTransitions),
        nameof(AccentEnabled),
        nameof(AccentDensityPercent),
        nameof(FeatureDensityPercent),
        nameof(ElevationRegions),
        nameof(EnableDecorations),
        nameof(DecorationPlacementStyle),
        nameof(DecorationDensityPercent)
    };

    /// <summary>The window's static labels, from the host's texts.</summary>
    public AreaGeneratorLabels Labels { get; }

    /// <summary>The host's texts, for the converters that label enum choices.</summary>
    public AreaGeneratorTexts Texts => _texts;

    /// <summary>Whether the host offers themes; without them the composition is a tileset and layout only.</summary>
    public bool HasThemes => Themes.Count > 0;

    public ObservableCollection<AreaGeneratorThemeChoice> Themes { get; } = new();
    public ObservableCollection<AreaGeneratorTierChoice> Tiers { get; } = new();
    public ObservableCollection<AreaGeneratorTilesetChoice> TilesetProfiles { get; } = new();
    public ObservableCollection<AreaGeneratorLayoutChoice> LayoutProfiles { get; } = new();
    public ObservableCollection<AreaGeneratorDecorationChoice> DecorationProfiles { get; } = new();
    public IReadOnlyList<DungeonLayoutStyle> LayoutStyles { get; } = Enum.GetValues<DungeonLayoutStyle>();
    public IReadOnlyList<AreaPreviewMode> PreviewModes { get; } = Enum.GetValues<AreaPreviewMode>();
    public IReadOnlyList<DecorationPlacementStyle> DecorationPlacementStyles { get; } = Enum.GetValues<DecorationPlacementStyle>();
    public int MinimumDimension => LayoutStyleSizeFloor.For(LayoutStyle);
    public int MinimumRoomSizeBound => EffectiveRoomSizeBounds().Min;
    public int MaximumRoomSizeBound => Math.Min(
        EffectiveRoomSizeBounds().Max,
        AreaSettingsBounds.RoomSizeSliderAbsoluteMax);
    public int MinimumOpenFillPercent => LayoutStyle == DungeonLayoutStyle.OrganicCave
        ? (int)Math.Ceiling(LayoutParameterConstraints.MinSafeOpenFillTarget((int)Width, (int)Height) * 100)
        : AreaSettingsBounds.OrganicFillPercentMin;
    public int MaximumElevationRegions => SelectedTilesetProfile == null
        ? AreaSettingsBounds.ElevationRegionsMin
        : Math.Max(
            SelectedTilesetProfile.Value.MaxElevationRegions,
            SelectedTilesetProfile.Value.MaxReliefRegions);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GeneratePreviewCommand))]
    private AreaGeneratorThemeChoice? _selectedTheme;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GeneratePreviewCommand))]
    private AreaGeneratorTierChoice? _selectedTier;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GeneratePreviewCommand))]
    private AreaGeneratorTilesetChoice? _selectedTilesetProfile;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GeneratePreviewCommand))]
    private AreaGeneratorLayoutChoice? _selectedLayoutProfile;
    [ObservableProperty] private AreaGeneratorDecorationChoice? _selectedDecorationProfile;
    [ObservableProperty] private AreaPreviewMode _previewMode = AreaPreviewMode.MapGraphics;
    [ObservableProperty] private bool _showRooms = true;
    [ObservableProperty] private bool _showTransitions = true;
    [ObservableProperty] private bool _showDecorations = true;
    [ObservableProperty] private bool _showRoutes;
    [ObservableProperty] private string _resRef = string.Empty;
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private double _width = 16;
    [ObservableProperty] private double _height = 16;
    [ObservableProperty] private double _seed = 4242;
    [ObservableProperty] private DungeonLayoutStyle _layoutStyle = DungeonLayoutStyle.RoomsAndCorridors;
    [ObservableProperty] private double _minRooms = 4;
    [ObservableProperty] private double _maxRooms = 8;
    [ObservableProperty] private double _minRoomSize = 3;
    [ObservableProperty] private double _maxRoomSize = 7;
    [ObservableProperty] private double _corridorWidth = 1;
    [ObservableProperty] private double _loopFactorPercent = 25;
    [ObservableProperty] private double _openFillPercent = 45;
    [ObservableProperty] private double _entranceCount = 1;
    [ObservableProperty] private double _exitCount = 1;
    [ObservableProperty] private bool _doorTransitions = true;
    [ObservableProperty] private bool _accentEnabled = true;
    [ObservableProperty] private double _accentDensityPercent = 8;
    [ObservableProperty] private double _featureDensityPercent = 3;
    [ObservableProperty] private double _elevationRegions;
    [ObservableProperty] private bool _enableDecorations = true;
    [ObservableProperty] private DecorationPlacementStyle _decorationPlacementStyle = DecorationPlacementStyle.Spacious;
    [ObservableProperty] private double _decorationDensityPercent = 100;
    [ObservableProperty] private Bitmap? _preview;
    [ObservableProperty] private string _statusMessage;
    [ObservableProperty] private string _resRefError = string.Empty;
    [ObservableProperty] private bool _statusIsError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyMessage = string.Empty;

    public bool HasResRefError => !string.IsNullOrEmpty(ResRefError);

    public event Action<string>? AreaCreated;

    public AreaGeneratorViewModel(AreaGeneratorHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _texts = host.Texts ?? AreaGeneratorTexts.English;
        Labels = new AreaGeneratorLabels(_texts);
        _log = host.Log ?? NullAreaGenerationLog.Instance;
        _blueprints = host.Blueprints ?? EmptyGeneratedAreaBlueprintSource.Instance;
        _authoring = new AreaGenerationAuthoringService(host.Tilesets, host.Catalog, _log);
        _renderer = new AreaGenerationPreviewRenderer(host.TileGraphics);
        _backgroundTasks = host.BackgroundTasks ?? new AreaGeneratorBackgroundTaskRunner();

        var initialStatus = _texts.Get(AreaGeneratorStringId.StatusInitial);
        _statusWithoutResRefValidation = initialStatus;
        _latestPreviewStatus = initialStatus;
        _statusMessage = initialStatus;
        _displayName = _texts.Get(AreaGeneratorStringId.DefaultDisplayName);

        foreach (var theme in host.Catalog.Themes)
            Themes.Add(new AreaGeneratorThemeChoice(theme));

        var availableTilesets = host.Tilesets.GetTilesetResRefs().ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in host.Catalog.TilesetProfiles.Values
                     .Where(profile => availableTilesets.Contains(profile.TilesetResref))
                     .OrderBy(profile => profile.DisplayName))
        {
            TilesetProfiles.Add(new AreaGeneratorTilesetChoice(profile));
        }

        SelectedTheme = Themes.FirstOrDefault();
        if (SelectedTilesetProfile == null)
            SelectedTilesetProfile = TilesetProfiles.FirstOrDefault();
        RefreshLayoutProfiles();

        if (TilesetProfiles.Count == 0)
            SetStatus(_texts.Get(AreaGeneratorStringId.NoTilesets), isError: true);

        PropertyChanged += OnGenerationInputChanged;
    }

    partial void OnSelectedThemeChanged(AreaGeneratorThemeChoice? value)
    {
        if (_loadingDefaults || value == null)
            return;

        _loadingDefaults = true;
        SelectedTilesetProfile = TilesetProfiles.FirstOrDefault(choice =>
            choice.Value.Key.Equals(value.Value.TilesetProfileKey, StringComparison.OrdinalIgnoreCase))
            ?? SelectedTilesetProfile;
        RefreshTiers();
        RefreshLayoutProfiles(value.Value.LayoutProfileKey);
        _loadingDefaults = false;
        LoadCompositionDefaults();
    }

    partial void OnSelectedTilesetProfileChanged(AreaGeneratorTilesetChoice? value)
    {
        OnPropertyChanged(nameof(MaximumElevationRegions));
        ElevationRegions = Math.Min(ElevationRegions, MaximumElevationRegions);
        if (_loadingDefaults)
            return;

        RefreshLayoutProfiles(SelectedTheme?.Value.LayoutProfileKey);
        LoadCompositionDefaults();
    }

    partial void OnSelectedLayoutProfileChanged(AreaGeneratorLayoutChoice? value)
    {
        if (!_loadingDefaults)
            LoadCompositionDefaults();
    }

    partial void OnIsBusyChanged(bool value)
    {
        if (!value)
            BusyMessage = string.Empty;

        GeneratePreviewCommand.NotifyCanExecuteChanged();
        CreateAreaCommand.NotifyCanExecuteChanged();
    }

    partial void OnResRefChanged(string value)
    {
        if (!_showResRefValidation)
            return;

        if (_statusSource == StatusSource.Creation)
            RestoreLatestPreviewStatus();

        ValidateResRef();
        ApplyStatus();
    }

    partial void OnResRefErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasResRefError));
    }

    partial void OnLayoutStyleChanged(DungeonLayoutStyle value)
    {
        OnPropertyChanged(nameof(MinimumDimension));
        var minimum = LayoutStyleSizeFloor.For(value);
        Width = Math.Max(Width, minimum);
        Height = Math.Max(Height, minimum);
        RefreshEffectiveRanges();
    }

    partial void OnWidthChanged(double value) => RefreshEffectiveRanges();

    partial void OnHeightChanged(double value) => RefreshEffectiveRanges();

    partial void OnMinRoomsChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value > MaxRooms)
            MaxRooms = value;
    }

    partial void OnMaxRoomsChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value < MinRooms)
            MinRooms = value;
    }

    partial void OnMinRoomSizeChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value > MaxRoomSize)
            MaxRoomSize = value;
    }

    partial void OnMaxRoomSizeChanged(double value)
    {
        if (!_loadingDefaults && !_adjustingRanges && value < MinRoomSize)
            MinRoomSize = value;
    }

    partial void OnAccentEnabledChanged(bool value)
    {
        if (!_loadingDefaults && value && SupportsBlobAccents() &&
            AccentDensityPercent < AreaSettingsBounds.AccentDensityPercentMin)
            AccentDensityPercent = AreaSettingsBounds.AccentDensityPercentMin;
    }

    partial void OnPreviewModeChanged(AreaPreviewMode value) => InvalidatePreviewDisplay();

    partial void OnShowRoomsChanged(bool value) => InvalidatePreviewDisplay();

    partial void OnShowTransitionsChanged(bool value) => InvalidatePreviewDisplay();

    partial void OnShowDecorationsChanged(bool value) => InvalidatePreviewDisplay();
    /// <summary>Requests a new rendering when the reserved-route overlay changes.</summary>
    partial void OnShowRoutesChanged(bool value) => InvalidatePreviewDisplay();

    partial void OnPreviewChanging(Bitmap? oldValue, Bitmap? newValue)
    {
        if (!ReferenceEquals(oldValue, newValue))
            oldValue?.Dispose();
    }

    private void RefreshLayoutProfiles(string? preferredKey = null)
    {
        var priorKey = preferredKey ?? SelectedLayoutProfile?.Value.Key;
        LayoutProfiles.Clear();

        TilesetModel? model = null;
        if (SelectedTilesetProfile != null &&
            _host.Tilesets.TryGetTileset(SelectedTilesetProfile.Value.TilesetResref, out var source))
        {
            model = source.Model;
        }

        foreach (var profile in _host.Catalog.LayoutProfiles.Values.OrderBy(profile => profile.DisplayName))
        {
            if (model == null || SelectedTilesetProfile == null ||
                LayoutSupportRules.Supports(SelectedTilesetProfile.Value, profile, model))
            {
                LayoutProfiles.Add(new AreaGeneratorLayoutChoice(profile));
            }
        }

        _loadingDefaults = true;
        SelectedLayoutProfile = LayoutProfiles.FirstOrDefault(choice =>
            choice.Value.Key.Equals(priorKey, StringComparison.OrdinalIgnoreCase))
            ?? LayoutProfiles.FirstOrDefault();
        _loadingDefaults = false;
    }

    private void RefreshTiers()
    {
        var priorTier = SelectedTier?.Value.Tier;
        Tiers.Clear();
        if (SelectedTheme != null)
        {
            foreach (var tier in SelectedTheme.Value.Tiers.Values.OrderBy(tier => tier.Tier))
                Tiers.Add(new AreaGeneratorTierChoice(tier, _texts.Get(AreaGeneratorStringId.TierLabel, tier.Tier)));
        }

        SelectedTier = Tiers.FirstOrDefault(choice => choice.Value.Tier == priorTier)
                       ?? Tiers.FirstOrDefault();
    }

    private void LoadCompositionDefaults()
    {
        if (SelectedTilesetProfile == null || SelectedLayoutProfile == null || (HasThemes && SelectedTheme == null))
            return;

        _loadingDefaults = true;
        var composition = new DungeonComposition
        {
            Content = SelectedTheme?.Value,
            Tileset = SelectedTilesetProfile.Value,
            Layout = SelectedLayoutProfile.Value
        };
        var parameters = composition.BuildLayoutParameters();
        LayoutStyle = parameters.Style;
        MinRooms = parameters.MinRooms;
        MaxRooms = parameters.MaxRooms;
        MinRoomSize = parameters.MinRoomCornerSize;
        MaxRoomSize = parameters.MaxRoomCornerSize;
        CorridorWidth = parameters.CorridorWidth;
        LoopFactorPercent = Math.Round(parameters.LoopFactor * 100);
        OpenFillPercent = Math.Round(parameters.OpenFillTarget * 100);
        EntranceCount = parameters.EntranceCount;
        ExitCount = parameters.ExitCount;
        DoorTransitions = parameters.DoorTransitions;
        AccentEnabled = (parameters.AccentDensity > 0 && !string.IsNullOrWhiteSpace(parameters.AccentTerrain)) ||
                        (parameters.AccentChannels > 0 && !string.IsNullOrWhiteSpace(parameters.ChannelTerrain)) ||
                        (parameters.PoolRegions > 0 && !string.IsNullOrWhiteSpace(parameters.PoolTerrain));
        AccentDensityPercent = Math.Round(parameters.AccentDensity * 100);
        FeatureDensityPercent = Math.Round(parameters.FeatureDensity * 100);
        ElevationRegions = Math.Max(parameters.ElevationRegions, parameters.ReliefRegions);
        EnableDecorations = true;
        DecorationDensityPercent = 100;

        DecorationProfiles.Clear();
        DecorationProfiles.Add(new AreaGeneratorDecorationChoice(string.Empty, _texts.Get(AreaGeneratorStringId.StandardPalette)));
        foreach (var key in SelectedTilesetProfile.Value.DecorationProfiles.Keys.OrderBy(key => key))
            DecorationProfiles.Add(new AreaGeneratorDecorationChoice(key, key));
        SelectedDecorationProfile = DecorationProfiles.FirstOrDefault(choice =>
            choice.Key.Equals(SelectedTheme?.Value.DecorationProfile ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            ?? DecorationProfiles[0];
        _loadingDefaults = false;
        RefreshEffectiveRanges();
        InvalidateAndRequestPreview(_texts.Get(AreaGeneratorStringId.StatusCompositionChanged));
    }

    private bool SupportsBlobAccents() =>
        SelectedTilesetProfile != null &&
        !string.IsNullOrWhiteSpace(SelectedTilesetProfile.Value.AccentTerrain);

    private bool CanGenerate() => !IsBusy &&
                                  (!HasThemes || (SelectedTheme != null && SelectedTier != null)) &&
                                  SelectedTilesetProfile != null &&
                                  SelectedLayoutProfile != null;

    private bool CanCreate() => CanGenerate() && _previewedDraft != null;

    /// <summary>Solves or rerenders a draft and publishes it only if its settings revision is still current.</summary>
    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GeneratePreview()
    {
        var revision = _previewRevision;
        var cachedDraft = _renderOnlyRequested ? _solvedDraft : null;
        _renderOnlyRequested = false;
        CancelAutomaticPreviewRequest();
        SetPreviewedDraft(null);
        BusyMessage = _texts.Get(AreaGeneratorStringId.BusyPreparingSettings);
        IsBusy = true;
        SetStatus(_texts.Get(AreaGeneratorStringId.StatusGenerating));
        try
        {
            var settings = BuildSettings();
            BusyMessage = _texts.Get(AreaGeneratorStringId.BusySolving);
            var draft = cachedDraft ?? await _backgroundTasks.RunAsync(() => _authoring.Generate(settings, _blueprints)).ConfigureAwait(true);
            if (_disposed || revision != _previewRevision)
                return;
            if (!draft.Result.Success)
            {
                Preview = null;
                SetStatus(draft.Result.FailureReason, isError: true);
                return;
            }

            _solvedDraft = draft;

            var previewMode = PreviewMode;
            var showRooms = ShowRooms;
            var showTransitions = ShowTransitions;
            var showDecorations = ShowDecorations;
            var showRoutes = ShowRoutes;
            BusyMessage = previewMode == AreaPreviewMode.MapGraphics
                ? _texts.Get(AreaGeneratorStringId.BusyRenderingMap)
                : _texts.Get(AreaGeneratorStringId.BusyRenderingSchematic);
            var image = await _backgroundTasks.RunAsync(() => _renderer.Render(
                draft,
                previewMode,
                showRooms,
                showTransitions,
                showDecorations,
                showRoutes: showRoutes)).ConfigureAwait(true);
            if (_disposed || revision != _previewRevision)
                return;
            BusyMessage = _texts.Get(AreaGeneratorStringId.BusyPreparingImage);
            Preview = ToBitmap(image);
            SetPreviewedDraft(draft);
            SetStatus(Describe(draft, image));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Area generation preview failed.");
            if (_disposed || revision != _previewRevision)
                return;
            Preview = null;
            SetPreviewedDraft(null);
            SetStatus(ex.GetBaseException().Message, isError: true);
        }
        finally
        {
            IsBusy = false;
            if (!_disposed && revision != _previewRevision)
                RequestAutomaticPreview();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateArea()
    {
        var draft = _previewedDraft;
        if (draft == null)
        {
            SetStatus(
                _texts.Get(AreaGeneratorStringId.WaitForPreview),
                isError: true,
                source: StatusSource.Creation);
            return;
        }

        _showResRefValidation = true;
        if (!ValidateResRef())
        {
            ApplyStatus();
            return;
        }

        BusyMessage = _texts.Get(AreaGeneratorStringId.BusyWriting);
        IsBusy = true;
        SetStatus(_texts.Get(AreaGeneratorStringId.StatusCreating), source: StatusSource.Creation);
        string? createdResref = null;
        try
        {
            var resRef = ResRef;
            var displayName = DisplayName;
            var createResult = await _backgroundTasks.RunAsync(() =>
            {
                var canonicalResRef = resRef.Trim().ToLowerInvariant();
                var request = new GeneratedAreaRequest(
                    draft,
                    canonicalResRef,
                    displayName,
                    GeneratedAreaDocumentPopulator.CreatePopulator(
                        draft,
                        _blueprints,
                        _host.PopulationPolicy,
                        _log,
                        canonicalResRef));
                var success = _host.Writer.TryCreate(request, out var error);
                return (Success: success, Error: error);
            }).ConfigureAwait(true);
            if (!createResult.Success)
            {
                SetStatus(createResult.Error, isError: true, source: StatusSource.Creation);
                return;
            }

            var normalized = resRef.Trim().ToLowerInvariant();
            SetStatus(_texts.Get(AreaGeneratorStringId.StatusCreated, normalized), source: StatusSource.Creation);
            createdResref = normalized;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Creating generated area {ResRef} failed.");
            SetStatus(ex.GetBaseException().Message, isError: true, source: StatusSource.Creation);
        }
        finally
        {
            IsBusy = false;
        }

        if (createdResref != null)
            AreaCreated?.Invoke(createdResref);
    }

    private void OnGenerationInputChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_loadingDefaults || e.PropertyName == null ||
            !GenerationInputProperties.Contains(e.PropertyName))
        {
            return;
        }

        InvalidateAndRequestPreview(_texts.Get(AreaGeneratorStringId.StatusSettingsChanged));
    }

    /// <summary>
    /// Starts the automatic preview lifecycle once the window is visible. Keeping this out of the
    /// constructor avoids doing rendering work for view models that are prepared but never shown.
    /// </summary>
    public void EnableAutomaticPreview()
    {
        if (_automaticPreviewEnabled || _disposed)
            return;

        RandomizeInitialSeed();
        _automaticPreviewEnabled = true;
        if (CanGenerate())
        {
            SetStatus(_texts.Get(AreaGeneratorStringId.StatusPreparing));
            RequestAutomaticPreview();
        }
    }

    private void RandomizeInitialSeed()
    {
        var wasLoadingDefaults = _loadingDefaults;
        _loadingDefaults = true;
        try
        {
            Seed = NextRandomSeed();
        }
        finally
        {
            _loadingDefaults = wasLoadingDefaults;
        }
    }

    [RelayCommand]
    private void RandomizeSeed()
    {
        Seed = NextRandomSeed();
    }

    private int NextRandomSeed()
    {
        var seed = Random.Shared.Next(AreaSettingsBounds.MaxSeed + 1);
        if (seed == Seed)
            seed = seed == AreaSettingsBounds.MaxSeed ? 0 : seed + 1;

        return seed;
    }

    private void InvalidateAndRequestPreview(string status)
    {
        InvalidatePreview(status);
        RequestAutomaticPreview();
    }

    /// <summary>Discards the solved draft and rendered preview when a generation input changes.</summary>
    private void InvalidatePreview(string status)
    {
        _previewRevision++;
        _solvedDraft = null;
        _renderOnlyRequested = false;
        Preview = null;
        SetPreviewedDraft(null);
        SetStatus(status);
    }

    /// <summary>Invalidates the displayed image while retaining the solved draft for overlay-only rendering.</summary>
    private void InvalidatePreviewDisplay()
    {
        if (_loadingDefaults)
            return;

        _previewRevision++;
        _renderOnlyRequested = true;
        if (Preview != null)
        {
            Preview = null;
            SetPreviewedDraft(null);
            SetStatus(_texts.Get(AreaGeneratorStringId.StatusDisplayChanged));
        }

        RequestAutomaticPreview();
    }

    private void RequestAutomaticPreview()
    {
        if (!_automaticPreviewEnabled || _loadingDefaults || _disposed || !CanGenerate())
            return;

        CancelAutomaticPreviewRequest();
        var request = new CancellationTokenSource();
        _automaticPreviewCancellation = request;
        _ = GeneratePreviewAfterDelay(request, request.Token);
    }

    private async Task GeneratePreviewAfterDelay(CancellationTokenSource request, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutomaticPreviewDelay, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested || _disposed ||
            !ReferenceEquals(_automaticPreviewCancellation, request) || !CanGenerate())
        {
            return;
        }

        await GeneratePreview().ConfigureAwait(true);
    }

    private void CancelAutomaticPreviewRequest()
    {
        var request = _automaticPreviewCancellation;
        _automaticPreviewCancellation = null;
        if (request == null)
            return;

        request.Cancel();
        request.Dispose();
    }

    private (int Min, int Max) EffectiveRoomSizeBounds()
    {
        var width = Math.Clamp((int)Width, AreaSettingsBounds.WidthMin, AreaSettingsBounds.WidthMax);
        var height = Math.Clamp((int)Height, AreaSettingsBounds.HeightMin, AreaSettingsBounds.HeightMax);
        return LayoutParameterConstraints.RoomSizeBounds(LayoutStyle, width, height);
    }

    private void RefreshEffectiveRanges()
    {
        OnPropertyChanged(nameof(MinimumRoomSizeBound));
        OnPropertyChanged(nameof(MaximumRoomSizeBound));
        OnPropertyChanged(nameof(MinimumOpenFillPercent));
        if (_loadingDefaults || _adjustingRanges)
            return;

        _adjustingRanges = true;
        try
        {
            MinRoomSize = Math.Clamp(MinRoomSize, MinimumRoomSizeBound, MaximumRoomSizeBound);
            MaxRoomSize = Math.Clamp(MaxRoomSize, MinimumRoomSizeBound, MaximumRoomSizeBound);
            if (MinRoomSize > MaxRoomSize)
                MinRoomSize = MaxRoomSize;
            OpenFillPercent = Math.Max(OpenFillPercent, MinimumOpenFillPercent);
        }
        finally
        {
            _adjustingRanges = false;
        }
    }

    private bool ValidateResRef()
    {
        var normalized = (ResRef ?? string.Empty).Trim().ToLowerInvariant();
        ResRefError = ResourceReferenceRules.IsCanonical(normalized)
            ? string.Empty
            : _texts.Get(AreaGeneratorStringId.ResRefInvalid, ResourceReferenceRules.MaxLength);
        return !HasResRefError;
    }

    private void SetStatus(
        string message,
        bool isError = false,
        StatusSource source = StatusSource.Preview)
    {
        if (source == StatusSource.Preview)
        {
            _latestPreviewStatus = message;
            _latestPreviewStatusIsError = isError;
        }

        _statusWithoutResRefValidation = message;
        _statusWithoutResRefValidationIsError = isError;
        _statusSource = source;

        ApplyStatus();
    }

    private void RestoreLatestPreviewStatus()
    {
        _statusWithoutResRefValidation = _latestPreviewStatus;
        _statusWithoutResRefValidationIsError = _latestPreviewStatusIsError;
        _statusSource = StatusSource.Preview;
    }

    private void ApplyStatus()
    {
        if (!HasResRefError)
        {
            StatusIsError = _statusWithoutResRefValidationIsError;
            StatusMessage = _statusWithoutResRefValidation;
            return;
        }

        StatusIsError = true;
        StatusMessage = _statusWithoutResRefValidationIsError
            ? $"{ResRefError}{Environment.NewLine}{_statusWithoutResRefValidation}"
            : ResRefError;
    }

    private void SetPreviewedDraft(AreaGenerationDraft? draft)
    {
        _previewedDraft = draft;
        CreateAreaCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Captures builder selections as generation settings, including decoration style, palette and density.</summary>
    private AreaGenerationSettings BuildSettings()
    {
        var wholeValues = new[]
        {
            Width, Height, Seed, MinRooms, MaxRooms, MinRoomSize, MaxRoomSize, CorridorWidth,
            LoopFactorPercent, OpenFillPercent, EntranceCount, ExitCount, AccentDensityPercent,
            FeatureDensityPercent, ElevationRegions, DecorationDensityPercent
        };
        if (wholeValues.Any(value => !double.IsFinite(value) || value != Math.Truncate(value)))
            throw new InvalidOperationException(_texts.Get(AreaGeneratorStringId.WholeNumbers));

        return new AreaGenerationSettings
        {
            ThemeKey = SelectedTheme?.Value.ThemeKey ?? string.Empty,
            Tier = SelectedTier?.Value.Tier ?? 1,
            TilesetProfileKey = SelectedTilesetProfile!.Value.Key,
            LayoutProfileKey = SelectedLayoutProfile!.Value.Key,
            Width = (int)Width,
            Height = (int)Height,
            Seed = (int)Seed,
            Overrides = new LayoutKnobOverrides
            {
                Style = LayoutStyle,
                MinRooms = (int)MinRooms,
                MaxRooms = (int)MaxRooms,
                MinRoomCornerSize = (int)MinRoomSize,
                MaxRoomCornerSize = (int)MaxRoomSize,
                CorridorWidth = (int)CorridorWidth,
                LoopFactorPercent = (int)LoopFactorPercent,
                OpenFillTargetPercent = (int)OpenFillPercent,
                EntranceCount = (int)EntranceCount,
                ExitCount = (int)ExitCount,
                DoorTransitions = DoorTransitions,
                AccentEnabled = AccentEnabled,
                AccentDensityPercent = (int)AccentDensityPercent,
                FeatureDensityPercent = (int)FeatureDensityPercent,
                ElevationRegions = (int)ElevationRegions,
                EnableDecorations = EnableDecorations,
                DecorationPlacementStyle = DecorationPlacementStyle,
                DecorationDensityPercent = (int)DecorationDensityPercent,
                DecorationProfile = SelectedDecorationProfile?.Key ?? string.Empty
            }
        };
    }

    /// <summary>Summarizes the generated layout, preview availability and decoration omission diagnostics for the builder.</summary>
    private string Describe(AreaGenerationDraft draft, AreaPreviewImage image)
    {
        var resolved = draft.Result.Resolved!;
        var missing = image.MissingTileGraphics == 0
            ? string.Empty
            : _texts.Get(AreaGeneratorStringId.MissingGraphics, image.MissingTileGraphics);
        var report = draft.Result.DecorationPlacementReport;
        var clearance = report is { OmittedCount: > 0 }
            ? _texts.Get(
                AreaGeneratorStringId.OmittedProps,
                report.OmittedCount,
                report.UnsupportedCount,
                report.RouteConflictCount,
                report.OverlapCount)
            : string.Empty;
        return _texts.Get(
            AreaGeneratorStringId.PreviewSummary,
            draft.Result.AttemptSeed,
            resolved.Rooms.Count,
            resolved.Transitions.Count,
            draft.Result.PlannedDecorationCount,
            clearance,
            missing);
    }

    private static Bitmap ToBitmap(AreaPreviewImage image)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(image.Width, image.Height),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);
        using var buffer = bitmap.Lock();
        var stride = image.Width * 4;
        if (buffer.RowBytes == stride)
        {
            System.Runtime.InteropServices.Marshal.Copy(
                image.Pixels,
                0,
                buffer.Address,
                image.Pixels.Length);
            return bitmap;
        }

        for (var y = 0; y < image.Height; y++)
        {
            System.Runtime.InteropServices.Marshal.Copy(
                image.Pixels,
                y * stride,
                buffer.Address + y * buffer.RowBytes,
                stride);
        }
        return bitmap;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CancelAutomaticPreviewRequest();
        PropertyChanged -= OnGenerationInputChanged;
        Preview = null;
        SetPreviewedDraft(null);
    }
}

