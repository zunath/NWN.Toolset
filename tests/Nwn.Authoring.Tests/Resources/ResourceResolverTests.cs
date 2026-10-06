using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Resources;
using Nwn.Formats.Erf;
using Nwn.Formats.Resources;

namespace Nwn.Authoring.Tests.Resources;

[TestClass]
public sealed class ResourceResolverTests
{
    [TestMethod]
    public void LegacyArchiveKeysRetainTheirProvenanceAndBoundedPayloadReads()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "legacy.hak");
        using var output = new MemoryStream();
        ErfWriter.Write(output, ErfContainerKind.Hak,
            [ErfResourceSource.FromBytes(Resref.Parse("original"), ResourceType.TwoDa, [1, 7, 25])], ErfBuildDate.ContentEpoch);
        var bytes = output.ToArray();
        var keyOffset = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)));
        bytes.AsSpan(keyOffset, Resref.MaxLength).Clear();
        System.Text.Encoding.ASCII.GetBytes("iprp_spells past").CopyTo(bytes, keyOffset);
        File.WriteAllBytes(path, bytes);
        using var resolver = new ResourceResolver([ResourceLayer.FromErf("assigned-hak", path)], ownsLayers: true);

        var identity = new ResourceIdentity("IPRP_SPELLS PAST", ResourceType.TwoDa);
        CollectionAssert.AreEqual(new[] { identity }, resolver.Resources.ToArray());
        var handle = resolver.ResolveHandle(identity);
        Assert.IsNotNull(handle);
        Assert.AreEqual("assigned-hak", handle.Winner.LayerName);
        Assert.AreEqual(Path.GetFullPath(path), handle.Winner.SourcePath);
        Assert.AreEqual("iprp_spells past.2da", handle.Winner.Entry);
        Assert.ThrowsExactly<FormatException>(() => handle.ReadBytes(2));
        CollectionAssert.AreEqual(new byte[] { 1, 7, 25 }, handle.ReadBytes(3));
    }

    [TestMethod]
    public void Resources_ReturnDistinctIdentitiesInLayerPrecedenceWithoutReadingBifPayloads()
    {
        using var temporary = new TemporaryDirectory();
        var firstLoose = Path.Combine(temporary.Path, "first");
        var secondLoose = Path.Combine(temporary.Path, "second");
        Directory.CreateDirectory(firstLoose);
        Directory.CreateDirectory(secondLoose);
        File.WriteAllBytes(Path.Combine(firstLoose, "shared.2da"), [1]);
        File.WriteAllBytes(Path.Combine(firstLoose, "first.2da"), [2]);
        File.WriteAllBytes(Path.Combine(secondLoose, "shared.2da"), [3]);
        var dataRoot = Path.Combine(temporary.Path, "data-root");
        Directory.CreateDirectory(Path.Combine(dataRoot, "data"));
        var keyed = new SyntheticKeyResource("keyed", (ushort)ResourceType.TwoDa, [4]);
        WriteKeyFixture(temporary.Path, dataRoot, [keyed]);
        using var keyLayer = ResourceLayer.FromKeyBif("key", Path.Combine(temporary.Path, "test.key"), dataRoot);
        var bifPath = Path.Combine(dataRoot, "data", "test.bif");
        File.Delete(bifPath);
        using var resolver = new ResourceResolver(
        [
            ResourceLayer.FromLooseDirectory("highest", firstLoose),
            ResourceLayer.FromLooseDirectory("lower", secondLoose),
            keyLayer
        ], ownsLayers: true);

        CollectionAssert.AreEqual(
            new[]
            {
                new ResourceIdentity("first", ResourceType.TwoDa),
                new ResourceIdentity("shared", ResourceType.TwoDa),
                new ResourceIdentity("keyed", ResourceType.TwoDa)
            }, resolver.Resources.ToArray());
        CollectionAssert.AreEqual(new byte[] { 1 },
            resolver.Resolve(new ResourceIdentity("shared", ResourceType.TwoDa))!.Bytes);
        Assert.IsNotNull(resolver.ResolveHandle(new ResourceIdentity("keyed", ResourceType.TwoDa)));
        Assert.ThrowsExactly<FileNotFoundException>(() =>
            resolver.ResolveHandle(new ResourceIdentity("keyed", ResourceType.TwoDa))!.ReadBytes());
        Assert.ThrowsExactly<NotSupportedException>(() =>
            ((IList<ResourceIdentity>)resolver.Resources).Add(new ResourceIdentity("mutate", ResourceType.TwoDa)));
    }

    [TestMethod]
    public void Resolve_UsesExplicitPrecedenceAndReportsContainerProvenance()
    {
        using var temporary = new TemporaryDirectory();
        var loose = Path.Combine(temporary.Path, "override");
        var dataRoot = Path.Combine(temporary.Path, "data-root");
        Directory.CreateDirectory(loose);
        Directory.CreateDirectory(Path.Combine(dataRoot, "data"));
        File.WriteAllBytes(Path.Combine(loose, "same.2da"), [3]);
        var hakPath = Path.Combine(temporary.Path, "test.hak");
        WriteErf(hakPath, [2]);
        WriteKeyFixture(temporary.Path, dataRoot, [new SyntheticKeyResource("same", (ushort)ResourceType.TwoDa, [1])]);
        using var resolver = new ResourceResolver(
        [
            ResourceLayer.FromLooseDirectory("explicit-override", loose),
            ResourceLayer.FromErf("module-hak", hakPath),
            ResourceLayer.FromKeyBif("base-game", Path.Combine(temporary.Path, "test.key"), dataRoot)
        ], ownsLayers: true);

        var result = resolver.Resolve(new ResourceIdentity("SAME", ResourceType.TwoDa));

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new byte[] { 3 }, result.Bytes);
        Assert.AreEqual(ResourceLayerKind.LooseDirectory, result.Winner.LayerKind);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(loose, "same.2da")), result.Winner.SourcePath);
        CollectionAssert.AreEqual(new[] { "module-hak", "base-game" },
            result.Overridden.Select(source => source.LayerName).ToArray());
        Assert.AreEqual(Path.GetFullPath(hakPath), result.Overridden[0].SourcePath);
        Assert.AreEqual(Path.GetFullPath(hakPath), result.Overridden[0].ContainerPath);
    }

    [TestMethod]
    public void Resolve_TgaResourcesFromLooseAndKeyLayers()
    {
        using var temporary = new TemporaryDirectory();
        var loose = Path.Combine(temporary.Path, "loose");
        var dataRoot = Path.Combine(temporary.Path, "data-root");
        Directory.CreateDirectory(loose);
        Directory.CreateDirectory(Path.Combine(dataRoot, "data"));
        File.WriteAllBytes(Path.Combine(loose, "texture.tga"), [2]);
        WriteKeyFixture(temporary.Path, dataRoot,
            [new SyntheticKeyResource("texture", (ushort)ResourceType.Tga, [1])]);
        using var looseResolver = new ResourceResolver(
            [ResourceLayer.FromLooseDirectory("loose", loose)]);
        using var keyResolver = new ResourceResolver(
            [ResourceLayer.FromKeyBif("base", Path.Combine(temporary.Path, "test.key"), dataRoot)]);

        var looseResult = looseResolver.Resolve(new ResourceIdentity("texture", ResourceType.Tga));
        var keyResult = keyResolver.Resolve(new ResourceIdentity("texture", ResourceType.Tga));

        Assert.IsNotNull(looseResult);
        CollectionAssert.AreEqual(new byte[] { 2 }, looseResult.Bytes);
        Assert.IsNotNull(keyResult);
        CollectionAssert.AreEqual(new byte[] { 1 }, keyResult.Bytes);
        Assert.AreEqual(ResourceLayerKind.KeyBif, keyResult.Winner.LayerKind);
    }

    [TestMethod]
    public void Resolve_PreservesNativeRawKeyResrefsAndAppliesDuplicatePolicy()
    {
        using var temporary = new TemporaryDirectory();
        var dataRoot = Path.Combine(temporary.Path, "data-root");
        Directory.CreateDirectory(Path.Combine(dataRoot, "data"));
        WriteKeyFixture(temporary.Path, dataRoot,
        [
            new SyntheticKeyResource("native name!", (ushort)ResourceType.TwoDa, [1]),
            new SyntheticKeyResource("native name!", (ushort)ResourceType.TwoDa, [2])
        ]);
        var keyPath = Path.Combine(temporary.Path, "test.key");

        Assert.ThrowsExactly<FormatException>(() => ResourceLayer.FromKeyBif("base", keyPath, dataRoot));
        using var layer = ResourceLayer.FromKeyBif("base", keyPath, dataRoot, ResourceDuplicatePolicy.LastWins);
        using var resolver = new ResourceResolver([layer]);
        var result = resolver.Resolve(new ResourceIdentity("NATIVE NAME!", ResourceType.TwoDa));

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new byte[] { 2 }, result.Bytes);
        Assert.AreEqual("native name!", result.Identity.Resref);
        Assert.AreEqual("data/test.bif", result.Winner.Entry);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(dataRoot, "data", "test.bif")), result.Winner.SourcePath);
        Assert.AreEqual(Path.GetFullPath(keyPath), result.Winner.ContainerPath);
        Assert.AreEqual(1, result.Overridden.Count);
    }

    [TestMethod]
    public void Resolve_DoesNotDiscoverUnconfiguredDirectories()
    {
        using var resolver = new ResourceResolver([]);
        Assert.IsNull(resolver.Resolve(new ResourceIdentity("missing", ResourceType.TwoDa)));
    }

    [TestMethod]
    public void LooseDirectory_IgnoresDotfilesButStillRejectsMalformedResourceNames()
    {
        using var temporary = new TemporaryDirectory();
        var directory = Path.Combine(temporary.Path, "module");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, ".git"), [1]);
        File.WriteAllBytes(Path.Combine(directory, "area.git"), [2, 3]);

        using (var layer = ResourceLayer.FromLooseDirectory("module", directory))
        using (var resolver = new ResourceResolver([layer]))
        {
            CollectionAssert.AreEqual(new[] { new ResourceIdentity("area", ResourceType.Git) }, resolver.Resources.ToArray());
            CollectionAssert.AreEqual(new byte[] { 2, 3 }, resolver.Resolve(new ResourceIdentity("area", ResourceType.Git))!.Bytes);
        }

        File.WriteAllBytes(Path.Combine(directory, "resource-name-that-is-far-too-long.git"), [4]);
        Assert.ThrowsExactly<ArgumentException>(() => ResourceLayer.FromLooseDirectory("malformed", directory));
    }
    [TestMethod]
    public void ResolveHandle_ReportsProvenanceWithoutReadingUntilRequested()
    {
        using var temporary = new TemporaryDirectory();
        var directory = Path.Combine(temporary.Path, "loose");
        Directory.CreateDirectory(directory);
        var resourcePath = Path.Combine(directory, "asset.2da");
        File.WriteAllBytes(resourcePath, [1, 2, 3]);
        using var layer = ResourceLayer.FromLooseDirectory("fixture", directory);
        using var resolver = new ResourceResolver([layer]);

        var handle = resolver.ResolveHandle(new ResourceIdentity("asset", ResourceType.TwoDa));
        Assert.IsNotNull(handle);
        Assert.AreEqual(Path.GetFullPath(resourcePath), handle.Winner.SourcePath);
        File.Delete(resourcePath);

        Assert.ThrowsExactly<FileNotFoundException>(() => handle.ReadBytes());
    }

    [TestMethod]
    public void ResolveHandle_AppliesCallerByteLimitBeforePayloadAllocation()
    {
        using var temporary = new TemporaryDirectory();
        var directory = Path.Combine(temporary.Path, "loose");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "large.2da"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(directory, "small.2da"), [4]);
        using var layer = ResourceLayer.FromLooseDirectory("fixture", directory);
        using var resolver = new ResourceResolver([layer]);

        var large = resolver.ResolveHandle(new ResourceIdentity("large", ResourceType.TwoDa));
        var small = resolver.ResolveHandle(new ResourceIdentity("small", ResourceType.TwoDa));
        Assert.IsNotNull(large);
        Assert.IsNotNull(small);
        Assert.ThrowsExactly<FormatException>(() => large.ReadBytes(2));
        CollectionAssert.AreEqual(new byte[] { 4 }, small.ReadBytes(1));
    }

    [TestMethod]
    public void ResolveHandle_ForwardsCallerByteLimitToErfAndKeyBifReaders()
    {
        using var temporary = new TemporaryDirectory();
        var archivePath = Path.Combine(temporary.Path, "large.hak");
        WriteErf(archivePath, [1, 2, 3]);
        using var erfLayer = ResourceLayer.FromErf("hak", archivePath);
        using var erfResolver = new ResourceResolver([erfLayer]);
        var erfHandle = erfResolver.ResolveHandle(new ResourceIdentity("same", ResourceType.TwoDa))!;
        Assert.ThrowsExactly<FormatException>(() => erfHandle.ReadBytes(2));

        var dataRoot = Path.Combine(temporary.Path, "data-root");
        Directory.CreateDirectory(Path.Combine(dataRoot, "data"));
        WriteKeyFixture(temporary.Path, dataRoot,
            [new SyntheticKeyResource("large", (ushort)ResourceType.TwoDa, [1, 2, 3])]);
        using var keyLayer = ResourceLayer.FromKeyBif("base", Path.Combine(temporary.Path, "test.key"), dataRoot);
        using var keyResolver = new ResourceResolver([keyLayer]);
        var keyHandle = keyResolver.ResolveHandle(new ResourceIdentity("large", ResourceType.TwoDa))!;
        Assert.ThrowsExactly<FormatException>(() => keyHandle.ReadBytes(2));
    }

    [TestMethod]
    public void ResourceIdentity_PreservesNativePunctuationButRejectsControlCharacters()
    {
        var identity = new ResourceIdentity("A name!", ResourceType.TwoDa);
        Assert.AreEqual("a name!", identity.Resref);
        Assert.ThrowsExactly<ArgumentException>(() => new ResourceIdentity("bad\0name", ResourceType.TwoDa));
        Assert.ThrowsExactly<ArgumentException>(() => new ResourceIdentity("bad\nname", ResourceType.TwoDa));
    }

    [TestMethod]
    public void Resolution_BytesAndProvenanceCollectionsCannotMutateTheResult()
    {
        using var temporary = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(temporary.Path, "loose"));
        File.WriteAllBytes(Path.Combine(temporary.Path, "loose", "same.2da"), [1]);
        using var layer = ResourceLayer.FromLooseDirectory("source", Path.Combine(temporary.Path, "loose"));
        using var resolver = new ResourceResolver([layer]);
        var result = resolver.Resolve(new ResourceIdentity("same", ResourceType.TwoDa))!;
        var bytes = result.Bytes;
        bytes[0] = 9;
        Assert.AreEqual(1, result.Bytes[0]);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<ResourceProvenance>)result.Overridden).Add(
            new ResourceProvenance("x", ResourceLayerKind.LooseDirectory, "x", null, "x")));
    }

    [TestMethod]
    public void ResourceLayers_RejectOversizedLooseErfKeyAndBifReadsBeforeResourceAllocation()
    {
        using var temporary = new TemporaryDirectory();
        var loose = Path.Combine(temporary.Path, "loose");
        var dataRoot = Path.Combine(temporary.Path, "data-root");
        Directory.CreateDirectory(loose);
        Directory.CreateDirectory(Path.Combine(dataRoot, "data"));
        File.WriteAllBytes(Path.Combine(loose, "large.2da"), [1, 2]);
        var looseLayer = ResourceLayer.FromLooseDirectory("loose", loose,
            readOptions: new ResourceLayerReadOptions { MaximumResourceBytes = 1 });
        using var looseResolver = new ResourceResolver([looseLayer]);
        Assert.ThrowsExactly<FormatException>(() => looseResolver.Resolve(new ResourceIdentity("large", ResourceType.TwoDa)));

        var hakPath = Path.Combine(temporary.Path, "large.hak");
        WriteErf(hakPath, [1, 2]);
        using var erfLayer = ResourceLayer.FromErf("hak", hakPath,
            readOptions: new ResourceLayerReadOptions { MaximumResourceBytes = 1 });
        using var erfResolver = new ResourceResolver([erfLayer]);
        Assert.ThrowsExactly<FormatException>(() => erfResolver.Resolve(new ResourceIdentity("same", ResourceType.TwoDa)));

        WriteKeyFixture(temporary.Path, dataRoot,
            [new SyntheticKeyResource("large", (ushort)ResourceType.TwoDa, [1, 2])]);
        var keyPath = Path.Combine(temporary.Path, "test.key");
        Assert.ThrowsExactly<FormatException>(() => ResourceLayer.FromKeyBif("base", keyPath, dataRoot,
            readOptions: new ResourceLayerReadOptions { MaximumKeyFileBytes = 1 }));
        using (var limitedBif = ResourceLayer.FromKeyBif("base", keyPath, dataRoot,
                   readOptions: new ResourceLayerReadOptions { MaximumBifFileBytes = 1 }))
        using (var limitedBifResolver = new ResourceResolver([limitedBif]))
            Assert.ThrowsExactly<FormatException>(() => limitedBifResolver.Resolve(new ResourceIdentity("large", ResourceType.TwoDa)));
        using var keyLayer = ResourceLayer.FromKeyBif("base", keyPath, dataRoot,
            readOptions: new ResourceLayerReadOptions { MaximumResourceBytes = 1 });
        using var keyResolver = new ResourceResolver([keyLayer]);
        Assert.ThrowsExactly<FormatException>(() => keyResolver.Resolve(new ResourceIdentity("large", ResourceType.TwoDa)));
        using var limitedCache = ResourceLayer.FromKeyBif("base", keyPath, dataRoot,
            readOptions: new ResourceLayerReadOptions { MaximumCachedBifBytes = 1 });
        using var limitedCacheResolver = new ResourceResolver([limitedCache]);
        Assert.ThrowsExactly<FormatException>(() => limitedCacheResolver.Resolve(new ResourceIdentity("large", ResourceType.TwoDa)));
    }

    [TestMethod]
    public void KeyBifLayer_DoesNotChargeMalformedBifMetadataBeforeSuccessfulCache()
    {
        using var temporary = new TemporaryDirectory();
        var dataRoot = Path.Combine(temporary.Path, "data-root");
        Directory.CreateDirectory(Path.Combine(dataRoot, "data"));
        var resource = new SyntheticKeyResource("small", (ushort)ResourceType.TwoDa, [7]);
        WriteKeyFixture(temporary.Path, dataRoot, [resource]);
        var bifPath = Path.Combine(dataRoot, "data", "test.bif");
        var validBif = SyntheticKeyBif.BuildBif([resource]);
        Assert.IsTrue(validBif.Length > 4);
        File.WriteAllBytes(bifPath, Enumerable.Repeat((byte)0x41, validBif.Length).ToArray());
        using var layer = ResourceLayer.FromKeyBif("base", Path.Combine(temporary.Path, "test.key"), dataRoot,
            readOptions: new ResourceLayerReadOptions { MaximumCachedBifBytes = 64 });
        using var resolver = new ResourceResolver([layer]);

        Assert.ThrowsExactly<FormatException>(() => resolver.Resolve(new ResourceIdentity("small", ResourceType.TwoDa)));
        File.WriteAllBytes(bifPath, validBif);
        var resolved = resolver.Resolve(new ResourceIdentity("small", ResourceType.TwoDa));

        Assert.IsNotNull(resolved);
        CollectionAssert.AreEqual(new byte[] { 7 }, resolved.Bytes);
    }

    [TestMethod]
    public void ErfLayer_SerializesConcurrentReadsFromItsSharedArchiveStream()
    {
        using var temporary = new TemporaryDirectory();
        var archivePath = Path.Combine(temporary.Path, "parallel.hak");
        var firstBytes = Enumerable.Repeat((byte)0x31, 256 * 1024).ToArray();
        var secondBytes = Enumerable.Repeat((byte)0xC7, 192 * 1024).ToArray();
        using (var output = File.Create(archivePath))
            ErfWriter.Write(output, ErfContainerKind.Hak,
            [
                ErfResourceSource.FromBytes(Resref.Parse("first"), ResourceType.TwoDa, firstBytes),
                ErfResourceSource.FromBytes(Resref.Parse("second"), ResourceType.TwoDa, secondBytes)
            ], ErfBuildDate.ContentEpoch);
        using var layer = ResourceLayer.FromErf("parallel", archivePath);
        using var resolver = new ResourceResolver([layer]);

        Parallel.For(0, 64, index =>
        {
            var expected = index % 2 == 0 ? firstBytes : secondBytes;
            var identity = new ResourceIdentity(index % 2 == 0 ? "first" : "second", ResourceType.TwoDa);
            var result = resolver.Resolve(identity);
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(expected, result.Bytes);
        });
    }

    private static void WriteErf(string path, byte[] bytes)
    {
        using var stream = File.Create(path);
        ErfWriter.Write(stream, ErfContainerKind.Hak,
            [ErfResourceSource.FromBytes(Resref.Parse("same"), ResourceType.TwoDa, bytes)], ErfBuildDate.ContentEpoch);
    }

    private static void WriteKeyFixture(string root, string dataRoot, IReadOnlyList<SyntheticKeyResource> resources)
    {
        const string bifName = "data/test.bif";
        var bifPath = Path.Combine(dataRoot, bifName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(bifPath)!);
        File.WriteAllBytes(bifPath, SyntheticKeyBif.BuildBif(resources));
        File.WriteAllBytes(Path.Combine(root, "test.key"), SyntheticKeyBif.BuildKey(bifName, resources));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
