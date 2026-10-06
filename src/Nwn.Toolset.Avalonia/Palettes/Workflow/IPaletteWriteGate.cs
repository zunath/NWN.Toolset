namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>
/// Whether a module-wide operation (pack, validation, build) currently forbids writes. Shared with every
/// other panel that writes to the module, so they grey out together.
/// </summary>
public interface IPaletteWriteGate
{
    /// <summary>Raised when <see cref="IsLocked"/> flips.</summary>
    event Action? Changed;

    bool IsLocked { get; }
}
