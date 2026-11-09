using System.Numerics;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static partial class TaoistSpellEffects
{
    private static void Trap(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        if (prepare)
        {
            float open = Ease(progress * 1.45f);
            Vector2 centre = feet - Vector2.UnitY * (14 + 49 * open);
            frame.Sprite(centre, 82, 71, Shape.Wisp, InkBlue, .83f, light: true);
            for (int i = 0; i < 2; i++)
            {
                float turn = progress * 6 + i * MathF.PI;
                Vector2 start = centre;
                for (int j = 1; j <= 15; j++)
                {
                    float t = j / 15f;
                    Vector2 end = centre + Ellipse(turn + t * 5, 36, 21) * (t * open);
                    frame.Thread(start, end, 2.1f * (.4f + t), 1.9f * (.4f + t),
                        j % 3 == 0 ? White : Ice, .74f, Depth(turn + t * 5));
                    if (j == 15) frame.Sprite(end, 22, 26, Shape.Flash, White, .83f, Layer.EmissiveCore, turn);
                    start = end;
                }
            }
            return;
        }
        float pulse = .31f + .69f * MathF.Sin(progress * MathF.PI);
        frame.Sprite(feet, 45, 27, Shape.Orb, InkBlue, .67f, Layer.Ground, light: true);
        Vector2 head = feet - Vector2.UnitY * (19 + 91 * pulse);
        frame.Sprite((head + feet) * .5f, 13, 100 * pulse, Shape.Wisp, Blue, .69f);
        frame.Thread(feet, head, 2.5f, .30f, Ice, .92f, Layer.EmissiveCore);
        frame.Sprite(feet, 21, 27, Shape.Flash, White, .91f, Layer.EmissiveCore, .22f);
        frame.Sparks(feet, 5, 17, 10, progress, Blue, 26);
    }

    private static void Phantom(Frame frame, Vector2 feet, float progress, float time)
    {
        float rise = Ease(progress) * 47;
        Vector2 centre = feet - Vector2.UnitY * (38 + rise);
        frame.Sprite(feet, 80, 38, Shape.Wisp, 0x4d4975, .82f, Layer.Ground);
        frame.Clip(centre, 72, 133, Shape.SmokeSequence, time * .76f, 0x625b80, .76f, Layer.Smoke);
        frame.Sprite(centre, 78, 146, Shape.SpiritFigure, 0x9183a7, .92f);
        frame.Sprite(centre - Vector2.UnitY * 4, 65, 135, Shape.SpiritFigure, 0xe4d9ef, .47f, light: true);
        frame.Sprite(centre - Vector2.UnitY * 36, 13, 11, Shape.Flash, Ice, .75f, Layer.EmissiveCore);
        frame.Sparks(centre, 10, 27, 57, progress, Violet, 26);
    }

    private static void Enhance(Frame frame, Vector2 feet, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * (23 + Ease(progress) * 29);
        float open = .40f + .60f * Ease(progress * 4);
        frame.Sprite(feet, 133 * open, 74 * open, Shape.Orb, Ochre, .69f, Layer.Ground, light: true);
        frame.Sprite(centre, 75 * open, 91 * open, Shape.Orb, Amber, .87f, light: true);
        frame.Sprite(centre - Vector2.UnitY * 14, 32 * open, 41 * open, Shape.Orb, Ivory, .91f, Layer.EmissiveCore);
        frame.Sprite(centre + Vector2.UnitY * 12, 26 * open, 38 * open, Shape.Orb, Gold, .91f, Layer.EmissiveCore);
        for (int i = 0; i < 17; i++)
        {
            float turn = (i + frame.Unit(i) * .71f) * MathF.Tau / 17 + progress;
            float reach = (42 + frame.Unit(i + 27) * 46) * open;
            Vector2 ray = Ellipse(turn, reach, reach * 1.16f);
            Vector2 point = centre + ray * (.55f + frame.Unit(i + 53) * .45f);
            float rotation = MathF.Atan2(ray.Y, ray.X);
            frame.Sprite(point, 24 + frame.Unit(i + 11) * 25, 12 + frame.Unit(i + 45) * 12, Shape.Wisp,
                i % 3 == 0 ? 0xd84b22 : 0xb1291c, .82f, Depth(turn), rotation);
            frame.Thread(centre + ray * .36f, point, 2.7f, .45f, i % 3 == 0 ? Gold : Amber, .76f, Depth(turn));
            if (i % 4 == 0)
                frame.Sprite(point, 18, 22, Shape.FireCurl, Gold, .94f, Layer.EmissiveCore, rotation);
        }
        if (progress > .61f)
            frame.Thread(centre + Vector2.UnitY * 18, centre - Vector2.UnitY * (135 * Ease((progress - .61f) / .39f)),
                3.4f, .35f, Ivory, .92f, Layer.EmissiveCore);
        frame.Sparks(centre, 27, 104 * open, 72 * open, progress, Gold, 22);
    }

    private static void Shinsu(Frame frame, Vector2 feet, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * (31 + progress * 18);
        float open = .60f + .40f * Ease(progress * 3);
        float fade = 1 - Ease((progress - .48f) / .44f);
        frame.Sprite(feet, 144 * open, 61 * open, Shape.Halo, Amber, .64f * fade,
            Layer.Ground, light: true);
        frame.Sprite(centre, 118, 92, Shape.Wisp, Ochre, .46f * fade, Layer.Glow, light: true);
        for (int i = 0; i < 6; i++)
        {
            float turn = i * MathF.Tau / 6 + progress * 5;
            Vector2 point = centre + Ellipse(turn, 49 * open, 31 * open);
            Layer layer = Depth(turn);
            frame.Sprite(point, 28 + i % 3 * 3, 18 + i % 2 * 5, Shape.Talisman, Ivory, fade, layer, turn + .3f);
            frame.Clip(point - Vector2.UnitY * 8, 18, 26, Shape.FlameSequence, progress * 1.8f + i * .21f,
                i % 2 == 0 ? 0xe84725 : Amber, .78f * fade, layer, turn * .19f);
            frame.Sprite(point, 31, 21, Shape.Wisp, Amber, .68f, layer);
            float ignite = .58f + .42f * MathF.Pow(MathF.Sin(progress * MathF.Tau * 1.8f - i), 2);
            frame.Sprite(point - Vector2.UnitY * 6, 26, 38, Shape.UserFlame,
                i % 2 == 0 ? Amber : Gold, .63f * ignite * fade, layer, turn * .15f, light: i % 2 == 0);
            if (progress > .32f)
                for (int j = 0; j < 3; j++)
                {
                    Vector2 debris = point + Ellipse(turn + (j - 1) * .6f, 41 * progress, 31 * progress);
                    frame.Sprite(debris, 6, 8, Shape.Feather, j % 2 == 0 ? Ivory : Amber, .89f - j * .15f, layer, turn * 1.4f);
                }
        }
        frame.Sparks(feet - Vector2.UnitY * 22, 21, 49 * open, 26 * open,
            progress, Gold, 53, Shape.Spark);
        for (int i = 0; i < 9; i++)
        {
            float age = Fract(progress * 1.45f + frame.Unit(i + 166));
            float turn = i * 2.39996f + progress * 4;
            Vector2 point = centre + Ellipse(turn, 25 + age * 34, 19 + age * 18) -
                Vector2.UnitY * (age * 36);
            frame.Sprite(point, 5 + 8 * (1 - age), 9 + 12 * (1 - age), Shape.Flash,
                i % 3 == 0 ? Ivory : Amber, .74f * fade * (1 - age * .45f),
                Layer.EmissiveCore, turn, light: i % 4 == 0);
        }
    }

    private static void Revive(Frame frame, Vector2 feet, float progress)
    {
        float lift = .55f + .45f * MathF.Sin(progress * MathF.PI);
        frame.Sprite(feet, 114, 54, Shape.Orb, InkBlue, .93f, Layer.Ground, light: true);
        frame.Sprite(feet, 81, 40, Shape.Halo, Ice, .77f, Layer.Ground);
        for (int i = -1; i <= 1; i += 2)
        {
            Vector2 root = feet + new Vector2(i * 16, 0);
            Vector2 centre = root - Vector2.UnitY * (65 * lift);
            frame.Sprite(centre + new Vector2(i * 11, 0), 68, 147 * lift, Shape.Pillar, InkBlue, .63f, Layer.Ground, i * .20f);
            frame.Sprite(centre, 29, 135 * lift, Shape.Wisp, Blue, .81f, Layer.Emissive, i * .16f, light: true);
            frame.Sprite(root - Vector2.UnitY * 4, 23, 34, Shape.Flash, White, .92f, Layer.EmissiveCore, i * .23f);
        }
        frame.Sparks(feet, 18, 42, 21, progress, Ice, 87);
    }

    private static void Plague(Frame frame, Vector2 feet, float progress, float time)
    {
        float open = .48f + .52f * Ease(progress * 3.7f);
        Vector2 centre = feet - Vector2.UnitY * 28;
        frame.Sprite(feet, 226 * open, 137 * open, Shape.Orb, 0x1933a6, .79f, Layer.Floor, light: true);
        for (int i = 0; i < 12; i++)
        {
            float turn = i * 2.39f + progress * 1.7f;
            float age = Fract(time * .94f + frame.Unit(i + 31) * .76f);
            float height = (48 + frame.Unit(i + 17) * 112) * open;
            Vector2 root = feet + Ellipse(turn, 110 * open, 66 * open) * (.37f + frame.Unit(i + 3) * .67f);
            Vector2 point = root + new Vector2(MathF.Cos(turn) * age * 17, -height * (.31f + age * .56f));
            Layer depth = Depth(turn);
            float lean = .23f * MathF.Cos(turn);
            frame.Clip(point, 48 + 29 * age, height, Shape.FlameSequence, age + time * .75f,
                i % 3 == 0 ? 0x4244d6 : 0x1778f6, .87f, depth, lean, sortY: root.Y);
            frame.Sprite(point - new Vector2(lean * 11, height * .23f), 31, 27, Shape.FireCurl,
                i % 3 == 0 ? Blue : 0x79dfff, .86f * (1 - age * .54f), depth, turn, light: i % 3 == 0, sortY: root.Y);
            frame.Sprite(point + Vector2.UnitY * (height * .13f), 17, height * .54f, Shape.FlameTongue,
                Ice, .71f * (1 - age * .67f), depth == Layer.Ground ? depth : Layer.EmissiveCore, lean, sortY: root.Y);
        }
        frame.Clip(centre, 208 * open, 160 * open, Shape.SmokeSequence, progress * .68f, InkBlue, .59f, Layer.Smoke);
        frame.Sparks(centre, 27, 104 * open, 58, progress, Ice, 61);
    }

    internal static void DrawPoisonField(CrystalM2.Rendering.Core.SpellParticlePool pool, Vector2 feet, long now, long start,
        float size, uint seed, uint field, Vector2 inward, int neighbours)
    {
        float time = (now - start) * .001f;
        if (time < 0) return;
        var frame = new Frame(pool, feet, now, 1, size, seed ^ (uint)Spell.PoisonCloud * 0x2d5bu, field);
        Cloud(frame, feet, time, inward, neighbours);
    }

    private static void Cloud(Frame frame, Vector2 feet, float time, Vector2 inward = default, int neighbours = 2)
    {
        float bloom = .56f + .44f * Ease(time * 1.8f);
        Vector2 drift = new(MathF.Sin(time * .63f + feet.Y * .024f) * 11,
            MathF.Cos(time * .77f + feet.X * .019f) * 6);
        Vector2 centre = feet + inward + drift;
        float coverage = .71f + neighbours * .071f;
        frame.Sprite(centre, 74 * bloom, 27 * bloom, Shape.MistFilament, 0x9bbf49, .76f * coverage,
            Layer.Floor, rotation: MathF.Sin(time + feet.X * .024f) * .39f, light: true);
        for (int i = 0; i < 5; i++)
        {
            float age = Fract(time * (.24f + .13f * frame.Unit(i + 34)) + frame.Unit(i + 59));
            float turn = i * 2.399f + time * .32f + frame.Unit(i + 11) * 1.24f;
            Vector2 root = centre + ProjectGround(Ellipse(turn, 29, 31)) * (.46f + .54f * age);
            Vector2 point = root - Vector2.UnitY * (5 + 15 * frame.Unit(i + 11) + age * 10);
            float opacity = coverage * (.28f + .47f * MathF.Sin(age * MathF.PI));
            frame.Clip(point, (72 + 36 * frame.Unit(i + 2)) * bloom, (26 + frame.Unit(i + 3) * 35) * bloom,
                Shape.SmokeSequence, age + frame.Unit(i + 8), i % 3 == 0 ? 0xb1c95a : i % 3 == 1 ? 0x628c39 : 0x2d532e, opacity,
                MathF.Sin(turn) < 0 ? Layer.Ground : Layer.Smoke, MathF.Sin(turn) * .27f, sortY: root.Y);
            if (i % 2 == 0)
                frame.Sprite(point, 54 + 26 * age, 21 + age * 14, Shape.MistFilament, 0xc9e26a, .82f * opacity,
                    Layer.Glow, turn * .11f, light: i == 0, sortY: root.Y);
        }
        frame.Sparks(centre, 4, 39 * bloom, 19 * bloom, time * .37f, 0xcbe871, 26, sortY: centre.Y);
    }

    private static void Shield(Frame frame, Vector2 feet, float progress, bool aura)
    {
        // Anchor the shell to the actor and send pulses along each curved rim.
        Vector2 centre = feet - Vector2.UnitY * 35;
        float spread = aura ? 0 : 31 * (1 - Ease(progress * 2.6f));

        for (int side = -1; side <= 1; side += 2)
        {
            int colour = side < 0 ? Blue : Gold;
            int rim = side < 0 ? Ice : Ivory;
            Vector2 offset = centre + new Vector2(side * spread, 0);

            Vector2 ShellPoint(float t) =>
                offset + Ellipse(-1.27f + t * 2.54f, side * 46f, 55f);

            Vector2 ElectricPoint(float t)
            {
                float angle = -1.27f + t * 2.54f;
                Vector2 tangent = new(-side * 46f * MathF.Sin(angle), 55f * MathF.Cos(angle));
                Vector2 normal = Vector2.Normalize(new Vector2(-tangent.Y, tangent.X));
                float noise = MathF.Sin(progress * MathF.Tau * 8f + t * 29f + side) * 2.5f +
                    MathF.Sin(progress * MathF.Tau * 13f - t * 51f + side * 1.7f) * 1.3f;
                // Fixed endpoints keep the bolt physically attached to the shell.
                return ShellPoint(t) + normal * (noise * MathF.Sin(t * MathF.PI));
            }

            const int segments = 24;
            for (int i = 1; i <= segments; i++)
            {
                float t0 = (i - 1) / (float)segments;
                float t1 = i / (float)segments;
                float taper = MathF.Sin(t1 * MathF.PI);
                Vector2 from = ShellPoint(t0), to = ShellPoint(t1);
                Vector2 arcFrom = ElectricPoint(t0), arcTo = ElectricPoint(t1);
                float wave = .5f + .5f * MathF.Cos(MathF.Tau * (progress * 2 + side * .12f - t1));
                float pulse = .62f + .38f * wave * wave;

                // Unchanging outline, with a separate high-frequency noisy core.
                frame.Thread(from, to, 1.7f + taper * 1.4f, 1.5f + taper * 1.4f,
                    colour, .77f * pulse, Layer.Emissive);
                frame.Thread(arcFrom, arcTo, 1.3f + taper * 1.4f, 1.0f + taper * 1.2f,
                    rim, .94f * pulse, Layer.EmissiveCore, light: i % 6 == 1);

                if (i % 4 == 1)
                {
                    Vector2 midpoint = (arcFrom + arcTo) * .5f;
                    frame.Sprite(midpoint, 13 + taper * 10, 8 + taper * 6,
                        Shape.Wisp, colour, .21f + .39f * wave * wave, Layer.Glow,
                        MathF.Atan2(arcTo.Y - arcFrom.Y, arcTo.X - arcFrom.X),
                        light: i % 8 == 1);
                    float tangent = MathF.Atan2(to.Y - from.Y, to.X - from.X);
                    frame.Sprite(midpoint + Vector2.UnitX * side * (4 + 4 * wave), 12 + taper * 9, 22 + taper * 16,
                        Shape.UserLightning, colour, .59f * pulse * taper, Layer.Emissive,
                        tangent - MathF.PI * .5f);
                    frame.Sprite(midpoint + Vector2.UnitX * side * 6, 34 * taper + 10, 16 + taper * 10,
                        Shape.MistFilament, side < 0 ? 0x3856cc : Amber, .51f * pulse * taper,
                        Layer.Glow, tangent, light: i % 8 == 5);
                }
            }

            // Sparks originate along the outline, not from an orbit around the
            // character. They eject a few pixels before fading away.
            for (int i = 0; i < 12; i++)
            {
                int seed = i + (side + 1) * 31;
                float age = Fract(progress * 2 + frame.Unit(seed + 179));
                float t = (.08f + frame.Unit(seed + 23) * .84f);
                Vector2 anchor = ElectricPoint(t);
                float drift = MathF.Sin(age * 5 + seed) * age * 7;
                Vector2 point = anchor + new Vector2(side * (4 + age * 11) + drift, -age * (22 + 27 * frame.Unit(seed + 71)));
                float alpha = .84f * MathF.Sin(age * MathF.PI);
                frame.Sprite(point, 3 + frame.Unit(seed + 29) * 4,
                    7 + frame.Unit(seed + 33) * 8,
                    i % 4 == 0 ? Shape.Flash : Shape.Spark, i % 2 == 0 ? rim : colour,
                    alpha, Layer.EmissiveCore, side * .19f, light: i % 6 == 0);
                if (i % 4 == 0)
                    frame.Sprite(point, 16 + age * 7, 12 + age * 12, Shape.Wisp, colour,
                        .36f * alpha, Layer.Glow, drift * .09f);
            }
        }
    }

    private static void Pet(Frame frame, Vector2 feet, float progress, float time, bool aura)
    {
        Vector2 centre = feet - Vector2.UnitY * (aura ? 78 : 48);
        if (!aura)
        {
            float open = .57f + .43f * Ease(progress * 2.7f);
            frame.Sprite(feet, 102 * open, 46 * open, Shape.Orb, Ochre, .75f, Layer.Ground);
            frame.Clip(centre, 112 * open, 159 * open, Shape.FlameSequence, .13f + progress * .72f, Amber, .93f, light: true);
            frame.Sparks(centre, 17, 43 * open, 52 * open, progress, Gold, 38);
            return;
        }
        // A small, shallow rune above the head: no giant vertical banner.
        float flicker = .84f + .16f * MathF.Sin(progress * MathF.Tau * 2f);
        frame.Sprite(centre, 35, 37, Shape.ArmourGlyph, Amber, .90f * flicker);
        frame.Sprite(centre, 23, 27, Shape.Claw, Gold, .73f * flicker,
            Layer.EmissiveCore, .06f * MathF.Sin(time * 3f));
        frame.Sprite(centre, 19, 22, Shape.Flash, Ivory, .31f * flicker,
            Layer.EmissiveCore, light: true);
        frame.Sparks(centre, 4, 13, 14, progress, Ivory, 10);
    }

    private static void HealingCircle(Frame frame, Vector2 feet, float progress, float time, bool field, float fieldSize)
    {
        float opening = Ease((field ? time : progress) * 4);
        float width = field ? fieldSize : 396 * (.42f + opening * .58f);
        float height = width * GroundAspect;
        float rotation = (field ? time : progress) * -.55f;
        Vector2 centre = feet - Vector2.UnitY * 1;
        float pulse = .84f + .16f * MathF.Sin(time * MathF.Tau / .88f);
        frame.Sprite(centre, width * 1.14f, height * 1.13f, Shape.Wisp, InkBlue, .84f, Layer.Floor, light: true);
        frame.FloorSeal(centre, width * 1.04f, 0, Shape.Trigrams, 0x4359e4, .63f);
        frame.FloorSeal(centre, width, 0, Shape.Trigrams, Blue, .95f, light: true);
        frame.FloorSeal(centre, width * .992f, 0, Shape.Trigrams, Ice, .89f * pulse);
        frame.FloorSeal(centre, width * .37f, rotation, Shape.YinYang, Ice, .93f, light: true);
        for (int i = 0; i < 16; i++)
        {
            float angle = i * MathF.PI / 8 + time * .22f;
            float age = Fract(time * .85f + frame.Unit(i));
            Vector2 point = feet + Ellipse(angle, width * .42f, height * .42f) * (.61f + age * .42f) -
                Vector2.UnitY * (age * 36);
            frame.Sprite(point, 4 + frame.Unit(i + 6) * 8, 12, Shape.Spark, i % 3 == 0 ? White : Blue, .89f - age * .53f,
                Depth(angle), -angle);
        }
    }
}
