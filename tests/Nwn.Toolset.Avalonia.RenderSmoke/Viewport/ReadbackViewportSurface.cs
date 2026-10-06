using Avalonia.OpenGL;
using System.Text;
using Nwn.Formats.Mdl;
using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;
using Nwn.Toolset.Avalonia.Viewport;
using Silk.NET.OpenGL;

namespace Nwn.Toolset.Avalonia.RenderSmoke.Viewport;

internal sealed class ReadbackViewportSurface : ModelViewportSurface
{
    private bool _completed;
    private int _successfulFrames;
    private int _initialGeometryUploads;
    private int _initialTextureUploads;

    public ReadbackViewportSurface()
    {
        var source = "newmodel quad\nbeginmodelgeom quad\nnode trimesh surface\nparent NULL\n" +
                     "bitmap asymmetric\nmaterialname material_surface\nverts 4\n-1 -1 0\n1 -1 0\n1 1 0\n-1 1 0\n" +
                     "normals 4\n0 0 1\n0 0 1\n0 0 1\n0 0 1\n" +
                     "tverts 4\n0 0 0\n1 0 0\n1 1 0\n0 1 0\n" +
                     "faces 2\n0 1 2 0 0 1 2 0\n0 2 3 0 0 2 3 0\nendnode\nendmodelgeom\n";
        SetScene(MdlScenePreparer.Prepare(MdlAsciiReader.Read(Encoding.ASCII.GetBytes(source))));
        SetTextures(new Dictionary<string, RgbaImage>(StringComparer.OrdinalIgnoreCase)
        {
            ["asymmetric"] = new RgbaImage(1, 1, [255, 0, 255, 255]),
            ["material_surface"] = new RgbaImage(2, 2,
            [
                255, 0, 0, 255, 0, 255, 0, 255,
                0, 0, 255, 255, 255, 255, 0, 255
            ])
        });
        Camera.Orbit(-87.5f, 157f);
    }

    public event Action<bool, string>? RenderCheckCompleted;

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        base.OnOpenGlRender(gl, fb);
        if (_completed)
            return;

        var api = GL.GetApi(gl.GetProcAddress);
        var viewport = stackalloc int[4];
        api.GetInteger(GetPName.Viewport, viewport);
        var width = viewport[2];
        var height = viewport[3];
        if (width < 80 || height < 80 || (long)width * height > 4_000_000)
        {
            Complete(false, $"Unexpected drawable framebuffer size: {width}x{height}.");
            return;
        }

        var pixels = new byte[checked(width * height * 4)];
        fixed (byte* data = pixels)
            api.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, data);

        var sums = new long[4, 2];
        var counts = new int[4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = checked((y * width + x) * 4);
                var red = pixels[offset];
                var green = pixels[offset + 1];
                var blue = pixels[offset + 2];
                var region = Classify(red, green, blue);
                if (region < 0)
                    continue;
                counts[region]++;
                sums[region, 0] += x;
                sums[region, 1] += y;
            }
        }

        if (counts.Any(count => count < 100))
        {
            Complete(false, $"Framebuffer {width}x{height} did not contain all four expected colors; pixel counts: {string.Join(',', counts)}. Context: {ContextDescription}.");
            return;
        }
        var redCenter = Center(0);
        var greenCenter = Center(1);
        var blueCenter = Center(2);
        var yellowCenter = Center(3);
        if (!(redCenter.X < greenCenter.X && blueCenter.X < yellowCenter.X && redCenter.Y > blueCenter.Y && greenCenter.Y > yellowCenter.Y))
        {
            Complete(false, $"Texture quadrants rendered with incorrect UV orientation at {width}x{height}: red {redCenter}, green {greenCenter}, blue {blueCenter}, yellow {yellowCenter}. Context: {ContextDescription}.");
            return;
        }

        if (_successfulFrames++ == 0)
        {
            _initialGeometryUploads = GeometryUploadCount;
            _initialTextureUploads = TextureUploadCount;
            RequestNextFrameRendering();
            return;
        }
        if (GeometryUploadCount != _initialGeometryUploads || TextureUploadCount != _initialTextureUploads)
        {
            Complete(false, $"A repeated real-control frame re-uploaded unchanged resources (geometry {_initialGeometryUploads}->{GeometryUploadCount}, textures {_initialTextureUploads}->{TextureUploadCount}).");
            return;
        }
        Complete(true, $"Rendered the real control into a {width}x{height} framebuffer; the explicit material binding wins over the magenta bitmap fallback and asymmetric RGBA texture quadrants are correctly oriented (red {redCenter}, green {greenCenter}, blue {blueCenter}, yellow {yellowCenter}); second frame reused {_initialGeometryUploads} geometry and {_initialTextureUploads} texture uploads; context: {ContextDescription}.");
        return;

        (double X, double Y) Center(int color) => ((double)sums[color, 0] / counts[color], (double)sums[color, 1] / counts[color]);
    }

    private void Complete(bool passed, string message)
    {
        if (_completed)
            return;
        _completed = true;
        RenderCheckCompleted?.Invoke(passed, message);
    }

    private static int Classify(byte red, byte green, byte blue)
    {
        if (red > 120 && red > green + 50 && red > blue + 50)
            return 0;
        if (green > 120 && green > red + 50 && green > blue + 50)
            return 1;
        if (blue > 120 && blue > red + 50 && blue > green + 50)
            return 2;
        if (red > 120 && green > 120 && blue < 80 && Math.Abs(red - green) < 75)
            return 3;
        return -1;
    }
}
