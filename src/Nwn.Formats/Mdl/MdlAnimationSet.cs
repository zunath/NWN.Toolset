namespace Nwn.Formats.Mdl;

/// <summary>What one model file says about animation: the supermodel it inherits animations from
/// (null for none) and the animations it declares itself.</summary>
public sealed record MdlAnimationSet(string? SuperModel, IReadOnlyList<string> Animations);
