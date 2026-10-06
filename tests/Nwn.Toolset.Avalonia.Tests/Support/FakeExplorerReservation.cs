namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeExplorerReservation : IDisposable
{
    private Action? _release;

    public FakeExplorerReservation(Action release)
    {
        _release = release;
    }

    public void Dispose()
    {
        _release?.Invoke();
        _release = null;
    }
}
