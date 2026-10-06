using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Behaviors;

/// <summary>
/// Layout rules of the shared door, waypoint and sound behavior editors, moved with their views from
/// SWLOR's DoorEditorView, SoundEditorView and WaypointEditorView checks.
/// </summary>
[TestClass]
public sealed class BehaviorEditorMarkupTests
{
    private static readonly (string Folder, string File)[] Editors =
    [
        ("Doors/Views", "DoorBehaviorEditorView.axaml"),
        ("Sounds/Views", "SoundBehaviorEditorView.axaml"),
        ("Waypoints/Views", "WaypointBehaviorEditorView.axaml"),
        ("Triggers/Views", "TriggerBehaviorEditorView.axaml"),
    ];

    [TestMethod]
    public void TheBehaviorRailListsShortNamesAndNeedsNoMoreWidth()
    {
        foreach (var (folder, file) in Editors)
        {
            var markup = Read(folder, file);
            StringAssert.Contains(markup, "ColumnDefinitions=\"210,*\"", $"{file}'s behavior rail lists short names");
        }
    }

    [TestMethod]
    public void RowsAndWhatIsDrawnUnderThemFollowTheSharedRow()
    {
        // Anything drawn underneath a row follows the row: indented under the label column when
        // there is room for one, and full width when there is not. A fixed grid cannot do the
        // second, which is how a key-item list ends up hanging off the side of a narrow pane.
        foreach (var file in new[] { "DoorBehaviorEditorView.axaml", "SoundBehaviorEditorView.axaml" })
        {
            var markup = Read(file.StartsWith("Door", StringComparison.Ordinal) ? "Doors/Views" : "Sounds/Views", file);
            StringAssert.Contains(markup, "sharedBehaviors:LabeledFieldPanel", $"{file} follows the shared row");
            StringAssert.Contains(markup, "<sharedBehaviors:BehaviorRowView />", $"{file} reuses the shared row");
        }

        StringAssert.Contains(Read("Waypoints/Views", "WaypointBehaviorEditorView.axaml"), "<sharedBehaviors:BehaviorRowView");
        StringAssert.Contains(Read("Triggers/Views", "TriggerBehaviorEditorView.axaml"), "<sharedBehaviors:BehaviorRowView");
    }

    [TestMethod]
    public void NoBehaviorEditorShowsAnAdvancedTabOrAFixedLabelColumn()
    {
        foreach (var (folder, file) in Editors)
        {
            var markup = Read(folder, file);
            Assert.IsFalse(markup.Contains("Header=\"Advanced\"", StringComparison.Ordinal),
                $"{file} folds its raw fields into Basic and the raw behavior");
            Assert.IsFalse(markup.Contains("ColumnDefinitions=\"220,*\"", StringComparison.Ordinal), file);
            Assert.IsFalse(markup.Contains("ColumnDefinitions=\"180,*\"", StringComparison.Ordinal), file);
        }
    }

    [TestMethod]
    public void TheDoorEditorPicksItsAppearanceFromTheSharedGallery()
    {
        StringAssert.Contains(Read("Doors/Views", "DoorBehaviorEditorView.axaml"), "<appearance:AppearanceGalleryView");
    }

    private static string Read(string folder, string file) =>
        ToolsetSourceFiles.Read([.. folder.Split('/'), file]);
}
