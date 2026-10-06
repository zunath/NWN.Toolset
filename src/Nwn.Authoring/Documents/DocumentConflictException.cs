namespace Nwn.Authoring.Documents;

/// <summary>Raised when the file changed outside the session since it was opened or last saved.</summary>
public sealed class DocumentConflictException(string path)
    : IOException($"Document '{path}' changed outside this session; the external file was left untouched.")
{
    public string Path { get; } = path;
}
