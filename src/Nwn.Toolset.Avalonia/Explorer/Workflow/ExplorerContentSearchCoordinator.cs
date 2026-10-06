using Avalonia.Threading;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// Runs the host's content search off the keystroke path: a pause in typing, then one background scan,
/// whose result is published only while its query is still the one in the box.
/// </summary>
/// <remarks>
/// A content search can read a whole corpus (every conversation, about a second). Run inline it froze the
/// window once per letter typed, and all but the last scan were for prefixes nobody wanted. Name and
/// resref matches never wait for it: they show at once, and content matches join them when the scan lands.
/// </remarks>
internal sealed class ExplorerContentSearchCoordinator : IDisposable
{
    /// <summary>How long typing has to pause before the corpus is read.</summary>
    internal static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(300);

    private readonly IModuleExplorerContentSearch? _search;
    private readonly Action<bool> _setSearching;
    private readonly Action _published;
    private readonly Action<string> _failed;

    private HashSet<string>? _hits;
    private string? _hitsQuery;
    private ModuleResourceType? _hitsType;
    private CancellationTokenSource? _scan;

    public ExplorerContentSearchCoordinator(
        IModuleExplorerContentSearch? search,
        Action<bool> setSearching,
        Action published,
        Action<string> failed)
    {
        _search = search;
        _setSearching = setSearching;
        _published = published;
        _failed = failed;
    }

    public bool Supports(ModuleResourceType type) => _search?.Supports(type) == true;

    public string SearchingLabel(ModuleResourceType type) =>
        Supports(type) ? _search!.SearchingLabel(type) : string.Empty;

    /// <summary>A resource of this type changed, so a completed scan for the same query is stale.</summary>
    public void Invalidate(ModuleResourceType type)
    {
        if (Supports(type))
            _hitsQuery = null;
    }

    /// <summary>Schedules a scan for the current query, cancelling any scan for an older one.</summary>
    public void Queue(ModuleResourceType type, string? filter)
    {
        _scan?.Cancel();
        _scan?.Dispose();
        _scan = null;

        var needle = filter?.Trim() ?? string.Empty;
        if (!Supports(type) || needle.Length == 0)
        {
            _hits = null;
            _hitsQuery = null;
            _hitsType = null;
            _setSearching(false);
            return;
        }

        // Already have it: retyping the same query should not re-read the corpus.
        if (_hitsType == type && string.Equals(_hitsQuery, needle, StringComparison.OrdinalIgnoreCase))
            return;

        // Prepared here, on the UI thread, so open-editor state is snapshotted before any worker runs.
        var scan = _search!.Prepare(type, needle);
        if (scan == null)
            return;

        _setSearching(true);

        var pending = new CancellationTokenSource();
        _scan = pending;
        var token = pending.Token;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    // Awaited, not just declared: this is what turns one scan per keystroke into one scan
                    // per pause in typing.
                    await Task.Delay(Debounce, token).ConfigureAwait(false);
                    var matching = scan.Run(token).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    Publish(type, needle, matching, token);
                }
                catch (OperationCanceledException)
                {
                    // Superseded by a later query; the scan that replaced it owns the result.
                }
                catch (Exception ex)
                {
                    PublishFailure(type, ex, token);
                }
            },
            token);
    }

    /// <summary>
    /// The items matching a query: resref or name immediately, and content once the scan for exactly this
    /// query and type has landed - so the single search box never blanks a result it already knows.
    /// </summary>
    public IReadOnlyList<ExplorerItem> Filter(
        ModuleResourceType type, IReadOnlyList<ExplorerItem> items, string needle)
    {
        if (!Supports(type) ||
            _hits == null ||
            _hitsType != type ||
            !string.Equals(_hitsQuery, needle, StringComparison.OrdinalIgnoreCase))
        {
            return items.Where(item => IsOrdinaryMatch(item, needle)).ToList();
        }

        var hits = _hits;
        return items.Where(item => IsOrdinaryMatch(item, needle) || hits.Contains(item.ResRef)).ToList();
    }

    public void Dispose()
    {
        _scan?.Cancel();
        _scan?.Dispose();
        _scan = null;
    }

    internal static bool IsOrdinaryMatch(ExplorerItem item, string needle) =>
        item.ResRef.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
        (item.Name?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);

    private void Publish(ModuleResourceType type, string needle, HashSet<string> matching, CancellationToken token)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (token.IsCancellationRequested)
                return;

            _hitsType = type;
            _hitsQuery = needle;
            _hits = matching;
            _setSearching(false);
            _published();
        });
    }

    private void PublishFailure(ModuleResourceType type, Exception exception, CancellationToken token)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (token.IsCancellationRequested)
                return;

            _setSearching(false);
            _failed(_search!.FailureMessage(type, exception));
        });
    }
}
