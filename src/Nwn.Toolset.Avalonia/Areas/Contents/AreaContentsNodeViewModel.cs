using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Areas.Contents;

public sealed class AreaContentsNodeViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;

    public AreaContentsNodeViewModel(
        AreaContentsNodeKind kind,
        ModuleResourceType resourceType,
        string name,
        int depth,
        string openPropertiesLabel,
        string openFirstPropertiesLabel)
    {
        Kind = kind;
        ResourceType = resourceType;
        Name = name;
        Depth = depth;
        OpenPropertiesText = openPropertiesLabel;
        OpenFirstPropertiesText = openFirstPropertiesLabel;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public AreaContentsNodeKind Kind { get; }
    public ModuleResourceType ResourceType { get; }
    public string Name { get; }
    public string Detail { get; init; } = string.Empty;
    public int Depth { get; }
    public IReadOnlyList<int> Indices { get; init; } = Array.Empty<int>();
    public IReadOnlyList<AreaContentsIdentity> Identities { get; init; } = Array.Empty<AreaContentsIdentity>();
    public System.Numerics.Vector3? Position { get; init; }
    public ObservableCollection<AreaContentsNodeViewModel> Children { get; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Twisty));
        }
    }

    public bool IsBranch => Kind is AreaContentsNodeKind.Kind or AreaContentsNodeKind.Group;
    public bool CanOpenProperties => (Kind is AreaContentsNodeKind.Instance or AreaContentsNodeKind.Group) && Identities.Count > 0;
    public string OpenPropertiesText { get; }
    public string OpenFirstPropertiesText { get; }
    public string OpenPropertiesLabel => Kind == AreaContentsNodeKind.Group ? OpenFirstPropertiesText : OpenPropertiesText;
    public bool IsDeletable => Identities.Count > 0;
    public bool IsEmptyKind => Kind == AreaContentsNodeKind.Kind && Identities.Count == 0 && Children.Count == 0;
    public bool IsDimmed => IsEmptyKind || Kind == AreaContentsNodeKind.Overflow;
    public string Twisty => Children.Count == 0 ? string.Empty : IsExpanded ? "▾" : "▸";
    public global::Avalonia.Thickness Indent => new(6 + Depth * 16, 0, 0, 0);
    public global::Avalonia.Media.FontWeight Weight => Kind == AreaContentsNodeKind.Kind ? global::Avalonia.Media.FontWeight.SemiBold : global::Avalonia.Media.FontWeight.Normal;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

