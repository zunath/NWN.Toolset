using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Fields;

/// <summary>Layout rules of the shared schema field templates, moved with them from SWLOR's App.axaml.</summary>
[TestClass]
public sealed class FieldViewMarkupTests
{
    [TestMethod]
    public void TheFieldPageSpendsItsWidthOnValuesAndItsHeightOnTheDescription()
    {
        var view = ToolsetSourceFiles.Read("Fields", "Views", "FieldView.axaml");

        Assert.IsFalse(view.Contains("ColumnDefinitions=\"180,*", StringComparison.Ordinal),
            "the label column was wider than the longest label these editors use");
        Assert.IsFalse(view.Contains("ColumnDefinitions=\"220,*", StringComparison.Ordinal));

        // The strref explains a blank box; it is not a second value beside the real one.
        Assert.IsFalse(view.Contains("Text=\"{Binding StrRefDisplay}\"", StringComparison.Ordinal));
        StringAssert.Contains(view, "Watermark=\"{Binding StrRefDisplay}\"");

        // A floor tall enough to write a description in, low enough not to own a short window.
        StringAssert.Contains(view, "MinHeight=\"140\"");
    }

    [TestMethod]
    public void DerivedFieldKindsAreMatchedBeforeTheTextFieldTheyExtend()
    {
        var view = ToolsetSourceFiles.Read("Fields", "Views", "FieldView.axaml");
        var text = view.IndexOf("DataType=\"fields:TextFieldViewModel\"", StringComparison.Ordinal);

        Assert.IsTrue(view.IndexOf("DataType=\"fields:ScriptFieldViewModel\"", StringComparison.Ordinal) < text);
        Assert.IsTrue(view.IndexOf("DataType=\"fields:ResourcePickerFieldViewModel\"", StringComparison.Ordinal) < text);
        Assert.IsTrue(view.LastIndexOf("DataType=\"fields:FieldViewModel\"", StringComparison.Ordinal) > text,
            "the fallback template comes last so a host template cannot recurse into this control");
    }
}
