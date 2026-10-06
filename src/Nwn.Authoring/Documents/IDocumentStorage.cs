namespace Nwn.Authoring.Documents;

/// <summary>File operations used by document sessions, with atomic replacement and an expected
/// on-disk hash to prevent overwriting external edits.</summary>
public interface IDocumentStorage
{
    byte[]? ReadIfExists(string path);
    void WriteAtomically(string path, ReadOnlyMemory<byte> bytes, string? expectedCurrentHash);
}
