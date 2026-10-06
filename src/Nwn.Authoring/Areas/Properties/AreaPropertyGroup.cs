namespace Nwn.Authoring.Areas.Properties;

public sealed record AreaPropertyGroup(
    AreaPropertyGroupId Id,
    IReadOnlyList<AreaPropertyField> Fields);
