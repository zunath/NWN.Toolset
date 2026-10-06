using Nwn.Authoring.Documents.Native;

namespace Nwn.Toolset.Avalonia.Variables;

/// <summary>
/// Builds the local-variable editor with the host's key suggestions, value hints and filtering.
/// Without one, editors use the plain <see cref="VarTableSectionViewModel"/>.
/// </summary>
public interface IVarTableSectionFactory
{
    VarTableSectionViewModel Create(Func<string, Action, bool> runEdit, VarTable table);
}
