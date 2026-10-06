using System.Collections.ObjectModel;
using Nwn.Authoring.Documents.Native;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// One node of a browsed palette tree: either a category or a blueprint leaf.
/// </summary>
public sealed class PaletteNodeViewModel
{
    public string? ResRef { get; }
    public bool IsLeaf => ResRef != null;
    public string DisplayName { get; }
    public ObservableCollection<PaletteNodeViewModel> Children { get; } = new();

    /// <param name="node">The palette entry this node wraps.</param>
    /// <param name="resolveStrRef">
    /// Resolves an entry that names itself by STRREF rather than NAME. Module palettes lean on this
    /// heavily, so without a resolver the picker lists numbers where the rest of the toolset lists
    /// names.
    /// </param>
    /// <param name="texts">The placeholder captions for unnamed entries.</param>
    public PaletteNodeViewModel(PaletteNode node, Func<uint, string?>? resolveStrRef, AreaPropertiesTexts? texts = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        texts ??= AreaPropertiesTexts.English;
        ResRef = string.IsNullOrWhiteSpace(node.ResRef) ? null : node.ResRef.Trim();

        var resolved = node.StrRef.HasValue ? resolveStrRef?.Invoke(node.StrRef.Value) : null;
        DisplayName = !string.IsNullOrWhiteSpace(node.Name)
            ? node.Name
            : !string.IsNullOrWhiteSpace(resolved)
                ? resolved
                : ResRef ?? (node.StrRef.HasValue
                    ? texts.Get(AreaPropertiesStringId.UnnamedStrRef, node.StrRef.Value)
                    : texts.Get(AreaPropertiesStringId.UnnamedNode));

        // DELETE_ME entries are tombstones Aurora leaves behind; the game ignores them and so
        // does the category importer, so offering one here would place a blueprint the module
        // has already retired.
        foreach (var child in node.Children.Where(candidate => candidate.DeleteMe != true))
            Children.Add(new PaletteNodeViewModel(child, resolveStrRef, texts));
    }
}
