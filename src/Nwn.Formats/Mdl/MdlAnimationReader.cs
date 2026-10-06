using System.Buffers.Binary;
using System.Text;

namespace Nwn.Formats.Mdl;

/// <summary>
/// Reads a model's supermodel name and animation names, from either model format the game loads: an
/// ASCII model (<c>setsupermodel</c> and <c>newanim</c> lines) or a compiled binary model (which starts
/// with four zero bytes). Nothing else in the model is interpreted.
///
/// Binary layout used, as offsets from the start of the model data (file offset 12): the model name is
/// a 64-byte field at 8, the animation pointer array's offset and count are at 120 and 124, and the
/// supermodel name is a 64-byte field at 168. Each animation pointer is relative to the model data and
/// points at an animation header whose 64-byte name field is at 8.
/// </summary>
public static class MdlAnimationReader
{
    private const int ModelDataOffset = 12;
    private const int AnimationArrayOffsetField = 120;
    private const int AnimationArrayCountField = 124;
    private const int SuperModelNameField = 168;
    private const int AnimationNameField = 8;
    private const int NameFieldLength = 64;
    private const string NoSuperModel = "NULL";
    private const string AsciiSuperModelKeyword = "setsupermodel";
    private const string AsciiAnimationKeyword = "newanim";

    public static MdlAnimationSet Read(byte[] model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return IsBinary(model) ? ReadBinary(model) : ReadAscii(model);
    }

    private static bool IsBinary(byte[] model) =>
        model.Length >= ModelDataOffset && BinaryPrimitives.ReadUInt32LittleEndian(model) == 0;

    private static MdlAnimationSet ReadBinary(byte[] model)
    {
        var arrayOffset = ReadUInt32(model, ModelDataOffset + AnimationArrayOffsetField);
        var count = ReadUInt32(model, ModelDataOffset + AnimationArrayCountField);
        var animations = new List<string>((int)Math.Min(count, 1024));
        for (var i = 0; i < count; i++)
        {
            var header = ReadUInt32(model, ModelDataOffset + checked((int)arrayOffset + (i * 4)));
            animations.Add(ReadName(model, ModelDataOffset + checked((int)header) + AnimationNameField));
        }

        return new(SuperModelOrNull(ReadName(model, ModelDataOffset + SuperModelNameField)), animations);
    }

    private static MdlAnimationSet ReadAscii(byte[] model)
    {
        string? superModel = null;
        var animations = new List<string>();
        foreach (var line in Encoding.Latin1.GetString(model).Split('\n'))
        {
            var words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 3 && words[0].Equals(AsciiSuperModelKeyword, StringComparison.OrdinalIgnoreCase))
            {
                superModel = SuperModelOrNull(words[2]);
            }
            else if (words.Length >= 2 && words[0].Equals(AsciiAnimationKeyword, StringComparison.OrdinalIgnoreCase))
            {
                animations.Add(words[1]);
            }
        }

        return new(superModel, animations);
    }

    private static uint ReadUInt32(byte[] model, int offset)
    {
        if (offset < 0 || offset + 4 > model.Length)
        {
            throw new FormatException($"Binary model is truncated: needed 4 bytes at offset {offset} of {model.Length}.");
        }

        return BinaryPrimitives.ReadUInt32LittleEndian(model.AsSpan(offset, 4));
    }

    private static string ReadName(byte[] model, int offset)
    {
        if (offset < 0 || offset + NameFieldLength > model.Length)
        {
            throw new FormatException($"Binary model is truncated: needed a {NameFieldLength}-byte name at offset {offset} of {model.Length}.");
        }

        var field = model.AsSpan(offset, NameFieldLength);
        var end = field.IndexOf((byte)0);
        return Encoding.Latin1.GetString(end < 0 ? field : field[..end]);
    }

    private static string? SuperModelOrNull(string name) =>
        name.Length == 0 || name.Equals(NoSuperModel, StringComparison.OrdinalIgnoreCase) ? null : name;
}
