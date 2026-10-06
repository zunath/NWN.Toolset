using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Fields;
using Nwn.Formats.Tlk;
using Nwn.Toolset.Avalonia.Fields;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Fields;

[TestClass]
public sealed class FieldViewModelTests
{
    [TestMethod]
    public void EditsRunThroughTheHostTransactionAndCreateAbsentFieldsWithTheirNativeType()
    {
        var document = NativeTestDocuments.Create("ARE ");
        var descriptions = new List<string>();
        var context = new EditorFieldContext(document, (description, mutation) =>
        {
            descriptions.Add(description);
            mutation();
            return true;
        });

        var text = (TextFieldViewModel)FieldViewModelFactory.Create(Descriptor("Tag", EditorKind.Text, GffFieldType.CExoString), context);
        var number = (IntegerFieldViewModel)FieldViewModelFactory.Create(Descriptor("WindPower", EditorKind.Integer, GffFieldType.Int), context);
        var check = (CheckFieldViewModel)FieldViewModelFactory.Create(Descriptor("NoRest", EditorKind.Check, GffFieldType.Byte), context);
        var single = (FloatFieldViewModel)FieldViewModelFactory.Create(Descriptor("FogClipDist", EditorKind.Float, GffFieldType.Float), context);

        text.Text = "area_tag";
        number.Value = 2;
        check.IsChecked = true;
        single.Value = 45.5;

        Assert.AreEqual("area_tag", document.Root.Get("Tag").GetString());
        Assert.AreEqual(GffFieldType.Int, document.Root.Get("WindPower").Type);
        Assert.AreEqual(2L, document.Root.Get("WindPower").GetInteger());
        Assert.AreEqual(1L, document.Root.Get("NoRest").GetInteger());
        Assert.AreEqual(45.5f, document.Root.Get("FogClipDist").GetSingle());
        CollectionAssert.AreEqual(
            new[] { "Change Tag", "Change WindPower", "Toggle NoRest", "Change FogClipDist" },
            descriptions);
    }

    [TestMethod]
    public void ARefusedEditReloadsTheStoredValue()
    {
        var document = NativeTestDocuments.Create("ARE ");
        document.Root.Add("WindPower", NativeTestDocuments.Integer(GffFieldType.Int, 1));
        var context = new EditorFieldContext(document, (_, _) => false);
        var number = (IntegerFieldViewModel)FieldViewModelFactory.Create(Descriptor("WindPower", EditorKind.Integer, GffFieldType.Int), context);

        number.Value = 9;

        Assert.AreEqual(1L, number.Value);
        Assert.AreEqual(1L, document.Root.Get("WindPower").GetInteger());
    }

    [TestMethod]
    public void LocalizedFieldsShowTheStrRefAsAWatermarkAndOnlyOpenCustomRows()
    {
        var custom = TlkTable.CustomStrRefBase + 42;
        var document = NativeTestDocuments.Parse(
            """{"__data_type":"ARE ","Name":{"id":""" + custom + ""","type":"cexolocstring","value":{}}}""");
        uint? opened = null;
        var context = new EditorFieldContext(document, (_, mutation) => { mutation(); return true; },
            strRef => strRef == custom ? "Resolved name" : null, strRef => opened = strRef);
        var field = (LocStringFieldViewModel)FieldViewModelFactory.Create(
            Descriptor("Name", EditorKind.LocString, GffFieldType.CExoLocString), context);

        Assert.AreEqual(string.Empty, field.Text);
        Assert.AreEqual($"strref {custom} - “Resolved name”", field.StrRefDisplay);
        Assert.IsTrue(field.CanOpenTlkRow);
        field.OpenTlkRowCommand.Execute(null);
        Assert.AreEqual(custom, opened);

        var narrowed = new EditorFieldContext(document, (_, _) => true, null, _ => { }, _ => false);
        var refused = (LocStringFieldViewModel)FieldViewModelFactory.Create(
            Descriptor("Name", EditorKind.LocString, GffFieldType.CExoLocString), narrowed);
        Assert.IsFalse(refused.CanOpenTlkRow, "The host's TLK predicate narrows which rows can be opened.");
    }

    [TestMethod]
    public void DropdownsOfferAnUnsetChoiceAndKeepUnavailableLookupsReadOnly()
    {
        var document = NativeTestDocuments.Create("ARE ");
        document.Root.Add("SkyBox", NativeTestDocuments.Integer(GffFieldType.Byte, 2));
        var context = new EditorFieldContext(document, (_, mutation) => { mutation(); return true; });
        var options = new[] { new LookupOption(1, "Grass"), new LookupOption(2, "Desert") };
        var dropdown = (DropdownFieldViewModel)FieldViewModelFactory.Create(
            new FieldDescriptor
            {
                Label = "Sky Box", FieldName = "SkyBox", Kind = EditorKind.TwoDaDropdown,
                FieldType = GffFieldType.Byte, LookupKey = "skyboxes"
            },
            context,
            key => key == "skyboxes" ? options : []);

        Assert.AreEqual(3, dropdown.Options.Count);
        Assert.AreEqual(255L, dropdown.Options[0].Id);
        Assert.AreEqual("(None)", dropdown.Options[0].Display);
        Assert.AreEqual("Desert", dropdown.SelectedOption!.Display);
        dropdown.SelectedOption = dropdown.Options[1];
        Assert.AreEqual(1L, document.Root.Get("SkyBox").GetInteger());

        dropdown.RefreshOptions([]);
        Assert.IsFalse(dropdown.HasOptions);
        Assert.AreEqual(1L, dropdown.RawValue);
        Assert.AreEqual("2DA metadata unavailable. The stored value is shown read-only.", dropdown.LookupUnavailableMessage);
    }

    [TestMethod]
    public void ScriptSlotsReportScriptsTheHostDoesNotHave()
    {
        var document = NativeTestDocuments.Create("UTD ");
        var context = new EditorFieldContext(document, (_, mutation) => { mutation(); return true; });
        var host = new ScriptHost();
        var script = (ScriptFieldViewModel)FieldViewModelFactory.Create(
            Descriptor("OnOpen", EditorKind.ScriptSlot, GffFieldType.ResRef), context, scriptSlotHost: host);

        script.Text = "missing_script";
        Assert.IsTrue(script.IsMissing);
        Assert.AreEqual("'missing_script' does not exist in this module.", script.MissingMessage);
        script.Text = "known";
        Assert.IsFalse(script.IsMissing);
        script.OpenScriptCommand.Execute(null);
        Assert.AreEqual("known", host.Opened);
    }

    private static FieldDescriptor Descriptor(string name, EditorKind kind, GffFieldType type) => new()
    {
        Label = name, FieldName = name, Kind = kind, FieldType = type
    };

    private sealed class ScriptHost : IScriptSlotHost
    {
        public string? Opened { get; private set; }

        public bool ScriptExists(string resRef) => resRef == "known";

        public void OpenScript(string resRef) => Opened = resRef;

        public Task<string?> PickScriptAsync(string current) => Task.FromResult<string?>(null);
    }
}
