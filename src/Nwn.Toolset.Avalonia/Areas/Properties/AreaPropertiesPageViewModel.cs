using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Fields;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>
/// The area document's Properties page: the native area property groups above one expandable
/// section per placed-instance list.
/// </summary>
/// <remarks>
/// The page owns presentation state that belongs to the open document rather than its transient
/// view - which cards are expanded - so switching document tabs returns the builder to the page as
/// they left it. Document history, saving and scene selection stay with the host.
/// </remarks>
public partial class AreaPropertiesPageViewModel : ObservableObject
{
    private readonly IAreaPropertyChoiceSource? _choices;

    /// <summary>The page's captions.</summary>
    public AreaPropertiesTexts Texts { get; }

    public ObservableCollection<EditorGroup> AreaPropertyGroups { get; } = new();

    public ObservableCollection<AreaInstanceSectionViewModel> Sections { get; } = new();

    /// <summary>Whether the top-level Area Properties card is expanded in this open document.</summary>
    [ObservableProperty]
    private bool _areaPropertiesExpanded;

    public string AreaPropertiesHeader => Texts.Get(AreaPropertiesStringId.AreaPropertiesHeader);

    /// <param name="areaContext">The area (ARE) document and the host transaction its fields edit through.</param>
    /// <param name="texts">The page's captions; English when omitted.</param>
    /// <param name="choices">Game-data choices for numeric area properties; plain numbers when null.</param>
    /// <param name="policy">Fields the host leaves out or shows read-only.</param>
    public AreaPropertiesPageViewModel(
        EditorFieldContext areaContext,
        AreaPropertiesTexts? texts = null,
        IAreaPropertyChoiceSource? choices = null,
        AreaPropertyFieldPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(areaContext);
        Texts = texts ?? AreaPropertiesTexts.English;
        _choices = choices;
        foreach (var group in AreaPropertyFieldGroups.Create(areaContext, Texts, choices, policy))
            AreaPropertyGroups.Add(group);
    }

    /// <summary>The section caption for one catalog entry.</summary>
    public string SectionTitle(AreaInstanceSectionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Texts.Get(definition.Title);
    }

    /// <summary>The section listing <paramref name="type"/>, if the page has one.</summary>
    public AreaInstanceSectionViewModel? SectionFor(ModuleResourceType type) =>
        Sections.FirstOrDefault(section => section.BlueprintType == type);

    /// <summary>Reloads every area property field after undo, redo or an external reload.</summary>
    public void RefreshAreaPropertyFields()
    {
        foreach (var group in AreaPropertyGroups)
        foreach (var field in group.Fields)
            field.RefreshFromDocument();
    }

    /// <summary>Rebuilds every section's rows after an instance-list change.</summary>
    public void RefreshInstanceSections()
    {
        foreach (var section in Sections)
            section.RefreshFromDocument();
    }

    /// <summary>Re-resolves TLK-backed labels and choices after the host's tables change.</summary>
    public void RefreshTlkLabels()
    {
        var fields = AreaPropertyGroups.SelectMany(group => group.Fields).ToArray();
        foreach (var field in fields.OfType<LocStringFieldViewModel>())
            field.RefreshFromDocument();
        foreach (var field in fields.OfType<DropdownFieldViewModel>())
            field.RefreshOptions(AreaPropertyFieldGroups.OptionsFor(field.Descriptor.LookupKey, _choices));
        foreach (var section in Sections)
            section.RefreshTlkLabels();
    }

    /// <summary>Applies every section's save-time normalization; false refuses the save.</summary>
    public bool PrepareForSave()
    {
        foreach (var section in Sections)
        {
            if (!section.PrepareForSave())
                return false;
        }

        return true;
    }
}
