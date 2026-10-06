using Microsoft.VisualStudio.TestTools.UnitTesting;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Resources;

[TestClass]
public sealed class HakNameTests
{
    [TestMethod]
    public void Validate_AcceptsTheHakNamesThisRepositoryActuallyShips()
    {
        foreach (var name in new[] { "xm_2da", "xm_placeable", "xm_portraits", "xm_mechs", "xm_pt_shoulderl" })
        {
            Assert.IsTrue(HakName.TryValidate(name, out _), $"'{name}' should be a usable hak name");
        }
    }

    /// <summary>Exactly at the cap, and a real hak under content/haks/ -- the bound is inclusive.</summary>
    [TestMethod]
    public void Validate_AcceptsASixteenCharacterName()
    {
        Assert.AreEqual(16, "xm_test_fixtures".Length);
        Assert.IsTrue(HakName.TryValidate("xm_test_fixtures", out _));
    }

    /// <summary>The case that prompted this check: xm_part_shoulderl is 17 characters.</summary>
    [TestMethod]
    public void Validate_RejectsASeventeenCharacterName()
    {
        Assert.AreEqual(17, "xm_part_shoulderl".Length);
        Assert.IsFalse(HakName.TryValidate("xm_part_shoulderl", out var error));
        StringAssert.Contains(error!, "exceeds 16 characters");
    }

    [TestMethod]
    public void Validate_RejectsUppercaseAndPunctuation()
    {
        Assert.IsFalse(HakName.TryValidate("XM_Props", out _));
        Assert.IsFalse(HakName.TryValidate("xm-props", out _));
        Assert.IsFalse(HakName.TryValidate("xm props", out _));
        Assert.IsFalse(HakName.TryValidate("", out _));
        Assert.IsFalse(HakName.TryValidate(null, out _));
    }

    [TestMethod]
    public void Validate_NamesTheSourceSoTheOffenderIsFindable()
    {
        var exception = Assert.ThrowsExactly<FormatException>(
            () => HakName.Validate("xm_part_shoulderl", @"C:\art\_dist\xm_part_shoulderl"));

        StringAssert.Contains(exception.Message, @"C:\art\_dist\xm_part_shoulderl");
    }
}
