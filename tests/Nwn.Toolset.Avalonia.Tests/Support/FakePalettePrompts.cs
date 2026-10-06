using Nwn.Toolset.Avalonia.Palettes.Workflow;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakePalettePrompts : IPalettePrompts
{
    public Queue<string?> Answers { get; } = new();

    public List<string> Headlines { get; } = new();

    public List<string> Messages { get; } = new();

    public bool Confirms { get; set; } = true;

    public Action? DuringConfirmation { get; set; }

    public Action? DuringPrompt { get; set; }

    public Task<string?> PromptForTextAsync(string headline, string message, string initialValue, string confirmLabel)
    {
        Headlines.Add(headline);
        Messages.Add(message);
        DuringPrompt?.Invoke();
        return Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : null);
    }

    public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel)
    {
        Headlines.Add(headline);
        Messages.Add(message);
        DuringConfirmation?.Invoke();
        return Task.FromResult(Confirms);
    }
}
