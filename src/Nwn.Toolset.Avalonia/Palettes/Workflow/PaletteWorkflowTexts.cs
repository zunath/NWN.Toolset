using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>Complete host-replaceable text for the shared palette workflow.</summary>
public sealed class PaletteWorkflowTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.PaletteWorkflowEnglish.json";
    private readonly IReadOnlyDictionary<PaletteWorkflowStringId, string> _values;

    public static PaletteWorkflowTexts English { get; } = LoadEnglish();

    public PaletteWorkflowTexts(IReadOnlyDictionary<PaletteWorkflowStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<PaletteWorkflowStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
        {
            throw new ArgumentException("The Palette workflow catalog does not match its typed IDs.", nameof(values));
        }

        _values = new Dictionary<PaletteWorkflowStringId, string>(values);
    }

    public string Get(PaletteWorkflowStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    private static PaletteWorkflowTexts LoadEnglish()
    {
        using var stream = typeof(PaletteWorkflowTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The Palette workflow string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<PaletteWorkflowStringId, string>>(stream)
            ?? throw new InvalidOperationException("The Palette workflow string catalog is null.");
        return new(values);
    }
}
