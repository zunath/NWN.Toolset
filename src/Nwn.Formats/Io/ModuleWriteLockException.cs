// SPDX-License-Identifier: MIT

namespace Nwn.Formats.Io;

/// <summary>Raised when another process is already mutating or walking the same module.</summary>
public sealed class ModuleWriteLockException(string moduleRoot, Exception innerException)
    : IOException($"Timed out waiting for the pack, unpack, or module writer using '{moduleRoot}' to finish.", innerException);
