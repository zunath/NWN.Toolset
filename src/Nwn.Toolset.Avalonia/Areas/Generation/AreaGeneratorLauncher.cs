using Avalonia.Controls;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>
/// Runs the Area Generator the way both hosts need it: editors saved first, the module writable for as long as the
/// window is open, and the created area registered and opened afterward.
/// </summary>
public sealed class AreaGeneratorLauncher
{
    private readonly IAreaGeneratorSession _session;
    private readonly Func<Task<AreaGeneratorHost>> _hostFactory;

    /// <param name="session">The host's module state.</param>
    /// <param name="hostFactory">Builds the generator host once the editors are saved, so it reflects the module on disk. It runs inside the module write scope.</param>
    public AreaGeneratorLauncher(IAreaGeneratorSession session, Func<Task<AreaGeneratorHost>> hostFactory)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _hostFactory = hostFactory ?? throw new ArgumentNullException(nameof(hostFactory));
    }

    /// <summary>Saves open editors, shows the generator over <paramref name="owner"/>, and opens the area it creates.</summary>
    public async Task<AreaGeneratorLaunchResult> LaunchAsync(Window owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        using (_session.AllowModuleWrites())
        {
            if (!await _session.SaveOpenEditorsAsync().ConfigureAwait(true))
                return new AreaGeneratorLaunchResult(AreaGeneratorOutcome.SaveFailed);
        }

        string? createdResRef;
        using (_session.AllowModuleWrites())
        {
            var host = await _hostFactory().ConfigureAwait(true);
            createdResRef = await AreaGeneratorWindow.ShowAsync(owner, host).ConfigureAwait(true);
        }

        if (string.IsNullOrWhiteSpace(createdResRef))
            return new AreaGeneratorLaunchResult(AreaGeneratorOutcome.Closed);

        await _session.OpenCreatedAreaAsync(createdResRef).ConfigureAwait(true);
        return new AreaGeneratorLaunchResult(AreaGeneratorOutcome.Created, createdResRef);
    }
}
