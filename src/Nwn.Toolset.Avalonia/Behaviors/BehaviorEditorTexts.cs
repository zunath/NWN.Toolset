using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Behaviors;

/// <summary>Complete host-replaceable text for the shared door, waypoint and sound behavior editors.</summary>
public sealed class BehaviorEditorTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.BehaviorEditorEnglish.json";
    private readonly IReadOnlyDictionary<BehaviorEditorStringId, string> _values;

    public static BehaviorEditorTexts English { get; } = LoadEnglish();

    public BehaviorEditorTexts(IReadOnlyDictionary<BehaviorEditorStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<BehaviorEditorStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The behavior editor catalog does not match its typed IDs.", nameof(values));

        _values = new Dictionary<BehaviorEditorStringId, string>(values);
    }

    public string Unsaved => Get(BehaviorEditorStringId.Unsaved);
    public string TabBasic => Get(BehaviorEditorStringId.TabBasic);
    public string TabAppearance => Get(BehaviorEditorStringId.TabAppearance);
    public string TabBehavior => Get(BehaviorEditorStringId.TabBehavior);
    public string TabVariables => Get(BehaviorEditorStringId.TabVariables);
    public string DoorPreviewHint => Get(BehaviorEditorStringId.DoorPreviewHint);
    public string KeyItemSearchWatermark => Get(BehaviorEditorStringId.KeyItemSearchWatermark);

    public string Get(BehaviorEditorStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    /// <summary>Names discarded slots in prose, capped so a prompt stays readable.</summary>
    public string DescribeLosses(IReadOnlyList<string> losses)
    {
        ArgumentNullException.ThrowIfNull(losses);
        const int shown = 6;
        var named = string.Join(", ", losses.Take(shown));
        return losses.Count <= shown
            ? named
            : Get(BehaviorEditorStringId.DescribeMore, named, losses.Count - shown);
    }

    private static BehaviorEditorTexts LoadEnglish()
    {
        using var stream = typeof(BehaviorEditorTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The behavior editor string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<BehaviorEditorStringId, string>>(stream)
            ?? throw new InvalidOperationException("The behavior editor string catalog is null.");
        return new(values);
    }
}
