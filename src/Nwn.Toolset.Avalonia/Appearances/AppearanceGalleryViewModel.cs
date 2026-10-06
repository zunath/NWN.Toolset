using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Nwn.Toolset.Avalonia.Appearances;

/// <summary>Search, paging, selection, and preview presentation for host-supplied appearance options.</summary>
public sealed partial class AppearanceGalleryViewModel : ObservableObject, IDisposable
{
    private const int PageSize = 48;
    private const int MaxPreviewAttempts = 5;
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(250);
    private IReadOnlyList<AppearanceGalleryOption> _options;
    private readonly IAppearanceGalleryPreviewProvider? _previews;
    private readonly Func<AppearanceGalleryOptionId> _currentId;
    private readonly Func<AppearanceGalleryOption, bool> _apply;
    private readonly AppearanceGalleryTexts _texts;
    private readonly string _noun;
    private readonly HashSet<AppearanceGalleryTile> _realizedTiles = new();
    private readonly Dictionary<AppearanceGalleryTile, CancellationTokenSource> _requests = new();
    private List<AppearanceGalleryOption> _matches = new();
    private CancellationTokenSource? _searchDebounce;
    private int _published;
    private long _snapshotVersion;
    private bool _loading;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<AppearanceGalleryTile> _tiles = new();

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private AppearanceGalleryTile? _highlighted;

    public string CurrentDescription
    {
        get
        {
            var id = _currentId();
            if (_options.FirstOrDefault(option => option.Id == id) is not { } current)
                return _texts.UnknownCurrent(id.Value);
            return current.Detail is { Length: > 0 } detail
                ? _texts.CurrentWithDetail(current.Caption, detail)
                : current.Caption;
        }
    }

    public bool CurrentIsUnknown => _options.All(option => option.Id != _currentId());
    public bool CanLoadMore => _published < _matches.Count;
    public double TileSize { get; }
    public double TileImageHeight => TileSize * 0.73;
    public string SearchWatermark => _texts.SearchWatermark(_noun);

    public string MatchSummary
    {
        get
        {
            if (_matches.Count == 0)
                return _texts.NoMatches(_noun);
            if (_published < _matches.Count)
                return _texts.PartialMatches(_noun, _published, _matches.Count);
            return _matches.Count == 1
                ? _texts.OneMatch(_noun, _matches.Count)
                : _texts.ManyMatches(_noun, _matches.Count);
        }
    }

    public AppearanceGalleryViewModel(
        IReadOnlyList<AppearanceGalleryOption> options,
        IAppearanceGalleryPreviewProvider? previews,
        Func<AppearanceGalleryOptionId> currentId,
        Func<AppearanceGalleryOption, bool> apply,
        AppearanceGalleryTexts? texts = null,
        string? noun = null,
        double tileSize = 112)
    {
        _options = ValidateSnapshot(options);
        _previews = previews;
        _currentId = currentId ?? throw new ArgumentNullException(nameof(currentId));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _texts = texts ?? AppearanceGalleryTexts.English;
        _noun = string.IsNullOrWhiteSpace(noun) ? _texts.DefaultNoun : noun;
        TileSize = tileSize;
        Rebuild();
    }

    public void SetOptions(IReadOnlyList<AppearanceGalleryOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _loading = true;
        try
        {
            Highlighted = null;
        }
        finally
        {
            _loading = false;
        }
        _options = ValidateSnapshot(options);
        Rebuild();
        NotifyCurrentChanged();
    }

    public void ReloadFromDocument()
    {
        _loading = true;
        try
        {
            Highlighted = null;
            var current = _currentId();
            foreach (var tile in Tiles)
                tile.IsCurrent = tile.Option.Id == current;
        }
        finally
        {
            _loading = false;
        }
        NotifyCurrentChanged();
    }

    public void ReloadPreviews()
    {
        if (_disposed)
            return;
        foreach (var tile in Tiles.ToArray())
        {
            var realized = _realizedTiles.Contains(tile);
            CancelRequest(tile);
            tile.Preview = null;
            tile.PreviewRequested = false;
            if (realized)
                EnsurePreview(tile);
        }
    }

    public void EnsurePreview(AppearanceGalleryTile? tile)
    {
        if (!IsActive(tile) || _previews == null || tile!.PreviewRequested || tile.Preview != null)
            return;
        _realizedTiles.Add(tile);
        tile.PreviewAttempts++;
        var version = _snapshotVersion;
        var cancellation = new CancellationTokenSource();
        CancelRequest(tile);
        _requests[tile] = cancellation;
        tile.PreviewRequested = true;
        var accepted = false;
        var requestReturned = false;
        try
        {
            accepted = _previews.Request(
                tile.Option,
                bitmap => Dispatcher.UIThread.Post(() =>
                {
                    if (requestReturned && accepted)
                        CompletePreview(tile, version, cancellation, bitmap);
                }),
                () => Dispatcher.UIThread.Post(() =>
                {
                    if (requestReturned && accepted)
                        FailPreview(tile, version, cancellation);
                }),
                tile.IsCurrent ? AppearanceGalleryPreviewPriority.Selected : AppearanceGalleryPreviewPriority.Visible);
        }
        catch (Exception)
        {
            requestReturned = true;
            FailPreview(tile, version, cancellation);
            return;
        }
        requestReturned = true;
        if (!accepted)
            FailPreview(tile, version, cancellation);
    }

    [RelayCommand]
    private void LoadMore()
    {
        if (_disposed)
            return;
        var end = Math.Min(_published + PageSize, _matches.Count);
        var current = _currentId();
        for (var index = _published; index < end; index++)
        {
            var option = _matches[index];
            var tile = CreateTile(option, option.Id == current);
            Tiles.Add(tile);
            ApplyCachedPreview(tile);
        }
        _published = end;
        NotifyProjectionChanged();
    }

    partial void OnQueryChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            Rebuild();
            return;
        }
        var pending = new CancellationTokenSource();
        _searchDebounce = pending;
        _ = Task.Delay(SearchDebounce, pending.Token).ContinueWith(
            task =>
            {
                if (!task.IsCanceled)
                    Dispatcher.UIThread.Post(Rebuild);
            },
            TaskScheduler.Default);
    }

    partial void OnHighlightedChanged(AppearanceGalleryTile? value)
    {
        if (_loading || value == null || !IsActive(value) || value.Option.Id == _currentId())
            return;
        if (!_apply(value.Option))
        {
            ReloadFromDocument();
            return;
        }
        foreach (var tile in Tiles)
            tile.IsCurrent = tile.Option.Id == value.Option.Id;
        NotifyCurrentChanged();
    }

    private void Rebuild()
    {
        if (_disposed)
            return;
        CancelAllRequests();
        _snapshotVersion++;
        var words = Query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _matches = _options
            .Where(option => words.All(word => option.SearchText.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        _realizedTiles.Clear();
        ClearPreviewReferences(Tiles);
        _published = Math.Min(PageSize, _matches.Count);
        var current = _currentId();
        var firstPage = new List<AppearanceGalleryTile>(_published);
        for (var index = 0; index < _published; index++)
        {
            var tile = CreateTile(_matches[index], _matches[index].Id == current);
            ApplyCachedPreview(tile);
            firstPage.Add(tile);
        }
        Tiles = new ObservableCollection<AppearanceGalleryTile>(firstPage);
        EnsurePreview(firstPage.FirstOrDefault(tile => tile.IsCurrent));
        NotifyProjectionChanged();
    }

    private static IReadOnlyList<AppearanceGalleryOption> ValidateSnapshot(
        IReadOnlyList<AppearanceGalleryOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var snapshot = options.ToArray();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in snapshot)
        {
            ArgumentNullException.ThrowIfNull(option);
            if (string.IsNullOrWhiteSpace(option.Id.Value) || !identities.Add(option.Id.Value))
                throw new ArgumentException("Appearance option ids must be non-empty and unique.", nameof(options));
        }
        return snapshot;
    }

    private AppearanceGalleryTile CreateTile(AppearanceGalleryOption option, bool current) =>
        new(option, current, TileSize, _snapshotVersion);

    private void ApplyCachedPreview(AppearanceGalleryTile tile)
    {
        if (_previews?.Cached(tile.Option) is { } bitmap)
        {
            tile.Preview = bitmap;
            tile.PreviewRequested = true;
        }
    }

    private void CompletePreview(
        AppearanceGalleryTile tile, long version, CancellationTokenSource request, global::Avalonia.Media.Imaging.Bitmap bitmap)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsActive(tile) || version != _snapshotVersion || !_requests.TryGetValue(tile, out var active) || !ReferenceEquals(active, request))
                return;
            _requests.Remove(tile);
            request.Dispose();
            tile.Preview = bitmap;
            tile.PreviewRequested = false;
            tile.PreviewAttempts = 0;
        });
    }

    private void FailPreview(AppearanceGalleryTile tile, long version, CancellationTokenSource request)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsActive(tile) || version != _snapshotVersion || !_requests.TryGetValue(tile, out var active) || !ReferenceEquals(active, request))
                return;
            _requests.Remove(tile);
            request.Dispose();
            tile.PreviewRequested = false;
            QueuePreviewRetry(tile);
        });
    }

    private void QueuePreviewRetry(AppearanceGalleryTile tile)
    {
        if (!IsActive(tile) || tile.PreviewAttempts >= MaxPreviewAttempts)
            return;
        var delay = TimeSpan.FromMilliseconds(250 * (1 << Math.Min(tile.PreviewAttempts - 1, 4)));
        _ = Task.Delay(delay).ContinueWith(
            _ => Dispatcher.UIThread.Post(() =>
            {
                if (IsActive(tile) && tile.Preview == null)
                    EnsurePreview(tile);
            }),
            TaskScheduler.Default);
    }

    private bool IsActive(AppearanceGalleryTile? tile) =>
        !_disposed && tile != null && tile.SnapshotVersion == _snapshotVersion && Tiles.Contains(tile);

    private static void ClearPreviewReferences(IEnumerable<AppearanceGalleryTile> tiles)
    {
        foreach (var tile in tiles)
        {
            tile.Preview = null;
            tile.PreviewRequested = false;
        }
    }

    private void CancelRequest(AppearanceGalleryTile tile)
    {
        if (_requests.Remove(tile, out var request))
        {
            request.Cancel();
            request.Dispose();
        }
    }

    private void CancelAllRequests()
    {
        foreach (var request in _requests.Values)
        {
            request.Cancel();
            request.Dispose();
        }
        _requests.Clear();
    }

    private void NotifyCurrentChanged()
    {
        OnPropertyChanged(nameof(CurrentDescription));
        OnPropertyChanged(nameof(CurrentIsUnknown));
    }

    private void NotifyProjectionChanged()
    {
        OnPropertyChanged(nameof(MatchSummary));
        OnPropertyChanged(nameof(CanLoadMore));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = null;
        CancelAllRequests();
        ClearPreviewReferences(Tiles);
    }
}
