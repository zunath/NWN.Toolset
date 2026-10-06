using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.Native;
using Nwn.Toolset.Avalonia.Palettes.Workflow;
using Nwn.Toolset.Avalonia.Variables;

namespace Nwn.Toolset.Avalonia.Behaviors;

/// <summary>What every behavior-shaped door, waypoint and sound editor takes from its host.</summary>
/// <remarks>
/// Prompts and the log are the palette's interfaces: a host implements each once and passes the same
/// adapter to the palette, Module Contents and these editors.
/// </remarks>
public abstract class BehaviorEditorHost
{
    /// <summary>Resolves a field's game-data choices by its <see cref="BehaviorFieldDefinition.ChoicesKey"/>.</summary>
    public Func<string, IReadOnlyList<BehaviorChoice>>? ResolveChoices { get; init; }

    /// <summary>Renders choice artwork for picture galleries.</summary>
    public IBehaviorChoicePreviewProvider? ChoicePreviews { get; init; }

    /// <summary>Builds the raw behavior's local-variable editor; the plain editor when null.</summary>
    public IVarTableSectionFactory? Variables { get; init; }

    /// <summary>Confirms a behavior switch that discards data; switches without asking when null.</summary>
    public IPalettePrompts? Prompts { get; init; }

    /// <summary>Receives failures that would otherwise vanish with a fire-and-forget command.</summary>
    public IPaletteLog? Log { get; init; }

    /// <summary>The editors' captions, prompts and statuses.</summary>
    public BehaviorEditorTexts Texts { get; init; } = BehaviorEditorTexts.English;

    /// <summary>Builds the raw behavior's local-variable editor.</summary>
    public VarTableSectionViewModel CreateVariables(Func<string, Action, bool> runEdit, VarTable table) =>
        Variables?.Create(runEdit, table) ?? new VarTableSectionViewModel(runEdit, table);

    /// <summary>The game-data choices for <paramref name="definition"/>.</summary>
    public IReadOnlyList<BehaviorChoice> ChoicesFor(BehaviorFieldDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.ChoicesKey == null)
            return definition.Choices;

        return ResolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
    }
}
