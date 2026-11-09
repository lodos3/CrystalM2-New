using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Client.Rendering;

// A CPU write exists only during upload. Original BGRA bytes are never
// premultiplied, colour-corrected or retained in a second full-size copy.
public sealed unsafe class Texture : IDisposable
{
    private readonly RendererDevice _owner;
    private UploadStream _upload;
    private void* _uploadMemory;
    internal ID3D11Texture2D Resource { get; private set; }
    internal ID3D11ShaderResourceView View { get; private set; }
    internal ID3D11RenderTargetView Target { get; private set; }
    public int Width { get; }
    public int Height { get; }
    public bool IsRenderTarget { get; }
    internal bool IsWorldArtwork { get; set; }
    public bool Disposed { get; private set; }
    private readonly int _bytesPerPixel = 4;
    private readonly int _mipLevels = 1;
    internal long Bytes { get; }
    internal bool IsHdrLightBuffer => _bytesPerPixel == 8;

    public Texture(RendererDevice owner, int width, int height, bool renderTarget = false)
        : this(owner, width, height, renderTarget, false) { }

    internal Texture(RendererDevice owner, int width, int height, bool renderTarget, bool hdr)
        : this(owner, width, height, renderTarget, hdr ? Format.R16G16B16A16_Float : Format.B8G8R8A8_UNorm, 1) { }

    internal static Texture CreateCoverageAtlas(RendererDevice owner, int width, int height, int mipLevels) =>
        new(owner, width, height, false, Format.R8_UNorm, mipLevels);

    internal static Texture CreateSpellOcclusion(RendererDevice owner, int width, int height) =>
        new(owner, width, height, true, Format.R16_Float, 1);

    internal static Texture CreateWeatherMask(RendererDevice owner, int width, int height) =>
        new(owner, width, height, true, Format.B8G8R8A8_UNorm,
            Math.Min(4, 1 + System.Numerics.BitOperations.Log2((uint)Math.Max(width, height))));

    internal void GenerateMips()
    {
        if (_mipLevels > 1) _owner.Context.GenerateMips(View);
    }

    private Texture(RendererDevice owner, int width, int height, bool renderTarget, Format format, int mipLevels)
    {
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384)
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid D3D11 texture dimensions.");
        _owner = owner;
        Width = width;
        Height = height;
        IsRenderTarget = renderTarget;
        _bytesPerPixel = format switch { Format.R8_UNorm => 1, Format.R16_Float => 2, Format.R16G16B16A16_Float => 8, _ => 4 };
        _mipLevels = mipLevels;
        bool generateMips = mipLevels > 1;
        for (int level = 0; level < mipLevels; level++)
            Bytes += (long)Math.Max(1, width >> level) * Math.Max(1, height >> level) * _bytesPerPixel;
        try
        {
            Resource = owner.NativeDevice.CreateTexture2D(new Texture2DDescription
            {
                Width = (uint)width, Height = (uint)height, MipLevels = (uint)mipLevels, ArraySize = 1,
                Format = format, SampleDescription = SampleDescription.Default,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | (renderTarget || generateMips ? BindFlags.RenderTarget : BindFlags.None),
                MiscFlags = generateMips ? ResourceOptionFlags.GenerateMips : ResourceOptionFlags.None
            });
            View = owner.NativeDevice.CreateShaderResourceView(Resource);
            if (renderTarget) Target = owner.NativeDevice.CreateRenderTargetView(Resource);
            owner.Register(this);
        }
        catch
        {
            Target?.Dispose();
            View?.Dispose();
            Resource?.Dispose();
            throw;
        }
    }

    internal Texture(RendererDevice owner, ID3D11Texture2D backBuffer, int width, int height)
    {
        _owner = owner;
        Resource = backBuffer;
        Width = width;
        Height = height;
        IsRenderTarget = true;
        Bytes = (long)width * height * _bytesPerPixel;
        try { Target = owner.NativeDevice.CreateRenderTargetView(Resource); }
        catch { Resource.Dispose(); throw; }
    }

    public DataRectangle BeginUpload()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        if (IsHdrLightBuffer) throw new InvalidOperationException("This texture is a floating-point render target.");
        if (_uploadMemory != null) throw new InvalidOperationException("Texture is already being uploaded.");
        int pitch = checked(Width * _bytesPerPixel), bytes = checked(pitch * Height);
        _uploadMemory = NativeMemory.AllocZeroed((nuint)bytes);
        if (_uploadMemory == null) throw new OutOfMemoryException();
        _upload = new UploadStream((byte*)_uploadMemory, bytes);
        return new DataRectangle(_upload, pitch);
    }

    public void EndUpload()
    {
        if (_uploadMemory == null) throw new InvalidOperationException("No texture upload is active.");
        try
        {
            // Pending draws must see the previous contents before replacement.
            _owner.Sprite.Flush();
            _owner.Context.UpdateSubresource(Resource, 0, null, (IntPtr)_uploadMemory, (uint)(Width * _bytesPerPixel), 0);
            GenerateMips();
        }
        finally { ReleaseUpload(); }
    }

    private void ReleaseUpload()
    {
        _upload?.Dispose();
        _upload = null;
        NativeMemory.Free(_uploadMemory);
        _uploadMemory = null;
    }

    public Surface GetSurface() => Target == null
        ? throw new InvalidOperationException("This texture has no render target.") : new Surface(this);

    public void Dispose()
    {
        if (Disposed) return;
        _owner.Sprite?.Flush();
        Disposed = true;
        _owner.Context.PSUnsetShaderResource(0);
        _owner.Unbind(this);
        ReleaseUpload();
        Target?.Dispose();
        View?.Dispose();
        Resource?.Dispose();
        _owner.Unregister(this);
    }
}

public readonly record struct DataRectangle(UploadStream Data, int Pitch);

public sealed unsafe class UploadStream : UnmanagedMemoryStream
{
    public IntPtr DataPointer { get; }
    internal UploadStream(byte* pointer, int size) : base(pointer, size, size, FileAccess.ReadWrite)
    {
        DataPointer = (IntPtr)pointer;
    }
}

// The texture owns its RTV. Temporary surface handles never own a COM
// reference, so cached UI drawing cannot pin old swap-chain buffers on resize.
public sealed class Surface : IDisposable
{
    internal Texture Texture { get; }
    public bool Disposed { get; private set; }
    internal Surface(Texture texture) => Texture = texture;
    public void Dispose() => Disposed = true;
}
