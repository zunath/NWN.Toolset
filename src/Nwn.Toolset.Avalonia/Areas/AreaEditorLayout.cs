using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Nwn.Toolset.Avalonia.Localization;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Scene/Properties split shared by every host; hosts own the contents and document state.</summary>
public sealed class AreaEditorLayout : TabControl
{
    public static readonly StyledProperty<Control?> SceneProperty =
        AvaloniaProperty.Register<AreaEditorLayout, Control?>(nameof(Scene));
    public static readonly StyledProperty<Control?> PropertiesProperty =
        AvaloniaProperty.Register<AreaEditorLayout, Control?>(nameof(Properties));

    protected override Type StyleKeyOverride => typeof(TabControl);

    private readonly TabItem _scene;
    private readonly TabItem _properties;

    public Control? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public Control? Properties
    {
        get => GetValue(PropertiesProperty);
        set => SetValue(PropertiesProperty, value);
    }

    public AreaEditorLayout() : this(AreaEditorTexts.English) { }

    public AreaEditorLayout(AreaEditorTexts texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        _scene = new TabItem { Header = texts.Get(AreaEditorStringId.Scene) };
        _properties = new TabItem { Header = texts.Get(AreaEditorStringId.Properties) };
        Items.Add(_scene);
        Items.Add(_properties);
        SelectedIndex = 0;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SceneProperty) _scene.Content = Scene;
        if (change.Property == PropertiesProperty) _properties.Content = Properties;
    }
}
