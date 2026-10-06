using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Doors;

/// <summary>Reads and writes the native fields that select a door appearance.</summary>
public static class DoorAppearanceValueStore
{
    public static DoorAppearanceSelection Read(BehaviorValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var specific = store.GetInteger(
            BehaviorFieldStorage.Field,
            GetFieldName(DoorAppearanceField.SpecificAppearance)) ?? 0;
        if (specific > 0)
            return new DoorAppearanceSelection(DoorAppearanceKind.Specific, specific);

        var generic = store.GetInteger(
                          BehaviorFieldStorage.Field,
                          GetFieldName(DoorAppearanceField.GenericTypeNew))
                      ?? store.GetInteger(
                          BehaviorFieldStorage.Field,
                          GetFieldName(DoorAppearanceField.LegacyGenericType))
                      ?? 0;
        return new DoorAppearanceSelection(DoorAppearanceKind.Generic, generic);
    }

    public static void Write(BehaviorValueStore store, DoorAppearanceSelection selection)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.Kind is not (DoorAppearanceKind.Generic or DoorAppearanceKind.Specific))
        {
            throw new ArgumentOutOfRangeException(
                nameof(selection), selection.Kind, "Unknown door appearance kind.");
        }

        // Validate before touching either field so a failed id cannot leave a half-updated pair.
        JsonGffField.ValidateIntegerValue(GffFieldType.Dword, selection.Id);
        var specificField = GetFieldName(DoorAppearanceField.SpecificAppearance);
        var genericField = GetFieldName(DoorAppearanceField.GenericTypeNew);
        if (selection.Kind == DoorAppearanceKind.Generic)
        {
            store.SetInteger(BehaviorFieldStorage.Field, specificField, GffFieldType.Dword, 0);
            store.SetInteger(BehaviorFieldStorage.Field, genericField, GffFieldType.Dword, selection.Id);
        }
        else
        {
            store.SetInteger(BehaviorFieldStorage.Field, specificField, GffFieldType.Dword, selection.Id);
            store.SetInteger(BehaviorFieldStorage.Field, genericField, GffFieldType.Dword, 0);
        }
    }

    private static string GetFieldName(DoorAppearanceField field) => field switch
    {
        DoorAppearanceField.SpecificAppearance => "Appearance",
        DoorAppearanceField.LegacyGenericType => "GenericType",
        DoorAppearanceField.GenericTypeNew => "GenericType_New",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown native door appearance field.")
    };

    private enum DoorAppearanceField
    {
        SpecificAppearance,
        LegacyGenericType,
        GenericTypeNew
    }
}
