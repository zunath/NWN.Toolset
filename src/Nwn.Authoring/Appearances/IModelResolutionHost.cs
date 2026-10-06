// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Documents.Native;

namespace Nwn.Authoring.Appearances;

/// <summary>
/// The host-supplied game data and policy <see cref="ModelReferenceResolver"/> reads. The required
/// members cover 2DA rows, blueprint loading and model existence; the members with default
/// implementations are optional extensions a host overrides to add its own rules.
/// </summary>
public interface IModelResolutionHost
{
    /// <summary>Whether a 2DA family is loaded. An unloaded family yields a <see cref="ModelReferenceKind.None"/> reference with an explanation.</summary>
    bool IsTableAvailable(ModelResolutionTable table);

    CreatureAppearanceModelRow? GetCreatureAppearance(int appearanceId);

    ModelNameRow? GetPlaceable(int appearanceId);

    ModelNameRow? GetWaypoint(int appearanceId);

    /// <summary>A doortypes.2da row, selected by a door's non-zero Appearance field.</summary>
    DoorModelRow? GetSpecificDoor(int appearanceId);

    /// <summary>A genericdoors.2da row, selected by a door's GenericType_New (or legacy GenericType) field.</summary>
    DoorModelRow? GetGenericDoor(int genericType);

    BaseItemModelRow? GetBaseItem(int baseItemId);

    /// <summary>The cloakmodel.2da row for a cloak appearance, or null when the table or row is absent.</summary>
    CloakModelRow? GetCloak(int appearance);

    /// <summary>Loads an item blueprint's root struct by resref; null when it does not exist.</summary>
    JsonGffStruct? LoadItemBlueprint(string resRef);

    /// <summary>Whether a body-part, equipment or attachment MDL resref resolves.</summary>
    bool PartModelExists(string modelResRef);

    /// <summary>
    /// Reads an item appearance field (ModelPart1-3, ArmorPart_*). Override when the host stores values
    /// larger than a byte in companion fields.
    /// </summary>
    int? ReadItemAppearanceValue(JsonGffStruct item, string field) => item.GetIntOrNull(field);

    /// <summary>Whether a base item is a shield, which attaches to the left hand as <c>shield</c> rather than <c>weaponl</c>.</summary>
    bool IsShield(int baseItemId, string itemClass) => baseItemId is 14 or 56 or 57;

    /// <summary>
    /// Whether palette layers a blueprint does not set resolve to row 0 (true) or are left out of the
    /// palette so the renderer applies its own default (false).
    /// </summary>
    bool FillMissingPaletteLayers => true;

    /// <summary>
    /// The palette a composite (ModelType 2) weapon's meshes use, taken from the item's own dye fields.
    /// The default is empty, so the renderer leaves a composite weapon's own surface colors alone; a host
    /// whose weapons are dye-tinted returns the layers its item blueprints set, such as
    /// <see cref="ModelPaletteResolver.ResolveItem"/> with <c>fillMissingLayers: false</c>.
    /// </summary>
    /// <param name="item">The composite weapon's item blueprint.</param>
    IReadOnlyDictionary<int, int> GetCompositeItemPalette(JsonGffStruct item) => new Dictionary<int, int>();

    /// <summary>
    /// The stand-in model shown for an item with no model of its own, such as the loot bag. Null means
    /// no stand-in: such an item resolves to <see cref="ModelReferenceKind.None"/>.
    /// </summary>
    string? FallbackItemModelResRef => null;

    /// <summary>
    /// Extra attachments the host draws on a creature after its equipment, such as wings and tails.
    /// </summary>
    /// <param name="creature">The creature blueprint.</param>
    /// <param name="armor">The equipped chest armor, or null.</param>
    /// <param name="layerColorIndices">The creature's resolved palette.</param>
    IReadOnlyList<ModelPartReference> GetCreatureAttachments(
        JsonGffStruct creature,
        JsonGffStruct? armor,
        IReadOnlyDictionary<int, int> layerColorIndices) => Array.Empty<ModelPartReference>();
}
