using Nwn.Authoring.Areas.Properties;
using Nwn.Authoring.Fields;
using Nwn.Toolset.Avalonia.Areas.Properties;
using Nwn.Toolset.Avalonia.Fields;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas.Properties;

[TestClass]
public sealed class AreaPropertyFieldGroupsTests
{
    [TestMethod]
    public void TheProjectionFollowsTheNativeCatalogExactly()
    {
        var groups = AreaPropertyFieldGroups.Describe(AreaPropertiesTexts.English);

        CollectionAssert.AreEqual(
            new[] { "Identity", "Flags", "Lighting", "Weather", "Loading" },
            groups.Select(group => group.Title).ToArray());
        var shared = AreaPropertyCatalog.Groups.SelectMany(group => group.Fields).ToArray();
        var projected = groups.SelectMany(group => group.Fields).ToArray();
        CollectionAssert.AreEqual(shared.Select(field => field.NativeName).ToArray(),
            projected.Select(field => field.FieldName).ToArray());
        foreach (var field in shared)
        {
            var descriptor = projected.Single(candidate => candidate.FieldName == field.NativeName);
            Assert.AreEqual(field.FieldType, descriptor.FieldType, field.NativeName);
            Assert.AreEqual(field.IsReadOnly, descriptor.IsReadOnly, field.NativeName);
        }

        var name = projected.Single(field => field.FieldName == "Name");
        Assert.AreEqual(EditorKind.LocString, name.Kind);
        Assert.AreEqual("Fog Clip Distance", projected.Single(field => field.FieldName == "FogClipDist").Label);
        Assert.AreEqual("The ResRef. Matches the file name.", projected.Single(field => field.FieldName == "ResRef").Description);
        Assert.AreEqual(EditorKind.Integer, projected.Single(field => field.FieldName == "SkyBox").Kind);
    }

    [TestMethod]
    public void HostChoicesTurnNumbersIntoDropdownsAndPolicyHidesOrLocksFields()
    {
        var choices = new FakeAreaPropertyChoiceSource();
        choices.Choices[AreaPropertyFieldId.SkyBox] = [new LookupOption(3, "Desert")];
        var policy = new AreaPropertyFieldPolicy(
            new HashSet<AreaPropertyFieldId> { AreaPropertyFieldId.Name },
            new HashSet<AreaPropertyFieldId> { AreaPropertyFieldId.Tag });
        using var documents = NativeTestDocuments.CreateArea();
        var context = new EditorFieldContext(documents.Area.Document, documents.ExecuteArea);

        var groups = AreaPropertyFieldGroups.Create(context, AreaPropertiesTexts.English, choices, policy);
        var fields = groups.SelectMany(group => group.Fields).ToArray();

        Assert.IsFalse(fields.Any(field => field.Descriptor.FieldName == "Name"));
        Assert.IsTrue(fields.Single(field => field.Descriptor.FieldName == "Tag").IsReadOnly);
        var sky = (DropdownFieldViewModel)fields.Single(field => field.Descriptor.FieldName == "SkyBox");
        Assert.AreEqual("Desert", sky.Options.Single(option => option.Id == 3).Display);
        sky.SelectedOption = sky.Options.Single(option => option.Id == 3);
        Assert.AreEqual(3L, documents.Area.Document.Root.Get("SkyBox").GetInteger());
        Assert.IsTrue(documents.UndoArea());
        Assert.IsNull(documents.Area.Document.Root.GetOrNull("SkyBox"));
    }

    [TestMethod]
    public void ThePageRefreshesFieldsAfterUndoAndReResolvesChoices()
    {
        var choices = new FakeAreaPropertyChoiceSource();
        choices.Choices[AreaPropertyFieldId.LoadScreenID] = [new LookupOption(1, "Old")];
        using var documents = NativeTestDocuments.CreateArea();
        var page = new AreaPropertiesPageViewModel(
            new EditorFieldContext(documents.Area.Document, documents.ExecuteArea), choices: choices);
        var wind = (IntegerFieldViewModel)page.AreaPropertyGroups.SelectMany(group => group.Fields)
            .Single(field => field.Descriptor.FieldName == "WindPower");

        wind.Value = 2;
        documents.UndoArea();
        page.RefreshAreaPropertyFields();
        Assert.AreEqual(0L, wind.Value);

        choices.Choices[AreaPropertyFieldId.LoadScreenID] = [new LookupOption(1, "Renamed")];
        page.RefreshTlkLabels();
        var loading = (DropdownFieldViewModel)page.AreaPropertyGroups.SelectMany(group => group.Fields)
            .Single(field => field.Descriptor.FieldName == "LoadScreenID");
        Assert.AreEqual("Renamed", loading.Options.Single(option => option.Id == 1).Display);
        Assert.AreEqual("Area Properties", page.AreaPropertiesHeader);
    }
}
