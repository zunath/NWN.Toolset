namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>How one Area Generator run ended.</summary>
public enum AreaGeneratorOutcome
{
    /// <summary>The builder closed the window without creating an area.</summary>
    Closed,

    /// <summary>An area was created and opened.</summary>
    Created,

    /// <summary>An open editor could not be saved, so the generator did not open.</summary>
    SaveFailed
}
