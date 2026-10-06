namespace Nwn.Authoring.Resources;

/// <summary>Identifies the configured layer, concrete container path, and entry that supplied a
/// resource.</summary>
public sealed record ResourceProvenance(
    string LayerName,
    ResourceLayerKind LayerKind,
    string SourcePath,
    string? ContainerPath,
    string Entry);
