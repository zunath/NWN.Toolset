namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>One selectable option of a 2DA-backed dropdown.</summary>
public sealed record LookupOption(long Id, string Display, bool ShowId = true)
{
    /// <summary>
    /// Behavior editors put an optional id after the readable name; generic dropdowns put it
    /// before. Both honor the same presentation rule so a lookup cannot drift between editors.
    /// </summary>
    public string BehaviorDisplay => ShowId ? $"{Display} ({Id})" : Display;

    public override string ToString() => ShowId ? $"{Id}: {Display}" : Display;
}
