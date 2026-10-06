using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Properties;
using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Tests.Areas.Properties;

[TestClass]
public sealed class AreaPropertyCatalogTests
{
    [TestMethod]
    public void CatalogDeclaresEveryNativeAreaFieldOnceWithTheVerifiedStorageType()
    {
        var fields = AreaPropertyCatalog.Groups.SelectMany(group => group.Fields).ToArray();

        Assert.AreEqual(Enum.GetValues<AreaPropertyFieldId>().Length, fields.Length);
        Assert.AreEqual(fields.Length, fields.Select(field => field.Id).Distinct().Count());
        Assert.AreEqual(fields.Length, fields.Select(field => field.NativeName).Distinct(StringComparer.Ordinal).Count());

        AssertField(AreaPropertyFieldId.Name, GffFieldType.CExoLocString, BehaviorFieldKind.LocalizedText, writable: true);
        AssertField(AreaPropertyFieldId.Flags, GffFieldType.Dword, BehaviorFieldKind.Integer, writable: true);
        AssertField(AreaPropertyFieldId.PlayerVsPlayer, GffFieldType.Byte, BehaviorFieldKind.Integer, writable: true);
        AssertField(AreaPropertyFieldId.SunAmbientColor, GffFieldType.Dword, BehaviorFieldKind.Integer, writable: true);
        AssertField(AreaPropertyFieldId.FogClipDist, GffFieldType.Float, BehaviorFieldKind.Float, writable: true);
        AssertField(AreaPropertyFieldId.LoadScreenID, GffFieldType.Word, BehaviorFieldKind.Integer, writable: true);

        foreach (var id in new[]
        {
            AreaPropertyFieldId.ResRef, AreaPropertyFieldId.Tileset,
            AreaPropertyFieldId.Width, AreaPropertyFieldId.Height,
        })
        {
            Assert.IsTrue(AreaPropertyCatalog.Groups.SelectMany(group => group.Fields)
                .Single(field => field.Id == id).IsReadOnly, id.ToString());
        }
    }

    private static void AssertField(
        AreaPropertyFieldId id,
        GffFieldType type,
        BehaviorFieldKind kind,
        bool writable)
    {
        var field = AreaPropertyCatalog.Groups.SelectMany(group => group.Fields).Single(row => row.Id == id);
        Assert.AreEqual(type, field.FieldType, id.ToString());
        Assert.AreEqual(kind, field.Kind, id.ToString());
        Assert.AreEqual(!writable, field.IsReadOnly, id.ToString());
        Assert.AreEqual(id.ToString(), field.NativeName, id.ToString());
    }
}
