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
        float opening = Ease(progress * 2.4f);
        frame.Sprite(feet, 96 + 47 * opening, 44 + 23 * opening, Shape.Halo, dusk,
            .78f * (1 - Ease((progress - .72f) / .28f)), Layer.Ground, light: true);
        frame.Sprite(feet, 74, 38, Shape.Wisp, dusk, .86f, Layer.Ground);
        frame.Sprite(feet - Vector2.UnitY * (11 + lift * .30f), 64, 74, Shape.Orb, glow,
            .37f * (1 - Ease((progress - .67f) / .33f)), Layer.Glow, light: true);
        for (int i = 0; i < (deva ? 5 : 3); i++)
        {
            float t = Math.Clamp(progress * 1.35f - i * .105f, 0, 1);
            float turn = progress * 8 + i * 2.34f;
            Vector2 star = feet + Ellipse(turn, 25 + i * 2, 13) - Vector2.UnitY * (10 + lift + i * 6);
            frame.Sprite(star, 27 + i * 4, 38, Shape.Wisp, i % 2 == 0 ? dusk : Teal, .92f, Depth(turn));
            frame.Sprite(star, 23 * (1 - t * .27f), 29, Shape.Flash, i == 0 ? Ice : glow, .91f, Layer.Emissive, turn, light: i == 0);
            frame.Sprite(star, 11, 11, Shape.Flash, White, .94f, Layer.EmissiveCore, -turn);
        }
        frame.Sparks(feet - Vector2.UnitY * (20 + lift), 20, 43, 22, progress, deva ? Gold : Mint, 27,
            deva ? Shape.Feather : Shape.Spark);
        for (int i = 0; i < 10; i++)
        {
            float age = Fract(progress * 1.2f + frame.Unit(i + 173));
            float turn = i * 2.39996f + progress * 5;
            Vector2 point = feet + Ellipse(turn, 23 + 13 * age, 12 + 7 * age) -
                Vector2.UnitY * (age * (34 + lift) + 9);
            frame.Sprite(point, 3 + frame.Unit(i + 31) * 6, 7 + age * 9,
                i % 4 == 0 ? Shape.Flash : Shape.Spark, i % 3 == 0 ? Ice : glow,
                .85f * (1 - age), Layer.EmissiveCore, turn, light: i == 0);
        }
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
        // On reaching the ground the concealment breaks into loose floor-hugging
        // fragments instead of finishing as a spherical cloud.
        float ground = Ease((progress - .29f) / .44f);
        float dustFade = 1 - Ease((progress - .70f) / .30f);
        for (int i = 0; i < (mass ? 11 : 8); i++)
        {
            float turn = i * 2.39996f + frame.Unit(i + 205) * .55f;
            float age = Fract(progress * .91f + frame.Unit(i + 113));
            Vector2 point = feet + ProjectGround(Ellipse(turn, (18 + i * 3.2f) * width * ground,
                (14 + i * 2.8f) * width * ground));
            frame.Sprite(point, (24 + age * 23) * width, 8 + age * 7, Shape.Wisp,
                i % 3 == 0 ? 0xd8ddd9 : 0x879caa, .36f * ground * dustFade * (1 - age * .45f),
                Layer.Floor, turn * .23f);
            if (i % 3 == 0)
                frame.Sprite(point - Vector2.UnitY * (2 + age * 5), 4 + age * 4, 6,
                    Shape.Spark, 0xe5e9e6, .47f * dustFade * ground, Layer.Emissive);
        }
    }

    private static void Rune(Frame frame, Vector2 feet, float progress, Shape glyph, float width, float height,
        int dark, int tint, int core)
    {
        bool curse = glyph == Shape.CurseGlyph;
        float opening = Ease(progress * 2.8f);
        float reveal = Ease((progress + .05f) / .54f);
        float formed = Ease((progress - .15f) / .32f);
        float fade = 1 - Ease((progress - .78f) / .22f);
        float pulse = .83f + .17f * MathF.Sin(progress * MathF.Tau * 3);
        Vector2 centre = feet - Vector2.UnitY * 26;
        // All strokes grow out from their own centres simultaneously. A corner-to-corner
        // burn order made ward formation look like an image being wiped on screen.
        Vector2 scale = new(width * (.34f + .16f * opening), height * (.34f + .16f * opening));

        // Curse keeps its dark, smoky foundation. Protective ward visuals are
        // exclusively generated by the glowing rune lines and their embers.
        if (curse)
        {
            frame.Sprite(feet, width * (.61f + .23f * opening), height * .28f,
                Shape.Orb, dark, .53f * fade, Layer.Floor);
            frame.Sprite(centre, width * (.79f + .19f * opening), height * (.75f + .22f * opening),
                Shape.Wisp, dark, .53f * formed * fade, Layer.Glow, light: true);
        }
        frame.Sprite(centre, width * (.68f + .32f * opening), height * (.68f + .32f * opening),
            glyph, tint, .84f * formed * fade);
        frame.Sprite(centre, width * (.69f + .31f * opening), height * (.69f + .31f * opening),
            glyph, core, .36f * formed * fade * pulse, Layer.EmissiveCore);

        int strokeIndex = 0;
        foreach (Vector4 stroke in SpellParticleAtlas.GlyphStrokes(glyph))
        {
            int index = strokeIndex++ * 17;
            Vector2 a = new(stroke.X, stroke.Y), b = new(stroke.Z, stroke.W);
            Vector2 middle = (a + b) * .5f;
            Vector2 from = centre + Vector2.Lerp(middle, a, reveal) * scale;
            Vector2 to = centre + Vector2.Lerp(middle, b, reveal) * scale;
            frame.Thread(from, to, height * .047f, height * .034f, tint, .85f * fade);
            float lineHeat = .70f + .30f * MathF.Sin(progress * MathF.Tau * 6f + index * .29f);
            frame.Thread(from, to, height * .020f, height * .016f, core, .79f * fade * lineHeat,
                Layer.EmissiveCore);
            if (!curse)
            {
                // Stationary glowing filaments breathe in brightness, rather
                // than sweeping sideways along each letter of the rune.
                float heat = .56f + .44f * MathF.Sin(progress * MathF.Tau * 5f + index * .27f);
                frame.Thread(from, to, height * .013f, height * .010f,
                    core, (.45f + .35f * heat) * formed * fade, Layer.EmissiveCore,
                    light: index % 4 == 0);
            }

            // Vertical combustion is attached to fixed points on each stroke.
            // Each spark is born at a line segment, rises, then disappears.
            int flames = Math.Clamp((int)(Vector2.Distance(a * scale, b * scale) / 20), 2, 6);
            for (int i = 0; i < flames; i++)
            {
                int seed = index + i;
                float step = (i + .3f + .4f * frame.Unit(seed)) / flames;
                Vector2 root = Vector2.Lerp(from, to, step);
                float age = Fract(progress * (1.04f + frame.Unit(seed + 9) * .26f) + frame.Unit(seed + 52));
                float sway = MathF.Sin(progress * MathF.Tau * 6f + seed * .67f) * 1.7f;
                float rise = age * (23 + 19 * frame.Unit(seed + 13));
                float opacity = MathF.Sin(MathF.PI * age) * fade;
                Vector2 flame = root + new Vector2(sway, -rise);
                float height = 23 + 21 * frame.Unit(seed + 74);
                float width = 10 + 8 * frame.Unit(seed + 37);

                // A textured flame folds and evaporates upward from each line.
                frame.Clip(flame - Vector2.UnitY * height * .20f, width, height,
                    Shape.FlameSequence, progress * 1.3f + age * .63f + frame.Unit(seed + 89),
                    tint, .64f * opacity, Layer.Emissive, sway * .023f);
                if (!curse)
                    frame.Sprite(flame - Vector2.UnitY * height * .27f,
                        width * 1.4f, height * 1.10f,
                        Shape.UserFlame, index % 3 == 0 ? core : tint,
                        .61f * opacity, Layer.Emissive, sway * .018f);
                frame.Sprite(flame - Vector2.UnitY * height * .18f,
                    width * .55f, height * .83f, Shape.FlameTongue, core,
                    .73f * opacity, Layer.EmissiveCore, sway * .015f, light: seed % 11 == 0);

                if (!curse)
                {
                    frame.Sprite(root + new Vector2(sway * .38f, -rise * 1.07f),
                        2.5f + frame.Unit(seed + 41) * 3f, 6 + frame.Unit(seed + 43) * 7,
                        Shape.UserEmber, core, .88f * opacity,
                        Layer.EmissiveCore, light: seed % 7 == 0);

                    if (seed % 3 == 0)
                    {
                        frame.Sprite(root - Vector2.UnitY * (rise + 8), 9, 12,
                            Shape.Flash, tint, .32f * opacity,
                            Layer.EmissiveCore, light: true);
                        frame.Clip(flame - Vector2.UnitY * (height * .78f + age * 13),
                            16 + age * 11, 17 + age * 11, Shape.SmokeSequence,
                            progress * .7f + age, dark, .19f * opacity, Layer.Smoke);
                    }
                }
                else
                {
                    if (seed % 2 == 0)
                        frame.Sprite(flame - Vector2.UnitY * 4, 4 + frame.Unit(seed + 3) * 4,
                            9 + age * 12, Shape.Flash, core, .75f * opacity,
                            Layer.EmissiveCore, light: seed % 6 == 0);
                }
            }
            if (progress < .67f)
            {
                frame.Sprite(from, 14, 18, Shape.Flash, core, .69f * (1 - formed) * fade, Layer.EmissiveCore);
                frame.Sprite(to, 14, 18, Shape.Flash, core, .69f * (1 - formed) * fade, Layer.EmissiveCore);
            }
        }
        if (curse) frame.Sparks(centre, 23, width * .41f, height * .34f, progress, core, 37);
        if (curse)
        {
            frame.Sprite(centre, width * .76f, height * .78f, Shape.Wisp, 0x31554c,
                .38f * fade, Layer.Glow, light: true);
            for (int i = 0; i < 9; i++)
            {
                float age = Fract(progress * .82f + frame.Unit(i + 141));
                float angle = i * 2.39996f + progress * 3.1f;
                Vector2 point = centre + Ellipse(angle, width * (.10f + age * .24f),
                    height * (.15f + age * .18f)) - Vector2.UnitY * (age * 29);
                frame.Clip(point, 25 + age * 31, 29 + age * 23, Shape.SmokeSequence,
                    age + progress, i % 2 == 0 ? dark : 0x597d6c,
                    .65f * (1 - Ease((age - .62f) / .38f)) * fade, Layer.Smoke, angle * .18f);
                if (i % 3 == 0)
                    frame.Sprite(point, 17, 24, Shape.Wisp, Mint, .34f * fade * (1 - age),
                        Layer.Glow, light: true);
            }
        }
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
        frame.Sprite(centre, radius * 2.05f, radius * 1.55f, Shape.Wisp, Jade,
            .27f * (1 - Ease((progress - .72f) / .28f)), Layer.Glow, light: true);
        frame.Sprite(centre, radius * 1.35f, radius * 1.10f, Shape.Orb, Ice,
            .20f * (.8f + .2f * MathF.Sin(progress * MathF.Tau * 3)), Layer.Glow, light: true);
        HealMotes(frame, centre, radius * 1.05f, progress, prepare ? 50 : 84);
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
