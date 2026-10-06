using Avalonia.Controls;
using Nwn.Toolset.Avalonia.Areas.Generation;
using Nwn.Toolset.Avalonia.Tests.Support;

namespace Nwn.Toolset.Avalonia.Tests.Areas.Generation;

[TestClass]
public sealed class AreaGeneratorLauncherTests
{
    [TestMethod]
    public async Task AnEditorThatCannotBeSavedStopsTheLaunchBeforeTheHostIsBuilt()
    {
        await GraphTestRuntime.DispatchAsync(async () =>
        {
            var session = new RecordingGeneratorSession { SaveSucceeds = false };
            var hostBuilt = false;
            var launcher = new AreaGeneratorLauncher(session, () =>
            {
                hostBuilt = true;
                return Task.FromResult(new GeneratorHostFixture().CreateHost());
            });

            var result = await launcher.LaunchAsync(new Window());

            Assert.AreEqual(AreaGeneratorOutcome.SaveFailed, result.Outcome);
            Assert.IsFalse(hostBuilt);
            CollectionAssert.AreEqual(new[] { "open", "save", "close" }, session.Events.ToArray());
            Assert.AreEqual(0, session.OpenScopes, "The module write scope is released.");
            Assert.AreEqual(0, session.OpenedAreas.Count);
        });
    }
}
