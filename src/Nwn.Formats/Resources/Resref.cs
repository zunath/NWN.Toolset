using System.Text;

namespace Nwn.Formats.Resources;

/// <summary>A bounded Aurora resource key. Public parsing validates canonical authored names;
/// archive readers preserve existing printable ASCII names with case-insensitive identity.</summary>
public readonly struct Resref : IEquatable<Resref>
{
    public const int MaxLength = 16;

    public string Value { get; }

    private Resref(string value) => Value = value;

    public static Resref Parse(string value)
    {
        if (!TryParse(value, out var resref, out var error))
        {
            throw new FormatException(error);
        }

        return resref;
    }

    public static bool TryParse(string? value, out Resref resref, out string? error)
    {
        resref = default;
        if (string.IsNullOrEmpty(value)) { error = "Resref cannot be empty."; return false; }
        if (value.Length > MaxLength) { error = $"Resref '{value}' exceeds {MaxLength} characters."; return false; }
        foreach (var character in value)
        {
            var isLower = character is >= 'a' and <= 'z';
            var isDigit = character is >= '0' and <= '9';
            var isUnderscore = character == '_';
            if (!isLower && !isDigit && !isUnderscore)
            {
                error = $"Resref '{value}' contains '{character}': only lowercase a-z, 0-9, and underscore are allowed.";
                return false;
            }
        }
        error = null;
        resref = new Resref(value);
        return true;
    }

    internal static bool TryReadArchiveKey(ReadOnlySpan<byte> bytes, out Resref resref, out string? error)
    {
        resref = default;
        var terminator = bytes.IndexOf((byte)0);
        var name = terminator < 0 ? bytes : bytes[..terminator];
        if (name.Length is < 1 or > MaxLength)
        {
            error = $"Archive resref must contain between 1 and {MaxLength} bytes.";
            return false;
        }
        foreach (var character in name)
        {
            if (character is < 0x20 or > 0x7e)
            {
                error = $"Archive resref contains unsupported byte 0x{character:x2}.";
                return false;
            }
        }
        var value = Encoding.ASCII.GetString(name).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Archive resref cannot contain only whitespace.";
            return false;
        }
        resref = new(value);
        error = null;
        return true;
    }

    public bool Equals(Resref other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is Resref other && Equals(other);
    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;

    public static bool operator ==(Resref left, Resref right) => left.Equals(right);
    public static bool operator !=(Resref left, Resref right) => !left.Equals(right);
}
