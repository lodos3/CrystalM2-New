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
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Poison);
        Vector2 body = feet - Vector2.UnitY * (prepare ? tuning.Body + Ease(progress) * tuning.Body2 : tuning.ReleaseBody);
        float bloom = tuning.Bloom + tuning.Bloom2 * Ease(progress * tuning.Bloom3);
        int bodyTint = prepare ? tuning.PrepareBodyTintColour : tuning.ReleaseBodyTintColour;
        frame.Clip(body, tuning.SmokeSequenceWidth * bloom, tuning.SmokeSequenceHeight * bloom, tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer);
        frame.Sprite(body, tuning.OrbWidth * bloom, tuning.OrbHeight * bloom, tuning.OrbShape, bodyTint, tuning.OrbAlpha, light: tuning.OrbLight);
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float age = Fract(progress * tuning.Age + i * tuning.Age2);
            float angle = i * tuning.Angle + time * tuning.Angle2;
            Vector2 point = body + Ellipse(angle, tuning.EllipseX * bloom, tuning.EllipseY * bloom) * (tuning.Point + age * tuning.Point2) -
                Vector2.UnitY * (age * tuning.Point3);
            frame.Sprite(point, tuning.WispWidth + (1 - age) * tuning.WispWidth2, tuning.WispHeight + (1 - age) * tuning.WispHeight2, tuning.WispShape,
                i % 3 == 0 ? tuning.WispColour : bodyTint, tuning.WispAlpha - tuning.WispAlpha2 * age, Depth(angle));
            if (!prepare) frame.Sprite(point, tuning.SparkWidth + frame.Unit(i) * tuning.SparkWidth2, tuning.SparkHeight, tuning.SparkShape, Gold, tuning.SparkAlpha - age * tuning.SparkAlpha2, tuning.SparkLayer);
        }
        if (prepare)
            frame.Orbit(body, tuning.OrbitX * bloom, tuning.OrbitY * bloom, progress * tuning.OrbitAngle, tuning.OrbitSweep, tuning.OrbitColour, tuning.OrbitWidth, tuning.OrbitAlpha);
    }

    private static SpellParticleColourCurve PaperFlame => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).PaperFlame;
    private static SpellParticleColourCurve PaperCore => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).PaperCore;
    private static SpellParticleColourCurve PaperHeat => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).PaperHeat;
    private static SpellParticleColourCurve PaperEmber => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).PaperEmber;
    private static SpellParticleColourCurve SoulFlame => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).SoulFlame;
    private static SpellParticleColourCurve ArmourFlame => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).ArmourFlame;

    private static void PaperBurst(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.PaperBurst);
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        float spread = tuning.Spread + tuning.Spread2 * Ease(progress * tuning.Spread3);
        float heat = 1 - Ease((progress - tuning.Heat) / tuning.HeatDivisor);
        frame.Sprite(feet, tuning.WispWidth * spread, tuning.WispHeight * spread, tuning.WispShape, PaperFlame.Sample(progress),
            tuning.WispAlpha * heat, tuning.WispLayer, light: tuning.WispLight);
        for (int i = 0; i < tuning.FireSequenceCount; i++)
        {
            float angle = i * tuning.Angle;
            Vector2 ray = Ellipse(angle, tuning.EllipseX, tuning.EllipseY);
            float age = Math.Clamp(progress * tuning.Age - i * tuning.Age2, 0, 1);
            Vector2 lobe = centre + (ray + new Vector2(tuning.X, tuning.Y * age)) * spread;
            float flare = (tuning.Flare + tuning.Flare2 * Ease(age * tuning.Flare3)) * (1 - tuning.Flare4 * Ease((age - tuning.Flare5) / tuning.FlareDivisor));
            frame.Clip(lobe, (tuning.FireSequenceWidth + i * tuning.FireSequenceWidth2) * spread, (tuning.FireSequenceHeight + i * tuning.FireSequenceHeight2) * spread, tuning.FireSequenceShape,
                tuning.FireSequencePhase + age * tuning.FireSequencePhase2 + i * tuning.FireSequencePhase3, PaperFlame.Sample(age), tuning.FireSequenceAlpha * flare, rotation: MathF.Cos(angle) * tuning.FireSequenceRotation);
            frame.Clip(Vector2.Lerp(centre, lobe, tuning.Amount), tuning.FireSequenceWidth3 * spread, tuning.FireSequenceHeight3 * spread, tuning.FireSequenceShape2,
                tuning.FireSequencePhase4 + age * tuning.FireSequencePhase5 + i * tuning.FireSequencePhase6, PaperHeat.Sample(age), tuning.FireSequenceAlpha2 * flare, tuning.FireSequenceLayer, MathF.Sin(angle) * tuning.FireSequenceRotation2);
            frame.Sprite(Vector2.Lerp(centre, lobe, tuning.Amount2), tuning.UserFlameWidth * spread, tuning.UserFlameHeight * spread, tuning.UserFlameShape,
                PaperCore.Sample(age), heat * flare * tuning.UserFlameAlpha, tuning.UserFlameLayer, angle - tuning.UserFlameRotation, light: i % 2 == 0);
            frame.Clip(lobe + new Vector2(tuning.X2 + age * tuning.X3, tuning.Y2 - age * tuning.Y3), tuning.SmokeSequenceWidth * spread, tuning.SmokeSequenceHeight * spread,
                tuning.SmokeSequenceShape, age * tuning.SmokeSequencePhase + i * tuning.SmokeSequencePhase2, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * Ease(age * tuning.Value), tuning.SmokeSequenceLayer);
        }
        frame.Clip(centre, tuning.FireSequenceWidth4 * spread, tuning.FireSequenceHeight4 * spread, tuning.FireSequenceShape3, tuning.FireSequencePhase7 + progress * tuning.FireSequencePhase8,
            PaperCore.Sample(progress), heat, tuning.FireSequenceLayer2, light: tuning.FireSequenceLight);
        frame.Sprite(centre - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth * spread, tuning.FlashHeight * spread, tuning.FlashShape, White,
            tuning.FlashAlpha * heat, tuning.FlashLayer);
        for (int i = 0; i < tuning.UserEmberCount; i++)
        {
            float birth = frame.Unit(i + 29) * tuning.Birth - tuning.Birth2;
            float age = (progress - birth) / (tuning.Age3 + tuning.Age4 * frame.Unit(i + 83));
            if (age is < 0 or >= 1) continue;
            float angle = i * tuning.Angle2;
            float reach = tuning.Reach + tuning.Reach2 * frame.Unit(i + 16);
            Vector2 ray = Ellipse(angle, tuning.EllipseX2 * reach, tuning.EllipseY2 * reach);
            Vector2 position = centre + ray * (tuning.Position + tuning.Position2 * Ease(age * tuning.Position3)) - Vector2.UnitY * (tuning.Position4 + tuning.Position5 * reach) * age;
            float size = tuning.Size + tuning.Size2 * frame.Unit(i + 41);
            float alpha = (tuning.Alpha + tuning.Alpha2 * Ease(age * tuning.Alpha3)) * (1 - Ease((age - tuning.Alpha4) / tuning.AlphaDivisor));
            frame.Sprite(position, size, size * (tuning.UserEmberHeight + tuning.UserEmberHeight2 * (1 - age)), tuning.UserEmberShape, PaperEmber.Sample(age),
                alpha, tuning.UserEmberLayer, angle, light: i % 9 == 0);
            if (i % 4 == 0)
            {
                frame.Sprite(position, size * tuning.FlashWidth2, size * tuning.FlashHeight2, tuning.FlashShape2, tuning.FlashColour, tuning.FlashAlpha2 * alpha, tuning.FlashLayer2);
                frame.Sprite(position, size * tuning.SparkWidth, size * tuning.SparkHeight, tuning.SparkShape, PaperEmber.Sample(age * tuning.Offset),
                    tuning.SparkAlpha * alpha, tuning.SparkLayer, angle);
            }
        }
    }

    private static void SummonStars(Frame frame, Vector2 feet, float progress, bool deva)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.SummonStars);
        int dusk = deva ? tuning.DuskColour : InkBlue, glow = deva ? Mint : Blue;
        float lift = Ease(progress) * tuning.Lift;
        float opening = Ease(progress * tuning.Opening);
        float ascension = Ease((progress - tuning.Ascension) / tuning.AscensionDivisor) * (1 - tuning.Ascension2 * Ease((progress - tuning.Ascension3) / tuning.AscensionDivisor2));
        frame.Sprite(feet, tuning.HaloWidth + tuning.HaloWidth2 * opening, tuning.HaloHeight + tuning.HaloHeight2 * opening, tuning.HaloShape, dusk,
            tuning.HaloAlpha * (1 - Ease((progress - tuning.Value) / tuning.ValueDivisor)), tuning.HaloLayer, light: tuning.HaloLight);
        frame.Sprite(feet, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, dusk, tuning.WispAlpha, tuning.WispLayer);
        Vector2 crown = feet - Vector2.UnitY * (tuning.Crown + lift * tuning.Crown2);
        frame.Sprite(crown, tuning.IonCoronaWidth, tuning.IonCoronaHeight, tuning.IonCoronaShape, glow, tuning.IonCoronaAlpha * ascension, tuning.IonCoronaLayer, tuning.IonCoronaRotation);
        frame.Sprite(crown, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, deva ? Ivory : Ice, tuning.FlashAlpha * ascension,
            tuning.FlashLayer, tuning.FlashRotation, light: tuning.FlashLight);
        for (int i = 0; i < (deva ? tuning.WispCount : tuning.WispCount2); i++)
        {
            float t = Math.Clamp(progress * tuning.T - i * tuning.T2, 0, 1);
            float turn = progress * tuning.Turn + i * tuning.Turn2;
            Vector2 star = feet + Ellipse(turn, tuning.EllipseX + i * tuning.EllipseX2, tuning.EllipseY) - Vector2.UnitY * (tuning.Star + lift + i * tuning.Star2);
            frame.Sprite(star, tuning.WispWidth2 + i * tuning.WispWidth3, tuning.WispHeight2, tuning.WispShape2, i % 2 == 0 ? dusk : Teal, tuning.WispAlpha2, Depth(turn));
            float flare = tuning.Flare + tuning.Flare2 * MathF.Pow(MathF.Sin(progress * MathF.Tau * tuning.Flare3 + i), tuning.Flare4);
            frame.Sprite(star, tuning.WispWidth4, tuning.WispHeight3, tuning.WispShape3, glow, tuning.WispAlpha3 * flare, Depth(turn), turn, light: i % 2 == 0);
            frame.Sprite(star, tuning.FlashWidth2 * (1 - t * tuning.FlashWidth3), tuning.FlashHeight2, tuning.FlashShape2, i == 0 ? Ice : glow,
                tuning.FlashAlpha2 * flare, Depth(turn), turn);
            frame.Sprite(star, tuning.FlashWidth4, tuning.FlashHeight3, tuning.FlashShape3, White, tuning.FlashAlpha3, tuning.FlashLayer2, -turn);
        }
        frame.Sparks(feet - Vector2.UnitY * (tuning.FeatherOffset + lift), tuning.FeatherCount, tuning.FeatherRx, tuning.FeatherRy, progress, deva ? Gold : Mint, tuning.FeatherRise,
            deva ? tuning.FeatherShape : tuning.FeatherShape2);
        for (int i = 0; i < tuning.FlashCount; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i + 173));
            float turn = i * tuning.Turn3 + progress * tuning.Turn4;
            Vector2 point = feet + Ellipse(turn, tuning.EllipseX3 + tuning.EllipseX4 * age, tuning.EllipseY2 + tuning.EllipseY3 * age) -
                Vector2.UnitY * (age * (tuning.Point + lift) + tuning.Point2);
            frame.Sprite(point, tuning.FlashWidth5 + frame.Unit(i + 31) * tuning.FlashWidth6, tuning.FlashHeight4 + age * tuning.FlashHeight5,
                i % 4 == 0 ? tuning.FlashShape4 : tuning.FlashShape5, i % 3 == 0 ? Ice : glow,
                tuning.FlashAlpha4 * (1 - age), tuning.FlashLayer3, turn, light: i == 0);
        }
    }

    private static void Veil(Frame frame, Vector2 feet, float progress, float time, bool mass)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Veil);
        float width = mass ? tuning.Width : 1;
        int puffs = mass ? tuning.Puffs : tuning.Puffs2;
        for (int i = 0; i < puffs; i++)
        {
            float age = (progress + tuning.Age - frame.Unit(i + 71) * tuning.Age2) / (tuning.Age3 + frame.Unit(i + 13) * tuning.Age4);
            float hit = Ease((age - tuning.Hit) / tuning.HitDivisor);
            float opacity = Ease(age * tuning.Opacity) * (1 - Ease((age - tuning.Opacity2) / tuning.OpacityDivisor));
            if (opacity < .02f) continue;
            float turn = i * tuning.Turn + tuning.Turn2 * progress;
            Vector2 basePoint = feet + ProjectGround(Ellipse(turn, tuning.EllipseX, tuning.EllipseY)) * (width * (tuning.BasePoint + tuning.BasePoint2 * hit));
            float lift = (tuning.Lift + frame.Unit(i) * tuning.Lift2) * (1 - Ease(age)) + tuning.Lift3;
            Vector2 point = basePoint - Vector2.UnitY * lift + new Vector2(MathF.Sin(age * tuning.Point + i) * (tuning.X + tuning.X2 * hit), 0);
            Layer layer = hit > .70f ? tuning.Layer : MathF.Sin(turn) < 0 ? tuning.Layer2 : tuning.Layer3;
            float tilt = MathF.Cos(turn) * (tuning.Tilt - tuning.Tilt2 * hit);
            int tint = i % 3 == 0 ? tuning.TintColour : i % 3 == 1 ? tuning.TintColour2 : tuning.TintColour3;
            frame.Clip(point, (tuning.SmokeSequenceWidth + frame.Unit(i + 28) * tuning.SmokeSequenceWidth2 + hit * tuning.SmokeSequenceWidth3) * width, tuning.SmokeSequenceHeight + frame.Unit(i + 4) * tuning.SmokeSequenceHeight2 - hit * tuning.SmokeSequenceHeight3,
                tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase + i * tuning.SmokeSequencePhase2, tint, tuning.SmokeSequenceAlpha * opacity, layer, tilt, sortY: basePoint.Y);
            frame.Clip(point + new Vector2(tilt * tuning.X3, tuning.Y * (1 - hit)), tuning.SmokeSequenceWidth4 + hit * tuning.SmokeSequenceWidth5, tuning.SmokeSequenceHeight4 - hit * tuning.SmokeSequenceHeight5,
                tuning.SmokeSequenceShape2, time * tuning.SmokeSequencePhase3 + i * tuning.SmokeSequencePhase4, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha2 * opacity, layer, -tilt, sortY: basePoint.Y);
            if (i % 3 == 0)
                frame.Sprite(point, tuning.WispWidth + tuning.WispWidth2 * hit, tuning.WispHeight - hit * tuning.WispHeight2, tuning.WispShape, tuning.WispColour,
                    tuning.WispAlpha * opacity * (1 - hit * tuning.WispAlpha2), layer == Layer.Smoke ? tuning.WispLayer : layer, tilt,
                    light: tuning.WispLight, sortY: basePoint.Y);
        }
        // On reaching the ground the concealment breaks into loose floor-hugging
        // fragments instead of finishing as a spherical cloud.
        float ground = Ease((progress - tuning.Ground) / tuning.GroundDivisor);
        float dustFade = 1 - tuning.DustFade * Ease((progress - tuning.DustFade2) / tuning.DustFadeDivisor);
        for (int i = 0; i < (mass ? tuning.WispCount : tuning.WispCount2); i++)
        {
            float turn = i * tuning.Turn3 + frame.Unit(i + 205) * tuning.Turn4;
            float age = Fract(progress * tuning.Age5 + frame.Unit(i + 113));
            Vector2 point = feet + ProjectGround(Ellipse(turn, (tuning.EllipseX2 + i * tuning.EllipseX3) * width * ground,
                (tuning.EllipseY2 + i * tuning.EllipseY3) * width * ground));
            frame.Sprite(point, (tuning.WispWidth3 + age * tuning.WispWidth4) * width, tuning.WispHeight3 + age * tuning.WispHeight4, tuning.WispShape2,
                i % 3 == 0 ? tuning.WispColour2 : tuning.WispColour3, tuning.WispAlpha3 * ground * dustFade * (1 - age * tuning.WispAlpha4),
                tuning.WispLayer2, turn * tuning.WispRotation);
            if (i % 3 == 0)
                frame.Sprite(point - Vector2.UnitY * (tuning.SparkOffset + age * tuning.SparkOffset2), tuning.SparkWidth + age * tuning.SparkWidth2, tuning.SparkHeight,
                    tuning.SparkShape, tuning.SparkColour, tuning.SparkAlpha * dustFade * ground, tuning.SparkLayer);
        }
    }

    private static void BurningWard(Frame frame, Vector2 feet, float progress, Shape glyph)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.BurningWard);
        bool armour = glyph == Shape.ArmourGlyph;
        float width = armour ? tuning.ArmourWidth : tuning.SoulShieldWidth, height = armour ? tuning.ArmourHeight : tuning.SoulShieldHeight;
        var flame = armour ? ArmourFlame : SoulFlame;
        int glow = armour ? tuning.ArmourGlowColour : tuning.SoulShieldGlowColour;
        Vector2 centre = feet + (armour ? new Vector2(tuning.X, tuning.Y) : new Vector2(0, tuning.Y2));
        Vector2 scale = new(width * tuning.Scale, height * tuning.Scale2);
        float front = armour ? tuning.Front + progress * tuning.Front2 : tuning.Front3 + progress * tuning.Front4;
        float formed = Ease((front - tuning.Formed) / tuning.FormedDivisor);
        float settling = Ease((progress - tuning.Settling) / tuning.SettlingDivisor);
        // The rune forms behind its fire, then burns away while the remaining
        // smoke continues rising off the same fixed brush-stroke positions.
        float dissipation = 1 - Ease((progress - tuning.DissipationStart) / tuning.DissipationDuration);
        frame.Sprite(centre, width * tuning.SpriteWidth, height * tuning.SpriteHeight, glyph, armour ? tuning.ArmourSpriteColour : tuning.SoulShieldSpriteColour,
            formed * dissipation * tuning.SpriteAlpha, tuning.SpriteLayer);
        frame.Sprite(centre, width, height, glyph, armour ? tuning.ArmourSpriteColour2 : tuning.SoulShieldSpriteColour2,
            formed * dissipation * (tuning.SpriteAlpha2 + tuning.SpriteAlpha3 * settling));
        frame.Sprite(centre, width, height, glyph, armour ? tuning.ArmourSpriteColour3 : tuning.SoulShieldSpriteColour3,
            formed * dissipation * (tuning.SpriteAlpha4 + tuning.SpriteAlpha5 * settling), tuning.SpriteLayer2);
        if (armour)
        {
            float spark = 1 - Ease(progress / tuning.SparkDivisor);
            Vector2 source = feet - Vector2.UnitY * (tuning.Source + tuning.Source2 * progress);
            frame.Sprite(source, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, tuning.WispColour, tuning.WispAlpha * spark, tuning.WispLayer);
            frame.Sprite(source, tuning.UserFlameWidth, tuning.UserFlameHeight, tuning.UserFlameShape, Amber, tuning.UserFlameAlpha * spark, tuning.UserFlameLayer, light: tuning.UserFlameLight);
            frame.Sprite(source, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Ivory, tuning.FlashAlpha * spark, tuning.FlashLayer);
        }

        for (int stroke = 0; stroke < TaoistRuneBrush.StrokeCount(glyph); stroke++)
        {
            ReadOnlySpan<Vector3> points = TaoistRuneBrush.Stroke(glyph, stroke);
            for (int i = 0; i < points.Length; i += tuning.BrushKnotStride)
            {
                Vector3 knot = points[i];
                int salt = stroke * tuning.StrokeSeedStride + i * tuning.KnotSeedStride;
                float ignition = tuning.Ignition + tuning.Ignition2 * knot.X + tuning.Ignition3 * knot.Y + tuning.Ignition4 * frame.Unit(salt);
                if (front < ignition) continue;
                Vector2 root = centre + new Vector2(knot.X, knot.Y) * scale;
                float age = Fract(progress * (tuning.Age + frame.Unit(salt + 3)) + frame.Unit(salt + 17));
                float press = Math.Clamp(knot.Z / tuning.PressDivisor, 0, armour ? 1.35f : 1);
                float size = tuning.Size + (armour ? tuning.ArmourSize : tuning.SoulShieldSize) * press;
                float lift = (tuning.Lift + (armour ? tuning.ArmourLift : tuning.SoulShieldLift) * press) * (1 - settling * tuning.Lift2);
                float rise = (tuning.Rise + (armour ? tuning.ArmourRise : tuning.SoulShieldRise) * press) * age;
                float tilt = tuning.Tilt * MathF.Sin(age * MathF.Tau + salt);
                float alpha = (tuning.Alpha + tuning.Alpha2 * MathF.Sin(age * MathF.PI)) *
                    (1 - settling * (armour ? tuning.ArmourAlpha : tuning.SoulShieldAlpha)) * (tuning.Alpha3 + tuning.Alpha4 * dissipation);
                frame.Clip(root - Vector2.UnitY * (lift * tuning.FlameSequenceOffset + rise * tuning.FlameSequenceOffset2), size, lift, tuning.FlameSequenceShape, age,
                    flame.Sample(tuning.Offset + age * tuning.Offset2), tuning.FlameSequenceAlpha * alpha, tuning.FlameSequenceLayer, tilt);
                frame.Sprite(root - Vector2.UnitY * (lift * tuning.UserFlameOffset + rise * tuning.UserFlameOffset2), size * tuning.UserFlameWidth2, lift * tuning.UserFlameHeight2, tuning.UserFlameShape2,
                    flame.Sample(age * tuning.Offset3), alpha, tuning.UserFlameLayer2, -tilt, light: i % 6 == 0);
                frame.Sprite(root - Vector2.UnitY * (lift * tuning.FlameTongueOffset + rise), size * tuning.FlameTongueWidth, lift, tuning.FlameTongueShape,
                    flame.Sample(age * tuning.Offset4), tuning.FlameTongueAlpha * alpha, tuning.FlameTongueLayer, tilt);
                if (armour && i % 4 == 0)
                    frame.Clip(root + new Vector2(tilt * tuning.X2, -lift * tuning.Y3), size * tuning.FlameSequenceWidth, lift * tuning.FlameSequenceHeight,
                        tuning.FlameSequenceShape2, age + tuning.FlameSequencePhase, flame.Sample(age * tuning.Offset5), tuning.FlameSequenceAlpha2 * alpha, tuning.FlameSequenceLayer2, tilt);
                if (i % 4 == 0)
                {
                    // Thin, independently phased smoke plumes peel upwards and
                    // drift sideways. Keep these attached to actual glyph knots,
                    // rather than introducing a backdrop billboard or ellipse.
                    float release = Ease((progress - tuning.SmokeReleaseStart) / tuning.SmokeReleaseDuration);
                    if (release > .014f)
                    {
                        float smokeAge = Fract(progress * (tuning.SmokeAge + frame.Unit(salt + 75) * tuning.SmokeAge2) +
                            frame.Unit(salt + 76));
                        float sideways = (frame.Unit(salt + 78) * tuning.Sideways - 1) * (tuning.Sideways2 + tuning.Sideways3 * smokeAge);
                        Vector2 plume = root + new Vector2(sideways, tuning.SmokeBaseOffsetY - (tuning.SmokeBaseRise + tuning.SmokePressureRise * press) * smokeAge);
                        float opacity = (tuning.SmokeBaseOpacity + tuning.SmokeReleaseOpacity * release) * (1 - tuning.SmokeAgeFade * smokeAge) *
                            (tuning.SmokeEarlyOpacity + tuning.SmokeSettlingOpacity * settling);
                        frame.Clip(plume, tuning.SmokeSequenceWidth + tuning.SmokeSequenceWidth2 * smokeAge, tuning.SmokeSequenceHeight + tuning.SmokeSequenceHeight2 * smokeAge, tuning.SmokeSequenceShape,
                            smokeAge + tuning.SmokeSequencePhase, armour ? tuning.ArmourSmokeSequenceColour : tuning.SoulShieldSmokeSequenceColour, opacity, tuning.SmokeSequenceLayer, tilt * tuning.SmokeSequenceRotation);
                    }
                    frame.Sprite(root, Math.Min(tuning.KnotGlowMaxWidth, size * (armour ? tuning.ArmourKnotGlowWidthScale : tuning.SoulShieldKnotGlowWidthScale)), lift * tuning.WispHeight2,
                        tuning.WispShape2, glow, tuning.WispAlpha2 * alpha, tuning.WispLayer2);
                    frame.Sprite(root - Vector2.UnitY * (tuning.UserEmberOffset + lift * age), tuning.UserEmberWidth, tuning.UserEmberHeight, tuning.UserEmberShape,
                        flame.Sample(age), tuning.UserEmberAlpha * alpha, tuning.UserEmberLayer, tilt);
                    if (i % 8 == 0)
                        frame.Sprite(root, size * tuning.FlashWidth2, lift * tuning.FlashHeight2, tuning.FlashShape2, armour ? Gold : Ice, tuning.FlashAlpha2 * alpha, tuning.FlashLayer2);
                }
            }
        }
    }

    private static void CurseRune(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.CurseRune);
        float width = tuning.Width, height = tuning.Height;
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        Vector2 scale = new(width * tuning.Scale, height * tuning.Scale2);
        float surge = Ease((progress - tuning.Surge) / tuning.SurgeDivisor) * (1 - Ease((progress - tuning.Surge2) / tuning.SurgeDivisor2));
        float breaking = Ease((progress - tuning.Breaking) / tuning.BreakingDivisor);
        float visible = 1 - breaking;
        frame.Sprite(centre, width * tuning.CurseGlyphWidth, height * tuning.CurseGlyphHeight, tuning.CurseGlyphShape, tuning.CurseGlyphColour,
            visible * (tuning.CurseGlyphAlpha + tuning.CurseGlyphAlpha2 * surge), tuning.CurseGlyphLayer);
        frame.Sprite(centre, width, height, tuning.CurseGlyphShape2, tuning.CurseGlyphColour2,
            visible * (tuning.CurseGlyphAlpha3 + tuning.CurseGlyphAlpha4 * surge), tuning.CurseGlyphLayer2, light: tuning.CurseGlyphLight);
        frame.Sprite(centre, width, height, tuning.CurseGlyphShape3, tuning.CurseGlyphColour3,
            visible * (tuning.CurseGlyphAlpha5 + tuning.CurseGlyphAlpha6 * surge), tuning.CurseGlyphLayer3);

        for (int stroke = 0; stroke < TaoistRuneBrush.StrokeCount(tuning.BrushShape); stroke++)
        {
            ReadOnlySpan<Vector3> points = TaoistRuneBrush.Stroke(tuning.BrushShape, stroke);
            for (int i = 0; i < points.Length; i += tuning.BrushKnotStride)
            {
                Vector3 knot = points[i];
                int seed = 97 * stroke + i * 23;
                Vector2 root = centre + new Vector2(knot.X, knot.Y) * scale;
                float flicker = tuning.Flicker + tuning.Flicker2 * MathF.Sin(progress * tuning.Flicker3 + frame.Unit(seed) * MathF.Tau);
                float pressure = Math.Clamp(knot.Z / tuning.PressureDivisor, 0, 1);
                float flare = visible * flicker * (tuning.Flare + tuning.Flare2 * surge);
                frame.Sprite(root, tuning.WispWidth + tuning.WispWidth2 * pressure, tuning.WispHeight + tuning.WispHeight2 * pressure, tuning.WispShape, tuning.WispColour,
                    tuning.WispAlpha * flare, tuning.WispLayer, light: i % 6 == 0);
                frame.Sprite(root + Ellipse(frame.Unit(seed + 12) * MathF.Tau, tuning.EllipseX, tuning.EllipseY),
                    tuning.FlashWidth + tuning.FlashWidth2 * pressure, tuning.FlashHeight + tuning.FlashHeight2 * pressure, tuning.FlashShape, i % 2 == 0 ? tuning.FlashColour : Mint,
                    tuning.FlashAlpha * flare, tuning.FlashLayer, seed);

                if (i % 6 == 0 && pressure > .40f && surge > .025f)
                {
                    frame.Clip(root - Vector2.UnitY * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape,
                        frame.Unit(seed + 18) + progress * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * flare, tuning.SmokeSequenceLayer);
                    Vector2 ray = Vector2.Normalize(new Vector2(knot.X * (tuning.X + frame.Unit(seed + 28) * tuning.X2), -1));
                    float length = (tuning.Length + tuning.Length2 * frame.Unit(seed + 29)) * (tuning.Length3 + tuning.Length4 * surge);
                    float angle = MathF.Atan2(ray.Y, ray.X);
                    frame.Sprite(root + ray * length * tuning.PillarOffset, tuning.PillarWidth + tuning.PillarWidth2 * pressure, length, tuning.PillarShape,
                        tuning.PillarColour, tuning.PillarAlpha * flare * surge, tuning.PillarLayer, angle + MathF.PI * tuning.PillarRotation);
                    frame.Sprite(root + ray * length * tuning.MistFilamentOffset, length, tuning.MistFilamentHeight + tuning.MistFilamentHeight2 * pressure, tuning.MistFilamentShape, tuning.MistFilamentColour,
                        tuning.MistFilamentAlpha * flare * surge, tuning.MistFilamentLayer, angle);
                    frame.Sprite(root + ray * length * tuning.MistFilamentOffset2, length * tuning.MistFilamentWidth, tuning.MistFilamentHeight3 + tuning.MistFilamentHeight4 * pressure, tuning.MistFilamentShape2, tuning.MistFilamentColour2,
                        tuning.MistFilamentAlpha2 * flare * surge, tuning.MistFilamentLayer2, angle);
                }

                float age = Math.Clamp((progress - tuning.Age - frame.Unit(seed + 51) * tuning.Age2) * tuning.Age3, 0, 1);
                if (age <= 0) continue;
                Vector2 fragment = root + Ellipse(frame.Unit(seed + 60) * MathF.Tau, tuning.EllipseX2, tuning.EllipseY2) * age;
                float remnant = Ease(age * tuning.Remnant) * (1 - tuning.Remnant2 * Ease((age - tuning.Remnant3) / tuning.RemnantDivisor));
                frame.Clip(fragment, tuning.SmokeSequenceWidth2 + tuning.SmokeSequenceWidth3 * age, tuning.SmokeSequenceHeight2 + tuning.SmokeSequenceHeight3 * age, tuning.SmokeSequenceShape2, frame.Unit(seed + 62) + age * tuning.SmokeSequencePhase2,
                    i % 2 == 0 ? tuning.SmokeSequenceColour2 : tuning.SmokeSequenceColour3, remnant, tuning.SmokeSequenceLayer2, age * tuning.SmokeSequenceRotation);
                frame.Sprite(fragment, tuning.MistFilamentWidth2 + tuning.MistFilamentWidth3 * pressure, tuning.MistFilamentHeight5 + tuning.MistFilamentHeight6 * age, tuning.MistFilamentShape3, tuning.MistFilamentColour3,
                    tuning.MistFilamentAlpha3 * remnant, tuning.MistFilamentLayer3, seed + age * tuning.MistFilamentRotation);
                frame.Sprite(fragment, tuning.SparkWidth + tuning.SparkWidth2 * pressure, tuning.SparkHeight, tuning.SparkShape, tuning.SparkColour,
                    tuning.SparkAlpha * remnant * (1 - age), tuning.SparkLayer);
            }
        }
        Vector2 escape = centre + new Vector2(tuning.X3 - tuning.X4 * breaking, tuning.Y - tuning.Y2 * breaking);
        frame.Sprite(escape, tuning.IonCoronaWidth, tuning.IonCoronaHeight, tuning.IonCoronaShape, tuning.IonCoronaColour, tuning.IonCoronaAlpha * breaking, tuning.IonCoronaLayer, tuning.IonCoronaRotation);
        frame.Sprite(escape, tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape2, tuning.WispColour2, tuning.WispAlpha2 * breaking, tuning.WispLayer2, light: tuning.WispLight);
        frame.Sprite(escape, tuning.FlashWidth3, tuning.FlashHeight3, tuning.FlashShape2, Mint, tuning.FlashAlpha2 * breaking, tuning.FlashLayer2);
    }

    private static void Reveal(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Reveal);
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + MathF.Sin(time * tuning.Centre2) * tuning.Centre3);
        float growth = tuning.Growth + tuning.Growth2 * Ease(progress * tuning.Growth3);
        int tint = prepare ? Jade : tuning.ReleaseTintColour;
        frame.Sprite(centre, tuning.OrbWidth * growth, tuning.OrbHeight * growth, tuning.OrbShape, Teal, tuning.OrbAlpha, light: tuning.OrbLight);
        frame.Sprite(centre, tuning.OrbWidth2 * growth, tuning.OrbHeight2 * growth, tuning.OrbShape2, tint, tuning.OrbAlpha2);
        frame.Sprite(centre + new Vector2(0, tuning.Y), tuning.SigilEyeWidth * growth, tuning.SigilEyeHeight * growth, tuning.SigilEyeShape,
            prepare && progress > .58f ? Ice : Gold, tuning.SigilEyeAlpha, tuning.SigilEyeLayer, tuning.SigilEyeRotation * MathF.Sin(time * tuning.X));
        frame.Sprite(centre - new Vector2(tuning.X2, tuning.Y2), tuning.FlashWidth * growth, tuning.FlashHeight * growth, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer);
        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float angle = time * tuning.Angle + i * tuning.Angle2;
            Vector2 point = centre + Ellipse(angle, tuning.EllipseX * growth, tuning.EllipseY * growth);
            frame.Thread(point, point + Ellipse(angle + tuning.EllipseAngle, tuning.EllipseX2, tuning.EllipseY2), tuning.ThreadStartWidth, tuning.ThreadEndWidth, prepare ? InkBlue : tuning.ReleaseThreadColour, tuning.ThreadAlpha, Depth(angle));
            frame.Sprite(point, tuning.SparkWidth, tuning.SparkHeight, tuning.SparkShape, i % 2 == 0 ? Ice : Mint, tuning.SparkAlpha);
        }
    }

    private static void Repulse(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Repulse);
        float open = Ease(progress * tuning.Open);
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        frame.Sprite(feet, tuning.OrbWidth + open * tuning.OrbWidth2, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        for (int i = 0; i < tuning.WardBackCount; i++)
        {
            float wave = Math.Clamp(open * tuning.Wave - i * tuning.Wave2, 0, 1);
            float width = tuning.Width + wave * tuning.Width2;
            float height = tuning.Height + wave * tuning.Height2;
            frame.Sprite(body, width, height, tuning.WardBackShape, tuning.WardBackColour, tuning.WardBackAlpha, tuning.WardBackLayer);
            frame.Sprite(body, width, height, tuning.WardFrontShape, tuning.WardFrontColour, tuning.WardFrontAlpha - wave * tuning.WardFrontAlpha2);
        }
        frame.Sparks(body, tuning.SparksCount, tuning.SparksRx + open * tuning.SparksRx2, tuning.SparksRy, progress, Ice, tuning.SparksRise);
    }

    private static void Cleanse(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Cleanse);
        float travel = Ease(progress);
        Vector2 head = feet - Vector2.UnitY * (prepare ? tuning.Head + travel * tuning.Head2 : tuning.Head3 + (1 - travel) * tuning.Head4);
        frame.Sprite(feet, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, InkBlue, tuning.OrbAlpha, tuning.OrbLayer);
        frame.Sprite(head, tuning.OrbWidth2, tuning.OrbHeight2, tuning.OrbShape2, Blue, tuning.OrbAlpha2, light: tuning.OrbLight);
        frame.Sprite(head, tuning.OrbWidth3, tuning.OrbHeight3, tuning.OrbShape3, White, tuning.OrbAlpha3, tuning.OrbLayer2);
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i));
            float offset = (frame.Unit(i + 7) - tuning.Offset) * (tuning.Offset2 + age * tuning.Offset3);
            Vector2 point = head + new Vector2(offset, tuning.Y + (prepare ? 1 : -1) * age * tuning.Y2);
            frame.Thread(point, point + new Vector2(-offset * tuning.X, prepare ? tuning.PrepareY : tuning.ReleaseY), tuning.ThreadStartWidth, tuning.ThreadEndWidth,
                i % 3 == 0 ? Mint : Blue, tuning.ThreadAlpha - age * tuning.ThreadAlpha2);
        }
        frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount, tuning.SparksRx * travel, tuning.SparksRy * travel, progress, Ice, tuning.SparksRise);
        if (prepare) return;
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float turn = i * MathF.Tau / tuning.TurnDivisor + progress * tuning.Turn;
            Vector2 root = feet + Ellipse(turn, tuning.EllipseX, tuning.EllipseY);
            Vector2 top = Vector2.Lerp(root, head, tuning.Top) + Vector2.UnitX * (MathF.Cos(turn) * tuning.Top2);
            Vector2 edge = root + new Vector2(MathF.Sin(turn) * tuning.X2, tuning.Y3 - frame.Unit(i) * tuning.Y4);
            frame.Thread(root, edge, tuning.ThreadStartWidth2, tuning.ThreadEndWidth2, Blue, tuning.ThreadAlpha3, Depth(turn));
            frame.Thread(edge, top, tuning.ThreadStartWidth3, tuning.ThreadEndWidth3, Mint, tuning.ThreadAlpha4 * (1 - progress), Depth(turn));
            frame.Sprite(edge, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, Blue, tuning.WispAlpha, Depth(turn), turn * tuning.WispRotation);
        }
    }

    private static void MassHeal(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.MassHeal);
        float bloom = tuning.Bloom + tuning.Bloom2 * Ease(progress * tuning.Bloom3);
        float radius = (prepare ? tuning.PrepareRadius : tuning.ReleaseRadius) * bloom;
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + (prepare ? tuning.PrepareCentre : tuning.ReleaseCentre) * Ease(progress));
        frame.Sprite(feet, radius * tuning.OrbWidth, radius * tuning.OrbHeight, tuning.OrbShape, Teal, tuning.OrbAlpha, tuning.OrbLayer);
        frame.Sprite(centre, radius * tuning.WispWidth, radius * tuning.WispHeight, tuning.WispShape, Jade,
            tuning.WispAlpha * (1 - Ease((progress - tuning.Value) / tuning.ValueDivisor)), tuning.WispLayer, light: tuning.WispLight);
        HealMotes(frame, centre, radius * tuning.Radius, progress, prepare ? tuning.PrepareCount : tuning.ReleaseCount);
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float angle = i * MathF.PI * tuning.Angle + progress * tuning.Angle2;
            Vector2 node = centre + Ellipse(angle, radius, radius * GroundAspect);
            Layer depth = Depth(angle);
            frame.Sprite(node, tuning.WispWidth2, tuning.WispHeight2, tuning.WispShape2, i % 2 == 0 ? Jade : Gold, tuning.WispAlpha2, depth, angle, light: tuning.WispLight2);
            float flare = tuning.Flare + tuning.Flare2 * MathF.Pow(MathF.Sin(progress * MathF.Tau * tuning.Flare3 + i * tuning.Flare4), tuning.Flare5);
            frame.Sprite(node, tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape3, i % 2 == 0 ? tuning.WispColour : Amber,
                tuning.WispAlpha3 * flare, depth, angle + MathF.PI * tuning.WispRotation, light: tuning.WispLight3);
            frame.Sprite(node, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Ivory, tuning.FlashAlpha, depth == Layer.Ground ? depth : tuning.FlashLayer, angle);
            for (int j = 0; j < tuning.SparkCount; j++)
            {
                int seed = i * 14 + j;
                float trail = angle - j * tuning.Trail;
                float spread = radius + (frame.Unit(seed + 62) - tuning.Spread) * (tuning.Spread2 + j * tuning.Spread3);
                Vector2 point = centre + Ellipse(trail, spread, spread * GroundAspect) - Vector2.UnitY * (frame.Unit(seed) * tuning.Point);
                frame.Sprite(point, tuning.SparkWidth + frame.Unit(seed + 45) * tuning.SparkWidth2, tuning.SparkHeight, tuning.SparkShape,
                    j % 3 == 0 ? Ice : j % 4 == 0 ? Gold : Mint, tuning.SparkAlpha - j * tuning.SparkAlpha2, Depth(trail), trail + j * tuning.SparkRotation);
            }
        }
    }

    private static void HealMotes(Frame frame, Vector2 centre, float radius, float progress, int count)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.HealMotes);
        for (int i = 0; i < count; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i + 271));
            float turn = i * tuning.Turn + progress * (tuning.Turn2 + tuning.Turn3 * frame.Unit(i + 71));
            float spread = radius * (tuning.Spread + frame.Unit(i + 113) * tuning.Spread2 + age * tuning.Spread3);
            Vector2 root = centre + ProjectGround(Ellipse(turn, spread, spread));
            Vector2 point = root - Vector2.UnitY * (age * (tuning.Point + tuning.Point2 * frame.Unit(i + 31)));
            float size = tuning.Size + tuning.Size2 * frame.Unit(i + 173);
            float shimmer = MathF.Sin(age * MathF.PI) * (tuning.Shimmer + tuning.Shimmer2 * MathF.Pow(MathF.Sin(age * tuning.Shimmer3 + i), tuning.Shimmer4));
            Layer depth = Depth(turn);
            int tint = i % 5 == 0 ? Gold : i % 2 == 0 ? Mint : Ice;
            frame.Sprite(point, size, size * tuning.FlashHeight, i % 6 == 0 ? tuning.FlashShape : tuning.FlashShape2, tint,
                tuning.FlashAlpha * shimmer, depth == Layer.Ground ? depth : tuning.FlashLayer, turn);
            if (i % 7 == 0)
                frame.Sprite(point, size * tuning.OrbWidth, size * tuning.OrbHeight, tuning.OrbShape, tint, tuning.OrbAlpha * shimmer, depth, light: tuning.OrbLight);
        }
    }
}
