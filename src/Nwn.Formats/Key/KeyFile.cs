using Nwn.Formats.Resources;

namespace Nwn.Formats.Key;

/// <summary>
/// A parsed KEY V1 file: the master resource index of a base-game (or expansion) data set. A KEY
/// names every resource the engine can load and which BIF holds it, but never resource bytes
/// themselves -- those live in the BIF named by <see cref="KeyResourceEntry.BifIndex"/> (see
/// <see cref="BifFile"/>).
/// </summary>
public sealed class KeyFile
{
    public required IReadOnlyList<KeyBifEntry> Bifs { get; init; }
    public required IReadOnlyList<KeyResourceEntry> Resources { get; init; }

    public KeyResourceEntry? Find(Resref resRef, ResourceType type) =>
        Resources.FirstOrDefault(r => string.Equals(r.ResRef, resRef.Value, StringComparison.Ordinal) && r.Type == type);
}
