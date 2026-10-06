using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Nwn.Toolset.Avalonia.Areas.Properties.Views;

/// <summary>
/// The area Properties page: area property groups above one expandable section per placed-instance
/// list, each with Add/Duplicate/Delete, its instance grid and the selected instance's editor.
/// </summary>
public sealed partial class AreaPropertiesPageView : UserControl
{
    public AreaPropertiesPageView() => InitializeComponent();

    /// <summary>Scrolls the host's Properties area to <paramref name="section"/>'s card, if it is shown.</summary>
    public bool BringSectionIntoView(AreaInstanceSectionViewModel section)
    {
        ArgumentNullException.ThrowIfNull(section);
        var card = this.GetVisualDescendants()
            .OfType<Expander>()
            .FirstOrDefault(expander => ReferenceEquals(expander.DataContext, section));
        card?.BringIntoView();
        return card != null;
    }

    private void OnInstanceGridAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not DataGrid { DataContext: AreaInstanceSectionViewModel section } grid)
            return;

        for (var index = 0; index < grid.Columns.Count && index < section.ColumnHeaders.Count; index++)
            grid.Columns[index].Header = section.ColumnHeaders[index];
    }
}
