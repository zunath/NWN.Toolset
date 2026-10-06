// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>The geometry, surface and wearer-part visibility selected by one cloakmodel.2da appearance.</summary>
public readonly record struct CloakModelRow(
    int Model,
    int Texture,
    bool HideLeftShoulder,
    bool HideRightShoulder);
