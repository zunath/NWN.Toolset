namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>A log that discards everything.</summary>
public sealed class NullAreaGenerationLog : IAreaGenerationLog
{
    public static NullAreaGenerationLog Instance { get; } = new();

    private NullAreaGenerationLog()
    {
    }

    public void Information(string message)
    {
    }

    public void Warning(string message)
    {
    }

    public void Error(Exception exception, string message)
    {
    }
}
