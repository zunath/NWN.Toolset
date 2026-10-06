using System.Globalization;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Doors;

/// <summary>
/// Door storage layered over the shared blueprint/placement value store: lock, key and transition
/// consistency, behavior application and clearing, and the module's closer and key-item conventions.
/// </summary>
public class DoorBehaviorValueStore : BehaviorValueStore
{
    public DoorBehaviorValueStore(JsonGffStruct door, DoorScriptConventions conventions)
        : base(door)
    {
        Conventions = conventions ?? throw new ArgumentNullException(nameof(conventions));
    }

    public DoorScriptConventions Conventions { get; }

    public JsonGffStruct Door => Owner;

    public bool HasRequiredKeyItemLocals =>
        Conventions.RequiredKeyItemPrefix is { } prefix &&
        Locals.Any(entry => entry.Name.StartsWith(prefix, StringComparison.Ordinal));

    public IReadOnlyList<int> GetRequiredKeyItemIds()
    {
        if (Conventions.RequiredKeyItemPrefix is not { } prefix)
            return Array.Empty<int>();

        return Locals
            .Select(entry => new
            {
                Entry = entry,
                Index = ParseRequiredKeyItemIndex(prefix, entry.Name)
            })
            .Where(item => item.Index.HasValue && item.Entry.IntValue.HasValue)
            .OrderBy(item => item.Index)
            .Select(item => item.Entry.IntValue!.Value)
            .ToList();
    }

    public void SetRequiredKeyItemIds(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var prefix = Conventions.RequiredKeyItemPrefix
            ?? throw new InvalidOperationException("This module declares no required key-item locals.");
        ClearRequiredKeyItemLocals();

        var index = 1;
        foreach (var id in ids)
            Locals.SetInt(prefix + (index++).ToString(CultureInfo.InvariantCulture), id);
    }

    public void ClearRequiredKeyItemLocals()
    {
        if (Conventions.RequiredKeyItemPrefix is not { } prefix)
            return;

        var names = Locals
            .Where(entry => entry.Name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(entry => entry.Name)
            .ToList();

        foreach (var name in names)
            Locals.Remove(name);
    }

    public bool IsSelfClosing =>
        Conventions.IsKnownCloser(GetString(BehaviorFieldStorage.Field, "OnOpen"));

    public void SetSelfClosing(bool enabled)
    {
        var current = GetString(BehaviorFieldStorage.Field, "OnOpen");
        if (enabled)
        {
            if (!Conventions.IsKnownCloser(current) && Conventions.DefaultCloser is { } closer)
                SetString(BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, closer);
        }
        else if (Conventions.IsKnownCloser(current))
        {
            SetString(BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, string.Empty);
        }
    }

    public void Apply(DoorBehavior behavior, bool isInstance)
    {
        ArgumentNullException.ThrowIfNull(behavior);

        foreach (var value in behavior.Manages)
            Apply(value, isInstance);

        if (behavior.KeyRequiredRule == DoorKeyRequiredRule.FromKeyTag)
            UpdateKeyRequired();
        else if (behavior.KeyRequiredRule == DoorKeyRequiredRule.FromKeyTagWhenLocked)
        {
            if (GetInteger(BehaviorFieldStorage.Field, "Locked") == 1)
                UpdateKeyRequired();
            else
                SetInteger(BehaviorFieldStorage.Field, "KeyRequired", GffFieldType.Byte, 0);
        }
    }

    /// <summary>
    /// Locals <see cref="Clear(DoorBehavior)"/> would remove beyond the behavior's named fields.
    /// </summary>
    /// <remarks>
    /// The raw behavior sweeps the entire table, and any behavior with owned prefixes sweeps everything
    /// under them. Neither set is derivable from the behavior's field list, so a caller that
    /// wants to warn the builder before the sweep has to ask for it here rather than reconstruct
    /// the rule and get it subtly wrong.
    /// </remarks>
    public static IReadOnlyList<string> LocalsClearedBySwitchingFrom(
        DoorBehaviorValueStore store, DoorBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(behavior);

        var names = store.Locals.Select(entry => entry.Name);

        if (!behavior.AllowsVariables)
        {
            names = names.Where(name => behavior.OwnedLocalPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.Ordinal)));
        }

        return names.ToList();
    }

    public void Clear(DoorBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        var closer = behavior.Fields.Any(field => field.Name == "OnOpen") &&
                     Conventions.IsKnownCloser(GetString(BehaviorFieldStorage.Field, "OnOpen"))
            ? GetString(BehaviorFieldStorage.Field, "OnOpen")
            : null;
        var defaultDeath = Conventions.DefaultDeathScript is { } deathScript &&
                           behavior.Fields.Any(field => field.Name == "OnDeath") &&
                           string.Equals(
                               GetString(BehaviorFieldStorage.Field, "OnDeath"),
                               deathScript,
                               StringComparison.OrdinalIgnoreCase)
            ? GetString(BehaviorFieldStorage.Field, "OnDeath")
            : null;

        Clear(behavior.Manages, behavior.Fields);

        if (behavior.AllowsVariables)
        {
            foreach (var name in Locals.Select(entry => entry.Name).ToList())
                Locals.Remove(name);
        }

        foreach (var prefix in behavior.OwnedLocalPrefixes)
        {
            var names = Locals
                .Where(entry => entry.Name.StartsWith(prefix, StringComparison.Ordinal))
                .Select(entry => entry.Name)
                .ToList();
            foreach (var name in names)
                Locals.Remove(name);
        }

        if (closer != null)
            SetString(BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, closer);
        if (defaultDeath != null)
            SetString(BehaviorFieldStorage.Field, "OnDeath", GffFieldType.ResRef, defaultDeath);
    }

    public void UpdateKeyRequired()
    {
        var key = GetString(BehaviorFieldStorage.Field, "KeyName");
        SetInteger(
            BehaviorFieldStorage.Field,
            "KeyRequired",
            GffFieldType.Byte,
            string.IsNullOrWhiteSpace(key) ? 0 : 1);
    }

    public void ClearConditionalLockFields(IEnumerable<DoorFieldDefinition> fields)
    {
        foreach (var field in fields.Where(field =>
                     string.Equals(field.VisibleWhenField, "Locked", StringComparison.Ordinal)))
        {
            ClearOne(field.Storage, field.Name, field.FieldType);
        }

        SetInteger(BehaviorFieldStorage.Field, "KeyRequired", GffFieldType.Byte, 0);
    }

    private static int? ParseRequiredKeyItemIndex(string prefix, string name)
    {
        if (!name.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        return int.TryParse(
            name[prefix.Length..],
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var index)
            ? index
            : null;
    }
}
