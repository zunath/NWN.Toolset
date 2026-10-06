using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePaletteWriteGate : IPaletteWriteGate
{
    public event Action? Changed;

    public bool IsLocked { get; private set; }

    public void Set(bool isLocked)
    {
        IsLocked = isLocked;
        Changed?.Invoke();
    }
}
