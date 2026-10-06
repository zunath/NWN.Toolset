using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace Nwn.Toolset.Avalonia.Docking;

/// <summary>Composes the existing builder rails, document tabs and output dock around host-owned panels.</summary>
public class ToolsetDockFactory : Factory
{
    private readonly ToolsetDockPanels _panels;
    private readonly IReadOnlyDictionary<string, double>? _savedProportions;
    private IRootDock? _rootDock;
    private DocumentDock? _documentDock;

    public event Action<Document?>? ActiveDocumentChanged;
    public event Action? ProportionsChanged;
    public Document? ActiveDocument => _documentDock?.ActiveDockable as Document;
    public IRootDock? RootDock => _rootDock;

    public ToolsetDockFactory(ToolsetDockPanels panels, IReadOnlyDictionary<string, double>? savedProportions = null)
    {
        ArgumentNullException.ThrowIfNull(panels);
        if (panels.ExplorerTools.Count == 0 || panels.PaletteTools.Count == 0 || panels.OutputTools.Count == 0)
            throw new ArgumentException("The explorer, palette and output rails require a host panel.", nameof(panels));
        _panels = panels;
        _savedProportions = savedProportions;
    }

    protected virtual void OnActiveDocumentChanged(Document? document) => ActiveDocumentChanged?.Invoke(document);

    // Dock uses these virtual constructors while splitting, floating, pinning, and otherwise
    // reshaping a layout after startup. Keep those newly-created models under the same non-null
    // theme contract as the fixed layout assembled below.
    public override IRootDock CreateRootDock() => PrepareForThemeBindings(base.CreateRootDock());
    public override IProportionalDock CreateProportionalDock() => PrepareForThemeBindings(base.CreateProportionalDock());
    public override IDockDock CreateDockDock() => PrepareForThemeBindings(base.CreateDockDock());
    public override IStackDock CreateStackDock() => PrepareForThemeBindings(base.CreateStackDock());
    public override IGridDock CreateGridDock() => PrepareForThemeBindings(base.CreateGridDock());
    public override IWrapDock CreateWrapDock() => PrepareForThemeBindings(base.CreateWrapDock());
    public override IUniformGridDock CreateUniformGridDock() => PrepareForThemeBindings(base.CreateUniformGridDock());
    public override IProportionalDockSplitter CreateProportionalDockSplitter() =>
        PrepareForThemeBindings(base.CreateProportionalDockSplitter());
    public override IGridDockSplitter CreateGridDockSplitter() =>
        PrepareForThemeBindings(base.CreateGridDockSplitter());
    public override IToolDock CreateToolDock() => PrepareForThemeBindings(base.CreateToolDock());
    public override IDocumentDock CreateDocumentDock() => PrepareForThemeBindings(base.CreateDocumentDock());
    public override ISplitViewDock CreateSplitViewDock() => PrepareForThemeBindings(base.CreateSplitViewDock());
    public override IDocument CreateDocument() => PrepareForThemeBindings(base.CreateDocument());
    public override ITool CreateTool() => PrepareForThemeBindings(base.CreateTool());

    public override IRootDock CreateLayout()
    {
        // Area Contents and the resource explorer share a rail without nesting their lists.
        var explorerDock = new ToolDock
        {
            Id = ToolsetDockId.ExplorerDock.ToString(),
            ActiveDockable = _panels.ExplorerTools[0],
            VisibleDockables = CreateList<IDockable>(_panels.ExplorerTools.ToArray()),
            Alignment = Alignment.Left,
            Proportion = 0.26,
            // Each panel draws its own title; Dock's chrome adds a dotted drag grip and window
            // buttons that appear nowhere in the design.
            GripMode = GripMode.Hidden
        };

        // Palette and optional reference tools occupy the right rail; properties stay in documents.
        var paletteDock = new ToolDock
        {
            Id = ToolsetDockId.PaletteDock.ToString(),
            ActiveDockable = _panels.PaletteTools[0],
            // The host supplies the tools appropriate to its active document.
            VisibleDockables = CreateList<IDockable>(_panels.PaletteTools.ToArray()),
            Alignment = Alignment.Right,
            Proportion = 0.25,
            // Each panel draws its own title; Dock's chrome adds a dotted drag grip and window
            // buttons that appear nowhere in the design.
            GripMode = GripMode.Hidden
        };

        _documentDock = new DocumentDock
        {
            Id = ToolsetDockId.Documents.ToString(),
            IsCollapsable = false,
            CanCreateDocument = false,
            VisibleDockables = CreateList<IDockable>(),
            Proportion = 0.47
        };
        _documentDock.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(DocumentDock.ActiveDockable))
                return;

            OnActiveDocumentChanged(_documentDock.ActiveDockable as Document);
        };

        var middleLayout = new ProportionalDock
        {
            Id = ToolsetDockId.MiddleLayout.ToString(),
            Orientation = Orientation.Horizontal,
            // Splitters consume width; the panel proportions leave space for them.
            Proportion = 0.72,
            VisibleDockables = CreateList<IDockable>(
                explorerDock,
                new ProportionalDockSplitter(),
                _documentDock,
                new ProportionalDockSplitter(),
                paletteDock)
        };

        var outputDock = new ToolDock
        {
            Id = ToolsetDockId.OutputDock.ToString(),
            ActiveDockable = _panels.OutputTools[0],
            VisibleDockables = CreateList<IDockable>(_panels.OutputTools.ToArray()),
            Alignment = Alignment.Bottom,
            Proportion = 0.20,
            // Each panel draws its own title; Dock's chrome adds a dotted drag grip and window
            // buttons that appear nowhere in the design.
            GripMode = GripMode.Hidden
        };

        var mainLayout = new ProportionalDock
        {
            Id = ToolsetDockId.MainLayout.ToString(),
            Orientation = Orientation.Vertical,
            // Searches remain inside their respective panels so the map keeps its height.
            VisibleDockables = CreateList<IDockable>(
                middleLayout,
                new ProportionalDockSplitter(),
                outputDock)
        };

        var rootDock = CreateRootDock();
        rootDock.Id = ToolsetDockId.Root.ToString();
        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        _rootDock = rootDock;

        PrepareForThemeBindings(rootDock);

        // The proportions set above are the designed layout; anything the builder dragged last
        // session replaces them before the layout is ever shown, so the window does not open on the
        // defaults and then jump.
        DockProportions.Apply(rootDock, _savedProportions);
        DockProportions.Watch(rootDock, () => ProportionsChanged?.Invoke());

        return rootDock;
    }

    public IReadOnlyDictionary<string, double> CaptureProportions() => DockProportions.Capture(_rootDock);

    public virtual void OpenDocument(Document document)
    {
        if (_documentDock is null) return;
        PrepareForThemeBindings(document);
        AddDockable(_documentDock, document);
        ActivateDocument(document);
    }

    public void ActivateDocument(Document document)
    {
        if (_documentDock is null) return;
        SetActiveDockable(document);
        SetFocusedDockable(_documentDock, document);
    }

    public void Focus(IDockable dockable)
    {
        SetActiveDockable(dockable);
        if (dockable.Owner is IDock owner) SetFocusedDockable(owner, dockable);
    }

    public void CloseDocument(Document document) => CloseDockable(document);

    private static T PrepareForThemeBindings<T>(T root) where T : IDockable
    {
        foreach (var dockable in DockProportions.Walk(root))
        {
            dockable.DockCapabilityOverrides ??= new DockCapabilityOverrides();

            if (dockable is IDock dock)
                dock.DockCapabilityPolicy ??= new DockCapabilityPolicy();

            if (dockable is IRootDock rootDock)
                rootDock.RootDockCapabilityPolicy ??= new DockCapabilityPolicy();
        }

        return root;
    }

    public override void InitLayout(IDockable layout)
    {
        var tools = _panels.ExplorerTools.Concat(_panels.PaletteTools).Concat(_panels.OutputTools)
            .Concat(_panels.AdditionalContextTools ?? Array.Empty<IDockable>());
        ContextLocator = tools.DistinctBy(tool => tool.Id).ToDictionary(tool => tool.Id,
            tool => (Func<object?>)(() => tool), StringComparer.Ordinal);
        DockableLocator = new Dictionary<string, Func<IDockable?>>
        {
            [ToolsetDockId.Root.ToString()] = () => _rootDock,
        };
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow(),
        };
        base.InitLayout(layout);
    }
}
