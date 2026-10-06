using Nwn.Toolset.Avalonia.Areas.Generation;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Runs generator phases on a worker thread and counts them by result type.</summary>
public sealed class InlineBackgroundTaskRunner : IAreaGeneratorBackgroundTaskRunner
{
    private readonly List<Type> _operationTypes = new();

    public IReadOnlyList<Type> OperationTypes
    {
        get
        {
            lock (_operationTypes)
                return _operationTypes.ToArray();
        }
    }

    public Task<T> RunAsync<T>(Func<T> operation)
    {
        lock (_operationTypes)
            _operationTypes.Add(typeof(T));
        return Task.Run(operation);
    }
}
