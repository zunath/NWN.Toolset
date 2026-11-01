using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Areas.Editing;

/// <summary>Shares the module entry point and its saved state between area and module editors.</summary>
public sealed class ModuleEntryEditor
{
    private byte[] _saved;
    private (string? Area, float X, float Y, float Z, float DirectionX, float DirectionY) _savedEntry;
    public DocumentSession Session { get; }
    public IfoDocument Document { get; }
    public bool IsDirty => !_saved.AsSpan().SequenceEqual(Session.ToBytes());
    public bool IsEntryDirty => EntryState() != _savedEntry;
    public event Action? Changed;

    public ModuleEntryEditor(DocumentSession session)
    {
        Session = session;
        Document = new IfoDocument(session.Document);
        _saved = session.ToBytes();
        _savedEntry = EntryState();
    }

    private (string? Area, float X, float Y, float Z, float DirectionX, float DirectionY) EntryState() =>
        (Document.EntryArea, Document.EntryX, Document.EntryY, Document.EntryZ,
            Document.EntryDirectionX, Document.EntryDirectionY);

    public void NotifyChanged() => Changed?.Invoke();
    public void MarkSaved()
    {
        _saved = Session.ToBytes();
        _savedEntry = EntryState();
        NotifyChanged();
    }
}
