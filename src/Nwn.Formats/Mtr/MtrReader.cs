using System.Globalization;
using System.Text;

namespace Nwn.Formats.Mtr;

/// <summary>Reads bounded material descriptors without loading or interpreting shader programs.</summary>
public static class MtrReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private const int MaximumTextureSlot = 14;

    public static MtrDocument Read(ReadOnlySpan<byte> bytes, MtrReadOptions? options = null)
    {
        options ??= new MtrReadOptions();
        options.Validate();
        if (bytes.Length > options.MaximumInputBytes)
            throw new FormatException("Material input exceeds its configured byte bound.");
        string text;
        try { text = StrictUtf8.GetString(bytes); }
        catch (DecoderFallbackException exception)
        { throw new FormatException("Material input is not valid UTF-8 text.", exception); }
        if (text.StartsWith('\uFEFF')) text = text[1..];

        var textures = new Dictionary<int, string?>();
        var shaders = new Dictionary<MtrShaderStage, string?>();
        var rawShaders = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var parameters = new Dictionary<string, MtrParameter>(StringComparer.Ordinal);
        var unrecognized = new List<MtrUnrecognizedDirective>();
        string? renderHint = null;
        var hasRenderHint = false;
        var directiveCount = 0;
        var lineNumber = 0;
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } sourceLine)
        {
            lineNumber++;
            if (sourceLine.Length > options.MaximumLineLength)
                throw Error(lineNumber, "The line exceeds its configured character bound.");
            var comment = sourceLine.IndexOf("//", StringComparison.Ordinal);
            var statement = (comment < 0 ? sourceLine : sourceLine[..comment]).Trim();
            if (statement.Length == 0) continue;
            if (++directiveCount > options.MaximumDirectiveCount)
                throw Error(lineNumber, "The directive count exceeds its configured bound.");
            var tokens = statement.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var command = tokens[0].ToLowerInvariant();
            if (command.StartsWith("texture", StringComparison.Ordinal) &&
                int.TryParse(command.AsSpan(7), NumberStyles.None, CultureInfo.InvariantCulture, out var slot) &&
                slot is >= 0 and <= MaximumTextureSlot)
            {
                if (tokens.Length != 2)
                {
                    unrecognized.Add(new(lineNumber, sourceLine));
                    continue;
                }
                if (!textures.TryAdd(slot, ResourceName(tokens[1])))
                    throw Error(lineNumber, $"Duplicate texture binding {slot} is ambiguous.");
            }
            else if (command.StartsWith("customshader", StringComparison.Ordinal) && command.Length > "customshader".Length)
            {
                RequireCount(tokens, 2, lineNumber);
                var shader = ResourceName(tokens[1]);
                if (!rawShaders.TryAdd(tokens[0], shader))
                    throw Error(lineNumber, $"Duplicate '{tokens[0]}' shader binding is ambiguous.");
                if (TryShaderStage(command, out var stage)) shaders.Add(stage, shader);
            }
            else if (command == "renderhint")
            {
                if (tokens.Length == 1)
                {
                    unrecognized.Add(new(lineNumber, sourceLine));
                    continue;
                }
                RequireCount(tokens, 2, lineNumber);
                if (hasRenderHint) throw Error(lineNumber, "Duplicate render hint is ambiguous.");
                hasRenderHint = true;
                renderHint = tokens[1];
            }
            else if (command == "parameter")
            {
                if (tokens.Length < 4) throw Error(lineNumber, "A parameter needs a type, name and value.");
                var kind = tokens[1].ToLowerInvariant();
                if (kind is not ("float" or "int"))
                {
                    if (!parameters.TryAdd(tokens[2], new(tokens[2], MtrParameterKind.Uninterpreted, [], tokens[1], tokens[3..])))
                        throw Error(lineNumber, $"Duplicate parameter '{tokens[2]}' is ambiguous.");
                    continue;
                }
                var componentCount = tokens.Length - 3;
                if (componentCount > 4 || (kind == "int" && componentCount != 1))
                    throw Error(lineNumber, "A float parameter needs one to four components; an int needs one.");
                var values = new double[componentCount];
                for (var index = 0; index < componentCount; index++)
                {
                    if (kind == "int")
                    {
                        if (!int.TryParse(tokens[index + 3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                            throw Error(lineNumber, "The parameter is not a signed 32-bit integer.");
                        values[index] = value;
                    }
                    else
                    {
                        if (!float.TryParse(tokens[index + 3], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value))
                            throw Error(lineNumber, "The parameter is not a finite 32-bit float.");
                        values[index] = value;
                    }
                }
                if (!parameters.TryAdd(tokens[2], new(tokens[2], kind == "int" ? MtrParameterKind.Int32 : MtrParameterKind.Float, values, tokens[1], tokens[3..])))
                    throw Error(lineNumber, $"Duplicate parameter '{tokens[2]}' is ambiguous.");
            }
            else unrecognized.Add(new(lineNumber, sourceLine));
        }
        return new(bytes, textures, shaders, rawShaders, parameters, renderHint, unrecognized);
    }

    private static string? ResourceName(string token) => token.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : token;

    private static void RequireCount(string[] tokens, int count, int lineNumber)
    {
        if (tokens.Length != count) throw Error(lineNumber, $"'{tokens[0]}' needs exactly {count - 1} value(s).");
    }

    private static bool TryShaderStage(string command, out MtrShaderStage stage)
    {
        stage = command switch
        {
            "customshadervs" => MtrShaderStage.Vertex,
            "customshaderfs" => MtrShaderStage.Fragment,
            "customshadergs" => MtrShaderStage.Geometry,
            _ => default,
        };
        return command is "customshadervs" or "customshaderfs" or "customshadergs";
    }

    private static FormatException Error(int lineNumber, string message) => new($"Material line {lineNumber}: {message}");
}
