// SPDX-License-Identifier: MIT

using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Resources;
using Nwn.Preview.Scene;

namespace Nwn.Preview.Areas;

/// <summary>Host-owned resource and appearance lookups used by <see cref="AreaSceneComposer"/>.</summary>
public sealed class AreaSceneResolvers
{
    public required Func<string, RenderModel?> ResolveTileModel { get; init; }
    public Func<string, WalkMesh?>? ResolveTileWalkmesh { get; init; }
    public Func<ModuleResourceType, JsonGffStruct, ResolvedInstanceAppearance?>? ResolveInstanceAppearance
        { get; init; }

    internal ResolvedInstanceAppearance ResolveInstance(ModuleResourceType type, JsonGffStruct instance)
        => ResolveInstanceAppearance?.Invoke(type, instance) ?? new ResolvedInstanceAppearance();
}
