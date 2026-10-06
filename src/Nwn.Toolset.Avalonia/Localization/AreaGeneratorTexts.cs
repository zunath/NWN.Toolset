using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Localization;

/// <summary>Complete host-replaceable text for the shared Area Generator.</summary>
public sealed class AreaGeneratorTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.AreaGeneratorEnglish.json";
    private readonly IReadOnlyDictionary<AreaGeneratorStringId, string> _values;

    public static AreaGeneratorTexts English { get; } = LoadEnglish();

    public AreaGeneratorTexts(IReadOnlyDictionary<AreaGeneratorStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<AreaGeneratorStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The Area Generator catalog does not match its typed IDs.", nameof(values));
        _values = new Dictionary<AreaGeneratorStringId, string>(values);
    }

    public string Get(AreaGeneratorStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    private static AreaGeneratorTexts LoadEnglish()
    {
        using var stream = typeof(AreaGeneratorTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The Area Generator string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<AreaGeneratorStringId, string>>(stream)
            ?? throw new InvalidOperationException("The Area Generator string catalog is null.");
        return new(values);
    }
}
