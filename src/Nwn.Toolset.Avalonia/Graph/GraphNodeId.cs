namespace Nwn.Toolset.Avalonia.Graph;

public readonly record struct GraphNodeId
{
    public GraphNodeId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 128) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}
