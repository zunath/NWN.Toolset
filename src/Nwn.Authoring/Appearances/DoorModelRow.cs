// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>A doortypes.2da or genericdoors.2da row.</summary>
/// <param name="DisplayName">The row's player-facing label.</param>
/// <param name="Model">The door's model resref.</param>
/// <param name="VisibleModel">False when the row declares <c>VisibleModel=0</c>, marking an area-transition plane.</param>
public sealed record DoorModelRow(string DisplayName, string? Model, bool VisibleModel);
