using System.Globalization;
using System.Text.Json;
using Nwn.Toolset.Avalonia.Sounds;

namespace Nwn.Toolset.Avalonia.Localization;

/// <summary>Host-replaceable text for the ordered native sound-list control.</summary>
public sealed class SoundListTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.SoundListEnglish.json";
    private readonly IReadOnlyDictionary<SoundListStringId, string> _values;

    public static SoundListTexts English { get; } = LoadEnglish();

    public SoundListTexts(IReadOnlyDictionary<SoundListStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var identifiers = Enum.GetValues<SoundListStringId>();
        if (values.Count != identifiers.Length || identifiers.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The sound-list catalog does not match its typed IDs.", nameof(values));
        _values = new Dictionary<SoundListStringId, string>(values);
    }

    public string Get(SoundListStringId id, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], arguments);

    private static SoundListTexts LoadEnglish()
    {
        using var stream = typeof(SoundListTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The sound-list string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<SoundListStringId, string>>(stream)
            ?? throw new InvalidOperationException("The sound-list string catalog is null.");
        return new(values);
    }
}
