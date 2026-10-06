using Nwn.Authoring.Areas.Properties;
using Nwn.Toolset.Avalonia.Areas.Properties;
using Nwn.Toolset.Avalonia.Fields;

namespace Nwn.Toolset.Avalonia.Tests.Support;

internal sealed class FakeAreaPropertyChoiceSource : IAreaPropertyChoiceSource
{
    public Dictionary<AreaPropertyFieldId, IReadOnlyList<LookupOption>> Choices { get; } = new();

    public IReadOnlyList<LookupOption>? ChoicesFor(AreaPropertyFieldId field) =>
        Choices.TryGetValue(field, out var choices) ? choices : null;
}
