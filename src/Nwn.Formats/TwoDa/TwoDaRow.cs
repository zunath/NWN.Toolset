namespace Nwn.Formats.TwoDa;

/// <summary>One data row: <see cref="Label"/> is the leading row-number column (stored as text
/// since the format allows arbitrary text there, though by convention it is the row's own index),
/// and <see cref="Values"/> has exactly one entry per <see cref="TwoDaTable.Columns"/>, in the same
/// order; <see langword="null"/> means the empty <c>****</c> cell.</summary>
public sealed record TwoDaRow(string Label, IReadOnlyList<string?> Values);
