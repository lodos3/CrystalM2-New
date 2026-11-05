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
    internal bool HasCutout { get; set; }
    public bool Disposed { get; private set; }
    internal long Bytes => (long)Width * Height * 4;

    public Texture(RendererDevice owner, int width, int height, bool renderTarget = false)
    {
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384)
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid D3D11 texture dimensions.");
        _owner = owner;
        Width = width;
        Height = height;
        IsRenderTarget = renderTarget;
        try
        {
            Resource = owner.NativeDevice.CreateTexture2D(new Texture2DDescription
            {
                Width = (uint)width, Height = (uint)height, MipLevels = 1, ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm, SampleDescription = SampleDescription.Default,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | (renderTarget ? BindFlags.RenderTarget : BindFlags.None)
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
        try { Target = owner.NativeDevice.CreateRenderTargetView(Resource); }
        catch { Resource.Dispose(); throw; }
    }

    public DataRectangle BeginUpload()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
        if (_uploadMemory != null) throw new InvalidOperationException("Texture is already being uploaded.");
        int pitch = checked(Width * 4), bytes = checked(pitch * Height);
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
            _owner.Context.UpdateSubresource(Resource, 0, null, (IntPtr)_uploadMemory, (uint)(Width * 4), 0);
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
