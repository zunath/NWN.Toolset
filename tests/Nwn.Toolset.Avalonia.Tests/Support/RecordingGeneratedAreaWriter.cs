using Nwn.Authoring.Areas.Generation.Hosting;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Captures the creation requests the Area Generator hands to its host.</summary>
public sealed class RecordingGeneratedAreaWriter : IGeneratedAreaWriter
{
    public List<GeneratedAreaRequest> Requests { get; } = new();

    public string FailureReason { get; set; } = string.Empty;

    public bool TryCreate(GeneratedAreaRequest request, out string error)
    {
        Requests.Add(request);
        error = FailureReason;
        return string.IsNullOrEmpty(FailureReason);
    }
}
