namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>Where the generator reports diagnostics. Hosts without one pass <see cref="NullAreaGenerationLog"/>.</summary>
public interface IAreaGenerationLog
{
    void Information(string message);

    void Warning(string message);

    void Error(Exception exception, string message);
}
