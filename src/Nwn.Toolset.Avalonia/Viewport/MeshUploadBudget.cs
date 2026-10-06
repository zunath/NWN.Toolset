namespace Nwn.Toolset.Avalonia.Viewport;

internal static class MeshUploadBudget
{
    internal const long MaximumCachedGeometryBytes = 128L * 1024 * 1024;

    internal static long GetRequiredByteCount(int faceCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(faceCount);
        var vertexCount = checked((long)faceCount * 3);
        var floatCount = checked(vertexCount * 5);
        var byteCount = checked(floatCount * sizeof(float));
        if (floatCount > Array.MaxLength)
            throw new FormatException($"Preview mesh needs {floatCount} vertex values; the runtime array limit is {Array.MaxLength}.");
        if (byteCount > MaximumCachedGeometryBytes)
            throw new FormatException($"Preview mesh requires {byteCount} bytes; viewport cache limit is {MaximumCachedGeometryBytes}.");
        return byteCount;
    }
}
