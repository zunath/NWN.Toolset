namespace Nwn.Toolset.Avalonia.Palettes.Workflow;

/// <summary>The host's modal prompts. Without them the palette offers nothing that asks a question.</summary>
public interface IPalettePrompts
{
    /// <summary>Asks for a line of text. Null when the builder cancels.</summary>
    Task<string?> PromptForTextAsync(string headline, string message, string initialValue, string confirmLabel);

    /// <summary>Asks the builder to confirm something that cannot be undone.</summary>
    Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel);
}
