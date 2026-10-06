using System.Buffers.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Formats.Mdl;

namespace Nwn.Formats.Tests.Mdl;

[TestClass]
[TestCategory("Unit")]
public sealed class MdlAnimationReaderTests
{
    [TestMethod]
    public void AsciiModel_ReadsSuperModelAndAnimationNames()
    {
        var model = Encoding.Latin1.GetBytes(
            "# comment\r\nnewmodel pmh0\r\nsetsupermodel pmh0 a_ba\r\nbeginmodelgeom pmh0\r\n" +
            "newanim pause1 pmh0\r\n  length 1.0\r\ndoneanim pause1 pmh0\r\nnewanim bow pmh0\r\n");

        var set = MdlAnimationReader.Read(model);

        Assert.AreEqual("a_ba", set.SuperModel);
        CollectionAssert.AreEqual(new[] { "pause1", "bow" }, set.Animations.ToArray());
    }

    [TestMethod]
    public void AsciiModel_NullSuperModel_IsNone()
    {
        var set = MdlAnimationReader.Read(Encoding.Latin1.GetBytes("setsupermodel a_ba_casts NULL\n"));

        Assert.IsNull(set.SuperModel);
        Assert.AreEqual(0, set.Animations.Count);
    }

    [TestMethod]
    public void BinaryModel_ReadsSuperModelAndAnimationNames()
    {
        var set = MdlAnimationReader.Read(BuildBinaryModel("a_ba_non_combat", "pause1", "tlklaugh"));

        Assert.AreEqual("a_ba_non_combat", set.SuperModel);
        CollectionAssert.AreEqual(new[] { "pause1", "tlklaugh" }, set.Animations.ToArray());
    }

    [TestMethod]
    public void BinaryModel_NullSuperModel_IsNone()
    {
        Assert.IsNull(MdlAnimationReader.Read(BuildBinaryModel("NULL")).SuperModel);
    }

    [TestMethod]
    public void TruncatedBinaryModel_Throws()
    {
        var model = BuildBinaryModel("a_ba", "pause1");

        Assert.ThrowsExactly<FormatException>(() => MdlAnimationReader.Read(model[..200]));
    }

    /// <summary>A minimal compiled model: the 12-byte file header, a model header with the animation
    /// array and supermodel fields filled, the pointer array, then one 72-byte animation header per
    /// name (function pointers, then the 64-byte name).</summary>
    private static byte[] BuildBinaryModel(string superModel, params string[] animations)
    {
        const int modelHeaderLength = 232;
        var pointerArray = modelHeaderLength;
        var firstAnimation = pointerArray + (animations.Length * 4);
        var modelData = new byte[firstAnimation + (animations.Length * 72)];

        WriteName(modelData, 8, "pmh0");
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(120), (uint)pointerArray);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(124), (uint)animations.Length);
        WriteName(modelData, 168, superModel);
        for (var i = 0; i < animations.Length; i++)
        {
            var header = firstAnimation + (i * 72);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(pointerArray + (i * 4)), (uint)header);
            WriteName(modelData, header + 8, animations[i]);
        }

        var file = new byte[12 + modelData.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), (uint)modelData.Length);
        modelData.CopyTo(file, 12);
        return file;
    }

    private static void WriteName(byte[] buffer, int offset, string name) => Encoding.Latin1.GetBytes(name).CopyTo(buffer, offset);
}
