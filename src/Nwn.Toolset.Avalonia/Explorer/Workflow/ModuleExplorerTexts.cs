using System.Globalization;
using System.Text.Json;

namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>Complete host-replaceable text for the shared Module Contents panel and its workflow.</summary>
public sealed class ModuleExplorerTexts
{
    private const string ResourceName = "Nwn.Toolset.Avalonia.Localization.ModuleExplorerEnglish.json";
    private readonly IReadOnlyDictionary<ModuleExplorerStringId, string> _values;

    public static ModuleExplorerTexts English { get; } = LoadEnglish();

    public ModuleExplorerTexts(IReadOnlyDictionary<ModuleExplorerStringId, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var ids = Enum.GetValues<ModuleExplorerStringId>();
        if (values.Count != ids.Length || ids.Any(id => !values.ContainsKey(id)))
            throw new ArgumentException("The Module Contents catalog does not match its typed IDs.", nameof(values));

        _values = new Dictionary<ModuleExplorerStringId, string>(values);
    }

    public string SearchWatermark => Get(ModuleExplorerStringId.SearchWatermark);
    public string NewFolderMenu => Get(ModuleExplorerStringId.NewFolderMenu);
    public string RenameFolderMenu => Get(ModuleExplorerStringId.RenameFolderMenu);
    public string DeleteFolderMenu => Get(ModuleExplorerStringId.DeleteFolderMenu);
    public string OpenMenu => Get(ModuleExplorerStringId.OpenMenu);
    public string CompileMenu => Get(ModuleExplorerStringId.CompileMenu);
    public string MoveToMenu => Get(ModuleExplorerStringId.MoveToMenu);
    public string RemoveFromFolderMenu => Get(ModuleExplorerStringId.RemoveFromFolderMenu);
    public string DeleteMenu => Get(ModuleExplorerStringId.DeleteMenu);

    public string Get(ModuleExplorerStringId id, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, _values[id], args);

    private static ModuleExplorerTexts LoadEnglish()
    {
        using var stream = typeof(ModuleExplorerTexts).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The Module Contents string catalog is missing.");
        var values = JsonSerializer.Deserialize<Dictionary<ModuleExplorerStringId, string>>(stream)
            ?? throw new InvalidOperationException("The Module Contents string catalog is null.");
        return new(values);
    }
}
