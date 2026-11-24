using System.Numerics;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

// Directional effects are constructed from geometry. No legacy sprite plane,
// sprite decompression, or per-pose GPU texture is needed to render them.
internal static class SpellVfxPoseMasks
{
    internal static bool IsWeaponSwing(Spell spell) =>
        spell is Spell.Slaying or Spell.Thrusting or Spell.HalfMoon or Spell.CrossHalfMoon;

    internal static int Facing(Vector2 heading, int directions)
    {
        float turn = MathF.Atan2(heading.X, -heading.Y / SpellParticleFrame.GroundAspect);
        return ((int)MathF.Round(turn * directions / MathF.Tau) + directions) % directions;
    }

    internal static void DrawSwing(SpellParticleFrame frame, Spell spell, Vector2 feet,
        int direction, float progress)
    {
        float heading = (direction - 2) * MathF.PI / 4;
        float t = Math.Clamp(progress, 0, 1);
        float fade = .8f * MathF.Sin(MathF.PI * Math.Clamp(t + .12f, 0, 1));
        Vector2 centre = feet - Vector2.UnitY * 23;
        Vector2 forward = new(MathF.Cos(heading), MathF.Sin(heading) * .68f);
        if (spell == Spell.Thrusting)
        {
            Vector2 tip = centre + forward * (27 + 29 * t);
            frame.Thread(centre + forward * 5, tip, 3.3f, .6f, 0x94C9FF, fade, emitter: "Procedural.Swing.Thrust");
            frame.Thread(centre + forward * 13, tip, 1.2f, .3f, 0xF2FAFF, fade, Layer.EmissiveCore, emitter: "Procedural.Swing.ThrustCore");
            return;
        }
        if (spell == Spell.CrossHalfMoon)
        {
            DrawCrossHalfMoon(frame, feet, heading, t);
            return;
        }
        float span = spell == Spell.HalfMoon ? MathF.PI * 1.1f : MathF.PI * .67f;
        float radius = spell == Spell.HalfMoon ? 40 : 29;
        float leading = heading + span * (t - .42f);
        float trail = span * (.32f + .44f * t);
        const int segments = 9;
        Vector2 previous = centre + SpellParticleFrame.Ellipse(leading - trail, radius, radius * .68f);
        for (int i = 1; i <= segments; i++)
        {
            float u = i / (float)segments;
            Vector2 point = centre + SpellParticleFrame.Ellipse(leading - trail + trail * u, radius, radius * .68f);
            frame.Thread(previous, point, 2.2f * u, 3.1f * u, 0x81ABEB, fade * u,
                emitter: "Procedural.Swing.Wake");
            frame.Thread(previous, point, .7f * u, 1f * u, 0xF0F8FF, fade * u, Layer.EmissiveCore,
                emitter: "Procedural.Swing.Core");
            previous = point;
        }
        frame.Sprite(previous, 10, 8, Shape.Flash, 0xD6EEFF, fade, Layer.EmissiveCore,
            light: true, emitter: "Procedural.Swing.Tip");
    }

    // Six authored poses, corresponding to the original directional attack's
    // six frame slots. These are procedural geometry controls, not sprite masks.
    // Reuse the shared directional filament texture already used by Drake.
    private readonly record struct FanPose(float Turn, float Radius, float Width, float Lift, float Span, float Energy);

    private static readonly FanPose[] CrossMoonPoses =
    [
        new(-1.12f, 29, 13, 31, 1.05f, .24f),
        new(-.74f, 39, 22, 34, 1.56f, .55f),
        new(-.18f, 49, 28, 37, 2.13f, .83f),
        new( .43f, 57, 33, 39, 2.48f, 1.00f),
        new(1.02f, 64, 30, 34, 2.60f, .91f),
        new(1.57f, 69, 19, 29, 2.12f, .29f)
    ];

    private static void DrawCrossHalfMoon(SpellParticleFrame frame, Vector2 feet, float heading, float progress)
    {
        // Separate horizontal rotation from elevation. The projected ground
        // row determines which fragments are occluded by the body.
        float time = Math.Clamp(progress * 6, 0, 5);
        int first = (int)time;
        FanPose a = CrossMoonPoses[first], b = CrossMoonPoses[Math.Min(first + 1, 5)];
        float mix = SpellParticleFrame.Ease(time - first);
        float Blend(float x, float y) => x + (y - x) * mix;
        float turn = Blend(a.Turn, b.Turn);
        float radius = Blend(a.Radius, b.Radius);
        float width = Blend(a.Width, b.Width);
        float lift = Blend(a.Lift, b.Lift);
        float span = Blend(a.Span, b.Span);
        float energy = Blend(a.Energy, b.Energy);

        // Three separate, overlapping fans open at different heights and
        // angular offsets; the third is a narrower trailing cross-cut.
        const int segments = 14;
        for (int fan = 0; fan < 3; fan++)
        {
            float offset = fan == 0 ? 0 : fan == 1 ? 2.17f : -2.08f;
            float r = radius * (fan == 0 ? 1 : fan == 1 ? .87f : .74f);
            float w = width * (fan == 0 ? 1 : fan == 1 ? .83f : .55f);
            float h = lift + (fan == 0 ? 0 : fan == 1 ? 11 : -10);
            float arc = span * (fan == 2 ? .79f : 1);
            float power = energy * (fan == 0 ? 1 : fan == 1 ? .78f : .53f);
            float leading = heading + turn + offset;
            var prior = Edge(0);
            for (int i = 1; i <= segments; i++)
            {
                float u0 = (i - 1f) / segments, u1 = i / (float)segments;
                var next = Edge(u1);
                float depth = feet.Y + (prior.Depth + next.Depth) * .5f * frame.Scale;
                bool rear = depth < feet.Y;
                Layer body = rear ? Layer.Ground : Layer.Emissive;
                Layer core = rear ? Layer.Ground : Layer.EmissiveCore;
                float taper = MathF.Sin(MathF.PI * (u0 + u1) * .5f);
                float alpha = power * (.28f + .72f * taper) * (rear ? .70f : 1);
                Vector2 uv = new(u0, u1);

                frame.TexturedRibbon(new SpriteQuad(prior.Inner, next.Inner, prior.Outer, next.Outer),
                    uv, Shape.DirectionalSweep, 0x6B9DDD, alpha * .65f,
                    "Procedural.CrossHalfMoon.Veil", body, depth);
                frame.TexturedRibbon(new SpriteQuad(Vector2.Lerp(prior.Inner, prior.Outer, .38f),
                    Vector2.Lerp(next.Inner, next.Outer, .38f), prior.Outer, next.Outer),
                    uv, Shape.DirectionalSweep, 0xBDDFFF, alpha * .94f,
                    "Procedural.CrossHalfMoon.Filaments", body, depth);
                frame.Thread(prior.Rim, next.Rim, .45f + 1.3f * taper, .35f + 1.1f * taper,
                    0xF3FAFF, alpha * .84f, core, sortY: depth, emitter: "Procedural.CrossHalfMoon.Edge");

                // Small torn streaks rather than a large flash billboard.
                if (i % 4 == 2 && power > .3f)
                {
                    Vector2 drift = next.Rim - prior.Rim;
                    frame.Sprite(next.Rim, 4 + 4 * taper, 1.4f, Shape.WarriorSpark, 0xD5EDFF,
                        alpha * .7f, core, MathF.Atan2(drift.Y, drift.X), light: fan == 0 && i == 6,
                        sortY: depth, emitter: "Procedural.CrossHalfMoon.Sparks");
                }
                prior = next;
            }

            (Vector2 Inner, Vector2 Outer, Vector2 Rim, float Depth) Edge(float u)
            {
                float angle = leading - arc + arc * u;
                float taper = MathF.Pow(MathF.Max(0, MathF.Sin(MathF.PI * u)), .72f);
                float spread = 1.2f + w * taper;
                float reach = r * (.76f + .24f * u);
                Vector2 ground = SpellParticleFrame.Ellipse(angle, reach, reach * SpellParticleFrame.GroundAspect);
                float rise = h + 8 * MathF.Sin(angle - heading + fan * .45f) + 5 * taper;
                Vector2 rim = feet + ground - Vector2.UnitY * rise;
                Vector2 radial = SpellParticleFrame.Ellipse(angle, 1, SpellParticleFrame.GroundAspect);
                Vector2 inner = rim - radial * spread + Vector2.UnitY * (spread * .17f);
                Vector2 outer = rim + radial * (spread * .16f) - Vector2.UnitY * (spread * .1f);
                return (inner, outer, rim, ground.Y);
            }
        }
    }
}
