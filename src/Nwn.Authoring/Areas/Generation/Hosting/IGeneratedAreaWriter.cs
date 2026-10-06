namespace Nwn.Authoring.Areas.Generation.Hosting;

/// <summary>The host's transaction for creating a generated area as a normal area in its open module.</summary>
public interface IGeneratedAreaWriter
{
    /// <summary>Creates the area. Returns false with a user-presentable reason when it cannot be created.</summary>
    bool TryCreate(GeneratedAreaRequest request, out string error);
}
