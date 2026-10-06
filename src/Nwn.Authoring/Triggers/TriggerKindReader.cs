using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Triggers;

/// <summary>Reads which native kind a trigger is from its <c>Type</c> and <c>TrapFlag</c> fields.</summary>
public static class TriggerKindReader
{
    /// <summary>The GFF field holding the trigger's <see cref="TriggerKind"/>.</summary>
    public const string TypeField = "Type";

    /// <summary>The GFF field marking a trigger as springing a trap.</summary>
    public const string TrapFlagField = "TrapFlag";

    /// <summary>
    /// A trap by type or by flag, an area transition by type, and a generic trigger otherwise. The trap
    /// flag wins over the type because a flagged trigger springs its trap whatever its type says.
    /// </summary>
    public static TriggerKind Read(JsonGffStruct trigger)
    {
        ArgumentNullException.ThrowIfNull(trigger);

        var store = new BehaviorValueStore(trigger);
        var type = store.GetInteger(BehaviorFieldStorage.Field, TypeField) ?? 0;
        var trapFlag = store.GetInteger(BehaviorFieldStorage.Field, TrapFlagField) ?? 0;
        if (type == (long)TriggerKind.Trap || trapFlag == 1)
            return TriggerKind.Trap;

        return type == (long)TriggerKind.AreaTransition ? TriggerKind.AreaTransition : TriggerKind.Generic;
    }
}
