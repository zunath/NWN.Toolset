namespace Nwn.Authoring.Editing;

/// <summary>Supplies host policy and a write lease for file mutations.</summary>
public interface IFileWriteAccess
{
    void EnsureAllowed();

    IDisposable Acquire(string path, TimeSpan? timeout = null);
}
