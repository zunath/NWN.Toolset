using System.Threading;

using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Nwn.Authoring.Areas.Placement;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;

namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Edits the tag and transform of one selected area instance through its host transaction.</summary>
public class AreaInstanceDetailState : ObservableObject, IAreaInstanceDetailFormState
{
    private readonly ModuleResourceType _type;
    private readonly Func<string, Action, bool> _edit;
    private readonly AreaInstanceDetailEditDescriptions _editDescriptions;
    private JsonGffStruct? _currentInstance;
    private bool _loading;
    private string _detailTag = string.Empty;
    private double _detailX;
    private double _detailY;
    private double _detailZ;
    private double _detailXOrientation;
    private double _detailYOrientation;
    private double _detailTriggerWidth;
    private double _detailTriggerHeight;

    /// <summary>Raised after a host transaction accepted and applied a detail edit.</summary>
    public event Action? EditApplied;

    /// <summary>The native placement currently shown by the form.</summary>
    protected JsonGffStruct? CurrentInstance => _currentInstance;

    public AreaInstanceDetailLabels InstanceLabels { get; }

    public virtual bool UsesGenericDetailEditor => true;

    public bool HasTriggerGeometry => _type == ModuleResourceType.Utt;

    public string DetailTag
    {
        get => _detailTag;
        set
        {
            if (_detailTag == value)
                return;

            if (_loading)
            {
                _detailTag = value;
                OnPropertyChanged();
                return;
            }

            if (!TryApply(_editDescriptions.Tag, instance => InstanceFieldMap.SetTag(instance, value)))
            {
                ReloadFromDocument();
                return;
            }

            _detailTag = value;
            OnPropertyChanged();
            EditApplied?.Invoke();
        }
    }

    public double DetailX
    {
        get => _detailX;
        set => SetTransformValue(ref _detailX, value, ApplyPosition);
    }

    public double DetailY
    {
        get => _detailY;
        set => SetTransformValue(ref _detailY, value, ApplyPosition);
    }

    public double DetailZ
    {
        get => _detailZ;
        set => SetTransformValue(ref _detailZ, value, ApplyPosition);
    }

    public double DetailXOrientation
    {
        get => _detailXOrientation;
        set => SetTransformValue(ref _detailXOrientation, value, ApplyOrientation);
    }

    public double DetailYOrientation
    {
        get => _detailYOrientation;
        set => SetTransformValue(ref _detailYOrientation, value, ApplyOrientation);
    }

    public double DetailTriggerWidth
    {
        get => _detailTriggerWidth;
        set => SetTransformValue(ref _detailTriggerWidth, value, ApplyGeometry);
    }

    public double DetailTriggerHeight
    {
        get => _detailTriggerHeight;
        set => SetTransformValue(ref _detailTriggerHeight, value, ApplyGeometry);
    }

    public AreaInstanceDetailState(
        ModuleResourceType type,
        Func<string, Action, bool> edit,
        AreaInstanceDetailLabels instanceLabels,
        AreaInstanceDetailEditDescriptions editDescriptions)
    {
        _type = type;
        _edit = edit ?? throw new ArgumentNullException(nameof(edit));
        InstanceLabels = instanceLabels ?? throw new ArgumentNullException(nameof(instanceLabels));
        _editDescriptions = editDescriptions ?? throw new ArgumentNullException(nameof(editDescriptions));
    }

    /// <summary>Binds the form to a placement, or clears it when the selection is removed.</summary>
    public void SetInstance(JsonGffStruct? instance)
    {
        if (ReferenceEquals(_currentInstance, instance))
        {
            ReloadFromDocument();
            return;
        }

        _currentInstance = null;
        _loading = true;
        try
        {
            if (instance == null)
            {
                SetDetailTag(string.Empty);
                SetDetailX(0);
                SetDetailY(0);
                SetDetailZ(0);
                SetDetailXOrientation(0);
                SetDetailYOrientation(0);
                SetDetailTriggerWidth(0);
                SetDetailTriggerHeight(0);
                return;
            }

            _currentInstance = instance;
            LoadValues(instance);
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Reloads the current selection after undo, redo, or a host refresh.</summary>
    public void ReloadFromDocument()
    {
        var instance = _currentInstance;
        _loading = true;
        try
        {
            if (instance == null)
            {
                SetDetailTag(string.Empty);
                SetDetailX(0);
                SetDetailY(0);
                SetDetailZ(0);
                SetDetailXOrientation(0);
                SetDetailYOrientation(0);
                SetDetailTriggerWidth(0);
                SetDetailTriggerHeight(0);
                return;
            }

            LoadValues(instance);
        }
        finally
        {
            _loading = false;
        }
    }

    private void LoadValues(JsonGffStruct instance)
    {
        SetDetailTag(InstanceFieldMap.GetTag(instance) ?? string.Empty);
        var (x, y, z) = InstanceFieldMap.GetPosition(_type, instance);
        SetDetailX(x);
        SetDetailY(y);
        SetDetailZ(z);
        var (xOrientation, yOrientation) = InstanceFieldMap.GetOrientation(_type, instance);
        SetDetailXOrientation(xOrientation);
        SetDetailYOrientation(yOrientation);
        if (HasTriggerGeometry)
        {
            var (width, height) = InstanceFieldMap.GetTriggerGeometrySize(instance);
            SetDetailTriggerWidth(width);
            SetDetailTriggerHeight(height);
        }
        else
        {
            SetDetailTriggerWidth(0);
            SetDetailTriggerHeight(0);
        }
    }

    private void SetTransformValue(ref double field, double value, Func<bool> apply,
        [CallerMemberName] string? propertyName = null)
    {
        if (field.Equals(value))
            return;

        if (_loading)
        {
            field = value;
            OnPropertyChanged(propertyName);
            return;
        }

        var instance = _currentInstance;
        field = value;
        if (!IsFiniteSingle(value))
        {
            ReloadFromDocument();
            return;
        }

        if (!apply())
        {
            if (!ReferenceEquals(_currentInstance, instance))
            {
                ReloadFromDocument();
                return;
            }

            if (CanKeepTriggerDimensionDraft(instance, value, propertyName))
            {
                OnPropertyChanged(propertyName);
                return;
            }

            ReloadFromDocument();
            return;
        }

        OnPropertyChanged(propertyName);
        EditApplied?.Invoke();
    }

    private bool ApplyPosition()
    {
        if (!IsFiniteSingle(_detailX) || !IsFiniteSingle(_detailY) || !IsFiniteSingle(_detailZ))
            return false;

        var x = (float)_detailX;
        var y = (float)_detailY;
        var z = (float)_detailZ;
        return TryApply(_editDescriptions.Position, instance =>
            InstanceFieldMap.SetPosition(_type, instance, x, y, z));
    }

    private bool ApplyOrientation()
    {
        if (!IsFiniteSingle(_detailXOrientation) || !IsFiniteSingle(_detailYOrientation))
            return false;

        var xOrientation = (float)_detailXOrientation;
        var yOrientation = (float)_detailYOrientation;
        return TryApply(_editDescriptions.Facing, instance =>
            InstanceFieldMap.SetOrientation(_type, instance, xOrientation, yOrientation));
    }

    private bool ApplyGeometry()
    {
        if (!HasTriggerGeometry || _detailTriggerWidth <= 0 || _detailTriggerHeight <= 0 ||
            !IsFiniteSingle(_detailTriggerWidth) || !IsFiniteSingle(_detailTriggerHeight))
        {
            return false;
        }

        var width = (float)_detailTriggerWidth;
        var height = (float)_detailTriggerHeight;
        return TryApply(_editDescriptions.Geometry, instance =>
            InstanceFieldMap.SetTriggerGeometrySize(instance, width, height));
    }

    private bool CanKeepTriggerDimensionDraft(JsonGffStruct? instance, double value, string? propertyName)
    {
        if (!HasTriggerGeometry || instance == null || value <= 0 ||
            propertyName is not (nameof(DetailTriggerWidth) or nameof(DetailTriggerHeight)) ||
            !IsFiniteSingle(_detailTriggerWidth) || !IsFiniteSingle(_detailTriggerHeight) ||
            _detailTriggerWidth < 0 || _detailTriggerHeight < 0 ||
            (_detailTriggerWidth > 0 && _detailTriggerHeight > 0))
        {
            return false;
        }

        var (nativeWidth, nativeHeight) = InstanceFieldMap.GetTriggerGeometrySize(instance);
        return nativeWidth <= 0 || nativeHeight <= 0;
    }

    private bool TryApply(string description, Action<JsonGffStruct> mutation)
    {
        var instance = _currentInstance;
        if (instance == null)
            return false;

        var applied = false;
        var invocationOpen = 1;
        bool accepted;
        try
        {
            accepted = _edit(description, () =>
            {
                if (Volatile.Read(ref invocationOpen) == 0 || !ReferenceEquals(_currentInstance, instance))
                    return;

                mutation(instance);
                applied = true;
            });
        }
        finally
        {
            Interlocked.Exchange(ref invocationOpen, 0);
        }

        return accepted && applied && ReferenceEquals(_currentInstance, instance);
    }

    private static bool IsFiniteSingle(double value) => float.IsFinite((float)value);

    private void SetDetailTag(string value) => SetProperty(ref _detailTag, value, nameof(DetailTag));
    private void SetDetailX(double value) => SetProperty(ref _detailX, value, nameof(DetailX));
    private void SetDetailY(double value) => SetProperty(ref _detailY, value, nameof(DetailY));
    private void SetDetailZ(double value) => SetProperty(ref _detailZ, value, nameof(DetailZ));
    private void SetDetailXOrientation(double value) => SetProperty(ref _detailXOrientation, value, nameof(DetailXOrientation));
    private void SetDetailYOrientation(double value) => SetProperty(ref _detailYOrientation, value, nameof(DetailYOrientation));
    private void SetDetailTriggerWidth(double value) => SetProperty(ref _detailTriggerWidth, value, nameof(DetailTriggerWidth));
    private void SetDetailTriggerHeight(double value) => SetProperty(ref _detailTriggerHeight, value, nameof(DetailTriggerHeight));
}
