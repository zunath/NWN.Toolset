using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Sounds;

/// <summary>Ambient-sound accessors over the shared behavior value store.</summary>
public class SoundBehaviorValueStore : BehaviorValueStore
{
    public const string SoundsField = "Sounds";
    public const string SoundEntryField = "Sound";

    public SoundBehaviorValueStore(JsonGffStruct sound) : base(sound)
    {
    }

    public JsonGffStruct Sound => ValueStruct;

    public IReadOnlyList<string> GetSounds() => GetResRefList(SoundsField, SoundEntryField);

    public void AddSound(string resRef) =>
        AddResRefListEntry(SoundsField, SoundEntryField, resRef);

    public void RemoveSound(int index) => RemoveListEntry(SoundsField, index);

    public void MoveSound(int fromIndex, int toIndex) =>
        MoveListEntry(SoundsField, fromIndex, toIndex);

    public void ReplaceSounds(IEnumerable<string> resRefs) =>
        ReplaceResRefList(SoundsField, SoundEntryField, resRefs);

    /// <summary>
    /// Clears what <paramref name="previous"/> owned before <paramref name="incoming"/> is
    /// applied.
    /// </summary>
    /// <remarks>
    /// The raw behavior owns every raw slot as one of its own editable fields, so leaving it is meant to
    /// erase everything - see the trigger and door editors for the same documented reset. Between
    /// two managed presets, though, a field can be one of both behaviors' own editable settings:
    /// Point Ambience and Area Ambience both expose Random, Interval, IntervalVrtn, PitchVariation,
    /// Volume and Times as their own fields. Only <see cref="BehaviorFieldDefinition"/> is a
    /// per-behavior editable slot, and only <see cref="BehaviorValueStore.Apply"/> (via <paramref name="incoming"/>'s
    /// <c>Manages</c>) writes anything back afterward, so clearing a field the incoming behavior also
    /// owns would discard a setting nothing then restores.
    /// </remarks>
    public void Clear(SoundBehavior previous, SoundBehavior incoming)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(incoming);

        if (previous.AllowsVariables)
        {
            Clear(previous.Manages, previous.Fields);
            return;
        }

        var keptByIncoming = new HashSet<string>(
            incoming.Fields.Select(field => field.Name), StringComparer.Ordinal);

        Clear(previous.Manages, previous.Fields.Where(field => !keptByIncoming.Contains(field.Name)));
    }
}
