// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>The 2DA families a host supplies to <see cref="IModelResolutionHost"/>.</summary>
public enum ModelResolutionTable
{
    /// <summary>appearance.2da.</summary>
    CreatureAppearances,

    /// <summary>placeables.2da.</summary>
    Placeables,

    /// <summary>doortypes.2da and genericdoors.2da.</summary>
    Doors,

    /// <summary>waypoints.2da.</summary>
    Waypoints,

    /// <summary>baseitems.2da.</summary>
    BaseItems,
}
