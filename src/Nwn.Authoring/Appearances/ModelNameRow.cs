// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>A placeables.2da or waypoints.2da row that names a single model.</summary>
public sealed record ModelNameRow(string DisplayName, string? ModelName);
