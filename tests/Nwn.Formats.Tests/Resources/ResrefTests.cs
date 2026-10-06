using Microsoft.VisualStudio.TestTools.UnitTesting;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Resources;

[TestClass]
public sealed class ResrefTests
{
    [TestMethod]
    public void Parse_AcceptsLowercaseAlphanumericAndUnderscore()
    {
        var resref = Resref.Parse("xm_persist_npc");
        Assert.AreEqual("xm_persist_npc", resref.Value);
    }

    [TestMethod]
    public void Parse_AcceptsExactly16Characters()
    {
        var resref = Resref.Parse("0123456789abcdef");
        Assert.AreEqual(16, resref.Value.Length);
    }

    [TestMethod]
    public void Parse_Rejects17Characters() => Assert.ThrowsExactly<FormatException>(() => Resref.Parse("01234567890123456"));

    [TestMethod]
    public void Parse_RejectsUppercase() => Assert.ThrowsExactly<FormatException>(() => Resref.Parse("Xm_Persist_Npc"));

    [TestMethod]
    public void Parse_RejectsSpace() => Assert.ThrowsExactly<FormatException>(() => Resref.Parse("xm persist"));

    [TestMethod]
    public void Parse_RejectsEmpty() => Assert.ThrowsExactly<FormatException>(() => Resref.Parse(""));

    [TestMethod]
    public void Equality_IsOrdinalAndCaseSensitiveAfterNormalization()
    {
        var a = Resref.Parse("xm_persist_npc");
        var b = Resref.Parse("xm_persist_npc");
        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
    }

    [TestMethod]
    public void InequalityOperator_ReflectsDifferentValues()
    {
        var a = Resref.Parse("xm_a");
        var b = Resref.Parse("xm_b");

        Assert.IsTrue(a != b);
        Assert.IsFalse(a != Resref.Parse("xm_a"));
    }

    [TestMethod]
    public void Equals_AgainstANonResrefObject_IsFalse()
    {
        var resref = Resref.Parse("xm_a");

        Assert.IsFalse(resref.Equals("xm_a"));
        Assert.IsFalse(resref.Equals(null));
    }

    [TestMethod]
    public void ToString_ReturnsTheUnderlyingValue()
    {
        var resref = Resref.Parse("xm_persist_npc");

        Assert.AreEqual("xm_persist_npc", resref.ToString());
    }

    [TestMethod]
    public void GetHashCode_MatchesForEqualValues()
    {
        var a = Resref.Parse("xm_persist_npc");
        var b = Resref.Parse("xm_persist_npc");

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_OnTheDefaultValue_IsZero()
    {
        Assert.AreEqual(0, default(Resref).GetHashCode());
    }

    [TestMethod]
    public void TryParse_WithANullValue_FailsWithAClearMessage()
    {
        var succeeded = Resref.TryParse(null, out var resref, out var error);

        Assert.IsFalse(succeeded);
        Assert.AreEqual(default, resref);
        Assert.AreEqual("Resref cannot be empty.", error);
    }

    [TestMethod]
    public void TryParse_WithAValidValue_ReturnsTrue_AndNoError()
    {
        var succeeded = Resref.TryParse("xm_persist_npc", out var resref, out var error);

        Assert.IsTrue(succeeded);
        Assert.AreEqual("xm_persist_npc", resref.Value);
        Assert.IsNull(error);
    }
}
