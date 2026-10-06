using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Doors;

/// <summary>A host's door behaviors, the rows every behavior shares, and its door conventions.</summary>
public interface IDoorBehaviorCatalog
{
    /// <summary>Every behavior, in rail order.</summary>
    IReadOnlyList<DoorBehavior> All { get; }

    /// <summary>The raw behavior a door falls back to.</summary>
    DoorBehavior Custom { get; }

    /// <summary>The fixed Basic rows shown for every behavior.</summary>
    IReadOnlyList<DoorFieldDefinition> BasicFields { get; }

    /// <summary>The module's door script and local conventions.</summary>
    DoorScriptConventions Conventions { get; }

    /// <summary>The behavior an existing door already plays.</summary>
    DoorBehavior Classify(JsonGffStruct door);
}
