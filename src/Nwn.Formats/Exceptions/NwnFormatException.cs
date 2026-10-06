// SPDX-License-Identifier: MIT

namespace Nwn.Formats.Exceptions;

/// <summary>A malformed, truncated, or unsupported native resource.</summary>
public sealed class NwnFormatException : FormatException
{
    public NwnFormatException(string message)
        : base(message)
    {
    }

    public NwnFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
