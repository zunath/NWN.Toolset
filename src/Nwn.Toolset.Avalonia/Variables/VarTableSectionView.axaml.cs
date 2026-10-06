using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace Nwn.Toolset.Avalonia.Variables;
/// <summary>Shared local-variable table editor.</summary>
public partial class VarTableSectionView : UserControl
{
    public VarTableSectionView() => AvaloniaXamlLoader.Load(this);
}
