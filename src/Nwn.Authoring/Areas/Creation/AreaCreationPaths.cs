namespace Nwn.Authoring.Areas.Creation;

/// <summary>Host-resolved module paths for one area creation transaction.</summary>
public sealed record AreaCreationPaths(
    string ModuleRoot,
    string TemplateResRef,
    string TemplateArePath,
    string TemplateGitPath,
    string TemplateGicPath,
    string DestinationArePath,
    string DestinationGitPath,
    string DestinationGicPath,
    string ModuleIfoPath,
    string PendingMarkerPrefix)
{
    public string PendingMarkerPath(string resRef) =>
        Path.Combine(ModuleRoot, PendingMarkerPrefix + resRef + ".pending");
}
