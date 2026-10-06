namespace Nwn.Formats.Key;

/// <summary>One BIF file named by a KEY's file table -- <see cref="Filename"/> is the path the engine
/// stores it under relative to the game's data root (backslashes normalized to forward slashes, e.g.
/// <c>"data/nwn_base.bif"</c>), never an absolute path on this machine.</summary>
public sealed record KeyBifEntry(string Filename, long FileSize);
