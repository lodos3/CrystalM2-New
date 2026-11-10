using System.Numerics;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal readonly struct SpellParticleFrame(SpellParticlePool pool, Vector2 pivot, long now, float strength, float size, uint seed,
    uint field = 0, bool scalePositions = true, float? depthY = null, bool alwaysOnTop = false, ushort opacityNoiseSeed = 0)
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

    internal static float Envelope(float progress, float? fadeStart = null, float? tail = null)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.Envelope);
        float fade = fadeStart ?? tuning.FadeStart;
        return (tuning.RiseBase + tuning.RiseGain * Ease(progress * tuning.RiseSpeed)) *
            (1 - (1 - (tail ?? tuning.TailOpacity)) * Ease((progress - fade) / (1 - fade)));
    }
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
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.FloorSeal);
        alpha *= strength;
        if (alpha < .014f) return;
        Vector2 right = Ellipse(angle, 1, 1) * (width * Scale * tuning.Right);
        Vector2 down = new(-right.Y, right.X);
        var quad = new SpriteQuad(ProjectGround(-right - down), ProjectGround(right - down),
            ProjectGround(-right + down), ProjectGround(right + down));
        pool.TryAdd(new SpellParticle(Place(centre), Vector2.Zero, now, tuning.SpellParticleDuration, 1, Tint(colour, alpha), shape, 0,
            Layer: tuning.SpellParticleLayer, CastsLight: light, FrameOnly: true, FieldObjectID: field, Ribbon: quad, AlwaysOnTop: alwaysOnTop,
            OpacityNoiseSeed: opacityNoiseSeed));
    }

    internal void Line(Vector2 start, Vector2 end, float width, int colour, float alpha,
        Layer layer = Layer.Emissive, bool light = false)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.Line);
        Vector2 delta = end - start;
        if (delta.LengthSquared() < .09f) return;
        bool haze = width > 4;
        if (haze)
            Sprite(Vector2.Lerp(start, end, tuning.Amount), delta.Length() + width * tuning.WispWidth, width * tuning.WispHeight, tuning.WispShape,
                colour, alpha * tuning.WispAlpha, layer, MathF.Atan2(delta.Y, delta.X), light);
        float edge = Math.Min(tuning.Edge, width * tuning.Edge2);
        Thread(start, end, edge, edge * tuning.ThreadEndWidth, colour, alpha, layer, light: light && !haze);
    }

    internal void Thread(Vector2 start, Vector2 end, float startWidth, float endWidth, int colour, float alpha,
        Layer layer = Layer.Emissive, bool light = false, float? sortY = null)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.Thread);
        alpha *= strength;
        if (alpha < .014f) return;
        (start, end) = (Place(start), Place(end));
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < .3f) return;
        Vector2 centre = (start + end) * tuning.Centre;
        Vector2 normal = new Vector2(-delta.Y, delta.X) * (tuning.Normal * Scale / length);
        Vector2 overlap = delta * (Math.Min(tuning.Overlap, length * tuning.Overlap2) / length);
        Vector2 a = start - centre - overlap, b = end - centre + overlap;
        var ribbon = new SpriteQuad(a - normal * startWidth, b - normal * endWidth,
            a + normal * startWidth, b + normal * endWidth);
        pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, tuning.FilamentDuration, 1, Tint(colour, alpha), tuning.FilamentShape, 0,
            Rotation: MathF.Atan2(delta.Y, delta.X), Layer: layer, CastsLight: light, FrameOnly: true, FieldObjectID: field,
            Ribbon: ribbon, DepthY: sortY ?? depthY ?? pivot.Y, AlwaysOnTop: alwaysOnTop, OpacityNoiseSeed: opacityNoiseSeed));
    }

    internal void WindRibbon(SpriteQuad edges, int colour, float alpha)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.WindRibbon);
        alpha *= strength;
        if (alpha < .014f) return;
        var (a, b, c, d) = edges;
        (a, b, c, d) = (Place(a), Place(b), Place(c), Place(d));
        Vector2 centre = (a + b + c + d) * tuning.Centre;
        pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, tuning.WindRibbonDuration, 1, Tint(colour, alpha),
            tuning.WindRibbonShape, 0, Layer: tuning.WindRibbonLayer, CastsLight: tuning.WindRibbonCastsLight, FrameOnly: true, FieldObjectID: field,
            Ribbon: new SpriteQuad(a - centre, b - centre, c - centre, d - centre), DepthY: depthY ?? pivot.Y,
            AlwaysOnTop: alwaysOnTop, OpacityNoiseSeed: opacityNoiseSeed));
    }

    internal void Orbit(Vector2 centre, float x, float y, float angle, float sweep, int colour, float width, float alpha)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.Orbit);
        int segments = Math.Max(tuning.Segments, (int)MathF.Ceiling(sweep * tuning.Segments2));
        Vector2 from = centre + Ellipse(angle, x, y);
        for (int i = 1; i <= segments; i++)
        {
            float current = angle + sweep * i / segments;
            Vector2 to = centre + Ellipse(current, x, y);
            float flicker = tuning.Flicker + tuning.Flicker2 * Unit(i + 130);
            Line(from, to, width * flicker, colour, alpha * (tuning.LineAlpha + flicker * tuning.LineAlpha2),
                Depth(current - sweep / (tuning.Angle * segments)));
            from = to;
        }
    }

    internal void Sparks(Vector2 centre, int count, float rx, float ry, float progress, int tint,
        float rise = 0, Shape shape = Shape.Spark, float? sortY = null)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.Sparks);
        for (int i = 0; i < count; i++)
        {
            float age = Fract(progress + Unit(i + 41) * tuning.Age);
            float angle = (i + Unit(i + 5)) * MathF.Tau / count;
            Vector2 point = centre + Ellipse(angle, rx, ry) * (tuning.Point + tuning.Point2 * age) - Vector2.UnitY * (rise * age);
            float width = tuning.Width + Unit(i + 11) * tuning.Width2;
            bool glint = i % 4 == 0;
            int colour = glint ? Mix(tint, tuning.ColourColour, tuning.Colour) : Mix(tint, tuning.ColourColour2, Unit(i + 131) * tuning.Colour2);
            float fade = (tuning.Fade - age * tuning.Fade2) * (tuning.Fade3 + tuning.Fade4 * Ease(age * tuning.Fade5));
            Sprite(point, width, width * (shape == Shape.Shard ? tuning.ShardHeight : tuning.ShardHeight2), shape, colour,
                fade, glint ? tuning.ShardLayer : tuning.ShardLayer2, angle + Unit(i + 94) * tuning.ShardRotation, sortY: sortY);
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
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Frame.Add);
        alpha *= strength;
        if (alpha < .014f || width <= 0 || height <= 0) return;
        if (light && layer == Layer.Emissive && shape is Shape.Orb or Shape.Wisp) layer = tuning.Layer;
        float renderScale = layer is Layer.Ground or Layer.Floor ? 1 : size;
        int pixels = Math.Max(1, (int)MathF.Round(width / renderScale));
        pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, tuning.SpellParticleDuration, pixels, Tint(colour, alpha), shape, 0,
            height / (pixels * renderScale), rotation, Layer: layer, CastsLight: light, FrameOnly: true, FieldObjectID: field,
            Frame: animationFrame, DepthY: sortY ?? depthY ?? pivot.Y, AlwaysOnTop: alwaysOnTop, OpacityNoiseSeed: opacityNoiseSeed));
    }
}
