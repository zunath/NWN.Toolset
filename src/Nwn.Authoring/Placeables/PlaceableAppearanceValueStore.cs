using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Placeables;

/// <summary>Reads and writes the native placeable blueprint appearance row.</summary>
public static class PlaceableAppearanceValueStore
{
    private const string AppearanceFieldName = "Appearance";

    public static long Read(BehaviorValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store.GetInteger(BehaviorFieldStorage.Field, AppearanceFieldName) ?? 0;
    }

    public static void Write(BehaviorValueStore store, long rowIndex)
    {
        ArgumentNullException.ThrowIfNull(store);
        var field = store.ValueStruct.GetOrNull(AppearanceFieldName);
        var fieldType = field?.Type ?? GffFieldType.Dword;
        store.SetInteger(BehaviorFieldStorage.Field, AppearanceFieldName, fieldType, rowIndex);
    }
}
