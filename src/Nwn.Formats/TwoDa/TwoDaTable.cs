namespace Nwn.Formats.TwoDa;

/// <summary>An in-memory 2DA table: an ordered column list plus ordered rows, each with one value
/// per column.</summary>
public sealed class TwoDaTable
{
    public IReadOnlyList<string> Columns { get; }
    public List<TwoDaRow> Rows { get; } = [];
    public string? DefaultValue { get; }

    public TwoDaTable(IReadOnlyList<string> columns, string? defaultValue = null)
    {
        if (columns.Count == 0)
        {
            throw new ArgumentException("A 2DA table needs at least one column.", nameof(columns));
        }

        if (columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Count)
        {
            throw new ArgumentException("2DA column names must be unique.", nameof(columns));
        }

        Columns = columns;
        DefaultValue = defaultValue;
    }

    public int ColumnIndex(string column)
    {
        for (var i = 0; i < Columns.Count; i++)
        {
            if (string.Equals(Columns[i], column, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new ArgumentException($"Column '{column}' does not exist.", nameof(column));
    }

    public void AddRow(string label, IReadOnlyDictionary<string, string?> values)
    {
        var cells = new string?[Columns.Count];
        foreach (var (column, value) in values)
        {
            cells[ColumnIndex(column)] = value;
        }

        Rows.Add(new TwoDaRow(label, cells));
    }

    public string? GetValue(int rowIndex, string column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(rowIndex, Rows.Count);
        return Rows[rowIndex].Values[ColumnIndex(column)];
    }

    public TwoDaRow? FindByLabel(string label) => Rows.FirstOrDefault(r => r.Label == label);
}
