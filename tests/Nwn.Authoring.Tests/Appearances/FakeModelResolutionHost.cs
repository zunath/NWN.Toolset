using Nwn.Authoring.Appearances;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Tests.Appearances;

/// <summary>An in-memory <see cref="IModelResolutionHost"/> whose tables a test fills directly.</summary>
internal sealed class FakeModelResolutionHost : IModelResolutionHost
{
    public HashSet<ModelResolutionTable> Tables { get; } = new(Enum.GetValues<ModelResolutionTable>());

    public Dictionary<int, CreatureAppearanceModelRow> Creatures { get; } = new();

    public Dictionary<int, ModelNameRow> Placeables { get; } = new();

    public Dictionary<int, ModelNameRow> Waypoints { get; } = new();

    public Dictionary<int, DoorModelRow> SpecificDoors { get; } = new();

    public Dictionary<int, DoorModelRow> GenericDoors { get; } = new();

    public Dictionary<int, BaseItemModelRow> BaseItems { get; } = new();

    public Dictionary<int, CloakModelRow> Cloaks { get; } = new();

    public Dictionary<string, JsonGffStruct> Blueprints { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Models { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool AllModelsExist { get; set; }

    public bool FillLayers { get; set; } = true;

    public string? Fallback { get; set; }

    public Func<JsonGffStruct, IReadOnlyDictionary<int, int>>? CompositePalette { get; set; }

    public Func<JsonGffStruct, string, int?>? ItemValueReader { get; set; }

    public Func<int, string, bool>? ShieldRule { get; set; }

    public Func<JsonGffStruct, JsonGffStruct?, IReadOnlyDictionary<int, int>, IReadOnlyList<ModelPartReference>>? Attachments { get; set; }

    public bool IsTableAvailable(ModelResolutionTable table) => Tables.Contains(table);

    public CreatureAppearanceModelRow? GetCreatureAppearance(int appearanceId) => Creatures.GetValueOrDefault(appearanceId);

    public ModelNameRow? GetPlaceable(int appearanceId) => Placeables.GetValueOrDefault(appearanceId);

    public ModelNameRow? GetWaypoint(int appearanceId) => Waypoints.GetValueOrDefault(appearanceId);

    public DoorModelRow? GetSpecificDoor(int appearanceId) => SpecificDoors.GetValueOrDefault(appearanceId);

    public DoorModelRow? GetGenericDoor(int genericType) => GenericDoors.GetValueOrDefault(genericType);

    public BaseItemModelRow? GetBaseItem(int baseItemId) => BaseItems.GetValueOrDefault(baseItemId);

    public CloakModelRow? GetCloak(int appearance) => Cloaks.TryGetValue(appearance, out var row) ? row : null;

    public JsonGffStruct? LoadItemBlueprint(string resRef) => Blueprints.GetValueOrDefault(resRef);

    public bool PartModelExists(string modelResRef) => AllModelsExist || Models.Contains(modelResRef);

    public int? ReadItemAppearanceValue(JsonGffStruct item, string field) =>
        ItemValueReader is null ? item.GetIntOrNull(field) : ItemValueReader(item, field);

    public bool IsShield(int baseItemId, string itemClass) =>
        ShieldRule?.Invoke(baseItemId, itemClass) ?? baseItemId is 14 or 56 or 57;

    public bool FillMissingPaletteLayers => FillLayers;

    public string? FallbackItemModelResRef => Fallback;

    public IReadOnlyDictionary<int, int> GetCompositeItemPalette(JsonGffStruct item) =>
        CompositePalette?.Invoke(item) ?? new Dictionary<int, int>();

    public IReadOnlyList<ModelPartReference> GetCreatureAttachments(
        JsonGffStruct creature, JsonGffStruct? armor, IReadOnlyDictionary<int, int> layerColorIndices) =>
        Attachments?.Invoke(creature, armor, layerColorIndices) ?? Array.Empty<ModelPartReference>();
}
