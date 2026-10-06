using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Tests.Scene;

[TestClass]
public sealed class MdlPartBoneMapTests
{
    [TestMethod]
    public void BoneCandidatesKeepOrderedAliasesAndCannotBeMutatedByCallers()
    {
        var candidates = MdlPartBoneMap.GetBoneCandidates("SHOL");

        CollectionAssert.AreEqual(new[] { "lshoul_g", "lshoulder_g", "lshoul" }, candidates.ToArray());
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<string>)candidates)[0] = "changed");
        Assert.AreEqual("lshoul_g", MdlPartBoneMap.GetBoneCandidates("shol")[0]);
    }

    [TestMethod]
    public void PreferredBoneMapIsReadOnly()
    {
        var bones = MdlPartBoneMap.Bones;

        Assert.AreEqual("lforearm", bones["shield"]);
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IDictionary<string, string>)bones)["shield"] = "changed");
        Assert.AreEqual("lforearm", MdlPartBoneMap.Bones["shield"]);
    }
}
