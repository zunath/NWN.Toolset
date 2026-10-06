using Avalonia.Media.Imaging;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Palettes;
using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Previews that never finish on their own, recording the token each request was given.</summary>
internal sealed class FakePalettePreviewSource : IPalettePreviewSource
{
    public event Action<ModuleResourceType, string>? Invalidated;

    public bool IsAvailable => true;

    public List<CancellationToken> Tokens { get; } = new();

    public Bitmap? TypeIcon(ModuleResourceType? type) => null;

    public Task<Bitmap?> LoadBlueprintAsync(
        ModuleResourceType type,
        string resRef,
        PaletteSource source,
        CancellationToken cancellationToken) => Render(cancellationToken);

    public Task<Bitmap?> LoadTileAsync(TilePaletteEntry tile, CancellationToken cancellationToken) =>
        Render(cancellationToken);

    public void Invalidate(ModuleResourceType type, string resRef) => Invalidated?.Invoke(type, resRef);

    private Task<Bitmap?> Render(CancellationToken cancellationToken)
    {
        Tokens.Add(cancellationToken);
        return Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith<Bitmap?>(
            _ => null,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
