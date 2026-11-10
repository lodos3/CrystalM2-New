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
        frame.Clip(body, tuning.SmokeSequenceWidth * bloom, tuning.SmokeSequenceHeight * bloom, tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer, emitter: "TaoistSpellEffects.Rituals.Poison.1");
        frame.Sprite(body, tuning.OrbWidth * bloom, tuning.OrbHeight * bloom, tuning.OrbShape, bodyTint, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "TaoistSpellEffects.Rituals.Poison.2");
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float age = Fract(progress * tuning.Age + i * tuning.Age2);
            float angle = i * tuning.Angle + time * tuning.Angle2;
            Vector2 point = body + Ellipse(angle, tuning.EllipseX * bloom, tuning.EllipseY * bloom) * (tuning.Point + age * tuning.Point2) -
                Vector2.UnitY * (age * tuning.Point3);
            frame.Sprite(point, tuning.WispWidth + (1 - age) * tuning.WispWidth2, tuning.WispHeight + (1 - age) * tuning.WispHeight2, tuning.WispShape,
                i % 3 == 0 ? tuning.WispColour : bodyTint, tuning.WispAlpha - tuning.WispAlpha2 * age, Depth(angle), emitter: "TaoistSpellEffects.Rituals.Poison.3");
            if (!prepare) frame.Sprite(point, tuning.SparkWidth + frame.Unit(i) * tuning.SparkWidth2, tuning.SparkHeight, tuning.SparkShape, Gold, tuning.SparkAlpha - age * tuning.SparkAlpha2, tuning.SparkLayer, emitter: "TaoistSpellEffects.Rituals.Poison.4");
        }
        if (prepare)
            frame.Orbit(body, tuning.OrbitX * bloom, tuning.OrbitY * bloom, progress * tuning.OrbitAngle, tuning.OrbitSweep, tuning.OrbitColour, tuning.OrbitWidth, tuning.OrbitAlpha, emitter: "TaoistSpellEffects.Rituals.Poison.5");
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
            tuning.WispAlpha * heat, tuning.WispLayer, light: tuning.WispLight, emitter: "TaoistSpellEffects.Rituals.PaperBurst.1");
        for (int i = 0; i < tuning.FireSequenceCount; i++)
        {
            float angle = i * tuning.Angle;
            Vector2 ray = Ellipse(angle, tuning.EllipseX, tuning.EllipseY);
            float age = Math.Clamp(progress * tuning.Age - i * tuning.Age2, 0, 1);
            Vector2 lobe = centre + (ray + new Vector2(tuning.X, tuning.Y * age)) * spread;
            float flare = (tuning.Flare + tuning.Flare2 * Ease(age * tuning.Flare3)) * (1 - tuning.Flare4 * Ease((age - tuning.Flare5) / tuning.FlareDivisor));
            frame.Clip(lobe, (tuning.FireSequenceWidth + i * tuning.FireSequenceWidth2) * spread, (tuning.FireSequenceHeight + i * tuning.FireSequenceHeight2) * spread, tuning.FireSequenceShape,
                tuning.FireSequencePhase + age * tuning.FireSequencePhase2 + i * tuning.FireSequencePhase3, PaperFlame.Sample(age), tuning.FireSequenceAlpha * flare, rotation: MathF.Cos(angle) * tuning.FireSequenceRotation, emitter: "TaoistSpellEffects.Rituals.PaperBurst.2");
            frame.Clip(Vector2.Lerp(centre, lobe, tuning.Amount), tuning.FireSequenceWidth3 * spread, tuning.FireSequenceHeight3 * spread, tuning.FireSequenceShape2,
                tuning.FireSequencePhase4 + age * tuning.FireSequencePhase5 + i * tuning.FireSequencePhase6, PaperHeat.Sample(age), tuning.FireSequenceAlpha2 * flare, tuning.FireSequenceLayer, MathF.Sin(angle) * tuning.FireSequenceRotation2, emitter: "TaoistSpellEffects.Rituals.PaperBurst.3");
            frame.Sprite(Vector2.Lerp(centre, lobe, tuning.Amount2), tuning.UserFlameWidth * spread, tuning.UserFlameHeight * spread, tuning.UserFlameShape,
                PaperCore.Sample(age), heat * flare * tuning.UserFlameAlpha, tuning.UserFlameLayer, angle - tuning.UserFlameRotation, light: i % 2 == 0, emitter: "TaoistSpellEffects.Rituals.PaperBurst.4");
            frame.Clip(lobe + new Vector2(tuning.X2 + age * tuning.X3, tuning.Y2 - age * tuning.Y3), tuning.SmokeSequenceWidth * spread, tuning.SmokeSequenceHeight * spread,
                tuning.SmokeSequenceShape, age * tuning.SmokeSequencePhase + i * tuning.SmokeSequencePhase2, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * Ease(age * tuning.Value), tuning.SmokeSequenceLayer, emitter: "TaoistSpellEffects.Rituals.PaperBurst.5");
        }
        frame.Clip(centre, tuning.FireSequenceWidth4 * spread, tuning.FireSequenceHeight4 * spread, tuning.FireSequenceShape3, tuning.FireSequencePhase7 + progress * tuning.FireSequencePhase8,
            PaperCore.Sample(progress), heat, tuning.FireSequenceLayer2, light: tuning.FireSequenceLight, emitter: "TaoistSpellEffects.Rituals.PaperBurst.6");
        frame.Sprite(centre - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth * spread, tuning.FlashHeight * spread, tuning.FlashShape, White,
            tuning.FlashAlpha * heat, tuning.FlashLayer, emitter: "TaoistSpellEffects.Rituals.PaperBurst.7");
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
                alpha, tuning.UserEmberLayer, angle, light: i % 9 == 0, emitter: "TaoistSpellEffects.Rituals.PaperBurst.8");
            if (i % 4 == 0)
            {
                frame.Sprite(position, size * tuning.FlashWidth2, size * tuning.FlashHeight2, tuning.FlashShape2, tuning.FlashColour, tuning.FlashAlpha2 * alpha, tuning.FlashLayer2, emitter: "TaoistSpellEffects.Rituals.PaperBurst.9");
                frame.Sprite(position, size * tuning.SparkWidth, size * tuning.SparkHeight, tuning.SparkShape, PaperEmber.Sample(age * tuning.Offset),
                    tuning.SparkAlpha * alpha, tuning.SparkLayer, angle, emitter: "TaoistSpellEffects.Rituals.PaperBurst.10");
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
            tuning.HaloAlpha * (1 - Ease((progress - tuning.Value) / tuning.ValueDivisor)), tuning.HaloLayer, light: tuning.HaloLight, emitter: "TaoistSpellEffects.Rituals.SummonStars.1");
        frame.Sprite(feet, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, dusk, tuning.WispAlpha, tuning.WispLayer, emitter: "TaoistSpellEffects.Rituals.SummonStars.2");
        Vector2 crown = feet - Vector2.UnitY * (tuning.Crown + lift * tuning.Crown2);
        frame.Sprite(crown, tuning.IonCoronaWidth, tuning.IonCoronaHeight, tuning.IonCoronaShape, glow, tuning.IonCoronaAlpha * ascension, tuning.IonCoronaLayer, tuning.IonCoronaRotation, emitter: "TaoistSpellEffects.Rituals.SummonStars.3");
        frame.Sprite(crown, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, deva ? Ivory : Ice, tuning.FlashAlpha * ascension,
            tuning.FlashLayer, tuning.FlashRotation, light: tuning.FlashLight, emitter: "TaoistSpellEffects.Rituals.SummonStars.4");
        for (int i = 0; i < (deva ? tuning.WispCount : tuning.WispCount2); i++)
        {
            float t = Math.Clamp(progress * tuning.T - i * tuning.T2, 0, 1);
            float turn = progress * tuning.Turn + i * tuning.Turn2;
            Vector2 star = feet + Ellipse(turn, tuning.EllipseX + i * tuning.EllipseX2, tuning.EllipseY) - Vector2.UnitY * (tuning.Star + lift + i * tuning.Star2);
            frame.Sprite(star, tuning.WispWidth2 + i * tuning.WispWidth3, tuning.WispHeight2, tuning.WispShape2, i % 2 == 0 ? dusk : Teal, tuning.WispAlpha2, Depth(turn), emitter: "TaoistSpellEffects.Rituals.SummonStars.5");
            float flare = tuning.Flare + tuning.Flare2 * MathF.Pow(MathF.Sin(progress * MathF.Tau * tuning.Flare3 + i), tuning.Flare4);
            frame.Sprite(star, tuning.WispWidth4, tuning.WispHeight3, tuning.WispShape3, glow, tuning.WispAlpha3 * flare, Depth(turn), turn, light: i % 2 == 0, emitter: "TaoistSpellEffects.Rituals.SummonStars.6");
            frame.Sprite(star, tuning.FlashWidth2 * (1 - t * tuning.FlashWidth3), tuning.FlashHeight2, tuning.FlashShape2, i == 0 ? Ice : glow,
                tuning.FlashAlpha2 * flare, Depth(turn), turn, emitter: "TaoistSpellEffects.Rituals.SummonStars.7");
            frame.Sprite(star, tuning.FlashWidth4, tuning.FlashHeight3, tuning.FlashShape3, White, tuning.FlashAlpha3, tuning.FlashLayer2, -turn, emitter: "TaoistSpellEffects.Rituals.SummonStars.8");
        }
        frame.Sparks(feet - Vector2.UnitY * (tuning.FeatherOffset + lift), tuning.FeatherCount, tuning.FeatherRx, tuning.FeatherRy, progress, deva ? Gold : Mint, tuning.FeatherRise,
            deva ? tuning.FeatherShape : tuning.FeatherShape2, emitter: "TaoistSpellEffects.Rituals.SummonStars.9");
        for (int i = 0; i < tuning.FlashCount; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i + 173));
            float turn = i * tuning.Turn3 + progress * tuning.Turn4;
            Vector2 point = feet + Ellipse(turn, tuning.EllipseX3 + tuning.EllipseX4 * age, tuning.EllipseY2 + tuning.EllipseY3 * age) -
                Vector2.UnitY * (age * (tuning.Point + lift) + tuning.Point2);
            frame.Sprite(point, tuning.FlashWidth5 + frame.Unit(i + 31) * tuning.FlashWidth6, tuning.FlashHeight4 + age * tuning.FlashHeight5,
                i % 4 == 0 ? tuning.FlashShape4 : tuning.FlashShape5, i % 3 == 0 ? Ice : glow,
                tuning.FlashAlpha4 * (1 - age), tuning.FlashLayer3, turn, light: i == 0, emitter: "TaoistSpellEffects.Rituals.SummonStars.10");
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
                tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase + i * tuning.SmokeSequencePhase2, tint, tuning.SmokeSequenceAlpha * opacity, layer, tilt, sortY: basePoint.Y, emitter: "TaoistSpellEffects.Rituals.Veil.1");
            frame.Clip(point + new Vector2(tilt * tuning.X3, tuning.Y * (1 - hit)), tuning.SmokeSequenceWidth4 + hit * tuning.SmokeSequenceWidth5, tuning.SmokeSequenceHeight4 - hit * tuning.SmokeSequenceHeight5,
                tuning.SmokeSequenceShape2, time * tuning.SmokeSequencePhase3 + i * tuning.SmokeSequencePhase4, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha2 * opacity, layer, -tilt, sortY: basePoint.Y, emitter: "TaoistSpellEffects.Rituals.Veil.2");
            if (i % 3 == 0)
                frame.Sprite(point, tuning.WispWidth + tuning.WispWidth2 * hit, tuning.WispHeight - hit * tuning.WispHeight2, tuning.WispShape, tuning.WispColour,
                    tuning.WispAlpha * opacity * (1 - hit * tuning.WispAlpha2), layer == Layer.Smoke ? tuning.WispLayer : layer, tilt,
                    light: tuning.WispLight, sortY: basePoint.Y, emitter: "TaoistSpellEffects.Rituals.Veil.3");
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
                tuning.WispLayer2, turn * tuning.WispRotation, emitter: "TaoistSpellEffects.Rituals.Veil.4");
            if (i % 3 == 0)
                frame.Sprite(point - Vector2.UnitY * (tuning.SparkOffset + age * tuning.SparkOffset2), tuning.SparkWidth + age * tuning.SparkWidth2, tuning.SparkHeight,
                    tuning.SparkShape, tuning.SparkColour, tuning.SparkAlpha * dustFade * ground, tuning.SparkLayer, emitter: "TaoistSpellEffects.Rituals.Veil.5");
        }
    }

    private static void BurningWard(Frame frame, Vector2 feet, float progress, Shape glyph)
    {
        var tuning = SpellVfxAssets.Current.Taoist.BurningWard;
        var fields = SpellVfxAssets.Fields("Taoist", "BurningWard");
        bool armour = glyph == Shape.ArmourGlyph;
        float width = armour ? fields.Read(tuning.ArmourWidth) : fields.Read(tuning.SoulShieldWidth), height = armour ? fields.Read(tuning.ArmourHeight) : fields.Read(tuning.SoulShieldHeight);
        var flame = armour ? ArmourFlame : SoulFlame;
        int glow = armour ? fields.Read(tuning.ArmourGlowColour) : fields.Read(tuning.SoulShieldGlowColour);
        Vector2 centre = feet + (armour ? new Vector2(fields.Read(tuning.X), fields.Read(tuning.Y)) : new Vector2(0, fields.Read(tuning.Y2)));
        Vector2 scale = new(width * fields.Read(tuning.Scale), height * fields.Read(tuning.Scale2));
        float front = armour ? fields.Read(tuning.Front) + progress * fields.Read(tuning.Front2) : fields.Read(tuning.Front3) + progress * fields.Read(tuning.Front4);
        float formed = Ease((front - fields.Read(tuning.Formed)) / fields.Read(tuning.FormedDivisor));
        float settling = Ease((progress - fields.Read(tuning.Settling)) / fields.Read(tuning.SettlingDivisor));
        // The rune forms behind its fire, then burns away while the remaining
        // smoke continues rising off the same fixed brush-stroke positions.
        float dissipation = 1 - Ease((progress - fields.Read(tuning.DissipationStart)) / fields.Read(tuning.DissipationDuration));
        frame.Sprite(centre, width * fields.Read(tuning.SpriteWidth), height * fields.Read(tuning.SpriteHeight), glyph, armour ? fields.Read(tuning.ArmourSpriteColour) : fields.Read(tuning.SoulShieldSpriteColour),
            formed * dissipation * fields.Read(tuning.SpriteAlpha), fields.Read(tuning.SpriteLayer), emitter: "TaoistSpellEffects.Rituals.BurningWard.1");
        frame.Sprite(centre, width, height, glyph, armour ? fields.Read(tuning.ArmourSpriteColour2) : fields.Read(tuning.SoulShieldSpriteColour2),
            formed * dissipation * (fields.Read(tuning.SpriteAlpha2) + fields.Read(tuning.SpriteAlpha3) * settling), emitter: "TaoistSpellEffects.Rituals.BurningWard.2");
        frame.Sprite(centre, width, height, glyph, armour ? fields.Read(tuning.ArmourSpriteColour3) : fields.Read(tuning.SoulShieldSpriteColour3),
            formed * dissipation * (fields.Read(tuning.SpriteAlpha4) + fields.Read(tuning.SpriteAlpha5) * settling), fields.Read(tuning.SpriteLayer2), emitter: "TaoistSpellEffects.Rituals.BurningWard.3");
        if (armour)
        {
            float spark = 1 - Ease(progress / fields.Read(tuning.SparkDivisor));
            Vector2 source = feet - Vector2.UnitY * (fields.Read(tuning.Source) + fields.Read(tuning.Source2) * progress);
            frame.Sprite(source, fields.Read(tuning.WispWidth), fields.Read(tuning.WispHeight), fields.Read(tuning.WispShape), fields.Read(tuning.WispColour), fields.Read(tuning.WispAlpha) * spark, fields.Read(tuning.WispLayer), emitter: "TaoistSpellEffects.Rituals.BurningWard.4");
            frame.Sprite(source, fields.Read(tuning.UserFlameWidth), fields.Read(tuning.UserFlameHeight), fields.Read(tuning.UserFlameShape), Amber, fields.Read(tuning.UserFlameAlpha) * spark, fields.Read(tuning.UserFlameLayer), light: fields.Read(tuning.UserFlameLight), emitter: "TaoistSpellEffects.Rituals.BurningWard.5");
            frame.Sprite(source, fields.Read(tuning.FlashWidth), fields.Read(tuning.FlashHeight), fields.Read(tuning.FlashShape), Ivory, fields.Read(tuning.FlashAlpha) * spark, fields.Read(tuning.FlashLayer), emitter: "TaoistSpellEffects.Rituals.BurningWard.6");
        }

        for (int stroke = 0; stroke < TaoistRuneBrush.StrokeCount(glyph); stroke++)
        {
            ReadOnlySpan<Vector3> points = TaoistRuneBrush.Stroke(glyph, stroke);
            for (int i = 0; i < points.Length; i += fields.Read(tuning.BrushKnotStride))
            {
                Vector3 knot = points[i];
                int salt = stroke * fields.Read(tuning.StrokeSeedStride) + i * fields.Read(tuning.KnotSeedStride);
                float ignition = fields.Read(tuning.Ignition) + fields.Read(tuning.Ignition2) * knot.X + fields.Read(tuning.Ignition3) * knot.Y + fields.Read(tuning.Ignition4) * frame.Unit(salt);
                if (front < ignition) continue;
                Vector2 root = centre + new Vector2(knot.X, knot.Y) * scale;
                float age = Fract(progress * (fields.Read(tuning.Age) + frame.Unit(salt + 3)) + frame.Unit(salt + 17));
                float press = Math.Clamp(knot.Z / fields.Read(tuning.PressDivisor), 0, armour ? 1.35f : 1);
                float size = fields.Read(tuning.Size) + (armour ? fields.Read(tuning.ArmourSize) : fields.Read(tuning.SoulShieldSize)) * press;
                float lift = (fields.Read(tuning.Lift) + (armour ? fields.Read(tuning.ArmourLift) : fields.Read(tuning.SoulShieldLift)) * press) * (1 - settling * fields.Read(tuning.Lift2));
                float rise = (fields.Read(tuning.Rise) + (armour ? fields.Read(tuning.ArmourRise) : fields.Read(tuning.SoulShieldRise)) * press) * age;
                float tilt = fields.Read(tuning.Tilt) * MathF.Sin(age * MathF.Tau + salt);
                float alpha = (fields.Read(tuning.Alpha) + fields.Read(tuning.Alpha2) * MathF.Sin(age * MathF.PI)) *
                    (1 - settling * (armour ? fields.Read(tuning.ArmourAlpha) : fields.Read(tuning.SoulShieldAlpha))) * (fields.Read(tuning.Alpha3) + fields.Read(tuning.Alpha4) * dissipation);
                frame.Clip(root - Vector2.UnitY * (lift * fields.Read(tuning.FlameSequenceOffset) + rise * fields.Read(tuning.FlameSequenceOffset2)), size, lift, fields.Read(tuning.FlameSequenceShape), age,
                    flame.Sample(fields.Read(tuning.Offset) + age * fields.Read(tuning.Offset2)), fields.Read(tuning.FlameSequenceAlpha) * alpha, fields.Read(tuning.FlameSequenceLayer), tilt, emitter: "TaoistSpellEffects.Rituals.BurningWard.7");
                frame.Sprite(root - Vector2.UnitY * (lift * fields.Read(tuning.UserFlameOffset) + rise * fields.Read(tuning.UserFlameOffset2)), size * fields.Read(tuning.UserFlameWidth2), lift * fields.Read(tuning.UserFlameHeight2), fields.Read(tuning.UserFlameShape2),
                    flame.Sample(age * fields.Read(tuning.Offset3)), alpha, fields.Read(tuning.UserFlameLayer2), -tilt, light: i % 6 == 0, emitter: "TaoistSpellEffects.Rituals.BurningWard.8");
                frame.Sprite(root - Vector2.UnitY * (lift * fields.Read(tuning.FlameTongueOffset) + rise), size * fields.Read(tuning.FlameTongueWidth), lift, fields.Read(tuning.FlameTongueShape),
                    flame.Sample(age * fields.Read(tuning.Offset4)), fields.Read(tuning.FlameTongueAlpha) * alpha, fields.Read(tuning.FlameTongueLayer), tilt, emitter: "TaoistSpellEffects.Rituals.BurningWard.9");
                if (armour && i % 4 == 0)
                    frame.Clip(root + new Vector2(tilt * fields.Read(tuning.X2), -lift * fields.Read(tuning.Y3)), size * fields.Read(tuning.FlameSequenceWidth), lift * fields.Read(tuning.FlameSequenceHeight),
                        fields.Read(tuning.FlameSequenceShape2), age + fields.Read(tuning.FlameSequencePhase), flame.Sample(age * fields.Read(tuning.Offset5)), fields.Read(tuning.FlameSequenceAlpha2) * alpha, fields.Read(tuning.FlameSequenceLayer2), tilt, emitter: "TaoistSpellEffects.Rituals.BurningWard.10");
                if (i % 4 == 0)
                {
                    // Thin, independently phased smoke plumes peel upwards and
                    // drift sideways. Keep these attached to actual glyph knots,
                    // rather than introducing a backdrop billboard or ellipse.
                    float release = Ease((progress - fields.Read(tuning.SmokeReleaseStart)) / fields.Read(tuning.SmokeReleaseDuration));
                    if (release > .014f)
                    {
                        float smokeAge = Fract(progress * (fields.Read(tuning.SmokeAge) + frame.Unit(salt + 75) * fields.Read(tuning.SmokeAge2)) +
                            frame.Unit(salt + 76));
                        float sideways = (frame.Unit(salt + 78) * fields.Read(tuning.Sideways) - 1) * (fields.Read(tuning.Sideways2) + fields.Read(tuning.Sideways3) * smokeAge);
                        Vector2 plume = root + new Vector2(sideways, fields.Read(tuning.SmokeBaseOffsetY) - (fields.Read(tuning.SmokeBaseRise) + fields.Read(tuning.SmokePressureRise) * press) * smokeAge);
                        float opacity = (fields.Read(tuning.SmokeBaseOpacity) + fields.Read(tuning.SmokeReleaseOpacity) * release) * (1 - fields.Read(tuning.SmokeAgeFade) * smokeAge) *
                            (fields.Read(tuning.SmokeEarlyOpacity) + fields.Read(tuning.SmokeSettlingOpacity) * settling);
                        frame.Clip(plume, fields.Read(tuning.SmokeSequenceWidth) + fields.Read(tuning.SmokeSequenceWidth2) * smokeAge, fields.Read(tuning.SmokeSequenceHeight) + fields.Read(tuning.SmokeSequenceHeight2) * smokeAge, fields.Read(tuning.SmokeSequenceShape),
                            smokeAge + fields.Read(tuning.SmokeSequencePhase), armour ? fields.Read(tuning.ArmourSmokeSequenceColour) : fields.Read(tuning.SoulShieldSmokeSequenceColour), opacity, fields.Read(tuning.SmokeSequenceLayer), tilt * fields.Read(tuning.SmokeSequenceRotation), emitter: "TaoistSpellEffects.Rituals.BurningWard.11");
                    }
                    frame.Sprite(root, Math.Min(fields.Read(tuning.KnotGlowMaxWidth), size * (armour ? fields.Read(tuning.ArmourKnotGlowWidthScale) : fields.Read(tuning.SoulShieldKnotGlowWidthScale))), lift * fields.Read(tuning.WispHeight2),
                        fields.Read(tuning.WispShape2), glow, fields.Read(tuning.WispAlpha2) * alpha, fields.Read(tuning.WispLayer2), emitter: "TaoistSpellEffects.Rituals.BurningWard.12");
                    frame.Sprite(root - Vector2.UnitY * (fields.Read(tuning.UserEmberOffset) + lift * age), fields.Read(tuning.UserEmberWidth), fields.Read(tuning.UserEmberHeight), fields.Read(tuning.UserEmberShape),
                        flame.Sample(age), fields.Read(tuning.UserEmberAlpha) * alpha, fields.Read(tuning.UserEmberLayer), tilt, emitter: "TaoistSpellEffects.Rituals.BurningWard.13");
                    if (i % 8 == 0)
                        frame.Sprite(root, size * fields.Read(tuning.FlashWidth2), lift * fields.Read(tuning.FlashHeight2), fields.Read(tuning.FlashShape2), armour ? Gold : Ice, fields.Read(tuning.FlashAlpha2) * alpha, fields.Read(tuning.FlashLayer2), emitter: "TaoistSpellEffects.Rituals.BurningWard.14");
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
            visible * (tuning.CurseGlyphAlpha + tuning.CurseGlyphAlpha2 * surge), tuning.CurseGlyphLayer, emitter: "TaoistSpellEffects.Rituals.CurseRune.1");
        frame.Sprite(centre, width, height, tuning.CurseGlyphShape2, tuning.CurseGlyphColour2,
            visible * (tuning.CurseGlyphAlpha3 + tuning.CurseGlyphAlpha4 * surge), tuning.CurseGlyphLayer2, light: tuning.CurseGlyphLight, emitter: "TaoistSpellEffects.Rituals.CurseRune.2");
        frame.Sprite(centre, width, height, tuning.CurseGlyphShape3, tuning.CurseGlyphColour3,
            visible * (tuning.CurseGlyphAlpha5 + tuning.CurseGlyphAlpha6 * surge), tuning.CurseGlyphLayer3, emitter: "TaoistSpellEffects.Rituals.CurseRune.3");

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
                    tuning.WispAlpha * flare, tuning.WispLayer, light: i % 6 == 0, emitter: "TaoistSpellEffects.Rituals.CurseRune.4");
                frame.Sprite(root + Ellipse(frame.Unit(seed + 12) * MathF.Tau, tuning.EllipseX, tuning.EllipseY),
                    tuning.FlashWidth + tuning.FlashWidth2 * pressure, tuning.FlashHeight + tuning.FlashHeight2 * pressure, tuning.FlashShape, i % 2 == 0 ? tuning.FlashColour : Mint,
                    tuning.FlashAlpha * flare, tuning.FlashLayer, seed, emitter: "TaoistSpellEffects.Rituals.CurseRune.5");

                if (i % 6 == 0 && pressure > .40f && surge > .025f)
                {
                    frame.Clip(root - Vector2.UnitY * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape,
                        frame.Unit(seed + 18) + progress * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * flare, tuning.SmokeSequenceLayer, emitter: "TaoistSpellEffects.Rituals.CurseRune.6");
                    Vector2 ray = Vector2.Normalize(new Vector2(knot.X * (tuning.X + frame.Unit(seed + 28) * tuning.X2), -1));
                    float length = (tuning.Length + tuning.Length2 * frame.Unit(seed + 29)) * (tuning.Length3 + tuning.Length4 * surge);
                    float angle = MathF.Atan2(ray.Y, ray.X);
                    frame.Sprite(root + ray * length * tuning.PillarOffset, tuning.PillarWidth + tuning.PillarWidth2 * pressure, length, tuning.PillarShape,
                        tuning.PillarColour, tuning.PillarAlpha * flare * surge, tuning.PillarLayer, angle + MathF.PI * tuning.PillarRotation, emitter: "TaoistSpellEffects.Rituals.CurseRune.7");
                    frame.Sprite(root + ray * length * tuning.MistFilamentOffset, length, tuning.MistFilamentHeight + tuning.MistFilamentHeight2 * pressure, tuning.MistFilamentShape, tuning.MistFilamentColour,
                        tuning.MistFilamentAlpha * flare * surge, tuning.MistFilamentLayer, angle, emitter: "TaoistSpellEffects.Rituals.CurseRune.8");
                    frame.Sprite(root + ray * length * tuning.MistFilamentOffset2, length * tuning.MistFilamentWidth, tuning.MistFilamentHeight3 + tuning.MistFilamentHeight4 * pressure, tuning.MistFilamentShape2, tuning.MistFilamentColour2,
                        tuning.MistFilamentAlpha2 * flare * surge, tuning.MistFilamentLayer2, angle, emitter: "TaoistSpellEffects.Rituals.CurseRune.9");
                }

                float age = Math.Clamp((progress - tuning.Age - frame.Unit(seed + 51) * tuning.Age2) * tuning.Age3, 0, 1);
                if (age <= 0) continue;
                Vector2 fragment = root + Ellipse(frame.Unit(seed + 60) * MathF.Tau, tuning.EllipseX2, tuning.EllipseY2) * age;
                float remnant = Ease(age * tuning.Remnant) * (1 - tuning.Remnant2 * Ease((age - tuning.Remnant3) / tuning.RemnantDivisor));
                frame.Clip(fragment, tuning.SmokeSequenceWidth2 + tuning.SmokeSequenceWidth3 * age, tuning.SmokeSequenceHeight2 + tuning.SmokeSequenceHeight3 * age, tuning.SmokeSequenceShape2, frame.Unit(seed + 62) + age * tuning.SmokeSequencePhase2,
                    i % 2 == 0 ? tuning.SmokeSequenceColour2 : tuning.SmokeSequenceColour3, remnant, tuning.SmokeSequenceLayer2, age * tuning.SmokeSequenceRotation, emitter: "TaoistSpellEffects.Rituals.CurseRune.10");
                frame.Sprite(fragment, tuning.MistFilamentWidth2 + tuning.MistFilamentWidth3 * pressure, tuning.MistFilamentHeight5 + tuning.MistFilamentHeight6 * age, tuning.MistFilamentShape3, tuning.MistFilamentColour3,
                    tuning.MistFilamentAlpha3 * remnant, tuning.MistFilamentLayer3, seed + age * tuning.MistFilamentRotation, emitter: "TaoistSpellEffects.Rituals.CurseRune.11");
                frame.Sprite(fragment, tuning.SparkWidth + tuning.SparkWidth2 * pressure, tuning.SparkHeight, tuning.SparkShape, tuning.SparkColour,
                    tuning.SparkAlpha * remnant * (1 - age), tuning.SparkLayer, emitter: "TaoistSpellEffects.Rituals.CurseRune.12");
            }
        }
        Vector2 escape = centre + new Vector2(tuning.X3 - tuning.X4 * breaking, tuning.Y - tuning.Y2 * breaking);
        frame.Sprite(escape, tuning.IonCoronaWidth, tuning.IonCoronaHeight, tuning.IonCoronaShape, tuning.IonCoronaColour, tuning.IonCoronaAlpha * breaking, tuning.IonCoronaLayer, tuning.IonCoronaRotation, emitter: "TaoistSpellEffects.Rituals.CurseRune.13");
        frame.Sprite(escape, tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape2, tuning.WispColour2, tuning.WispAlpha2 * breaking, tuning.WispLayer2, light: tuning.WispLight, emitter: "TaoistSpellEffects.Rituals.CurseRune.14");
        frame.Sprite(escape, tuning.FlashWidth3, tuning.FlashHeight3, tuning.FlashShape2, Mint, tuning.FlashAlpha2 * breaking, tuning.FlashLayer2, emitter: "TaoistSpellEffects.Rituals.CurseRune.15");
    }

    private static void Reveal(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Reveal);
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + MathF.Sin(time * tuning.Centre2) * tuning.Centre3);
        float growth = tuning.Growth + tuning.Growth2 * Ease(progress * tuning.Growth3);
        int tint = prepare ? Jade : tuning.ReleaseTintColour;
        frame.Sprite(centre, tuning.OrbWidth * growth, tuning.OrbHeight * growth, tuning.OrbShape, Teal, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "TaoistSpellEffects.Rituals.Reveal.1");
        frame.Sprite(centre, tuning.OrbWidth2 * growth, tuning.OrbHeight2 * growth, tuning.OrbShape2, tint, tuning.OrbAlpha2, emitter: "TaoistSpellEffects.Rituals.Reveal.2");
        frame.Sprite(centre + new Vector2(0, tuning.Y), tuning.SigilEyeWidth * growth, tuning.SigilEyeHeight * growth, tuning.SigilEyeShape,
            prepare && progress > .58f ? Ice : Gold, tuning.SigilEyeAlpha, tuning.SigilEyeLayer, tuning.SigilEyeRotation * MathF.Sin(time * tuning.X), emitter: "TaoistSpellEffects.Rituals.Reveal.3");
        frame.Sprite(centre - new Vector2(tuning.X2, tuning.Y2), tuning.FlashWidth * growth, tuning.FlashHeight * growth, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer, emitter: "TaoistSpellEffects.Rituals.Reveal.4");
        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float angle = time * tuning.Angle + i * tuning.Angle2;
            Vector2 point = centre + Ellipse(angle, tuning.EllipseX * growth, tuning.EllipseY * growth);
            frame.Thread(point, point + Ellipse(angle + tuning.EllipseAngle, tuning.EllipseX2, tuning.EllipseY2), tuning.ThreadStartWidth, tuning.ThreadEndWidth, prepare ? InkBlue : tuning.ReleaseThreadColour, tuning.ThreadAlpha, Depth(angle), emitter: "TaoistSpellEffects.Rituals.Reveal.5");
            frame.Sprite(point, tuning.SparkWidth, tuning.SparkHeight, tuning.SparkShape, i % 2 == 0 ? Ice : Mint, tuning.SparkAlpha, emitter: "TaoistSpellEffects.Rituals.Reveal.6");
        }
    }

    private static void Repulse(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Repulse);
        float open = Ease(progress * tuning.Open);
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        frame.Sprite(feet, tuning.OrbWidth + open * tuning.OrbWidth2, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight, emitter: "TaoistSpellEffects.Rituals.Repulse.1");
        for (int i = 0; i < tuning.WardBackCount; i++)
        {
            float wave = Math.Clamp(open * tuning.Wave - i * tuning.Wave2, 0, 1);
            float width = tuning.Width + wave * tuning.Width2;
            float height = tuning.Height + wave * tuning.Height2;
            frame.Sprite(body, width, height, tuning.WardBackShape, tuning.WardBackColour, tuning.WardBackAlpha, tuning.WardBackLayer, emitter: "TaoistSpellEffects.Rituals.Repulse.2");
            frame.Sprite(body, width, height, tuning.WardFrontShape, tuning.WardFrontColour, tuning.WardFrontAlpha - wave * tuning.WardFrontAlpha2, emitter: "TaoistSpellEffects.Rituals.Repulse.3");
        }
        frame.Sparks(body, tuning.SparksCount, tuning.SparksRx + open * tuning.SparksRx2, tuning.SparksRy, progress, Ice, tuning.SparksRise, emitter: "TaoistSpellEffects.Rituals.Repulse.4");
    }

    private static void Cleanse(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Cleanse);
        float travel = Ease(progress);
        Vector2 head = feet - Vector2.UnitY * (prepare ? tuning.Head + travel * tuning.Head2 : tuning.Head3 + (1 - travel) * tuning.Head4);
        frame.Sprite(feet, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, InkBlue, tuning.OrbAlpha, tuning.OrbLayer, emitter: "TaoistSpellEffects.Rituals.Cleanse.1");
        frame.Sprite(head, tuning.OrbWidth2, tuning.OrbHeight2, tuning.OrbShape2, Blue, tuning.OrbAlpha2, light: tuning.OrbLight, emitter: "TaoistSpellEffects.Rituals.Cleanse.2");
        frame.Sprite(head, tuning.OrbWidth3, tuning.OrbHeight3, tuning.OrbShape3, White, tuning.OrbAlpha3, tuning.OrbLayer2, emitter: "TaoistSpellEffects.Rituals.Cleanse.3");
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i));
            float offset = (frame.Unit(i + 7) - tuning.Offset) * (tuning.Offset2 + age * tuning.Offset3);
            Vector2 point = head + new Vector2(offset, tuning.Y + (prepare ? 1 : -1) * age * tuning.Y2);
            frame.Thread(point, point + new Vector2(-offset * tuning.X, prepare ? tuning.PrepareY : tuning.ReleaseY), tuning.ThreadStartWidth, tuning.ThreadEndWidth,
                i % 3 == 0 ? Mint : Blue, tuning.ThreadAlpha - age * tuning.ThreadAlpha2, emitter: "TaoistSpellEffects.Rituals.Cleanse.4");
        }
        frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount, tuning.SparksRx * travel, tuning.SparksRy * travel, progress, Ice, tuning.SparksRise, emitter: "TaoistSpellEffects.Rituals.Cleanse.5");
        if (prepare) return;
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float turn = i * MathF.Tau / tuning.TurnDivisor + progress * tuning.Turn;
            Vector2 root = feet + Ellipse(turn, tuning.EllipseX, tuning.EllipseY);
            Vector2 top = Vector2.Lerp(root, head, tuning.Top) + Vector2.UnitX * (MathF.Cos(turn) * tuning.Top2);
            Vector2 edge = root + new Vector2(MathF.Sin(turn) * tuning.X2, tuning.Y3 - frame.Unit(i) * tuning.Y4);
            frame.Thread(root, edge, tuning.ThreadStartWidth2, tuning.ThreadEndWidth2, Blue, tuning.ThreadAlpha3, Depth(turn), emitter: "TaoistSpellEffects.Rituals.Cleanse.6");
            frame.Thread(edge, top, tuning.ThreadStartWidth3, tuning.ThreadEndWidth3, Mint, tuning.ThreadAlpha4 * (1 - progress), Depth(turn), emitter: "TaoistSpellEffects.Rituals.Cleanse.7");
            frame.Sprite(edge, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, Blue, tuning.WispAlpha, Depth(turn), turn * tuning.WispRotation, emitter: "TaoistSpellEffects.Rituals.Cleanse.8");
        }
    }

    private static void MassHeal(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.MassHeal);
        float bloom = tuning.Bloom + tuning.Bloom2 * Ease(progress * tuning.Bloom3);
        float radius = (prepare ? tuning.PrepareRadius : tuning.ReleaseRadius) * bloom;
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + (prepare ? tuning.PrepareCentre : tuning.ReleaseCentre) * Ease(progress));
        frame.Sprite(feet, radius * tuning.OrbWidth, radius * tuning.OrbHeight, tuning.OrbShape, Teal, tuning.OrbAlpha, tuning.OrbLayer, emitter: "TaoistSpellEffects.Rituals.MassHeal.1");
        frame.Sprite(centre, radius * tuning.WispWidth, radius * tuning.WispHeight, tuning.WispShape, Jade,
            tuning.WispAlpha * (1 - Ease((progress - tuning.Value) / tuning.ValueDivisor)), tuning.WispLayer, light: tuning.WispLight, emitter: "TaoistSpellEffects.Rituals.MassHeal.2");
        HealMotes(frame, centre, radius * tuning.Radius, progress, prepare ? tuning.PrepareCount : tuning.ReleaseCount);
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float angle = i * MathF.PI * tuning.Angle + progress * tuning.Angle2;
            Vector2 node = centre + Ellipse(angle, radius, radius * GroundAspect);
            Layer depth = Depth(angle);
            frame.Sprite(node, tuning.WispWidth2, tuning.WispHeight2, tuning.WispShape2, i % 2 == 0 ? Jade : Gold, tuning.WispAlpha2, depth, angle, light: tuning.WispLight2, emitter: "TaoistSpellEffects.Rituals.MassHeal.3");
            float flare = tuning.Flare + tuning.Flare2 * MathF.Pow(MathF.Sin(progress * MathF.Tau * tuning.Flare3 + i * tuning.Flare4), tuning.Flare5);
            frame.Sprite(node, tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape3, i % 2 == 0 ? tuning.WispColour : Amber,
                tuning.WispAlpha3 * flare, depth, angle + MathF.PI * tuning.WispRotation, light: tuning.WispLight3, emitter: "TaoistSpellEffects.Rituals.MassHeal.4");
            frame.Sprite(node, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Ivory, tuning.FlashAlpha, depth == Layer.Ground ? depth : tuning.FlashLayer, angle, emitter: "TaoistSpellEffects.Rituals.MassHeal.5");
            for (int j = 0; j < tuning.SparkCount; j++)
            {
                int seed = i * 14 + j;
                float trail = angle - j * tuning.Trail;
                float spread = radius + (frame.Unit(seed + 62) - tuning.Spread) * (tuning.Spread2 + j * tuning.Spread3);
                Vector2 point = centre + Ellipse(trail, spread, spread * GroundAspect) - Vector2.UnitY * (frame.Unit(seed) * tuning.Point);
                frame.Sprite(point, tuning.SparkWidth + frame.Unit(seed + 45) * tuning.SparkWidth2, tuning.SparkHeight, tuning.SparkShape,
                    j % 3 == 0 ? Ice : j % 4 == 0 ? Gold : Mint, tuning.SparkAlpha - j * tuning.SparkAlpha2, Depth(trail), trail + j * tuning.SparkRotation, emitter: "TaoistSpellEffects.Rituals.MassHeal.6");
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
                tuning.FlashAlpha * shimmer, depth == Layer.Ground ? depth : tuning.FlashLayer, turn, emitter: "TaoistSpellEffects.Rituals.HealMotes.1");
            if (i % 7 == 0)
                frame.Sprite(point, size * tuning.OrbWidth, size * tuning.OrbHeight, tuning.OrbShape, tint, tuning.OrbAlpha * shimmer, depth, light: tuning.OrbLight, emitter: "TaoistSpellEffects.Rituals.HealMotes.2");
        }
    }
}
