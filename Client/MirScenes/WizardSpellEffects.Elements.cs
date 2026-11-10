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
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FireCharge);
        Vector2 hand = feet + facing * (tuning.Hand + progress * tuning.Hand2) - Vector2.UnitY * tuning.Hand3;
        for (int i = 0; i < tuning.UserEmberCount; i++)
        {
            int seed = 173 + i * 37;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 3) * tuning.Age2) +
                frame.Unit(seed + 5));
            float theta = frame.Unit(seed + 7) * MathF.Tau + progress * tuning.Theta;
            float radius = (tuning.Radius + tuning.Radius2 * frame.Unit(seed + 11)) * (1f - progress * tuning.Radius3);
            Vector2 point = hand + Ellipse(theta, radius, radius * tuning.EllipseY) -
                Vector2.UnitY * (age * tuning.Point);
            float fade = MathF.Sin(age * MathF.PI);
            frame.Sprite(point, tuning.UserEmberWidth + frame.Unit(seed + 13) * tuning.UserEmberWidth2,
                tuning.UserEmberHeight + frame.Unit(seed + 17) * tuning.UserEmberHeight2,
                i % 4 == 0 ? tuning.UserEmberShape : tuning.UserEmberShape2,
                i % 5 == 0 ? Hot : i % 3 == 0 ? Gold : Fire,
                tuning.UserEmberAlpha * fade, i % 6 == 0 ? tuning.UserEmberLayer : tuning.UserEmberLayer2,
                (frame.Unit(seed + 21) - tuning.UserEmberRotation) * tuning.UserEmberRotation2, light: i % 19 == 0);
            if (i % 7 == 0)
                frame.Clip(point - Vector2.UnitY * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight,
                    tuning.SmokeSequenceShape, progress + frame.Unit(seed + 23),
                    tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * fade, tuning.SmokeSequenceLayer);
        }
    }

    private static void GreatFireCharge(Frame frame, Vector2 feet, float progress, int accent)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.GreatFireCharge);
        Vector2 focus = feet - Vector2.UnitY * (tuning.Focus + tuning.Focus2 * Ease(progress));
        for (int i = 0; i < tuning.UserEmberCount; i++)
        {
            int seed = 227 + i * 29;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 7) * tuning.Age2) +
                frame.Unit(seed + 11));
            float angle = frame.Unit(seed + 17) * MathF.Tau - progress * tuning.Angle;
            float radius = (tuning.Radius + tuning.Radius2 * frame.Unit(seed + 19)) *
                (1f - tuning.Radius3 * Ease(progress));
            Vector2 point = focus + Ellipse(angle, radius, radius * tuning.EllipseY) -
                Vector2.UnitY * (age * tuning.Point);
            float fade = MathF.Sin(age * MathF.PI);
            frame.Sprite(point, tuning.UserEmberWidth + frame.Unit(seed + 23) * tuning.UserEmberWidth2,
                tuning.UserEmberHeight + frame.Unit(seed + 27) * tuning.UserEmberHeight2,
                i % 5 == 0 ? tuning.UserEmberShape : tuning.UserEmberShape2,
                i % 4 == 0 ? Hot : i % 2 == 0 ? accent : Fire,
                tuning.UserEmberAlpha * fade, i % 7 == 0 ? tuning.UserEmberLayer : tuning.UserEmberLayer2,
                angle * tuning.UserEmberRotation, light: i % 17 == 0);
            if (i % 8 == 0)
                frame.Clip(point - Vector2.UnitY * tuning.SmokeSequenceOffset,
                    tuning.SmokeSequenceWidth + frame.Unit(seed + 29) * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + frame.Unit(seed + 31) * tuning.SmokeSequenceHeight2,
                    tuning.SmokeSequenceShape, progress + frame.Unit(seed + 37),
                    tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * fade, tuning.SmokeSequenceLayer);
        }
    }

    private static void HellfireCharge(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.HellfireCharge);
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        frame.Orbit(centre, tuning.OrbitX, tuning.OrbitY, progress * tuning.OrbitAngle, tuning.OrbitSweep, Fire, tuning.OrbitWidth, tuning.OrbitAlpha);
        frame.Orbit(centre, tuning.OrbitX2, tuning.OrbitY2, progress * tuning.OrbitAngle2 + tuning.OrbitAngle3, tuning.OrbitSweep2, Gold, tuning.OrbitWidth2, tuning.OrbitAlpha2);
        for (int i = 0; i < tuning.FlameCount; i++)
        {
            float angle = progress * tuning.Angle + i * MathF.PI;
            Vector2 point = centre + Ellipse(angle, tuning.EllipseX, tuning.EllipseY);
            frame.Sprite(point, tuning.FlameWidth, tuning.FlameHeight, tuning.FlameShape, Ember, tuning.FlameAlpha, Depth(angle), angle, light: tuning.FlameLight);
            frame.Sprite(point + new Vector2(tuning.X, tuning.Y), tuning.FlameWidth2, tuning.FlameHeight2, tuning.FlameShape2, Gold, tuning.FlameAlpha2, Depth(angle), angle - tuning.FlameRotation);
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Gold, rise: tuning.SparksRise);
        frame.Sprite(centre, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, tuning.WispColour, tuning.WispAlpha, tuning.WispLayer);
    }

    private static void FireBurst(Frame frame, Vector2 centre, float progress, float diameter, bool fierce = false)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FireBurst);
        // Individually animated pockets of combustion; no oversized fire card.
        float radius = diameter * (tuning.Radius + tuning.Radius2 * Ease(progress * tuning.Radius3));
        float fade = 1f - Ease((progress - tuning.Fade) / tuning.FadeDivisor);
        float time = progress * (fierce ? tuning.Time : tuning.Time2);
        int count = fierce ? tuning.Count : tuning.Count2;
        for (int i = 0; i < count; i++)
        {
            int seed = i * 37 + 27;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 2) * tuning.Age2) +
                frame.Unit(seed + 7));
            float x = (frame.Unit(seed + 13) - tuning.X) * radius * tuning.X2;
            float rise = age * (tuning.Rise + frame.Unit(seed + 21) * tuning.Rise2);
            Vector2 point = centre + new Vector2(x, (frame.Unit(seed + 33) - tuning.Y) * tuning.Y2 - rise);
            int tint = i % 7 == 0 ? Hot : i % 3 == 0 ? Gold : fierce ? Ember : Fire;
            float alpha = MathF.Sin(age * MathF.PI) * fade;
            frame.Sprite(point, tuning.UserFlameWidth + frame.Unit(seed + 39) * tuning.UserFlameWidth2,
                tuning.UserFlameHeight + frame.Unit(seed + 41) * tuning.UserFlameHeight2, tuning.UserFlameShape,
                tint, alpha * tuning.UserFlameAlpha, i % 8 == 0 ? tuning.UserFlameLayer : tuning.UserFlameLayer2,
                (frame.Unit(seed + 43) - tuning.UserFlameRotation) * tuning.UserFlameRotation2, light: i % 19 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - Vector2.UnitY * (tuning.UserEmberOffset + age * tuning.UserEmberOffset2),
                    tuning.UserEmberWidth + frame.Unit(seed + 47) * tuning.UserEmberWidth2, tuning.UserEmberHeight + frame.Unit(seed + 49) * tuning.UserEmberHeight2,
                    tuning.UserEmberShape, Hot, alpha * tuning.UserEmberAlpha, tuning.UserEmberLayer);
            if (i % 6 == 0)
                frame.Clip(point - Vector2.UnitY * (tuning.SmokeSequenceOffset + age * tuning.SmokeSequenceOffset2),
                    tuning.SmokeSequenceWidth + age * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + age * tuning.SmokeSequenceHeight2, tuning.SmokeSequenceShape,
                    time * tuning.SmokeSequencePhase + frame.Unit(seed + 53),
                    fierce ? tuning.SmokeSequenceColour : tuning.SmokeSequenceColour2, tuning.SmokeSequenceAlpha * (1 - age) * fade,
                    tuning.SmokeSequenceLayer);
        }
    }

    private static void SearingImpact(Frame frame, Vector2 centre, float progress, int accent)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.SearingImpact);
        float open = tuning.Open + tuning.Open2 * Ease(progress * tuning.Open3);
        float heat = 1f - Ease((progress - tuning.Heat) / tuning.HeatDivisor);
        for (int i = 0; i < tuning.UserFlameCount; i++)
        {
            int seed = i * 41 + 107;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 3) * tuning.Age2) +
                frame.Unit(seed + 7));
            float spread = (frame.Unit(seed + 11) - tuning.Spread) * tuning.Spread2 * open;
            Vector2 point = centre + new Vector2(spread + (frame.Unit(seed + 13) - tuning.X) * age * tuning.X2,
                (frame.Unit(seed + 15) - tuning.Y) * tuning.Y2 - age * (tuning.Y3 + frame.Unit(seed + 17) * tuning.Y4));
            float alive = heat * MathF.Sin(age * MathF.PI);
            frame.Sprite(point, tuning.UserFlameWidth + frame.Unit(seed + 19) * tuning.UserFlameWidth2,
                tuning.UserFlameHeight + frame.Unit(seed + 21) * tuning.UserFlameHeight2,
                tuning.UserFlameShape, i % 5 == 0 ? Hot : i % 2 == 0 ? accent : Gold,
                tuning.UserFlameAlpha * alive, i % 7 == 0 ? tuning.UserFlameLayer : tuning.UserFlameLayer2,
                (frame.Unit(seed + 25) - tuning.UserFlameRotation) * tuning.UserFlameRotation2, light: i % 23 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - Vector2.UnitY * tuning.UserEmberOffset, tuning.UserEmberWidth + frame.Unit(seed + 27) * tuning.UserEmberWidth2,
                    tuning.UserEmberHeight + frame.Unit(seed + 29) * tuning.UserEmberHeight2,
                    tuning.UserEmberShape, i % 3 == 0 ? Hot : accent,
                    tuning.UserEmberAlpha * alive, tuning.UserEmberLayer);
            if (i % 5 == 0)
                frame.Clip(point - Vector2.UnitY * (tuning.SmokeSequenceOffset + age * tuning.SmokeSequenceOffset2),
                    tuning.SmokeSequenceWidth + age * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + age * tuning.SmokeSequenceHeight2,
                    tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase + frame.Unit(seed + 31),
                    tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * (1 - age) * heat, tuning.SmokeSequenceLayer);
        }
    }

    private static void Repel(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Repel);
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        float radius = tuning.Radius + tuning.Radius2 * Ease(progress * tuning.Radius3);
        frame.Sprite(centre, radius * tuning.OrbWidth, radius * tuning.OrbHeight, tuning.OrbShape, Ember, tuning.OrbAlpha, light: tuning.OrbLight);
        frame.Orbit(centre, radius, radius * tuning.OrbitY, 0, MathF.Tau, Ember, tuning.OrbitWidth, tuning.OrbitAlpha);
        frame.Orbit(centre, radius - tuning.OrbitX, radius * tuning.OrbitY2, progress * tuning.OrbitAngle, MathF.Tau, Fire, tuning.OrbitWidth2, tuning.OrbitAlpha2);
        frame.Sprite(feet, radius * tuning.WispWidth, radius * tuning.WispHeight, tuning.WispShape, tuning.WispColour, tuning.WispAlpha, tuning.WispLayer);
        for (int i = 0; i < tuning.FlameCount; i++)
        {
            float angle = i * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle;
            Vector2 arc = centre + Ellipse(angle, radius + (frame.Unit(i) - tuning.EllipseX) * tuning.EllipseX2, radius * tuning.EllipseY);
            frame.Sprite(arc, tuning.FlameWidth + frame.Unit(i + 10) * tuning.FlameWidth2, tuning.FlameHeight, tuning.FlameShape, i % 3 == 0 ? Gold : i % 3 == 1 ? Fire : Ember,
                tuning.FlameAlpha, Depth(angle), angle, light: i % 3 == 0);
        }
        frame.Sparks(centre, tuning.SparksCount, radius, radius * tuning.SparksRy, progress, Fire);
    }

    // Every tongue has a floor root. Different widths, inclinations and rise phases keep a
    // line of Fire Wall cells from animating as identical flames or floating projectiles.
    internal static void Flames(Frame frame, Vector2 root, float width, float height, float time, float alpha, int index = 0,
        Layer layer = Layer.Emissive, float? sortY = null)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Flames);
        float phase = time * (tuning.Phase + frame.Unit(index + 21) * tuning.Phase2) + frame.Unit(index + 52);
        float sway = MathF.Sin(time * tuning.Sway + frame.Unit(index + 7) * MathF.Tau) * tuning.Sway2;
        frame.Sprite(root, width * tuning.OrbWidth, width * tuning.OrbHeight, tuning.OrbShape, Fire, alpha * tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight, sortY: sortY);
        // Pivot the flame at the lower edge of the clip to keep it on the floor.
        float column = height * tuning.Column;
        Vector2 centre = root + new Vector2(MathF.Sin(sway), -MathF.Cos(sway)) * (column * tuning.Centre);
        frame.Clip(centre, width * tuning.FlameSequenceWidth, column, tuning.FlameSequenceShape, phase, Fire, alpha * tuning.FlameSequenceAlpha,
            layer, sway, light: tuning.FlameSequenceLight, sortY: sortY);
        frame.Sprite(centre - Vector2.UnitY * (height * tuning.UserFlameOffset),
            width * tuning.UserFlameWidth, column * tuning.UserFlameHeight, tuning.UserFlameShape, Fire,
            alpha * (tuning.UserFlameAlpha + tuning.UserFlameAlpha2 * MathF.Sin(time * tuning.X + index)),
            layer, sway, light: tuning.UserFlameLight, sortY: sortY);
        frame.Clip(root + new Vector2(-width * tuning.X2, -height * tuning.Y), width * tuning.FlameSequenceWidth2, height * tuning.FlameSequenceHeight,
            tuning.FlameSequenceShape2, phase + tuning.FlameSequencePhase, Ember, alpha * tuning.FlameSequenceAlpha2, layer, sway * tuning.FlameSequenceRotation, sortY: sortY);
        Layer core = layer == Layer.Ground ? tuning.Core : tuning.Core2;
        frame.Clip(root + new Vector2(width * tuning.X3, -height * tuning.Y2), width * tuning.FlameSequenceWidth3, height * tuning.FlameSequenceHeight2,
            tuning.FlameSequenceShape3, phase + tuning.FlameSequencePhase2, Gold, alpha * tuning.FlameSequenceAlpha3, core, sway * tuning.FlameSequenceRotation2, sortY: sortY);
        frame.Sprite(root - Vector2.UnitY * (height * tuning.OrbOffset), width * tuning.OrbWidth2, height * tuning.OrbHeight2, tuning.OrbShape2,
            Hot, alpha * tuning.OrbAlpha2, core, sortY: sortY);
    }

    private static void FireWallCharge(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FireWallCharge);
        // The cast starts at the caster's feet, climbs the torso, gathers as
        // one overhead fire mass and releases an outward ember burst.
        float climb = Ease(progress / tuning.ClimbDivisor);
        Vector2 overhead = feet - Vector2.UnitY * tuning.Overhead;
        Vector2 heart = Vector2.Lerp(feet, overhead, climb);
        float ignite = Ease(progress * tuning.Ignite);
        float burst = Ease((progress - tuning.Burst) / tuning.BurstDivisor);
        for (int i = 0; i < tuning.UserFlameCount; i++)
        {
            int seed = i * 37 + 63;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 9) * tuning.Age2) +
                frame.Unit(seed + 13));
            float scatter = (frame.Unit(seed + 17) - tuning.Scatter) * (tuning.Scatter2 - tuning.Scatter3 * climb);
            float upward = age * (tuning.Upward + tuning.Upward2 * frame.Unit(seed + 23));
            Vector2 source = feet + new Vector2(scatter * tuning.X,
                -(progress * tuning.Y + upward));
            Vector2 point = Vector2.Lerp(source,
                heart + new Vector2(scatter * tuning.X2, (frame.Unit(seed + 31) - tuning.Y2) * tuning.Y3),
                climb * tuning.Point);
            float alpha = ignite * (1f - age * tuning.Alpha) * (1f - burst * tuning.Alpha2);
            frame.Sprite(point, tuning.UserFlameWidth + frame.Unit(seed + 33) * tuning.UserFlameWidth2,
                tuning.UserFlameHeight + frame.Unit(seed + 35) * tuning.UserFlameHeight2, tuning.UserFlameShape,
                i % 5 == 0 ? Hot : i % 3 == 0 ? Gold : Fire,
                tuning.UserFlameAlpha * alpha, i % 7 == 0 ? tuning.UserFlameLayer : tuning.UserFlameLayer2,
                (frame.Unit(seed + 41) - tuning.UserFlameRotation) * tuning.UserFlameRotation2, light: i % 19 == 0);
        }

        // Compact layered flames read as a single fireball, not a 100px orb.
        if (progress >= .55f)
        {
            float gather = Ease((progress - tuning.Gather) / tuning.GatherDivisor) * (1 - burst);
            for (int i = 0; i < tuning.UserFlameCount2; i++)
            {
                int seed = i * 41 + 401;
                float angle = frame.Unit(seed + 7) * MathF.Tau + progress * tuning.Angle;
                float radius = (tuning.Radius + frame.Unit(seed + 11) * tuning.Radius2) * (1 - tuning.Radius3 * gather);
                Vector2 point = overhead + Ellipse(angle, radius, radius * tuning.EllipseY);
                frame.Sprite(point, tuning.UserFlameWidth3 + frame.Unit(seed + 13) * tuning.UserFlameWidth4,
                    tuning.UserFlameHeight3 + frame.Unit(seed + 17) * tuning.UserFlameHeight4, tuning.UserFlameShape2,
                    i % 4 == 0 ? Hot : i % 3 == 0 ? Gold : Fire,
                    gather * tuning.UserFlameAlpha2, i % 5 == 0 ? tuning.UserFlameLayer3 : tuning.UserFlameLayer4,
                    angle * tuning.UserFlameRotation3, light: i == 3);
            }
        }

        if (burst > .005f)
        {
            for (int i = 0; i < tuning.UserFlameCount3; i++)
            {
                int seed = i * 31 + 707;
                float theta = (i + frame.Unit(seed)) * MathF.Tau / tuning.ThetaDivisor;
                Vector2 ray = Ellipse(theta, tuning.EllipseX + tuning.EllipseX2 * frame.Unit(seed + 13),
                    tuning.EllipseY2 + tuning.EllipseY3 * frame.Unit(seed + 19));
                Vector2 point = overhead + ray * burst -
                    Vector2.UnitY * (burst * frame.Unit(seed + 23) * tuning.Point2);
                frame.Sprite(point, tuning.UserFlameWidth5 + frame.Unit(seed + 29) * tuning.UserFlameWidth6,
                    tuning.UserFlameHeight5 + frame.Unit(seed + 37) * tuning.UserFlameHeight6,
                    i % 4 == 0 ? tuning.UserFlameShape3 : tuning.UserFlameShape4,
                    i % 5 == 0 ? Hot : Gold,
                    tuning.UserFlameAlpha3 * (1 - burst * tuning.UserFlameAlpha4), tuning.UserFlameLayer5,
                    theta, light: i % 13 == 0);
            }
            for (int i = 0; i < tuning.SmokeSequenceCount; i++)
                frame.Clip(overhead + new Vector2((i - tuning.X3) * tuning.X4, tuning.Y4 - burst * tuning.Y5),
                    tuning.SmokeSequenceWidth + burst * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + burst * tuning.SmokeSequenceHeight2, tuning.SmokeSequenceShape,
                    progress + i * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour,
                    tuning.SmokeSequenceAlpha * burst * (1 - Ease((progress - tuning.Value) / tuning.ValueDivisor)), tuning.SmokeSequenceLayer);
        }
    }

    private static void FireWall(Frame frame, Vector2 feet, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FireWall);
        Flames(frame, feet, tuning.Width, tuning.Height, time, tuning.Alpha);
        Flames(frame, feet + new Vector2(tuning.X, tuning.Y), tuning.Width2, tuning.Height2, time, tuning.Alpha2, 3, tuning.Layer);
        float smoke = Fract(time * tuning.Smoke + frame.Unit(34));
        frame.Clip(feet + new Vector2((frame.Unit(12) - tuning.X2) * tuning.X3, tuning.Y2 - smoke * tuning.Y3), tuning.SmokeSequenceWidth + smoke * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight,
            tuning.SmokeSequenceShape, smoke, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * MathF.Sin(smoke * MathF.PI), tuning.SmokeSequenceLayer, tuning.SmokeSequenceRotation);
        frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, time * tuning.SparksProgress, Gold, rise: tuning.SparksRise);
    }

    private static void FireBang(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FireBang);
        if (prepare)
        {
            Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
            frame.Sprite(centre, tuning.FlameWidth, tuning.FlameHeight, tuning.FlameShape, Gold, tuning.FlameAlpha, tuning.FlameLayer, light: tuning.FlameLight);
            for (int i = 0; i < tuning.FlameCount; i++)
            {
                float angle = (i + frame.Unit(i + 9) * tuning.Angle) * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle2;
                Vector2 ray = Ellipse(angle, tuning.EllipseX, tuning.EllipseY) * (tuning.Ray + tuning.Ray2 * Ease(progress * tuning.Ray3));
                frame.Line(centre + ray * tuning.LineStart, centre + ray, tuning.LineWidth, Fire, tuning.LineAlpha, Depth(angle), light: i % 3 == 0);
                frame.Line(centre + ray * tuning.LineStart2, centre + ray * tuning.LineEnd, tuning.LineWidth2, Gold, tuning.LineAlpha2, Depth(angle));
                frame.Sprite(centre + ray * tuning.FlameOffset, tuning.FlameWidth2, tuning.FlameHeight2, tuning.FlameShape2, Gold, tuning.FlameAlpha2, Depth(angle), angle);
            }
            frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Hot);
            return;
        }

        float spread = tuning.Spread + tuning.Spread2 * Ease(progress * tuning.Spread3);
        frame.Sprite(feet - Vector2.UnitY * 1, tuning.OrbWidth * spread, tuning.OrbHeight * spread, tuning.OrbShape, Ember, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        for (int i = 0; i < tuning.FlameCount2; i++)
        {
            float angle = i * MathF.Tau / tuning.AngleDivisor2 + frame.Unit(i + 6) * tuning.Angle3;
            float distance = (tuning.Distance + tuning.Distance2 * frame.Unit(i)) * spread;
            Vector2 root = feet + Ellipse(angle, tuning.EllipseX2, tuning.EllipseY2) * distance;
            float height = (tuning.Height + tuning.Height2 * frame.Unit(i + 44)) * (1 - progress * tuning.Height3);
            Flames(frame, root, tuning.Width + tuning.Width2 * frame.Unit(i + 12), height, time, tuning.Alpha, i, Depth(angle), root.Y);
            frame.Sprite(root - Vector2.UnitY * (height * tuning.FlameOffset2), tuning.FlameWidth3, tuning.FlameHeight3, tuning.FlameShape3, i % 3 == 0 ? Hot : Gold,
                tuning.FlameAlpha3, Depth(angle) == Layer.Ground ? tuning.FlameLayer2 : tuning.FlameLayer3, i * tuning.FlameRotation, light: i % 3 == 0, sortY: root.Y);
        }
        frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount2, tuning.SparksRx2, tuning.SparksRy2, progress, Gold, rise: tuning.SparksRise);
    }

    private static void Disruptor(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Disruptor);
        float height = (prepare ? tuning.PrepareHeight : tuning.ReleaseHeight) * (tuning.Height + tuning.Height2 * MathF.Sin(progress * MathF.PI));
        float width = prepare ? tuning.PrepareWidth : tuning.ReleaseWidth;
        frame.Sprite(feet - Vector2.UnitY * tuning.OrbOffset, width * tuning.OrbWidth, width * tuning.OrbHeight, tuning.OrbShape, Ember, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        Flames(frame, feet, width, height, time, tuning.Alpha);
        frame.Clip(feet - Vector2.UnitY * (height * tuning.SmokeSequenceOffset), width * tuning.SmokeSequenceWidth, height * tuning.SmokeSequenceHeight,
            tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase + tuning.SmokeSequencePhase2, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer, tuning.SmokeSequenceRotation);
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float angle = time * tuning.Angle + i * tuning.Angle2;
            Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + height * (i + 1) / tuning.CentreDivisor);
            frame.Orbit(centre, width * (tuning.OrbitX - i * tuning.OrbitX2), tuning.OrbitY - i * tuning.OrbitY2, angle, tuning.OrbitSweep, i == 1 ? Gold : Fire, tuning.OrbitWidth, tuning.OrbitAlpha);
        }
        for (int i = 0; i < tuning.FlameCount; i++)
        {
            float angle = frame.Unit(i) * MathF.Tau;
            Vector2 root = feet + Ellipse(angle, width * tuning.EllipseX, width * tuning.EllipseY);
            frame.Sprite(root, tuning.FlameWidth, tuning.FlameHeight, tuning.FlameShape, Fire, tuning.FlameAlpha, rotation: angle + time * tuning.FlameRotation);
        }
        frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount, width * tuning.SparksRx, width * tuning.SparksRy, progress, Gold, rise: height * tuning.SparksRise);
    }

    private static void FlameField(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FlameField);
        float open = tuning.Open + tuning.Open2 * Ease(progress * tuning.Open3);
        float radius = tuning.Radius * open;
        float rotation = progress * tuning.Rotation;
        frame.Sprite(feet, radius * tuning.OrbWidth, radius * tuning.OrbHeight, tuning.OrbShape, Ember, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        Seal(frame, feet, radius, radius * FloorAspect, Gold, rotation, tuning.Alpha, tuning.Spokes);
        Seal(frame, feet, radius * tuning.X, radius * FloorAspect * tuning.Y, Fire, -rotation, tuning.Alpha2, tuning.Spokes2);
        frame.Sprite(feet, radius * tuning.OrbWidth2, radius * tuning.OrbHeight2, tuning.OrbShape2, Gold, tuning.OrbAlpha2, tuning.OrbLayer2, light: tuning.OrbLight2);
        frame.Sprite(feet, radius * tuning.SigilDiscWidth, radius * tuning.SigilDiscHeight, tuning.SigilDiscShape, Hot, tuning.SigilDiscAlpha, tuning.SigilDiscLayer);

        float upper = 1 - Ease((progress - tuning.Upper) * tuning.Upper2);
        if (upper > 0)
        {
            Vector2 crown = feet - Vector2.UnitY * (tuning.Crown + tuning.Crown2 * progress);
            Seal(frame, crown, radius * tuning.X2, radius * tuning.Y2, Gold, -rotation, tuning.Alpha3 * upper, tuning.Spokes3, tuning.Layer);
            frame.Sprite(crown, radius * tuning.OrbWidth3, radius * tuning.OrbHeight3, tuning.OrbShape3, Fire, tuning.OrbAlpha3 * upper, light: tuning.OrbLight3);
            frame.Sprite(crown, radius * tuning.OrbWidth4, radius * tuning.OrbHeight4, tuning.OrbShape4, Hot, tuning.OrbAlpha4 * upper, tuning.OrbLayer3, light: tuning.OrbLight4);
            for (int i = 0; i < tuning.ParticleCount; i++)
            {
                float angle = (i + frame.Unit(i + 13)) * MathF.Tau / tuning.AngleDivisor;
                Vector2 ray = Ellipse(angle, radius * tuning.EllipseX, radius * tuning.EllipseY) - Vector2.UnitY * tuning.Ray;
                frame.Line(crown + ray * tuning.LineStart, crown + ray * (1 + frame.Unit(i + 39) * tuning.LineEnd),
                    tuning.LineWidth + frame.Unit(i + 5) * tuning.LineWidth2, Gold, (tuning.LineAlpha + tuning.LineAlpha2 * frame.Unit(i + 1)) * upper);
            }
        }

        for (int i = 0; i < tuning.ParticleCount2; i++)
        {
            float angle = i * MathF.Tau / tuning.AngleDivisor2 + rotation;
            Vector2 root = feet + Ellipse(angle, radius * (tuning.EllipseX2 + tuning.EllipseX3 * frame.Unit(i)), radius * tuning.EllipseY2);
            float beat = Fract(progress * tuning.Beat + frame.Unit(i + 37) * tuning.Beat2);
            float height = tuning.Height + tuning.Height2 * frame.Unit(i + 3);
            if (i % 3 == 0)
            {
                Vector2 falling = root - Vector2.UnitY * (height * (1 - beat));
                frame.Line(falling - Vector2.UnitY * (tuning.LineStart2 + tuning.LineStart3 * upper), falling, tuning.LineWidth3, Gold, tuning.LineAlpha3 * upper, Depth(angle), light: tuning.LineLight);
            }
            Flames(frame, root, tuning.Width + tuning.Width2 * frame.Unit(i + 15), height * (tuning.Height3 + tuning.Height4 * MathF.Sin(beat * MathF.PI)),
                time, tuning.Alpha4, i, Depth(angle), root.Y);
        }
        frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount, radius, radius * tuning.SparksRy, progress, Gold, rise: tuning.SparksRise);
    }

    private static void Seal(Frame frame, Vector2 centre, float x, float y, int colour, float rotation,
        float alpha, int spokes, Layer layer = Layer.Ground)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Seal);
        frame.Sprite(centre, x * tuning.SigilDiscWidth, y * tuning.SigilDiscHeight, tuning.SigilDiscShape, colour, alpha, layer, light: tuning.SigilDiscLight);
        frame.Sprite(centre, x * tuning.HaloWidth, y * tuning.HaloHeight, tuning.HaloShape, colour, alpha * tuning.HaloAlpha, layer);
        for (int i = 0; i < spokes; i++)
        {
            float angle = i * MathF.Tau / spokes + rotation;
            Vector2 spoke = Ellipse(angle, x, y);
            float inward = i % 2 == 0 ? tuning.Inward : tuning.Inward2;
            Vector2 end = centre + spoke * inward;
            Vector2 fork = centre + Ellipse(angle + tuning.EllipseAngle, x, y) * tuning.Fork;
            frame.Line(centre + spoke, end, i % 2 == 0 ? tuning.LineWidth : tuning.LineWidth2, colour, alpha * tuning.LineAlpha, layer);
            if (i % 2 == 0) frame.Line(end, fork, tuning.LineWidth3, colour, alpha * tuning.LineAlpha2, layer);
        }
    }

    private static void FrostCharge(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FrostCharge);
        // Cold flame climbs like a blue torch, with shard flecks and vapour.
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + tuning.Centre2 * Ease(progress * tuning.Centre3));
        for (int i = 0; i < tuning.UserFlameCount; i++)
        {
            int seed = 53 + i * 29;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 3) * tuning.Age2) +
                frame.Unit(seed + 7));
            float angle = frame.Unit(seed + 11) * MathF.Tau +
                progress * (tuning.Angle + frame.Unit(seed + 13) * tuning.Angle2);
            float radius = (tuning.Radius + tuning.Radius2 * frame.Unit(seed + 17)) *
                (tuning.Radius3 + tuning.Radius4 * Ease(progress * tuning.Radius5));
            Vector2 point = centre + Ellipse(angle, radius, radius * tuning.EllipseY) -
                Vector2.UnitY * (age * (tuning.Point + frame.Unit(seed + 19) * tuning.Point2));
            float alive = MathF.Sin(age * MathF.PI);
            int tint = i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice;
            frame.Sprite(point, tuning.UserFlameWidth + frame.Unit(seed + 19) * tuning.UserFlameWidth2,
                tuning.UserFlameHeight + frame.Unit(seed + 23) * tuning.UserFlameHeight2,
                tuning.UserFlameShape, tint, tuning.UserFlameAlpha * alive,
                i % 4 == 0 ? tuning.UserFlameLayer : tuning.UserFlameLayer2,
                (frame.Unit(seed + 27) - tuning.UserFlameRotation) * tuning.UserFlameRotation2, light: i % 13 == 0);
            if (i % 3 == 0)
                frame.Sprite(point - Vector2.UnitY * tuning.UserEmberOffset, tuning.UserEmberWidth + frame.Unit(seed + 37) * tuning.UserEmberWidth2,
                    tuning.UserEmberHeight + frame.Unit(seed + 41) * tuning.UserEmberHeight2,
                    tuning.UserEmberShape, IceWhite, tuning.UserEmberAlpha * alive, tuning.UserEmberLayer);
            if (i % 6 == 0)
                frame.Clip(point - Vector2.UnitY * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight,
                    tuning.SmokeSequenceShape, progress + frame.Unit(seed + 47),
                    tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * alive, tuning.SmokeSequenceLayer);
        }
        // Bright overlapping cold-blue shell / white frostfire core. The
        // highlights occupy a sphere; none is a flat opaque orb backdrop.
        frame.Sprite(centre, tuning.FrostCoreWidth, tuning.FrostCoreHeight, tuning.FrostCoreShape, Ice, tuning.FrostCoreAlpha, tuning.FrostCoreLayer);
        frame.Sprite(centre, tuning.FrostCoreWidth2, tuning.FrostCoreHeight2, tuning.FrostCoreShape2, PaleBlue, tuning.FrostCoreAlpha2,
            tuning.FrostCoreLayer2, light: tuning.FrostCoreLight);
        frame.Sprite(centre - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight,
            tuning.FlashShape, IceWhite, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight);
        for (int i = 0; i < tuning.FrostCoreCount; i++)
        {
            int seed = 1109 + i * 17;
            float angle = frame.Unit(seed) * MathF.Tau + progress * tuning.Angle3;
            Vector2 point = centre + Ellipse(angle,
                tuning.EllipseX + frame.Unit(seed + 3) * tuning.EllipseX2, tuning.EllipseY2 + frame.Unit(seed + 5) * tuning.EllipseY3);
            frame.Sprite(point, tuning.FrostCoreWidth3 + frame.Unit(seed + 7) * tuning.FrostCoreWidth4,
                tuning.FrostCoreHeight3 + frame.Unit(seed + 9) * tuning.FrostCoreHeight4, tuning.FrostCoreShape3,
                IceWhite, tuning.FrostCoreAlpha3 + tuning.FrostCoreAlpha4 * frame.Unit(seed + 11),
                tuning.FrostCoreLayer3, light: i % 7 == 0);
        }
    }

    private static void FrostFireImpact(Frame frame, Vector2 centre, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FrostFireImpact);
        // Impact returns to cold combustion rather than a large blue orb.
        float fade = 1f - Ease((progress - tuning.Fade) / tuning.FadeDivisor);
        for (int i = 0; i < tuning.UserFlameCount; i++)
        {
            int seed = i * 43 + 119;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 5) * tuning.Age2) +
                frame.Unit(seed + 7));
            float angle = frame.Unit(seed + 9) * MathF.Tau +
                progress * (tuning.Angle + frame.Unit(seed + 11) * tuning.Angle2);
            float radius = (tuning.Radius + tuning.Radius2 * Ease(progress * tuning.Radius3)) *
                (tuning.Radius4 + tuning.Radius5 * frame.Unit(seed + 13));
            Vector2 point = centre + Ellipse(angle, radius, radius * tuning.EllipseY) -
                Vector2.UnitY * (age * (tuning.Point + frame.Unit(seed + 15) * tuning.Point2));
            float strength = MathF.Sin(age * MathF.PI) * fade;
            frame.Sprite(point, tuning.UserFlameWidth + frame.Unit(seed + 19) * tuning.UserFlameWidth2,
                tuning.UserFlameHeight + frame.Unit(seed + 21) * tuning.UserFlameHeight2,
                tuning.UserFlameShape, i % 6 == 0 ? IceWhite : i % 2 == 0 ? Ice : PaleBlue,
                tuning.UserFlameAlpha * strength, i % 4 == 0 ? tuning.UserFlameLayer : tuning.UserFlameLayer2,
                (frame.Unit(seed + 25) - tuning.UserFlameRotation) * tuning.UserFlameRotation2, light: i % 17 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - Vector2.UnitY * tuning.ShardOffset, tuning.ShardWidth + frame.Unit(seed + 31) * tuning.ShardWidth2,
                    tuning.ShardHeight + frame.Unit(seed + 37) * tuning.ShardHeight2, tuning.ShardShape,
                    IceWhite, tuning.ShardAlpha * strength, tuning.ShardLayer);
            if (i % 6 == 0)
                frame.Clip(point, tuning.SmokeSequenceWidth + age * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + age * tuning.SmokeSequenceHeight2,
                    tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase + frame.Unit(seed + 41),
                    tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * strength, tuning.SmokeSequenceLayer);
        }
        // Frostfire impact retains a bright blue-white centre while the
        // textured outer shell disperses into shards and vapour.
        frame.Sprite(centre, tuning.FrostCoreWidth, tuning.FrostCoreHeight, tuning.FrostCoreShape, Ice,
            tuning.FrostCoreAlpha * fade, tuning.FrostCoreLayer);
        frame.Sprite(centre, tuning.FrostCoreWidth2, tuning.FrostCoreHeight2, tuning.FrostCoreShape2, PaleBlue,
            tuning.FrostCoreAlpha2 * fade, tuning.FrostCoreLayer2, light: tuning.FrostCoreLight);
        frame.Sprite(centre - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight,
            tuning.FlashShape, IceWhite, tuning.FlashAlpha * fade,
            tuning.FlashLayer, light: tuning.FlashLight);
        for (int i = 0; i < tuning.FrostCoreCount; i++)
        {
            int seed = 1601 + i * 19;
            float angle = frame.Unit(seed) * MathF.Tau + progress * tuning.Angle3;
            Vector2 point = centre + Ellipse(angle,
                tuning.EllipseX + frame.Unit(seed + 3) * (tuning.EllipseX2 + progress * tuning.EllipseX3),
                tuning.EllipseY2 + frame.Unit(seed + 5) * (tuning.EllipseY3 + progress * tuning.EllipseY4));
            frame.Sprite(point, tuning.FrostCoreWidth3 + frame.Unit(seed + 7) * tuning.FrostCoreWidth4,
                tuning.FrostCoreHeight3 + frame.Unit(seed + 9) * tuning.FrostCoreHeight4, tuning.FrostCoreShape3,
                IceWhite, fade * (tuning.FrostCoreAlpha3 + tuning.FrostCoreAlpha4 * frame.Unit(seed + 11)),
                tuning.FrostCoreLayer3, light: i % 7 == 0);
        }
    }

    private static void IceBreak(Frame frame, Vector2 centre, float progress, float radius, int count)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.IceBreak);
        float spread = tuning.Spread + tuning.Spread2 * Ease(progress * tuning.Spread3);
        frame.Sprite(centre, radius * tuning.OrbWidth, radius * tuning.OrbHeight, tuning.OrbShape, Ice, tuning.OrbAlpha, light: tuning.OrbLight);
        frame.Clip(centre, radius * tuning.SmokeSequenceWidth, radius * tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase + tuning.SmokeSequencePhase2, tuning.SmokeSequenceColour,
            tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer, progress * tuning.SmokeSequenceRotation);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + frame.Unit(i + 25)) * MathF.Tau / count;
            Vector2 point = centre + Ellipse(angle, radius, radius * tuning.EllipseY) * spread;
            float length = (tuning.Length + frame.Unit(i) * tuning.Length2) * (1 - tuning.Length3 * progress);
            frame.Sprite(point, length * tuning.IceSpireWidth, length, tuning.IceSpireShape, i % 3 == 0 ? IceWhite : Ice, tuning.IceSpireAlpha,
                tuning.IceSpireLayer, angle + MathF.PI / 2, light: i % 3 == 0);
            if (i % 2 == 0)
                frame.Sprite(point, length * tuning.FrostCoreWidth, length * tuning.FrostCoreHeight, tuning.FrostCoreShape, IceWhite, tuning.FrostCoreAlpha, tuning.FrostCoreLayer, angle);
        }
        frame.Sprite(centre, radius * tuning.FrostCoreWidth2, radius * tuning.FrostCoreHeight2, tuning.FrostCoreShape2, IceWhite, tuning.FrostCoreAlpha2 * (1 - progress),
            tuning.FrostCoreLayer2, progress * tuning.FrostCoreRotation, light: tuning.FrostCoreLight);
        frame.Sparks(centre, count, radius * tuning.ShardRx, radius * tuning.ShardRy, progress, IceWhite, rise: tuning.ShardRise, shape: tuning.ShardShape);
    }

    private static void IceStorm(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.IceStorm);
        // Both cast and contact carry the same detailed runic seal, while
        // irregular spinning cold ribbons breathe small > large > small.
        float spread = prepare ? tuning.PrepareSpread : tuning.ReleaseSpread;
        float wind = progress * (prepare ? tuning.PrepareWind : tuning.ReleaseWind);
        float runeFade = prepare ? tuning.PrepareRuneFade : tuning.RuneFade * (1f - Ease((progress - tuning.RuneFade2) / tuning.RuneFadeDivisor));
        // Keep an already projected rune at a fixed screen-space orientation.
        // Rotating this flattened ellipse skews its silhouette.
        frame.Sprite(feet, tuning.SigilDiscWidth, tuning.SigilDiscHeight,
            tuning.SigilDiscShape, IceWhite, runeFade * (tuning.SigilDiscAlpha + tuning.SigilDiscAlpha2 * MathF.Sin(progress * tuning.X)),
            tuning.SigilDiscLayer, rotation: tuning.SigilDiscRotation, light: tuning.SigilDiscLight, sortY: feet.Y);

        int ribbons = prepare ? tuning.PrepareRibbons : tuning.ReleaseRibbons;
        for (int i = 0; i < ribbons; i++)
        {
            int seed = 83 + i * 31;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 3) * tuning.Age2) +
                frame.Unit(seed + 7));
            float lifeCurve = MathF.Sin(age * MathF.PI);
            if (lifeCurve < .02f) continue;
            float angle = frame.Unit(seed + 11) * MathF.Tau +
                wind * (tuning.Angle + frame.Unit(seed + 13) * tuning.Angle2);
            float radius = (tuning.Radius + tuning.Radius2 * frame.Unit(seed + 17)) * spread +
                MathF.Sin(progress * (tuning.Radius3 + frame.Unit(seed + 55) * tuning.Radius4) +
                    frame.Unit(seed + 57) * MathF.Tau) * (tuning.Radius5 + frame.Unit(seed + 59) * tuning.Radius6);
            Vector2 root = feet + ProjectGround(Ellipse(angle, radius, radius));
            float height = (tuning.Height + tuning.Height2 * frame.Unit(seed + 19)) * spread;
            Vector2 centre = root - Vector2.UnitY * (height + age * tuning.Centre);
            Vector2 tangent = Vector2.Normalize(ProjectGround(Ellipse(angle + MathF.PI / 2, 1, 1)));
            // Face tangential travel, never radially outward.
            float orientation = MathF.Atan2(tangent.Y, tangent.X);
            float breathe = tuning.Breathe + tuning.Breathe2 * MathF.Sin(progress * tuning.Breathe3 + frame.Unit(seed + 61) * MathF.Tau);
            float width = (tuning.Width + (tuning.Width2 + frame.Unit(seed + 63) * tuning.Width3) * lifeCurve) * spread * breathe;
            float thickness = (tuning.Thickness + (tuning.Thickness2 + frame.Unit(seed + 65) * tuning.Thickness3) * lifeCurve) * spread;
            float alpha = tuning.Alpha * lifeCurve;
            frame.Sprite(centre, width, thickness,
                tuning.UserSlashShape, i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice,
                alpha, Depth(angle), orientation, light: i % 9 == 0, sortY: root.Y);
            // Backtrace three earlier points on the same projected orbit.
            // Narrow connected filaments and translucent wisps form a tail,
            // rather than detached spokes or oversized billboard cards.
            Vector2 previous = centre;
            for (int tail = 1; tail <= tuning.TailCount; tail++)
            {
                float backAngle = angle - tail * (tuning.BackAngle + frame.Unit(seed + 67) * tuning.BackAngle2);
                Vector2 backRoot = feet + ProjectGround(Ellipse(backAngle, radius, radius));
                Vector2 back = backRoot - Vector2.UnitY * (height + age * tuning.Back + tail * tuning.Back2);
                float tailFade = alpha * (1 - tail * tuning.TailFade);
                frame.Thread(back, previous, tuning.ThreadStartWidth + tuning.ThreadStartWidth2 * lifeCurve * (1 - tail * tuning.ThreadStartWidth3),
                    tuning.ThreadEndWidth + tuning.ThreadEndWidth2 * lifeCurve, i % 5 == 0 ? IceWhite : PaleBlue,
                    tailFade * tuning.ThreadAlpha, Depth(angle), sortY: backRoot.Y);
                frame.Sprite((back + previous) * tuning.WispOffset, (back - previous).Length() + tuning.WispWidth,
                    tuning.WispHeight + tuning.WispHeight2 * lifeCurve, tuning.WispShape, PaleBlue, tailFade * tuning.WispAlpha,
                    Depth(angle), MathF.Atan2(previous.Y - back.Y, previous.X - back.X),
                    sortY: backRoot.Y);
                previous = back;
            }
            frame.Clip(centre - tangent * tuning.SmokeSequenceOffset, width * tuning.SmokeSequenceWidth, thickness * tuning.SmokeSequenceHeight,
                tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase + frame.Unit(seed + 27),
                tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * lifeCurve,
                Depth(angle), orientation + tuning.SmokeSequenceRotation, sortY: root.Y);

            // Snow is born on each swirling ribbon, travels briefly and fades.
            for (int snow = 0; snow < tuning.SnowCount; snow++)
            {
                int flake = seed + snow * tuning.Flake;
                float driftAge = Fract(age * tuning.DriftAge + frame.Unit(flake + 33));
                Vector2 point = centre + tangent *
                    ((frame.Unit(flake + 37) - tuning.Point) * width * tuning.Point2) +
                    new Vector2((frame.Unit(flake + 41) - tuning.X2) * driftAge * tuning.X3,
                        -driftAge * (tuning.Y + frame.Unit(flake + 43) * tuning.Y2));
                frame.Sprite(point, tuning.SparkWidth + frame.Unit(flake + 47) * tuning.SparkWidth2,
                    tuning.SparkHeight + frame.Unit(flake + 49) * tuning.SparkHeight2,
                    tuning.SparkShape, snow % 3 == 0 ? IceWhite : PaleBlue,
                    lifeCurve * (1 - driftAge) * tuning.SparkAlpha, Depth(angle),
                    orientation, sortY: root.Y);
            }
        }
        int dust = prepare ? tuning.PrepareDust : tuning.ReleaseDust;
        for (int i = 0; i < dust; i++)
        {
            int seed = i * 37 + 511;
            float age = Fract(progress * tuning.Age3 + frame.Unit(seed + 3));
            float angle = frame.Unit(seed + 7) * MathF.Tau +
                wind * (tuning.Angle3 + frame.Unit(seed + 9) * tuning.Angle4);
            float radius = (tuning.Radius7 + frame.Unit(seed + 11) * tuning.Radius8) * spread;
            Vector2 root = feet + ProjectGround(Ellipse(angle, radius, radius));
            Vector2 point = root - Vector2.UnitY *
                ((tuning.Point3 + frame.Unit(seed + 13) * tuning.Point4) * (1 - age));
            frame.Sprite(point, tuning.SparkWidth3 + frame.Unit(seed + 17) * tuning.SparkWidth4,
                tuning.SparkHeight3 + frame.Unit(seed + 19) * tuning.SparkHeight4, tuning.SparkShape2,
                i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice,
                tuning.SparkAlpha2 * MathF.Sin(age * MathF.PI), Depth(angle),
                wind * tuning.SparkRotation, sortY: root.Y);
        }
    }

    private static void IceThrust(Frame frame, Vector2 feet, Vector2 facing, float progress, int extraReach)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.IceThrust);
        Vector2 forward = GroundHeading(facing);
        var side = new Vector2(-forward.Y, forward.X);
        float reach = (tuning.Reach + extraReach * tuning.Reach2) / MathF.Max(MathF.Abs(forward.X), MathF.Abs(forward.Y));
        float surge = tuning.Surge + tuning.Surge2 * Ease(progress * tuning.Surge3);
        Vector2 chest = feet - Vector2.UnitY * tuning.Chest;
        frame.Sprite(chest + facing * tuning.OrbOffset, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Ice, tuning.OrbAlpha * (1 - Ease(progress * tuning.Value)), light: tuning.OrbLight);

        for (int stream = tuning.Stream; stream <= tuning.StreamCount; stream++)
        {
            float breadth = stream * tuning.Breadth;
            float length = reach * surge * (1 - Math.Abs(stream) * tuning.Length);
            Vector2 previous = chest;
            for (int sample = 1; sample <= tuning.SampleCount; sample++)
            {
                float t = sample / 4f;
                float roll = MathF.Sin(t * tuning.Roll - progress * tuning.Roll2 + stream * tuning.Roll3);
                Vector2 lane = forward * (length * t) + side * ((breadth + roll * tuning.Lane) * t);
                Vector2 point = chest + ProjectGround(lane) + Vector2.UnitY * (t * (tuning.Point + roll * tuning.Point2));
                Vector2 travel = point - previous;
                float angle = MathF.Atan2(travel.Y, travel.X);
                float taper = 1 - tuning.Taper * Math.Abs(stream) - tuning.Taper2 * t;
                float depth = feet.Y + ProjectGround(lane).Y;
                frame.Clip((point + previous) * tuning.SmokeSequenceOffset, travel.Length() + tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight + t * tuning.SmokeSequenceHeight2, tuning.SmokeSequenceShape,
                    progress * tuning.SmokeSequencePhase + sample * tuning.SmokeSequencePhase2 + stream * tuning.SmokeSequencePhase3, stream == 0 ? IceWhite : Ice,
                    tuning.SmokeSequenceAlpha * taper, rotation: angle, light: stream == 0, sortY: depth);
                frame.Thread(previous, point, tuning.ThreadStartWidth - t, tuning.ThreadEndWidth, IceWhite, tuning.ThreadAlpha * taper, tuning.ThreadLayer, sortY: depth);
                frame.Sprite(point - Vector2.UnitY * (tuning.OrbOffset2 + t * tuning.OrbOffset3), tuning.OrbWidth2 + t * tuning.OrbWidth3, tuning.OrbHeight2 + t * tuning.OrbHeight3, tuning.OrbShape2,
                    PaleBlue, tuning.OrbAlpha2 * taper, tuning.OrbLayer, sortY: depth);
                previous = point;
            }
            Vector2 front = feet + ProjectGround(forward * length + side * breadth);
            frame.Sprite(front, tuning.SmokeWidth, tuning.SmokeHeight, tuning.SmokeShape, Ice, tuning.SmokeAlpha, tuning.SmokeLayer, light: stream == 0, sortY: front.Y);
            if (stream % 2 == 0)
            {
                Vector2 start = chest + ProjectGround(side * breadth * tuning.Start);
                Channel(frame, start, front - Vector2.UnitY * tuning.End, tuning.Spread + Math.Abs(stream) * tuning.Spread2, stream == 0 ? tuning.Width : tuning.Width2,
                    stream == 0 ? tuning.Alpha : tuning.Alpha2, progress * tuning.Time, tuning.Forks, tuning.Offset + stream * tuning.Offset2, depths: new(feet.Y, front.Y));
            }
            frame.Sparks(front - Vector2.UnitY * tuning.OrbOffset4, tuning.OrbCount, tuning.OrbRx, tuning.OrbRy, progress, IceWhite, rise: tuning.OrbRise, shape: tuning.OrbShape3, sortY: front.Y);
        }
    }

    private static void SnowContact(Frame frame, Vector2 feet, float progress, bool tile)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.SnowContact);
        float lift = Ease(progress * tuning.Lift);
        Vector2 centre = feet + new Vector2(progress * tuning.X, -(tile ? tuning.Y : tuning.Y2) - lift * tuning.Y3);
        frame.Clip(centre, tile ? tuning.SmokeSequenceWidth : tuning.SmokeSequenceWidth2, tile ? tuning.SmokeSequenceHeight : tuning.SmokeSequenceHeight2, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase,
            IceWhite, tile ? tuning.SmokeSequenceAlpha : tuning.SmokeSequenceAlpha2, tile ? tuning.SmokeSequenceLayer : tuning.SmokeSequenceLayer2, tuning.SmokeSequenceRotation, light: tuning.SmokeSequenceLight);
        frame.Sparks(centre, tile ? tuning.OrbCount : tuning.OrbCount2, tuning.OrbRx, tuning.OrbRy, progress, IceWhite, rise: tuning.OrbRise, shape: tuning.OrbShape);
    }

    private static void SnowballWake(Frame frame, SpellParticlePool pool, Vector2 head, Vector2 heading,
        long now, int frameIndex, int remaining, bool emit)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.SnowballWake);
        float phase = now % 700 / tuning.PhaseDivisor;
        for (int i = 0; i < tuning.OrbCount; i++)
        {
            float fall = Fract(phase + frame.Unit(i + 18));
            Vector2 point = head - heading * (tuning.Point + frame.Unit(i + 30) * tuning.Point2) +
                new Vector2((frame.Unit(i + 55) - tuning.X) * tuning.X2, tuning.Y + tuning.Y2 * fall);
            float size = tuning.Size + frame.Unit(i + 62) * tuning.Size2;
            frame.Sprite(point, size, size * tuning.OrbHeight, tuning.OrbShape, IceWhite, tuning.OrbAlpha * MathF.Sin(fall * MathF.PI), tuning.OrbLayer);
        }
        if (!emit) return;

        for (int i = 0; i < tuning.OrbCount2; i++)
        {
            int seed = frameIndex * 11 + i;
            Vector2 point = head - heading * (tuning.Point3 + frame.Unit(seed) * tuning.Point4) + Vector2.UnitY * (tuning.Point5 + tuning.Point6 * frame.Unit(seed + 73));
            Vector2 drift = new((frame.Unit(seed + 32) - tuning.Drift) * tuning.Drift2, tuning.Drift3 + frame.Unit(seed + 53) * tuning.Drift4);
            pool.TryAdd(new SpellParticle(point, drift, now, Math.Min(remaining, tuning.Val2 + i * tuning.Val22), tuning.OrbSize + i * tuning.OrbSize2,
                Tint(IceWhite, tuning.Opacity), tuning.OrbShape2, 0, Aspect: tuning.OrbAspect, Growth: tuning.OrbGrowth,
                Layer: tuning.OrbLayer2, Linear: true, CastsLight: tuning.OrbCastsLight, DepthY: head.Y + tuning.OrbDepthY));
        }
    }

    private static void Blizzard(Frame frame, Vector2 feet, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Blizzard);
        float cycle = FieldDrop(time, tuning.Cycle, frame.Unit(1));
        Vector2 root = feet + new Vector2((frame.Unit(20) - tuning.X) * tuning.X2, (frame.Unit(3) - tuning.Y) * tuning.Y2);
        float fall = 1 - Ease(cycle * tuning.Fall);
        float density = frame.Unit(31);
        float lull = 1 - Ease((time % 3 - tuning.Lull) / tuning.LullDivisor);
        float warning = 1 - Ease(time / tuning.WarningDivisor);
        frame.Clip(root + new Vector2(tuning.X3, tuning.Y3), tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase + density,
            tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * warning, tuning.SmokeSequenceLayer, tuning.SmokeSequenceRotation, sortY: root.Y);
        frame.Sprite(root + new Vector2(tuning.X4, tuning.Y4), tuning.SparkWidth, tuning.SparkHeight, tuning.SparkShape, IceWhite, tuning.SparkAlpha * warning,
            tuning.SparkLayer, rotation: tuning.SparkRotation, sortY: root.Y);
        if (fall > .004f && density > .26f && lull > .015f)
        {
            Vector2 head = root - new Vector2(tuning.X5 + frame.Unit(7) * tuning.X6, tuning.Y5 + frame.Unit(9) * tuning.Y6) * fall;
            Vector2 tail = new Vector2(tuning.X7 + frame.Unit(10) * tuning.X8, tuning.Y7 + frame.Unit(17) * tuning.Y8);
            float opacity = (tuning.Opacity + density * tuning.Opacity2) * lull;
            frame.Line(head - tail, head, tuning.LineWidth, Blue, opacity, light: tuning.LineLight);
            frame.Line(head - tail * tuning.LineStart, head, tuning.LineWidth2, IceWhite, opacity, tuning.LineLayer);
            frame.Clip(head - tail * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth2, tail.Length() * tuning.SmokeSequenceHeight2, tuning.SmokeSequenceShape2, cycle * tuning.SmokeSequencePhase2,
                tuning.SmokeSequenceColour2, tuning.SmokeSequenceAlpha2 * lull, tuning.SmokeSequenceLayer2, tuning.SmokeSequenceRotation2);
            frame.Sprite(head, tuning.FlameWidth, tuning.FlameHeight, tuning.FlameShape, Blue, opacity, rotation: tuning.FlameRotation);
            frame.Sprite(head, tuning.FrostCoreWidth, tuning.FrostCoreHeight, tuning.FrostCoreShape, IceWhite, opacity, tuning.FrostCoreLayer, tuning.FrostCoreRotation);
        }
        float land = Ease((time - tuning.Land) / tuning.LandDivisor);
        frame.Sprite(root - Vector2.UnitY * tuning.SmokeOffset, tuning.SmokeWidth, tuning.SmokeHeight, tuning.SmokeShape, Ice, tuning.SmokeAlpha * land * (tuning.SmokeAlpha2 + tuning.SmokeAlpha3 * lull), tuning.SmokeLayer, light: tuning.SmokeLight);
        if (land > .8f && cycle > .63f)
        {
            float spread = Ease((cycle - tuning.Spread) / tuning.SpreadDivisor);
            frame.Sprite(root - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, IceWhite, tuning.FlashAlpha * (1 - spread) * lull, tuning.FlashLayer);
            if (lull > .3f) frame.Sparks(root, tuning.ShardCount, tuning.ShardRx, tuning.ShardRy, cycle, IceWhite, rise: tuning.ShardRise, shape: tuning.ShardShape);
        }
    }

    private static void MeteorStrike(Frame frame, Vector2 feet, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.MeteorStrike);
        float phase = FieldDrop(time, tuning.Phase, frame.Unit(4));
        float density = frame.Unit(51);
        float warmup = tuning.Warmup + tuning.Warmup2 * Ease((time - tuning.Warmup3) / tuning.WarmupDivisor);
        frame.Sprite(feet, tuning.SmokeWidth, tuning.SmokeHeight, tuning.SmokeShape, Ember, tuning.SmokeAlpha * warmup, tuning.SmokeLayer, light: tuning.SmokeLight);
        if (density < .32f) return;

        float fall = 1 - Ease(phase * tuning.Fall);
        if (fall > .004f)
        {
            Vector2 head = feet - new Vector2(tuning.X + frame.Unit(5) * tuning.X2, tuning.Y + frame.Unit(8) * tuning.Y2) * fall;
            Vector2 tail = new(tuning.Tail + frame.Unit(12) * tuning.Tail2, tuning.Tail3 + density * tuning.Tail4);
            frame.Line(head - tail, head, tuning.LineWidth, Ember, tuning.LineAlpha, light: tuning.LineLight);
            frame.Line(head - tail * tuning.LineStart, head, tuning.LineWidth2, Gold, tuning.LineAlpha2, tuning.LineLayer);
            frame.Clip(head - tail * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth, tail.Length(), tuning.SmokeSequenceShape, phase * tuning.SmokeSequencePhase,
                tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer, tuning.SmokeSequenceRotation);
            frame.Clip(head, tuning.FireSequenceWidth, tuning.FireSequenceHeight, tuning.FireSequenceShape, phase * tuning.FireSequencePhase, tuning.FireSequenceColour, tuning.FireSequenceAlpha);
            frame.Sprite(head, tuning.UserRockWidth, tuning.UserRockHeight, tuning.UserRockShape, tuning.UserRockColour, tuning.UserRockAlpha,
                tuning.UserRockLayer, rotation: tuning.UserRockRotation + MathF.Sin(time * tuning.X3) * tuning.UserRockRotation2);
            frame.Sprite(head + new Vector2(tuning.X4, tuning.Y3), tuning.UserFlameWidth, tuning.UserFlameHeight, tuning.UserFlameShape,
                Gold, tuning.UserFlameAlpha, tuning.UserFlameLayer, light: tuning.UserFlameLight);
        }
        else
        {
            float flash = 1 - Ease((phase - tuning.Flash) / tuning.FlashDivisor);
            frame.Sprite(feet, tuning.FlameWidth, tuning.FlameHeight, tuning.FlameShape, Gold, tuning.FlameAlpha * flash, tuning.FlameLayer, light: tuning.FlameLight);
            Flames(frame, feet, tuning.Width, tuning.Height * (tuning.Height2 - phase), time, tuning.Alpha * flash);
            frame.Sparks(feet, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, phase, Gold, rise: tuning.SparksRise);
            for (int fragment = 0; fragment < tuning.FragmentCount; fragment++)
            {
                float age = Fract(phase * tuning.Age + frame.Unit(fragment + 190));
                Vector2 debris = feet + new Vector2(
                    (frame.Unit(fragment + 200) - tuning.X5) * (tuning.X6 + tuning.X7 * age),
                    tuning.Y4 - (1 - age) * (tuning.Y5 + frame.Unit(fragment + 205) * tuning.Y6));
                frame.Sprite(debris, tuning.UserRockWidth2 + frame.Unit(fragment + 210) * tuning.UserRockWidth3,
                    tuning.UserRockHeight2 + frame.Unit(fragment + 215) * tuning.UserRockHeight3,
                    tuning.UserRockShape2, tuning.UserRockColour2, tuning.UserRockAlpha2 * (1 - age * tuning.UserRockAlpha3),
                    tuning.UserRockLayer2, age * (fragment % 2 == 0 ? tuning.UserRockRotation3 : tuning.UserRockRotation4));
            }
        }
    }

    private static void MeteorCharge(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.MeteorCharge);
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        for (int i = 0; i < tuning.SigilDiscCount; i++)
        {
            float angle = i * MathF.Tau / 3 + progress * tuning.Angle;
            Vector2 seal = centre + Ellipse(angle, tuning.EllipseX, tuning.EllipseY);
            frame.Sprite(seal, tuning.SigilDiscWidth, tuning.SigilDiscHeight, tuning.SigilDiscShape, Gold, tuning.SigilDiscAlpha, Depth(angle), angle, light: tuning.SigilDiscLight);
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Fire);
    }

    private static void MeteorSeal(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.MeteorSeal);
        float open = tuning.Open + tuning.Open2 * Ease(progress * tuning.Open3);
        float pulse = tuning.Pulse + tuning.Pulse2 * MathF.Sin(progress * tuning.Pulse3);
        frame.Sprite(feet, tuning.OrbWidth * open, tuning.OrbHeight * open, tuning.OrbShape, Ember, tuning.OrbAlpha * pulse, tuning.OrbLayer, light: tuning.OrbLight);
        Seal(frame, feet, tuning.X * open, tuning.Y * open, Fire, progress * tuning.Rotation, pulse, tuning.Spokes);
        frame.Sprite(feet, tuning.GlyphWidth * open, tuning.GlyphHeight * open, tuning.GlyphShape, Gold, tuning.GlyphAlpha * pulse, tuning.GlyphLayer, rotation: progress * tuning.GlyphRotation);
    }

    private static float FieldDrop(float time, float contact, float variation)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.FieldDrop);
        // These fields get their tile packets before the server's 800 ms warmup ends.
        // Start with a descent, then stagger the repeated drops across neighboring tiles.
        float lead = tuning.Lead + variation * tuning.Lead2;
        if (time <= lead) return time / lead * contact;
        float after = time - lead;
        return Fract(contact + after * (tuning.Value + variation * tuning.Value2) + variation * Ease(after * tuning.Value3) * tuning.Value4);
    }
}
