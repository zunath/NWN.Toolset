using System.Numerics;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Nwn.Preview.Pixels;
using Nwn.Preview.Scene;
using Silk.NET.OpenGL;

namespace Nwn.Toolset.Avalonia.Viewport;

/// <summary>OpenGL surface for immutable prepared scenes and decoded RGBA textures.</summary>
public class ModelViewportSurface : OpenGlControlBase
{
    private const string VertexShaderBody = "layout(location=0) in vec3 p;layout(location=1) in vec2 uv;uniform mat4 mvp;out vec2 t;void main(){t=uv;gl_Position=mvp*vec4(p,1.0);}";
    private const string FragmentShaderBody = "in vec2 t;uniform sampler2D image;uniform int hasTexture;out vec4 color;void main(){color=hasTexture==1?texture(image,t):vec4(0.72,0.75,0.8,1.0);}";
    private const int MaximumCachedMeshes = 256;
    private const int MaximumCachedTextures = 128;
    private const long MaximumCachedTextureBytes = 256L * 1024 * 1024;
    private GL? _gl;
    private uint _program;
    private uint _vao;
    private int _mvpLocation;
    private int _texturedLocation;
    private PreparedScene? _scene;
    private IReadOnlyDictionary<string, RgbaImage> _textures = new Dictionary<string, RgbaImage>(StringComparer.OrdinalIgnoreCase);
    private Vector3 _center;
    private float _radius = 1;
    private readonly Dictionary<PreparedMesh, CachedMesh> _meshCache = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<RgbaImage, CachedTexture> _textureCache = new(ReferenceEqualityComparer.Instance);
    private PreparedScene? _cachedScene;
    private IReadOnlyDictionary<string, RgbaImage>? _cachedTextureSet;
    private long _cacheClock;
    private long _cachedGeometryBytes;
    private long _cachedTextureBytes;
    private long _frameNumber;
    private string _openGlVendor = string.Empty;
    private string _openGlRenderer = string.Empty;
    private string _openGlVersion = string.Empty;
    private ModelViewportRenderObservation? _lastSuccessfulRenderObservation;

    public int GeometryUploadCount { get; private set; }
    public int TextureUploadCount { get; private set; }

    public OrbitCameraState Camera { get; } = new();
    public string? ContextDescription { get; private set; }
    public PreparedScene? Scene => _scene;
    public IReadOnlyDictionary<string, RgbaImage> Textures => _textures;
    public ModelViewportRenderObservation? LastSuccessfulRenderObservation => Volatile.Read(ref _lastSuccessfulRenderObservation);

    public void SetScene(PreparedScene? scene)
    {
        _scene = scene;
        Volatile.Write(ref _lastSuccessfulRenderObservation, null);
        if (scene is not null)
            FitScene(scene);
        RequestNextFrameRendering();
    }

    public void SetTextures(IReadOnlyDictionary<string, RgbaImage> textures)
    {
        _textures = new Dictionary<string, RgbaImage>(textures ?? throw new ArgumentNullException(nameof(textures)), StringComparer.OrdinalIgnoreCase);
        Volatile.Write(ref _lastSuccessfulRenderObservation, null);
        RequestNextFrameRendering();
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        var version = GlVersion;
        var shaderPrefix = version.Type switch
        {
            GlProfileType.OpenGL when version.Major > 3 || version.Major == 3 && version.Minor >= 3 => "#version 330 core\n",
            GlProfileType.OpenGLES when version.Major >= 3 => "#version 300 es\nprecision highp float;\n",
            GlProfileType.OpenGL => throw new NotSupportedException($"The viewport requires desktop OpenGL 3.3 or later; Avalonia supplied {version}."),
            GlProfileType.OpenGLES => throw new NotSupportedException($"The viewport requires OpenGL ES 3.0 or later; Avalonia supplied {version}."),
            _ => throw new NotSupportedException($"The viewport does not recognize Avalonia's supplied GL profile {version}.")
        };
        ContextDescription = $"{version.Type} {version.Major}.{version.Minor}";
        _gl = GL.GetApi(gl.GetProcAddress);
        _openGlVendor = _gl.GetStringS(StringName.Vendor);
        _openGlRenderer = _gl.GetStringS(StringName.Renderer);
        _openGlVersion = _gl.GetStringS(StringName.Version);
        Volatile.Write(ref _lastSuccessfulRenderObservation, null);
        _program = CreateProgram(_gl, shaderPrefix + VertexShaderBody, shaderPrefix + FragmentShaderBody);
        _mvpLocation = _gl.GetUniformLocation(_program, "mvp");
        _texturedLocation = _gl.GetUniformLocation(_program, "hasTexture");
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);
        _gl.Enable(EnableCap.DepthTest);
        _gl.ClearColor(0.12f, 0.14f, 0.18f, 1);
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        Volatile.Write(ref _lastSuccessfulRenderObservation, null);
        if (_gl is null)
            return;
        ClearMeshCache();
        ClearTextureCache();
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
        _gl.Dispose();
        _gl = null;
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_gl is null)
            return;
        var scene = _scene;
        var textures = _textures;
        if (!ReferenceEquals(_cachedScene, scene))
        {
            ClearMeshCache();
            _cachedScene = scene;
        }
        if (!ReferenceEquals(_cachedTextureSet, textures))
        {
            ClearTextureCache();
            _cachedTextureSet = textures;
        }
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);
        var renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var framebufferWidth = Math.Max(1, checked((int)Math.Ceiling(Bounds.Width * renderScaling)));
        var framebufferHeight = Math.Max(1, checked((int)Math.Ceiling(Bounds.Height * renderScaling)));
        _gl.Viewport(0, 0, (uint)framebufferWidth, (uint)framebufferHeight);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        if (scene is null)
        {
            Volatile.Write(ref _lastSuccessfulRenderObservation, null);
            return;
        }
        _gl.UseProgram(_program);
        var aspect = (float)framebufferWidth / framebufferHeight;
        var direction = Vector3.Normalize(Camera.Target - Camera.Position);
        var up = Math.Abs(Vector3.Dot(direction, Vector3.UnitZ)) > 0.98f ? Vector3.UnitY : Vector3.UnitZ;
        var view = Matrix4x4.CreateLookAt(Camera.Position, Camera.Target, up);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(0.78f, Math.Max(0.01f, aspect), 0.001f, Math.Max(1000, _radius * 100));
        var mvp = view * projection;
        unsafe { _gl.UniformMatrix4(_mvpLocation, 1, false, (float*)&mvp); }
        _gl.BindVertexArray(_vao);
        foreach (var node in scene.Nodes)
        {
            if (!node.RenderEnabled || node.Mesh is null)
                continue;
            var mesh = node.Mesh;
            var gpuMesh = GetOrUploadMesh(mesh);
            if (gpuMesh is null)
                continue;
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, gpuMesh.Buffer);
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
            var hasTexture = TryTexture(textures, mesh.MaterialName, out var image) || TryTexture(textures, mesh.BitmapName, out image);
            _gl.Uniform1(_texturedLocation, hasTexture ? 1 : 0);
            if (hasTexture)
                BindTexture(image!);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, gpuMesh.VertexCount);
        }
        var frameNumber = Interlocked.Increment(ref _frameNumber);
        Volatile.Write(ref _lastSuccessfulRenderObservation, new(
            scene,
            textures,
            frameNumber,
            _openGlVendor,
            _openGlRenderer,
            _openGlVersion));
    }

    private static uint CreateProgram(GL gl, string vertexSource, string fragmentSource)
    {
        var vertex = CompileShader(gl, ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(gl, ShaderType.FragmentShader, fragmentSource);
        var program = gl.CreateProgram();
        gl.AttachShader(program, vertex);
        gl.AttachShader(program, fragment);
        gl.LinkProgram(program);
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var linked);
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
        if (linked == 0)
            throw new InvalidOperationException($"Viewport shader link failed: {gl.GetProgramInfoLog(program)}");
        return program;
    }

    private static uint CompileShader(GL gl, ShaderType kind, string source)
    {
        var shader = gl.CreateShader(kind);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);
        if (compiled == 0)
            throw new InvalidOperationException($"Viewport shader compilation failed: {gl.GetShaderInfoLog(shader)}");
        return shader;
    }

    private static float[] BuildVertices(PreparedMesh mesh)
    {
        var vertices = new float[checked(mesh.Faces.Count * 3 * 5)];
        var offset = 0;
        foreach (var face in mesh.Faces)
        {
            AddVertex(face.VertexA, face.TextureA);
            AddVertex(face.VertexB, face.TextureB);
            AddVertex(face.VertexC, face.TextureC);
        }
        return vertices;

        void AddVertex(int vertexIndex, int textureIndex)
        {
            var position = mesh.Vertices[vertexIndex];
            var uv = (uint)textureIndex < (uint)mesh.TextureVertices.Count ? mesh.TextureVertices[textureIndex] : Vector2.Zero;
            vertices[offset++] = position.X;
            vertices[offset++] = position.Y;
            vertices[offset++] = position.Z;
            vertices[offset++] = uv.X;
            vertices[offset++] = 1 - uv.Y;
        }
    }

    private static bool TryTexture(IReadOnlyDictionary<string, RgbaImage> textures, string? name, out RgbaImage? image)
    {
        if (name is not null && textures.TryGetValue(name, out image))
            return true;
        image = null;
        return false;
    }

    private void BindTexture(RgbaImage image)
    {
        if (!_textureCache.TryGetValue(image, out var cached))
        {
            var byteCount = checked((long)image.Width * image.Height * 4);
            if (byteCount > MaximumCachedTextureBytes)
                throw new FormatException($"Preview texture requires {byteCount} bytes; viewport cache limit is {MaximumCachedTextureBytes}.");
            EnsureTextureCapacity(byteCount);
            cached = new CachedTexture(_gl!.GenTexture(), byteCount, ++_cacheClock);
            _textureCache.Add(image, cached);
            _cachedTextureBytes += byteCount;
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, cached.Handle);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            var pixels = image.CopyRgbaBytes();
            unsafe { fixed (byte* data = pixels) _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data); }
            TextureUploadCount++;
        }
        else
        {
            cached = cached with { LastUse = ++_cacheClock };
            _textureCache[image] = cached;
            _gl!.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, cached.Handle);
        }
    }

    private CachedMesh? GetOrUploadMesh(PreparedMesh mesh)
    {
        if (_meshCache.TryGetValue(mesh, out var cached))
        {
            cached = cached with { LastUse = ++_cacheClock };
            _meshCache[mesh] = cached;
            return cached;
        }
        if (mesh.Faces.Count == 0)
            return null;
        var byteCount = MeshUploadBudget.GetRequiredByteCount(mesh.Faces.Count);
        EnsureMeshCapacity(byteCount);
        var vertices = BuildVertices(mesh);
        var buffer = _gl!.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
        unsafe { fixed (float* data = vertices) _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)byteCount, data, BufferUsageARB.StaticDraw); }
        cached = new CachedMesh(buffer, (uint)(vertices.Length / 5), byteCount, ++_cacheClock);
        _meshCache.Add(mesh, cached);
        _cachedGeometryBytes += byteCount;
        GeometryUploadCount++;
        return cached;
    }

    private void EnsureMeshCapacity(long requiredBytes)
    {
        while (_meshCache.Count >= MaximumCachedMeshes || _cachedGeometryBytes > MeshUploadBudget.MaximumCachedGeometryBytes - requiredBytes)
        {
            var oldest = _meshCache.MinBy(pair => pair.Value.LastUse);
            _gl!.DeleteBuffer(oldest.Value.Buffer);
            _cachedGeometryBytes -= oldest.Value.ByteCount;
            _meshCache.Remove(oldest.Key);
        }
    }

    private void EnsureTextureCapacity(long requiredBytes)
    {
        while (_textureCache.Count >= MaximumCachedTextures || _cachedTextureBytes > MaximumCachedTextureBytes - requiredBytes)
        {
            var oldest = _textureCache.MinBy(pair => pair.Value.LastUse);
            _gl!.DeleteTexture(oldest.Value.Handle);
            _cachedTextureBytes -= oldest.Value.ByteCount;
            _textureCache.Remove(oldest.Key);
        }
    }

    private void ClearMeshCache()
    {
        if (_gl is not null)
            foreach (var mesh in _meshCache.Values)
                _gl.DeleteBuffer(mesh.Buffer);
        _meshCache.Clear();
        _cachedGeometryBytes = 0;
    }

    private void ClearTextureCache()
    {
        if (_gl is not null)
            foreach (var texture in _textureCache.Values)
                _gl.DeleteTexture(texture.Handle);
        _textureCache.Clear();
        _cachedTextureBytes = 0;
    }

    private sealed record CachedMesh(uint Buffer, uint VertexCount, long ByteCount, long LastUse);
    private sealed record CachedTexture(uint Handle, long ByteCount, long LastUse);

    private void FitScene(PreparedScene scene)
    {
        if (scene.Bounds is not { } bounds)
        {
            _center = Vector3.Zero;
            _radius = 1;
        }
        else
        {
            _center = bounds.Center;
            _radius = bounds.Radius;
        }
        Camera.Fit(_center, _radius);
    }
}
