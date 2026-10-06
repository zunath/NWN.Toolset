namespace Nwn.Formats.Resources;

/// <summary>The four-character "file type" tag in an ERF container's header. Distinct from
/// <see cref="ResourceType"/>: this identifies the CONTAINER (hak/mod/erf), not a resource inside
/// it.</summary>
public enum ErfContainerKind
{
    Erf,
    Hak,
    Mod,
}
