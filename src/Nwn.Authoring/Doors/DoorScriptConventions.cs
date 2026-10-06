namespace Nwn.Authoring.Doors;

/// <summary>
/// A module's script and local-variable conventions for doors: the scripts that close a door
/// after it opens, the stock death script, and the local prefix of an ordered key-item list.
/// </summary>
/// <param name="DefaultCloser">Written to OnOpen when "closes itself" is turned on; null disables the switch.</param>
/// <param name="KnownClosers">Every OnOpen script recognised as closing the door again.</param>
/// <param name="DefaultDeathScript">The stock OnDeath script a behavior switch keeps.</param>
/// <param name="RequiredKeyItemPrefix">Local name prefix of the ordered required-key-item list, or null.</param>
public sealed record DoorScriptConventions(
    string? DefaultCloser,
    IReadOnlyCollection<string> KnownClosers,
    string? DefaultDeathScript,
    string? RequiredKeyItemPrefix)
{
    /// <summary>No module conventions: no closer scripts, death script or key-item locals.</summary>
    public static DoorScriptConventions None { get; } = new(null, Array.Empty<string>(), null, null);

    /// <summary>True when <paramref name="script"/> is one of <see cref="KnownClosers"/>.</summary>
    public bool IsKnownCloser(string? script) =>
        !string.IsNullOrWhiteSpace(script) &&
        KnownClosers.Any(closer => string.Equals(closer, script, StringComparison.OrdinalIgnoreCase));
}
