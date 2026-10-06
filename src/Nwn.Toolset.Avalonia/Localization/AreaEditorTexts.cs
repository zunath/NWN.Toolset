using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Localization;

/// <summary>Complete host-replaceable text for the area editor.</summary>
public sealed class AreaEditorTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.AreaEditorEnglish.json";
    private readonly IReadOnlyDictionary<AreaEditorStringId, string> _values;
    public static AreaEditorTexts English { get; } = LoadEnglish();
    public AreaEditorTexts(IReadOnlyDictionary<AreaEditorStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<AreaEditorStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The area-editor catalog does not match its typed IDs.", nameof(values));
        _values = new Dictionary<AreaEditorStringId, string>(values);
    }
    public string Get(AreaEditorStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);
    private static AreaEditorTexts LoadEnglish()
    {
        using var stream = typeof(AreaEditorTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The area-editor string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<AreaEditorStringId, string>>(stream)
            ?? throw new InvalidOperationException("The area-editor string catalog is null.");
        return new(values);
    }
}
