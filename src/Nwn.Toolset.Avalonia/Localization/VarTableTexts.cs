using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Localization;

/// <summary>Complete host-replaceable text for the local-variable editor.</summary>
public sealed class VarTableTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.VarTableEnglish.json";
    private readonly IReadOnlyDictionary<VarTableStringId, string> _values;
    public static VarTableTexts English { get; } = LoadEnglish();
    public VarTableTexts(IReadOnlyDictionary<VarTableStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<VarTableStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The local-variable catalog does not match its typed IDs.", nameof(values));
        _values = new Dictionary<VarTableStringId, string>(values);
    }
    public string Get(VarTableStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);
    private static VarTableTexts LoadEnglish()
    {
        using var stream = typeof(VarTableTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The local-variable string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<VarTableStringId, string>>(stream)
            ?? throw new InvalidOperationException("The local-variable string catalog is null.");
        return new(values);
    }
}
