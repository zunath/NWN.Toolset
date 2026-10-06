using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Deletes resources from the fake module, with switches for every refusal the flow handles.</summary>
internal sealed class FakeExplorerDeletion : IModuleExplorerDeletion
{
    private readonly FakeExplorerContentSource _content;

    public FakeExplorerDeletion(FakeExplorerContentSource content)
    {
        _content = content;
    }

    public string? PrepareRefusal { get; set; }

    public string? CommitFailure { get; set; }

    public bool ReservationRefused { get; set; }

    public int ActiveReservations { get; private set; }

    public IReadOnlyList<string> CleanupWarnings { get; set; } = Array.Empty<string>();

    public List<(ModuleResourceType Type, string ResRef)> DeletedNotices { get; } = new();

    public bool CanDelete(ModuleResourceType type) => true;

    public IModuleExplorerPreparedDeletion Prepare(ModuleResourceType type, string resRef)
    {
        if (PrepareRefusal is { } refusal)
            throw new InvalidOperationException(refusal);

        return new FakeExplorerPreparedDeletion(() =>
        {
            if (CommitFailure is { } failure)
                throw new IOException(failure);

            _content.Remove(type, resRef);
            return new ModuleExplorerDeletionResult(new[] { resRef + ".file" }, CleanupWarnings);
        });
    }

    public IDisposable? TryReserve()
    {
        if (ReservationRefused)
            return null;

        ActiveReservations++;
        return new FakeExplorerReservation(() => ActiveReservations--);
    }

    public void Deleted(ModuleResourceType type, string resRef) => DeletedNotices.Add((type, resRef));
}
