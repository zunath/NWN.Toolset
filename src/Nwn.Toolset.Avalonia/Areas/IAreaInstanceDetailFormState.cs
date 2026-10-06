using System.ComponentModel;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Connects the shared placed-object form to each host's document transactions.</summary>
public interface IAreaInstanceDetailFormState : INotifyPropertyChanged
{
    string DetailTag { get; set; }
    double DetailX { get; set; }
    double DetailY { get; set; }
    double DetailZ { get; set; }
    double DetailXOrientation { get; set; }
    double DetailYOrientation { get; set; }
    double DetailTriggerWidth { get; set; }
    double DetailTriggerHeight { get; set; }
    bool UsesGenericDetailEditor { get; }
    bool HasTriggerGeometry { get; }
    AreaInstanceDetailLabels InstanceLabels { get; }
}
