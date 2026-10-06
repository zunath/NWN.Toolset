namespace Nwn.Toolset.Avalonia.Explorer;

/// <summary>One resource a Module Contents section lists, as the host enumerated it.</summary>
/// <param name="ResRef">The resource reference.</param>
/// <param name="Name">The display name, or null when the host has none.</param>
/// <param name="Tag">The tag, or null when the host has none.</param>
public sealed record ExplorerItem(string ResRef, string? Name, string? Tag)
{
    /// <summary>
    /// The line a builder reads first. The name when there is one, and only then the resref - which the
    /// row still shows at its end in monospace, so nothing is hidden from anyone who needs to type it.
    /// </summary>
    public string PrimaryText => string.IsNullOrWhiteSpace(Name) ? ResRef : Name;
}
