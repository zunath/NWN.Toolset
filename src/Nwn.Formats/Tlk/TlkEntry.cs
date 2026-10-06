namespace Nwn.Formats.Tlk;

/// <summary>One TLK string entry. <see cref="SoundResRef"/>/<see cref="SoundLength"/> are almost
/// always empty/zero for a custom content tlk but are preserved for fidelity. Flags are derived from
/// which optional fields are present rather than stored separately, since that is exactly what the
/// on-disk flag bits mean.</summary>
public sealed record TlkEntry(string Text, string SoundResRef = "", float SoundLength = 0f)
{
    public static readonly TlkEntry Empty = new(string.Empty);
}
