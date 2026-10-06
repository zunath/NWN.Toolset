using System.Security.Cryptography;
using System.Text.Json;
using Nwn.Authoring.Documents;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using Nwn.Authoring.Areas.Tiles;
using Nwn.Formats.Io;
using Nwn.Formats.Resources;
using Nwn.Formats.Tilesets;

namespace Nwn.Authoring.Areas.Creation;

/// <summary>Creates and registers a new area using paths and JSON-GFF representation supplied by its host.</summary>
public static class AreaCreationWriter
{
    public const int MaximumDimension = 32;

    public static bool TryCreate(
        AreaCreationPaths paths,
        IDocumentCodec<JsonGffDocument> codec,
        AreaTilesetResolver? resolveTileset,
        string resRef,
        string displayName,
        string tilesetResRef,
        int width,
        int height,
        AreaDocumentPopulator? populate,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(codec);
        error = string.Empty;
        resRef = (resRef ?? string.Empty).Trim().ToLowerInvariant();
        if (!ResourceReferenceRules.IsCanonical(resRef))
        {
            error = $"ResRef must be 1-{ResourceReferenceRules.MaxLength} characters, lowercase letters/digits/underscore only.";
            return false;
        }
        if (width is < 1 or > MaximumDimension || height is < 1 or > MaximumDimension)
        {
            error = $"Width and height must each be between 1 and {MaximumDimension}.";
            return false;
        }
        try { ValidatePaths(paths, resRef); }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }

        try
        {
            using var moduleLease = ModuleWriteLock.Acquire(paths.ModuleRoot);
            var arePath = paths.DestinationArePath;
            var gitPath = paths.DestinationGitPath;
            var gicPath = paths.DestinationGicPath;
            var markerPath = paths.PendingMarkerPath(resRef);
            if (!File.Exists(markerPath))
            {
                var existing = new[] { arePath, gitPath, gicPath }.FirstOrDefault(File.Exists);
                if (existing != null)
                    throw new IOException($"An area named '{resRef}' already exists ({Path.GetFileName(existing)} is present).");
            }

            if (resolveTileset == null || !resolveTileset(tilesetResRef, out var tileset))
                throw new InvalidDataException($"Tileset '{tilesetResRef}' could not be resolved.");
            var terrain = TilePainter.DefaultFillTerrain(tileset);
            if (terrain == null || TilePainter.FindSolidTile(tileset, terrain) is not { } fill)
                throw new InvalidDataException($"Tileset '{tilesetResRef}' has no solid tile for terrain '{terrain ?? "(none)"}'.");

            if (!File.Exists(paths.TemplateArePath) || !File.Exists(paths.TemplateGitPath) || !File.Exists(paths.TemplateGicPath))
                throw new FileNotFoundException($"The '{paths.TemplateResRef}' template triplet is missing from the module.");
            if (!File.Exists(paths.ModuleIfoPath))
                throw new FileNotFoundException("module.ifo document was not found; cannot register the new area.");

            using var ifoLease = ModuleIfoUpdateLock.Acquire(paths.ModuleRoot);
            RecoverPending(paths, codec, resRef, markerPath);
            var occupied = new[] { arePath, gitPath, gicPath }.FirstOrDefault(File.Exists);
            if (occupied != null)
                throw new IOException($"An area named '{resRef}' already exists ({Path.GetFileName(occupied)} is present).");

            var ifoBaseline = File.ReadAllBytes(paths.ModuleIfoPath);
            var templateAreBytes = File.ReadAllBytes(paths.TemplateArePath);
            var templateGitBytes = File.ReadAllBytes(paths.TemplateGitPath);
            var templateGicBytes = File.ReadAllBytes(paths.TemplateGicPath);
            var are = new AreDocument(DecodeExpected(codec, templateAreBytes, "ARE ", paths.TemplateArePath));
            var git = (GitDocument?)null;
            var gic = (GicDocument?)null;
            _ = DecodeExpected(codec, templateGitBytes, "GIT ", paths.TemplateGitPath);
            _ = DecodeExpected(codec, templateGicBytes, "GIC ", paths.TemplateGicPath);
            using (EditScope.EnterConstruction())
            {
                AreaTemplateFactory.PopulateNewArea(are, resRef, displayName, tilesetResRef,
                    width, height, fill.TileId, fill.Orientation);
                if (populate != null)
                {
                    git = new GitDocument(DecodeExpected(codec, templateGitBytes, "GIT ", paths.TemplateGitPath));
                    gic = new GicDocument(DecodeExpected(codec, templateGicBytes, "GIC ", paths.TemplateGicPath));
                    populate(are, git, gic);
                }
            }

            var ifo = new IfoDocument(DecodeExpected(codec, ifoBaseline, "IFO ", paths.ModuleIfoPath));
            using (EditScope.EnterConstruction())
                AreaTemplateFactory.AddAreaToModule(ifo, resRef);
            var areBytes = codec.Encode(are.Document);
            var gitBytes = git == null ? templateGitBytes : codec.Encode(git.Document);
            var gicBytes = gic == null ? templateGicBytes : codec.Encode(gic.Document);
            var manifest = new PendingAreaCreationManifest
            {
                ResRef = resRef,
                Are = Fingerprint(areBytes),
                Git = Fingerprint(gitBytes),
                Gic = Fingerprint(gicBytes)
            };
            WriteAtomic(markerPath, JsonSerializer.SerializeToUtf8Bytes(manifest), overwrite: true);
            var created = new List<string>();
            try
            {
                WriteAtomic(arePath, areBytes, overwrite: false);
                created.Add(arePath);
                WriteAtomic(gitPath, gitBytes, overwrite: false);
                created.Add(gitPath);
                WriteAtomic(gicPath, gicBytes, overwrite: false);
                created.Add(gicPath);
                if (!File.ReadAllBytes(paths.ModuleIfoPath).AsSpan().SequenceEqual(ifoBaseline))
                    throw new IOException("module IFO changed while the area was being created. Try again.");
                WriteAtomic(paths.ModuleIfoPath, codec.Encode(ifo.Document), overwrite: true);
                TryDelete(markerPath);
                return true;
            }
            catch
            {
                var entries = new[] { (arePath, manifest.Are), (gitPath, manifest.Git), (gicPath, manifest.Gic) };
                var cleanupComplete = true;
                try
                {
                    foreach (var (path, expected) in entries.Where(entry => created.Contains(entry.Item1, StringComparer.OrdinalIgnoreCase)))
                    {
                        if (File.Exists(path) && Fingerprint(File.ReadAllBytes(path)) != expected)
                            throw new IOException($"'{Path.GetFileName(path)}' changed during recovery; the newer file was preserved.");
                    }
                    foreach (var path in created.AsEnumerable().Reverse())
                        File.Delete(path);
                }
                catch { cleanupComplete = false; }
                if (cleanupComplete)
                    TryDelete(markerPath);
                throw;
            }
        }
        catch (Exception exception)
        {
            error = $"Failed to create area '{resRef}': {exception.Message}";
            return false;
        }
    }

    private static void RecoverPending(AreaCreationPaths paths, IDocumentCodec<JsonGffDocument> codec, string resRef, string markerPath)
    {
        if (!File.Exists(markerPath)) return;
        var ifoBytes = File.ReadAllBytes(paths.ModuleIfoPath);
        var ifo = new IfoDocument(DecodeExpected(codec, ifoBytes, "IFO ", paths.ModuleIfoPath));
        var registered = ifo.AreaResRefs.Contains(resRef, StringComparer.OrdinalIgnoreCase);
        if (!registered)
        {
            var manifest = JsonSerializer.Deserialize<PendingAreaCreationManifest>(File.ReadAllBytes(markerPath));
            if (manifest == null || !string.Equals(manifest.ResRef, resRef, StringComparison.OrdinalIgnoreCase) ||
                !ValidFingerprint(manifest.Are) || !ValidFingerprint(manifest.Git) || !ValidFingerprint(manifest.Gic))
                throw new InvalidDataException($"The pending marker '{Path.GetFileName(markerPath)}' is incomplete.");
            var destinations = new[]
            {
                (paths.DestinationArePath, manifest.Are),
                (paths.DestinationGitPath, manifest.Git),
                (paths.DestinationGicPath, manifest.Gic)
            };
            foreach (var (path, expected) in destinations)
            {
                if (!File.Exists(path)) continue;
                if (Fingerprint(File.ReadAllBytes(path)) != expected)
                    throw new IOException($"'{Path.GetFileName(path)}' changed after the interrupted area creation. " +
                        "Recovery was refused so the newer file is preserved.");
            }
            foreach (var (path, _) in destinations)
                if (File.Exists(path)) File.Delete(path);
        }
        File.Delete(markerPath);
    }

    private static AreaFileFingerprint Fingerprint(byte[] bytes) =>
        new(bytes.LongLength, Convert.ToHexString(SHA256.HashData(bytes)));

    private static JsonGffDocument DecodeExpected(
        IDocumentCodec<JsonGffDocument> codec,
        byte[] bytes,
        string expectedType,
        string path)
    {
        var document = codec.Decode(bytes);
        if (!string.Equals(document.DataType, expectedType, StringComparison.Ordinal))
            throw new InvalidDataException($"'{Path.GetFileName(path)}' contains GFF type '{document.DataType}', expected '{expectedType}'.");
        return document;
    }

    private static void ValidatePaths(AreaCreationPaths paths, string resRef)
    {
        if (!Directory.Exists(paths.ModuleRoot))
            throw new DirectoryNotFoundException($"Module root does not exist: {paths.ModuleRoot}");
        if (string.IsNullOrWhiteSpace(paths.PendingMarkerPrefix) ||
            paths.PendingMarkerPrefix.Contains("..", StringComparison.Ordinal) ||
            paths.PendingMarkerPrefix.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, ':', '*' }) >= 0)
            throw new InvalidDataException("The pending-marker prefix must be a filename prefix without traversal characters.");

        var root = Path.GetFullPath(paths.ModuleRoot);
        var templatePaths = new[]
        {
            paths.TemplateArePath, paths.TemplateGitPath, paths.TemplateGicPath
        }.Select(Path.GetFullPath).ToArray();
        var mutationPaths = new[]
        {
            paths.DestinationArePath, paths.DestinationGitPath, paths.DestinationGicPath,
            paths.ModuleIfoPath
        }.Select(Path.GetFullPath).ToArray();
        var marker = Path.GetFullPath(paths.PendingMarkerPath(resRef));
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        if (templatePaths.Concat(mutationPaths).Append(marker).Distinct(comparer).Count() !=
            templatePaths.Length + mutationPaths.Length + 1)
            throw new InvalidDataException("Area creation paths alias one another.");
        foreach (var path in mutationPaths.Append(marker))
        {
            var relative = Path.GetRelativePath(root, path);
            if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException("Area creation paths must remain inside the configured module root.");
        }
    }

    private static bool ValidFingerprint(AreaFileFingerprint? value) => value != null && value.Length >= 0 &&
        value.Sha256.Length == 64 && value.Sha256.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'F');

    private static void WriteAtomic(string path, byte[] bytes, bool overwrite)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { /* A stale marker is recoverable on the next creation attempt. */ }
    }
}
