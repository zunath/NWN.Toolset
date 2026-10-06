using System.Collections.ObjectModel;

namespace Nwn.Formats.Mtr;

/// <summary>An immutable named parameter retaining its original type and values alongside supported numeric components.</summary>
public sealed class MtrParameter
{
    public string Name { get; }
    public MtrParameterKind Kind { get; }
    public IReadOnlyList<double> Values { get; }
    public string TypeName { get; }
    public IReadOnlyList<string> RawValues { get; }

    internal MtrParameter(string name, MtrParameterKind kind, double[] values, string typeName, string[] rawValues)
    {
        Name = name;
        Kind = kind;
        Values = new ReadOnlyCollection<double>(values.ToArray());
        TypeName = typeName;
        RawValues = new ReadOnlyCollection<string>(rawValues.ToArray());
    }
}
