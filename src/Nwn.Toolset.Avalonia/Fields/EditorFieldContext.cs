using Nwn.Authoring.Documents.NimGff;
using Nwn.Formats.Tlk;

namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>
/// Shared context the field view models edit through: runs a mutation inside a one-step
/// transaction on the owning session and notifies the editor afterwards so dirty/undo
/// state refreshes.
/// </summary>
public sealed class EditorFieldContext
{
    private readonly Func<string, Action, bool> _runEdit;
    private readonly Func<uint, bool> _canOpenTlkRow;

    public JsonGffDocument Document { get; }

    /// <summary>True while the editor is loading values into view models; suppresses writes.</summary>
    public bool IsRefreshing { get; set; }

    /// <summary>
    /// Resolves a TLK strref for a localized field that carries one but no language-0 override, or
    /// null when no TLK is loaded - in which case such a field reads blank, as it did before.
    /// </summary>
    public Func<uint, string?>? ResolveStrRef { get; }

    /// <summary>Opens the host's TLK editor at a full StrRef.</summary>
    public Action<uint>? OpenTlkRow { get; }

    /// <summary>The captions and messages the field editors show.</summary>
    public FieldTexts Texts { get; }

    /// <param name="document">The native document the fields edit.</param>
    /// <param name="runEdit">The host transaction every write runs through.</param>
    /// <param name="resolveStrRef">Resolves TLK text for a strref-only localized value.</param>
    /// <param name="openTlkRow">Opens the host's TLK editor at a StrRef.</param>
    /// <param name="canOpenTlkRow">
    /// Which StrRefs the TLK editor can open. Defaults to the custom-TLK range, starting at
    /// <see cref="TlkTable.CustomStrRefBase"/>.
    /// </param>
    /// <param name="texts">Field captions and messages; English when omitted.</param>
    public EditorFieldContext(
        JsonGffDocument document,
        Func<string, Action, bool> runEdit,
        Func<uint, string?>? resolveStrRef = null,
        Action<uint>? openTlkRow = null,
        Func<uint, bool>? canOpenTlkRow = null,
        FieldTexts? texts = null)
    {
        Document = document;
        _runEdit = runEdit;
        ResolveStrRef = resolveStrRef;
        OpenTlkRow = openTlkRow;
        _canOpenTlkRow = canOpenTlkRow ?? (strRef => strRef >= TlkTable.CustomStrRefBase);
        Texts = texts ?? FieldTexts.English;
    }

    /// <summary>True when the host can open <paramref name="strRef"/> in its TLK editor.</summary>
    public bool CanOpenTlkRow(uint strRef) => OpenTlkRow != null && _canOpenTlkRow(strRef);

    public bool RunEdit(string description, Action mutation)
    {
        return !IsRefreshing && _runEdit(description, mutation);
    }
}
