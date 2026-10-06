using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Io;

namespace Nwn.Formats.Tests.Io;

[TestClass]
public sealed class PendingAreaCreationMarkerTests
{
    [TestMethod]
    public void EnumerateUsesCompatibleTopLevelMarkerNamesAndHonorsHostPrefix()
    {
        var root = Path.Combine(Path.GetTempPath(), "area-marker-" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "nested");
        Directory.CreateDirectory(nested);
        try
        {
            var compatible = Path.Combine(root, PendingAreaCreationMarker.DefaultPrefix + "one.pending");
            var hostSpecific = Path.Combine(root, ".host-area-create-two.pending");
            File.WriteAllText(compatible, "pending");
            File.WriteAllText(hostSpecific, "pending");
            File.WriteAllText(Path.Combine(nested, PendingAreaCreationMarker.DefaultPrefix + "nested.pending"), "pending");

            CollectionAssert.AreEqual(new[] { compatible }, PendingAreaCreationMarker.Enumerate(root).ToArray());
            CollectionAssert.AreEqual(new[] { hostSpecific },
                PendingAreaCreationMarker.Enumerate(root, ".host-area-create-").ToArray());
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
