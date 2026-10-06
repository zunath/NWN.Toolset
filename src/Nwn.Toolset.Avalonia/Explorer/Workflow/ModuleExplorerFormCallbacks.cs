namespace Nwn.Toolset.Avalonia.Explorer.Workflow;

/// <summary>
/// What a creation form reports back. The folder to file into was captured when the form opened: the form
/// is nonmodal, so the builder can switch tabs or folders while it sits open.
/// </summary>
/// <param name="Created">Call with the new resref once the resource has been written.</param>
/// <param name="Cancelled">Call when the builder closes the form without creating anything.</param>
/// <param name="CanWrite">
/// Ask at the moment of writing, not when the form opened: a module-wide operation or a delete may have
/// started while it sat on screen.
/// </param>
public sealed record ModuleExplorerFormCallbacks(Action<string> Created, Action Cancelled, Func<bool> CanWrite);
