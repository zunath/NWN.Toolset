using Nwn.Authoring.Sounds;
using Nwn.Toolset.Avalonia.Behaviors;

namespace Nwn.Toolset.Avalonia.Sounds;

/// <summary>The game data and services an ambient-sound behavior editor takes from its host.</summary>
public sealed class SoundBehaviorEditorHost : BehaviorEditorHost
{
    /// <summary>The host's sound behaviors and Basic rows.</summary>
    public required ISoundBehaviorCatalog Catalog { get; init; }

    /// <summary>Every playable audio resref the playlist can name.</summary>
    public IReadOnlyList<string> AudioResources { get; init; } = Array.Empty<string>();

    /// <summary>Plays a playlist entry; no preview button when null.</summary>
    public ISoundListPreview? Preview { get; init; }
}
