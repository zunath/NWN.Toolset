using Nwn.Toolset.Avalonia.Areas.Generation;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Records the module-session calls the Area Generator launcher makes, in order.</summary>
public sealed class RecordingGeneratorSession : IAreaGeneratorSession
{
    public bool SaveSucceeds { get; set; } = true;

    public List<string> Events { get; } = new();

    public List<string> OpenedAreas { get; } = new();

    public int OpenScopes { get; private set; }

    public Task<bool> SaveOpenEditorsAsync()
    {
        Events.Add("save");
        return Task.FromResult(SaveSucceeds);
    }

    public IDisposable AllowModuleWrites()
    {
        Events.Add("open");
        OpenScopes++;
        return new Scope(this);
    }

    public Task OpenCreatedAreaAsync(string resRef)
    {
        OpenedAreas.Add(resRef);
        return Task.CompletedTask;
    }

    private sealed class Scope(RecordingGeneratorSession owner) : IDisposable
    {
        public void Dispose()
        {
            owner.Events.Add("close");
            owner.OpenScopes--;
        }
    }
}
