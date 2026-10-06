using Nwn.Authoring.Behaviors;
using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Authoring.Areas.Properties;

public static class AreaPropertyCatalog
{
    public static IReadOnlyList<AreaPropertyGroup> Groups { get; } =
    [
        new(AreaPropertyGroupId.Identity,
        [
            new(AreaPropertyFieldId.Name, nameof(AreaPropertyFieldId.Name), BehaviorFieldKind.LocalizedText, GffFieldType.CExoLocString, MaxLength: 512),
            new(AreaPropertyFieldId.Tag, nameof(AreaPropertyFieldId.Tag), BehaviorFieldKind.Text, GffFieldType.CExoString),
            new(AreaPropertyFieldId.ResRef, nameof(AreaPropertyFieldId.ResRef), BehaviorFieldKind.Text, GffFieldType.ResRef, IsReadOnly: true),
            new(AreaPropertyFieldId.Tileset, nameof(AreaPropertyFieldId.Tileset), BehaviorFieldKind.Text, GffFieldType.ResRef, IsReadOnly: true),
            new(AreaPropertyFieldId.Width, nameof(AreaPropertyFieldId.Width), BehaviorFieldKind.Integer, GffFieldType.Int, IsReadOnly: true),
            new(AreaPropertyFieldId.Height, nameof(AreaPropertyFieldId.Height), BehaviorFieldKind.Integer, GffFieldType.Int, IsReadOnly: true),
            new(AreaPropertyFieldId.Comments, nameof(AreaPropertyFieldId.Comments), BehaviorFieldKind.Text, GffFieldType.CExoString),
        ]),
        new(AreaPropertyGroupId.Flags,
        [
            new(AreaPropertyFieldId.Flags, nameof(AreaPropertyFieldId.Flags), BehaviorFieldKind.Integer, GffFieldType.Dword),
            new(AreaPropertyFieldId.NoRest, nameof(AreaPropertyFieldId.NoRest), BehaviorFieldKind.Check, GffFieldType.Byte),
            new(AreaPropertyFieldId.PlayerVsPlayer, nameof(AreaPropertyFieldId.PlayerVsPlayer), BehaviorFieldKind.Integer, GffFieldType.Byte),
        ]),
        new(AreaPropertyGroupId.Lighting,
        [
            new(AreaPropertyFieldId.LightingScheme, nameof(AreaPropertyFieldId.LightingScheme), BehaviorFieldKind.Integer, GffFieldType.Byte),
            new(AreaPropertyFieldId.SkyBox, nameof(AreaPropertyFieldId.SkyBox), BehaviorFieldKind.Integer, GffFieldType.Byte),
            new(AreaPropertyFieldId.DayNightCycle, nameof(AreaPropertyFieldId.DayNightCycle), BehaviorFieldKind.Check, GffFieldType.Byte),
            new(AreaPropertyFieldId.IsNight, nameof(AreaPropertyFieldId.IsNight), BehaviorFieldKind.Check, GffFieldType.Byte),
            new(AreaPropertyFieldId.SunAmbientColor, nameof(AreaPropertyFieldId.SunAmbientColor), BehaviorFieldKind.Integer, GffFieldType.Dword),
            new(AreaPropertyFieldId.SunDiffuseColor, nameof(AreaPropertyFieldId.SunDiffuseColor), BehaviorFieldKind.Integer, GffFieldType.Dword),
            new(AreaPropertyFieldId.SunShadows, nameof(AreaPropertyFieldId.SunShadows), BehaviorFieldKind.Check, GffFieldType.Byte),
            new(AreaPropertyFieldId.SunFogAmount, nameof(AreaPropertyFieldId.SunFogAmount), BehaviorFieldKind.Integer, GffFieldType.Byte),
            new(AreaPropertyFieldId.MoonAmbientColor, nameof(AreaPropertyFieldId.MoonAmbientColor), BehaviorFieldKind.Integer, GffFieldType.Dword),
            new(AreaPropertyFieldId.MoonDiffuseColor, nameof(AreaPropertyFieldId.MoonDiffuseColor), BehaviorFieldKind.Integer, GffFieldType.Dword),
            new(AreaPropertyFieldId.MoonShadows, nameof(AreaPropertyFieldId.MoonShadows), BehaviorFieldKind.Check, GffFieldType.Byte),
            new(AreaPropertyFieldId.MoonFogAmount, nameof(AreaPropertyFieldId.MoonFogAmount), BehaviorFieldKind.Integer, GffFieldType.Byte),
            new(AreaPropertyFieldId.FogClipDist, nameof(AreaPropertyFieldId.FogClipDist), BehaviorFieldKind.Float, GffFieldType.Float),
        ]),
        new(AreaPropertyGroupId.Weather,
        [
            new(AreaPropertyFieldId.ChanceRain, nameof(AreaPropertyFieldId.ChanceRain), BehaviorFieldKind.Integer, GffFieldType.Int),
            new(AreaPropertyFieldId.ChanceSnow, nameof(AreaPropertyFieldId.ChanceSnow), BehaviorFieldKind.Integer, GffFieldType.Int),
            new(AreaPropertyFieldId.ChanceLightning, nameof(AreaPropertyFieldId.ChanceLightning), BehaviorFieldKind.Integer, GffFieldType.Int),
            new(AreaPropertyFieldId.WindPower, nameof(AreaPropertyFieldId.WindPower), BehaviorFieldKind.Integer, GffFieldType.Int),
        ]),
        new(AreaPropertyGroupId.Loading,
        [
            new(AreaPropertyFieldId.LoadScreenID, nameof(AreaPropertyFieldId.LoadScreenID), BehaviorFieldKind.Integer, GffFieldType.Word),
        ]),
    ];
}
