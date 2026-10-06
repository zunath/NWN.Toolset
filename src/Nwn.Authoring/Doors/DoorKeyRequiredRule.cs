namespace Nwn.Authoring.Doors;

/// <summary>How applying a door behavior derives the native KeyRequired flag from KeyName.</summary>
public enum DoorKeyRequiredRule
{
    /// <summary>The behavior's managed values alone decide KeyRequired.</summary>
    Unchanged,

    /// <summary>KeyRequired follows whether a key tag is set.</summary>
    FromKeyTag,

    /// <summary>KeyRequired follows the key tag while the door is locked, and is cleared otherwise.</summary>
    FromKeyTagWhenLocked,
}
