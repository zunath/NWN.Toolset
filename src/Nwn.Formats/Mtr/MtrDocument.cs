using System.Collections.ObjectModel;

namespace Nwn.Formats.Mtr;

/// <summary>Immutable material bindings and parameters, independent of any game's shader policy.</summary>
public sealed class MtrDocument
{
    private readonly byte[] _sourceBytes;

    public IReadOnlyDictionary<int, string?> Textures { get; }
    public IReadOnlyDictionary<MtrShaderStage, string?> Shaders { get; }
    public IReadOnlyDictionary<string, string?> RawShaderBindings { get; }
    public IReadOnlyDictionary<string, MtrParameter> Parameters { get; }
    public string? RenderHint { get; }
    public IReadOnlyList<MtrUnrecognizedDirective> UnrecognizedDirectives { get; }

    internal MtrDocument(ReadOnlySpan<byte> sourceBytes, Dictionary<int, string?> textures,
        Dictionary<MtrShaderStage, string?> shaders, Dictionary<string, string?> rawShaders, Dictionary<string, MtrParameter> parameters,
        string? renderHint, List<MtrUnrecognizedDirective> unrecognized)
    {
        _sourceBytes = sourceBytes.ToArray();
        Textures = new ReadOnlyDictionary<int, string?>(new Dictionary<int, string?>(textures));
        Shaders = new ReadOnlyDictionary<MtrShaderStage, string?>(new Dictionary<MtrShaderStage, string?>(shaders));
        RawShaderBindings = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(rawShaders, StringComparer.OrdinalIgnoreCase));
        Parameters = new ReadOnlyDictionary<string, MtrParameter>(new Dictionary<string, MtrParameter>(parameters, StringComparer.Ordinal));
        RenderHint = renderHint;
        UnrecognizedDirectives = new ReadOnlyCollection<MtrUnrecognizedDirective>(unrecognized.ToArray());
    }

    /// <summary>Returns the original bytes, including comments, unknown directives and lexical details.</summary>
    public byte[] CopySourceBytes() => _sourceBytes.ToArray();
}
