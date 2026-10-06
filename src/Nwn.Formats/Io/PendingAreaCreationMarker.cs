namespace Nwn.Formats.Io;

/// <summary>Shared filename contract for interrupted new-area transactions.</summary>
public static class PendingAreaCreationMarker
{
    public const string DefaultPrefix = ".nwn-toolset-new-area-";
    public const string Extension = ".pending";

    public static IEnumerable<string> Enumerate(string moduleRoot, string prefix = DefaultPrefix) =>
        Directory.Exists(moduleRoot)
            ? Directory.EnumerateFiles(moduleRoot, prefix + "*" + Extension, SearchOption.TopDirectoryOnly)
            : Enumerable.Empty<string>();
}
