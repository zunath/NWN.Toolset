using Microsoft.VisualStudio.TestTools.UnitTesting;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Resources;

[TestClass]
[TestCategory("Unit")]
public sealed class ResourceTypesTests
{
    [TestMethod]
    public void GuiExtension_MapsToTheEngineGuiTypeCode()
    {
        Assert.AreEqual(ResourceType.Gui, ResourceTypes.GetByExtension("gui"));
        Assert.AreEqual("gui", ResourceTypes.GetExtension(ResourceType.Gui));
        Assert.IsTrue(ResourceTypes.TryGetByCode(2047, out var type));
        Assert.AreEqual(ResourceType.Gui, type);
    }

    [TestMethod]
    public void TgaExtension_MapsToTheEngineTgaTypeCode()
    {
        Assert.AreEqual(ResourceType.Tga, ResourceTypes.GetByExtension("tga"));
        Assert.AreEqual("tga", ResourceTypes.GetExtension(ResourceType.Tga));
        Assert.IsTrue(ResourceTypes.TryGetByCode(3, out var type));
        Assert.AreEqual(ResourceType.Tga, type);
    }

    [TestMethod]
    public void StandardNwnResourceTypes_RoundTripTheirExtensionAndCode()
    {
        foreach (var type in Enum.GetValues<ResourceType>())
        {
            var extension = ResourceTypes.GetExtension(type);
            Assert.AreEqual(type, ResourceTypes.GetByExtension(extension), extension);
            if (type is ResourceType.Erf or ResourceType.Bif or ResourceType.Key)
            {
                Assert.IsFalse(ResourceTypes.TryGetByCode((ushort)type, out _), extension);
                continue;
            }

            Assert.IsTrue(ResourceTypes.TryGetByCode((ushort)type, out var codeType), extension);
            Assert.AreEqual(type, codeType, extension);
        }
    }

    [TestMethod]
    public void LegacyGenericAliasesAndUnknownCodesKeepDeterministicMappings()
    {
        Assert.AreEqual(ResourceType.Gff, ResourceTypes.GetByExtension("gff"));
        Assert.IsTrue(ResourceTypes.TryGetByCode(2037, out var gffType));
        Assert.AreEqual(ResourceType.Gff, gffType);
        Assert.AreEqual(ResourceType.Bmu, ResourceTypes.GetByExtension("bmu"));
        Assert.AreEqual(ResourceType.Mp3, ResourceTypes.GetByExtension("mp3"));
        Assert.AreEqual("bmu", ResourceTypes.GetExtension(ResourceType.Mp3));
        Assert.IsFalse(ResourceTypes.TryGetByCode(2037 + 100, out _));
        Assert.ThrowsExactly<FormatException>(() => ResourceTypes.GetByExtension("unknown"));
    }
}
