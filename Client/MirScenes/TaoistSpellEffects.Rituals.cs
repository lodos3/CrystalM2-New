using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static partial class TaoistSpellEffects
{
    private static void Poison(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        Vector2 body = feet - Vector2.UnitY * (prepare ? 10 + Ease(progress) * 35 : 32);
        float bloom = .36f + .64f * Ease(progress * 3);
        int bodyTint = prepare ? 0x867a51 : 0xa5b637;
        frame.Clip(body, 88 * bloom, 80 * bloom, Shape.SmokeSequence, time * .53f, 0x506431, .71f, Layer.Smoke);
        frame.Sprite(body, 70 * bloom, 68 * bloom, Shape.Orb, bodyTint, .85f, light: true);
        for (int i = 0; i < 9; i++)
        {
            float age = Fract(progress * .76f + i * .21f);
            float angle = i * 2.39f + time * 2;
            Vector2 point = body + Ellipse(angle, 31 * bloom, 20 * bloom) * (.28f + age * .81f) -
                Vector2.UnitY * (age * 22);
            frame.Sprite(point, 13 + (1 - age) * 13, 11 + (1 - age) * 13, Shape.Wisp,
                i % 3 == 0 ? 0xe6d569 : bodyTint, .88f - .57f * age, Depth(angle));
            if (!prepare) frame.Sprite(point, 5 + frame.Unit(i) * 5, 6, Shape.Spark, Gold, .68f - age * .43f, Layer.EmissiveCore);
        }
        if (prepare)
            frame.Orbit(body, 11 * bloom, 6 * bloom, progress * 7, 4.6f, 0xe5d9a4, 2.1f, .76f);
    }

    private static void PaperBurst(Frame frame, Vector2 feet, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * 32;
        float spread = .45f + .55f * Ease(progress * 3.1f);
        frame.Sprite(feet, 113 * spread, 53 * spread, Shape.Orb, Ochre, .61f, Layer.Ground, light: true);
        frame.Clip(centre, 116 * spread, 130 * spread, Shape.FireSequence, .18f + progress * .51f, Amber, .94f, light: true);
        frame.Clip(centre + new Vector2(0, 6 - progress * 27), 130 * spread, 133 * spread, Shape.SmokeSequence,
            .31f + progress * .38f, Ochre, .58f, Layer.Smoke);
        frame.Sprite(centre - Vector2.UnitY * 5, 42 * spread, 38 * spread, Shape.Flash, Ivory,
            .97f * (1 - Ease(progress)), Layer.EmissiveCore);
        frame.Sparks(centre, 20, 51 * spread, 53 * spread, progress, Gold, 22);
    }

    private static void SummonStars(Frame frame, Vector2 feet, float progress, bool deva)
    {
        int dusk = deva ? 0x426d8c : InkBlue, glow = deva ? Mint : Blue;
        float lift = Ease(progress) * 55;
        frame.Sprite(feet, 74, 38, Shape.Wisp, dusk, .86f, Layer.Ground);
        for (int i = 0; i < (deva ? 5 : 3); i++)
        {
            float t = Math.Clamp(progress * 1.35f - i * .105f, 0, 1);
            float turn = progress * 8 + i * 2.34f;
            Vector2 star = feet + Ellipse(turn, 25 + i * 2, 13) - Vector2.UnitY * (10 + lift + i * 6);
            frame.Sprite(star, 27 + i * 4, 38, Shape.Wisp, i % 2 == 0 ? dusk : Teal, .92f, Depth(turn));
            frame.Sprite(star, 23 * (1 - t * .27f), 29, Shape.Flash, i == 0 ? Ice : glow, .91f, Layer.Emissive, turn, light: i == 0);
            frame.Sprite(star, 11, 11, Shape.Flash, White, .94f, Layer.EmissiveCore, -turn);
        }
        frame.Sparks(feet - Vector2.UnitY * (20 + lift), 16, 43, 22, progress, deva ? Gold : Mint, 27,
            deva ? Shape.Feather : Shape.Spark);
    }

    private static void Veil(Frame frame, Vector2 feet, float progress, float time, bool mass)
    {
        float width = mass ? 1.65f : 1;
        float landing = Ease((progress - .36f) / .36f);
        frame.Sprite(feet - Vector2.UnitY * (6 + 50 * (1 - landing)), 63 * width, 42, Shape.Wisp, 0xbcced6,
            .47f * (1 - landing * .65f), Layer.Glow, light: true);
        int puffs = mass ? 11 : 7;
        for (int i = 0; i < puffs; i++)
        {
            float age = (progress + .095f - frame.Unit(i + 71) * .21f) / (.48f + frame.Unit(i + 13) * .18f);
            float hit = Ease((age - .78f) / .62f);
            float opacity = Ease(age * 4.1f) * (1 - Ease((age - 1.05f) / .60f));
            if (opacity < .02f) continue;
            float turn = i * 2.39996f + .71f * progress;
            Vector2 basePoint = feet + ProjectGround(Ellipse(turn, 48, 42)) * (width * (.23f + .86f * hit));
            float lift = (57 + frame.Unit(i) * 56) * (1 - Ease(age)) + 4;
            Vector2 point = basePoint - Vector2.UnitY * lift + new Vector2(MathF.Sin(age * 4 + i) * (7 + 10 * hit), 0);
            Layer layer = hit > .70f ? Layer.Floor : MathF.Sin(turn) < 0 ? Layer.Ground : Layer.Smoke;
            float tilt = MathF.Cos(turn) * (.36f - .19f * hit);
            int tint = i % 3 == 0 ? 0xe5ecdf : i % 3 == 1 ? 0x9faebb : 0x838396;
            frame.Clip(point, (31 + frame.Unit(i + 28) * 24 + hit * 46) * width, 29 + frame.Unit(i + 4) * 24 - hit * 18,
                Shape.SmokeSequence, time * .68f + i * .17f, tint, .89f * opacity, layer, tilt, sortY: basePoint.Y);
            frame.Clip(point + new Vector2(tilt * 22, -9 * (1 - hit)), 31 + hit * 33, 24 - hit * 12,
                Shape.SmokeSequence, time * .93f + i * .28f, 0xd3d9d5, .56f * opacity, layer, -tilt, sortY: basePoint.Y);
        }
    }

    private static void Rune(Frame frame, Vector2 feet, float progress, Shape glyph, float width, float height,
        int dark, int tint, int core)
    {
        bool curse = glyph == Shape.CurseGlyph;
        float front = curse ? 1.14f : .16f + progress * 1.98f;
        float settled = Ease((front - .92f) / .20f);
        Vector2 centre = feet - Vector2.UnitY * 26;
        Vector2 scale = new(width * .5f, height * .5f);
        frame.Sprite(feet, width * .88f, height * .29f, Shape.Orb, dark, .59f * Ease(front), Layer.Floor);
        frame.Sprite(centre, width * .88f, height * .88f, Shape.Wisp, dark, .59f * settled, Layer.Glow);
        frame.Sprite(centre, width, height, glyph, tint, .90f * settled);
        frame.Sprite(centre, width, height, glyph, core, .32f * settled, Layer.EmissiveCore);
        int strokeIndex = 0;
        foreach (Vector4 stroke in SpellParticleAtlas.GlyphStrokes(glyph))
        {
            int index = strokeIndex++ * 9;
            Vector2 start = new(stroke.X, stroke.Y), end = new(stroke.Z, stroke.W);
            float first = BurnOrder(start), last = BurnOrder(end);
            if (last < first) { (first, last) = (last, first); (start, end) = (end, start); }
            if (front < first) continue;
            Vector2 from = start * scale, to = end * scale;
            float reveal = Math.Clamp((front - first) / MathF.Max(.001f, last - first), 0, 1);
            Vector2 edge = Vector2.Lerp(from, to, reveal);
            frame.Thread(centre + from, centre + edge, height * .055f, height * .037f, tint, .91f * (1 - settled));
            frame.Thread(centre + from, centre + edge, height * .024f, height * .017f, core, .76f * (1 - settled),
                Layer.EmissiveCore);
            int flames = Math.Clamp((int)(Vector2.Distance(from, to) / 14), 1, 9);
            for (int i = 0; i < flames; i++, index++)
            {
                float step = (i + .16f + frame.Unit(index) * .68f) / flames;
                if (step > reveal) break;
                Vector2 point = centre + Vector2.Lerp(from, to, step);
                float heat = 1 - Ease((front - (first + (last - first) * step)) / .32f);
                float age = Fract(progress * 1.87f + frame.Unit(index + 52));
                float rise = (12 + frame.Unit(index + 13) * 42) * (.56f + age * .57f + heat * .54f);
                float lean = (frame.Unit(index + 35) - .5f) * .47f;
                float flare = (.65f + .35f * MathF.Sin(age * MathF.PI)) * (.58f + .42f * heat);
                frame.Clip(point - Vector2.UnitY * (rise * .34f), 16 + frame.Unit(index + 74) * 13, rise,
                    Shape.FlameSequence, age + progress, tint, .87f * flare, rotation: lean);
                frame.Sprite(point - Vector2.UnitY * (rise * .18f), 8 + frame.Unit(index + 3) * 7, rise * .61f,
                    Shape.FlameTongue, core, .83f * flare, Layer.EmissiveCore, lean, light: index % 8 == 0);
                if (index % 3 == 0)
                {
                    Vector2 ember = point + new Vector2(lean * (22 + 32 * age), -rise * .66f - age * 32);
                    frame.Sprite(ember, 2 + 4 * frame.Unit(index + 41), 5, Shape.Spark,
                        core, .92f * MathF.Sin(age * MathF.PI), Layer.EmissiveCore, lean);
                    frame.Sprite(point, 28, 36 + rise * .5f, Shape.Wisp, tint, .25f + heat * .31f,
                        Layer.Glow, lean, light: index % 9 == 0);
                }
                if (curse && index % 5 == 0)
                    frame.Clip(point + new Vector2(lean * 38, -rise * .73f - age * 32), 32 + age * 24, 25 + age * 18,
                        Shape.SmokeSequence, age, index % 2 == 0 ? 0x62727b : dark, .53f * MathF.Sin(age * MathF.PI), Layer.Smoke, lean);
            }
            if (reveal < 1)
                frame.Sprite(centre + edge, 21, 29, Shape.Flash, core, .78f, Layer.EmissiveCore, light: true);
        }

        static float BurnOrder(Vector2 point) => .5f + (point.X + point.Y) * .25f;
    }

    private static void Reveal(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        Vector2 centre = feet - Vector2.UnitY * (53 + MathF.Sin(time * 4) * 5);
        float growth = .44f + .56f * Ease(progress * 3);
        int tint = prepare ? Jade : 0x54cbff;
        frame.Sprite(centre, 80 * growth, 76 * growth, Shape.Orb, Teal, .76f, light: true);
        frame.Sprite(centre, 50 * growth, 44 * growth, Shape.Orb, tint, .92f);
        frame.Sprite(centre + new Vector2(0, 3), 62 * growth, 30 * growth, Shape.SigilEye,
            prepare && progress > .58f ? Ice : Gold, .91f, Layer.Emissive, .08f * MathF.Sin(time * 8));
        frame.Sprite(centre - new Vector2(5, 6), 19 * growth, 18 * growth, Shape.Flash, White, .91f, Layer.EmissiveCore);
        for (int i = 0; i < 8; i++)
        {
            float angle = time * 7 + i * .83f;
            Vector2 point = centre + Ellipse(angle, 39 * growth, 29 * growth);
            frame.Thread(point, point + Ellipse(angle + 1.1f, 10, 13), 2.7f, .55f, prepare ? InkBlue : 0xdc6cf4, .84f, Depth(angle));
            frame.Sprite(point, 6, 9, Shape.Spark, i % 2 == 0 ? Ice : Mint, .81f);
        }
    }

    private static void Repulse(Frame frame, Vector2 feet, float progress)
    {
        float open = Ease(progress * 1.45f);
        Vector2 body = feet - Vector2.UnitY * 35;
        frame.Sprite(feet, 87 + open * 61, 36, Shape.Orb, 0x687f85, .58f, Layer.Ground, light: true);
        for (int i = 0; i < 3; i++)
        {
            float wave = Math.Clamp(open * 1.28f - i * .13f, 0, 1);
            float width = 32 + wave * 122;
            float height = 51 + wave * 60;
            frame.Sprite(body, width, height, Shape.WardBack, 0x526976, .43f, Layer.Ground);
            frame.Sprite(body, width, height, Shape.WardFront, 0xdce5e5, .76f - wave * .23f);
        }
        frame.Sparks(body, 12, 40 + open * 35, 31, progress, Ice, 4);
    }

    private static void Cleanse(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        float travel = Ease(progress);
        Vector2 head = feet - Vector2.UnitY * (prepare ? 20 + travel * 87 : 13 + (1 - travel) * 103);
        frame.Sprite(feet, 95, 47, Shape.Orb, InkBlue, .55f, Layer.Ground);
        frame.Sprite(head, 49, 62, Shape.Orb, Blue, .82f, light: true);
        frame.Sprite(head, 23, 26, Shape.Orb, White, .96f, Layer.EmissiveCore);
        for (int i = 0; i < 17; i++)
        {
            float age = Fract(progress * 1.3f + frame.Unit(i));
            float offset = (frame.Unit(i + 7) - .5f) * (20 + age * 16);
            Vector2 point = head + new Vector2(offset, 9 + (prepare ? 1 : -1) * age * 54);
            frame.Thread(point, point + new Vector2(-offset * .28f, prepare ? 15 : -18), 2.2f, .38f,
                i % 3 == 0 ? Mint : Blue, .86f - age * .44f);
        }
        frame.Sparks(feet - Vector2.UnitY * 7, 10, 40 * travel, 23 * travel, progress, Ice, 28);
        if (prepare) return;
        for (int i = 0; i < 7; i++)
        {
            float turn = i * MathF.Tau / 7 + progress * 3.2f;
            Vector2 root = feet + Ellipse(turn, 25, 14);
            Vector2 top = Vector2.Lerp(root, head, .81f) + Vector2.UnitX * (MathF.Cos(turn) * 12);
            Vector2 edge = root + new Vector2(MathF.Sin(turn) * 5, -39 - frame.Unit(i) * 21);
            frame.Thread(root, edge, 3.1f, 1.4f, Blue, .84f, Depth(turn));
            frame.Thread(edge, top, 1.4f, .37f, Mint, .80f * (1 - progress), Depth(turn));
            frame.Sprite(edge, 14, 29, Shape.Wisp, Blue, .55f, Depth(turn), turn * .2f);
        }
    }

    private static void MassHeal(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        float bloom = .44f + .56f * Ease(progress * 2.6f);
        float radius = (prepare ? 46 : 77) * bloom;
        Vector2 centre = feet - Vector2.UnitY * (13 + (prepare ? 48 : 15) * Ease(progress));
        frame.Sprite(feet, radius * 2.45f, radius * 1.44f, Shape.Orb, Teal, .51f, Layer.Ground);
        HealMotes(frame, centre, radius * 1.05f, progress, prepare ? 44 : 76);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * MathF.PI * .5f + progress * 4;
            Vector2 node = centre + Ellipse(angle, radius, radius * GroundAspect);
            Layer depth = Depth(angle);
            frame.Sprite(node, 43, 38, Shape.Wisp, i % 2 == 0 ? Jade : Gold, .78f, depth, angle, light: true);
            frame.Sprite(node, 54, 44, Shape.Orb, i % 2 == 0 ? 0x44a4a5 : Amber, .44f, depth, light: true);
            frame.Sprite(node, 24, 23, Shape.Flash, Ivory, .98f, depth == Layer.Ground ? depth : Layer.EmissiveCore, angle);
            for (int j = 0; j < 14; j++)
            {
                int seed = i * 14 + j;
                float trail = angle - j * .093f;
                float spread = radius + (frame.Unit(seed + 62) - .5f) * (12 + j * 1.4f);
                Vector2 point = centre + Ellipse(trail, spread, spread * GroundAspect) - Vector2.UnitY * (frame.Unit(seed) * 19);
                frame.Sprite(point, 3 + frame.Unit(seed + 45) * 8, 7, Shape.Spark,
                    j % 3 == 0 ? Ice : j % 4 == 0 ? Gold : Mint, .94f - j * .046f, Depth(trail), trail + j * .7f);
            }
        }
    }

    private static void HealMotes(Frame frame, Vector2 centre, float radius, float progress, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float age = Fract(progress * 1.24f + frame.Unit(i + 271));
            float turn = i * 2.39996f + progress * (3.6f + 2.1f * frame.Unit(i + 71));
            float spread = radius * (.48f + frame.Unit(i + 113) * .62f + age * .19f);
            Vector2 root = centre + ProjectGround(Ellipse(turn, spread, spread));
            Vector2 point = root - Vector2.UnitY * (age * (12 + 24 * frame.Unit(i + 31)));
            float size = 1.6f + 3.7f * frame.Unit(i + 173);
            float shimmer = MathF.Sin(age * MathF.PI) * (.56f + .44f * MathF.Pow(MathF.Sin(age * 8 + i), 2));
            Layer depth = Depth(turn);
            int tint = i % 5 == 0 ? Gold : i % 2 == 0 ? Mint : Ice;
            frame.Sprite(point, size, size * 1.19f, i % 6 == 0 ? Shape.Flash : Shape.Spark, tint,
                .98f * shimmer, depth == Layer.Ground ? depth : Layer.EmissiveCore, turn);
            if (i % 7 == 0)
                frame.Sprite(point, size * 4.6f, size * 3.5f, Shape.Orb, tint, .44f * shimmer, depth, light: true);
        }
    }
}
