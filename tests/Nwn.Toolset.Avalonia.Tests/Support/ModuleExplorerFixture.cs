using Nwn.Authoring.Categories;
using Nwn.Authoring.Resources;
using Nwn.Toolset.Avalonia.Explorer;
using Nwn.Toolset.Avalonia.Explorer.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

/// <summary>A Module Contents controller wired to in-memory fakes for every host service.</summary>
internal sealed class ModuleExplorerFixture : IDisposable
{
    private ModuleExplorerController? _controller;

    public ModuleExplorerFixture()
    {
        Creation = new FakeExplorerCreation(Content);
        Deletion = new FakeExplorerDeletion(Content);
    }

    public FakeExplorerContentSource Content { get; } = new();

    public FakePaletteCategoryStore Categories { get; } = new();

    public FakeExplorerOrganization Organization { get; } = new();

    public FakeExplorerContentSearch Search { get; } = new();

    public FakeExplorerCreation Creation { get; }

    public FakeExplorerEditors Editors { get; } = new();

    public FakeExplorerDeletion Deletion { get; }

    public FakeExplorerSelection Selection { get; } = new();

    public FakePalettePrompts Prompts { get; } = new();

    public FakeExplorerSettings Settings { get; } = new();

    public FakePaletteWriteGate WriteGate { get; } = new();

    public FakePaletteLog Log { get; } = new();

    public ModuleExplorerController Controller => _controller ??= new ModuleExplorerController(
        new ModuleExplorerHost(Content, Categories)
        {
            Organization = Organization,
            Search = Search,
            Creation = Creation,
            Editors = Editors,
            Deletion = Deletion,
            Selection = Selection,
            Prompts = Prompts,
            Settings = Settings,
            WriteGate = WriteGate,
            Log = Log
        });

    public CategorySection Section(ModuleResourceType type) => Categories.Live.Section(type);

    /// <summary>Marks a section organized so seeding leaves it alone, and persists it.</summary>
    public CategorySection SeededSection(ModuleResourceType type)
    {
        var section = Section(type);
        section.IsSeeded = true;
        Categories.Commit();
        return section;
    }

    /// <summary>Opens the controller on a tab and builds its tree.</summary>
    public ModuleExplorerController Open(ModuleResourceType type)
    {
        Controller.SelectedType = type;
        Controller.Initialize();
        return Controller;
    }

    public ExplorerNodeViewModel Row(string name) =>
        Controller.Rows.FirstOrDefault(row => row.Name == name)
        ?? throw new InvalidOperationException($"No visible row named '{name}'.");

    public ExplorerNodeViewModel FolderRow(CategoryFolder folder) =>
        Controller.Rows.Single(row => ReferenceEquals(row.Folder, folder));

    public ExplorerNodeViewModel Resource(ExplorerNodeViewModel parent, string resRef) =>
        parent.Children.Single(row => row.ResRef == resRef);

    public void Dispose() => _controller?.Dispose();
}
