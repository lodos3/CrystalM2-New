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
        Vector2 centre = feet + new Vector2(4, -45) + facing * (progress * 8);
        for (int i = 0; i < 2; i++)
        {
            float angle = progress * 5.7f + i * MathF.PI;
            Vector2 point = centre + Ellipse(angle, 22 - progress * 7, 16) - Vector2.UnitY * 6 * MathF.Sin(progress * MathF.PI);
            frame.Clip(point, 29, 27, Shape.FireSequence, progress + i * .5f, Fire, .87f, Depth(angle), angle, light: true);
            frame.Sprite(point, 18, 17, Shape.Orb, Hot, .98f, Depth(angle));
            frame.Line(point, point + Ellipse(angle - 1.37f, 14, 9), 7, Gold, .81f, Depth(angle));
        }
        frame.Sprite(centre, 81, 67, Shape.Wisp, 0x8b3116, .73f, Layer.Glow);
        frame.Sparks(centre, 9, 33, 26, progress, Gold, 11);
    }

    private static void GreatFireCharge(Frame frame, Vector2 feet, float progress, int accent)
    {
        Vector2 centre = feet - Vector2.UnitY * (8 + 32 * Ease(progress));
        float turn = -progress * 5.6f;
        frame.Sprite(centre, 89, 70, Shape.Orb, Ember, .63f, light: true);
        frame.Sprite(centre, 86, 76, Shape.FireCurl, Fire, .92f, rotation: turn);
        frame.Sprite(centre, 71, 67, Shape.FireCurl, accent, .93f, Layer.EmissiveCore, turn + .34f);
        frame.Clip(centre + new Vector2(-6, 9), 99, 86, Shape.SmokeSequence, progress * .74f, 0x762a18, .51f, Layer.Smoke);
        frame.Sprite(centre, 25, 24, Shape.Orb, Hot, .35f + .58f * progress, Layer.EmissiveCore, light: true);
        frame.Sparks(centre, 8, 45, 34, progress, Gold, rise: 16);
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
        // A layered combustion front replaces the former round glow billboard.
        // Smoke rises separately; sharp directional fire and embers follow.
        float spread = diameter * (.57f + .41f * Ease(progress * 2.7f));
        float burn = 1f - Ease((progress - .59f) / .41f);
        float time = progress * (fierce ? 1.48f : 1.06f);

        frame.Clip(centre - Vector2.UnitY * (19 + 39 * progress),
            spread * 1.24f, spread * 1.18f, Shape.SmokeSequence,
            time * .61f, fierce ? 0x461e15 : 0x534037,
            .52f * (1 - burn * .28f), Layer.Smoke);
        for (int i = 0; i < (fierce ? 9 : 7); i++)
        {
            int seed = i * 31 + 27;
            float age = Fract(progress * (1.03f + frame.Unit(seed + 2) * .32f) + frame.Unit(seed + 7));
            float x = (frame.Unit(seed + 13) - .5f) * spread * 1.11f;
            float rise = age * (29 + frame.Unit(seed + 21) * 44);
            Vector2 source = centre + new Vector2(x, -rise);
            float lean = (frame.Unit(seed + 35) - .5f) * .33f;
            int tint = i % 3 == 0 ? Hot : i % 3 == 1 ? Gold : (fierce ? Ember : Fire);
            frame.Sprite(source - Vector2.UnitY * 19, 23 + spread * .18f,
                62 + frame.Unit(seed + 39) * 54, Shape.UserFlame,
                tint, .88f * burn * (1f - age * .46f),
                i % 3 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                lean, light: i % 4 == 0);
            frame.Clip(source - Vector2.UnitY * 7,
                25 + spread * .14f, 54 + frame.Unit(seed + 41) * 36,
                Shape.FlameSequence, time * 1.22f + frame.Unit(seed + 43),
                tint, .63f * burn, Layer.Emissive, lean);
            frame.Sprite(source + new Vector2(lean * 13, -9 - age * 29),
                5 + frame.Unit(seed + 45) * 8, 8 + frame.Unit(seed + 47) * 10,
                Shape.UserEmber, Hot, .82f * burn * (1 - age * .58f),
                Layer.EmissiveCore, lean, light: i % 5 == 0);
        }
    }

    private static void SearingImpact(Frame frame, Vector2 centre, float progress, int accent)
    {
        // Great Fire Ball / Fire Bounce / Meteor Shower are impact events:
        // fast ignition, textured inner combustion, smoky aftermath.
        float open = .56f + .44f * Ease(progress * 3f);
        float heat = 1f - Ease((progress - .39f) * 2.6f);
        float width = 94 * open;
        frame.Clip(centre - Vector2.UnitY * (13 + progress * 27),
            117 * open, 108 * open, Shape.SmokeSequence, progress * .77f,
            0x694032, .58f * progress, Layer.Smoke);
        for (int i = 0; i < 5; i++)
        {
            int seed = i * 41 + 107;
            float angle = (frame.Unit(seed) - .5f) * .83f;
            Vector2 point = centre + new Vector2((i - 2) * 17 * open, -progress * (8 + i * 6));
            frame.Sprite(point - Vector2.UnitY * 11, width * .55f,
                72 * open + frame.Unit(seed + 9) * 29,
                Shape.UserFlame, i % 2 == 0 ? accent : Hot,
                .82f * heat, i % 3 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                angle, light: i == 2);
            frame.Sprite(point + new Vector2((frame.Unit(seed + 15) - .5f) * 23, -progress * 30),
                6 + frame.Unit(seed + 17) * 7, 10 + frame.Unit(seed + 21) * 13,
                Shape.UserEmber, Gold, .73f * (1 - progress * .65f),
                Layer.EmissiveCore, angle);
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
        frame.Sprite(feet - Vector2.UnitY * 4, 63, 35, Shape.Orb, Ice, .63f, Layer.Ground, light: true);
        frame.Clip(feet - Vector2.UnitY * 22, 75, 64, Shape.SmokeSequence, progress * .71f, 0x5876b0, .53f, Layer.Smoke);
        frame.Sprite(feet - Vector2.UnitY * 15, 55, 49, Shape.Flame, IceWhite, .79f * (1 - progress), Layer.EmissiveCore, light: true);
        for (int i = 0; i < 8; i++)
        {
            float rise = Fract(progress * 1.07f + frame.Unit(i + 27) * .67f);
            Vector2 root = feet + Ellipse(i * .79f, 26, 13);
            frame.Sprite(root - Vector2.UnitY * (8 + 58 * rise), 11 + frame.Unit(i) * 13, 31, Shape.IceSpire,
                i % 2 == 0 ? IceWhite : Ice, .93f - rise * .43f, Layer.Emissive, (frame.Unit(i + 11) - .5f) * .83f, light: i % 2 == 0);
        }
        frame.Sparks(feet - Vector2.UnitY * 7, 13, 30, 21, progress, IceWhite, rise: 38, shape: Shape.Shard);
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
