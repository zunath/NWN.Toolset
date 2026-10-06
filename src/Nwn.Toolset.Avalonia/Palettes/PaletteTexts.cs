using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Palettes;

/// <summary>Complete host-replaceable text for the shared palette presentation.</summary>
public sealed class PaletteTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.PaletteEnglish.json";
    private readonly IReadOnlyDictionary<PaletteStringId, string> _values;

    public static PaletteTexts English { get; } = LoadEnglish();

    public PaletteTexts(IReadOnlyDictionary<PaletteStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<PaletteStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
        {
            throw new ArgumentException("The Palette catalog does not match its typed IDs.", nameof(values));
        }

        _values = new Dictionary<PaletteStringId, string>(values);
    }

    public string Title => Get(PaletteStringId.Title);
    public string CustomSource => Get(PaletteStringId.CustomSource);
    public string StandardSource => Get(PaletteStringId.StandardSource);
    public string CustomSourceTooltip => Get(PaletteStringId.CustomSourceTooltip);
    public string StandardSourceTooltip => Get(PaletteStringId.StandardSourceTooltip);
    public string AutoTilePaint => Get(PaletteStringId.AutoTilePaint);
    public string ManualTilePaint => Get(PaletteStringId.ManualTilePaint);
    public string AutoTilePaintTooltip => Get(PaletteStringId.AutoTilePaintTooltip);
    public string ManualTilePaintTooltip => Get(PaletteStringId.ManualTilePaintTooltip);
    public string SearchWatermark => Get(PaletteStringId.SearchWatermark);
    public string Categories => Get(PaletteStringId.Categories);
    public string NewCategory => Get(PaletteStringId.NewCategory);
    public string RenameCategory => Get(PaletteStringId.RenameCategory);
    public string DeleteCategory => Get(PaletteStringId.DeleteCategory);
    public string PinToTop => Get(PaletteStringId.PinToTop);
    public string UnpinFromTop => Get(PaletteStringId.UnpinFromTop);
    public string FileSelectedEntry => Get(PaletteStringId.FileSelectedEntry);
    public string GoToCategory => Get(PaletteStringId.GoToCategory);
    public string GridSettings => Get(PaletteStringId.GridSettings);
    public string PreviewSize => Get(PaletteStringId.PreviewSize);
    public string NoAreaOpen => Get(PaletteStringId.NoAreaOpen);
    public string NoAreaOpenDescription => Get(PaletteStringId.NoAreaOpenDescription);
    public string EditBlueprint => Get(PaletteStringId.EditBlueprint);
    public string EditCopy => Get(PaletteStringId.EditCopy);
    public string DeleteBlueprint => Get(PaletteStringId.DeleteBlueprint);
    public string ActionsForBlueprint => Get(PaletteStringId.ActionsForBlueprint);
    public string SelectCategory => Get(PaletteStringId.SelectCategory);
    public string OpenAreaToPlace => Get(PaletteStringId.OpenAreaToPlace);

    public string Get(PaletteStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    private static PaletteTexts LoadEnglish()
    {
        using var stream = typeof(PaletteTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The Palette string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<PaletteStringId, string>>(stream)
            ?? throw new InvalidOperationException("The Palette string catalog is null.");
        return new(values);
    }
}
