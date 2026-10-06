// SPDX-License-Identifier: MIT
namespace Nwn.Authoring.Appearances;

/// <summary>The appearance.2da columns that choose a creature's model.</summary>
/// <param name="Id">The appearance row id.</param>
/// <param name="DisplayName">The row's player-facing label, used in status text.</param>
/// <param name="ModelType">MODELTYPE; <c>P</c> marks a segmented player-body creature.</param>
/// <param name="Race">RACE: the race letter of a segmented body, or the model resref of a simple one.</param>
public sealed record CreatureAppearanceModelRow(int Id, string DisplayName, string? ModelType, string? Race);
