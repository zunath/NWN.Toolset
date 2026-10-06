using System.Text;

namespace Nwn.Formats.TwoDa;

/// <summary>Writes the 2DA V2.0 text format, column-aligned with padding so a diff of a single
/// changed cell touches only that cell's column, not the whole line's alignment.</summary>
public static class TwoDaWriter
{
    public static string Write(TwoDaTable table)
    {
        var labelWidth = Math.Max("".Length, table.Rows.Count == 0 ? 0 : table.Rows.Max(r => r.Label.Length));
        var columnWidths = new int[table.Columns.Count];
        for (var c = 0; c < table.Columns.Count; c++)
        {
            var width = table.Columns[c].Length;
            foreach (var row in table.Rows)
            {
                width = Math.Max(width, TwoDaTokenizer.Format(row.Values[c]).Length);
            }

            columnWidths[c] = width;
        }

        var builder = new StringBuilder();
        builder.Append("2DA V2.0\n\n");

        builder.Append(new string(' ', labelWidth));
        for (var c = 0; c < table.Columns.Count; c++)
        {
            builder.Append(' ').Append(table.Columns[c].PadRight(columnWidths[c]));
        }

        builder.Append('\n');

        foreach (var row in table.Rows)
        {
            builder.Append(row.Label.PadRight(labelWidth));
            for (var c = 0; c < table.Columns.Count; c++)
            {
                builder.Append(' ').Append(TwoDaTokenizer.Format(row.Values[c]).PadRight(columnWidths[c]));
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }
}
