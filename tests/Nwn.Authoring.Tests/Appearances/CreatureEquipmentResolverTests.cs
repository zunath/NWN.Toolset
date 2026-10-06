using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Appearances;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Formats.Gff;
using NativeField = Nwn.Formats.Gff.GffField;

namespace Nwn.Authoring.Tests.Appearances;

[TestClass]
public sealed class CreatureEquipmentResolverTests
{
    [TestMethod]
    public void EmbeddedInstanceWinsAndKeepsNativeSlotAndArmorPrecedence()
    {
        var creature = Creature(
            new GffStruct((uint)CreatureEquipmentSlot.Chest)
                .Add(NativeField.Int("BaseItem", 3))
                .Add(NativeField.Int("ArmorPart_Torso", 7))
                .Add(NativeField.ResRef("TemplateResRef", "armor_a")),
            new GffStruct((uint)CreatureEquipmentSlot.RightHand)
                .Add(NativeField.Int("BaseItem", 4))
                .Add(NativeField.Int("ModelPart1", 9)));
        var loaded = new List<string>();
        var projection = CreatureEquipmentResolver.Resolve(creature, resref =>
        {
            loaded.Add(resref);
            return null;
        });

        Assert.AreEqual(2, projection.VisibleItems.Count);
        Assert.AreEqual(7, projection.Armor!.Item.GetIntOrNull("ArmorPart_Torso"));
        Assert.IsTrue(projection.Armor.IsEmbeddedInstance);
        Assert.AreEqual("armor_a", projection.Armor.BlueprintResRef);
        Assert.AreEqual(0, loaded.Count);
        Assert.AreEqual(0, CreatureEquipmentResolver.ResolveBodyPartNumber(0, 7));
        Assert.AreEqual(7, CreatureEquipmentResolver.ResolveBodyPartNumber(1, 7));
        Assert.AreEqual(1, CreatureEquipmentResolver.ResolveBodyPartNumber(1, 0));
    }

    [TestMethod]
    public void ReferencedEquipmentAcceptsBothResRefSpellingsAndEmptySlotIsIgnored()
    {
        var creature = Creature(
            new GffStruct((uint)CreatureEquipmentSlot.Head)
                .Add(NativeField.ResRef("EquippedRes", "helmet_a")),
            new GffStruct((uint)CreatureEquipmentSlot.Cloak)
                .Add(NativeField.ResRef("TemplateResRef", "cloak_a")),
            new GffStruct((uint)CreatureEquipmentSlot.LeftHand)
                .Add(NativeField.Int("BaseItem", 0))
                .Add(NativeField.Int("ModelPart1", 0)));
        var projection = CreatureEquipmentResolver.Resolve(creature, resref =>
        {
            var item = new JsonGffStruct();
            item.SetInt("BaseItem", Nwn.Authoring.Documents.NimGff.GffFieldType.Int, resref == "helmet_a" ? 1 : 2);
            return item;
        });

        Assert.AreEqual("helmet_a", projection.Helmet!.BlueprintResRef);
        Assert.AreEqual("cloak_a", projection.Cloak!.BlueprintResRef);
        Assert.IsFalse(projection.Helmet.IsEmbeddedInstance);
        Assert.IsFalse(projection.Cloak.IsEmbeddedInstance);
        Assert.IsNull(projection.LeftHand);
        CollectionAssert.AreEqual(new[] { "helmet_a", "cloak_a" }, CreatureEquipmentResolver.GetVisibleBlueprintResRefs(creature).ToArray());
    }

    private static JsonGffStruct Creature(params GffStruct[] equipment)
    {
        var root = new GffStruct(uint.MaxValue).Add(NativeField.List("Equip_ItemList", equipment));
        return NativeGffBridge.ToJsonDocument(new GffDocument { FileType = "UTC ", Root = root }).Root;
    }
}
