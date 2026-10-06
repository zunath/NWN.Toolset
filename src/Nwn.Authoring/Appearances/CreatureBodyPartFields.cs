// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>The native body-part mapping shared by creature and armor previews.</summary>
/// <remarks>A creature stores its right foot in ArmorPart_RFoot; BodyPart_RFoot is not a native field.</remarks>
public static class CreatureBodyPartFields
{
    public static IReadOnlyList<CreatureBodyPartField> All { get; } = Array.AsReadOnly<CreatureBodyPartField>(
    [
        new("BodyPart_Neck", "Neck", "neck"),
        new("BodyPart_Torso", "Torso", "chest"),
        new("BodyPart_Belt", "Belt", "belt"),
        new("BodyPart_Pelvis", "Pelvis", "pelvis"),
        new("BodyPart_LShoul", "LShoul", "shol"),
        new("BodyPart_RShoul", "RShoul", "shor"),
        new("BodyPart_LBicep", "LBicep", "bicepl"),
        new("BodyPart_RBicep", "RBicep", "bicepr"),
        new("BodyPart_LFArm", "LFArm", "forel"),
        new("BodyPart_RFArm", "RFArm", "forer"),
        new("BodyPart_LHand", "LHand", "handl"),
        new("BodyPart_RHand", "RHand", "handr"),
        new("BodyPart_LThigh", "LThigh", "legl"),
        new("BodyPart_RThigh", "RThigh", "legr"),
        new("BodyPart_LShin", "LShin", "shinl"),
        new("BodyPart_RShin", "RShin", "shinr"),
        new("BodyPart_LFoot", "LFoot", "footl"),
        new("ArmorPart_RFoot", "RFoot", "footr"),
    ]);
}
