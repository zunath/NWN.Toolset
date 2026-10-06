// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Localization;

/// <summary>Complete host-replaceable text for the area selection context menu.</summary>
public sealed class AreaSelectionTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.AreaSelectionEnglish.json";
    private readonly IReadOnlyDictionary<AreaSelectionStringId, string> _values;

    public static AreaSelectionTexts English { get; } = LoadEnglish();

    public AreaSelectionTexts(IReadOnlyDictionary<AreaSelectionStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<AreaSelectionStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The area-selection catalog does not match its typed IDs.", nameof(values));
        _values = new Dictionary<AreaSelectionStringId, string>(values);
    }

    public string Get(AreaSelectionStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    private static AreaSelectionTexts LoadEnglish()
    {
        using var stream = typeof(AreaSelectionTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The area-selection string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<AreaSelectionStringId, string>>(stream)
            ?? throw new InvalidOperationException("The area-selection string catalog is null.");
        return new(values);
    }
}
