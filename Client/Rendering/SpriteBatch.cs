using System.Numerics;
using CrystalM2.Rendering.Core;
using Vortice.Direct3D11;

namespace Client.Rendering;

public sealed unsafe class SpriteBatch : IDisposable
{
    private const int MaxVertices = 2048 * 6;
    private readonly RendererDevice _owner;
    private readonly SpriteVertex[] _pending = new SpriteVertex[MaxVertices];
    private readonly ID3D11Buffer _buffer;
    private Texture _texture;
    private int _count;
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
    public bool Disposed { get; private set; }

    internal SpriteBatch(RendererDevice owner)
    {
        _owner = owner;
        _buffer = owner.NativeDevice.CreateBuffer((uint)(sizeof(SpriteVertex) * MaxVertices),
            BindFlags.VertexBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write);
    }

    public void Begin(bool alphaBlend = true)
    {
        Flush();
        if (alphaBlend) _owner.Blend = SpriteBlendMode.Alpha;
    }

    public void End() => Flush();

    public void Draw(Texture texture, Rectangle? source, Vector3? position, Vector4 colour)
    {
        if (texture == null || texture.Disposed || colour.W <= 0) return;
        if (ReferenceEquals(texture, _owner.CurrentSurface?.Texture))
            throw new InvalidOperationException("Cannot sample the active render target.");
        Vector3 p = position ?? Vector3.Zero;
        Span<SpriteVertex> quad = stackalloc SpriteVertex[6];
        colour.W *= _owner.Opacity;
        if (!SpriteGeometry.WriteQuad(quad, source ?? new Rectangle(0, 0, texture.Width, texture.Height),
            texture.Width, texture.Height, new Vector2(p.X, p.Y), Transform, colour,
            _owner.TargetWidth, _owner.TargetHeight)) return;

        // Preserve painter's order. Never sort by texture across actors/layers.
        if (!ReferenceEquals(texture, _texture) || _count == MaxVertices) Flush();
        _texture = texture;
        quad.CopyTo(_pending.AsSpan(_count));
        _count += 6;
    }

    public void Flush()
    {
        if (_count == 0 || Disposed) return;
        int count = _count;
        _count = 0; // Failed device operations must not replay stale commands.
        try
        {
            var mapped = _owner.Context.Map(_buffer, MapMode.WriteDiscard);
            try { _pending.AsSpan(0, count).CopyTo(mapped.AsSpan<SpriteVertex>(count)); }
            finally { _owner.Context.Unmap(_buffer, 0); }
            _owner.Bind(_texture, _buffer, (uint)sizeof(SpriteVertex));
            _owner.Context.Draw((uint)count, 0);
            _owner.DrawCalls++;
            _owner.SpritesSubmitted += count / 6;
        }
        finally { _owner.Context.PSUnsetShaderResource(0); }
    }

    internal void Discard()
    {
        _count = 0;
        _texture = null;
        Transform = Matrix4x4.Identity;
    }

    public void Dispose()
    {
        if (Disposed) return;
        Discard();
        Disposed = true;
        _buffer.Dispose();
    }
}
