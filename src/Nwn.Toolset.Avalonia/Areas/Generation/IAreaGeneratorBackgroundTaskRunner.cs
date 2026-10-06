namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>Runs CPU- and I/O-heavy area-generator phases away from the UI thread.</summary>
public interface IAreaGeneratorBackgroundTaskRunner
{
    Task<T> RunAsync<T>(Func<T> operation);
}
