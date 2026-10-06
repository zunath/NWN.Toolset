using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Toolset.Avalonia.Tests.Support;
using Nwn.Toolset.Avalonia.Triggers;

namespace Nwn.Toolset.Avalonia.Tests.Triggers;

[TestClass]
public sealed class TriggerBehaviorEditorViewModelTests
{
    [TestMethod]
    public void TheHostCatalogClassifiesTheTriggerFromItsTypeAndTrapFlag()
    {
        var (_, generic) = Trigger();
        var (_, transition) = Trigger(("Type", NativeTestDocuments.Integer(GffFieldType.Int, 1)));
        var (_, trap) = Trigger(("TrapFlag", NativeTestDocuments.Integer(GffFieldType.Byte, 1)));

        using var rawEditor = Editor(generic);
        using var transitionEditor = Editor(transition);
        using var trapEditor = Editor(trap);

        Assert.AreEqual("custom", rawEditor.Behavior.Id);
        Assert.IsNotNull(rawEditor.Variables, "Only the raw behavior edits locals.");
        Assert.AreEqual("transition", transitionEditor.Behavior.Id);
        Assert.AreEqual("Area Transition", transitionEditor.HeaderName);
        Assert.IsNull(transitionEditor.Variables);
        Assert.AreEqual("trap", trapEditor.Behavior.Id);
        Assert.AreEqual("blueprint", transitionEditor.HeaderKind);
        CollectionAssert.AreEqual(new[] { "Tag" }, transitionEditor.BasicRows.Select(row => row.Label).ToArray());
        Assert.AreEqual(3, transitionEditor.BehaviorList.Count(item => item.IsSelectable));
    }

    [TestMethod]
    public void DestinationEditsRoundTripThroughTheSessionAndUndo()
    {
        var (session, trigger) = Trigger(("Type", NativeTestDocuments.Integer(GffFieldType.Int, 1)));
        using var editor = Editor(trigger, session: session);
        var tag = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");
        var type = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedToFlags");
        Assert.IsNull(type.Choice, "An unset destination type is not defaulted to a door.");
        Assert.AreEqual("Area Transition still needs Destination tag.", editor.Incomplete);

        tag.Text = "moseis_gate";
        type.Choice = type.Choices.Single(choice => choice.Display == "Waypoint");

        Assert.AreEqual("moseis_gate", trigger.GetStringOrNull("LinkedTo"));
        Assert.AreEqual(2, trigger.GetIntOrNull("LinkedToFlags"));
        Assert.IsNull(editor.Incomplete);

        session.Undo();
        editor.ReloadFromDocument();
        Assert.IsNull(type.Choice, "Undo takes the destination type back out.");
        Assert.AreEqual("moseis_gate", tag.Text);

        session.Undo();
        editor.ReloadFromDocument();
        Assert.AreEqual(string.Empty, tag.Text);
        Assert.AreEqual("Area Transition still needs Destination tag.", editor.Incomplete);

        session.Redo();
        session.Redo();
        editor.ReloadFromDocument();
        Assert.AreEqual("moseis_gate", tag.Text);
        Assert.AreEqual("Waypoint", type.Choice!.Display);
    }

    [TestMethod]
    public void TheStatusDescribesWhatTheTagReachesForTheChosenDestinationType()
    {
        var (session, trigger) = Trigger(
            ("Type", NativeTestDocuments.Integer(GffFieldType.Int, 1)),
            ("LinkedTo", NativeTestDocuments.Text(GffFieldType.CExoString, "gate")));
        var answers = new Dictionary<BehaviorTagScope, TransitionDestinationResult>();
        var asked = new List<(BehaviorTagScope Scope, string Tag)>();
        using var editor = Editor(trigger, session: session, resolve: (scope, tag) =>
        {
            asked.Add((scope, tag));
            return answers.GetValueOrDefault(scope, TransitionDestinationResult.NotFound);
        });
        var tag = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");
        var type = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedToFlags");

        answers[BehaviorTagScope.None] = TransitionDestinationResult.TypeUnset;
        tag.RefreshStatus();
        Assert.AreEqual("⚠ destination type is unset; this transition will do nothing", tag.Status);
        Assert.IsFalse(tag.IsStatusGood);

        answers[BehaviorTagScope.None] = TransitionDestinationResult.TypeNone;
        tag.RefreshStatus();
        Assert.AreEqual("destination type is None; the tag is not used", tag.Status);
        Assert.IsTrue(tag.IsStatusGood);

        type.Choice = type.Choices.Single(choice => choice.Display == "Door");
        Assert.AreEqual(BehaviorTagScope.Door, asked[^1].Scope, "Choosing the type refreshes the tag row's status.");
        Assert.AreEqual("gate", asked[^1].Tag);

        var cases = new (TransitionDestinationResult Result, string Expected, bool Good)[]
        {
            (TransitionDestinationResult.Resolved("door in moseis_cantina"), "✓ door in moseis_cantina", true),
            (TransitionDestinationResult.NotFound, "⚠ no door carries this tag", false),
            (TransitionDestinationResult.Ambiguous(3),
                "⚠ 3 door objects carry this tag, so the destination is ambiguous", false),
            (TransitionDestinationResult.WrongType(BehaviorTagScope.Waypoint),
                "⚠ this tag belongs to a waypoint, not a door", false),
            (TransitionDestinationResult.CatalogIncomplete,
                "⚠ part of the module could not be read, so this tag cannot be verified", false),
        };
        foreach (var (result, expected, good) in cases)
        {
            answers[BehaviorTagScope.Door] = result;
            tag.RefreshStatus();
            Assert.AreEqual(expected, tag.Status, result.Status.ToString());
            Assert.AreEqual(good, tag.IsStatusGood, result.Status.ToString());
        }

        type.Choice = type.Choices.Single(choice => choice.Display == "Waypoint");
        answers[BehaviorTagScope.Waypoint] = TransitionDestinationResult.NotFound;
        tag.RefreshStatus();
        Assert.AreEqual("⚠ no waypoint carries this tag", tag.Status);
    }

    [TestMethod]
    public void WithoutAResolverTheDestinationRowPrintsNoStatus()
    {
        var (_, trigger) = Trigger(
            ("Type", NativeTestDocuments.Integer(GffFieldType.Int, 1)),
            ("LinkedTo", NativeTestDocuments.Text(GffFieldType.CExoString, "gate")));
        using var editor = Editor(trigger);

        Assert.IsNull(editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo").Status);
    }

    [TestMethod]
    public async Task SwitchingBehaviorConfirmsWhatItClearsAndWritesTheManagedValues()
    {
        var (session, trigger) = Trigger(("ScriptOnEnter", NativeTestDocuments.Text(GffFieldType.ResRef, "my_enter")));
        var prompts = new FakePalettePrompts { Confirms = false };
        using var editor = Editor(trigger, session: session, prompts: prompts);
        Assert.AreEqual("custom", editor.Behavior.Id);

        await editor.ChooseBehaviorAsync(FakeTriggerBehaviorCatalog.Trap);
        Assert.AreEqual("Change behavior to Trap?", prompts.Headlines.Single());
        Assert.AreEqual(
            "This clears ScriptOnEnter, which is not part of Trap. Undo will put it back until the trigger is saved.",
            prompts.Messages.Single());
        Assert.AreEqual("custom", editor.Behavior.Id, "A declined prompt changes nothing.");
        Assert.AreEqual("my_enter", trigger.GetStringOrNull("ScriptOnEnter"));

        prompts.Confirms = true;
        await editor.ChooseBehaviorAsync(FakeTriggerBehaviorCatalog.Trap);
        Assert.AreEqual("trap", editor.Behavior.Id);
        Assert.AreEqual(2, trigger.GetIntOrNull("Type"));
        Assert.AreEqual(1, trigger.GetIntOrNull("TrapFlag"));
        Assert.AreEqual(string.Empty, trigger.GetStringOrNull("ScriptOnEnter"));
        Assert.IsNull(editor.Variables);

        await editor.ChooseBehaviorAsync(FakeTriggerBehaviorCatalog.Transition);
        Assert.AreEqual(1, trigger.GetIntOrNull("Type"));
        Assert.AreEqual(0, trigger.GetIntOrNull("TrapFlag"), "Leaving the trap clears the flag it set.");

        await editor.ChooseBehaviorAsync(FakeTriggerBehaviorCatalog.Raw);
        Assert.AreEqual(2, prompts.Headlines.Count, "Entering the raw behavior clears nothing and asks nothing.");
        Assert.IsNotNull(editor.Variables);
    }

    [TestMethod]
    public void AConditionalRowAppearsOnlyWhileItsFieldHoldsTheRequiredValue()
    {
        var (session, trigger) = Trigger();
        using var editor = Editor(trigger, session: session);
        var trapScript = editor.BehaviorRows.Single(row => row.Definition.Name == "OnTrapTriggered");
        Assert.IsFalse(trapScript.IsVisible);
        Assert.IsNull(editor.Incomplete, "A hidden required row is not missing.");

        editor.BehaviorRows.Single(row => row.Definition.Name == "TrapFlag").IsChecked = true;
        Assert.IsTrue(trapScript.IsVisible);
        Assert.AreEqual("Custom still needs Trap script.", editor.Incomplete);

        trapScript.Text = "my_trap";
        Assert.IsNull(editor.Incomplete);

        editor.BehaviorRows.Single(row => row.Definition.Name == "TrapFlag").IsChecked = false;
        Assert.IsFalse(trapScript.IsVisible);
    }

    [TestMethod]
    public void ARowEditorsAPlacementWritesThroughTheHostTransaction()
    {
        var (session, trigger) = Trigger(("Type", NativeTestDocuments.Integer(GffFieldType.Int, 2)));
        var descriptions = new List<string>();
        using var editor = Editor(trigger, session: session, isInstance: true, descriptions: descriptions);

        editor.BasicRows.Single(row => row.Definition.Name == "Tag").Text = "snare";
        editor.BehaviorRows.Single(row => row.Definition.Name == "TrapDetectDC").Number = 14;

        Assert.AreEqual("snare", trigger.GetStringOrNull("Tag"));
        Assert.AreEqual(14, trigger.GetIntOrNull("TrapDetectDC"));
        Assert.AreEqual("instance", editor.HeaderKind);
        Assert.AreEqual(2, descriptions.Count);
    }

    private static TriggerBehaviorEditorViewModel Editor(
        JsonGffStruct trigger,
        DocumentSession? session = null,
        TransitionDestinationResolver? resolve = null,
        FakePalettePrompts? prompts = null,
        bool isInstance = false,
        List<string>? descriptions = null) =>
        new(trigger, "trigger_test", isInstance, (description, mutation) =>
        {
            descriptions?.Add(description);
            if (session == null)
                mutation();
            else
                session.Execute(description, mutation);
            return true;
        }, new TriggerBehaviorEditorHost
        {
            Catalog = new FakeTriggerBehaviorCatalog(),
            ResolveDestination = resolve,
            Prompts = prompts,
        });

    private static (DocumentSession Session, JsonGffStruct Trigger) Trigger(
        params (string Name, JsonGffField Field)[] fields)
    {
        var document = NativeTestDocuments.Create("UTT ");
        foreach (var (name, field) in fields)
            document.Root.Add(name, field);
        return (new DocumentSession("trigger.utt", document), document.Root);
    }
}
