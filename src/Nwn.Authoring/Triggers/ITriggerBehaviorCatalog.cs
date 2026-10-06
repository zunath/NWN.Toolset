using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Triggers;

/// <summary>A host's trigger behaviors and the rows every behavior shares.</summary>
public interface ITriggerBehaviorCatalog
{
    /// <summary>Every behavior, in rail order.</summary>
    IReadOnlyList<TriggerBehavior> All { get; }

    /// <summary>The raw behavior a trigger falls back to.</summary>
    TriggerBehavior Custom { get; }

    /// <summary>The fixed Basic rows shown for every behavior.</summary>
    IReadOnlyList<BehaviorFieldDefinition> BasicFields { get; }

    /// <summary>The behavior an existing trigger already plays.</summary>
    TriggerBehavior Classify(JsonGffStruct trigger);
}
