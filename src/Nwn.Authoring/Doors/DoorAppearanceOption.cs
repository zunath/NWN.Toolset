namespace Nwn.Authoring.Doors;

/// <summary>A selectable native door row with metadata for host display and preview policy.</summary>
public sealed record DoorAppearanceOption(
    DoorAppearanceKind Kind,
    long Id,
    string InternalLabel,
    string Model,
    bool VisibleModel,
    int? StringRef);
