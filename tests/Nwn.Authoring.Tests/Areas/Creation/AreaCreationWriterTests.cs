using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Authoring.Areas.Creation;
using Nwn.Authoring.Documents;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Tests.Areas.Creation;

[TestClass]
public sealed class AreaCreationWriterTests
{
    [TestMethod]
    public void HostPathsAndCodecControlFlatModuleCreationAndCompanionByteCopy()
    {
        var root = Path.Combine(Path.GetTempPath(), "area-creation-flat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var codec = new CountingCodec();
        try
        {
            var templateAre = Path.Combine(root, "area_template.are.json");
            var templateGit = Path.Combine(root, "area_template.git.json");
            var templateGic = Path.Combine(root, "area_template.gic.json");
            var ifo = Path.Combine(root, "module.ifo.json");
            File.WriteAllBytes(templateAre, codec.Encode(new JsonGffDocument("ARE ", new JsonGffStruct())));
            var gitBytes = codec.Encode(new JsonGffDocument("GIT ", new JsonGffStruct()));
            var gicBytes = codec.Encode(new JsonGffDocument("GIC ", new JsonGffStruct()));
            File.WriteAllBytes(templateGit, gitBytes);
            File.WriteAllBytes(templateGic, gicBytes);
            File.WriteAllBytes(ifo, codec.Encode(new JsonGffDocument("IFO ", new JsonGffStruct())));
            var paths = new AreaCreationPaths(root, "area_template", templateAre, templateGit, templateGic,
                Path.Combine(root, "newarea.are.json"), Path.Combine(root, "newarea.git.json"),
                Path.Combine(root, "newarea.gic.json"), ifo, ".host-area-create-");

            var created = AreaCreationWriter.TryCreate(paths, codec, ResolveTileset, "newarea", "New Area",
                "synthetic", 2, 1, populate: null, out var error);

            Assert.IsTrue(created, error);
            CollectionAssert.AreEqual(gitBytes, File.ReadAllBytes(paths.DestinationGitPath));
            CollectionAssert.AreEqual(gicBytes, File.ReadAllBytes(paths.DestinationGicPath));
            Assert.IsTrue(new AreDocument(codec.Decode(File.ReadAllBytes(paths.DestinationArePath))).Tiles.Count == 2);
            Assert.IsTrue(new IfoDocument(codec.Decode(File.ReadAllBytes(ifo))).AreaResRefs.Contains("newarea"));
            Assert.IsTrue(codec.DecodeCount > 0 && codec.EncodeCount > 4,
                "the host codec must own every document parse and serialization");
            Assert.IsFalse(File.Exists(paths.PendingMarkerPath("newarea")));

            File.WriteAllBytes(templateGit, codec.Encode(new JsonGffDocument("ARE ", new JsonGffStruct())));
            var wrongTypePaths = paths with
            {
                DestinationArePath = Path.Combine(root, "badtype.are.json"),
                DestinationGitPath = Path.Combine(root, "badtype.git.json"),
                DestinationGicPath = Path.Combine(root, "badtype.gic.json")
            };
            Assert.IsFalse(AreaCreationWriter.TryCreate(wrongTypePaths, codec, ResolveTileset, "badtype", "Bad Type",
                "synthetic", 1, 1, populate: null, out var typeError));
            StringAssert.Contains(typeError, "expected 'GIT '");
            Assert.IsFalse(File.Exists(wrongTypePaths.DestinationArePath));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void InvalidAliasesAndTraversalPrefixesAreRejectedBeforeAnyWrite()
    {
        var root = Path.Combine(Path.GetTempPath(), "area-paths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var template = Path.Combine(root, "template.are.json");
            var destinations = new AreaCreationPaths(root, "template", template,
                Path.Combine(root, "template.git.json"), Path.Combine(root, "template.gic.json"),
                Path.Combine(root, "new.are.json"), Path.Combine(root, "new.git.json"),
                Path.Combine(root, "new.gic.json"), Path.Combine(root, "module.ifo.json"), ".pending-");
            var alias = destinations with { DestinationArePath = template };
            Assert.IsFalse(AreaCreationWriter.TryCreate(alias, new NimGffDocumentCodec(), ResolveTileset,
                "new", "New", "synthetic", 1, 1, null, out var aliasError));
            StringAssert.Contains(aliasError, "alias");
            var traversal = destinations with { PendingMarkerPrefix = "../outside-" };
            Assert.IsFalse(AreaCreationWriter.TryCreate(traversal, new NimGffDocumentCodec(), ResolveTileset,
                "new", "New", "synthetic", 1, 1, null, out var traversalError));
            StringAssert.Contains(traversalError, "filename prefix");
            var markerAlias = destinations with
            {
                PendingMarkerPrefix = ".pending-",
                DestinationArePath = Path.Combine(root, ".pending-new.pending")
            };
            Assert.IsFalse(AreaCreationWriter.TryCreate(markerAlias, new NimGffDocumentCodec(), ResolveTileset,
                "new", "New", "synthetic", 1, 1, null, out var markerAliasError));
            StringAssert.Contains(markerAliasError, "alias");
            Assert.AreEqual(0, Directory.GetFiles(root).Length);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static bool ResolveTileset(string _, out TilesetDefinition tileset)
    {
        tileset = new TilesetDefinition
        {
            Floor = "Ground",
            Terrains = new[] { new TerrainDefinition("Ground", null, null) },
            Tiles = new[]
            {
                new TileDefinition
                {
                    Model = "synthetic",
                    TopLeft = "Ground",
                    TopRight = "Ground",
                    BottomLeft = "Ground",
                    BottomRight = "Ground"
                }
            }
        };
        return true;
    }

    private sealed class CountingCodec : IDocumentCodec<JsonGffDocument>
    {
        private readonly NimGffDocumentCodec _inner = new();
        public int DecodeCount { get; private set; }
        public int EncodeCount { get; private set; }
        public JsonGffDocument Decode(ReadOnlyMemory<byte> bytes)
        {
            DecodeCount++;
            return _inner.Decode(bytes);
        }
        public byte[] Encode(JsonGffDocument document)
        {
            EncodeCount++;
            return _inner.Encode(document);
        }
    }
}
