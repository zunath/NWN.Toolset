using Nwn.Authoring.Documents.NimGff;

namespace Nwn.Toolset.Avalonia.Sounds;

/// <summary>One sound ResRef in its native ordered list.</summary>
public sealed class SoundListEntryViewModel
{
    public int Index { get; }
    public string ResRef { get; }
    internal JsonGffStruct? NativeEntry { get; }

    public SoundListEntryViewModel(int index, string resRef, JsonGffStruct? nativeEntry)
    {
        Index = index;
        ResRef = resRef;
        NativeEntry = nativeEntry;
    }
}
