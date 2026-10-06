using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Appearances;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Formats.Gff;
using NativeField = Nwn.Formats.Gff.GffField;

namespace Nwn.Authoring.Tests.Appearances;

[TestClass]
public sealed class ModelReferenceResolverTests
{
    private const int HumanAppearance = 6;
    private const int RobeBaseItem = 16;
    private const int HelmetBaseItem = 85;
    private const int WeaponBaseItem = 1;
    private const int SaberBaseItem = 2;
    private const int CloakBaseItem = 80;
    private const int SmallShieldBaseItem = 14;

    [TestMethod]
    public void SegmentedCreatureNamesBodyPartsFromGenderRacePhenotypeAndArmorOverrides()
    {
        var host = Host();
        host.Blueprints["armor_a"] = Native(new GffStruct(0).Add(NativeField.Int("BaseItem", RobeBaseItem)).Add(NativeField.Byte("ArmorPart_Torso", 5))
            .Add(NativeField.Byte("ArmorPart_Robe", 2)).Add(NativeField.Byte("Cloth1Color", 17)));
        var creature = Creature(gender: 1, phenotype: 2, equipment:
            [new GffStruct((uint)CreatureEquipmentSlot.Chest).Add(NativeField.ResRef("EquippedRes", "armor_a"))]);

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host);

        Assert.AreEqual(ModelReferenceKind.Segmented, reference.Kind);
        Assert.AreEqual("pfh2", reference.SkeletonResRef);
        var parts = reference.Parts.ToDictionary(part => part.PartType);
        Assert.AreEqual("pfh2_robe002", parts["robe"].ModelResRef);
        Assert.AreEqual("pfh2_head003", parts["head"].ModelResRef);
        Assert.AreEqual("pfh2_chest005", parts["chest"].ModelResRef, "The armor part replaces the creature's torso number.");
        Assert.IsTrue(parts["chest"].IsItemSupplied);
        Assert.IsTrue(parts["chest"].UsesItemTintOverrides);
        Assert.AreEqual("pfh2_footr001", parts["footr"].ModelResRef);
        Assert.IsFalse(parts["footr"].IsItemSupplied);
        Assert.IsTrue(parts["footr"].UsesItemTintOverrides, "A dressed creature's body parts all take the armor's tint overrides.");
        Assert.IsFalse(parts["head"].UsesItemTintOverrides);
        Assert.IsFalse(parts.ContainsKey("neck"), "A creature part of 0 means the body has no such part.");
        Assert.AreEqual(17, reference.LayerColorIndices[ModelPaletteLayers.Cloth1]);
        Assert.AreEqual(6, reference.LayerColorIndices[ModelPaletteLayers.Skin]);
        Assert.AreEqual(ModelPaletteLayers.Count, reference.LayerColorIndices.Count);
    }

    [TestMethod]
    public void MissingRobeModelIsOmittedButAllBodyPartsRemain()
    {
        var host = Host();
        host.AllModelsExist = false;
        host.Models.Add("pmh0_chest001");
        host.Blueprints["armor_a"] = Native(new GffStruct(0).Add(NativeField.Int("BaseItem", RobeBaseItem)).Add(NativeField.Byte("ArmorPart_Robe", 9)));
        var creature = Creature(equipment:
            [new GffStruct((uint)CreatureEquipmentSlot.Chest).Add(NativeField.ResRef("EquippedRes", "armor_a"))]);

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host);

        Assert.IsFalse(reference.Parts.Any(part => part.PartType == "robe"));
        Assert.IsTrue(reference.Parts.Any(part => part.PartType == "chest"));
    }

    [TestMethod]
    public void VisibleEquipmentAttachesHelmetShieldCompositeWeaponAndRetargetedCloak()
    {
        var host = Host();
        host.Cloaks[3] = new CloakModelRow(Model: 7, Texture: 9, HideLeftShoulder: true, HideRightShoulder: false);
        host.Models.Add("pmh0_cloak_007");
        var creature = Creature(equipment:
        [
            new GffStruct((uint)CreatureEquipmentSlot.Head).Add(NativeField.Int("BaseItem", HelmetBaseItem))
                .Add(NativeField.Byte("ModelPart1", 4)).Add(NativeField.Byte("Metal1Color", 5)),
            new GffStruct((uint)CreatureEquipmentSlot.RightHand).Add(NativeField.Int("BaseItem", SaberBaseItem))
                .Add(NativeField.Byte("ModelPart1", 1)).Add(NativeField.Byte("ModelPart2", 2)).Add(NativeField.Byte("ModelPart3", 3)),
            new GffStruct((uint)CreatureEquipmentSlot.LeftHand).Add(NativeField.Int("BaseItem", SmallShieldBaseItem))
                .Add(NativeField.Byte("ModelPart1", 6)),
            new GffStruct((uint)CreatureEquipmentSlot.Cloak).Add(NativeField.Int("BaseItem", CloakBaseItem))
                .Add(NativeField.Byte("ModelPart1", 3)),
        ]);

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host);

        var parts = reference.Parts;
        var helmet = parts.Single(part => part.PartType == "helmet");
        Assert.AreEqual("helm_004", helmet.ModelResRef);
        Assert.AreEqual(5, helmet.LayerColorIndices![ModelPaletteLayers.Metal1]);
        Assert.IsNotNull(helmet.TintSourceItem);
        CollectionAssert.AreEqual(
            new[] { "wswls_b_001", "wswls_m_002", "wswls_t_003" },
            parts.Where(part => part.PartType == "weaponr").Select(part => part.ModelResRef).ToArray());
        Assert.AreEqual("ashsw_006", parts.Single(part => part.PartType == "shield").ModelResRef);
        Assert.IsFalse(parts.Any(part => part.PartType == "weaponl"));
        var cloak = parts.Single(part => part.PartType == "cloak");
        Assert.AreEqual("pmh0_cloak_007", cloak.ModelResRef, "cloakmodel.2da's MODEL rewrites the geometry number.");
        Assert.AreEqual("pmh0_cloak_009", cloak.TextureResRef, "cloakmodel.2da's TEXTURE rewrites the surface number.");
        Assert.IsFalse(parts.Any(part => part.PartType == "shol"), "The cloak hides the left shoulder.");
        Assert.IsTrue(parts.Any(part => part.PartType == "shor"), "Only the shoulder the cloak mapping names is hidden.");
    }

    [TestMethod]
    public void CloakIsRetargetedFromTheMannequinToTheWearersPrefix()
    {
        var host = Host();
        host.Models.Add("pfe1_cloak_002");
        host.Models.Add("pmh0_cloak_002");
        var creature = Creature(appearance: 7, gender: 1, phenotype: 1, equipment:
            [new GffStruct((uint)CreatureEquipmentSlot.Cloak).Add(NativeField.Int("BaseItem", CloakBaseItem))
                .Add(NativeField.Byte("ModelPart1", 2))]);

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host);

        var cloak = reference.Parts.Single(part => part.PartType == "cloak");
        Assert.AreEqual("pfe1_cloak_002", cloak.ModelResRef);
        Assert.AreEqual("pfe1_cloak_002", cloak.TextureResRef);
    }

    [TestMethod]
    public void HostShieldRuleAndExtendedPartReaderOverrideTheDefaults()
    {
        var host = Host();
        host.ShieldRule = (_, itemClass) => itemClass == "wswss";
        host.ItemValueReader = (item, field) => field == "ModelPart1" ? 300 : item.GetIntOrNull(field);
        host.AllModelsExist = true;
        var creature = Creature(equipment:
            [new GffStruct((uint)CreatureEquipmentSlot.LeftHand).Add(NativeField.Int("BaseItem", WeaponBaseItem))
                .Add(NativeField.Byte("ModelPart1", 1))]);

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host);

        var shield = reference.Parts.Single(part => part.PartType == "shield");
        Assert.AreEqual("wswss_300", shield.ModelResRef);
    }

    [TestMethod]
    public void HostAttachmentsAreAppendedAfterEquipmentWithTheResolvedPalette()
    {
        var host = Host();
        IReadOnlyDictionary<int, int>? seen = null;
        host.Attachments = (_, armor, palette) =>
        {
            seen = palette;
            return [new ModelPartReference("wing", "wing_001", palette, UsesItemTintOverrides: armor != null)];
        };

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, Creature(), host);

        Assert.AreEqual("wing", reference.Parts[^1].PartType);
        Assert.AreSame(seen, reference.Parts[^1].LayerColorIndices);
        Assert.AreEqual(6, seen![ModelPaletteLayers.Skin]);
    }

    [TestMethod]
    public void SimpleCreatureResolvesItsRaceColumnAsTheModelAndKeepsEquipmentParts()
    {
        var host = Host();
        host.Creatures[40] = new CreatureAppearanceModelRow(40, "Rancor", "S", "c_rancor");
        var creature = Creature(appearance: 40, equipment:
            [new GffStruct((uint)CreatureEquipmentSlot.RightHand).Add(NativeField.Int("BaseItem", SaberBaseItem))
                .Add(NativeField.Byte("ModelPart1", 1))]);
        host.AllModelsExist = true;

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host);

        Assert.AreEqual(ModelReferenceKind.Simple, reference.Kind);
        Assert.AreEqual("c_rancor", reference.ModelResRef);
        Assert.AreEqual(3, reference.Parts.Count(part => part.PartType == "weaponr"));
    }

    [TestMethod]
    public void UnavailableTablesUnknownRowsAndEmptyColumnsExplainTheMissingModel()
    {
        var host = Host();
        host.Tables.Remove(ModelResolutionTable.CreatureAppearances);
        StringAssert.Contains(ModelReferenceResolver.Resolve(ModuleResourceType.Utc, Creature(), host).Status, "appearance data not loaded");
        host.Tables.Add(ModelResolutionTable.CreatureAppearances);

        StringAssert.Contains(ModelReferenceResolver.Resolve(ModuleResourceType.Utc, Creature(appearance: 999), host).Status, "Unknown appearance id 999");

        host.Creatures[41] = new CreatureAppearanceModelRow(41, "Blank", "S", null);
        StringAssert.Contains(ModelReferenceResolver.Resolve(ModuleResourceType.Utc, Creature(appearance: 41), host).Status, "no model ResRef");

        host.Creatures[42] = new CreatureAppearanceModelRow(42, "NoRace", "P", " ");
        StringAssert.Contains(ModelReferenceResolver.Resolve(ModuleResourceType.Utc, Creature(appearance: 42), host).Status, "no race letter");

        Assert.AreEqual(ModelReferenceKind.None, ModelReferenceResolver.Resolve(ModuleResourceType.Utm, Creature(), host).Kind);
    }

    [TestMethod]
    public void PlaceableAndWaypointResolveTheirRowModels()
    {
        var host = Host();
        host.Placeables[12] = new ModelNameRow("Crate", "plc_crate");
        host.Waypoints[3] = new ModelNameRow("Marker", "wp_marker");
        host.Waypoints[4] = new ModelNameRow("Empty", null);

        var placeable = ModelReferenceResolver.Resolve(ModuleResourceType.Utp, Native(new GffStruct(0).Add(NativeField.Dword("Appearance", 12))), host);
        var waypoint = ModelReferenceResolver.Resolve(ModuleResourceType.Utw, Native(new GffStruct(0).Add(NativeField.Dword("Appearance", 3))), host);
        var emptyWaypoint = ModelReferenceResolver.Resolve(ModuleResourceType.Utw, Native(new GffStruct(0).Add(NativeField.Dword("Appearance", 4))), host);
        var unknown = ModelReferenceResolver.Resolve(ModuleResourceType.Utp, Native(new GffStruct(0).Add(NativeField.Dword("Appearance", 13))), host);

        Assert.AreEqual("plc_crate", placeable.ModelResRef);
        Assert.AreEqual("Crate (plc_crate.mdl)", placeable.Status);
        Assert.AreEqual("wp_marker", waypoint.ModelResRef);
        Assert.AreEqual(ModelReferenceKind.None, emptyWaypoint.Kind);
        Assert.AreEqual(ModelReferenceKind.None, unknown.Kind);
    }

    [TestMethod]
    public void DoorUsesSpecificRowOverGenericAndFlagsHiddenTransitionModels()
    {
        var host = Host();
        host.SpecificDoors[7] = new DoorModelRow("Blast door", "door_blast", VisibleModel: true);
        host.GenericDoors[2] = new DoorModelRow("Generic", "door_generic", VisibleModel: true);
        host.GenericDoors[5] = new DoorModelRow("Transition", "door_plane", VisibleModel: false);

        var specific = ModelReferenceResolver.Resolve(ModuleResourceType.Utd,
            Native(new GffStruct(0).Add(NativeField.Dword("Appearance", 7)).Add(NativeField.Dword("GenericType_New", 2))), host);
        var generic = ModelReferenceResolver.Resolve(ModuleResourceType.Utd,
            Native(new GffStruct(0).Add(NativeField.Dword("Appearance", 0)).Add(NativeField.Dword("GenericType_New", 2))), host);
        var legacy = ModelReferenceResolver.Resolve(ModuleResourceType.Utd,
            Native(new GffStruct(0).Add(NativeField.Byte("GenericType", 5))), host);
        var unknown = ModelReferenceResolver.Resolve(ModuleResourceType.Utd,
            Native(new GffStruct(0).Add(NativeField.Dword("Appearance", 99))), host);

        var sentinel = ModelReferenceResolver.Resolve(ModuleResourceType.Utd,
            Native(new GffStruct(0).Add(NativeField.Dword("GenericType_New", uint.MaxValue))), host);

        Assert.AreEqual(ModelReferenceKind.None, sentinel.Kind, "A Dword sentinel is an unknown door type, not an error.");
        Assert.AreEqual("door_blast", specific.ModelResRef);
        Assert.IsFalse(specific.IsDoorTransition);
        Assert.AreEqual("door_generic", generic.ModelResRef);
        Assert.AreEqual("door_plane", legacy.ModelResRef);
        Assert.IsTrue(legacy.IsDoorTransition);
        StringAssert.Contains(unknown.Status, "Unknown specific door type 99");
    }

    [TestMethod]
    public void ItemModelTypesResolveGroundCompositeAndMannequinModels()
    {
        var host = Host();
        host.Models.Add("helm_004");
        host.Models.Add("wswls_m_002");
        host.Models.Add("pmh0_cloak_003");
        host.Cloaks[3] = new CloakModelRow(Model: 3, Texture: 8, HideLeftShoulder: false, HideRightShoulder: false);

        var ground = ModelReferenceResolver.Resolve(ModuleResourceType.Uti,
            Native(new GffStruct(0).Add(NativeField.Int("BaseItem", HelmetBaseItem)).Add(NativeField.Byte("ModelPart1", 4)).Add(NativeField.Byte("Leather1Color", 12))), host);
        var composite = ModelReferenceResolver.Resolve(ModuleResourceType.Uti,
            Native(new GffStruct(0).Add(NativeField.Int("BaseItem", SaberBaseItem)).Add(NativeField.Byte("ModelPart1", 1)).Add(NativeField.Byte("ModelPart2", 2))), host);
        var cape = ModelReferenceResolver.Resolve(ModuleResourceType.Uti,
            Native(new GffStruct(0).Add(NativeField.Int("BaseItem", CloakBaseItem)).Add(NativeField.Byte("ModelPart1", 3))), host, armorPreviewFemale: false);

        Assert.AreEqual("helm_004", ground.ModelResRef);
        Assert.IsTrue(ground.RootUsesItemTintOverrides);
        Assert.AreEqual(12, ground.LayerColorIndices[ModelPaletteLayers.Leather1]);
        Assert.AreEqual(ModelReferenceKind.ItemComposite, composite.Kind);
        CollectionAssert.AreEqual(new[] { "bottom", "middle", "top" }, composite.Parts.Select(part => part.PartType).ToArray());
        Assert.AreEqual("wswls_b_001", composite.Parts[0].ModelResRef);
        Assert.AreEqual(ModelReferenceKind.Segmented, cape.Kind);
        Assert.AreEqual("pmh0", cape.SkeletonResRef);
        Assert.AreEqual("pmh0_cloak_003", cape.Parts[0].ModelResRef);
        Assert.AreEqual("pmh0_cloak_008", cape.Parts[0].TextureResRef);
        Assert.IsFalse(cape.Parts.Any(part => part.PartType is "shol" or "shor"));
    }

    [TestMethod]
    public void CompositeWeaponPaletteIsEmptyUnlessTheHostSuppliesDyeLayers()
    {
        var host = Host();
        var weapon = new GffStruct(0).Add(NativeField.Int("BaseItem", SaberBaseItem)).Add(NativeField.Byte("ModelPart1", 1))
            .Add(NativeField.Byte("Metal1Color", 5)).Add(NativeField.Byte("Cloth2Color", 7));
        var creature = Creature(equipment: [new GffStruct((uint)CreatureEquipmentSlot.RightHand)
            .Add(NativeField.Int("BaseItem", SaberBaseItem)).Add(NativeField.Byte("ModelPart1", 1))
            .Add(NativeField.Byte("Metal1Color", 5)).Add(NativeField.Byte("Cloth2Color", 7))]);

        var undyed = ModelReferenceResolver.Resolve(ModuleResourceType.Uti, Native(weapon), host);
        var undyedAttachments = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host)
            .Parts.Where(part => part.PartType == "weaponr").ToArray();

        Assert.AreEqual(0, undyed.LayerColorIndices.Count, "The default hook leaves a composite weapon's palette empty.");
        Assert.AreEqual(3, undyedAttachments.Length);
        Assert.IsTrue(undyedAttachments.All(part => part.LayerColorIndices is { Count: 0 }));

        host.CompositePalette = item => ModelPaletteResolver.ResolveItem(item, fillMissingLayers: false);
        var dyed = ModelReferenceResolver.Resolve(ModuleResourceType.Uti, Native(weapon), host);
        var dyedAttachments = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host)
            .Parts.Where(part => part.PartType == "weaponr").ToArray();

        CollectionAssert.AreEquivalent(
            new[] { ModelPaletteLayers.Metal1, ModelPaletteLayers.Cloth2 }, dyed.LayerColorIndices.Keys.ToArray());
        Assert.AreEqual(5, dyed.LayerColorIndices[ModelPaletteLayers.Metal1]);
        Assert.AreEqual(3, dyedAttachments.Length);
        Assert.IsTrue(dyedAttachments.All(part => part.LayerColorIndices![ModelPaletteLayers.Cloth2] == 7));
    }

    [TestMethod]
    public void ArmorItemDressesTheSelectedMannequinAndLeavesShouldersBareUnlessWorn()
    {
        var host = Host();
        host.BaseItems[3] = new BaseItemModelRow("armor", 3);
        host.AllModelsExist = true;
        var armor = Native(new GffStruct(0).Add(NativeField.Int("BaseItem", 3)).Add(NativeField.Byte("ArmorPart_Torso", 4))
            .Add(NativeField.Byte("ArmorPart_LShoul", 2)).Add(NativeField.Byte("ArmorPart_Robe", 1)));

        var male = ModelReferenceResolver.Resolve(ModuleResourceType.Uti, armor, host);
        var female = ModelReferenceResolver.Resolve(ModuleResourceType.Uti, armor, host, armorPreviewFemale: true);

        Assert.AreEqual("pmh0", male.SkeletonResRef);
        Assert.AreEqual("pfh0", female.SkeletonResRef);
        var parts = male.Parts.ToDictionary(part => part.PartType);
        Assert.AreEqual("pmh0_chest004", parts["chest"].ModelResRef);
        Assert.AreEqual("pmh0_shol002", parts["shol"].ModelResRef);
        Assert.IsFalse(parts.ContainsKey("shor"));
        Assert.AreEqual("pmh0_belt001", parts["belt"].ModelResRef);
        Assert.AreEqual("pmh0_head001", parts["head"].ModelResRef);
        Assert.AreEqual("pmh0_robe001", parts["robe"].ModelResRef);
    }

    [TestMethod]
    public void ItemWithoutAModelUsesTheHostFallbackOrResolvesToNothing()
    {
        var host = Host(allModelsExist: false);
        var item = Native(new GffStruct(0).Add(NativeField.Int("BaseItem", HelmetBaseItem)).Add(NativeField.Byte("ModelPart1", 4)));

        var none = ModelReferenceResolver.Resolve(ModuleResourceType.Uti, item, host);
        host.Fallback = "it_bag";
        var missingBag = ModelReferenceResolver.Resolve(ModuleResourceType.Uti, item, host);
        host.Models.Add("it_bag");
        var bag = ModelReferenceResolver.Resolve(ModuleResourceType.Uti, item, host);

        Assert.AreEqual(ModelReferenceKind.None, none.Kind);
        Assert.AreEqual(ModelReferenceKind.None, missingBag.Kind);
        Assert.AreEqual("it_bag", bag.ModelResRef);
        Assert.IsTrue(bag.IsFallbackModel);
        Assert.AreEqual(ModelReferenceKind.None, ModelReferenceResolver.Resolve(ModuleResourceType.Uti,
            Native(new GffStruct(0).Add(NativeField.Int("BaseItem", 999))), host).Kind);
        host.Tables.Remove(ModelResolutionTable.BaseItems);
        StringAssert.Contains(ModelReferenceResolver.Resolve(ModuleResourceType.Uti, item, host).Status, "base item data not loaded");
    }

    [TestMethod]
    public void HeldFallbackModelsAreNotAttachedToACreature()
    {
        var host = Host(allModelsExist: false);
        host.Fallback = "it_bag";
        host.Models.Add("it_bag");
        var creature = Creature(equipment:
            [new GffStruct((uint)CreatureEquipmentSlot.RightHand).Add(NativeField.Int("BaseItem", WeaponBaseItem))
                .Add(NativeField.Byte("ModelPart1", 1))]);

        var reference = ModelReferenceResolver.Resolve(ModuleResourceType.Utc, creature, host);

        Assert.IsFalse(reference.Parts.Any(part => part.PartType == "weaponr"));
    }

    [TestMethod]
    public void PaletteFillsEveryLayerOrKeepsOnlyTheFieldsTheBlueprintSets()
    {
        var creature = Native(new GffStruct(uint.MaxValue).Add(NativeField.Byte("Color_Skin", 3)).Add(NativeField.Byte("Color_Tattoo2", 9)));
        var armor = Native(new GffStruct(0).Add(NativeField.Byte("Metal2Color", 4)));

        var filled = ModelPaletteResolver.Resolve(creature, armor);
        var sparse = ModelPaletteResolver.Resolve(creature, armor, fillMissingLayers: false);
        var sparseNoArmor = ModelPaletteResolver.Resolve(creature, null, fillMissingLayers: false);
        var sparseItem = ModelPaletteResolver.ResolveItem(armor, fillMissingLayers: false);

        Assert.AreEqual(ModelPaletteLayers.Count, filled.Count);
        Assert.AreEqual(3, filled[ModelPaletteLayers.Skin]);
        Assert.AreEqual(9, filled[ModelPaletteLayers.Tattoo2]);
        Assert.AreEqual(4, filled[ModelPaletteLayers.Metal2]);
        Assert.AreEqual(0, filled[ModelPaletteLayers.Leather2]);
        CollectionAssert.AreEquivalent(new[] { 0, 1, 3, 8, 9 }, sparse.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { 0, 1, 8, 9 }, sparseNoArmor.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { ModelPaletteLayers.Metal2 }, sparseItem.Keys.ToArray());
    }

    private static FakeModelResolutionHost Host(bool allModelsExist = true)
    {
        var host = new FakeModelResolutionHost();
        host.Creatures[HumanAppearance] = new CreatureAppearanceModelRow(HumanAppearance, "Human", "P", "H");
        host.Creatures[7] = new CreatureAppearanceModelRow(7, "Eshari", "P", "E");
        host.BaseItems[RobeBaseItem] = new BaseItemModelRow("armor", 3);
        host.BaseItems[HelmetBaseItem] = new BaseItemModelRow("helm", 0);
        host.BaseItems[WeaponBaseItem] = new BaseItemModelRow("wswss", 0);
        host.BaseItems[SaberBaseItem] = new BaseItemModelRow("wswls", 2);
        host.BaseItems[CloakBaseItem] = new BaseItemModelRow("cloak", 0);
        host.BaseItems[SmallShieldBaseItem] = new BaseItemModelRow("ashsw", 0);
        host.AllModelsExist = allModelsExist;
        return host;
    }

    private static JsonGffStruct Creature(
        int appearance = HumanAppearance, int gender = 0, int phenotype = 0, GffStruct[]? equipment = null)
    {
        var root = new GffStruct(uint.MaxValue)
            .Add(NativeField.Word("Appearance_Type", (ushort)appearance))
            .Add(NativeField.Byte("Gender", (byte)gender))
            .Add(NativeField.Int("Phenotype", phenotype))
            .Add(NativeField.Byte("Appearance_Head", 3))
            .Add(NativeField.Byte("BodyPart_Torso", 1))
            .Add(NativeField.Byte("BodyPart_Belt", 0))
            .Add(NativeField.Byte("BodyPart_LShoul", 1))
            .Add(NativeField.Byte("BodyPart_RShoul", 1))
            .Add(NativeField.Byte("ArmorPart_RFoot", 1))
            .Add(NativeField.Byte("Color_Skin", 6));
        if (equipment is { Length: > 0 }) root.Add(NativeField.List("Equip_ItemList", equipment));
        return Native(root);
    }

    private static JsonGffStruct Native(GffStruct root) =>
        NativeGffBridge.ToJsonDocument(new GffDocument { FileType = "GFF ", Root = root }).Root;
}
