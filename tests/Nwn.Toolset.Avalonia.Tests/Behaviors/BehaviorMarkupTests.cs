using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Behaviors;

/// <summary>Layout rules of the shared behavior row markup that every host's property editors draw.</summary>
[TestClass]
public sealed class BehaviorMarkupTests
{
    [TestMethod]
    public void SearchableChoicePickerLoadsMoreChoicesAsTheUserScrolls()
    {
        var picker = ToolsetSourceFiles.Read("Behaviors", "SearchableChoicePickerView.axaml");
        var codeBehind = ToolsetSourceFiles.Read("Behaviors", "SearchableChoicePickerView.axaml.cs");

        StringAssert.Contains(picker, "ScrollViewer.ScrollChanged=\"OnSearchResultsScrollChanged\"");
        Assert.IsFalse(picker.Contains("Content=\"Load more\"", StringComparison.Ordinal),
            "scrolling loads more results; there is no button for it");
        StringAssert.Contains(codeBehind, "row.LoadMoreSearchResultsCommand.Execute(null)");
    }

    [TestMethod]
    public void PlainChoiceTemplateWrapsLongLabels()
    {
        var view = ToolsetSourceFiles.Read("Behaviors", "BehaviorRowView.axaml");

        StringAssert.Contains(view, "<TextBlock Text=\"{Binding Display}\" TextWrapping=\"Wrap\" MaxWidth=\"420\" />");
    }

    [TestMethod]
    public void TheRowGivesItsWidthToTheValueRatherThanTheLabel()
    {
        // Every pixel the label column takes comes out of the value, and the value is the part
        // that has to hold a search list, a picture grid, or a tag.
        var view = ToolsetSourceFiles.Read("Behaviors", "BehaviorRowView.axaml");

        Assert.IsFalse(view.Contains("ColumnDefinitions=\"220,*\"", StringComparison.Ordinal), "the old label column returned");
        Assert.IsFalse(view.Contains("ColumnDefinitions=\"180,*\"", StringComparison.Ordinal), "the old label column returned");
    }

    [TestMethod]
    public void APictureSetThatFitsThePageIsNotHiddenBehindAButton()
    {
        var row = ToolsetSourceFiles.Read("Behaviors", "BehaviorRowView.axaml");

        // The inline grid is the whole point of a picture picker: names are what it replaces.
        StringAssert.Contains(row, "IsVisible=\"{Binding IsInlineGallery}\"");
        Assert.IsFalse(row.Contains("Content=\"Choose&#x2026;\"", StringComparison.Ordinal),
            "a picture set on the page needs no button, and one behind the preview is opened by clicking the preview");

        // The large sets keep their popup, opened by the picture itself.
        StringAssert.Contains(row, "IsVisible=\"{Binding IsPopupGallery}\"");
        StringAssert.Contains(row, "Command=\"{Binding OpenGalleryCommand}\"");
    }
}
