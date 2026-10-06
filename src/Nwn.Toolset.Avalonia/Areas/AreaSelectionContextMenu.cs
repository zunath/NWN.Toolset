// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>The area selection menu, with selection state and actions supplied by the host.</summary>
public sealed class AreaSelectionContextMenu : ContextMenu
{
    public AreaSelectionContextMenu(IAreaSelectionContextMenuState state, AreaSelectionTexts? texts = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        texts ??= AreaSelectionTexts.English;
        DataContext = state;

        var name = new MenuItem { IsEnabled = false };
        name.Bind(MenuItem.HeaderProperty, new Binding(nameof(IAreaSelectionContextMenuState.SelectionName)));
        var glyph = new TextBlock
        {
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
        };
        glyph.Bind(TextBlock.TextProperty, new Binding(nameof(IAreaSelectionContextMenuState.SelectionGlyph)));
        glyph.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("AccentBrush"));
        name.Icon = glyph;

        var kind = new MenuItem { IsEnabled = false };
        kind.Bind(MenuItem.HeaderProperty, new Binding(nameof(IAreaSelectionContextMenuState.SelectionKindLabel)));
        kind.Bind(ToolTip.TipProperty, new Binding(nameof(IAreaSelectionContextMenuState.SelectionResRef)));

        Items.Add(name);
        Items.Add(kind);
        Items.Add(new Separator());
        Items.Add(CreateAction(texts.Get(AreaSelectionStringId.OpenProperties),
            texts.Get(AreaSelectionStringId.OpenPropertiesTooltip),
            nameof(IAreaSelectionContextMenuState.OpenPropertiesCommand),
            nameof(IAreaSelectionContextMenuState.CanOpenProperties)));
        Items.Add(CreateAction(texts.Get(AreaSelectionStringId.EditBlueprint),
            texts.Get(AreaSelectionStringId.EditBlueprintTooltip),
            nameof(IAreaSelectionContextMenuState.EditBlueprintCommand),
            nameof(IAreaSelectionContextMenuState.CanEditBlueprint)));
        Items.Add(CreateAction(texts.Get(AreaSelectionStringId.EditCopy),
            texts.Get(AreaSelectionStringId.EditCopyTooltip),
            nameof(IAreaSelectionContextMenuState.EditCopyCommand),
            nameof(IAreaSelectionContextMenuState.CanEditCopy)));
    }

    private static MenuItem CreateAction(string header, string tooltip, string commandProperty, string canExecuteProperty)
    {
        var item = new MenuItem { Header = header };
        ToolTip.SetTip(item, tooltip);
        item.Bind(MenuItem.CommandProperty, new Binding(commandProperty));
        item.Bind(MenuItem.IsEnabledProperty, new Binding(canExecuteProperty));
        return item;
    }
}
