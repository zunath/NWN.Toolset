using System.Globalization;
using System.Text;
using System.Text.Json;
using Nwn.Authoring.Areas.Editing;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Builds small in-memory native documents and area edit sessions for property tests.</summary>
internal static class NativeTestDocuments
{
    public static AreaDocumentEditSession CreateArea(JsonGffDocument? area = null)
    {
        area ??= Create("ARE ");
        return new AreaDocumentEditSession(
            new DocumentSession("area.are", area),
            new DocumentSession("area.git", Create("GIT ")),
            new DocumentSession("area.gic", Create("GIC ")));
    }

    public static JsonGffDocument Create(string type)
    {
        var root = JsonGffField.CreateStruct(0).Struct!;
        return new JsonGffDocument(type, root);
    }

    public static JsonGffDocument Parse(string json) => JsonGffDocument.Parse(Encoding.UTF8.GetBytes(json));

    public static JsonGffStruct Struct(params (string Name, JsonGffField Field)[] fields)
    {
        var value = JsonGffField.CreateStruct(0).Struct!;
        foreach (var (name, field) in fields)
            value.Add(name, field);
        return value;
    }

    public static JsonGffStruct Placement(string tag, string templateResRef)
    {
        return Struct(
            ("Tag", Text(GffFieldType.CExoString, tag)),
            ("TemplateResRef", Text(GffFieldType.ResRef, templateResRef)),
            ("XPosition", Float(0f)),
            ("YPosition", Float(0f)),
            ("ZPosition", Float(0f)),
            ("XOrientation", Float(1f)),
            ("YOrientation", Float(0f)));
    }

    public static JsonGffField Text(GffFieldType type, string value) =>
        JsonGffField.CreateScalar(type, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));

    public static JsonGffField Integer(GffFieldType type, long value) =>
        JsonGffField.CreateScalar(type, Encoding.ASCII.GetBytes(value.ToString(CultureInfo.InvariantCulture)));

    public static JsonGffField Float(float value) => JsonGffField.CreateScalar(
        GffFieldType.Float, Encoding.ASCII.GetBytes(value.ToString("R", CultureInfo.InvariantCulture)));
}
