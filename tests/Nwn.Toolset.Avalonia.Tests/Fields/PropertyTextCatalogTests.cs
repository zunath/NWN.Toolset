using Nwn.Toolset.Avalonia.Areas.Properties;
using Nwn.Toolset.Avalonia.Behaviors;
using Nwn.Toolset.Avalonia.Fields;

namespace Nwn.Toolset.Avalonia.Tests.Fields;

[TestClass]
public sealed class PropertyTextCatalogTests
{
    [TestMethod]
    public void EnglishCatalogsCoverEveryTypedId()
    {
        foreach (var id in Enum.GetValues<FieldStringId>())
            Assert.IsFalse(string.IsNullOrEmpty(FieldTexts.English.Get(id, "a", "b")), id.ToString());
        foreach (var id in Enum.GetValues<BehaviorEditorStringId>())
            Assert.IsFalse(string.IsNullOrEmpty(BehaviorEditorTexts.English.Get(id, "a", "b", "c")), id.ToString());
        foreach (var id in Enum.GetValues<AreaPropertiesStringId>())
            Assert.IsFalse(string.IsNullOrEmpty(AreaPropertiesTexts.English.Get(id, "a", "b", "c")), id.ToString());
    }

    [TestMethod]
    public void AnIncompleteReplacementCatalogIsRefused()
    {
        var values = Enum.GetValues<AreaPropertiesStringId>().Skip(1).ToDictionary(id => id, id => id.ToString());
        Assert.ThrowsExactly<ArgumentException>(() => new AreaPropertiesTexts(values));

        var complete = Enum.GetValues<AreaPropertiesStringId>().ToDictionary(id => id, id => "x" + id);
        var replaced = new AreaPropertiesTexts(complete);
        Assert.AreEqual("xFieldName", replaced.FieldLabel(Nwn.Authoring.Areas.Properties.AreaPropertyFieldId.Name));
    }

    [TestMethod]
    public void LongLossListsAreSummarized()
    {
        var losses = Enumerable.Range(1, 9).Select(index => $"Field{index}").ToArray();
        Assert.AreEqual("Field1, Field2, Field3, Field4, Field5, Field6 and 3 more",
            BehaviorEditorTexts.English.DescribeLosses(losses));
        Assert.AreEqual("A, B", BehaviorEditorTexts.English.DescribeLosses(["A", "B"]));
    }
}
