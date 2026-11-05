using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CrystalM2.Rendering.Core;

[StructLayout(LayoutKind.Sequential)]
public struct SpriteVertex
{
    public Vector2 Position;
    public Vector2 Uv;
    public Vector4 Tint;
    public Vector4 UvBounds;

    public SpriteVertex(Vector2 position, Vector2 uv, Vector4 tint, Vector4 uvBounds)
    {
        Position = position;
        Uv = uv;
        Tint = tint;
        UvBounds = uvBounds;
    }
}

public static class SpriteGeometry
{
    // Coordinates are pixel edges, with no D3D9 half-texel correction.
    // Check all transformed corners: a sheared shadow can enter the viewport
    // even when its untransformed sprite origin is outside it.
    public static bool WriteQuad(Span<SpriteVertex> output, Rectangle source,
        int textureWidth, int textureHeight, Vector2 position, Matrix4x4 transform,
        Vector4 tint, int viewportWidth, int viewportHeight)
    {
        if (output.Length < 6) throw new ArgumentException("Six vertices required.", nameof(output));
        if (source.Width <= 0 || source.Height <= 0 || textureWidth <= 0 || textureHeight <= 0 ||
            viewportWidth <= 0 || viewportHeight <= 0) return false;

        Vector2 a = Vector2.Transform(position, transform);
        Vector2 b = Vector2.Transform(position + new Vector2(source.Width, 0), transform);
        Vector2 c = Vector2.Transform(position + new Vector2(0, source.Height), transform);
        Vector2 d = Vector2.Transform(position + new Vector2(source.Width, source.Height), transform);
        if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(d)) return false;
        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        if (min.X >= viewportWidth || min.Y >= viewportHeight || max.X <= 0 || max.Y <= 0) return false;

        float left = (float)source.Left / textureWidth, right = (float)source.Right / textureWidth;
        float top = (float)source.Top / textureHeight, bottom = (float)source.Bottom / textureHeight;
        Vector4 uvBounds = new(left, top, right, bottom);
        output[0] = new SpriteVertex(a, new Vector2(left, top), tint, uvBounds);
        output[1] = new SpriteVertex(b, new Vector2(right, top), tint, uvBounds);
        output[2] = new SpriteVertex(c, new Vector2(left, bottom), tint, uvBounds);
        output[3] = output[1];
        output[4] = new SpriteVertex(d, new Vector2(right, bottom), tint, uvBounds);
        output[5] = output[2];
        return true;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
