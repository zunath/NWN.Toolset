using Nwn.Toolset.Avalonia.Areas;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas;

[TestClass]
public sealed class AreaViewportDisplayOptionsTests
{
    [TestMethod]
    public void WithoutPersistenceTheOptionsUseTheAreaEditingDefaults()
    {
        var options = new AreaViewportDisplayOptions();

        Assert.IsFalse(options.ShowAreaLighting);
        Assert.IsFalse(options.ShowFog);
        Assert.IsFalse(options.ShowCeilings);
        Assert.IsTrue(options.ShowMaterialMaps);
        Assert.AreEqual(AreaViewportDisplaySettings.Default, options.Settings);
    }

    [TestMethod]
    public void LoadedSettingsSeedTheOptionsWithoutBeingSavedBack()
    {
        var persistence = new RecordingPersistence(new AreaViewportDisplaySettings(
            ShowAreaLighting: true, ShowFog: true, ShowCeilings: true, ShowMaterialMaps: false));

        var options = new AreaViewportDisplayOptions(persistence);

        Assert.IsTrue(options.ShowAreaLighting);
        Assert.IsTrue(options.ShowFog);
        Assert.IsTrue(options.ShowCeilings);
        Assert.IsFalse(options.ShowMaterialMaps);
        Assert.AreEqual(1, persistence.LoadCount);
        Assert.AreEqual(0, persistence.Saved.Count);
    }

    [TestMethod]
    public void EachSwitchChangeSavesTheCompleteSettingsOnce()
    {
        var persistence = new RecordingPersistence(AreaViewportDisplaySettings.Default);
        var options = new AreaViewportDisplayOptions(persistence);

        options.ShowFog = true;
        options.ShowCeilings = true;
        options.ShowAreaLighting = true;
        options.ShowMaterialMaps = false;
        options.ShowMaterialMaps = false;

        CollectionAssert.AreEqual(new[]
        {
            new AreaViewportDisplaySettings(false, true, false, true),
            new AreaViewportDisplaySettings(false, true, true, true),
            new AreaViewportDisplaySettings(true, true, true, true),
            new AreaViewportDisplaySettings(true, true, true, false),
        }, persistence.Saved);
    }

    [TestMethod]
    public void ChangesAreRaisedForEverySwitch()
    {
        var options = new AreaViewportDisplayOptions();
        var raised = new List<string?>();
        options.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        options.ShowAreaLighting = true;
        options.ShowFog = true;
        options.ShowCeilings = true;
        options.ShowMaterialMaps = false;

        CollectionAssert.AreEqual(new[]
        {
            nameof(AreaViewportDisplayOptions.ShowAreaLighting),
            nameof(AreaViewportDisplayOptions.ShowFog),
            nameof(AreaViewportDisplayOptions.ShowCeilings),
            nameof(AreaViewportDisplayOptions.ShowMaterialMaps),
        }, raised);
    }

    [TestMethod]
    public void ApplyToRejectsNullViewport()
    {
        var options = new AreaViewportDisplayOptions();

        Assert.ThrowsExactly<ArgumentNullException>(() => options.ApplyTo(null!));
    }

    [TestMethod]
    public async Task ApplyToPushesAllFourSwitchesOntoTheViewport()
    {
        var options = new AreaViewportDisplayOptions(new RecordingPersistence(new AreaViewportDisplaySettings(
            ShowAreaLighting: true, ShowFog: true, ShowCeilings: true, ShowMaterialMaps: false)));

        await GraphTestRuntime.RunAsync(() => new AreaViewportControl(), window =>
        {
            var viewport = (AreaViewportControl)window.Content!;

            options.ApplyTo(viewport);

            Assert.IsTrue(viewport.ShowAreaLighting);
            Assert.IsTrue(viewport.ShowFog);
            Assert.IsTrue(viewport.ShowCeilings);
            Assert.IsFalse(viewport.ShowMaterialMaps);
        });
    }

    private sealed class RecordingPersistence : IAreaViewportDisplayPersistence
    {
        private readonly AreaViewportDisplaySettings _stored;

        public RecordingPersistence(AreaViewportDisplaySettings stored) => _stored = stored;

        public int LoadCount { get; private set; }

        public List<AreaViewportDisplaySettings> Saved { get; } = [];

        public AreaViewportDisplaySettings Load()
        {
            LoadCount++;
            return _stored;
        }

        public void Save(AreaViewportDisplaySettings settings) => Saved.Add(settings);
    }
}
