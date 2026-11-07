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
        frame.Sprite(centre, 106, 68, Shape.Orb, Ochre, .54f, light: true);
        for (int i = 0; i < 6; i++)
        {
            float turn = i * MathF.Tau / 6 + progress * 5;
            Vector2 point = centre + Ellipse(turn, 49 * open, 31 * open);
            Layer layer = Depth(turn);
            frame.Sprite(point, 28 + i % 3 * 3, 18 + i % 2 * 5, Shape.Talisman, Ivory, fade, layer, turn + .3f);
            frame.Clip(point - Vector2.UnitY * 8, 18, 26, Shape.FlameSequence, progress * 1.8f + i * .21f,
                i % 2 == 0 ? 0xe84725 : Amber, .78f * fade, layer, turn * .19f);
            frame.Sprite(point, 31, 21, Shape.Wisp, Amber, .68f, layer);
            if (progress > .32f)
                for (int j = 0; j < 3; j++)
                {
                    Vector2 debris = point + Ellipse(turn + (j - 1) * .6f, 41 * progress, 31 * progress);
                    frame.Sprite(debris, 6, 8, Shape.Feather, j % 2 == 0 ? Ivory : Amber, .89f - j * .15f, layer, turn * 1.4f);
                }
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

    private static void Cloud(Frame frame, Vector2 feet, float time)
    {
        float bloom = .44f + .56f * Ease(time * 1.9f);
        float pulse = .81f + .19f * MathF.Sin(time * 3.1f);
        // Each spell object is one affected tile. These overlapping small puffs
        // make one cloud bank when the server sends the whole area.
        frame.Sprite(feet, 82 * bloom, 56 * bloom, Shape.Orb, 0x39741d, .49f, Layer.Floor, light: true);
        for (int i = 0; i < 4; i++)
        {
            float turn = i * 2.399f + time * .32f;
            Vector2 point = feet + Ellipse(turn, 30 * bloom, 17 * bloom) * (.28f + frame.Unit(i) * .62f) -
                Vector2.UnitY * (8 + 15 * frame.Unit(i + 11));
            frame.Clip(point, (54 + 18 * frame.Unit(i + 2)) * bloom, (37 + frame.Unit(i + 3) * 23) * bloom,
                Shape.SmokeSequence, time * .31f + i * .19f, i % 3 == 0 ? 0xb0cf56 : 0x598739, .66f * pulse,
                MathF.Sin(turn) < 0 ? Layer.Ground : Layer.Smoke, turn * .26f);
        }
        frame.Sprite(feet - Vector2.UnitY * 19, 62 * bloom, 45 * bloom, Shape.Orb, 0xb3df59, .49f * pulse, light: true);
        frame.Sparks(feet, 5, 33 * bloom, 19 * bloom, time * .37f, 0xcbe871, 26);
    }

    private static void Shield(Frame frame, Vector2 feet, float progress, bool aura)
    {
        Vector2 centre = feet - Vector2.UnitY * 35;
        float spread = aura ? 0 : 31 * (1 - Ease(progress * 2.6f));
        float flicker = .85f + .15f * MathF.Sin(progress * MathF.Tau);
        frame.Sprite(centre, 100, 113, Shape.Wisp, 0x444f9c, .62f, Layer.Ground);
        for (int side = -1; side <= 1; side += 2)
        {
            int colour = side < 0 ? Blue : Gold;
            int rim = side < 0 ? 0x4357d0 : Amber;
            Vector2 offset = centre + new Vector2(side * spread, 0);
            Vector2 from = offset + Ellipse(-1.27f, side * 46, 55);
            for (int i = 1; i <= 16; i++)
            {
                float t = i / 16f;
                float taper = MathF.Sin(t * MathF.PI);
                Vector2 to = offset + Ellipse(-1.27f + t * 2.54f, side * 46, 55);
                Vector2 midpoint = (from + to) * .5f;
                frame.Sprite(midpoint, 19 + taper * 15, 19 + taper * 9, Shape.Wisp, rim, .69f * flicker,
                    Layer.Glow, light: i % 5 == 1);
                frame.Sprite(midpoint + Vector2.UnitX * side * (4 + frame.Unit(i + side * 29) * 6), 17 + taper * 16,
                    10 + taper * 9, Shape.FlameTongue, rim, .74f * taper, Layer.Emissive, side * 1.35f + (t - .5f));
                frame.Thread(from, to, 1.4f + 2.6f * taper, 1.1f + 2 * taper, colour, .91f * flicker,
                    Layer.EmissiveCore);
                if (i % 4 == 1)
                    frame.Sprite(to + Vector2.UnitX * side * 3, 12, 18, Shape.Flash, side < 0 ? Ice : Ivory,
                        .64f * flicker, Layer.EmissiveCore, t * 1.6f);
                from = to;
            }
        }
    }

    private static void Pet(Frame frame, Vector2 feet, float progress, float time, bool aura)
    {
        Vector2 centre = feet - Vector2.UnitY * (aura ? 91 : 48);
        if (!aura)
        {
            float open = .57f + .43f * Ease(progress * 2.7f);
            frame.Sprite(feet, 102 * open, 46 * open, Shape.Orb, Ochre, .75f, Layer.Ground);
            frame.Clip(centre, 112 * open, 159 * open, Shape.FlameSequence, .13f + progress * .72f, Amber, .93f, light: true);
            frame.Sparks(centre, 17, 43 * open, 52 * open, progress, Gold, 38);
            return;
        }
        float flicker = .79f + .21f * MathF.Sin(progress * MathF.Tau);
        frame.Sprite(centre, 77, 82, Shape.Orb, 0xf44b18, .84f * flicker, light: true);
        frame.Sprite(centre, 60, 81, Shape.ArmourGlyph, Amber, .88f);
        frame.Sprite(centre, 48, 67, Shape.Claw, Gold, .69f, Layer.EmissiveCore, .14f * MathF.Sin(time));
        frame.Sparks(centre, 7, 31, 39, progress, Ivory, 20);
    }

    private static void HealingCircle(Frame frame, Vector2 feet, float progress, float time, bool field, float fieldSize)
    {
        float opening = Ease((field ? time : progress) * 4);
        float width = (field ? fieldSize : 396) * (.42f + opening * .58f);
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
