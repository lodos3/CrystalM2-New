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
    public float Depth;

    public SpriteVertex(Vector2 position, Vector2 uv, Vector4 tint, Vector4 uvBounds, float depth = 0)
    {
        Position = position;
        Uv = uv;
        Tint = tint;
        UvBounds = uvBounds;
        Depth = depth;
    }
}

public readonly record struct SpriteQuad(Vector2 TopLeft, Vector2 TopRight, Vector2 BottomLeft, Vector2 BottomRight)
{
    public SpriteQuad Scaled(float factor) => factor == 1f ? this :
        new(TopLeft * factor, TopRight * factor, BottomLeft * factor, BottomRight * factor);

    public SpriteQuad Transform(Vector2 origin, Matrix4x4 transform) => new(
        Vector2.Transform(TopLeft + origin, transform), Vector2.Transform(TopRight + origin, transform),
        Vector2.Transform(BottomLeft + origin, transform), Vector2.Transform(BottomRight + origin, transform));

    public Rectangle Bounds(Vector2 origin)
    {
        Vector2 min = Vector2.Min(Vector2.Min(TopLeft, TopRight), Vector2.Min(BottomLeft, BottomRight)) + origin;
        Vector2 max = Vector2.Max(Vector2.Max(TopLeft, TopRight), Vector2.Max(BottomLeft, BottomRight)) + origin;
        return Rectangle.FromLTRB((int)MathF.Floor(min.X), (int)MathF.Floor(min.Y), (int)MathF.Ceiling(max.X), (int)MathF.Ceiling(max.Y));
    }
}

public static class SpriteGeometry
{
    // Coordinates are pixel edges, with no D3D9 half-texel correction.
    // Check all transformed corners: a sheared shadow can enter the viewport
    // even when its untransformed sprite origin is outside it.
    public static bool WriteQuad(Span<SpriteVertex> output, Rectangle source,
        int textureWidth, int textureHeight, Vector2 position, Matrix4x4 transform,
        Vector4 tint, int viewportWidth, int viewportHeight, float depth = 0) => WriteQuad(output, source, textureWidth, textureHeight,
            new SpriteQuad(Vector2.Zero, new Vector2(source.Width, 0), new Vector2(0, source.Height),
                new Vector2(source.Width, source.Height)).Transform(position, transform), tint, viewportWidth, viewportHeight, depth);

    public static bool WriteQuad(Span<SpriteVertex> output, Rectangle source,
        int textureWidth, int textureHeight, SpriteQuad quad, Vector4 tint, int viewportWidth, int viewportHeight, float depth = 0)
    {
        if (output.Length < 6) throw new ArgumentException("Six vertices required.", nameof(output));
        if (source.Width <= 0 || source.Height <= 0 || textureWidth <= 0 || textureHeight <= 0 ||
            viewportWidth <= 0 || viewportHeight <= 0) return false;

        var (a, b, c, d) = quad;
        if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(d)) return false;
        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        if (min.X >= viewportWidth || min.Y >= viewportHeight || max.X <= 0 || max.Y <= 0) return false;

        float left = (float)source.Left / textureWidth, right = (float)source.Right / textureWidth;
        float top = (float)source.Top / textureHeight, bottom = (float)source.Bottom / textureHeight;
        Vector4 uvBounds = new(left, top, right, bottom);
        output[0] = new SpriteVertex(a, new Vector2(left, top), tint, uvBounds, depth);
        output[1] = new SpriteVertex(b, new Vector2(right, top), tint, uvBounds, depth);
        output[2] = new SpriteVertex(c, new Vector2(left, bottom), tint, uvBounds, depth);
        output[3] = output[1];
        output[4] = new SpriteVertex(d, new Vector2(right, bottom), tint, uvBounds, depth);
        output[5] = output[2];
        return true;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
