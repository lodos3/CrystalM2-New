using System.Numerics;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal readonly struct SpellParticleFrame(SpellParticlePool pool, Vector2 pivot, long now, float strength, float size, uint seed,
    uint field = 0, bool scalePositions = true, float? depthY = null, bool alwaysOnTop = false)
{
    internal const float ReferenceSize = 1.28f;
    internal const float GroundAspect = 2f / 3f;

    internal static Vector2 Ellipse(float angle, float x, float y) => new(MathF.Cos(angle) * x, MathF.Sin(angle) * y);
    internal static Vector2 ProjectGround(Vector2 point) => new(point.X, point.Y * GroundAspect);
    internal static Vector2 GroundHeading(Vector2 facing) => Vector2.Normalize(new Vector2(facing.X, facing.Y / GroundAspect));
    internal static Layer Depth(float angle) => MathF.Sin(angle) < 0 ? Layer.Ground : Layer.Emissive;
    internal static float Fract(float value) => value - MathF.Floor(value);
    internal static float Ease(float value)
    {
        value = Math.Clamp(value, 0, 1);
        return value * value * (3 - 2 * value);
    }

    internal static float Envelope(float progress) => (.61f + .39f * Ease(progress * 8)) * (1 - Ease((progress - .65f) / .35f));
    internal static Color Tint(int rgb, float opacity) => Color.FromArgb((int)(255 * Math.Clamp(opacity, 0, 1)) << 24 | rgb);

    internal float Scale => size / ReferenceSize;
    private Vector2 Place(Vector2 point) => scalePositions ? pivot + (point - pivot) * Scale : point;

    internal float Unit(int index)
    {
        uint hash = seed + (uint)index * 0x9e3779b9u;
        hash ^= hash >> 16;
        hash *= 0x7feb352du;
        hash ^= hash >> 15;
        hash *= 0x846ca68bu;
        return ((hash ^ (hash >> 16)) >> 8) / 16777216f;
    }

    internal void Sprite(Vector2 centre, float width, float height, Shape shape, int colour, float alpha,
        Layer layer = Layer.Emissive, float rotation = 0, bool light = false, float? sortY = null) =>
        Add(Place(centre), width * Scale, height * Scale, shape, colour, alpha, layer, rotation, light, sortY: sortY);

    internal void Clip(Vector2 centre, float width, float height, Shape shape, float phase, int colour, float alpha,
        Layer layer = Layer.Emissive, float rotation = 0, bool light = false, float? sortY = null)
    {
        float sample = Fract(phase) * SpellParticleAtlas.ClipFrames;
        byte first = (byte)sample;
        byte second = (byte)((first + 1) % SpellParticleAtlas.ClipFrames);
        float mix = sample - first;
        centre = Place(centre);
        width *= Scale;
        height *= Scale;
        Add(centre, width, height, shape, colour, alpha * (1 - mix), layer, rotation, light, first, sortY);
        Add(centre, width, height, shape, colour, alpha * mix, layer, rotation, light, second, sortY);
    }

    internal void FloorSeal(Vector2 centre, float width, float angle, Shape shape, int colour, float alpha, bool light = false)
    {
        alpha *= strength;
        if (alpha < .014f) return;
        Vector2 right = Ellipse(angle, 1, 1) * (width * Scale * .5f);
        Vector2 down = new(-right.Y, right.X);
        var quad = new SpriteQuad(ProjectGround(-right - down), ProjectGround(right - down),
            ProjectGround(-right + down), ProjectGround(right + down));
        pool.TryAdd(new SpellParticle(Place(centre), Vector2.Zero, now, 50, 1, Tint(colour, alpha), shape, 0,
            Layer: Layer.Floor, CastsLight: light, FrameOnly: true, FieldObjectID: field, Ribbon: quad, AlwaysOnTop: alwaysOnTop));
    }

    internal void Line(Vector2 start, Vector2 end, float width, int colour, float alpha,
        Layer layer = Layer.Emissive, bool light = false)
    {
        Vector2 delta = end - start;
        if (delta.LengthSquared() < .09f) return;
        bool haze = width > 4;
        if (haze)
            Sprite(Vector2.Lerp(start, end, .45f), delta.Length() + width * .65f, width * 1.36f, Shape.Wisp,
                colour, alpha * .88f, layer, MathF.Atan2(delta.Y, delta.X), light);
        float edge = Math.Min(3.65f, width * .79f);
        Thread(start, end, edge, edge * .49f, colour, alpha, layer, light: light && !haze);
    }

    internal void Thread(Vector2 start, Vector2 end, float startWidth, float endWidth, int colour, float alpha,
        Layer layer = Layer.Emissive, bool light = false, float? sortY = null)
    {
        alpha *= strength;
        if (alpha < .014f) return;
        (start, end) = (Place(start), Place(end));
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < .3f) return;
        Vector2 centre = (start + end) * .5f;
        Vector2 normal = new Vector2(-delta.Y, delta.X) * (.5f * Scale / length);
        Vector2 overlap = delta * (Math.Min(.85f, length * .12f) / length);
        Vector2 a = start - centre - overlap, b = end - centre + overlap;
        var ribbon = new SpriteQuad(a - normal * startWidth, b - normal * endWidth,
            a + normal * startWidth, b + normal * endWidth);
        pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, 50, 1, Tint(colour, alpha), Shape.Filament, 0,
            Rotation: MathF.Atan2(delta.Y, delta.X), Layer: layer, CastsLight: light, FrameOnly: true, FieldObjectID: field,
            Ribbon: ribbon, DepthY: sortY ?? depthY ?? pivot.Y, AlwaysOnTop: alwaysOnTop));
    }

    internal void WindRibbon(SpriteQuad edges, int colour, float alpha)
    {
        alpha *= strength;
        if (alpha < .014f) return;
        var (a, b, c, d) = edges;
        (a, b, c, d) = (Place(a), Place(b), Place(c), Place(d));
        Vector2 centre = (a + b + c + d) * .25f;
        pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, 50, 1, Tint(colour, alpha),
            Shape.WindRibbon, 0, Layer: Layer.Emissive, CastsLight: false, FrameOnly: true, FieldObjectID: field,
            Ribbon: new SpriteQuad(a - centre, b - centre, c - centre, d - centre), DepthY: depthY ?? pivot.Y,
            AlwaysOnTop: alwaysOnTop));
    }

    internal void Orbit(Vector2 centre, float x, float y, float angle, float sweep, int colour, float width, float alpha)
    {
        int segments = Math.Max(4, (int)MathF.Ceiling(sweep * 3.2f));
        Vector2 from = centre + Ellipse(angle, x, y);
        for (int i = 1; i <= segments; i++)
        {
            float current = angle + sweep * i / segments;
            Vector2 to = centre + Ellipse(current, x, y);
            float flicker = .67f + .33f * Unit(i + 130);
            Line(from, to, width * flicker, colour, alpha * (.74f + flicker * .26f),
                Depth(current - sweep / (2 * segments)));
            from = to;
        }
    }

    internal void Sparks(Vector2 centre, int count, float rx, float ry, float progress, int tint,
        float rise = 0, Shape shape = Shape.Spark, float? sortY = null)
    {
        for (int i = 0; i < count; i++)
        {
            float age = Fract(progress + Unit(i + 41) * .81f);
            float angle = (i + Unit(i + 5)) * MathF.Tau / count;
            Vector2 point = centre + Ellipse(angle, rx, ry) * (.20f + .82f * age) - Vector2.UnitY * (rise * age);
            float width = 3.1f + Unit(i + 11) * 8.7f;
            bool glint = i % 4 == 0;
            int colour = glint ? Mix(tint, 0xffffff, .61f) : Mix(tint, 0x243055, Unit(i + 131) * .34f);
            float fade = (.89f - age * .66f) * (.55f + .45f * Ease(age * 12));
            Sprite(point, width, width * (shape == Shape.Shard ? 2.2f : 1.46f), shape, colour,
                fade, glint ? Layer.EmissiveCore : Layer.Emissive, angle + Unit(i + 94) * .88f, sortY: sortY);
        }
    }

    private static int Mix(int a, int b, float amount)
    {
        int Channel(int shift) => (int)(((a >> shift) & 255) * (1 - amount) + ((b >> shift) & 255) * amount);
        return Channel(16) << 16 | Channel(8) << 8 | Channel(0);
    }

    private void Add(Vector2 centre, float width, float height, Shape shape, int colour, float alpha,
        Layer layer, float rotation, bool light, byte animationFrame = 0, float? sortY = null)
    {
        alpha *= strength;
        if (alpha < .014f || width <= 0 || height <= 0) return;
        if (light && layer == Layer.Emissive && shape is Shape.Orb or Shape.Wisp) layer = Layer.Glow;
        float renderScale = layer is Layer.Ground or Layer.Floor ? 1 : size;
        int pixels = Math.Max(1, (int)MathF.Round(width / renderScale));
        pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, 50, pixels, Tint(colour, alpha), shape, 0,
            height / (pixels * renderScale), rotation, Layer: layer, CastsLight: light, FrameOnly: true, FieldObjectID: field,
            Frame: animationFrame, DepthY: sortY ?? depthY ?? pivot.Y, AlwaysOnTop: alwaysOnTop));
    }
}
