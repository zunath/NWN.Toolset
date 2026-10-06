using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Appearances;

/// <summary>Complete replaceable text catalog for the appearance gallery.</summary>
public sealed class AppearanceGalleryTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.AppearanceGalleryEnglish.json";
    private readonly IReadOnlyDictionary<AppearanceGalleryStringId, string> _values;

    public static AppearanceGalleryTexts English { get; } = LoadEnglish();

    public AppearanceGalleryTexts(IReadOnlyDictionary<AppearanceGalleryStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<AppearanceGalleryStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The appearance gallery catalog does not match its typed ids.", nameof(values));
        _values = new Dictionary<AppearanceGalleryStringId, string>(values);
    }

    public string DefaultNoun => Get(AppearanceGalleryStringId.DefaultNoun);
    public string NoMatches(string noun) => Get(AppearanceGalleryStringId.NoMatches, noun);
    public string OneMatch(string noun, int count) => Get(AppearanceGalleryStringId.OneMatch, count, noun);
    public string ManyMatches(string noun, int count) => Get(AppearanceGalleryStringId.ManyMatches, count, noun);
    public string PartialMatches(string noun, int shown, int total) => Get(AppearanceGalleryStringId.PartialMatches, shown, total, noun);
    public string SearchWatermark(string noun) => Get(AppearanceGalleryStringId.SearchWatermark, noun);
    public string UnknownCurrent(string id) => Get(AppearanceGalleryStringId.UnknownCurrent, id);
    public string CurrentWithDetail(string caption, string detail) => Get(AppearanceGalleryStringId.CurrentWithDetail, caption, detail);

    private string Get(AppearanceGalleryStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    private static AppearanceGalleryTexts LoadEnglish()
    {
        using var stream = typeof(AppearanceGalleryTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The appearance gallery string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<AppearanceGalleryStringId, string>>(stream)
            ?? throw new InvalidOperationException("The appearance gallery string catalog is null.");
        return new(values);
    }
}
