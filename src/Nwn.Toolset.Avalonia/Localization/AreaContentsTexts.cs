using System.Globalization;
using System.Text.Json;
namespace Nwn.Toolset.Avalonia.Localization;
/// <summary>Complete host-replaceable text for the shared Area Contents view.</summary>
public sealed class AreaContentsTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.AreaContentsEnglish.json";
    private readonly IReadOnlyDictionary<AreaContentsStringId, string> _values;
    public static AreaContentsTexts English { get; } = LoadEnglish();
    public AreaContentsTexts(IReadOnlyDictionary<AreaContentsStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<AreaContentsStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The Area Contents catalog does not match its typed IDs.", nameof(values));
        _values = new Dictionary<AreaContentsStringId, string>(values);
    }
    public string Get(AreaContentsStringId id, params object?[] args) => string.Format(CultureInfo.CurrentCulture, _values[id], args);
    private static AreaContentsTexts LoadEnglish()
    {
        using var stream = typeof(AreaContentsTexts).Assembly.GetManifestResourceStream(ResourceName) ?? throw new InvalidOperationException("The Area Contents string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<AreaContentsStringId, string>>(stream) ?? throw new InvalidOperationException("The Area Contents string catalog is null.");
        return new(values);
    }
}
