using Nwn.Formats.Resources;

namespace Nwn.Formats.Erf;

/// <summary>One resource to be written into an ERF/HAK/MOD: a name/type plus a way to open its
/// bytes on demand. <see cref="OpenRead"/> is only invoked while the writer is actually streaming
/// that resource's data to the output, so a hak of many large binary resources never needs all of
/// them open (or in memory) at once.</summary>
public sealed class ErfResourceSource
{
    public Resref ResRef { get; }
    public ResourceType Type { get; }
    public long Length { get; }
    public Func<Stream> OpenRead { get; }

    public ErfResourceSource(Resref resRef, ResourceType type, long length, Func<Stream> openRead)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Resource length cannot be negative.");
        }

        ResRef = resRef;
        Type = type;
        Length = length;
        OpenRead = openRead;
    }

    public static ErfResourceSource FromFile(Resref resRef, ResourceType type, string path)
    {
        var length = new FileInfo(path).Length;
        return new ErfResourceSource(resRef, type, length, () => File.OpenRead(path));
    }

    public static ErfResourceSource FromBytes(Resref resRef, ResourceType type, byte[] bytes) =>
        new(resRef, type, bytes.LongLength, () => new MemoryStream(bytes, writable: false));
}
