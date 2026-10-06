// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>The baseitems.2da columns that choose an item's model.</summary>
/// <param name="ItemClass">ItemClass, the model-name prefix.</param>
/// <param name="ModelType">ModelType: 0/1 simple, 2 composite, 3 armor.</param>
public sealed record BaseItemModelRow(string? ItemClass, int ModelType);
