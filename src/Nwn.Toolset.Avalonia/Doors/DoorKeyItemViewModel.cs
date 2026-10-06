namespace Nwn.Toolset.Avalonia.Doors;

/// <summary>One selected or selectable required key item.</summary>
public sealed class DoorKeyItemViewModel
{
    public int Id { get; }

    public string Display { get; }

    public bool IsValid { get; }

    public DoorKeyItemViewModel(int id, string display, bool isValid)
    {
        Id = id;
        Display = display;
        IsValid = isValid;
    }

    public override string ToString() => Display;
}
