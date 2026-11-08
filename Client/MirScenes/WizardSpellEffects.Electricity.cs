using System.Numerics;
using Frame = Client.MirScenes.SpellParticleFrame;
using static Client.MirScenes.SpellParticleFrame;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static partial class WizardSpellEffects
{
    internal readonly record struct ArcColours(int Shadow, int Glow, int Filament, int Core);
    private static readonly ArcColours StormColours = new(0x233d89, 0x568af4, 0xa2d3ff, White);
    internal static readonly ArcColours DrakeColours = new(0x142466, 0x3861da, 0x82b8ff, 0xf4fbff);

    internal static void DrawArc(SpellParticlePool pool, Spell spell, Vector2 from, Vector2 to,
        long now, long start, int duration, float size, uint seed)
    {
        if (duration <= 0 || now < start || now - start >= duration) return;
        float progress = (now - start) / (float)duration;
        float time = (now - start) / 1000f;
        var frame = new Frame(pool, from, now, Envelope(progress), size, seed, scalePositions: false);
        Vector2 source = from - Vector2.UnitY * 44;
        Vector2 destination = to - Vector2.UnitY * 41;
        if (spell == Spell.Vampirism)
        {
            Vector2 head = Vector2.Lerp(source, destination, Ease(progress)) - Vector2.UnitY * (35 * MathF.Sin(progress * MathF.PI));
            float depth = Vector2.Lerp(from, to, Ease(progress)).Y;
            frame.Sprite(head, 42, 49, Shape.Orb, 0x931b34, .71f, light: true, sortY: depth);
            frame.Sprite(head, 28, 35, Shape.FireCurl, 0xf64842, .85f, rotation: time * 12, sortY: depth);
            frame.Sprite(head, 15, 18, Shape.Orb, 0xffb3a7, .91f, Layer.EmissiveCore, sortY: depth);
            for (int i = 1; i <= 5; i++)
            {
                float age = Math.Max(0, progress - i * .047f);
                Vector2 tail = Vector2.Lerp(source, destination, Ease(age)) - Vector2.UnitY * (35 * MathF.Sin(age * MathF.PI));
                frame.Sprite(tail, 19 - i * 2, 27 - i * 3, Shape.Wisp, i % 2 == 0 ? 0x9b1631 : 0xe63139,
                    .78f - i * .09f, sortY: Vector2.Lerp(from, to, Ease(age)).Y);
            }
        }
        else if (spell == Spell.ThunderBolt)
        {
            Channel(frame, source, destination, 14, 3.7f, .96f, time, 3, 42, depths: new(from.Y, to.Y));
            frame.Sprite(destination, 35, 41, Shape.Flash, White, .82f, Layer.EmissiveCore, light: true, sortY: to.Y);
        }
        else
            Lightning(frame, source, destination, time, new(from.Y, to.Y));
    }

    private static void Lightning(Frame frame, Vector2 source, Vector2 destination, float time, Vector2? depths = null)
    {
        Channel(frame, source, destination, 33, 5.3f, 1, time, 5, 0, depths: depths);
        Vector2 direction = destination - source;
        if (direction.LengthSquared() >= 36)
        {
            Vector2 normal = Vector2.Normalize(new Vector2(-direction.Y, direction.X));
            Channel(frame, source + normal * 3, destination - normal * 6, 32, 3.5f, .91f, time + .13f, 3, 105, depths: depths);
            Channel(frame, source - normal * 5, Vector2.Lerp(source, destination, .76f) + normal * 13,
                28, 2.4f, .72f, time + .064f, 2, 207, depths: depths);
        }
        frame.Sprite(source, 61, 43, Shape.Orb, Blue, .66f, light: true);
        frame.Sprite(source, 22, 24, Shape.Flash, White, .86f, Layer.EmissiveCore);
    }

    // The 2D path, tapered branches and filtered halo adapt ARCFLUX Lite by gennai (MIT).
    // See Data/ParticleCredits.txt. Coarse points survive each flicker, with new fine detail.
    internal static void Channel(Frame frame, Vector2 source, Vector2 end, float spread, float width, float alpha,
        float time, int forks, int offset, Layer layer = Layer.Emissive, ArcColours? colours = null, Vector2? depths = null)
    {
        Vector2 delta = end - source;
        float length = delta.Length();
        if (length < 2) return;
        Vector2 heading = delta / length;
        int count = 1 << Math.Clamp((int)MathF.Ceiling(MathF.Log2(length / 18)), 2, 5);
        int flash = (int)(time * 13);
        Span<Vector2> points = stackalloc Vector2[count + 1];
        Displace(frame, points, source, end, spread, offset, flash);
        ArcColours palette = colours ?? StormColours;
        Layer core = layer == Layer.Ground ? Layer.Ground : Layer.EmissiveCore;
        float thread = Math.Clamp(width * .63f, .9f, 3.2f);
        for (int i = 1; i <= count; i++)
        {
            float step = i / (float)count;
            float decay = (1 - .55f * step) * (.75f + frame.Unit(offset + i * 3 + flash * 113) * .25f);
            float? sortY = depths is { } path ? path.X + (path.Y - path.X) * ((i - .5f) / count) : null;
            Vector2 segment = points[i] - points[i - 1];
            Vector2 centre = (points[i - 1] + points[i]) * .5f;
            float angle = MathF.Atan2(segment.Y, segment.X);
            float glowSize = (8 + width * 3.7f) * decay;
            frame.Sprite(centre, segment.Length() + glowSize * .82f, glowSize * 1.38f,
                Shape.Wisp, palette.Shadow, alpha * .67f, layer == Layer.Ground ? layer : Layer.Glow, angle, sortY: sortY);
            frame.Sprite(centre, segment.Length() + glowSize * .59f, glowSize,
                Shape.Wisp, palette.Glow, alpha * (.48f + .23f * decay), layer, angle, light: i % 3 == 1, sortY: sortY);
            float thickness = thread * (.34f + decay * .78f);
            frame.Thread(points[i - 1], points[i], thickness, thickness * (.78f + .20f * decay),
                i % 4 == 0 ? palette.Filament : palette.Core, alpha * (.63f + .37f * decay), core, sortY: sortY);
            if (i % 3 == 0)
                // Fine raster lightning detail rides each jagged section.
                // The bright authored core stays readable at low F12 glow.
                frame.Sprite(centre, segment.Length() + 9, 8 + width * 1.9f,
                    Shape.UserLightning, i % 2 == 0 ? palette.Core : palette.Filament,
                    alpha * .53f * decay, core, angle, sortY: sortY);
            if (i % 3 == 1)
            {
                float shift = (frame.Unit(i + offset + flash * 51) - .5f) * 22;
                Vector2 spark = points[i] + new Vector2(-heading.Y, heading.X) * shift;
                frame.Sprite(spark, 5 + 4 * decay, 7 + 6 * decay, Shape.Spark, palette.Filament,
                    alpha * (.34f + decay * .61f), core, angle, sortY: sortY);
            }
        }

        for (int i = 0; i < forks; i++)
        {
            int seed = offset + 1391 + i * 37;
            float distribution = (i + .20f + frame.Unit(seed) * .65f) / forks;
            int index = Math.Clamp((int)(distribution * count), 1, count - 1);
            float remaining = length * (1 - index / (float)count);
            Vector2 tangent = Vector2.Normalize(points[index + 1] - points[index - 1]);
            Vector2 forward = Vector2.Normalize(heading * .8f + tangent * .2f);
            Vector2 side = new(-forward.Y, forward.X);
            float reach = Math.Min(remaining * .64f, 57);
            if (reach < 3) continue;
            float? sortY = depths is { } path ? path.X + (path.Y - path.X) * (index / (float)count) : null;
            Fork(frame, points[index], forward, side, reach, width * .69f, alpha * .81f, seed, layer,
                split: width >= 4.5f && i % 2 == 0, colours: palette, sortY: sortY);
        }
    }

    private static void Fork(Frame frame, Vector2 source, Vector2 heading, Vector2 side, float length, float width,
        float alpha, int seed, Layer layer, bool split = false, ArcColours? colours = null, float? sortY = null)
    {
        float angle = (.51f + .56f * frame.Unit(seed + 1)) * (frame.Unit(seed) < .5f ? -1 : 1);
        Vector2 forward = heading * MathF.Cos(angle) + side * MathF.Sin(angle);
        float reach = length * (.53f + .47f * frame.Unit(seed + 2));
        Span<Vector2> points = stackalloc Vector2[5];
        Displace(frame, points, source, source + forward * reach, reach * .32f, seed + 3, 0);
        ArcColours palette = colours ?? StormColours;
        for (int j = 1; j < points.Length; j++)
        {
            float progress = (j - .5f) / (points.Length - 1);
            float taper = 1 - progress * progress * .95f;
            float thickness = Math.Clamp(width * .72f * taper, .55f, 2.25f);
            frame.Thread(points[j - 1], points[j], thickness * 2.45f, thickness * 1.18f,
                palette.Glow, alpha * .46f * taper, layer, sortY: sortY);
            frame.Thread(points[j - 1], points[j], thickness, thickness * .66f,
                palette.Filament, alpha * taper, layer, sortY: sortY);
        }

        if (split && reach > 22)
            Fork(frame, points[2], forward, new Vector2(-forward.Y, forward.X), reach * .42f, width * .57f,
                alpha * .51f, seed + 617, layer, colours: palette, sortY: sortY);
    }

    private static void Displace(Frame frame, Span<Vector2> points, Vector2 source, Vector2 end, float spread, int seed, int flash)
    {
        int segments = points.Length - 1;
        points[0] = source;
        points[segments] = end;
        Vector2 delta = end - source;
        float length = delta.Length();
        Vector2 side = new Vector2(-delta.Y, delta.X) / length;
        float chaos = Math.Clamp(spread * 3.1f / length, .10f, .89f);
        for (int stride = segments, level = 0; stride > 1; stride >>= 1, level++)
        {
            float variation = chaos * (.34f + level * .16f);
            int detail = stride <= 4 ? flash * 317 : 0;
            for (int start = 0; start < segments; start += stride)
            {
                int index = start + stride / 2;
                Vector2 a = points[start], b = points[start + stride];
                float random = frame.Unit(seed + index * 19 + detail) + frame.Unit(seed + index * 19 + detail + 1) - 1;
                float bend = Math.Min(spread, Vector2.Distance(a, b) * variation);
                points[index] = (a + b) * .5f + side * (random * bend);
            }
        }
    }

    private static void ElectricWrap(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        float radius = prepare ? 31 : 24;
        float height = prepare ? 87 : 89;
        float drop = (prepare ? 16 : 43) * Ease(progress * 1.25f);
        float startHeight = height + 6 - drop;
        int flash = (int)(time * 19);
        for (int i = 0; i < 3; i++)
        {
            float sweep = (prepare ? 1.67f : 1.34f) * MathF.Tau * (.84f + frame.Unit(i + 9) * .34f);
            float spin = i * MathF.Tau / 3 + time * (prepare ? 8 : 5.4f) + frame.Unit(i + 18) * .91f;
            Vector2 from = Point(0, spin);
            for (int j = 1; j <= 23; j++)
            {
                float step = j / 23f;
                float angle = spin + step * sweep;
                Vector2 to = Point(step, angle);
                Layer layer = Depth(angle - sweep / 46);
                float opacity = (.91f - step * .20f) * (.69f + frame.Unit(j + i * 47 + flash * 21) * .31f);
                Vector2 middle = (from + to) * .5f;
                frame.Sprite(middle, Vector2.Distance(from, to) + 13, 8 + opacity * 6, Shape.Wisp, j % 4 == 0 ? 0x25348a : Blue,
                    opacity * .54f, layer, MathF.Atan2(to.Y - from.Y, to.X - from.X), light: j % 7 == 1);
                frame.Thread(from, to, 3.1f - step * 1.4f, 2.4f - step * 1.1f, j % 3 == 0 ? PaleBlue : White,
                    opacity, layer == Layer.Ground ? layer : Layer.EmissiveCore);
                if (j == 8 || j == 16)
                {
                    Fork(frame, to, Vector2.UnitY, Vector2.UnitX, 15, 1.8f, .76f, i * 29 + j + flash * 197, layer);
                    frame.Sprite(to, 15 + frame.Unit(flash * 23 + i) * 13, 16, Shape.Flash, PaleBlue, .81f,
                        layer == Layer.Ground ? layer : Layer.EmissiveCore);
                }
                from = to;
            }

            Vector2 Point(float step, float angle)
            {
                float cross = .69f + .33f * MathF.Sin(step * MathF.PI);
                float jitter = frame.Unit(flash * 181 + (int)MathF.Round(step * 23) + i * 37) - .5f;
                float lift = MathF.Max(2, startHeight - step * (height * (.82f + frame.Unit(i + 7) * .16f)));
                return feet + Ellipse(angle, radius * cross + jitter * 20, radius * .34f) - Vector2.UnitY * (lift + jitter * 11);
            }
        }
        frame.Sprite(feet - Vector2.UnitY * (startHeight * .47f), radius * 2.51f, height * .85f, Shape.Wisp,
            0x405bf2, .63f, light: true);
        frame.Sparks(feet - Vector2.UnitY * (startHeight * .45f), 23, radius * 1.19f, height * .46f, progress, PaleBlue);
    }

    private static void ThunderStrike(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        if (prepare)
        {
            Vector2 bottom = feet - Vector2.UnitY * 86;
            Channel(frame, bottom + new Vector2(-7, -68), bottom, 10, 2.6f, .83f, time, 2, 12);
            frame.Sprite(bottom - Vector2.UnitY * 15, 31, 71,
                Shape.UserLightning, Blue, .76f, Layer.EmissiveCore, light: true);
            return;
        }

        float power = 1 - Ease((progress - .38f) * 2.8f);
        if (power > .015f)
        {
            Vector2 head = feet + new Vector2(15, -931);
            Channel(frame, feet - Vector2.UnitY * 6, head, 42, 6.1f, power, time, 9, 22);
            Channel(frame, feet + new Vector2(-5, -15), head + new Vector2(17, 0), 55, 3.6f, power * .87f, time + .035f, 6, 417);
            Channel(frame, feet + new Vector2(4, -14), head - new Vector2(18, 0), 31, 3.3f, power * .94f, time, 4, 22);
            frame.Sprite(feet - Vector2.UnitY * 28, 34, 118,
                Shape.UserLightning, Blue, power * .79f, Layer.EmissiveCore, light: true);
            frame.Sprite(feet - Vector2.UnitY * 31, 46, 91, Shape.Flash, White, power * .90f, Layer.EmissiveCore, light: true);
        }
        frame.Sprite(feet - Vector2.UnitY * 7, 117, 36,
            Shape.UserLightning, Blue, .56f, Layer.Ground, light: true);
        for (int i = 0; i < 8; i++)
        {
            float angle = (i + frame.Unit(i)) * MathF.Tau / 8;
            Vector2 target = feet + Ellipse(angle, 61, 37) * (.42f + progress * .57f);
            Channel(frame, feet, target, 9, 2.4f, .8f, time, 1, 53 * i, Layer.Ground);
        }
        frame.Sparks(feet, 12, 74, 44, progress, PaleBlue, rise: 26);
    }

    private static void ThunderStorm(Frame frame, Vector2 feet, float progress, float time)
    {
        float radius = 129 * (.49f + .51f * Ease(progress * 3.4f));
        // Grounded strikes are made of independent bolts; never fill the
        // entire radius with an opaque blue disc.
        for (int i = 0; i < 13; i++)
        {
            float angle = (i + frame.Unit(i)) * MathF.Tau / 13 + progress * .71f;
            Vector2 spread = Ellipse(angle, radius, radius * FloorAspect);
            Channel(frame, feet + spread * .11f, feet + spread * (.64f + frame.Unit(i + 40) * .37f),
                20, 3.7f, .94f, time, 2, 71 * i, Layer.Ground);
            Vector2 root = feet + spread * (.25f + .36f * frame.Unit(i + 17));
            Vector2 flare = feet + Ellipse(angle + .19f, radius * .95f, radius * .57f) - Vector2.UnitY * (7 + 22 * frame.Unit(i + 31));
            Channel(frame, root, flare, 14, 2.0f, .75f, time, 1, 153 + i * 31, Depth(angle));
        }
        Vector2 body = feet - Vector2.UnitY * 34;
        frame.Sprite(body, 62, 91, Shape.UserLightning, PaleBlue,
            .69f, Layer.EmissiveCore, rotation: progress * .2f, light: true);
        frame.Sprite(body + new Vector2(3, -15), 25, 68,
            Shape.UserLightning, White, .88f * (1 - progress * .68f), Layer.EmissiveCore);
        frame.Sparks(feet - Vector2.UnitY * 13, 18, radius, radius * FloorAspect, progress, PaleBlue, rise: 21);
    }

    private static void StormEscape(Frame frame, Vector2 feet, float progress, float time)
    {
        float radius = 29 + 124 * Ease(progress * 1.81f);
        Vector2 centre = feet - Vector2.UnitY * 26;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * MathF.Tau / 12 + progress * 1.6f;
            Vector2 from = centre + Ellipse(angle, radius, radius * .77f);
            Vector2 to = centre + Ellipse(angle + .55f, radius, radius * .77f);
            Layer layer = Depth(angle + .26f);
            Channel(frame, from, to, 24, 5.3f, .92f, time, 1, i * 117, layer);
            frame.Sprite(from, 40, 76, Shape.UserLightning, i % 2 == 0 ? PaleBlue : Blue,
                .47f, layer, angle, light: i % 2 == 0);
            Vector2 edge = Ellipse(angle + frame.Unit(i) * .36f, 20 + 29 * frame.Unit(i + 19), 28);
            Channel(frame, from, from + edge, 14, 2.9f, .78f, time, 1, 291 + i * 57, layer);
        }
        // No orbit/ring billboard; moving arcs already define the storm. 
        frame.Sparks(centre, 16, radius * 1.07f, radius * .89f, progress, PaleBlue, rise: 17);
    }

    private static void StormArrival(Frame frame, Vector2 feet, float progress, float time)
    {
        Vector2 centre = feet - Vector2.UnitY * 41;
        float radius = 13 + 26 * Ease(progress * 2.4f);
        frame.Sprite(centre, 67, 86, Shape.UserLightning, Blue, .80f,
            Layer.Emissive, rotation: progress * .24f, light: true);
        frame.Sprite(centre, 39, 73, Shape.UserLightning, PaleBlue, .79f,
            Layer.EmissiveCore, rotation: -.18f + progress * .34f);
        frame.Sprite(centre, 41, 47, Shape.Flash, White, .86f * (1 - progress), Layer.EmissiveCore, light: true);
        for (int i = 0; i < 6; i++)
        {
            float angle = (i + frame.Unit(i) * .56f) * MathF.Tau / 6 + progress * 1.2f;
            Channel(frame, centre, centre + Ellipse(angle, radius, radius * .87f), 11, 2.4f, .82f, time, 1, i * 173);
        }
    }
}
