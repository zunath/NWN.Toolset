namespace Nwn.Authoring.Areas.Creation;

internal sealed class PendingAreaCreationManifest
{
    public string ResRef { get; set; } = string.Empty;
    public AreaFileFingerprint Are { get; set; } = new();
    public AreaFileFingerprint Git { get; set; } = new();
    public AreaFileFingerprint Gic { get; set; } = new();
}
