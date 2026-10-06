namespace Nwn.Formats.Resources;

/// <summary>Extension &lt;-&gt; <see cref="ResourceType"/> lookups, and the ERF container "file
/// type" tags (HAK/MOD/ERF), which are a different, string-keyed namespace from resource types.</summary>
public static class ResourceTypes
{
    private static readonly IReadOnlyDictionary<string, ResourceType> ByExtension = new Dictionary<string, ResourceType>(StringComparer.Ordinal)
    {
        ["res"] = ResourceType.Res,
        ["bmp"] = ResourceType.Bmp,
        ["mve"] = ResourceType.Mve,
        ["tga"] = ResourceType.Tga,
        ["wav"] = ResourceType.Wav,
        ["wfx"] = ResourceType.Wfx,
        ["plt"] = ResourceType.Plt,
        ["ini"] = ResourceType.Ini,
        ["bmu"] = ResourceType.Bmu,
        ["mp3"] = ResourceType.Mp3,
        ["mpg"] = ResourceType.Mpg,
        ["txt"] = ResourceType.Txt,
        ["plh"] = ResourceType.Plh,
        ["tex"] = ResourceType.Tex,
        ["mdl"] = ResourceType.Mdl,
        ["thg"] = ResourceType.Thg,
        ["fnt"] = ResourceType.Fnt,
        ["lua"] = ResourceType.Lua,
        ["slt"] = ResourceType.Slt,
        ["mod"] = ResourceType.Mod,
        ["nss"] = ResourceType.Nss,
        ["ncs"] = ResourceType.Ncs,
        ["are"] = ResourceType.Are,
        ["set"] = ResourceType.Set,
        ["ifo"] = ResourceType.Ifo,
        ["bic"] = ResourceType.Bic,
        ["wok"] = ResourceType.Wok,
        ["2da"] = ResourceType.TwoDa,
        ["tlk"] = ResourceType.Tlk,
        ["txi"] = ResourceType.Txi,
        ["git"] = ResourceType.Git,
        ["bti"] = ResourceType.Bti,
        ["uti"] = ResourceType.Uti,
        ["btc"] = ResourceType.Btc,
        ["utc"] = ResourceType.Utc,
        ["dlg"] = ResourceType.Dlg,
        ["itp"] = ResourceType.Itp,
        ["btt"] = ResourceType.Btt,
        ["utt"] = ResourceType.Utt,
        ["dds"] = ResourceType.Dds,
        ["gff"] = ResourceType.Gff,
        ["bts"] = ResourceType.Bts,
        ["uts"] = ResourceType.Uts,
        ["ltr"] = ResourceType.Ltr,
        ["fac"] = ResourceType.Fac,
        ["bte"] = ResourceType.Bte,
        ["ute"] = ResourceType.Ute,
        ["btd"] = ResourceType.Btd,
        ["utd"] = ResourceType.Utd,
        ["btp"] = ResourceType.Btp,
        ["utp"] = ResourceType.Utp,
        ["dft"] = ResourceType.Dft,
        ["gic"] = ResourceType.Gic,
        ["gui"] = ResourceType.Gui,
        ["css"] = ResourceType.Css,
        ["ccs"] = ResourceType.Ccs,
        ["btm"] = ResourceType.Btm,
        ["utm"] = ResourceType.Utm,
        ["dwk"] = ResourceType.Dwk,
        ["pwk"] = ResourceType.Pwk,
        ["btg"] = ResourceType.Btg,
        ["utg"] = ResourceType.Utg,
        ["jrl"] = ResourceType.Jrl,
        ["utw"] = ResourceType.Utw,
        ["ssf"] = ResourceType.Ssf,
        ["sav"] = ResourceType.Sav,
        ["4pc"] = ResourceType.FourPc,
        ["hak"] = ResourceType.Hak,
        ["nwm"] = ResourceType.Nwm,
        ["bik"] = ResourceType.Bik,
        ["ndb"] = ResourceType.Ndb,
        ["ptm"] = ResourceType.Ptm,
        ["ptt"] = ResourceType.Ptt,
        ["bak"] = ResourceType.Bak,
        ["shd"] = ResourceType.Shd,
        ["dat"] = ResourceType.Dat,
        ["xbc"] = ResourceType.Xbc,
        ["wbm"] = ResourceType.Wbm,
        ["mtr"] = ResourceType.Mtr,
        ["ktx"] = ResourceType.Ktx,
        ["ttf"] = ResourceType.Ttf,
        ["sql"] = ResourceType.Sql,
        ["tml"] = ResourceType.Tml,
        ["sq3"] = ResourceType.Sq3,
        ["lod"] = ResourceType.Lod,
        ["gif"] = ResourceType.Gif,
        ["png"] = ResourceType.Png,
        ["jpg"] = ResourceType.Jpg,
        ["caf"] = ResourceType.Caf,
        ["ids"] = ResourceType.Ids,
        ["erf"] = ResourceType.Erf,
        ["bif"] = ResourceType.Bif,
        ["key"] = ResourceType.Key,
    };

    private static readonly IReadOnlyDictionary<ResourceType, string> ByCode =
        ByExtension.GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.First().Key);

    public static bool TryGetByExtension(string extension, out ResourceType type) =>
        ByExtension.TryGetValue(extension.ToLowerInvariant(), out type);

    public static ResourceType GetByExtension(string extension) =>
        TryGetByExtension(extension, out var type)
            ? type
            : throw new FormatException($"Unsupported resource extension '.{extension}'.");

    public static string GetExtension(ResourceType type) =>
        ByCode.TryGetValue(type, out var extension)
            ? extension
            : throw new FormatException($"Unsupported resource type code {(int)type}.");

    public static bool TryGetExtension(ResourceType type, out string extension) => ByCode.TryGetValue(type, out extension!);

    /// <summary>Resolves a raw KEY/BIF resource type code to one of the types this pipeline
    /// recognizes. The Aurora engine's resource-type space is larger than this content-facing
    /// enum, and container-file codes (ERF/BIF/KEY) are not resource entries; callers treat
    /// <see langword="false"/> as "not a resource type this layer indexes", not as an error.</summary>
    public static bool TryGetByCode(ushort code, out ResourceType type)
    {
        type = (ResourceType)code;
        if (type is ResourceType.Erf or ResourceType.Bif or ResourceType.Key)
            return false;
        return ByCode.ContainsKey(type);
    }
}
