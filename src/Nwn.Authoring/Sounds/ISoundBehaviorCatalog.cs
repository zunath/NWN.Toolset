using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Sounds;

/// <summary>A host's ambient-sound behaviors and the rows every behavior shares.</summary>
public interface ISoundBehaviorCatalog
{
    /// <summary>Every behavior, in rail order.</summary>
    IReadOnlyList<SoundBehavior> All { get; }

    /// <summary>The raw behavior a sound falls back to.</summary>
    SoundBehavior Custom { get; }

    /// <summary>The fixed Basic rows shown for every behavior.</summary>
    IReadOnlyList<BehaviorFieldDefinition> BasicFields { get; }

    /// <summary>The behavior an existing sound already plays.</summary>
    SoundBehavior Classify(JsonGffStruct sound);
}
