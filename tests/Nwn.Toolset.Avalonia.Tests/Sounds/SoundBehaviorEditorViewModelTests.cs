using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Authoring.Sounds;
using Nwn.Toolset.Avalonia.Sounds;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Sounds;

[TestClass]
public sealed class SoundBehaviorEditorViewModelTests
{
    [TestMethod]
    public async Task SwitchingToALoopAsksBeforeDroppingPlaylistEntriesAndSaysWhatItKept()
    {
        var document = NativeTestDocuments.Create("UTS ");
        var session = new DocumentSession("snd.uts", document);
        var store = new SoundBehaviorValueStore(document.Root);
        session.Execute("seed", () =>
        {
            store.AddSound("rain_a");
            store.AddSound("rain_b");
        });
        var prompts = new FakePalettePrompts { Confirms = false };
        var changes = 0;
        var editor = new SoundBehaviorEditorViewModel(document.Root, "snd_test", isInstance: true,
            (description, mutation) =>
            {
                session.Execute(description, mutation);
                return true;
            },
            new SoundBehaviorEditorHost
            {
                Catalog = new FakeSoundBehaviorCatalog(),
                AudioResources = ["rain_a", "rain_b"],
                Prompts = prompts,
            });
        editor.ValueChanged += () => changes++;
        Assert.AreEqual("playlist", editor.Behavior.Id);
        Assert.IsTrue(editor.BehaviorRows.Single().IsSoundList);
        Assert.IsNotNull(editor.BehaviorRows.Single().SoundList);

        await editor.ChooseBehaviorAsync(FakeSoundBehaviorCatalog.Loop);
        Assert.AreEqual(
            "This drops rain_b because a loop plays a single sound. Undo will put everything back until the sound is saved.",
            prompts.Messages.Single());
        Assert.AreEqual("playlist", editor.Behavior.Id);
        Assert.AreEqual(0, changes);

        prompts.Confirms = true;
        await editor.ChooseBehaviorAsync(FakeSoundBehaviorCatalog.Loop);
        Assert.AreEqual("loop", editor.Behavior.Id);
        CollectionAssert.AreEqual(new[] { "rain_a" }, store.GetSounds().ToArray());
        Assert.AreEqual(1, document.Root.GetIntOrNull("Looping"));
        Assert.AreEqual("Kept rain_a; dropped rain_b because a loop uses one sound.", editor.BehaviorChangeNotice);
        Assert.AreEqual(1, changes);

        editor.ReloadFromDocument();
        Assert.IsNull(editor.BehaviorChangeNotice);
    }
}
