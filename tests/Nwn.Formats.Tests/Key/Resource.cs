namespace Nwn.Formats.Tests.Key;

/// <summary>One synthetic KEY/BIF resource entry for <see cref="KeyBifFixture"/> to assemble.</summary>
public sealed record Resource(string ResRef, ushort TypeCode, string BifFilename, byte[] Bytes);
