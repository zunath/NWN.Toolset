using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nwn.Preview.Areas;
using Nwn.Preview.Scene;
using Nwn.Preview.Thumbnails;
using System.Numerics;

namespace Nwn.Preview.Tests.Thumbnails;

[TestClass]
public sealed class ThumbnailRendererTests
{
    private const int Size = 32;

    private static RenderModel Box(float sizeX = 1f, float sizeY = 1f, float sizeZ = 1f) => new()
    {
        Name = "test",
        Meshes = [new RenderMesh
        {
            NodeName = "quad", TextureName = string.Empty,
            Positions = [0f, 0f, 0f, sizeX, 0f, 0f, sizeX, sizeY, sizeZ, 0f, sizeY, sizeZ],
            Normals = [], TexCoords = [], Indices = [0, 1, 2, 0, 2, 3], Transform = Matrix4x4.Identity
        }]
    };

    private static RenderModel TexturedQuad(string textureName, Vector3? diffuse = null, string nodeName = "quad") => new()
    {
        Name = "textured",
        Meshes = [new RenderMesh
        {
            NodeName = nodeName, TextureName = textureName, DiffuseColor = diffuse ?? Vector3.One,
            Positions = [0f, 0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f, 0f, 1f, 1f],
            Normals = [], TexCoords = [0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f],
            Indices = [0, 1, 2, 0, 2, 3], Transform = Matrix4x4.Identity
        }]
    };

    private static ThumbnailTexture SolidTexture(byte r, byte g, byte b, byte a = 255, int size = 4,
        byte alphaCutoff = ThumbnailTexture.DefaultAlphaCutoff)
    {
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;
        }
        return new ThumbnailTexture(size, size, pixels, alphaCutoff);
    }

    private static int OpaquePixels(byte[] pixels)
    {
        var count = 0;
        for (var i = 3; i < pixels.Length; i += ThumbnailRenderer.BytesPerPixel)
            if (pixels[i] != 0) count++;
        return count;
    }

    private static (byte B, byte G, byte R) Brightest(byte[] pixels)
    {
        (byte B, byte G, byte R) best = (0, 0, 0);
        var bestSum = -1;
        for (var i = 0; i < pixels.Length; i += ThumbnailRenderer.BytesPerPixel)
        {
            if (pixels[i + 3] == 0) continue;
            var sum = pixels[i] + pixels[i + 1] + pixels[i + 2];
            if (sum > bestSum)
            {
                bestSum = sum;
                best = (pixels[i], pixels[i + 1], pixels[i + 2]);
            }
        }
        return best;
    }

    [TestMethod]
    public void Renders_A_Buffer_Of_The_Requested_Size()
    {
        var pixels = ThumbnailRenderer.Render(Box(), Size);
        Assert.IsNotNull(pixels);
        Assert.AreEqual(Size * Size * ThumbnailRenderer.BytesPerPixel, pixels.Length);
    }

    [TestMethod]
    public void Draws_Something_For_A_Model_With_Geometry() =>
        Assert.IsTrue(OpaquePixels(ThumbnailRenderer.Render(Box(), Size)!) > Size,
            "a quad facing the camera should cover a meaningful part of the tile");

    [TestMethod]
    public void Background_Is_Transparent_So_Tiles_Keep_Their_Own_Surface()
    {
        var pixels = ThumbnailRenderer.Render(Box(0.2f, 0.2f, 0.2f), Size)!;
        Assert.AreEqual(0, pixels[3]);
    }

    [TestMethod]
    public void A_Model_With_No_Triangles_Renders_Nothing()
    {
        var empty = new RenderModel
        {
            Name = "empty",
            Meshes = [new RenderMesh { NodeName = "none", TextureName = string.Empty,
            Positions = [], Normals = [], TexCoords = [], Indices = [], Transform = Matrix4x4.Identity }]
        };
        Assert.IsNull(ThumbnailRenderer.Render(empty, Size));
    }

    [TestMethod]
    public void A_Transition_Model_With_No_Triangles_Renders_The_Doorway_Fallback()
    {
        var pixels = ThumbnailRenderer.Render(new RenderModel { Name = "transition", IsDoorTransitionGeometry = true }, Size);
        Assert.IsNotNull(pixels);
        Assert.IsTrue(OpaquePixels(pixels) > 0);
    }

    [TestMethod]
    public void Transition_Metadata_Without_A_Model_Renders_The_Doorway_Fallback()
    {
        var pixels = ThumbnailRenderer.Render(null, Size, renderDoorTransitionFallback: true);
        Assert.IsNotNull(pixels);
        Assert.IsTrue(OpaquePixels(pixels) > 0);
    }

    [TestMethod]
    public void A_Transition_Model_With_Unprojectable_Triangles_Renders_The_Doorway_Fallback()
    {
        var box = Box(0f, 0f, 0f);
        var pixels = ThumbnailRenderer.Render(new RenderModel { Name = box.Name, Meshes = box.Meshes, IsDoorTransitionGeometry = true }, Size);
        Assert.IsNotNull(pixels);
        Assert.IsTrue(OpaquePixels(pixels) > 0);
    }

    [TestMethod]
    public void A_Transition_Model_With_Collinear_Triangles_Renders_The_Doorway_Fallback()
    {
        var box = Box(1f, 0f, 0f);
        var pixels = ThumbnailRenderer.Render(new RenderModel { Name = box.Name, Meshes = box.Meshes, IsDoorTransitionGeometry = true }, Size);
        Assert.IsNotNull(pixels);
        Assert.IsTrue(OpaquePixels(pixels) > 0,
            "a nonzero projected span is not enough when every authored triangle has zero area");
    }

    [TestMethod]
    public void A_Null_Model_Renders_Nothing() => Assert.IsNull(ThumbnailRenderer.Render(null, Size));

    [TestMethod]
    public void Degenerate_Geometry_Does_Not_Throw()
    {
        var flat = Box(0f, 0f, 0f);
        _ = ThumbnailRenderer.Render(flat, Size);
    }

    [TestMethod]
    public void Out_Of_Range_Indices_Are_Skipped_Rather_Than_Fatal()
    {
        var model = new RenderModel
        {
            Name = "broken",
            Meshes = [new RenderMesh { NodeName = "broken", TextureName = string.Empty,
            Positions = [0f,0f,0f, 1f,0f,0f, 1f,1f,0f], Normals = [], TexCoords = [], Indices = [0,1,2, 0,1,99], Transform = Matrix4x4.Identity }]
        };
        var pixels = ThumbnailRenderer.Render(model, Size);
        Assert.IsNotNull(pixels);
    }

    [TestMethod]
    public void Framing_Is_Uniform_So_A_Long_Model_Is_Not_Stretched()
    {
        var wide = ThumbnailRenderer.Render(Box(8f, 1f, 1f), Size)!;
        var square = ThumbnailRenderer.Render(Box(1f, 1f, 1f), Size)!;
        Assert.IsTrue(OpaquePixels(wide) < OpaquePixels(square), "a long thin model should letterbox inside the tile, not fill it");
    }

    [TestMethod]
    public void A_Textured_Mesh_Takes_Its_Colour_From_The_Texture()
    {
        var rgb = Brightest(ThumbnailRenderer.Render(TexturedQuad("wall"), Size, resolveTexture: _ => SolidTexture(200, 30, 30))!);
        Assert.IsTrue(rgb.R > rgb.B, "the quad should read as the texture's red, not the palette's blue");
    }

    [TestMethod]
    public void A_Mesh_Takes_Its_Colour_From_Its_Diffuse_When_The_Texture_Is_White()
    {
        var cyan = Brightest(ThumbnailRenderer.Render(TexturedQuad("tcn01_white", new Vector3(0f, .5f, .5f)), Size, resolveTexture: _ => SolidTexture(255, 255, 255))!);
        var orange = Brightest(ThumbnailRenderer.Render(TexturedQuad("tcn01_white", new Vector3(.7f, .35f, 0f)), Size, resolveTexture: _ => SolidTexture(255, 255, 255))!);
        Assert.IsTrue(cyan.B > cyan.R);
        Assert.IsTrue(cyan.G > cyan.R);
        Assert.IsTrue(orange.R > orange.B);
    }

    [TestMethod]
    public void An_Unstated_Diffuse_Leaves_A_Texture_Exactly_As_It_Is()
    {
        var rgb = Brightest(ThumbnailRenderer.Render(TexturedQuad("wall"), Size, resolveTexture: _ => SolidTexture(200, 200, 200))!);
        Assert.AreEqual(rgb.R, rgb.G);
        Assert.AreEqual(rgb.G, rgb.B);
    }

    [TestMethod]
    public void A_Mesh_Whose_Texture_Does_Not_Resolve_Still_Renders_Flat()
    {
        var pixels = ThumbnailRenderer.Render(TexturedQuad("missing"), Size, resolveTexture: _ => null)!;
        Assert.IsTrue(OpaquePixels(pixels) > Size, "an unresolvable texture must not blank the model out");
    }

    [TestMethod]
    public void A_Mesh_With_No_Texture_Name_Is_Not_Looked_Up_At_All()
    {
        var lookups = 0;
        _ = ThumbnailRenderer.Render(Box(), Size, resolveTexture: _ =>
            {
                lookups++;
                return null;
            });
        Assert.AreEqual(0, lookups);
    }

    [TestMethod]
    public void A_Texture_Is_Only_Resolved_Once_Per_Render()
    {
        var model = new RenderModel { Name = "twoMeshes", Meshes = [TexturedQuad("shared").Meshes[0], TexturedQuad("shared").Meshes[0]] };
        var lookups = 0;
        _ = ThumbnailRenderer.Render(model, Size, resolveTexture: _ =>
            {
                lookups++;
                return SolidTexture(10, 10, 10);
            });
        Assert.AreEqual(1, lookups);
    }

    [TestMethod]
    public void TheSameTextureWithDifferentEquipmentPalettesIsResolvedPerMesh()
    {
        var first = TexturedQuad("shared_plt").Meshes[0];
        first.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
        var second = TexturedQuad("shared_plt").Meshes[0];
        second.LayerColorIndices = new Dictionary<int, int> { [2] = 97 };
        var seen = new List<int>();
        _ = ThumbnailRenderer.Render(new RenderModel { Name = "independent-dyes", Meshes = [first, second] }, Size,
            resolveLayeredTexture: (_, colors) =>
            {
                seen.Add(colors![2]);
                return SolidTexture(10, 10, 10);
            });
        CollectionAssert.AreEqual(new[] { 45, 97 }, seen);
    }

    [TestMethod]
    public void APlainTextureResolverIgnoresUnusedMeshPalettesInItsCacheKey()
    {
        var first = TexturedQuad("shared").Meshes[0];
        first.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
        var second = TexturedQuad("shared").Meshes[0];
        second.LayerColorIndices = new Dictionary<int, int> { [2] = 97 };
        var calls = 0;
        _ = ThumbnailRenderer.Render(new RenderModel { Name = "plain", Meshes = [first, second] }, Size,
            resolveTexture: _ =>
            {
                calls++;
                return SolidTexture(10, 10, 10);
            });
        Assert.AreEqual(1, calls, "the plain resolver cannot use layer colors and should share one decoded texture");
    }

    [TestMethod]
    public void APerMeshResolverCachesDistinctDyeTintOwnershipAndArmorPartStateSeparately()
    {
        var first = TexturedQuad("shared_mtr", nodeName: "first").Meshes[0];
        first.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
        first.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 123 };
        var otherPart = TexturedQuad("shared_mtr", nodeName: "otherPart").Meshes[0];
        otherPart.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
        otherPart.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 123 };
        var itemOwned = TexturedQuad("shared_mtr", nodeName: "itemOwned").Meshes[0];
        itemOwned.LayerColorIndices = new Dictionary<int, int> { [2] = 45 };
        itemOwned.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 123 };
        itemOwned.UsesItemTintOverrides = true;
        var second = TexturedQuad("shared_mtr", nodeName: "second").Meshes[0];
        second.LayerColorIndices = new Dictionary<int, int> { [2] = 97 };
        second.TintMapOverrides = new Dictionary<string, int> { ["TM_shared_mtr_2"] = 456 };
        var seen = new List<(int Palette, int Tint)>();
        _ = ThumbnailRenderer.Render(new RenderModel { Name = "custom", Meshes = [first, otherPart, itemOwned, second] }, Size,
            resolveMeshTexture: mesh =>
            {
                seen.Add((mesh.LayerColorIndices[2], mesh.TintMapOverrides["TM_shared_mtr_2"]));
                return SolidTexture(10, 10, 10);
            },
            resolveCacheVariant: mesh => new ThumbnailTextureCacheVariant(mesh.NodeName));
        CollectionAssert.AreEqual(new[] { (45, 123), (45, 123), (45, 123), (97, 456) }, seen);
    }

    [TestMethod]
    public void Fully_Transparent_Texels_Are_Cut_Out_Rather_Than_Drawn()
    {
        var opaque = ThumbnailRenderer.Render(TexturedQuad("leaf"), Size, resolveTexture: _ => SolidTexture(9, 9, 9))!;
        var cutOut = ThumbnailRenderer.Render(TexturedQuad("leaf"), Size, resolveTexture: _ => SolidTexture(9, 9, 9, a: 0))!;
        Assert.AreEqual(0, OpaquePixels(cutOut));
        Assert.IsTrue(OpaquePixels(opaque) > 0);
    }

    [TestMethod]
    public void TextureSpecificAlphaCutoffControlsSoftwarePreviewCutout()
    {
        var runtime = ThumbnailRenderer.Render(TexturedQuad("leaf"), Size, resolveTexture: _ => SolidTexture(9, 9, 9, a: 80, alphaCutoff: 77))!;
        var legacy = ThumbnailRenderer.Render(TexturedQuad("leaf"), Size, resolveTexture: _ => SolidTexture(9, 9, 9, a: 80))!;
        Assert.IsTrue(OpaquePixels(runtime) > 0, "texture9 alpha 80 survives cutoff 77");
        Assert.AreEqual(0, OpaquePixels(legacy), "ordinary preview textures retain cutoff 96");
    }

    [TestMethod]
    public void A_Texture_Shorter_Than_Its_Declared_Size_Is_Ignored_Rather_Than_Fatal()
    {
        var truncated = new ThumbnailTexture(64, 64, new byte[16]);
        var pixels = ThumbnailRenderer.Render(TexturedQuad("bad"), Size, resolveTexture: _ => truncated);
        Assert.IsNotNull(pixels);
        Assert.IsTrue(OpaquePixels(pixels) > 0, "it falls back to the flat tone");
    }

    [TestMethod]
    public void A_Throwing_Texture_Resolver_Does_Not_Take_The_Render_Down()
    {
        var pixels = ThumbnailRenderer.Render(TexturedQuad("boom"), Size, resolveTexture: _ => throw new InvalidOperationException("resource layer exploded"));
        Assert.IsNotNull(pixels);
        Assert.IsTrue(OpaquePixels(pixels) > 0);
    }

    [TestMethod]
    public void A_Mesh_With_No_Uvs_Falls_Back_To_Flat_Shading()
    {
        var noUvs = new RenderModel
        {
            Name = "noUvs",
            Meshes = [new RenderMesh { NodeName="quad", TextureName="wall",
            Positions=[0f,0f,0f, 1f,0f,0f, 1f,1f,1f, 0f,1f,1f], Normals=[], TexCoords=[], Indices=[0,1,2,0,2,3], Transform=Matrix4x4.Identity }]
        };
        var pixels = ThumbnailRenderer.Render(noUvs, Size, resolveTexture: _ => SolidTexture(200, 30, 30))!;
        var brightest = Brightest(pixels);
        Assert.IsTrue(brightest.B > brightest.R, "without UVs the palette's blue is the only honest answer");
    }

    [TestMethod]
    public void UsesSharedDoorwayFallbackForTransitionWithoutDrawableGeometry()
    {
        var pixels = ThumbnailRenderer.Render(new RenderModel { Name = "transition", IsDoorTransitionGeometry = true }, Size);
        Assert.IsNotNull(pixels);
        Assert.IsTrue(OpaquePixels(pixels) > 0);
    }

    [TestMethod]
    public void ResolvesOneCaseInsensitiveTextureOnlyOncePerRender()
    {
        var model = new RenderModel { Name = "case", Meshes = [TexturedQuad("surface").Meshes[0], TexturedQuad("SURFACE").Meshes[0]] };
        var calls = 0;
        var pixels = ThumbnailRenderer.Render(model, Size, resolveTexture: _ =>
            {
                calls++;
                return SolidTexture(24, 180, 55);
            });
        Assert.IsNotNull(pixels);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void IncludesTypedHostVariationInPerMeshTextureCacheKey()
    {
        var model = new RenderModel { Name = "variant", Meshes = [TexturedQuad("surface").Meshes[0], TexturedQuad("surface").Meshes[0]] };
        var calls = 0;
        var pixels = ThumbnailRenderer.Render(model, Size, resolveMeshTexture: mesh =>
            {
                calls++;
                return SolidTexture(70, 130, 210);
            },
            resolveCacheVariant: mesh => new ThumbnailTextureCacheVariant(mesh.NodeName + calls));
        Assert.IsNotNull(pixels);
        Assert.AreEqual(2, calls);
    }
}