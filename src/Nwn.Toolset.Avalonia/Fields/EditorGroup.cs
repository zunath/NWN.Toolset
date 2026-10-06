namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>A titled group of field editors, rendered as one expandable section.</summary>
public sealed record EditorGroup(string Title, IReadOnlyList<FieldViewModel> Fields);
