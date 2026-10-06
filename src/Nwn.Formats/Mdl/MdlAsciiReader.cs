using System.Globalization;
using System.Numerics;
using System.Text;

namespace Nwn.Formats.Mdl;

/// <summary>Reads bounded static geometry from ASCII NWN MDL files. Animation presence is reported;
/// animation tracks and non-dummy/non-trimesh nodes are not evaluated.</summary>
public static class MdlAsciiReader
{
    private const string NullParent = "NULL";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static MdlScene Read(ReadOnlySpan<byte> bytes, MdlAsciiReadOptions? options = null)
    {
        options ??= new MdlAsciiReadOptions();
        options.Validate();
        if (bytes.Length > options.MaximumInputBytes)
            throw new FormatException($"ASCII MDL input size {bytes.Length} exceeds the configured limit {options.MaximumInputBytes}.");
        if (bytes.IsEmpty)
            throw new FormatException("ASCII MDL input is empty.");
        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
            throw new NotSupportedException("Compiled binary MDL is unsupported by the ASCII geometry reader.");
        if (bytes.Contains((byte)0))
            throw new FormatException("ASCII MDL input contains a NUL byte.");

        string text;
        try
        {
            text = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new FormatException("ASCII MDL input is not valid UTF-8/ASCII text.", exception);
        }

        var cursor = new LineCursor(text);
        var modelName = string.Empty;
        var geometryName = string.Empty;
        var nodes = new List<MdlNode>();
        var hasGeometry = false;
        var geometryEnded = false;
        var hasAnimations = false;
        var inAnimation = false;
        var totals = new ParseTotals();
        while (cursor.TryRead(out var line))
        {
            if (line.Tokens.Length == 0)
                continue;
            var keyword = line.Tokens[0];
            if (inAnimation)
            {
                if (keyword.Equals("doneanim", StringComparison.OrdinalIgnoreCase))
                    inAnimation = false;
                continue;
            }
            if (keyword.Equals("newmodel", StringComparison.OrdinalIgnoreCase))
            {
                RequireTokenCount(line, 2);
                if (modelName.Length != 0)
                    throw Error(line, "Multiple newmodel declarations are unsupported.");
                modelName = line.Tokens[1];
            }
            else if (keyword.Equals("beginmodelgeom", StringComparison.OrdinalIgnoreCase))
            {
                RequireTokenCount(line, 2);
                if (hasGeometry)
                    throw Error(line, "Multiple geometry blocks are unsupported.");
                hasGeometry = true;
                geometryName = line.Tokens[1];
                ParseGeometry(cursor, line.Tokens[1], nodes, totals, options);
                geometryEnded = true;
            }
            else if (keyword.Equals("newanim", StringComparison.OrdinalIgnoreCase))
            {
                hasAnimations = true;
                inAnimation = true;
            }
            else if (keyword.Equals("node", StringComparison.OrdinalIgnoreCase) ||
                     keyword.Equals("endmodelgeom", StringComparison.OrdinalIgnoreCase))
            {
                throw Error(line, "Node declarations must be inside one beginmodelgeom/endmodelgeom block.");
            }
        }

        if (modelName.Length == 0)
            throw new FormatException("ASCII MDL is missing a newmodel declaration.");
        if (!hasGeometry || !geometryEnded)
            throw new FormatException("ASCII MDL is missing a complete geometry block.");
        if (inAnimation)
            throw new FormatException("ASCII MDL animation section is truncated before doneanim.");
        if (!geometryName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
            throw new FormatException($"Geometry block model '{geometryName}' does not match newmodel '{modelName}'.");
        ValidateHierarchy(nodes);
        return new MdlScene(modelName, nodes.ToArray(), hasAnimations);
    }

    private static void ParseGeometry(LineCursor cursor, string geometryName, List<MdlNode> nodes, ParseTotals totals,
        MdlAsciiReadOptions options)
    {
        while (cursor.TryRead(out var line))
        {
            if (line.Tokens.Length == 0)
                continue;
            if (line.Tokens[0].Equals("endmodelgeom", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Tokens.Length is not (1 or 2) ||
                    (line.Tokens.Length == 2 && !line.Tokens[1].Equals(geometryName, StringComparison.OrdinalIgnoreCase)))
                    throw Error(line, $"endmodelgeom must omit its name or repeat '{geometryName}'.");
                return;
            }
            if (!line.Tokens[0].Equals("node", StringComparison.OrdinalIgnoreCase))
                throw Error(line, "Expected a node declaration or endmodelgeom.");
            if (nodes.Count >= options.MaximumNodeCount)
                throw Error(line, $"Node count exceeds the configured limit {options.MaximumNodeCount}.");
            nodes.Add(ParseNode(cursor, line, totals, options));
        }

        throw new FormatException("ASCII MDL geometry block is truncated before endmodelgeom.");
    }

    private static MdlNode ParseNode(LineCursor cursor, MdlLine declaration, ParseTotals totals,
        MdlAsciiReadOptions options)
    {
        RequireTokenCount(declaration, 3);
        var type = declaration.Tokens[1].ToLowerInvariant() switch
        {
            "dummy" => MdlNodeType.Dummy,
            "trimesh" => MdlNodeType.Trimesh,
            var unsupported => throw new NotSupportedException($"ASCII MDL line {declaration.Number}: node type '{unsupported}' is unsupported by the static reader.")
        };
        var name = declaration.Tokens[2];
        string? parentName = null;
        var hasParent = false;
        var position = Vector3.Zero;
        var orientation = Vector4.Zero;
        var scale = 1.0f;
        var render = true;
        string? bitmap = null;
        string? materialName = null;
        Vector3[] vertices = [];
        Vector3[] normals = [];
        Vector2[] textureVertices = [];
        MdlTriangle[] faces = [];
        var seenArrays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasMeshData = false;

        while (cursor.TryRead(out var line))
        {
            if (line.Tokens.Length == 0)
                continue;
            var keyword = line.Tokens[0].ToLowerInvariant();
            if (keyword == "endnode")
            {
                RequireTokenCount(line, 1);
                if (!hasParent)
                    throw Error(declaration, $"Node '{name}' is missing its parent declaration.");
                if (type == MdlNodeType.Dummy && hasMeshData)
                    throw Error(declaration, $"Dummy node '{name}' contains mesh arrays.");
                if (type == MdlNodeType.Trimesh && (vertices.Length == 0 || faces.Length == 0))
                    throw Error(declaration, $"Trimesh node '{name}' requires vertices and faces.");
                if (normals.Length != 0 && normals.Length != vertices.Length)
                    throw Error(declaration, $"Trimesh node '{name}' has {normals.Length} normals for {vertices.Length} vertices.");
                ValidateFaces(declaration, name, vertices, textureVertices, faces);
                MdlMesh? mesh = type == MdlNodeType.Trimesh
                    ? new MdlMesh(vertices, normals, textureVertices, faces, bitmap, materialName)
                    : null;
                return new MdlNode(type, name, parentName, position, orientation, scale, render, mesh);
            }

            switch (keyword)
            {
                case "parent":
                    RequireTokenCount(line, 2);
                    if (hasParent)
                        throw Error(line, "A node cannot declare its parent twice.");
                    parentName = line.Tokens[1].Equals(NullParent, StringComparison.OrdinalIgnoreCase)
                        ? null
                        : line.Tokens[1];
                    hasParent = true;
                    break;
                case "position":
                    RequireTokenCount(line, 4);
                    position = ReadVector3(line, 1);
                    break;
                case "orientation":
                    RequireTokenCount(line, 5);
                    orientation = new Vector4(ReadFloat(line, 1), ReadFloat(line, 2), ReadFloat(line, 3), ReadFloat(line, 4));
                    break;
                case "scale":
                    RequireTokenCount(line, 2);
                    scale = ReadFloat(line, 1);
                    break;
                case "render":
                    RequireTokenCount(line, 2);
                    render = line.Tokens[1] switch
                    {
                        "0" => false,
                        "1" => true,
                        _ when line.Tokens[1].Equals("false", StringComparison.OrdinalIgnoreCase) => false,
                        _ when line.Tokens[1].Equals("true", StringComparison.OrdinalIgnoreCase) => true,
                        _ => throw Error(line, "Render must be 0, 1, false, or true.")
                    };
                    break;
                case "bitmap":
                    RequireTokenCount(line, 2);
                    bitmap = line.Tokens[1];
                    break;
                case "materialname":
                    RequireTokenCount(line, 2);
                    materialName = line.Tokens[1];
                    break;
                case "verts":
                    MarkArray(line, seenArrays, ref hasMeshData);
                    vertices = ReadVector3Array(cursor, line, options, totals, isVertexArray: true);
                    break;
                case "normals":
                    MarkArray(line, seenArrays, ref hasMeshData);
                    normals = ReadVector3Array(cursor, line, options, totals, isVertexArray: true);
                    break;
                case "tverts":
                    MarkArray(line, seenArrays, ref hasMeshData);
                    textureVertices = ReadVector2Array(cursor, line, options, totals);
                    break;
                case "faces":
                    MarkArray(line, seenArrays, ref hasMeshData);
                    faces = ReadFaces(cursor, line, options, totals);
                    break;
                case "ambient":
                case "diffuse":
                case "specular":
                case "selfillumcolor":
                case "wirecolor":
                    _ = ReadVector3(line, 1, expectedTokenCount: 4);
                    break;
                case "shininess":
                case "lightmapped":
                case "transparencyhint":
                case "tilefade":
                case "rotatetexture":
                case "shadow":
                case "radius":
                    RequireTokenCount(line, 2);
                    break;
                case "renderhint":
                    RequireTokenCount(line, 2);
                    break;
                default:
                    throw Error(line, $"Node property '{line.Tokens[0]}' is unsupported by the static ASCII reader.");
            }
        }

        throw Error(declaration, $"Node '{name}' is truncated before endnode.");
    }

    private static Vector3[] ReadVector3Array(LineCursor cursor, MdlLine declaration,
        MdlAsciiReadOptions options, ParseTotals totals, bool isVertexArray)
    {
        var count = ReadArrayCount(declaration, options, totals, isVertexArray);
        var result = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            var line = ReadRequiredArrayLine(cursor, declaration, i);
            RequireTokenCount(line, 3);
            result[i] = ReadVector3(line, 0);
        }
        return result;
    }

    private static Vector2[] ReadVector2Array(LineCursor cursor, MdlLine declaration,
        MdlAsciiReadOptions options, ParseTotals totals)
    {
        var count = ReadArrayCount(declaration, options, totals, isVertexArray: true);
        var result = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var line = ReadRequiredArrayLine(cursor, declaration, i);
            if (line.Tokens.Length is not (2 or 3))
                throw Error(line, "Texture vertices require U and V, with an optional zero third component.");
            result[i] = new Vector2(ReadFloat(line, 0), ReadFloat(line, 1));
            if (line.Tokens.Length == 3 && ReadFloat(line, 2) != 0)
                throw Error(line, "The third texture-vertex component must be zero.");
        }
        return result;
    }

    private static MdlTriangle[] ReadFaces(LineCursor cursor, MdlLine declaration,
        MdlAsciiReadOptions options, ParseTotals totals)
    {
        var count = ReadArrayCount(declaration, options, totals, isVertexArray: false);
        var result = new MdlTriangle[count];
        for (var i = 0; i < count; i++)
        {
            var line = ReadRequiredArrayLine(cursor, declaration, i);
            RequireTokenCount(line, 8);
            result[i] = new MdlTriangle(ReadInteger(line, 0), ReadInteger(line, 1), ReadInteger(line, 2),
                ReadInteger(line, 3), ReadInteger(line, 4), ReadInteger(line, 5), ReadInteger(line, 6), ReadInteger(line, 7));
        }
        return result;
    }

    private static int ReadArrayCount(MdlLine declaration, MdlAsciiReadOptions options, ParseTotals totals, bool isVertexArray)
    {
        RequireTokenCount(declaration, 2);
        var count = ReadInteger(declaration, 1);
        if (count < 0)
            throw Error(declaration, "Array count cannot be negative.");
        if (isVertexArray)
        {
            if (count > options.MaximumVertexCount - totals.VertexCount)
                throw Error(declaration, $"Combined vertex/normal/UV count exceeds the configured limit {options.MaximumVertexCount}.");
            totals.VertexCount += count;
        }
        else
        {
            if (count > options.MaximumFaceCount - totals.FaceCount)
                throw Error(declaration, $"Face count exceeds the configured limit {options.MaximumFaceCount}.");
            totals.FaceCount += count;
        }
        return count;
    }

    private static MdlLine ReadRequiredArrayLine(LineCursor cursor, MdlLine declaration, int index)
    {
        if (!cursor.TryRead(out var line) || line.Tokens.Length == 0)
            throw Error(declaration, $"Array '{declaration.Tokens[0]}' is truncated at element {index}.");
        return line;
    }

    private static void ValidateFaces(MdlLine declaration, string name, Vector3[] vertices,
        Vector2[] textureVertices, MdlTriangle[] faces)
    {
        for (var i = 0; i < faces.Length; i++)
        {
            var face = faces[i];
            if (face.VertexA < 0 || face.VertexB < 0 || face.VertexC < 0 ||
                face.VertexA >= vertices.Length || face.VertexB >= vertices.Length || face.VertexC >= vertices.Length)
                throw Error(declaration, $"Trimesh '{name}' face {i} references a vertex outside its vertex array.");
            if (face.TextureA < 0 || face.TextureB < 0 || face.TextureC < 0)
                throw Error(declaration, $"Trimesh '{name}' face {i} contains a negative texture-vertex index.");
            if (textureVertices.Length > 0 &&
                (face.TextureA >= textureVertices.Length || face.TextureB >= textureVertices.Length || face.TextureC >= textureVertices.Length))
                throw Error(declaration, $"Trimesh '{name}' face {i} references a texture vertex outside its tvert array.");
            if (face.SmoothingGroup < 0 || face.MaterialIndex < 0)
                throw Error(declaration, $"Trimesh '{name}' face {i} has a negative smoothing-group or material index.");
        }
    }

    private static void ValidateHierarchy(List<MdlNode> nodes)
    {
        if (nodes.Count == 0)
            throw new FormatException("ASCII MDL geometry contains no nodes.");
        var byName = new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            if (!byName.TryAdd(node.Name, node))
                throw new FormatException($"ASCII MDL contains duplicate node name '{node.Name}'.");
        }
        foreach (var node in nodes)
        {
            if (node.ParentName is not null && !byName.ContainsKey(node.ParentName))
                throw new FormatException($"Node '{node.Name}' refers to missing parent '{node.ParentName}'.");
        }
        var complete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            var path = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = node;
            while (current.ParentName is not null && !complete.Contains(current.Name))
            {
                if (!path.Add(current.Name))
                    throw new FormatException($"ASCII MDL node hierarchy contains a cycle at '{current.Name}'.");
                current = byName[current.ParentName];
            }
            foreach (var name in path)
                complete.Add(name);
        }
    }

    private static Vector3 ReadVector3(MdlLine line, int start, int expectedTokenCount = 0)
    {
        if (expectedTokenCount != 0)
            RequireTokenCount(line, expectedTokenCount);
        return new Vector3(ReadFloat(line, start), ReadFloat(line, start + 1), ReadFloat(line, start + 2));
    }

    private static float ReadFloat(MdlLine line, int index)
    {
        if (!float.TryParse(line.Tokens[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value))
            throw Error(line, $"'{line.Tokens[index]}' is not a finite invariant-culture number.");
        return value;
    }

    private static int ReadInteger(MdlLine line, int index)
    {
        if (!int.TryParse(line.Tokens[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw Error(line, $"'{line.Tokens[index]}' is not an integer.");
        return value;
    }

    private static void MarkArray(MdlLine line, HashSet<string> seen, ref bool hasMeshData)
    {
        if (!seen.Add(line.Tokens[0]))
            throw Error(line, $"Array '{line.Tokens[0]}' is declared more than once in a node.");
        hasMeshData = true;
    }

    private static void RequireTokenCount(MdlLine line, int count)
    {
        if (line.Tokens.Length != count)
            throw Error(line, $"Expected {count} token(s), found {line.Tokens.Length}.");
    }

    private static FormatException Error(MdlLine line, string message) =>
        new($"ASCII MDL line {line.Number}: {message}");

    private sealed class ParseTotals
    {
        public int VertexCount { get; set; }
        public int FaceCount { get; set; }
    }

    private readonly record struct MdlLine(int Number, string[] Tokens);

    private sealed class LineCursor(string text)
    {
        private readonly string[] _lines = text.Split('\n');
        private int _index;

        public bool TryRead(out MdlLine line)
        {
            while (_index < _lines.Length)
            {
                var number = ++_index;
                var content = _lines[number - 1];
                var comment = content.IndexOf('#');
                if (comment >= 0)
                    content = content[..comment];
                var tokens = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;
                line = new MdlLine(number, tokens);
                return true;
            }
            line = default;
            return false;
        }
    }
}
