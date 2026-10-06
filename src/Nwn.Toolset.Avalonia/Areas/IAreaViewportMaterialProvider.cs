namespace Nwn.Toolset.Avalonia.Areas;

/// <summary>Resolves host resources and appearance policy to neutral CPU image surfaces.</summary>
public interface IAreaViewportMaterialProvider
{
    /// <summary>Changes whenever host resources or appearance policy change.</summary>
    long Revision { get; }

    AreaViewportMaterial Resolve(AreaViewportMaterialRequest request);
}
