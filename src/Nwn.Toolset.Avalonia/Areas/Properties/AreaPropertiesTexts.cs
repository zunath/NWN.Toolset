using System.Globalization;
using System.Text.Json;
using Nwn.Authoring.Areas.Properties;

namespace Nwn.Toolset.Avalonia.Areas.Properties;

/// <summary>Complete host-replaceable text for the shared area Properties page.</summary>
public sealed class AreaPropertiesTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.AreaPropertiesEnglish.json";
    private readonly IReadOnlyDictionary<AreaPropertiesStringId, string> _values;

    public static AreaPropertiesTexts English { get; } = LoadEnglish();

    public AreaPropertiesTexts(IReadOnlyDictionary<AreaPropertiesStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<AreaPropertiesStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The area Properties catalog does not match its typed IDs.", nameof(values));

        _values = new Dictionary<AreaPropertiesStringId, string>(values);
    }

    public string Get(AreaPropertiesStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    /// <summary>The caption of one native area property group.</summary>
    public string GroupTitle(AreaPropertyGroupId id) => Get(id switch
    {
        AreaPropertyGroupId.Identity => AreaPropertiesStringId.GroupIdentity,
        AreaPropertyGroupId.Flags => AreaPropertiesStringId.GroupFlags,
        AreaPropertyGroupId.Lighting => AreaPropertiesStringId.GroupLighting,
        AreaPropertyGroupId.Weather => AreaPropertiesStringId.GroupWeather,
        AreaPropertyGroupId.Loading => AreaPropertiesStringId.GroupLoading,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
    });

    /// <summary>The label of one native area property.</summary>
    public string FieldLabel(AreaPropertyFieldId id) => Get(id switch
    {
        AreaPropertyFieldId.Name => AreaPropertiesStringId.FieldName,
        AreaPropertyFieldId.Tag => AreaPropertiesStringId.FieldTag,
        AreaPropertyFieldId.ResRef => AreaPropertiesStringId.FieldResRef,
        AreaPropertyFieldId.Tileset => AreaPropertiesStringId.FieldTileset,
        AreaPropertyFieldId.Width => AreaPropertiesStringId.FieldWidth,
        AreaPropertyFieldId.Height => AreaPropertiesStringId.FieldHeight,
        AreaPropertyFieldId.Comments => AreaPropertiesStringId.FieldComments,
        AreaPropertyFieldId.Flags => AreaPropertiesStringId.FieldFlags,
        AreaPropertyFieldId.NoRest => AreaPropertiesStringId.FieldNoRest,
        AreaPropertyFieldId.PlayerVsPlayer => AreaPropertiesStringId.FieldPlayerVsPlayer,
        AreaPropertyFieldId.LightingScheme => AreaPropertiesStringId.FieldLightingScheme,
        AreaPropertyFieldId.SkyBox => AreaPropertiesStringId.FieldSkyBox,
        AreaPropertyFieldId.DayNightCycle => AreaPropertiesStringId.FieldDayNightCycle,
        AreaPropertyFieldId.IsNight => AreaPropertiesStringId.FieldIsNight,
        AreaPropertyFieldId.SunAmbientColor => AreaPropertiesStringId.FieldSunAmbientColor,
        AreaPropertyFieldId.SunDiffuseColor => AreaPropertiesStringId.FieldSunDiffuseColor,
        AreaPropertyFieldId.SunShadows => AreaPropertiesStringId.FieldSunShadows,
        AreaPropertyFieldId.SunFogAmount => AreaPropertiesStringId.FieldSunFogAmount,
        AreaPropertyFieldId.MoonAmbientColor => AreaPropertiesStringId.FieldMoonAmbientColor,
        AreaPropertyFieldId.MoonDiffuseColor => AreaPropertiesStringId.FieldMoonDiffuseColor,
        AreaPropertyFieldId.MoonShadows => AreaPropertiesStringId.FieldMoonShadows,
        AreaPropertyFieldId.MoonFogAmount => AreaPropertiesStringId.FieldMoonFogAmount,
        AreaPropertyFieldId.FogClipDist => AreaPropertiesStringId.FieldFogClipDist,
        AreaPropertyFieldId.ChanceRain => AreaPropertiesStringId.FieldChanceRain,
        AreaPropertyFieldId.ChanceSnow => AreaPropertiesStringId.FieldChanceSnow,
        AreaPropertyFieldId.ChanceLightning => AreaPropertiesStringId.FieldChanceLightning,
        AreaPropertyFieldId.WindPower => AreaPropertiesStringId.FieldWindPower,
        AreaPropertyFieldId.LoadScreenID => AreaPropertiesStringId.FieldLoadScreen,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
    });

    /// <summary>The help line under one native area property, or null.</summary>
    public string? FieldDescription(AreaPropertyFieldId id) => id switch
    {
        AreaPropertyFieldId.ResRef => Get(AreaPropertiesStringId.ResRefDescription),
        AreaPropertyFieldId.Flags => Get(AreaPropertiesStringId.FlagsDescription),
        _ => null,
    };

    private static AreaPropertiesTexts LoadEnglish()
    {
        using var stream = typeof(AreaPropertiesTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The area Properties string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<AreaPropertiesStringId, string>>(stream)
            ?? throw new InvalidOperationException("The area Properties string catalog is null.");
        return new(values);
    }
}
