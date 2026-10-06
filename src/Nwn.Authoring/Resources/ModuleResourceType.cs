namespace Nwn.Authoring.Resources;

/// <summary>Native module authoring resource kinds used by area and blueprint editors.</summary>
public enum ModuleResourceType
    {
        Area,
        Utc,
        Uti,
        Utp,
        Utd,
        Utm,
        Utt,
        Uts,
        Utw,

        /// <summary>A dialog (.dlg). Stored like the blueprints - nwn_gff JSON under Module/dlg.</summary>
        Dlg,

        /// <summary>
        /// A NWScript source file (.nss). The one type that is NOT nwn_gff JSON: these are plain text,
        /// so they live at Module/nss/&lt;resref&gt;.nss with no second extension.
        /// </summary>
        Nss
    }
