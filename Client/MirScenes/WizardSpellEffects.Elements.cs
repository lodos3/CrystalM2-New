using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using static Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static partial class WizardSpellEffects
{
    private static void FireCharge(Frame frame, Vector2 feet, float progress, Vector2 facing)
    {
        Vector2 hand = feet + facing * (6 + progress * 8) - Vector2.UnitY * 48;
        for (int i = 0; i < 34; i++)
        {
            int seed = 173 + i * 37;
            float age = Fract(progress * (1.11f + frame.Unit(seed + 3) * .69f) +
                frame.Unit(seed + 5));
            float theta = frame.Unit(seed + 7) * MathF.Tau + progress * 3.1f;
            float radius = (6 + 18 * frame.Unit(seed + 11)) * (1f - progress * .33f);
            Vector2 point = hand + Ellipse(theta, radius, radius * .73f) -
                Vector2.UnitY * (age * 19);
            float fade = MathF.Sin(age * MathF.PI);
            frame.Sprite(point, 4 + frame.Unit(seed + 13) * 8,
                8 + frame.Unit(seed + 17) * 13,
                i % 4 == 0 ? Shape.UserEmber : Shape.UserFlame,
                i % 5 == 0 ? Hot : i % 3 == 0 ? Gold : Fire,
                .83f * fade, i % 6 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                (frame.Unit(seed + 21) - .5f) * .32f, light: i % 19 == 0);
            if (i % 7 == 0)
                frame.Clip(point - Vector2.UnitY * 9, 13, 17,
                    Shape.SmokeSequence, progress + frame.Unit(seed + 23),
                    0x75402f, .21f * fade, Layer.Smoke);
        }
    }

    private static void GreatFireCharge(Frame frame, Vector2 feet, float progress, int accent)
    {
        Vector2 focus = feet - Vector2.UnitY * (21 + 43 * Ease(progress));
        for (int i = 0; i < 48; i++)
        {
            int seed = 227 + i * 29;
            float age = Fract(progress * (1.13f + frame.Unit(seed + 7) * .53f) +
                frame.Unit(seed + 11));
            float angle = frame.Unit(seed + 17) * MathF.Tau - progress * 5.6f;
            float radius = (8 + 30 * frame.Unit(seed + 19)) *
                (1f - .51f * Ease(progress));
            Vector2 point = focus + Ellipse(angle, radius, radius * .77f) -
                Vector2.UnitY * (age * 15);
            float fade = MathF.Sin(age * MathF.PI);
            frame.Sprite(point, 6 + frame.Unit(seed + 23) * 10,
                10 + frame.Unit(seed + 27) * 18,
                i % 5 == 0 ? Shape.UserEmber : Shape.UserFlame,
                i % 4 == 0 ? Hot : i % 2 == 0 ? accent : Fire,
                .82f * fade, i % 7 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                angle * .20f, light: i % 17 == 0);
            if (i % 8 == 0)
                frame.Clip(point - Vector2.UnitY * 9,
                    12 + frame.Unit(seed + 29) * 12, 15 + frame.Unit(seed + 31) * 12,
                    Shape.SmokeSequence, progress + frame.Unit(seed + 37),
                    0x762f18, .26f * fade, Layer.Smoke);
        }
    }

    private static void HellfireCharge(Frame frame, Vector2 feet, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * 49;
        frame.Orbit(centre, 37, 32, progress * 4.9f, 4.7f, Fire, 13, .77f);
        frame.Orbit(centre, 35, 30, progress * 4.9f + .26f, 3.4f, Gold, 6, .93f);
        for (int i = 0; i < 2; i++)
        {
            float angle = progress * 6.8f + i * MathF.PI;
            Vector2 point = centre + Ellipse(angle, 30, 17);
            frame.Sprite(point, 53, 58, Shape.Flame, Ember, .91f, Depth(angle), angle, light: true);
            frame.Sprite(point + new Vector2(2, 3), 31, 34, Shape.Flame, Gold, .81f, Depth(angle), angle - 1.1f);
        }
        frame.Sparks(centre, 11, 49, 43, progress, Gold, rise: 13);
        frame.Sprite(centre, 114, 88, Shape.Wisp, 0x852210, .67f, Layer.Glow);
    }

    private static void FireBurst(Frame frame, Vector2 centre, float progress, float diameter, bool fierce = false)
    {
        // Individually animated pockets of combustion; no oversized fire card.
        float radius = diameter * (.37f + .33f * Ease(progress * 2.7f));
        float fade = 1f - Ease((progress - .70f) / .30f);
        float time = progress * (fierce ? 1.58f : 1.12f);
        int count = fierce ? 72 : 58;
        for (int i = 0; i < count; i++)
        {
            int seed = i * 37 + 27;
            float age = Fract(progress * (1.05f + frame.Unit(seed + 2) * .59f) +
                frame.Unit(seed + 7));
            float x = (frame.Unit(seed + 13) - .5f) * radius * 2;
            float rise = age * (16 + frame.Unit(seed + 21) * 35);
            Vector2 point = centre + new Vector2(x, (frame.Unit(seed + 33) - .5f) * 28 - rise);
            int tint = i % 7 == 0 ? Hot : i % 3 == 0 ? Gold : fierce ? Ember : Fire;
            float alpha = MathF.Sin(age * MathF.PI) * fade;
            frame.Sprite(point, 4.5f + frame.Unit(seed + 39) * 10.5f,
                9 + frame.Unit(seed + 41) * 18, Shape.UserFlame,
                tint, alpha * .87f, i % 8 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                (frame.Unit(seed + 43) - .5f) * .35f, light: i % 19 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - Vector2.UnitY * (5 + age * 12),
                    3 + frame.Unit(seed + 47) * 5, 5 + frame.Unit(seed + 49) * 7,
                    Shape.UserEmber, Hot, alpha * .86f, Layer.EmissiveCore);
            if (i % 6 == 0)
                frame.Clip(point - Vector2.UnitY * (9 + age * 18),
                    11 + age * 13, 14 + age * 16, Shape.SmokeSequence,
                    time * .61f + frame.Unit(seed + 53),
                    fierce ? 0x461e15 : 0x534037, .34f * (1 - age) * fade,
                    Layer.Smoke);
        }
    }

    private static void SearingImpact(Frame frame, Vector2 centre, float progress, int accent)
    {
        float open = .50f + .50f * Ease(progress * 3f);
        float heat = 1f - Ease((progress - .65f) / .35f);
        for (int i = 0; i < 64; i++)
        {
            int seed = i * 41 + 107;
            float age = Fract(progress * (1.07f + frame.Unit(seed + 3) * .69f) +
                frame.Unit(seed + 7));
            float spread = (frame.Unit(seed + 11) - .5f) * 112 * open;
            Vector2 point = centre + new Vector2(spread + (frame.Unit(seed + 13) - .5f) * age * 9,
                (frame.Unit(seed + 15) - .5f) * 25 - age * (17 + frame.Unit(seed + 17) * 40));
            float alive = heat * MathF.Sin(age * MathF.PI);
            frame.Sprite(point, 4 + frame.Unit(seed + 19) * 11,
                9 + frame.Unit(seed + 21) * 17,
                Shape.UserFlame, i % 5 == 0 ? Hot : i % 2 == 0 ? accent : Gold,
                .87f * alive, i % 7 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                (frame.Unit(seed + 25) - .5f) * .39f, light: i % 23 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - Vector2.UnitY * 8, 3 + frame.Unit(seed + 27) * 6,
                    5 + frame.Unit(seed + 29) * 7,
                    Shape.UserEmber, i % 3 == 0 ? Hot : accent,
                    .78f * alive, Layer.EmissiveCore);
            if (i % 5 == 0)
                frame.Clip(point - Vector2.UnitY * (8 + age * 16),
                    12 + age * 14, 15 + age * 15,
                    Shape.SmokeSequence, progress * .77f + frame.Unit(seed + 31),
                    0x694032, .34f * (1 - age) * heat, Layer.Smoke);
        }
    }

    private static void Repel(Frame frame, Vector2 feet, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * 45;
        float radius = 23 + 38 * Ease(progress * 1.45f);
        frame.Sprite(centre, radius * 2.8f, radius * 1.78f, Shape.Orb, Ember, .35f, light: true);
        frame.Orbit(centre, radius, radius * .65f, 0, MathF.Tau, Ember, 22, .93f);
        frame.Orbit(centre, radius - 2, radius * .61f, progress * 1.7f, MathF.Tau, Fire, 15, .91f);
        frame.Sprite(feet, radius * 2.14f, radius * 1.17f, Shape.Wisp, 0x89311f, .71f, Layer.Ground);
        for (int i = 0; i < 11; i++)
        {
            float angle = i * MathF.Tau / 11 + progress * 2.1f;
            Vector2 arc = centre + Ellipse(angle, radius + (frame.Unit(i) - .5f) * 9, radius * .64f);
            frame.Sprite(arc, 17 + frame.Unit(i + 10) * 11, 20, Shape.Flame, i % 3 == 0 ? Gold : i % 3 == 1 ? Fire : Ember,
                .84f, Depth(angle), angle, light: i % 3 == 0);
        }
        frame.Sparks(centre, 12, radius, radius * .72f, progress, Fire);
    }

    // Every tongue has a floor root. Different widths, inclinations and rise phases keep a
    // line of Fire Wall cells from animating as identical flames or floating projectiles.
    internal static void Flames(Frame frame, Vector2 root, float width, float height, float time, float alpha, int index = 0,
        Layer layer = Layer.Emissive, float? sortY = null)
    {
        float phase = time * (.83f + frame.Unit(index + 21) * .31f) + frame.Unit(index + 52);
        float sway = MathF.Sin(time * 6 + frame.Unit(index + 7) * MathF.Tau) * .10f;
        frame.Sprite(root, width * 1.29f, width * .75f, Shape.Orb, Fire, alpha * .36f, Layer.Ground, light: true, sortY: sortY);
        // Pivot the flame at the lower edge of the clip to keep it on the floor.
        float column = height * 1.15f;
        Vector2 centre = root + new Vector2(MathF.Sin(sway), -MathF.Cos(sway)) * (column * .48f);
        frame.Clip(centre, width * 1.42f, column, Shape.FlameSequence, phase, Fire, alpha * .79f,
            layer, sway, light: true, sortY: sortY);
        frame.Sprite(centre - Vector2.UnitY * (height * .04f),
            width * 1.12f, column * .91f, Shape.UserFlame, Fire,
            alpha * (.77f + .12f * MathF.Sin(time * 8f + index)),
            layer, sway, light: true, sortY: sortY);
        frame.Clip(root + new Vector2(-width * .07f, -height * .63f), width * .61f, height * .80f,
            Shape.FlameSequence, phase + .49f, Ember, alpha * .81f, layer, sway * .65f, sortY: sortY);
        Layer core = layer == Layer.Ground ? Layer.Ground : Layer.EmissiveCore;
        frame.Clip(root + new Vector2(width * .10f, -height * .27f), width * 1.04f, height * .66f,
            Shape.FlameSequence, phase + .27f, Gold, alpha * .93f, core, sway * .5f, sortY: sortY);
        frame.Sprite(root - Vector2.UnitY * (height * .14f), width * .66f, height * .34f, Shape.Orb,
            Hot, alpha * .54f, core, sortY: sortY);
    }

    private static void FireWallCharge(Frame frame, Vector2 feet, float progress)
    {
        // The cast starts at the caster's feet, climbs the torso, gathers as
        // one overhead fire mass and releases an outward ember burst.
        float climb = Ease(progress / .68f);
        Vector2 overhead = feet - Vector2.UnitY * 113;
        Vector2 heart = Vector2.Lerp(feet, overhead, climb);
        float ignite = Ease(progress * 4.6f);
        float burst = Ease((progress - .82f) / .18f);
        for (int i = 0; i < 48; i++)
        {
            int seed = i * 37 + 63;
            float age = Fract(progress * (1.08f + frame.Unit(seed + 9) * .69f) +
                frame.Unit(seed + 13));
            float scatter = (frame.Unit(seed + 17) - .5f) * (24 - 9 * climb);
            float upward = age * (21 + 32 * frame.Unit(seed + 23));
            Vector2 source = feet + new Vector2(scatter * .80f,
                -(progress * 94 + upward));
            Vector2 point = Vector2.Lerp(source,
                heart + new Vector2(scatter * .55f, (frame.Unit(seed + 31) - .5f) * 23),
                climb * .90f);
            float alpha = ignite * (1f - age * .72f) * (1f - burst * .70f);
            frame.Sprite(point, 5 + frame.Unit(seed + 33) * 8,
                10 + frame.Unit(seed + 35) * 19, Shape.UserFlame,
                i % 5 == 0 ? Hot : i % 3 == 0 ? Gold : Fire,
                .86f * alpha, i % 7 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                (frame.Unit(seed + 41) - .5f) * .3f, light: i % 19 == 0);
        }

        // Compact layered flames read as a single fireball, not a 100px orb.
        if (progress >= .55f)
        {
            float gather = Ease((progress - .55f) / .22f) * (1 - burst);
            for (int i = 0; i < 25; i++)
            {
                int seed = i * 41 + 401;
                float angle = frame.Unit(seed + 7) * MathF.Tau + progress * 4;
                float radius = (6 + frame.Unit(seed + 11) * 17) * (1 - .36f * gather);
                Vector2 point = overhead + Ellipse(angle, radius, radius * .68f);
                frame.Sprite(point, 7 + frame.Unit(seed + 13) * 10,
                    10 + frame.Unit(seed + 17) * 16, Shape.UserFlame,
                    i % 4 == 0 ? Hot : i % 3 == 0 ? Gold : Fire,
                    gather * .93f, i % 5 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                    angle * .13f, light: i == 3);
            }
        }

        if (burst > .005f)
        {
            for (int i = 0; i < 36; i++)
            {
                int seed = i * 31 + 707;
                float theta = (i + frame.Unit(seed)) * MathF.Tau / 36;
                Vector2 ray = Ellipse(theta, 40 + 43 * frame.Unit(seed + 13),
                    24 + 28 * frame.Unit(seed + 19));
                Vector2 point = overhead + ray * burst -
                    Vector2.UnitY * (burst * frame.Unit(seed + 23) * 24);
                frame.Sprite(point, 4 + frame.Unit(seed + 29) * 7,
                    6 + frame.Unit(seed + 37) * 9,
                    i % 4 == 0 ? Shape.UserFlame : Shape.UserEmber,
                    i % 5 == 0 ? Hot : Gold,
                    .82f * (1 - burst * .52f), Layer.EmissiveCore,
                    theta, light: i % 13 == 0);
            }
            for (int i = 0; i < 5; i++)
                frame.Clip(overhead + new Vector2((i - 2) * 12, -12 - burst * 20),
                    17 + burst * 14, 19 + burst * 14, Shape.SmokeSequence,
                    progress + i * .17f, 0x655044,
                    .31f * burst * (1 - Ease((progress - .92f) / .08f)), Layer.Smoke);
        }
    }

    private static void FireWall(Frame frame, Vector2 feet, float time)
    {
        Flames(frame, feet, 51, 156, time, 1);
        Flames(frame, feet + new Vector2(-7, -2), 19, 83, time, .57f, 3, Layer.Ground);
        float smoke = Fract(time * .66f + frame.Unit(34));
        frame.Clip(feet + new Vector2((frame.Unit(12) - .5f) * 21, -90 - smoke * 31), 54 + smoke * 18, 52,
            Shape.SmokeSequence, smoke, 0x615046, .28f * MathF.Sin(smoke * MathF.PI), Layer.Smoke, .16f);
        frame.Sparks(feet - Vector2.UnitY * 24, 10, 23, 24, time * 1.2f, Gold, rise: 102);
    }

    private static void FireBang(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        if (prepare)
        {
            Vector2 centre = feet - Vector2.UnitY * 28;
            frame.Sprite(centre, 53, 50, Shape.Flame, Gold, .89f, Layer.EmissiveCore, light: true);
            for (int i = 0; i < 12; i++)
            {
                float angle = (i + frame.Unit(i + 9) * .4f) * MathF.Tau / 12 + progress * .3f;
                Vector2 ray = Ellipse(angle, 83, 55) * (.47f + .57f * Ease(progress * 2.2f));
                frame.Line(centre + ray * .15f, centre + ray, 15, Fire, .78f, Depth(angle), light: i % 3 == 0);
                frame.Line(centre + ray * .42f, centre + ray * .94f, 7, Gold, .95f, Depth(angle));
                frame.Sprite(centre + ray * .93f, 21, 18, Shape.Flame, Gold, .83f, Depth(angle), angle);
            }
            frame.Sparks(centre, 17, 83, 54, progress, Hot);
            return;
        }

        float spread = .68f + .32f * Ease(progress * 3);
        frame.Sprite(feet - Vector2.UnitY * 1, 184 * spread, 122 * spread, Shape.Orb, Ember, .73f, Layer.Ground, light: true);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * MathF.Tau / 12 + frame.Unit(i + 6) * .42f;
            float distance = (.27f + .78f * frame.Unit(i)) * spread;
            Vector2 root = feet + Ellipse(angle, 82, 51) * distance;
            float height = (65 + 73 * frame.Unit(i + 44)) * (1 - progress * .48f);
            Flames(frame, root, 41 + 24 * frame.Unit(i + 12), height, time, .91f, i, Depth(angle), root.Y);
            frame.Sprite(root - Vector2.UnitY * (height * .33f), 43, 57, Shape.Flame, i % 3 == 0 ? Hot : Gold,
                .69f, Depth(angle) == Layer.Ground ? Layer.Ground : Layer.EmissiveCore, i * .71f, light: i % 3 == 0, sortY: root.Y);
        }
        frame.Sparks(feet - Vector2.UnitY * 18, 19, 95, 54, progress, Gold, rise: 106);
    }

    private static void Disruptor(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        float height = (prepare ? 147 : 187) * (.7f + .36f * MathF.Sin(progress * MathF.PI));
        float width = prepare ? 79 : 102;
        frame.Sprite(feet - Vector2.UnitY * 3, width * 1.46f, width * .88f, Shape.Orb, Ember, .85f, Layer.Ground, light: true);
        Flames(frame, feet, width, height, time, .94f);
        frame.Clip(feet - Vector2.UnitY * (height * .58f), width * 1.39f, height * .74f,
            Shape.SmokeSequence, time * .83f + .16f, 0x603323, .53f, Layer.Smoke, -.27f);
        for (int i = 0; i < 3; i++)
        {
            float angle = time * 8 + i * 2.09f;
            Vector2 centre = feet - Vector2.UnitY * (20 + height * (i + 1) / 5);
            frame.Orbit(centre, width * (.53f - i * .12f), 21 - i * 4, angle, 3.4f, i == 1 ? Gold : Fire, 14, .66f);
        }
        for (int i = 0; i < 7; i++)
        {
            float angle = frame.Unit(i) * MathF.Tau;
            Vector2 root = feet + Ellipse(angle, width * .59f, width * .33f);
            frame.Sprite(root, 38, 49, Shape.Flame, Fire, .74f, rotation: angle + time * 1.3f);
        }
        frame.Sparks(feet - Vector2.UnitY * 24, 26, width * .83f, width * .43f, progress, Gold, rise: height * .74f);
    }

    private static void FlameField(Frame frame, Vector2 feet, float progress, float time)
    {
        float open = .46f + .54f * Ease(progress * 4.6f);
        float radius = 140 * open;
        float rotation = progress * 1.8f;
        frame.Sprite(feet, radius * 2.31f, radius * 1.43f, Shape.Orb, Ember, .42f, Layer.Ground, light: true);
        Seal(frame, feet, radius, radius * FloorAspect, Gold, rotation, .95f, 16);
        Seal(frame, feet, radius * .72f, radius * FloorAspect * .72f, Fire, -rotation, .74f, 8);
        frame.Sprite(feet, radius * 1.29f, radius * .87f, Shape.Orb, Gold, .75f, Layer.Ground, light: true);
        frame.Sprite(feet, radius * .81f, radius * .57f, Shape.SigilDisc, Hot, .97f, Layer.Ground);

        float upper = 1 - Ease((progress - .46f) * 3.2f);
        if (upper > 0)
        {
            Vector2 crown = feet - Vector2.UnitY * (105 + 24 * progress);
            Seal(frame, crown, radius * .56f, radius * .30f, Gold, -rotation, .86f * upper, 12, Layer.Emissive);
            frame.Sprite(crown, radius * 1.21f, radius * .76f, Shape.Orb, Fire, .71f * upper, light: true);
            frame.Sprite(crown, radius * .56f, radius * .48f, Shape.Orb, Hot, .83f * upper, Layer.EmissiveCore, light: true);
            for (int i = 0; i < 14; i++)
            {
                float angle = (i + frame.Unit(i + 13)) * MathF.Tau / 14;
                Vector2 ray = Ellipse(angle, radius * .85f, radius * .61f) - Vector2.UnitY * 33;
                frame.Line(crown + ray * .17f, crown + ray * (1 + frame.Unit(i + 39) * .49f),
                    5 + frame.Unit(i + 5) * 12, Gold, (.27f + .20f * frame.Unit(i + 1)) * upper);
            }
        }

        for (int i = 0; i < 10; i++)
        {
            float angle = i * MathF.Tau / 10 + rotation;
            Vector2 root = feet + Ellipse(angle, radius * (.59f + .28f * frame.Unit(i)), radius * .49f);
            float beat = Fract(progress * 1.56f + frame.Unit(i + 37) * .70f);
            float height = 43 + 107 * frame.Unit(i + 3);
            if (i % 3 == 0)
            {
                Vector2 falling = root - Vector2.UnitY * (height * (1 - beat));
                frame.Line(falling - Vector2.UnitY * (33 + 43 * upper), falling, 14, Gold, .81f * upper, Depth(angle), light: true);
            }
            Flames(frame, root, 32 + 21 * frame.Unit(i + 15), height * (.53f + .47f * MathF.Sin(beat * MathF.PI)),
                time, .91f, i, Depth(angle), root.Y);
        }
        frame.Sparks(feet - Vector2.UnitY * 10, 23, radius, radius * .64f, progress, Gold, rise: 105);
    }

    private static void Seal(Frame frame, Vector2 centre, float x, float y, int colour, float rotation,
        float alpha, int spokes, Layer layer = Layer.Ground)
    {
        frame.Sprite(centre, x * 2.25f, y * 2.25f, Shape.SigilDisc, colour, alpha, layer, light: true);
        frame.Sprite(centre, x * 2.67f, y * 2.67f, Shape.Halo, colour, alpha * .26f, layer);
        for (int i = 0; i < spokes; i++)
        {
            float angle = i * MathF.Tau / spokes + rotation;
            Vector2 spoke = Ellipse(angle, x, y);
            float inward = i % 2 == 0 ? .67f : .77f;
            Vector2 end = centre + spoke * inward;
            Vector2 fork = centre + Ellipse(angle + .11f, x, y) * .92f;
            frame.Line(centre + spoke, end, i % 2 == 0 ? 4.8f : 3.2f, colour, alpha * .9f, layer);
            if (i % 2 == 0) frame.Line(end, fork, 3.2f, colour, alpha * .84f, layer);
        }
    }

    private static void FrostCharge(Frame frame, Vector2 feet, float progress)
    {
        // Cold flame climbs like a blue torch, with shard flecks and vapour.
        Vector2 centre = feet - Vector2.UnitY * (34 + 19 * Ease(progress * 2));
        for (int i = 0; i < 32; i++)
        {
            int seed = 53 + i * 29;
            float age = Fract(progress * (1.13f + frame.Unit(seed + 3) * .63f) +
                frame.Unit(seed + 7));
            Vector2 point = centre + new Vector2((frame.Unit(seed + 11) - .5f) * 35,
                17 - age * (37 + frame.Unit(seed + 17) * 30));
            float alive = MathF.Sin(age * MathF.PI);
            int tint = i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice;
            frame.Sprite(point, 5 + frame.Unit(seed + 19) * 10,
                13 + frame.Unit(seed + 23) * 19,
                Shape.UserFlame, tint, .82f * alive,
                i % 4 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                (frame.Unit(seed + 27) - .5f) * .26f, light: i % 13 == 0);
            if (i % 3 == 0)
                frame.Sprite(point - Vector2.UnitY * 8, 3 + frame.Unit(seed + 37) * 4,
                    5 + frame.Unit(seed + 41) * 7,
                    Shape.UserEmber, IceWhite, .83f * alive, Layer.EmissiveCore);
            if (i % 6 == 0)
                frame.Clip(point - Vector2.UnitY * 11, 15, 20,
                    Shape.SmokeSequence, progress + frame.Unit(seed + 47),
                    0x5c7894, .29f * alive, Layer.Smoke);
        }
    }

    private static void FrostFireImpact(Frame frame, Vector2 centre, float progress)
    {
        // Impact returns to cold combustion rather than a large blue orb.
        float fade = 1f - Ease((progress - .74f) / .26f);
        for (int i = 0; i < 39; i++)
        {
            int seed = i * 43 + 119;
            float age = Fract(progress * (1.02f + frame.Unit(seed + 5) * .75f) +
                frame.Unit(seed + 7));
            float range = 10 + 36 * Ease(progress * 2.1f);
            Vector2 point = centre + new Vector2((frame.Unit(seed + 9) - .5f) * range * 2,
                (frame.Unit(seed + 13) - .5f) * 24 - age * (11 + frame.Unit(seed + 15) * 38));
            float strength = MathF.Sin(age * MathF.PI) * fade;
            frame.Sprite(point, 5 + frame.Unit(seed + 19) * 10,
                12 + frame.Unit(seed + 21) * 20,
                Shape.UserFlame, i % 6 == 0 ? IceWhite : i % 2 == 0 ? Ice : PaleBlue,
                .86f * strength, i % 4 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                (frame.Unit(seed + 25) - .5f) * .33f, light: i % 17 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - Vector2.UnitY * 9, 4 + frame.Unit(seed + 31) * 5,
                    7 + frame.Unit(seed + 37) * 7, Shape.Shard,
                    IceWhite, .72f * strength, Layer.EmissiveCore);
            if (i % 6 == 0)
                frame.Clip(point, 17 + age * 7, 19 + age * 10,
                    Shape.SmokeSequence, progress * .71f + frame.Unit(seed + 41),
                    0x587ba5, .29f * strength, Layer.Smoke);
        }
    }

    private static void IceBreak(Frame frame, Vector2 centre, float progress, float radius, int count)
    {
        float spread = .37f + .63f * Ease(progress * 1.9f);
        frame.Sprite(centre, radius * 2.35f, radius * 2.1f, Shape.Orb, Ice, .69f, light: true);
        frame.Clip(centre, radius * 2.28f, radius * 2.02f, Shape.SmokeSequence, progress * .75f + .18f, 0x506dab,
            .66f, Layer.Smoke, progress * 2.3f);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + frame.Unit(i + 25)) * MathF.Tau / count;
            Vector2 point = centre + Ellipse(angle, radius, radius * .88f) * spread;
            float length = (16 + frame.Unit(i) * 31) * (1 - .48f * progress);
            frame.Sprite(point, length * .65f, length, Shape.IceSpire, i % 3 == 0 ? IceWhite : Ice, .94f,
                Layer.Emissive, angle + MathF.PI / 2, light: i % 3 == 0);
            if (i % 2 == 0)
                frame.Sprite(point, length * .5f, length * .64f, Shape.FrostCore, IceWhite, .73f, Layer.EmissiveCore, angle);
        }
        frame.Sprite(centre, radius * 1.03f, radius * .93f, Shape.FrostCore, IceWhite, .96f * (1 - progress),
            Layer.EmissiveCore, progress * 3, light: true);
        frame.Sparks(centre, count, radius * 1.17f, radius * .87f, progress, IceWhite, rise: 21, shape: Shape.Shard);
    }

    private static void IceStorm(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        // Both cast and contact carry the same detailed runic seal, while
        // irregular spinning cold ribbons breathe small > large > small.
        float spread = prepare ? .62f : .90f;
        float wind = progress * (prepare ? 7.1f : 11.4f);
        float runeFade = prepare ? .74f : .57f * (1f - Ease((progress - .67f) / .33f));
        frame.Sprite(feet, 208 * spread, 133 * spread,
            Shape.SigilDisc, IceWhite, runeFade, Layer.Ground,
            rotation: progress * .31f, light: true, sortY: feet.Y);

        int ribbons = prepare ? 16 : 23;
        for (int i = 0; i < ribbons; i++)
        {
            int seed = 83 + i * 31;
            float age = Fract(progress * (1.02f + frame.Unit(seed + 3) * .73f) +
                frame.Unit(seed + 7));
            float lifeCurve = MathF.Sin(age * MathF.PI);
            if (lifeCurve < .02f) continue;
            float angle = frame.Unit(seed + 11) * MathF.Tau +
                wind * (.66f + frame.Unit(seed + 13) * .74f);
            float radius = (36 + 78 * frame.Unit(seed + 17)) * spread;
            Vector2 root = feet + ProjectGround(Ellipse(angle, radius, radius));
            float height = (20 + 55 * frame.Unit(seed + 19)) * spread;
            Vector2 centre = root - Vector2.UnitY * (height + age * 17);
            Vector2 tangent = ProjectGround(Ellipse(angle + MathF.PI / 2, 1, 1));
            float orientation = MathF.Atan2(tangent.Y, tangent.X) +
                progress * (i % 2 == 0 ? 2.7f : -2.2f);
            float width = (6 + 42 * lifeCurve) * spread;
            float thickness = (3 + 14 * lifeCurve) * spread;
            float alpha = .78f * lifeCurve;
            frame.Sprite(centre, width, thickness,
                Shape.UserSlash, i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice,
                alpha, Depth(angle), orientation, light: i % 9 == 0, sortY: root.Y);
            frame.Clip(centre - tangent * 5, width * .89f, thickness * 1.35f,
                Shape.SmokeSequence, progress * 1.47f + frame.Unit(seed + 27),
                0x82aabd, .39f * lifeCurve,
                Depth(angle), orientation + .17f, sortY: root.Y);

            // Snow is born on each swirling ribbon, travels briefly and fades.
            for (int snow = 0; snow < 4; snow++)
            {
                int flake = seed + snow * 29;
                float driftAge = Fract(age * 1.29f + frame.Unit(flake + 33));
                Vector2 point = centre + tangent *
                    ((frame.Unit(flake + 37) - .5f) * width * .75f) +
                    new Vector2((frame.Unit(flake + 41) - .5f) * driftAge * 12,
                        -driftAge * (7 + frame.Unit(flake + 43) * 19));
                frame.Sprite(point, 1.8f + frame.Unit(flake + 47) * 2.6f,
                    2.1f + frame.Unit(flake + 49) * 2.1f,
                    Shape.Spark, snow % 3 == 0 ? IceWhite : PaleBlue,
                    lifeCurve * (1 - driftAge) * .80f, Depth(angle),
                    orientation, sortY: root.Y);
            }
        }
        int dust = prepare ? 34 : 80;
        for (int i = 0; i < dust; i++)
        {
            int seed = i * 37 + 511;
            float age = Fract(progress * 1.71f + frame.Unit(seed + 3));
            float angle = frame.Unit(seed + 7) * MathF.Tau +
                wind * (.71f + frame.Unit(seed + 9) * .42f);
            float radius = (17 + frame.Unit(seed + 11) * 109) * spread;
            Vector2 root = feet + ProjectGround(Ellipse(angle, radius, radius));
            Vector2 point = root - Vector2.UnitY *
                ((13 + frame.Unit(seed + 13) * 114) * (1 - age));
            frame.Sprite(point, 1.5f + frame.Unit(seed + 17) * 2.8f,
                1.8f + frame.Unit(seed + 19) * 2.6f, Shape.Spark,
                i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice,
                .78f * MathF.Sin(age * MathF.PI), Depth(angle),
                wind * .13f, sortY: root.Y);
        }
    }

    private static void IceThrust(Frame frame, Vector2 feet, Vector2 facing, float progress, int extraReach)
    {
        Vector2 forward = GroundHeading(facing);
        var side = new Vector2(-forward.Y, forward.X);
        float reach = (192 + extraReach * 48) / MathF.Max(MathF.Abs(forward.X), MathF.Abs(forward.Y));
        float surge = .22f + .78f * Ease(progress * 3.4f);
        Vector2 chest = feet - Vector2.UnitY * 42;
        frame.Sprite(chest + facing * 8, 43, 39, Shape.Orb, Ice, .77f * (1 - Ease(progress * 1.9f)), light: true);

        for (int stream = -2; stream <= 2; stream++)
        {
            float breadth = stream * 22;
            float length = reach * surge * (1 - Math.Abs(stream) * .085f);
            Vector2 previous = chest;
            for (int sample = 1; sample <= 4; sample++)
            {
                float t = sample / 4f;
                float roll = MathF.Sin(t * 4.7f - progress * 11 + stream * 1.3f);
                Vector2 lane = forward * (length * t) + side * ((breadth + roll * 12) * t);
                Vector2 point = chest + ProjectGround(lane) + Vector2.UnitY * (t * (18 + roll * 8));
                Vector2 travel = point - previous;
                float angle = MathF.Atan2(travel.Y, travel.X);
                float taper = 1 - .13f * Math.Abs(stream) - .22f * t;
                float depth = feet.Y + ProjectGround(lane).Y;
                frame.Clip((point + previous) * .5f, travel.Length() + 32, 27 + t * 27, Shape.SmokeSequence,
                    progress * 1.7f + sample * .14f + stream * .2f, stream == 0 ? IceWhite : Ice,
                    .73f * taper, rotation: angle, light: stream == 0, sortY: depth);
                frame.Thread(previous, point, 2.9f - t, .85f, IceWhite, .56f * taper, Layer.EmissiveCore, sortY: depth);
                frame.Sprite(point - Vector2.UnitY * (4 + t * 12), 9 + t * 5, 7 + t * 4, Shape.Orb,
                    PaleBlue, .68f * taper, Layer.Emissive, sortY: depth);
                previous = point;
            }
            Vector2 front = feet + ProjectGround(forward * length + side * breadth);
            frame.Sprite(front, 67, 36, Shape.Smoke, Ice, .58f, Layer.Ground, light: stream == 0, sortY: front.Y);
            if (stream % 2 == 0)
            {
                Vector2 start = chest + ProjectGround(side * breadth * .08f);
                Channel(frame, start, front - Vector2.UnitY * 7, 14 + Math.Abs(stream) * 6, stream == 0 ? 2.9f : 1.8f,
                    stream == 0 ? .72f : .52f, progress * 1.3f, 2, 72 + stream * 29, depths: new(feet.Y, front.Y));
            }
            frame.Sparks(front - Vector2.UnitY * 7, 7, 37, 20, progress, IceWhite, rise: 46, shape: Shape.Orb, sortY: front.Y);
        }
    }

    private static void SnowContact(Frame frame, Vector2 feet, float progress, bool tile)
    {
        float lift = Ease(progress * 1.5f);
        Vector2 centre = feet + new Vector2(progress * 17, -(tile ? 12 : 36) - lift * 15);
        frame.Clip(centre, tile ? 50 : 57, tile ? 25 : 39, Shape.SmokeSequence, progress * .84f,
            IceWhite, tile ? .46f : .68f, tile ? Layer.Ground : Layer.Emissive, -.29f, light: true);
        frame.Sparks(centre, tile ? 3 : 6, 24, 19, progress, IceWhite, rise: 13, shape: Shape.Orb);
    }

    private static void SnowballWake(Frame frame, SpellParticlePool pool, Vector2 head, Vector2 heading,
        long now, int frameIndex, int remaining, bool emit)
    {
        float phase = now % 700 / 700f;
        for (int i = 0; i < 6; i++)
        {
            float fall = Fract(phase + frame.Unit(i + 18));
            Vector2 point = head - heading * (8 + frame.Unit(i + 30) * 23) +
                new Vector2((frame.Unit(i + 55) - .5f) * 41, 9 + 42 * fall);
            float size = 5 + frame.Unit(i + 62) * 5;
            frame.Sprite(point, size, size * 1.3f, Shape.Orb, IceWhite, .83f * MathF.Sin(fall * MathF.PI), Layer.EmissiveCore);
        }
        if (!emit) return;

        for (int i = 0; i < 4; i++)
        {
            int seed = frameIndex * 11 + i;
            Vector2 point = head - heading * (12 + frame.Unit(seed) * 25) + Vector2.UnitY * (8 + 12 * frame.Unit(seed + 73));
            Vector2 drift = new((frame.Unit(seed + 32) - .5f) * 22, 24 + frame.Unit(seed + 53) * 32);
            pool.TryAdd(new SpellParticle(point, drift, now, Math.Min(remaining, 130 + i * 25), 4 + i * 2,
                Tint(IceWhite, .84f), Shape.Orb, 0, Aspect: 1.16f, Growth: -.18f,
                Layer: Layer.EmissiveCore, Linear: true, CastsLight: false, DepthY: head.Y + 47));
        }
    }

    private static void Blizzard(Frame frame, Vector2 feet, float time)
    {
        float cycle = FieldDrop(time, .72f, frame.Unit(1));
        Vector2 root = feet + new Vector2((frame.Unit(20) - .5f) * 45, (frame.Unit(3) - .5f) * 29);
        float fall = 1 - Ease(cycle * 1.39f);
        float density = frame.Unit(31);
        float lull = 1 - Ease((time % 3 - 2.59f) / .37f);
        float warning = 1 - Ease(time / .44f);
        frame.Clip(root + new Vector2(-11, -22), 53, 25, Shape.SmokeSequence, time * .92f + density,
            0x8cb7eb, .74f * warning, Layer.Smoke, -.22f, sortY: root.Y);
        frame.Sprite(root + new Vector2(3, -16), 11, 17, Shape.Spark, IceWhite, .76f * warning,
            Layer.EmissiveCore, rotation: .31f, sortY: root.Y);
        if (fall > .004f && density > .26f && lull > .015f)
        {
            Vector2 head = root - new Vector2(58 + frame.Unit(7) * 89, 450 + frame.Unit(9) * 130) * fall;
            Vector2 tail = new Vector2(11 + frame.Unit(10) * 28, 62 + frame.Unit(17) * 113);
            float opacity = (.54f + density * .38f) * lull;
            frame.Line(head - tail, head, 24, Blue, opacity, light: true);
            frame.Line(head - tail * .71f, head, 9, IceWhite, opacity, Layer.EmissiveCore);
            frame.Clip(head - tail * .53f, 57, tail.Length() * .86f, Shape.SmokeSequence, cycle * .91f,
                0x6686cc, .66f * lull, Layer.Smoke, -.22f);
            frame.Sprite(head, 39, 53, Shape.Flame, Blue, opacity, rotation: -.27f);
            frame.Sprite(head, 24, 31, Shape.FrostCore, IceWhite, opacity, Layer.EmissiveCore, -.24f);
        }
        float land = Ease((time - .67f) / .13f);
        frame.Sprite(root - Vector2.UnitY * 6, 60, 40, Shape.Smoke, Ice, .53f * land * (.24f + .76f * lull), Layer.Ground, light: true);
        if (land > .8f && cycle > .63f)
        {
            float spread = Ease((cycle - .63f) / .34f);
            frame.Sprite(root - Vector2.UnitY * 13, 52, 33, Shape.Flash, IceWhite, .66f * (1 - spread) * lull, Layer.EmissiveCore);
            if (lull > .3f) frame.Sparks(root, 7, 28, 18, cycle, IceWhite, rise: 30, shape: Shape.Shard);
        }
    }

    private static void MeteorStrike(Frame frame, Vector2 feet, float time)
    {
        float phase = FieldDrop(time, .62f, frame.Unit(4));
        float density = frame.Unit(51);
        float warmup = .27f + .73f * Ease((time - .54f) / .26f);
        frame.Sprite(feet, 84, 47, Shape.Smoke, Ember, .59f * warmup, Layer.Ground, light: true);
        if (density < .32f) return;

        float fall = 1 - Ease(phase * 1.63f);
        if (fall > .004f)
        {
            Vector2 head = feet - new Vector2(65 + frame.Unit(5) * 61, 470 + frame.Unit(8) * 102) * fall;
            Vector2 tail = new(22 + frame.Unit(12) * 30, 116 + density * 102);
            frame.Line(head - tail, head, 32, Ember, .79f, light: true);
            frame.Line(head - tail * .61f, head, 17, Gold, .78f, Layer.EmissiveCore);
            frame.Clip(head - tail * .51f, 76, tail.Length(), Shape.SmokeSequence, phase * .89f,
                0x79331c, .74f, Layer.Smoke, -.29f);
            frame.Clip(head, 59, 76, Shape.FireSequence, phase * 1.52f, 0xef6026, .91f);
            frame.Sprite(head, 58, 67, Shape.UserRock, 0xdfb59a, .96f,
                Layer.Emissive, rotation: -.28f + MathF.Sin(time * 9f) * .17f);
            frame.Sprite(head + new Vector2(-4, 9), 47, 56, Shape.UserFlame,
                Gold, .91f, Layer.EmissiveCore, light: true);
        }
        else
        {
            float flash = 1 - Ease((phase - .61f) / .36f);
            frame.Sprite(feet, 65, 37, Shape.Flame, Gold, .76f * flash, Layer.Ground, light: true);
            Flames(frame, feet, 47, 71 * (1.4f - phase), time, .88f * flash);
            frame.Sparks(feet, 9, 45, 22, phase, Gold, rise: 61);
            for (int fragment = 0; fragment < 5; fragment++)
            {
                float age = Fract(phase * 1.7f + frame.Unit(fragment + 190));
                Vector2 debris = feet + new Vector2(
                    (frame.Unit(fragment + 200) - .5f) * (40 + 34 * age),
                    -9 - (1 - age) * (18 + frame.Unit(fragment + 205) * 30));
                frame.Sprite(debris, 11 + frame.Unit(fragment + 210) * 13,
                    13 + frame.Unit(fragment + 215) * 14,
                    Shape.UserRock, 0xad8c74, .78f * (1 - age * .57f),
                    Layer.Emissive, age * (fragment % 2 == 0 ? 1.5f : -1.5f));
            }
        }
    }

    private static void MeteorCharge(Frame frame, Vector2 feet, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * 36;
        for (int i = 0; i < 3; i++)
        {
            float angle = i * MathF.Tau / 3 + progress * 5.2f;
            Vector2 seal = centre + Ellipse(angle, 26, 16);
            frame.Sprite(seal, 42, 38, Shape.SigilDisc, Gold, .79f, Depth(angle), angle, light: true);
        }
        frame.Sparks(centre, 9, 40, 27, progress, Fire);
    }

    private static void MeteorSeal(Frame frame, Vector2 feet, float progress)
    {
        float open = .79f + .21f * Ease(progress * 9);
        float pulse = .84f + .16f * MathF.Sin(progress * 21);
        frame.Sprite(feet, 254 * open, 165 * open, Shape.Orb, Ember, .41f * pulse, Layer.Ground, light: true);
        Seal(frame, feet, 122 * open, 78 * open, Fire, progress * 1.67f, pulse, 12);
        frame.Sprite(feet, 176 * open, 114 * open, Shape.Glyph, Gold, .83f * pulse, Layer.Ground, rotation: progress * .34f);
    }

    private static float FieldDrop(float time, float contact, float variation)
    {
        // These fields get their tile packets before the server's 800 ms warmup ends.
        // Start with a descent, then stagger the repeated drops across neighboring tiles.
        float lead = .80f + variation * .29f;
        if (time <= lead) return time / lead * contact;
        float after = time - lead;
        return Fract(contact + after * (1.17f + variation * .55f) + variation * Ease(after * 2) * .30f);
    }
}
