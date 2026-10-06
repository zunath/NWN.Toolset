namespace Nwn.Formats.Tests.Key;

/// <summary>The synthetic KEY file bytes plus one BIF's bytes per filename, from <see cref="KeyBifFixture.Build"/>.</summary>
public sealed record Built(byte[] KeyBytes, IReadOnlyDictionary<string, byte[]> BifBytesByFilename);
