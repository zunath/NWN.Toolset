namespace Nwn.Formats.Gff;

/// <summary>One localized substring within a CExoLocString field: <paramref name="StringId"/> is
/// <c>language * 2 + gender</c> (gender 0 = male/neutral, 1 = female), the same encoding the engine
/// itself uses.</summary>
public sealed record GffLocStringEntry(uint StringId, string Text)
{
    public int Language => (int)(StringId / 2);
    public int Gender => (int)(StringId % 2);

    public static GffLocStringEntry FromLanguageGender(int language, int gender, string text) =>
        new((uint)(language * 2 + gender), text);
}
