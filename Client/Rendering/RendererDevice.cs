using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using CrystalM2.Rendering.Core;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;
using DxBlend = Vortice.Direct3D11.Blend;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace Client.Rendering;

// The game's sole graphics device. All maps, UI, effects and screenshots use
// this Vortice D3D11 device; there is no alternate client or renderer selector.
public sealed unsafe class RendererDevice : IDisposable
{
    internal const int EffectConstantCount = 8;
    internal ID3D11Device NativeDevice { get; private set; }
    internal ID3D11DeviceContext Context { get; private set; }
    private IDXGIFactory2 _factory;
    private IDXGISwapChain1 _swapChain;
    private ID3D11VertexShader _vertexShader;
    private ID3D11InputLayout _layout;
    private ID3D11SamplerState _pointSampler, _linearSampler, _particleSampler;
    private ID3D11RasterizerState _rasterizer;
    private ID3D11DepthStencilState _noDepth;
    private ID3D11Buffer _frameBuffer, _effectBuffer;
    private readonly ID3D11PixelShader[] _shaders = new ID3D11PixelShader[Enum.GetValues<ShaderEffect>().Length];
    private readonly ID3D11BlendState[] _blends = new ID3D11BlendState[Enum.GetValues<SpriteBlendMode>().Length];
    private readonly Vector4[] _constants = new Vector4[EffectConstantCount];
    private readonly HashSet<Texture> _textures = new();
    private Texture _backBuffer;
    private Texture _white;
    private bool _tearing, _disposed;
    private SpriteBlendMode _blend;
    private SpriteBlendMode? _artworkBlend;
    private TextureFilterMode _textureFilter;
    private Vector3 _textureAdjustment = new(0.08f, 1, 0);
    private Texture _auxiliaryTexture;
    private ShaderEffect _effect;
    private float _blendRate = 1, _opacity = 1;
    public SpriteBatch Sprite { get; private set; }
    private UiSkinRenderer _ui;
    private SpellParticleRenderer _spellParticles;
    internal SpellParticleRenderer SpellParticles => _spellParticles ??= new SpellParticleRenderer(this);
    public UiSkinRenderer Ui => _ui ??= new UiSkinRenderer(this);
    public Surface CurrentSurface { get; private set; }
    public int TargetWidth => CurrentSurface?.Texture.Width ?? 0;
    public int TargetHeight => CurrentSurface?.Texture.Height ?? 0;
    public int DrawCalls { get; internal set; }
    public int SpritesSubmitted { get; internal set; }
    public long TextureBytes { get; private set; }
    public int TextureCount => _textures.Count;
    public bool IsRemoved => NativeDevice.DeviceRemovedReason.Failure;

    public SpriteBlendMode Blend
    {
        get => _blend;
        set { if (_blend == value) return; Sprite.Flush(); _blend = value; }
    }
    internal SpriteBlendMode? ArtworkBlend
    {
        get => _artworkBlend;
        set { if (_artworkBlend == value) return; Sprite.Flush(); _artworkBlend = value; }
    }
    public TextureFilterMode TextureFilter
    {
        get => _textureFilter;
        set { if (_textureFilter == value) return; Sprite.Flush(); _textureFilter = value; }
    }
    public Vector3 TextureAdjustment
    {
        get => _textureAdjustment;
        set { if (_textureAdjustment == value) return; Sprite.Flush(); _textureAdjustment = value; }
    }
    internal Texture AuxiliaryTexture
    {
        get => _auxiliaryTexture;
        set { if (ReferenceEquals(_auxiliaryTexture, value)) return; Sprite.Flush(); _auxiliaryTexture = value; }
    }
    public ShaderEffect PixelShader
    {
        get => _effect;
        set { if (_effect == value) return; Sprite.Flush(); _effect = value; }
    }
    public float BlendRate
    {
        get => _blendRate;
        set { if (_blendRate == value) return; Sprite.Flush(); _blendRate = Math.Clamp(value, 0, 1); }
    }
    public float Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0, 1);
    }

    public RendererDevice(IntPtr window, int width, int height)
        : this(window, width, height, DriverType.Hardware) { }

    // Internal driver injection lets the Windows test assembly compile this
    // same renderer against real D3D11 WARP. The game always requests hardware.
    internal RendererDevice(IntPtr window, int width, int height, DriverType driver)
    {
        try
        {
            D3D11CreateDevice(IntPtr.Zero, driver, DeviceCreationFlags.BgraSupport,
                new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_1, FeatureLevel.Level_10_0 },
                out ID3D11Device device, out _, out ID3D11DeviceContext context).CheckError();
            NativeDevice = device;
            Context = context;
            _factory = CreateDXGIFactory1<IDXGIFactory2>();
            using (var modernFactory = _factory.QueryInterfaceOrNull<IDXGIFactory5>())
                _tearing = modernFactory?.PresentAllowTearing ?? false;
            _swapChain = _factory.CreateSwapChainForHwnd(device, window, new SwapChainDescription1
            {
                Width = (uint)width, Height = (uint)height,
                Format = Format.B8G8R8A8_UNorm, BufferCount = 2,
                BufferUsage = Usage.RenderTargetOutput, SampleDescription = SampleDescription.Default,
                Scaling = Scaling.Stretch, SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore, Flags = _tearing ? SwapChainFlags.AllowTearing : SwapChainFlags.None
            });
            _factory.MakeWindowAssociation(window, WindowAssociationFlags.IgnoreAltEnter).CheckError();
            using (var dxgiDevice = device.QueryInterface<IDXGIDevice1>())
                dxgiDevice.SetMaximumFrameLatency(1).CheckError();

            ReadOnlyMemory<byte> vertexCode = Compile("VSMain", "vs_4_0");
            _vertexShader = device.CreateVertexShader(vertexCode.Span);
            _layout = device.CreateInputLayout(new[]
            {
                new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 8, 0),
                new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, 16, 0),
                new InputElementDescription("TEXCOORD", 1, Format.R32G32B32A32_Float, 32, 0),
                new InputElementDescription("TEXCOORD", 2, Format.R32_Float, 48, 0)
            }, vertexCode.Span);
            foreach (ShaderEffect effect in Enum.GetValues<ShaderEffect>())
                _shaders[(int)effect] = device.CreatePixelShader(Compile("PS" + effect, "ps_4_0").Span);

            _pointSampler = device.CreateSamplerState(SamplerDescription.PointClamp);
            _linearSampler = device.CreateSamplerState(SamplerDescription.LinearClamp);
            var particleSampler = SamplerDescription.LinearClamp;
            particleSampler.Filter = Filter.Anisotropic;
            particleSampler.MaxAnisotropy = 8;
            _particleSampler = device.CreateSamplerState(particleSampler);
            _rasterizer = device.CreateRasterizerState(new RasterizerDescription(CullMode.None, FillMode.Solid));
            _noDepth = device.CreateDepthStencilState(DepthStencilDescription.None);
            _frameBuffer = device.CreateBuffer(32, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
            _effectBuffer = device.CreateBuffer(EffectConstantCount * 16, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
            // Correct accumulated alpha for intermediate map/UI textures.
            _blends[0] = device.CreateBlendState(new BlendDescription(
                DxBlend.SourceAlpha, DxBlend.InverseSourceAlpha, DxBlend.One, DxBlend.InverseSourceAlpha));
            _blends[1] = device.CreateBlendState(new BlendDescription(
                DxBlend.SourceAlpha, DxBlend.One, DxBlend.One, DxBlend.One));
            _blends[2] = device.CreateBlendState(new BlendDescription(
                DxBlend.BlendFactor, DxBlend.InverseSourceColor, DxBlend.One, DxBlend.InverseSourceAlpha));
            _blends[3] = device.CreateBlendState(new BlendDescription(
                DxBlend.Zero, DxBlend.SourceColor, DxBlend.Zero, DxBlend.One));
            _blends[(int)SpriteBlendMode.Screen] = device.CreateBlendState(new BlendDescription(
                DxBlend.One, DxBlend.InverseSourceColor, DxBlend.One, DxBlend.InverseSourceAlpha));
            _blends[(int)SpriteBlendMode.ArtworkMultiply] = _blends[3].QueryInterface<ID3D11BlendState>();
            var subtractive = new BlendDescription(DxBlend.SourceAlpha, DxBlend.One, DxBlend.Zero, DxBlend.One);
            subtractive.RenderTarget[0].BlendOperation = BlendOperation.ReverseSubtract;
            _blends[(int)SpriteBlendMode.Subtractive] = device.CreateBlendState(subtractive);
            var channelBlend = new BlendDescription(DxBlend.One, DxBlend.One, DxBlend.Zero, DxBlend.One);
            channelBlend.RenderTarget[0].BlendOperation = BlendOperation.Max;
            _blends[(int)SpriteBlendMode.Maximum] = device.CreateBlendState(channelBlend);
            channelBlend.RenderTarget[0].BlendOperation = BlendOperation.Min;
            _blends[(int)SpriteBlendMode.Minimum] = device.CreateBlendState(channelBlend);
            Sprite = new SpriteBatch(this);
            Resize(width, height);
        }
        catch { Dispose(); throw; }
    }

    private static ReadOnlyMemory<byte> Compile(string entry, string profile) =>
        Compiler.CompileFromFile(Path.Combine(AppContext.BaseDirectory, "Data", "Shaders", "World.hlsl"), entry, profile);

    internal static void ValidateShaders()
    {
        Compile("VSMain", "vs_4_0");
        foreach (ShaderEffect effect in Enum.GetValues<ShaderEffect>()) Compile("PS" + effect, "ps_4_0");
        Console.WriteLine($"All {Enum.GetValues<ShaderEffect>().Length + 1} production D3D11 shader entry points compiled.");
    }

    internal void Register(Texture texture)
    {
        _textures.Add(texture);
        TextureBytes += texture.Bytes;
    }
    internal void Unregister(Texture texture)
    {
        if (_textures.Remove(texture)) TextureBytes -= texture.Bytes;
    }

    public Surface GetBackBuffer() => _backBuffer.GetSurface();
    public Size BackBufferSize => new(_backBuffer.Width, _backBuffer.Height);

    internal void Unbind(Texture texture)
    {
        if (CurrentSurface?.Texture != texture) return;
        Context.OMSetRenderTargets((ID3D11RenderTargetView)null);
        CurrentSurface = null;
    }

    public void SetRenderTarget(Surface surface)
    {
        if (surface == null || surface.Disposed || surface.Texture.Disposed)
            throw new ObjectDisposedException(nameof(surface));
        Sprite.Flush();
        Context.PSUnsetShaderResource(0);
        CurrentSurface = surface;
        Context.OMSetRenderTargets(surface.Texture.Target);
        Context.RSSetViewport(new Viewport(TargetWidth, TargetHeight));
    }

    public void Clear(Color colour)
    {
        Sprite.Flush();
        // Intermediate textures hold premultiplied composited pixels. Sources
        // remain straight alpha; the shader reverses this only on sampled RTs.
        float alpha = colour.A / 255f;
        Context.ClearRenderTargetView(CurrentSurface.Texture.Target,
            new Color4(colour.R / 255f * alpha, colour.G / 255f * alpha, colour.B / 255f * alpha, alpha));
    }

    public void SetPixelShaderConstant(int start, ReadOnlySpan<Vector4> values)
    {
        Sprite.Flush();
        values.CopyTo(_constants.AsSpan(start));
    }

    public void CopyPixelShaderConstants(Span<Vector4> destination) => _constants.AsSpan().CopyTo(destination);

    internal void Bind(Texture source, ID3D11Buffer vertices, uint stride)
    {
        // Flip-model Present unbinds its backbuffer; bind the current RTV every
        // flush, including when the scene reuses a cached world/UI texture.
        Context.OMSetRenderTargets(CurrentSurface.Texture.Target);
        Context.RSSetViewport(new Viewport(TargetWidth, TargetHeight));
        Context.RSSetState(_rasterizer);
        Context.OMSetDepthStencilState(_noDepth);
        bool artwork = _effect is ShaderEffect.Artwork or ShaderEffect.Normal or ShaderEffect.Grayscale or ShaderEffect.Magic or ShaderEffect.SpellArtwork;
        bool particle = _effect == ShaderEffect.ParticleCoverage;
        SpriteBlendMode blend = artwork || particle ? _artworkBlend ?? _blend : _blend;
        Context.OMSetBlendState(_blends[(int)blend], new Color4(_blendRate, _blendRate, _blendRate, _blendRate));
        Context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        Context.IASetInputLayout(_layout);
        Context.IASetVertexBuffer(0, vertices, stride);
        Context.VSSetShader(_vertexShader);
        Context.PSSetShader(_shaders[(int)_effect]);
        Context.PSSetShaderResource(0, source.View);
        if (_auxiliaryTexture is { Disposed: false })
        {
            Context.PSSetShaderResource(1, _auxiliaryTexture.View);
            Context.PSSetSampler(1, _linearSampler);
        }
        bool smooth = _effect is ShaderEffect.BloomBlur or ShaderEffect.BloomExtract or ShaderEffect.ShadowMask or
            ShaderEffect.ProjectedShadow or ShaderEffect.GaussianLight or ShaderEffect.ConeLight or ShaderEffect.MapColourDownsample ||
            _effect is ShaderEffect.WeatherSceneryBlur or ShaderEffect.WeatherGround or ShaderEffect.WeatherParticle or ShaderEffect.WeatherMaskView ||
            _effect == ShaderEffect.ShapedLight && _constants[0].Y > 0;
        Context.PSSetSampler(0, particle ? _particleSampler : smooth ? _linearSampler : _pointSampler);
        var frame = Context.Map(_frameBuffer, MapMode.WriteDiscard);
        int style = (int)blend + (artwork ? 16 * (int)_textureFilter : 0);
        try
        {
            var values = frame.AsSpan<Vector4>(2);
            // Floating-point emission stores additive RGB. Its alpha can
            // exceed one and must not be used to unpremultiply the light.
            values[0] = new Vector4(TargetWidth, TargetHeight, source.IsRenderTarget && !source.IsHdrLightBuffer ? 1 : 0, style);
            values[1] = new Vector4(_textureAdjustment, 0);
        }
        finally { Context.Unmap(_frameBuffer, 0); }
        var constants = Context.Map(_effectBuffer, MapMode.WriteDiscard);
        try { _constants.AsSpan().CopyTo(constants.AsSpan<Vector4>(EffectConstantCount)); }
        finally { Context.Unmap(_effectBuffer, 0); }
        Context.VSSetConstantBuffer(0, _frameBuffer);
        Context.PSSetConstantBuffer(0, _frameBuffer);
        Context.PSSetConstantBuffer(1, _effectBuffer);
    }

    public void BeginFrame()
    {
        DrawCalls = SpritesSubmitted = 0;
        PixelShader = ShaderEffect.Artwork;
    }

    public void Present(bool vsync)
    {
        Sprite.Flush();
        _swapChain.Present(vsync ? 1u : 0u,
            !vsync && _tearing ? PresentFlags.AllowTearing : PresentFlags.None).CheckError();
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        Sprite.Discard();
        Context.ClearState();
        CurrentSurface = null;
        _backBuffer?.Dispose();
        _backBuffer = null;
        _swapChain.ResizeBuffers(0, (uint)width, (uint)height, Format.Unknown,
            _tearing ? SwapChainFlags.AllowTearing : SwapChainFlags.None).CheckError();
        _backBuffer = new Texture(this, _swapChain.GetBuffer<ID3D11Texture2D>(0), width, height);
        SetRenderTarget(GetBackBuffer());
    }

    public Bitmap Capture(Surface surface)
    {
        Sprite.Flush();
        var description = surface.Texture.Resource.Description;
        if (description.Format != Format.B8G8R8A8_UNorm)
            throw new InvalidOperationException("Bitmap capture requires a BGRA8 render target.");
        description.BindFlags = BindFlags.None;
        description.Usage = ResourceUsage.Staging;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        description.MiscFlags = ResourceOptionFlags.None;
        using var readback = NativeDevice.CreateTexture2D(description);
        Context.CopyResource(readback, surface.Texture.Resource);
        var mapped = Context.Map(readback, 0, MapMode.Read);
        try
        {
            using var borrowed = new Bitmap(surface.Texture.Width, surface.Texture.Height,
                (int)mapped.RowPitch, PixelFormat.Format32bppArgb, mapped.DataPointer);
            return new Bitmap(borrowed);
        }
        finally { Context.Unmap(readback, 0); }
    }

    public void DrawLines(Vector2[] points, Color colour)
    {
        if (points == null || points.Length < 2) return;
        if (_white == null)
        {
            _white = new Texture(this, 1, 1);
            var upload = _white.BeginUpload();
            new Span<byte>((void*)upload.Data.DataPointer, 4).Fill(255);
            _white.EndUpload();
        }
        Matrix4x4 old = Sprite.Transform;
        try
        {
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 delta = points[i] - points[i - 1];
                float length = delta.Length();
                if (length == 0) continue;
                Sprite.Transform = Matrix4x4.CreateScale(length, 1, 1) *
                    Matrix4x4.CreateRotationZ(MathF.Atan2(delta.Y, delta.X)) *
                    Matrix4x4.CreateTranslation(points[i - 1].X, points[i - 1].Y, 0);
                Sprite.Draw(_white, null, Vector3.Zero, ToVector(colour));
            }
        }
        finally { Sprite.Transform = old; }
    }

    public static Vector4 ToVector(Color colour) =>
        new(colour.R / 255f, colour.G / 255f, colour.B / 255f, colour.A / 255f);

    public void DiscardPending() => Sprite?.Discard();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Sprite?.Discard();
        Context?.ClearState();
        foreach (Texture texture in _textures.ToArray()) texture.Dispose();
        _backBuffer?.Dispose();
        Sprite?.Dispose();
        foreach (var shader in _shaders) shader?.Dispose();
        foreach (var blend in _blends) blend?.Dispose();
        _frameBuffer?.Dispose();
        _effectBuffer?.Dispose();
        _layout?.Dispose();
        _vertexShader?.Dispose();
        _pointSampler?.Dispose();
        _linearSampler?.Dispose();
        _particleSampler?.Dispose();
        _noDepth?.Dispose();
        _rasterizer?.Dispose();
        _swapChain?.Dispose();
        _factory?.Dispose();
        Context?.Flush();
        Context?.Dispose();
        NativeDevice?.Dispose();
    }
}
