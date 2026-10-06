using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>Complete host-replaceable text for the schema field editors.</summary>
public sealed class FieldTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.FieldEnglish.json";
    private readonly IReadOnlyDictionary<FieldStringId, string> _values;

    public static FieldTexts English { get; } = LoadEnglish();

    public FieldTexts(IReadOnlyDictionary<FieldStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<FieldStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The field editor catalog does not match its typed IDs.", nameof(values));

        _values = new Dictionary<FieldStringId, string>(values);
    }

    public string OpenTlkRow => Get(FieldStringId.OpenTlkRow);
    public string OpenTlkRowTooltip => Get(FieldStringId.OpenTlkRowTooltip);
    public string LookupUnavailableTooltip => Get(FieldStringId.LookupUnavailableTooltip);
    public string BrowseScript => Get(FieldStringId.BrowseScript);
    public string BrowseScriptTooltip => Get(FieldStringId.BrowseScriptTooltip);
    public string OpenScript => Get(FieldStringId.OpenScript);
    public string OpenScriptTooltip => Get(FieldStringId.OpenScriptTooltip);

    public string Get(FieldStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    private static FieldTexts LoadEnglish()
    {
        using var stream = typeof(FieldTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The field editor string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<FieldStringId, string>>(stream)
            ?? throw new InvalidOperationException("The field editor string catalog is null.");
        return new(values);
    }
}
