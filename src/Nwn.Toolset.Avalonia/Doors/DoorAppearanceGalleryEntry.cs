using Nwn.Authoring.Doors;
using Nwn.Toolset.Avalonia.Appearances;

namespace Nwn.Toolset.Avalonia.Doors;

/// <summary>One door appearance and the gallery option id its tile carries.</summary>
/// <param name="Choice">The door appearance.</param>
/// <param name="OptionId">
/// The id the gallery gives <paramref name="Choice"/>'s tile, and later passes back to a preview provider.
/// </param>
public sealed record DoorAppearanceGalleryEntry(DoorAppearanceChoice Choice, AppearanceGalleryOptionId OptionId);
