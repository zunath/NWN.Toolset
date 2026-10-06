using System.Buffers.Binary;
using System.Text;

namespace Nwn.Formats.TwoDa;

/// <summary>Reads text 2DA V2.0 tables and, when selected, binary 2DA V2.b resources.</summary>
public static class TwoDaReader
{
    private static readonly byte[] BinarySignature = "2DA V2.b\n"u8.ToArray();

    static TwoDaReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>Reads the strict text profile used by the original Xenomech authoring path.</summary>
    public static TwoDaTable Read(string text) => ReadText(text, TwoDaReadOptions.Strict);

    /// <summary>Reads a text resource with the supplied compatibility profile.</summary>
    public static TwoDaTable Read(string text, TwoDaReadOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);
        return ReadText(text, options);
    }

    /// <summary>Reads encoded text or binary resource bytes with an explicit compatibility profile.</summary>
    public static TwoDaTable Read(ReadOnlySpan<byte> bytes, TwoDaReadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        if (bytes.StartsWith(Encoding.UTF8.Preamble))
        {
            if (!options.AllowUtf8Bom)
                throw new FormatException("UTF-8 byte-order marks are disabled for this 2DA profile.");
            bytes = bytes[Encoding.UTF8.Preamble.Length..];
        }

        if (bytes.StartsWith(BinarySignature))
        {
            if (!options.AllowBinary)
                throw new FormatException("Binary 2DA V2.b is disabled for this 2DA profile.");
            return ReadBinary(bytes, options);
        }

        return ReadText(Decode(bytes, options), options);
    }

    private static TwoDaTable ReadText(string text, TwoDaReadOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateOptions(options);

        var compatibilityMode = options.AllowDefaultHeader ||
                                options.TextRowPolicy == TwoDaTextRowPolicy.PadMissingCellsAndJoinSurplusIntoLastColumn;
        var normalizedText = compatibilityMode
            ? text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            : text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = normalizedText.Split('\n');
        int cursor;
        string columnLine;
        string? defaultValue = null;
        if (compatibilityMode)
        {
            cursor = 0;
            var signatureLine = NextNonEmpty(lines, ref cursor);
            if (!string.Equals(signatureLine.Trim(), "2DA V2.0", StringComparison.Ordinal))
                throw new FormatException("Expected the 2DA V2.0 signature.");

            columnLine = NextNonEmpty(lines, ref cursor);
            if (options.AllowDefaultHeader &&
                columnLine.TrimStart().StartsWith("DEFAULT:", StringComparison.OrdinalIgnoreCase))
            {
                var colon = columnLine.IndexOf(':');
                var defaultTokens = TwoDaTokenizer.Tokenize(columnLine[(colon + 1)..]);
                defaultValue = defaultTokens.Count == 0 ? string.Empty : defaultTokens[0];
                columnLine = NextNonEmpty(lines, ref cursor);
            }
        }
        else
        {
            if (lines.Length < 2)
                throw new FormatException("2DA file is too short to contain a header.");
            if (lines[0].TrimEnd() != "2DA V2.0")
                throw new FormatException($"Expected '2DA V2.0' on the first line, found '{lines[0]}'.");

            const int columnLineIndex = 2;
            if (columnLineIndex >= lines.Length)
                throw new FormatException("2DA file is missing its column header line.");
            columnLine = lines[columnLineIndex];
            cursor = columnLineIndex + 1;
        }

        var columns = TwoDaTokenizer.Tokenize(columnLine);
        ValidateColumns(columns, options);
        ValidateTextShape(lines, cursor, columns.Count, options);

        var table = CreateTable(columns, options.AllowDefaultHeader ? defaultValue : null);
        for (; cursor < lines.Length; cursor++)
        {
            if (string.IsNullOrWhiteSpace(lines[cursor]))
                continue;

            var tokens = TwoDaTokenizer.Tokenize(lines[cursor]);
            if (tokens.Count == 0)
                continue;

            var label = tokens[0];
            var cells = tokens.Skip(1).ToList();
            if (compatibilityMode)
            {
                while (cells.Count < columns.Count)
                    cells.Add("****");
                if (cells.Count > columns.Count)
                {
                    cells[columns.Count - 1] = string.Join(' ', cells.Skip(columns.Count - 1));
                    cells.RemoveRange(columns.Count, cells.Count - columns.Count);
                }
            }
            else if (cells.Count != columns.Count)
            {
                throw new FormatException(
                    $"Row '{label}' has {cells.Count} values but the table has {columns.Count} columns.");
            }

            table.Rows.Add(new TwoDaRow(label, cells.Select(ToCellValue).ToArray()));
        }

        if (options.TextRowPolicy == TwoDaTextRowPolicy.RequireExactColumnCount)
            RejectDuplicateLabels(table);

        return table;
    }

    private static TwoDaTable ReadBinary(ReadOnlySpan<byte> bytes, TwoDaReadOptions options)
    {
        var cursor = BinarySignature.Length;
        var columns = ReadDelimitedAscii(bytes, ref cursor, (byte)'\t', stopAtNull: true,
            options.MaximumColumns, "column names");
        ValidateColumns(columns, options);

        var rowCount = ReadUInt32(bytes, ref cursor, "row count");
        if (rowCount > options.MaximumRows)
            throw new FormatException($"2DA row count {rowCount} exceeds {options.MaximumRows}.");

        var labels = new List<string>((int)rowCount);
        for (var row = 0; row < rowCount; row++)
            labels.Add(ReadDelimitedAsciiValue(bytes, ref cursor, (byte)'\t', "row label"));

        var cellCount = checked((long)rowCount * columns.Count);
        if (cellCount > options.MaximumCells)
            throw new FormatException($"2DA cell count {cellCount} exceeds {options.MaximumCells}.");
        var offsetBytes = checked(cellCount * sizeof(ushort));
        EnsureRange(bytes, cursor, checked(offsetBytes + sizeof(ushort)), "cell offsets and data size");
        var offsetsStart = cursor;
        cursor = checked(cursor + (int)offsetBytes);
        var dataSize = ReadUInt16(bytes, ref cursor, "string data size");
        var dataStart = cursor;
        EnsureRange(bytes, dataStart, dataSize, "string data");
        var dataEnd = dataStart + dataSize;

        var table = CreateTable(columns, defaultValue: null);
        for (var row = 0; row < labels.Count; row++)
        {
            var cells = new string?[columns.Count];
            for (var column = 0; column < columns.Count; column++)
            {
                var cellIndex = checked((long)row * columns.Count + column);
                var offset = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(
                    checked(offsetsStart + (int)(cellIndex * sizeof(ushort))), sizeof(ushort)));
                if (offset >= dataSize)
                    throw new FormatException("A binary 2DA cell offset is outside the declared string data.");

                var start = dataStart + offset;
                var terminator = bytes[start..dataEnd].IndexOf((byte)0);
                if (terminator < 0)
                    throw new FormatException("A binary 2DA cell is not terminated inside the declared string data.");
                cells[column] = ToCellValue(Decode(bytes.Slice(start, terminator), options));
            }

            table.Rows.Add(new TwoDaRow(labels[row], cells));
        }

        return table;
    }

    private static List<string> ReadDelimitedAscii(
        ReadOnlySpan<byte> bytes,
        ref int cursor,
        byte delimiter,
        bool stopAtNull,
        int maximumCount,
        string context)
    {
        var values = new List<string>();
        while (cursor < bytes.Length)
        {
            if (stopAtNull && bytes[cursor] == 0)
            {
                cursor++;
                return values;
            }

            values.Add(ReadDelimitedAsciiValue(bytes, ref cursor, delimiter, context));
            if (values.Count > maximumCount)
                throw new FormatException($"2DA {context} exceed the limit {maximumCount}.");
        }

        throw new FormatException($"Binary 2DA {context} are truncated.");
    }

    private static string ReadDelimitedAsciiValue(
        ReadOnlySpan<byte> bytes,
        ref int cursor,
        byte delimiter,
        string context)
    {
        var end = bytes[cursor..].IndexOf(delimiter);
        if (end < 0)
            throw new FormatException($"Binary 2DA {context} are truncated.");
        var value = Encoding.ASCII.GetString(bytes.Slice(cursor, end));
        cursor += end + 1;
        return value;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, ref int cursor, string context)
    {
        EnsureRange(bytes, cursor, sizeof(uint), context);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(cursor, sizeof(uint)));
        cursor += sizeof(uint);
        return value;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> bytes, ref int cursor, string context)
    {
        EnsureRange(bytes, cursor, sizeof(ushort), context);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(cursor, sizeof(ushort)));
        cursor += sizeof(ushort);
        return value;
    }

    private static void EnsureRange(ReadOnlySpan<byte> bytes, int offset, long count, string context)
    {
        if (offset < 0 || count < 0 || offset > bytes.Length || count > bytes.Length - offset)
            throw new FormatException($"Binary 2DA {context} exceed the input bounds.");
    }

    private static void ValidateTextShape(
        IReadOnlyList<string> lines,
        int cursor,
        int columnCount,
        TwoDaReadOptions options)
    {
        long rowCount = 0;
        for (var index = cursor; index < lines.Count; index++)
        {
            if (!string.IsNullOrWhiteSpace(lines[index]))
                rowCount++;
        }

        ValidateDimensions(rowCount, columnCount, options);
    }

    private static void ValidateDimensions(long rows, int columns, TwoDaReadOptions options)
    {
        if (rows > options.MaximumRows)
            throw new FormatException($"2DA row count {rows} exceeds {options.MaximumRows}.");
        var cells = checked(rows * columns);
        if (cells > options.MaximumCells)
            throw new FormatException($"2DA cell count {cells} exceeds {options.MaximumCells}.");
    }

    private static void ValidateColumns(IReadOnlyList<string> columns, TwoDaReadOptions options)
    {
        if (columns.Count == 0 || columns.Count > options.MaximumColumns)
            throw new FormatException($"2DA column count {columns.Count} is invalid.");
        if (columns.Any(string.IsNullOrWhiteSpace))
            throw new FormatException("2DA column names must not be empty.");
        if (columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Count)
            throw new FormatException("2DA column names must be unique.");
    }

    private static TwoDaTable CreateTable(IReadOnlyList<string> columns, string? defaultValue)
    {
        return new TwoDaTable(columns, defaultValue);
    }

    private static void RejectDuplicateLabels(TwoDaTable table)
    {
        var duplicate = table.Rows
            .GroupBy(row => row.Label, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new FormatException($"2DA file has duplicate row label '{duplicate.Key}'.");
    }

    private static string? ToCellValue(string value) => value == "****" ? null : value;

    private static string Decode(ReadOnlySpan<byte> bytes, TwoDaReadOptions options)
    {
        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(bytes);
        }
        catch (DecoderFallbackException) when (options.DecodeWindows1252WhenNotUtf8)
        {
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    private static string NextNonEmpty(IReadOnlyList<string> lines, ref int cursor)
    {
        while (cursor < lines.Count)
        {
            var line = lines[cursor++];
            if (!string.IsNullOrWhiteSpace(line))
                return line;
        }

        throw new FormatException("2DA ended before its required header rows.");
    }

    private static void ValidateOptions(TwoDaReadOptions options)
    {
        if (!Enum.IsDefined(options.TextRowPolicy))
            throw new ArgumentOutOfRangeException(nameof(options), "The 2DA text row policy is not recognized.");
        if (options.MaximumColumns < 1 || options.MaximumRows < 0 || options.MaximumCells < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "2DA read limits must be nonnegative and column limit must be positive.");
    }
}
