using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Key;

namespace Nwn.Formats.Tests.Key;

[TestClass]
public sealed class KeyBifMalformedInputTests
{
    private static byte[] ValidKeyBytes() =>
        KeyBifFixture.Build([new Resource("xm_a", 2017, "data/one.bif", [1, 2, 3])]).KeyBytes;

    private static byte[] ValidBifBytes() =>
        KeyBifFixture.Build([new Resource("xm_a", 2017, "data/one.bif", [1, 2, 3])]).BifBytesByFilename["data/one.bif"];

    [TestMethod]
    public void Key_TruncatedHeader_Throws() => Assert.ThrowsExactly<FormatException>(() => KeyReader.Read(ValidKeyBytes()[..30]));

    [TestMethod]
    public void Key_WrongSignature_Throws()
    {
        var bytes = ValidKeyBytes();
        bytes[0] = (byte)'X';
        Assert.ThrowsExactly<FormatException>(() => KeyReader.Read(bytes));
    }

    [TestMethod]
    public void Key_WrongVersion_Throws()
    {
        var bytes = ValidKeyBytes();
        bytes[4] = (byte)'V'; bytes[5] = (byte)'9'; bytes[6] = (byte)' '; bytes[7] = (byte)' ';
        Assert.ThrowsExactly<FormatException>(() => KeyReader.Read(bytes));
    }

    [TestMethod]
    public void Key_FileTableOffsetPastEndOfFile_Throws()
    {
        var bytes = ValidKeyBytes();
        BitConverter.GetBytes((uint)(bytes.Length + 10_000)).CopyTo(bytes, 16); // OffsetToFileTable
        Assert.ThrowsExactly<FormatException>(() => KeyReader.Read(bytes));
    }

    [TestMethod]
    public void Key_ImplausibleBifCount_Throws()
    {
        var bytes = ValidKeyBytes();
        BitConverter.GetBytes(0x7FFFFFFFu).CopyTo(bytes, 8); // BifCount
        Assert.ThrowsExactly<FormatException>(() => KeyReader.Read(bytes));
    }

    [TestMethod]
    public void Key_ResRefWithCharactersOurOwnResrefRulesWouldReject_IsToleratedNotThrown()
    {
        // Real base-game KEY files carry legacy resrefs (embedded spaces, stray punctuation) that
        // Nwn.Formats.Resref would never accept from OUR OWN content -- a stock resref can never
        // collide with one of ours anyway (see KeyResourceEntry's doc comment), so the reader must
        // tolerate this rather than treat a merely-odd stock resref as file corruption.
        var built = KeyBifFixture.Build([new Resource("ief_see invis", 2027, "data/one.bif", [1])]);
        var key = KeyReader.Read(built.KeyBytes);
        Assert.AreEqual("ief_see invis", key.Resources[0].ResRef);
    }

    [TestMethod]
    public void Bif_TruncatedHeader_Throws() => Assert.ThrowsExactly<FormatException>(() => BifReader.Read(ValidBifBytes()[..10]));

    [TestMethod]
    public void Bif_WrongSignature_Throws()
    {
        var bytes = ValidBifBytes();
        bytes[0] = (byte)'X';
        Assert.ThrowsExactly<FormatException>(() => BifReader.Read(bytes));
    }

    [TestMethod]
    public void Bif_VariableTableOffsetPastEndOfFile_Throws()
    {
        var bytes = ValidBifBytes();
        BitConverter.GetBytes((uint)(bytes.Length + 10_000)).CopyTo(bytes, 16); // VariableTableOffset
        Assert.ThrowsExactly<FormatException>(() => BifReader.Read(bytes));
    }

    [TestMethod]
    public void Bif_ResourceOffsetPastEndOfFile_Throws()
    {
        var bytes = ValidBifBytes();
        BitConverter.GetBytes((uint)(bytes.Length + 10_000)).CopyTo(bytes, 20 + 4); // first entry's Offset field
        Assert.ThrowsExactly<FormatException>(() => BifReader.Read(bytes));
    }
}
