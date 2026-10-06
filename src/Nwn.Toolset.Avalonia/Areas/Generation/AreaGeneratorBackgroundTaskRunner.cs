namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>Default thread-pool implementation used by the Area Generator window.</summary>
public sealed class AreaGeneratorBackgroundTaskRunner : IAreaGeneratorBackgroundTaskRunner
{
    public Task<T> RunAsync<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Task.Run(operation);
    }
}
