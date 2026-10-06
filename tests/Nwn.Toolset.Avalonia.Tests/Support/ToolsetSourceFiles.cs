namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>Reads markup and code-behind of this repository's Nwn.Toolset.Avalonia source.</summary>
internal static class ToolsetSourceFiles
{
    private const string SolutionFile = "NWN.Toolset.sln";

    public static string Read(params string[] relativePath)
    {
        var project = Path.Combine(RepositoryRoot(), "src", "Nwn.Toolset.Avalonia");
        return File.ReadAllText(Path.Combine([project, .. relativePath]));
    }

    private static string RepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFile)))
                return current.FullName;
        }

        throw new DirectoryNotFoundException($"Could not locate {SolutionFile} above the test assembly.");
    }
}
