namespace Nwn.Authoring.Resources;

/// <summary>How duplicate identities inside one explicitly configured layer are resolved.</summary>
public enum ResourceDuplicatePolicy
{
    Reject,
    FirstWins,
    LastWins
}
