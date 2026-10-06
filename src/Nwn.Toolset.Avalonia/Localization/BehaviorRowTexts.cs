using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Localization;

/// <summary>Complete host-replaceable text for the shared property row and its pickers.</summary>
public sealed class BehaviorRowTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.BehaviorRowEnglish.json";
    private readonly IReadOnlyDictionary<BehaviorRowStringId, string> _values;

    public static BehaviorRowTexts English { get; } = LoadEnglish();

    public BehaviorRowTexts(IReadOnlyDictionary<BehaviorRowStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var identifiers = Enum.GetValues<BehaviorRowStringId>();
        if (values.Count != identifiers.Length || identifiers.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The property-row catalog does not match its typed IDs.", nameof(values));
        _values = new Dictionary<BehaviorRowStringId, string>(values);
    }

    public string Get(BehaviorRowStringId id, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], arguments);

    private static BehaviorRowTexts LoadEnglish()
    {
        using var stream = typeof(BehaviorRowTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The property-row string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<BehaviorRowStringId, string>>(stream)
            ?? throw new InvalidOperationException("The property-row string catalog is null.");
        return new(values);
    }
}
