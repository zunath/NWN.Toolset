namespace Nwn.Authoring.Areas.Editing;

/// <summary>Immutable encoded bytes for all three files that make up one area document.</summary>
public sealed class AreaDocumentSaveSnapshot
{
    private readonly byte[] _areaBytes;
    private readonly byte[] _instanceBytes;
    private readonly byte[] _commentBytes;

    public byte[] AreaBytes => _areaBytes.ToArray();
    public byte[] InstanceBytes => _instanceBytes.ToArray();
    public byte[] CommentBytes => _commentBytes.ToArray();

    internal AreaDocumentSaveSnapshot(byte[] areaBytes, byte[] instanceBytes, byte[] commentBytes)
    {
        _areaBytes = areaBytes.ToArray();
        _instanceBytes = instanceBytes.ToArray();
        _commentBytes = commentBytes.ToArray();
    }

    internal bool MatchesArea(byte[] bytes) => _areaBytes.AsSpan().SequenceEqual(bytes);
    internal bool MatchesInstances(byte[] bytes) => _instanceBytes.AsSpan().SequenceEqual(bytes);
    internal bool MatchesComments(byte[] bytes) => _commentBytes.AsSpan().SequenceEqual(bytes);
}
