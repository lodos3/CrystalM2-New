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

    private static readonly SpellParticleColourCurve PaperFlame = new(
        (0, 0xff6729), (.32f, 0xfb481c), (.70f, 0xa22618), (1, 0x521b17));
    private static readonly SpellParticleColourCurve PaperCore = new(
        (0, 0xfffcf4), (.53f, 0xffffed), (.76f, 0xff9d43), (1, 0xd2451d));
    private static readonly SpellParticleColourCurve PaperHeat = new(
        (0, 0xffd65e), (.53f, 0xff9b32), (.77f, 0xe9591e), (1, 0x872d1f));
    private static readonly SpellParticleColourCurve PaperEmber = new(
        (0, 0xfff4c4), (.38f, 0xffa130), (.75f, 0xec4c1d), (1, 0x892617));
    private static readonly SpellParticleColourCurve SoulFlame = new(
        (0, 0xd5faff), (.36f, 0x51c9ff), (.72f, 0x377bdf), (1, 0x244582));
    private static readonly SpellParticleColourCurve ArmourFlame = new(
        (0, 0xffffd2), (.31f, 0xffd054), (.67f, 0xff7823), (1, 0xbc3513));

    private static void PaperBurst(Frame frame, Vector2 feet, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * 32;
        float spread = .48f + .52f * Ease(progress * 2.7f);
        float heat = 1 - Ease((progress - .53f) / .43f);
        frame.Sprite(feet, 94 * spread, 48 * spread, Shape.Wisp, PaperFlame.Sample(progress),
            .63f * heat, Layer.Ground, light: true);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 2.39996f;
            Vector2 ray = Ellipse(angle, 34, 37);
            float age = Math.Clamp(progress * 1.14f - i * .065f, 0, 1);
            Vector2 lobe = centre + (ray + new Vector2(9, -14 * age)) * spread;
            float flare = (.58f + .42f * Ease(age * 5)) * (1 - .72f * Ease((age - .57f) / .43f));
            frame.Clip(lobe, (48 + i * 3) * spread, (69 + i * 4) * spread, Shape.FireSequence,
                .20f + age * .53f + i * .117f, PaperFlame.Sample(age), .75f * flare, rotation: MathF.Cos(angle) * .46f);
            frame.Clip(Vector2.Lerp(centre, lobe, .81f), 37 * spread, 57 * spread, Shape.FireSequence,
                .29f + age * .58f + i * .154f, PaperHeat.Sample(age), .91f * flare, Layer.EmissiveCore, MathF.Sin(angle) * .6f);
            frame.Sprite(Vector2.Lerp(centre, lobe, .58f), 37 * spread, 59 * spread, Shape.UserFlame,
                PaperCore.Sample(age), heat * flare * .93f, Layer.EmissiveCore, angle - .5f, light: i % 2 == 0);
            frame.Clip(lobe + new Vector2(10 + age * 12, -14 - age * 23), 49 * spread, 72 * spread,
                Shape.SmokeSequence, age * .58f + i * .162f, 0x873d2a, .58f * Ease(age * 2.7f), Layer.Smoke);
        }
        frame.Clip(centre, 52 * spread, 75 * spread, Shape.FireSequence, .27f + progress * .43f,
            PaperCore.Sample(progress), heat, Layer.EmissiveCore, light: true);
        frame.Sprite(centre - Vector2.UnitY * 9, 36 * spread, 50 * spread, Shape.Flash, White,
            .98f * heat, Layer.EmissiveCore);
        for (int i = 0; i < 34; i++)
        {
            float birth = frame.Unit(i + 29) * .62f - .09f;
            float age = (progress - birth) / (.36f + .48f * frame.Unit(i + 83));
            if (age is < 0 or >= 1) continue;
            float angle = i * 2.39996f;
            float reach = .52f + .48f * frame.Unit(i + 16);
            Vector2 ray = Ellipse(angle, 111 * reach, 76 * reach);
            Vector2 position = centre + ray * (.25f + .75f * Ease(age * 1.58f)) - Vector2.UnitY * (19 + 30 * reach) * age;
            float size = 3.2f + 4.1f * frame.Unit(i + 41);
            float alpha = (.65f + .35f * Ease(age * 8)) * (1 - Ease((age - .63f) / .37f));
            frame.Sprite(position, size, size * (1.2f + .67f * (1 - age)), Shape.UserEmber, PaperEmber.Sample(age),
                alpha, Layer.EmissiveCore, angle, light: i % 9 == 0);
            if (i % 4 == 0)
            {
                frame.Sprite(position, size * 2.3f, size * 2.5f, Shape.Flash, 0xff7b25, .51f * alpha, Layer.Glow);
                frame.Sprite(position, size * .71f, size * 1.18f, Shape.Spark, PaperEmber.Sample(age * .72f),
                    .91f * alpha, Layer.EmissiveCore, angle);
            }
        }
    }

    private static void SummonStars(Frame frame, Vector2 feet, float progress, bool deva)
    {
        int dusk = deva ? 0x426d8c : InkBlue, glow = deva ? Mint : Blue;
        float lift = Ease(progress) * 55;
        float opening = Ease(progress * 2.4f);
        float ascension = Ease((progress - .58f) / .21f) * (1 - .64f * Ease((progress - .87f) / .13f));
        frame.Sprite(feet, 96 + 47 * opening, 44 + 23 * opening, Shape.Halo, dusk,
            .78f * (1 - Ease((progress - .72f) / .28f)), Layer.Ground, light: true);
        frame.Sprite(feet, 74, 38, Shape.Wisp, dusk, .86f, Layer.Ground);
        Vector2 crown = feet - Vector2.UnitY * (64 + lift * .67f);
        frame.Sprite(crown, 38, 49, Shape.IonCorona, glow, .76f * ascension, Layer.Glow, -.27f);
        frame.Sprite(crown, 25, 32, Shape.Flash, deva ? Ivory : Ice, .94f * ascension,
            Layer.EmissiveCore, -.27f, light: true);
        for (int i = 0; i < (deva ? 5 : 3); i++)
        {
            float t = Math.Clamp(progress * 1.35f - i * .105f, 0, 1);
            float turn = progress * 8 + i * 2.34f;
            Vector2 star = feet + Ellipse(turn, 25 + i * 2, 13) - Vector2.UnitY * (10 + lift + i * 6);
            frame.Sprite(star, 27 + i * 4, 38, Shape.Wisp, i % 2 == 0 ? dusk : Teal, .92f, Depth(turn));
            float flare = .60f + .40f * MathF.Pow(MathF.Sin(progress * MathF.Tau * 1.7f + i), 2);
            frame.Sprite(star, 29, 45, Shape.Wisp, glow, .51f * flare, Depth(turn), turn, light: i % 2 == 0);
            frame.Sprite(star, 23 * (1 - t * .27f), 29, Shape.Flash, i == 0 ? Ice : glow,
                .92f * flare, Depth(turn), turn);
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
            if (i % 3 == 0)
                frame.Sprite(point, 21 + 9 * hit, 30 - hit * 16, Shape.Wisp, 0xbcced6,
                    .63f * opacity * (1 - hit * .67f), layer == Layer.Smoke ? Layer.Glow : layer, tilt,
                    light: true, sortY: basePoint.Y);
        }
        // On reaching the ground the concealment breaks into loose floor-hugging
        // fragments instead of finishing as a spherical cloud.
        float ground = Ease((progress - .29f) / .44f);
        float dustFade = 1 - .77f * Ease((progress - .70f) / .30f);
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

    private static void BurningWard(Frame frame, Vector2 feet, float progress, Shape glyph)
    {
        bool armour = glyph == Shape.ArmourGlyph;
        float width = armour ? 295 : 327, height = armour ? 219 : 255;
        var flame = armour ? ArmourFlame : SoulFlame;
        int glow = armour ? 0xe45717 : 0x367de4;
        Vector2 centre = feet + (armour ? new Vector2(7, -11) : new Vector2(0, -28));
        Vector2 scale = new(width * .5f, height * .5f);
        float front = armour ? .16f + progress * 1.88f : .25f + progress * 1.65f;
        float formed = Ease((front - .86f) / .15f);
        float settling = Ease((progress - .59f) / .40f);
        // The rune forms behind its fire, then burns away while the remaining
        // smoke continues rising off the same fixed brush-stroke positions.
        float dissipation = 1 - Ease((progress - .71f) / .28f);
        frame.Sprite(centre, width * 1.025f, height * 1.03f, glyph, armour ? 0xdd591f : 0x4c89ef,
            formed * dissipation * .65f, Layer.Glow);
        frame.Sprite(centre, width, height, glyph, armour ? 0xf97026 : 0x4ea9ff,
            formed * dissipation * (.56f + .36f * settling));
        frame.Sprite(centre, width, height, glyph, armour ? 0xff982f : 0xa9e2ff,
            formed * dissipation * (.33f + .30f * settling), Layer.EmissiveCore);
        if (armour)
        {
            float spark = 1 - Ease(progress / .27f);
            Vector2 source = feet - Vector2.UnitY * (87 + 34 * progress);
            frame.Sprite(source, 46, 52, Shape.Wisp, 0xf57b29, .92f * spark, Layer.Glow);
            frame.Sprite(source, 27, 39, Shape.UserFlame, Amber, .93f * spark, Layer.EmissiveCore, light: true);
            frame.Sprite(source, 20, 26, Shape.Flash, Ivory, .98f * spark, Layer.EmissiveCore);
        }

        for (int stroke = 0; stroke < TaoistRuneBrush.StrokeCount(glyph); stroke++)
        {
            ReadOnlySpan<Vector3> points = TaoistRuneBrush.Stroke(glyph, stroke);
            for (int i = 0; i < points.Length; i += 2)
            {
                Vector3 knot = points[i];
                int salt = stroke * 109 + i * 17;
                float ignition = .5f + .23f * knot.X + .28f * knot.Y + .025f * frame.Unit(salt);
                if (front < ignition) continue;
                Vector2 root = centre + new Vector2(knot.X, knot.Y) * scale;
                float age = Fract(progress * (2.1f + frame.Unit(salt + 3)) + frame.Unit(salt + 17));
                float press = Math.Clamp(knot.Z / 5, 0, armour ? 1.35f : 1);
                float size = 9 + (armour ? 25 : 20) * press;
                float lift = (17 + (armour ? 43 : 38) * press) * (1 - settling * .55f);
                float rise = (8 + (armour ? 38 : 34) * press) * age;
                float tilt = .23f * MathF.Sin(age * MathF.Tau + salt);
                float alpha = (.69f + .31f * MathF.Sin(age * MathF.PI)) *
                    (1 - settling * (armour ? .81f : .61f)) * (.48f + .52f * dissipation);
                frame.Clip(root - Vector2.UnitY * (lift * .23f + rise * .25f), size, lift, Shape.FlameSequence, age,
                    flame.Sample(.33f + age * .50f), .85f * alpha, Layer.Emissive, tilt);
                frame.Sprite(root - Vector2.UnitY * (lift * .12f + rise * .62f), size * .8f, lift * .91f, Shape.UserFlame,
                    flame.Sample(age * .55f), alpha, Layer.EmissiveCore, -tilt, light: i % 6 == 0);
                frame.Sprite(root - Vector2.UnitY * (lift * .30f + rise), size * .56f, lift, Shape.FlameTongue,
                    flame.Sample(age * .48f), .74f * alpha, Layer.EmissiveCore, tilt);
                if (armour && i % 4 == 0)
                    frame.Clip(root + new Vector2(tilt * 21, -lift * .34f), size * 1.18f, lift * 1.19f,
                        Shape.FlameSequence, age + .36f, flame.Sample(age * .29f), .90f * alpha, Layer.EmissiveCore, tilt);
                if (i % 4 == 0)
                {
                    // Thin, independently phased smoke plumes peel upwards and
                    // drift sideways. Keep these attached to actual glyph knots,
                    // rather than introducing a backdrop billboard or ellipse.
                    float release = Ease((progress - .31f) / .48f);
                    if (release > .014f)
                    {
                        float smokeAge = Fract(progress * (1.13f + frame.Unit(salt + 75) * .45f) +
                            frame.Unit(salt + 76));
                        float sideways = (frame.Unit(salt + 78) * 2 - 1) * (6 + 13 * smokeAge);
                        Vector2 plume = root + new Vector2(sideways, -9 - (25 + 20 * press) * smokeAge);
                        float opacity = (.17f + .45f * release) * (1 - .62f * smokeAge) *
                            (.7f + .3f * settling);
                        frame.Clip(plume, 10 + 11 * smokeAge, 19 + 17 * smokeAge, Shape.SmokeSequence,
                            smokeAge + .12f, armour ? 0x675249 : 0x526875, opacity, Layer.Smoke, tilt * .38f);
                    }
                    frame.Sprite(root, Math.Min(65, size * (armour ? 1.8f : 1.33f)), lift * .69f,
                        Shape.Wisp, glow, .65f * alpha, Layer.Glow);
                    frame.Sprite(root - Vector2.UnitY * (6 + lift * age), 4, 7, Shape.UserEmber,
                        flame.Sample(age), .76f * alpha, Layer.EmissiveCore, tilt);
                    if (i % 8 == 0)
                        frame.Sprite(root, size * .69f, lift * .58f, Shape.Flash, armour ? Gold : Ice, .39f * alpha, Layer.EmissiveCore);
                }
            }
        }
    }

    private static void CurseRune(Frame frame, Vector2 feet, float progress)
    {
        const float width = 299, height = 221;
        Vector2 centre = feet - Vector2.UnitY * 15;
        Vector2 scale = new(width * .5f, height * .5f);
        float surge = Ease((progress - .12f) / .30f) * (1 - Ease((progress - .58f) / .24f));
        float breaking = Ease((progress - .63f) / .32f);
        float visible = 1 - breaking;
        frame.Sprite(centre, width * 1.04f, height * 1.06f, Shape.CurseGlyph, 0x82b48d,
            visible * (.61f + .30f * surge), Layer.Glow);
        frame.Sprite(centre, width, height, Shape.CurseGlyph, 0x93b99b,
            visible * (.53f + .39f * surge), Layer.Emissive, light: true);
        frame.Sprite(centre, width, height, Shape.CurseGlyph, 0xe5ffea,
            visible * (.24f + .71f * surge), Layer.EmissiveCore);

        for (int stroke = 0; stroke < TaoistRuneBrush.StrokeCount(Shape.CurseGlyph); stroke++)
        {
            ReadOnlySpan<Vector3> points = TaoistRuneBrush.Stroke(Shape.CurseGlyph, stroke);
            for (int i = 0; i < points.Length; i += 3)
            {
                Vector3 knot = points[i];
                int seed = 97 * stroke + i * 23;
                Vector2 root = centre + new Vector2(knot.X, knot.Y) * scale;
                float flicker = .64f + .36f * MathF.Sin(progress * 18 + frame.Unit(seed) * MathF.Tau);
                float pressure = Math.Clamp(knot.Z / 5, 0, 1);
                float flare = visible * flicker * (.34f + .66f * surge);
                frame.Sprite(root, 21 + 24 * pressure, 23 + 19 * pressure, Shape.Wisp, 0x7ab592,
                    .87f * flare, Layer.Glow, light: i % 6 == 0);
                frame.Sprite(root + Ellipse(frame.Unit(seed + 12) * MathF.Tau, 11, 9),
                    4 + 8 * pressure, 7 + 8 * pressure, Shape.Flash, i % 2 == 0 ? 0xe7fff1 : Mint,
                    .76f * flare, Layer.EmissiveCore, seed);

                if (i % 6 == 0 && pressure > .40f && surge > .025f)
                {
                    frame.Clip(root - Vector2.UnitY * 7, 29, 37, Shape.SmokeSequence,
                        frame.Unit(seed + 18) + progress * .58f, 0x738c7c, .49f * flare, Layer.Smoke);
                    Vector2 ray = Vector2.Normalize(new Vector2(knot.X * (.30f + frame.Unit(seed + 28) * .50f), -1));
                    float length = (93 + 88 * frame.Unit(seed + 29)) * (.56f + .44f * surge);
                    float angle = MathF.Atan2(ray.Y, ray.X);
                    frame.Sprite(root + ray * length * .42f, 22 + 24 * pressure, length, Shape.Pillar,
                        0xa0caa7, .67f * flare * surge, Layer.Glow, angle + MathF.PI * .5f);
                    frame.Sprite(root + ray * length * .42f, length, 13 + 16 * pressure, Shape.MistFilament, 0x91c9a2,
                        .70f * flare * surge, Layer.Glow, angle);
                    frame.Sprite(root + ray * length * .25f, length * .70f, 5 + 7 * pressure, Shape.MistFilament, 0xdbf7de,
                        .62f * flare * surge, Layer.EmissiveCore, angle);
                }

                float age = Math.Clamp((progress - .57f - frame.Unit(seed + 51) * .22f) * 2.7f, 0, 1);
                if (age <= 0) continue;
                Vector2 fragment = root + Ellipse(frame.Unit(seed + 60) * MathF.Tau, 26, 21) * age;
                float remnant = Ease(age * 5) * (1 - .69f * Ease((age - .65f) / .35f));
                frame.Clip(fragment, 11 + 15 * age, 15 + 14 * age, Shape.SmokeSequence, frame.Unit(seed + 62) + age * .69f,
                    i % 2 == 0 ? 0x99a3b2 : 0x52677b, remnant, Layer.Emissive, age * 1.2f);
                frame.Sprite(fragment, 9 + 10 * pressure, 7 + 6 * age, Shape.MistFilament, 0x919cad,
                    .66f * remnant, Layer.Emissive, seed + age * 2);
                frame.Sprite(fragment, 3 + 4 * pressure, 6, Shape.Spark, 0xa1b8b1,
                    .61f * remnant * (1 - age), Layer.EmissiveCore);
            }
        }
        Vector2 escape = centre + new Vector2(-82 - 15 * breaking, -52 - 19 * breaking);
        frame.Sprite(escape, 25, 28, Shape.IonCorona, 0x80c4a5, .79f * breaking, Layer.Emissive, -.15f);
        frame.Sprite(escape, 34, 36, Shape.Wisp, 0x6fbd9e, .88f * breaking, Layer.Glow, light: true);
        frame.Sprite(escape, 12, 14, Shape.Flash, Mint, .91f * breaking, Layer.EmissiveCore);
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
        HealMotes(frame, centre, radius * 1.05f, progress, prepare ? 50 : 84);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * MathF.PI * .5f + progress * 4;
            Vector2 node = centre + Ellipse(angle, radius, radius * GroundAspect);
            Layer depth = Depth(angle);
            frame.Sprite(node, 43, 38, Shape.Wisp, i % 2 == 0 ? Jade : Gold, .78f, depth, angle, light: true);
            float flare = .67f + .33f * MathF.Pow(MathF.Sin(progress * MathF.Tau * 2 + i * .7f), 2);
            frame.Sprite(node, 51, 34, Shape.Wisp, i % 2 == 0 ? 0x44a4a5 : Amber,
                .52f * flare, depth, angle + MathF.PI * .5f, light: true);
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
