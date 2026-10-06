// SPDX-License-Identifier: MIT
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Appearances;

/// <summary>Extracts PLT palette-row choices from the color fields of creature and item blueprints.</summary>
public static class ModelPaletteResolver
{
    private static readonly (int Layer, string Field)[] ItemLayers =
    [
        (ModelPaletteLayers.Metal1, "Metal1Color"),
        (ModelPaletteLayers.Metal2, "Metal2Color"),
        (ModelPaletteLayers.Cloth1, "Cloth1Color"),
        (ModelPaletteLayers.Cloth2, "Cloth2Color"),
        (ModelPaletteLayers.Leather1, "Leather1Color"),
        (ModelPaletteLayers.Leather2, "Leather2Color"),
    ];

    /// <summary>
    /// The skin, hair and tattoo layers come from <paramref name="creature"/>; the armor dye layers come
    /// from <paramref name="armor"/> when present.
    /// </summary>
    /// <param name="creature">The creature blueprint.</param>
    /// <param name="armor">The equipped chest armor, or null.</param>
    /// <param name="fillMissingLayers">
    /// True: every native layer is present and an absent color is row 0, which is what Aurora shows.
    /// False: the armor dye layers appear only when the armor sets them, and none appear without armor.
    /// </param>
    public static IReadOnlyDictionary<int, int> Resolve(
        JsonGffStruct creature,
        JsonGffStruct? armor,
        bool fillMissingLayers = true)
    {
        ArgumentNullException.ThrowIfNull(creature);

        var colors = new Dictionary<int, int>();
        if (fillMissingLayers)
        {
            for (var layer = 0; layer < ModelPaletteLayers.Count; layer++)
                colors[layer] = 0;
        }

        colors[ModelPaletteLayers.Skin] = creature.GetIntOrNull("Color_Skin") ?? 0;
        colors[ModelPaletteLayers.Hair] = creature.GetIntOrNull("Color_Hair") ?? 0;
        colors[ModelPaletteLayers.Tattoo1] = creature.GetIntOrNull("Color_Tattoo1") ?? 0;
        colors[ModelPaletteLayers.Tattoo2] = creature.GetIntOrNull("Color_Tattoo2") ?? 0;

        if (armor != null)
            AddItemLayers(colors, armor, fillMissingLayers);
        return colors;
    }

    /// <summary>The palette an item blueprint's own dye fields select, for the item's meshes.</summary>
    public static IReadOnlyDictionary<int, int> ResolveItem(JsonGffStruct item, bool fillMissingLayers = true)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (fillMissingLayers)
            return Resolve(item, item, true);

        var colors = new Dictionary<int, int>();
        AddItemLayers(colors, item, false);
        return colors;
    }

    private static void AddItemLayers(Dictionary<int, int> colors, JsonGffStruct item, bool fillMissingLayers)
    {
        foreach (var (layer, field) in ItemLayers)
        {
            if (item.GetIntOrNull(field) is { } value)
                colors[layer] = value;
            else if (fillMissingLayers)
                colors[layer] = 0;
        }
    }
}
