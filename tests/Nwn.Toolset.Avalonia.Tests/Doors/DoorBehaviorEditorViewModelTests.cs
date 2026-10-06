using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Doors;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Toolset.Avalonia.Appearances;
using Nwn.Toolset.Avalonia.Doors;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Doors;

[TestClass]
public sealed class DoorBehaviorEditorViewModelTests
{
    [TestMethod]
    public void TheHostCatalogClassifiesTheDoorAndSuppliesItsRowsAndChoices()
    {
        var (session, door) = Door(("Locked", NativeTestDocuments.Integer(GffFieldType.Byte, 1)));
        var editor = Editor(session, door, new DoorBehaviorEditorHost
        {
            Catalog = new FakeDoorBehaviorCatalog(),
            ResolveChoices = key => key == "factions" ? [new BehaviorChoice(2, "Merchant")] : [],
        });

        Assert.AreEqual("locked", editor.Behavior.Id);
        Assert.AreEqual("Locked Door", editor.HeaderName);
        Assert.AreEqual("blueprint", editor.HeaderKind);
        CollectionAssert.AreEqual(new[] { "Tag", "Faction" }, editor.BasicRows.Select(row => row.Label).ToArray());
        Assert.AreEqual("Merchant", editor.BasicRows[1].Choices.Single().Display);
        Assert.AreEqual(3, editor.BehaviorList.Count(item => item.IsSelectable));
        Assert.IsTrue(editor.BehaviorList.Single(item => item.IsSelected).Behavior!.Id == "locked");
        Assert.IsNull(editor.Variables, "Only the raw behavior edits locals.");
        Assert.IsNull(editor.PreviewView);
    }

    [TestMethod]
    public void AKeyTagDerivesKeyRequiredAndAnUnlockedTransitionClearsItsConditionalLockFields()
    {
        var (session, door) = Door(("Locked", NativeTestDocuments.Integer(GffFieldType.Byte, 1)));
        var editor = Editor(session, door, new DoorBehaviorEditorHost { Catalog = new FakeDoorBehaviorCatalog() });

        editor.BehaviorRows.Single(row => row.Definition.Name == "KeyName").Text = "gate_key";
        Assert.AreEqual(1, door.GetIntOrNull("KeyRequired"));
        Assert.IsTrue(editor.IsDirty);

        session.Execute("link", () =>
        {
            door.Remove("Locked");
            door.SetString("LinkedTo", GffFieldType.CExoString, "dest");
        });
        editor.ReloadFromDocument();
        Assert.AreEqual("transition", editor.Behavior.Id);
        var pickLock = editor.BehaviorRows.Single(row => row.Definition.Name == "OpenLockDC");
        Assert.IsFalse(pickLock.IsVisible, "The pick-lock row only shows while the door is locked.");

        editor.BehaviorRows.Single(row => row.Definition.Name == "Locked").IsChecked = true;
        Assert.IsTrue(pickLock.IsVisible);
        pickLock.Number = 12;
        Assert.AreEqual(12, door.GetIntOrNull("OpenLockDC"));
        editor.BehaviorRows.Single(row => row.Definition.Name == "Locked").IsChecked = false;
        Assert.AreEqual(0, door.GetIntOrNull("OpenLockDC"), "Unlocking clears the conditional lock fields.");
        Assert.AreEqual(0, door.GetIntOrNull("KeyRequired"));
    }

    [TestMethod]
    public async Task SwitchingAwayFromRawConfirmsTheLocalsItSweepsAndAppliesTheKeyRule()
    {
        var (session, door) = Door(("KeyName", NativeTestDocuments.Text(GffFieldType.CExoString, "gate_key")));
        session.Execute("seed", () => new VarTable(door).SetString("WIRING", "kept"));
        var prompts = new FakePalettePrompts { Confirms = false };
        var editor = Editor(session, door, new DoorBehaviorEditorHost { Catalog = new FakeDoorBehaviorCatalog(), Prompts = prompts });
        Assert.AreEqual("custom", editor.Behavior.Id);
        Assert.IsNotNull(editor.Variables);

        await editor.ChooseBehaviorAsync(FakeDoorBehaviorCatalog.Locked);
        Assert.AreEqual("Change behavior to Locked Door?", prompts.Headlines.Single());
        Assert.AreEqual(
            "This clears WIRING, which is not part of Locked Door. Undo will put it back until the door is saved.",
            prompts.Messages.Single());
        Assert.AreEqual("custom", editor.Behavior.Id, "A declined prompt changes nothing.");
        Assert.AreEqual("kept", new VarTable(door).GetString("WIRING"));

        prompts.Confirms = true;
        await editor.ChooseBehaviorAsync(FakeDoorBehaviorCatalog.Locked);
        Assert.AreEqual("locked", editor.Behavior.Id);
        Assert.IsNull(new VarTable(door).GetString("WIRING"));
        Assert.AreEqual(1, door.GetIntOrNull("Locked"));
        Assert.AreEqual(1, door.GetIntOrNull("KeyRequired"), "Locked Door derives KeyRequired from the key tag.");
    }

    [TestMethod]
    public void TheAppearanceGalleryWritesTheGenericOrSpecificFieldAndReportsTagStatus()
    {
        var (session, door) = Door(("Tag", NativeTestDocuments.Text(GffFieldType.CExoString, "door_a")));
        var editor = Editor(session, door, new DoorBehaviorEditorHost
        {
            Catalog = new FakeDoorBehaviorCatalog(),
            Appearances =
            [
                new DoorAppearanceChoice(DoorAppearanceKind.Generic, 3, "Generic ▸ Wood", "wood"),
                new DoorAppearanceChoice(DoorAppearanceKind.Specific, 3, "Specific ▸ Iron", "iron"),
            ],
            ResolveDestination = (scope, tag) => TransitionDestinationResult.FromLocation(
                scope == BehaviorTagScope.Item && tag == "real_key" ? "Real Key" : null),
        });

        editor.Appearance.Highlighted = editor.Appearance.Tiles.Single(tile => tile.Caption == "Specific ▸ Iron");
        Assert.AreEqual(DoorAppearanceKind.Specific, DoorAppearanceValueStore.Read(new BehaviorValueStore(door)).Kind);
        Assert.AreEqual(3, door.GetIntOrNull("Appearance"));

        session.Execute("lock", () => door.SetInt("Locked", GffFieldType.Byte, 1));
        editor.ReloadFromDocument();
        var key = editor.BehaviorRows.Single(row => row.Definition.Name == "KeyName");
        key.Text = "fake_key";
        Assert.AreEqual("⚠ no item blueprint carries this tag", key.Status);
        key.Text = "real_key";
        Assert.AreEqual("✓ Real Key", key.Status);
    }

    [TestMethod]
    public void ATransitionDestinationDescribesEachStatusTheHostCanReport()
    {
        var (session, door) = Door(
            ("LinkedTo", NativeTestDocuments.Text(GffFieldType.CExoString, "gate")),
            ("LinkedToFlags", NativeTestDocuments.Integer(GffFieldType.Byte, 1)));
        var answers = new Dictionary<BehaviorTagScope, TransitionDestinationResult>();
        var editor = Editor(session, door, new DoorBehaviorEditorHost
        {
            Catalog = new FakeDoorBehaviorCatalog(),
            ResolveDestination = (scope, _) => answers.GetValueOrDefault(scope, TransitionDestinationResult.NotFound),
        });
        Assert.AreEqual("transition", editor.Behavior.Id);
        var destination = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");

        var cases = new (TransitionDestinationResult Result, string Expected, bool Good)[]
        {
            (TransitionDestinationResult.Resolved("door in moseis_cantina"), "✓ door in moseis_cantina", true),
            (TransitionDestinationResult.NotFound, "⚠ no door carries this tag", false),
            (TransitionDestinationResult.Ambiguous(2),
                "⚠ 2 door objects carry this tag, so the destination is ambiguous", false),
            (TransitionDestinationResult.WrongType(BehaviorTagScope.Waypoint),
                "⚠ this tag belongs to a waypoint, not a door", false),
            (TransitionDestinationResult.CatalogIncomplete,
                "⚠ part of the module could not be read, so this tag cannot be verified", false),
        };
        foreach (var (result, expected, good) in cases)
        {
            answers[BehaviorTagScope.Door] = result;
            editor.ReloadFromDocument();
            Assert.AreEqual(expected, destination.Status, result.Status.ToString());
            Assert.AreEqual(good, destination.IsStatusGood, result.Status.ToString());
        }

        session.Execute("none", () => door.SetInt("LinkedToFlags", GffFieldType.Byte, 0));
        answers[BehaviorTagScope.None] = TransitionDestinationResult.TypeNone;
        editor.ReloadFromDocument();
        Assert.AreEqual("destination type is None; the tag is not used", destination.Status);
        Assert.IsTrue(destination.IsStatusGood);

        answers[BehaviorTagScope.None] = TransitionDestinationResult.TypeUnset;
        editor.ReloadFromDocument();
        Assert.AreEqual("⚠ destination type is unset; this transition will do nothing", destination.Status);
        Assert.IsFalse(destination.IsStatusGood);
    }

    [TestMethod]
    public void WithoutAResolverATaggedTransitionIsMissingItsTagOrItsType()
    {
        var (session, door) = Door(
            ("LinkedTo", NativeTestDocuments.Text(GffFieldType.CExoString, "gate")),
            ("LinkedToFlags", NativeTestDocuments.Integer(GffFieldType.Byte, 2)));
        var editor = Editor(session, door, new DoorBehaviorEditorHost { Catalog = new FakeDoorBehaviorCatalog() });
        var destination = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");
        Assert.AreEqual("⚠ no waypoint carries this tag", destination.Status);

        session.Execute("unset", () => door.SetInt("LinkedToFlags", GffFieldType.Byte, 0));
        editor.ReloadFromDocument();
        Assert.AreEqual("⚠ destination type is unset; this transition will do nothing", destination.Status);
    }

    [TestMethod]
    public void ThePreviewSourceReceivesTheOptionIdOfEveryGalleryTile()
    {
        var (session, door) = Door();
        var previews = new CapturingPreviews();
        var editor = Editor(session, door, new DoorBehaviorEditorHost
        {
            Catalog = new FakeDoorBehaviorCatalog(),
            AppearancePreviews = previews,
            Appearances =
            [
                new DoorAppearanceChoice(DoorAppearanceKind.Generic, 3, "Generic ▸ Wood", "wood"),
                new DoorAppearanceChoice(DoorAppearanceKind.Specific, 3, "Specific ▸ Iron", "iron"),
            ],
        });

        var entries = previews.Entries!;
        CollectionAssert.AreEqual(new[] { "Generic:3", "Specific:3" }, entries.Select(entry => entry.OptionId.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "wood", "iron" }, entries.Select(entry => entry.Choice.Model).ToArray());
        CollectionAssert.AreEquivalent(
            entries.Select(entry => entry.OptionId).ToArray(),
            editor.Appearance.Tiles.Select(tile => tile.Option.Id).ToArray(),
            "The ids handed to the host are the ids the gallery's own tiles carry.");
    }

    private sealed class CapturingPreviews : IDoorAppearancePreviewSource
    {
        public IReadOnlyList<DoorAppearanceGalleryEntry>? Entries { get; private set; }

        public IAppearanceGalleryPreviewProvider? Create(IReadOnlyList<DoorAppearanceGalleryEntry> entries)
        {
            Entries = entries;
            return null;
        }
    }

    private static DoorBehaviorEditorViewModel Editor(DocumentSession session, JsonGffStruct door, DoorBehaviorEditorHost host) =>
        new(door, "door_test", isInstance: false, (description, mutation) =>
        {
            session.Execute(description, mutation);
            return true;
        }, host);

    private static (DocumentSession Session, JsonGffStruct Door) Door(params (string Name, JsonGffField Field)[] fields)
    {
        var document = NativeTestDocuments.Create("UTD ");
        foreach (var (name, field) in fields)
            document.Root.Add(name, field);
        return (new DocumentSession("door.utd", document), document.Root);
    }
}
