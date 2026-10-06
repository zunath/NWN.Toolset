using Microsoft.VisualStudio.TestTools.UnitTesting;

using Nwn.Formats.Resources;

namespace Nwn.Formats.Tests.Erf;

[TestClass]
public sealed class ErfEntryTests
{
    [TestMethod]
    public void FileName_CombinesTheResRefAndTheTypesExtension()
    {
        var entry = new Nwn.Formats.Erf.ErfEntry(Resref.Parse("xm_grunt"), ResourceType.Utc, Offset: 0, Size: 0);

        Assert.AreEqual("xm_grunt.utc", entry.FileName);
    }
}
