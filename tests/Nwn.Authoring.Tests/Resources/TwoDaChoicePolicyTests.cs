using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;

namespace Nwn.Authoring.Tests.Resources;

[TestClass]
public sealed class TwoDaChoicePolicyTests
{
    [TestMethod]
    public void IsSelectableLabel_RejectsPlaceholderLabels()
    {
        string?[] labels =
        [
            null, "", "****", "(Null Human)", "reserved door", "deleted_door", "invalid-door",
            "padding", "unused_12", "user", "User12", "null_01", "Null12"
        ];

        foreach (var label in labels)
            Assert.IsFalse(TwoDaChoicePolicy.IsSelectableLabel(label), $"'{label}' is a placeholder.");
    }

    [TestMethod]
    public void IsSelectableLabel_RetainsRealContentLabels()
    {
        string[] labels = ["Nullifier", "UserSelection", "door_event", "Astroport_door"];

        foreach (var label in labels)
            Assert.IsTrue(TwoDaChoicePolicy.IsSelectableLabel(label), $"'{label}' is real content.");
    }
}
