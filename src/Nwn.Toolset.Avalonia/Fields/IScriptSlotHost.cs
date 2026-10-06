namespace Nwn.Toolset.Avalonia.Fields;

/// <summary>What a script slot needs from the host to browse, open and validate.</summary>
public interface IScriptSlotHost
{
    bool ScriptExists(string resRef);

    void OpenScript(string resRef);

    /// <summary>Shows the picker, returning the chosen resref or null if cancelled.</summary>
    Task<string?> PickScriptAsync(string current);
}
