namespace Nwn.Toolset.Avalonia.Explorer;

/// <summary>What a row in the Module Contents tree stands for.</summary>
public enum ExplorerNodeKind
{
    /// <summary>A resource type. A tab rather than a row; kept so a host could show types as roots.</summary>
    Type,

    /// <summary>A folder from the category sidecar, or the synthetic Unsorted bucket.</summary>
    Group,

    /// <summary>One resource.</summary>
    Resource
}
