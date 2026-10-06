using Nwn.Formats.Resources;

namespace Nwn.Formats.Key;

/// <summary>
/// One resource a KEY's key table names. <see cref="KeyResourceEntry.ResRef"/> is lowercased exactly as the engine
/// itself does, but otherwise kept RAW (unlike <see cref="Nwn.Formats.Resources.Resref"/>, which enforces
/// this pipeline's own, stricter authoring rules for resources WE produce) -- decades of base-game
/// content include resrefs this pipeline would never accept from itself (embedded spaces, stray
/// punctuation), and a stock resref can never collide with one of ours anyway, since ours always
/// satisfies <see cref="Nwn.Formats.Resources.Resref"/>'s rules. <see cref="RawTypeCode"/> is the resource
/// type exactly as the KEY stores it (the full Aurora resource-type space is far larger than
/// <see cref="ResourceType"/>, which only lists the types this pipeline ever builds); <see cref="Type"/>
/// is that code resolved to a known <see cref="ResourceType"/> when possible, or
/// <see langword="null"/> for a stock type this pipeline has no reason to recognize. Locating the
/// resource's bytes needs both <see cref="BifIndex"/> (an index into the KEY's own
/// <see cref="KeyFile.Bifs"/>) and <see cref="IndexInBif"/> (a position in that BIF's own variable
/// resource table) -- see the BioWare Aurora KEY/BIF format's ResID encoding: the high 12 bits select
/// the BIF, the low 20 bits select the resource within it.
/// </summary>
public sealed record KeyResourceEntry(string ResRef, ResourceType? Type, ushort RawTypeCode, int BifIndex, int IndexInBif);
