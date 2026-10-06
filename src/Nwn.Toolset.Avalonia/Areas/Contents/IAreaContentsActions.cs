namespace Nwn.Toolset.Avalonia.Areas.Contents;
/// <summary>Host-owned operations invoked by the reusable Area Contents view.</summary>
public interface IAreaContentsActions
{
    void Select(AreaContentsIdentity identity);
    void Frame(AreaContentsIdentity identity);
    void OpenProperties(AreaContentsIdentity identity);
    Task<bool> DeleteAsync(IReadOnlyList<AreaContentsIdentity> identities, string displayName, CancellationToken cancellationToken = default);
}



