using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nwn.Formats.Gff;

/// <summary>
/// A lossless, stable GFF &lt;-&gt; JSON mapping, so module/blueprint sources can live in
/// <c>content/</c> as reviewable text instead of binary GFF. Field order is preserved (it is part of
/// a struct's identity); struct/field JSON shape is this pipeline's own design, not a copy of any
/// other tool's GFF-JSON convention.
///
/// Shape: <c>{"fileType":"UTC ","fileVersion":"V3.2","root":&lt;struct&gt;}</c>. A struct is
/// <c>{"id":&lt;uint&gt;,"fields":{"Label":&lt;field&gt;, ...}}</c> (an object, so field order survives
/// standard JSON parsing/serialization). A field is <c>{"type":"int","value":...}</c>; Void is
/// base64, ResRef/String are plain strings, Struct's value is a nested struct object, List's value
/// is an array of struct objects, LocString's value is
/// <c>{"strRef":-1,"strings":[{"language":0,"gender":0,"text":"..."}]}</c>.
/// </summary>
public static class GffJson
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static string ToJson(GffDocument document)
    {
        var root = new JsonObject
        {
            ["fileType"] = document.FileType,
            ["fileVersion"] = document.FileVersion,
            ["root"] = StructToJson(document.Root),
        };
        return root.ToJsonString(WriteOptions);
    }

    public static GffDocument FromJson(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject ?? throw new FormatException("GFF JSON root must be an object.");
        var fileType = (string?)node["fileType"] ?? throw new FormatException("GFF JSON is missing 'fileType'.");
        var fileVersion = (string?)node["fileVersion"] ?? "V3.2";
        var rootNode = node["root"] as JsonObject ?? throw new FormatException("GFF JSON is missing a 'root' struct.");
        return new GffDocument { FileType = fileType, FileVersion = fileVersion, Root = StructFromJson(rootNode) };
    }

    private static JsonObject StructToJson(GffStruct value)
    {
        var fields = new JsonObject();
        foreach (var field in value.Fields)
        {
            fields[field.Label] = FieldToJson(field);
        }

        return new JsonObject { ["id"] = value.StructId, ["fields"] = fields };
    }

    private static GffStruct StructFromJson(JsonObject node)
    {
        var id = (uint?)node["id"] ?? throw new FormatException("GFF JSON struct is missing 'id'.");
        var result = new GffStruct(id);
        var fields = node["fields"] as JsonObject ?? throw new FormatException("GFF JSON struct is missing 'fields'.");
        foreach (var (label, fieldNode) in fields)
        {
            result.Add(FieldFromJson(label, fieldNode as JsonObject ?? throw new FormatException($"Field '{label}' is not an object.")));
        }

        return result;
    }

    private static JsonObject FieldToJson(GffField field)
    {
        JsonNode value = field.Type switch
        {
            GffFieldType.Byte => field.AsByte(),
            GffFieldType.Char => field.AsChar(),
            GffFieldType.Word => field.AsWord(),
            GffFieldType.Short => field.AsShort(),
            GffFieldType.Dword => field.AsDword(),
            GffFieldType.Int => field.AsInt(),
            GffFieldType.Dword64 => field.AsDword64().ToString(),
            GffFieldType.Int64 => field.AsInt64().ToString(),
            GffFieldType.Float => field.AsFloat(),
            GffFieldType.Double => field.AsDouble(),
            GffFieldType.String => field.AsString(),
            GffFieldType.ResRef => field.AsResRef(),
            GffFieldType.Void => Convert.ToBase64String(field.AsVoid()),
            GffFieldType.Struct => StructToJson(field.AsStruct()),
            GffFieldType.List => new JsonArray(field.AsList().Select(s => (JsonNode)StructToJson(s)).ToArray()),
            GffFieldType.LocString => LocStringToJson(field.AsLocString()),
            _ => throw new FormatException($"Unsupported GFF field type {field.Type}."),
        };
        return new JsonObject { ["type"] = TypeName(field.Type), ["value"] = value };
    }

    private static GffField FieldFromJson(string label, JsonObject node)
    {
        var typeName = (string?)node["type"] ?? throw new FormatException($"Field '{label}' is missing 'type'.");
        var value = node["value"] ?? throw new FormatException($"Field '{label}' is missing 'value'.");
        return typeName switch
        {
            "byte" => GffField.Byte(label, (byte)value),
            "char" => GffField.Char(label, (sbyte)value),
            "word" => GffField.Word(label, (ushort)value),
            "short" => GffField.Short(label, (short)value),
            "dword" => GffField.Dword(label, (uint)value),
            "int" => GffField.Int(label, (int)value),
            "dword64" => GffField.Dword64(label, ulong.Parse((string)value!)),
            "int64" => GffField.Int64(label, long.Parse((string)value!)),
            "float" => GffField.Float(label, (float)value),
            "double" => GffField.Double(label, (double)value),
            "string" => GffField.String(label, (string)value!),
            "resref" => GffField.ResRef(label, (string)value!),
            "void" => GffField.Void(label, Convert.FromBase64String((string)value!)),
            "struct" => GffField.Struct(label, StructFromJson((JsonObject)value)),
            "list" => GffField.List(label, ((JsonArray)value).Select(n => StructFromJson((JsonObject)n!)).ToList()),
            "locstring" => GffField.LocString(label, LocStringFromJson((JsonObject)value)),
            _ => throw new FormatException($"Field '{label}': unknown GFF JSON type '{typeName}'."),
        };
    }

    private static JsonObject LocStringToJson(GffLocString value)
    {
        var strings = new JsonArray(value.Strings
            .Select(entry => (JsonNode)new JsonObject { ["language"] = entry.Language, ["gender"] = entry.Gender, ["text"] = entry.Text })
            .ToArray());
        var hasStringRef = value.StringRef != GffLocString.NoStringRef;
        return new JsonObject
        {
            ["strRef"] = hasStringRef ? value.StringRef : -1,
            ["strings"] = strings,
        };
    }

    private static GffLocString LocStringFromJson(JsonObject node)
    {
        var strRef = (long?)node["strRef"] ?? -1;
        var strings = (node["strings"] as JsonArray ?? [])
            .Select(n =>
            {
                var entry = (JsonObject)n!;
                return GffLocStringEntry.FromLanguageGender((int)entry["language"]!, (int)entry["gender"]!, (string)entry["text"]!);
            })
            .ToList();
        return new GffLocString
        {
            StringRef = strRef < 0 ? GffLocString.NoStringRef : (uint)strRef,
            Strings = strings,
        };
    }

    private static string TypeName(GffFieldType type) => type switch
    {
        GffFieldType.Byte => "byte",
        GffFieldType.Char => "char",
        GffFieldType.Word => "word",
        GffFieldType.Short => "short",
        GffFieldType.Dword => "dword",
        GffFieldType.Int => "int",
        GffFieldType.Dword64 => "dword64",
        GffFieldType.Int64 => "int64",
        GffFieldType.Float => "float",
        GffFieldType.Double => "double",
        GffFieldType.String => "string",
        GffFieldType.ResRef => "resref",
        GffFieldType.Void => "void",
        GffFieldType.Struct => "struct",
        GffFieldType.List => "list",
        GffFieldType.LocString => "locstring",
        _ => throw new FormatException($"Unsupported GFF field type {type}."),
    };
}
