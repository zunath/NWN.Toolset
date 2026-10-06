using Nwn.Toolset.Avalonia.Viewport;

namespace Nwn.Toolset.Avalonia.Tests.Viewport;

[TestClass]
public sealed class MeshUploadBudgetTests
{
    [TestMethod]
    public void GetRequiredByteCount_RejectsOverBudgetMeshBeforeGeometryAllocation()
    {
        var facesWithinBudget = MeshUploadBudget.MaximumCachedGeometryBytes / (3 * 5 * sizeof(float));
        var overBudgetFaceCount = checked((int)facesWithinBudget + 1);

        Assert.ThrowsExactly<FormatException>(() => MeshUploadBudget.GetRequiredByteCount(overBudgetFaceCount));
        Assert.AreEqual(facesWithinBudget * 3 * 5 * sizeof(float),
            MeshUploadBudget.GetRequiredByteCount((int)facesWithinBudget));
    }
}
