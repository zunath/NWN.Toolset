using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Appearances;

namespace Nwn.Authoring.Tests.Appearances;

[TestClass]
public sealed class CreatureBodyPartFieldsTests
{
    [TestMethod]
    public void MappingRetainsNativeRightFootAndDistinctSkeletonCategories()
    {
        Assert.AreEqual(18, CreatureBodyPartFields.All.Count);
        Assert.AreEqual(18, CreatureBodyPartFields.All.Select(field => field.CreatureField).Distinct().Count());
        Assert.AreEqual(18, CreatureBodyPartFields.All.Select(field => field.PartType).Distinct().Count());
        Assert.AreEqual(new CreatureBodyPartField("ArmorPart_RFoot", "RFoot", "footr"),
            CreatureBodyPartFields.All.Single(field => field.PartType == "footr"));
        Assert.IsFalse(CreatureBodyPartFields.All.Any(field => field.CreatureField == "BodyPart_RFoot"));
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<CreatureBodyPartField>)CreatureBodyPartFields.All).Clear());
    }
}
