// SPDX-License-Identifier: MIT

namespace Nwn.Formats.Resources;

/// <summary>Native case-insensitive resource-field rules, distinct from canonical output identifiers.</summary>
public static class ResourceReferenceRules
{
    public const int MaxLength = 16;

    public static bool IsValid(string? value) =>
        value is { Length: >= 1 and <= MaxLength } &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    public static bool IsCanonical(string? value) =>
        value is { Length: >= 1 and <= MaxLength } &&
        value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
}
