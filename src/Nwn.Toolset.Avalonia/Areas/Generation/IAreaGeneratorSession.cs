namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>The host's module state around one Area Generator run.</summary>
public interface IAreaGeneratorSession
{
    /// <summary>Saves every open editor so the generator writes against what is on disk. Returns false when one cannot be saved.</summary>
    Task<bool> SaveOpenEditorsAsync();

    /// <summary>Opens a scope in which the generator may write the module; the host's module write guard releases when it is disposed.</summary>
    IDisposable AllowModuleWrites();

    /// <summary>Registers the created area with the host's catalogs and opens it.</summary>
    Task OpenCreatedAreaAsync(string resRef);
}
