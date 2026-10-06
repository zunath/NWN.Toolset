namespace Nwn.Toolset.Avalonia.Sounds;

/// <summary>Host-owned playback policy used by the shared sound-list control.</summary>
public interface ISoundListPreview
{
    bool IsAvailable { get; }
    string? Play(string? resRef);
    void Stop();
}
