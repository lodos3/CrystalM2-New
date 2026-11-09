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
        var tuning = SpellVfxAssets.Current.Taoist.Trap;
        if (prepare)
        {
            float open = Ease(progress * tuning.Open);
            Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + tuning.Centre2 * open);
            frame.Sprite(centre, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, InkBlue, tuning.WispAlpha, light: tuning.WispLight);
            for (int i = 0; i < tuning.FlashCount; i++)
            {
                float turn = progress * tuning.Turn + i * MathF.PI;
                Vector2 start = centre;
                for (int j = 1; j <= tuning.FlashCount2; j++)
                {
                    float t = j / tuning.TDivisor;
                    Vector2 end = centre + Ellipse(turn + t * tuning.EllipseAngle, tuning.EllipseX, tuning.EllipseY) * (t * open);
                    frame.Thread(start, end, tuning.ThreadStartWidth * (tuning.ThreadStartWidth2 + t), tuning.ThreadEndWidth * (tuning.ThreadEndWidth2 + t),
                        j % 3 == 0 ? White : Ice, tuning.ThreadAlpha, Depth(turn + t * tuning.Angle));
                    if (j == 15) frame.Sprite(end, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer, turn);
                    start = end;
                }
            }
            return;
        }
        float pulse = tuning.Pulse + tuning.Pulse2 * MathF.Sin(progress * MathF.PI);
        frame.Sprite(feet, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, InkBlue, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        Vector2 head = feet - Vector2.UnitY * (tuning.Head + tuning.Head2 * pulse);
        frame.Sprite((head + feet) * tuning.WispOffset, tuning.WispWidth2, tuning.WispHeight2 * pulse, tuning.WispShape2, Blue, tuning.WispAlpha2);
        frame.Thread(feet, head, tuning.ThreadStartWidth3, tuning.ThreadEndWidth3, Ice, tuning.ThreadAlpha2, tuning.ThreadLayer);
        frame.Sprite(feet, tuning.FlashWidth2, tuning.FlashHeight2, tuning.FlashShape2, White, tuning.FlashAlpha2, tuning.FlashLayer2, tuning.FlashRotation);
        frame.Sparks(feet, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Blue, tuning.SparksRise);
    }

    private static void Phantom(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Phantom;
        float rise = Ease(progress) * tuning.Rise;
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + rise);
        frame.Sprite(feet, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, tuning.WispColour, tuning.WispAlpha, tuning.WispLayer);
        frame.Clip(centre, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer);
        frame.Sprite(centre, tuning.SpiritFigureWidth, tuning.SpiritFigureHeight, tuning.SpiritFigureShape, tuning.SpiritFigureColour, tuning.SpiritFigureAlpha);
        frame.Sprite(centre - Vector2.UnitY * tuning.SpiritFigureOffset, tuning.SpiritFigureWidth2, tuning.SpiritFigureHeight2, tuning.SpiritFigureShape2, tuning.SpiritFigureColour2, tuning.SpiritFigureAlpha2, light: tuning.SpiritFigureLight);
        frame.Sprite(centre - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Ice, tuning.FlashAlpha, tuning.FlashLayer);
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Violet, tuning.SparksRise);
    }

    private static void Enhance(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Enhance;
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + Ease(progress) * tuning.Centre2);
        float open = tuning.Open + tuning.Open2 * Ease(progress * tuning.Open3);
        frame.Sprite(feet, tuning.OrbWidth * open, tuning.OrbHeight * open, tuning.OrbShape, Ochre, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        frame.Sprite(centre, tuning.OrbWidth2 * open, tuning.OrbHeight2 * open, tuning.OrbShape2, Amber, tuning.OrbAlpha2, light: tuning.OrbLight2);
        frame.Sprite(centre - Vector2.UnitY * tuning.OrbOffset, tuning.OrbWidth3 * open, tuning.OrbHeight3 * open, tuning.OrbShape3, Ivory, tuning.OrbAlpha3, tuning.OrbLayer2);
        frame.Sprite(centre + Vector2.UnitY * tuning.OrbOffset2, tuning.OrbWidth4 * open, tuning.OrbHeight4 * open, tuning.OrbShape4, Gold, tuning.OrbAlpha4, tuning.OrbLayer3);
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float turn = (i + frame.Unit(i) * tuning.Turn) * MathF.Tau / tuning.TurnDivisor + progress;
            float reach = (tuning.Reach + frame.Unit(i + 27) * tuning.Reach2) * open;
            Vector2 ray = Ellipse(turn, reach, reach * tuning.EllipseY);
            Vector2 point = centre + ray * (tuning.Point + frame.Unit(i + 53) * tuning.Point2);
            float rotation = MathF.Atan2(ray.Y, ray.X);
            frame.Sprite(point, tuning.WispWidth + frame.Unit(i + 11) * tuning.WispWidth2, tuning.WispHeight + frame.Unit(i + 45) * tuning.WispHeight2, tuning.WispShape,
                i % 3 == 0 ? tuning.WispColour : tuning.WispColour2, tuning.WispAlpha, Depth(turn), rotation);
            frame.Thread(centre + ray * tuning.ThreadStart, point, tuning.ThreadStartWidth, tuning.ThreadEndWidth, i % 3 == 0 ? Gold : Amber, tuning.ThreadAlpha, Depth(turn));
            if (i % 4 == 0)
                frame.Sprite(point, tuning.FireCurlWidth, tuning.FireCurlHeight, tuning.FireCurlShape, Gold, tuning.FireCurlAlpha, tuning.FireCurlLayer, rotation);
        }
        if (progress > .61f)
            frame.Thread(centre + Vector2.UnitY * tuning.ThreadStart2, centre - Vector2.UnitY * (tuning.ThreadEnd * Ease((progress - tuning.Value) / tuning.ValueDivisor)),
                tuning.ThreadStartWidth2, tuning.ThreadEndWidth2, Ivory, tuning.ThreadAlpha2, tuning.ThreadLayer);
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx * open, tuning.SparksRy * open, progress, Gold, tuning.SparksRise);
    }

    private static void Shinsu(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Shinsu;
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + progress * tuning.Centre2);
        float open = tuning.Open + tuning.Open2 * Ease(progress * tuning.Open3);
        float fade = 1 - Ease((progress - tuning.Fade) / tuning.FadeDivisor);
        frame.Sprite(feet, tuning.HaloWidth * open, tuning.HaloHeight * open, tuning.HaloShape, Amber, tuning.HaloAlpha * fade,
            tuning.HaloLayer, light: tuning.HaloLight);
        frame.Sprite(centre, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, Ochre, tuning.WispAlpha * fade, tuning.WispLayer, light: tuning.WispLight);
        for (int i = 0; i < tuning.TalismanCount; i++)
        {
            float turn = i * MathF.Tau / tuning.TurnDivisor + progress * tuning.Turn;
            Vector2 point = centre + Ellipse(turn, tuning.EllipseX * open, tuning.EllipseY * open);
            Layer layer = Depth(turn);
            frame.Sprite(point, tuning.TalismanWidth + i % 3 * tuning.TalismanWidth2, tuning.TalismanHeight + i % 2 * tuning.TalismanHeight2, tuning.TalismanShape, Ivory, fade, layer, turn + tuning.TalismanRotation);
            frame.Clip(point - Vector2.UnitY * tuning.FlameSequenceOffset, tuning.FlameSequenceWidth, tuning.FlameSequenceHeight, tuning.FlameSequenceShape, progress * tuning.FlameSequencePhase + i * tuning.FlameSequencePhase2,
                i % 2 == 0 ? tuning.FlameSequenceColour : Amber, tuning.FlameSequenceAlpha * fade, layer, turn * tuning.FlameSequenceRotation);
            frame.Sprite(point, tuning.WispWidth2, tuning.WispHeight2, tuning.WispShape2, Amber, tuning.WispAlpha2, layer);
            float ignite = tuning.Ignite + tuning.Ignite2 * MathF.Pow(MathF.Sin(progress * MathF.Tau * tuning.Ignite3 - i), tuning.Ignite4);
            frame.Sprite(point - Vector2.UnitY * tuning.UserFlameOffset, tuning.UserFlameWidth, tuning.UserFlameHeight, tuning.UserFlameShape,
                i % 2 == 0 ? Amber : Gold, tuning.UserFlameAlpha * ignite * fade, layer, turn * tuning.UserFlameRotation, light: i % 2 == 0);
            if (progress > .32f)
                for (int j = 0; j < tuning.FeatherCount; j++)
                {
                    Vector2 debris = point + Ellipse(turn + (j - 1) * tuning.EllipseAngle, tuning.EllipseX2 * progress, tuning.EllipseY2 * progress);
                    frame.Sprite(debris, tuning.FeatherWidth, tuning.FeatherHeight, tuning.FeatherShape, j % 2 == 0 ? Ivory : Amber, tuning.FeatherAlpha - j * tuning.FeatherAlpha2, layer, turn * tuning.FeatherRotation);
                }
        }
        frame.Sparks(feet - Vector2.UnitY * tuning.SparkOffset, tuning.SparkCount, tuning.SparkRx * open, tuning.SparkRy * open,
            progress, Gold, tuning.SparkRise, tuning.SparkShape);
        for (int i = 0; i < tuning.FlashCount; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i + 166));
            float turn = i * tuning.Turn2 + progress * tuning.Turn3;
            Vector2 point = centre + Ellipse(turn, tuning.EllipseX3 + age * tuning.EllipseX4, tuning.EllipseY3 + age * tuning.EllipseY4) -
                Vector2.UnitY * (age * tuning.Point);
            frame.Sprite(point, tuning.FlashWidth + tuning.FlashWidth2 * (1 - age), tuning.FlashHeight + tuning.FlashHeight2 * (1 - age), tuning.FlashShape,
                i % 3 == 0 ? Ivory : Amber, tuning.FlashAlpha * fade * (1 - age * tuning.FlashAlpha2),
                tuning.FlashLayer, turn, light: i % 4 == 0);
        }
    }

    private static void Revive(Frame frame, Vector2 feet, float progress)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Revive;
        float lift = tuning.Lift + tuning.Lift2 * MathF.Sin(progress * MathF.PI);
        frame.Sprite(feet, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, InkBlue, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        frame.Sprite(feet, tuning.HaloWidth, tuning.HaloHeight, tuning.HaloShape, Ice, tuning.HaloAlpha, tuning.HaloLayer);
        for (int i = -1; i <= tuning.PillarCount; i += tuning.SampleStride)
        {
            Vector2 root = feet + new Vector2(i * tuning.X, 0);
            Vector2 centre = root - Vector2.UnitY * (tuning.Centre * lift);
            frame.Sprite(centre + new Vector2(i * tuning.X2, 0), tuning.PillarWidth, tuning.PillarHeight * lift, tuning.PillarShape, InkBlue, tuning.PillarAlpha, tuning.PillarLayer, i * tuning.PillarRotation);
            frame.Sprite(centre, tuning.WispWidth, tuning.WispHeight * lift, tuning.WispShape, Blue, tuning.WispAlpha, tuning.WispLayer, i * tuning.WispRotation, light: tuning.WispLight);
            frame.Sprite(root - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer, i * tuning.FlashRotation);
        }
        frame.Sparks(feet, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Ice, tuning.SparksRise);
    }

    private static void Plague(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Plague;
        float open = tuning.Open + tuning.Open2 * Ease(progress * tuning.Open3);
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        frame.Sprite(feet, tuning.OrbWidth * open, tuning.OrbHeight * open, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
        for (int i = 0; i < tuning.FlameSequenceCount; i++)
        {
            float turn = i * tuning.Turn + progress * tuning.Turn2;
            float age = Fract(time * tuning.Age + frame.Unit(i + 31) * tuning.Age2);
            float height = (tuning.Height + frame.Unit(i + 17) * tuning.Height2) * open;
            Vector2 root = feet + Ellipse(turn, tuning.EllipseX * open, tuning.EllipseY * open) * (tuning.Root + frame.Unit(i + 3) * tuning.Root2);
            Vector2 point = root + new Vector2(MathF.Cos(turn) * age * tuning.X, -height * (tuning.Y + age * tuning.Y2));
            Layer depth = Depth(turn);
            float lean = tuning.Lean * MathF.Cos(turn);
            frame.Clip(point, tuning.FlameSequenceWidth + tuning.FlameSequenceWidth2 * age, height, tuning.FlameSequenceShape, age + time * tuning.FlameSequencePhase,
                i % 3 == 0 ? tuning.FlameSequenceColour : tuning.FlameSequenceColour2, tuning.FlameSequenceAlpha, depth, lean, sortY: root.Y);
            frame.Sprite(point - new Vector2(lean * tuning.X2, height * tuning.Y3), tuning.FireCurlWidth, tuning.FireCurlHeight, tuning.FireCurlShape,
                i % 3 == 0 ? Blue : tuning.FireCurlColour, tuning.FireCurlAlpha * (1 - age * tuning.FireCurlAlpha2), depth, turn, light: i % 3 == 0, sortY: root.Y);
            frame.Sprite(point + Vector2.UnitY * (height * tuning.FlameTongueOffset), tuning.FlameTongueWidth, height * tuning.FlameTongueHeight, tuning.FlameTongueShape,
                Ice, tuning.FlameTongueAlpha * (1 - age * tuning.FlameTongueAlpha2), depth == Layer.Ground ? depth : tuning.FlameTongueLayer, lean, sortY: root.Y);
        }
        frame.Clip(centre, tuning.SmokeSequenceWidth * open, tuning.SmokeSequenceHeight * open, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase, InkBlue, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer);
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx * open, tuning.SparksRy, progress, Ice, tuning.SparksRise);
    }

    internal static void DrawPoisonField(CrystalM2.Rendering.Core.SpellParticlePool pool, Vector2 feet, long now, long start,
        float size, uint seed, uint field, Vector2 inward, int neighbours)
    {
        using var vfxScope = SpellVfxAssets.For(Spell.PoisonCloud);
        var tuning = SpellVfxAssets.Current.Taoist.DrawPoisonField;
        float time = (now - start) * tuning.Time;
        if (time < 0) return;
        var frame = new Frame(pool, feet, now, 1, size, seed ^ (uint)Spell.PoisonCloud * 0x2d5bu, field);
        Cloud(frame, feet, time, inward, neighbours);
    }

    private static void Cloud(Frame frame, Vector2 feet, float time, Vector2 inward = default, int neighbours = 2)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Cloud;
        float bloom = tuning.Bloom + tuning.Bloom2 * Ease(time * tuning.Bloom3);
        Vector2 drift = new(MathF.Sin(time * tuning.Drift + feet.Y * tuning.Drift2) * tuning.Drift3,
            MathF.Cos(time * tuning.Drift4 + feet.X * tuning.Drift5) * tuning.Drift6);
        Vector2 centre = feet + inward + drift;
        float coverage = tuning.Coverage + neighbours * tuning.Coverage2;
        frame.Sprite(centre, tuning.MistFilamentWidth * bloom, tuning.MistFilamentHeight * bloom, tuning.MistFilamentShape, tuning.MistFilamentColour, tuning.MistFilamentAlpha * coverage,
            tuning.MistFilamentLayer, rotation: MathF.Sin(time + feet.X * tuning.X) * tuning.MistFilamentRotation, light: tuning.MistFilamentLight);
        for (int i = 0; i < tuning.SmokeSequenceCount; i++)
        {
            float age = Fract(time * (tuning.Age + tuning.Age2 * frame.Unit(i + 34)) + frame.Unit(i + 59));
            float turn = i * tuning.Turn + time * tuning.Turn2 + frame.Unit(i + 11) * tuning.Turn3;
            Vector2 root = centre + ProjectGround(Ellipse(turn, tuning.EllipseX, tuning.EllipseY)) * (tuning.Root + tuning.Root2 * age);
            Vector2 point = root - Vector2.UnitY * (tuning.Point + tuning.Point2 * frame.Unit(i + 11) + age * tuning.Point3);
            float opacity = coverage * (tuning.Opacity + tuning.Opacity2 * MathF.Sin(age * MathF.PI));
            frame.Clip(point, (tuning.SmokeSequenceWidth + tuning.SmokeSequenceWidth2 * frame.Unit(i + 2)) * bloom, (tuning.SmokeSequenceHeight + frame.Unit(i + 3) * tuning.SmokeSequenceHeight2) * bloom,
                tuning.SmokeSequenceShape, age + frame.Unit(i + 8), i % 3 == 0 ? tuning.SmokeSequenceColour : i % 3 == 1 ? tuning.SmokeSequenceColour2 : tuning.SmokeSequenceColour3, opacity,
                MathF.Sin(turn) < 0 ? tuning.SmokeSequenceLayer : tuning.SmokeSequenceLayer2, MathF.Sin(turn) * tuning.SmokeSequenceRotation, sortY: root.Y);
            if (i % 2 == 0)
                frame.Sprite(point, tuning.MistFilamentWidth2 + tuning.MistFilamentWidth3 * age, tuning.MistFilamentHeight2 + age * tuning.MistFilamentHeight3, tuning.MistFilamentShape2, tuning.MistFilamentColour2, tuning.MistFilamentAlpha2 * opacity,
                    tuning.MistFilamentLayer2, turn * tuning.MistFilamentRotation2, light: i == 0, sortY: root.Y);
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx * bloom, tuning.SparksRy * bloom, time * tuning.SparksProgress, tuning.SparksColour, tuning.SparksRise, sortY: centre.Y);
    }

    private static void Shield(Frame frame, Vector2 feet, float progress, bool aura)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Shield;
        // Anchor the shell to the actor and send pulses along each curved rim.
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        float spread = aura ? 0 : tuning.Spread * (1 - Ease(progress * tuning.Spread2));

        for (int side = -1; side <= tuning.SideCount; side += tuning.SampleStride)
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

            int segments = tuning.Segments;
            for (int i = 1; i <= segments; i++)
            {
                float t0 = (i - 1) / (float)segments;
                float t1 = i / (float)segments;
                float taper = MathF.Sin(t1 * MathF.PI);
                Vector2 from = ShellPoint(t0), to = ShellPoint(t1);
                Vector2 arcFrom = ElectricPoint(t0), arcTo = ElectricPoint(t1);
                float wave = tuning.Wave + tuning.Wave2 * MathF.Cos(MathF.Tau * (progress * tuning.Wave3 + side * tuning.Wave4 - t1));
                float pulse = tuning.Pulse + tuning.Pulse2 * wave * wave;

                // Unchanging outline, with a separate high-frequency noisy core.
                frame.Thread(from, to, tuning.ThreadStartWidth + taper * tuning.ThreadStartWidth2, tuning.ThreadEndWidth + taper * tuning.ThreadEndWidth2,
                    colour, tuning.ThreadAlpha * pulse, tuning.ThreadLayer);
                frame.Thread(arcFrom, arcTo, tuning.ThreadStartWidth3 + taper * tuning.ThreadStartWidth4, 1.0f + taper * tuning.ThreadEndWidth3,
                    rim, tuning.ThreadAlpha2 * pulse, tuning.ThreadLayer2, light: i % 6 == 1);

                if (i % 4 == 1)
                {
                    Vector2 midpoint = (arcFrom + arcTo) * tuning.Midpoint;
                    frame.Sprite(midpoint, tuning.WispWidth + taper * tuning.WispWidth2, tuning.WispHeight + taper * tuning.WispHeight2,
                        tuning.WispShape, colour, tuning.WispAlpha + tuning.WispAlpha2 * wave * wave, tuning.WispLayer,
                        MathF.Atan2(arcTo.Y - arcFrom.Y, arcTo.X - arcFrom.X),
                        light: i % 8 == 1);
                    float tangent = MathF.Atan2(to.Y - from.Y, to.X - from.X);
                    frame.Sprite(midpoint + Vector2.UnitX * side * (tuning.UserLightningOffset + tuning.UserLightningOffset2 * wave), tuning.UserLightningWidth + taper * tuning.UserLightningWidth2, tuning.UserLightningHeight + taper * tuning.UserLightningHeight2,
                        tuning.UserLightningShape, colour, tuning.UserLightningAlpha * pulse * taper, tuning.UserLightningLayer,
                        tangent - MathF.PI * tuning.UserLightningRotation);
                    frame.Sprite(midpoint + Vector2.UnitX * side * tuning.MistFilamentOffset, tuning.MistFilamentWidth * taper + tuning.MistFilamentWidth2, tuning.MistFilamentHeight + taper * tuning.MistFilamentHeight2,
                        tuning.MistFilamentShape, side < 0 ? tuning.MistFilamentColour : Amber, tuning.MistFilamentAlpha * pulse * taper,
                        tuning.MistFilamentLayer, tangent, light: i % 8 == 5);
                }
            }

            // Sparks originate along the outline, not from an orbit around the
            // character. They eject a few pixels before fading away.
            for (int i = 0; i < tuning.FlashCount; i++)
            {
                int seed = i + (side + 1) * 31;
                float age = Fract(progress * tuning.Age + frame.Unit(seed + 179));
                float t = (tuning.T + frame.Unit(seed + 23) * tuning.T2);
                Vector2 anchor = ElectricPoint(t);
                float drift = MathF.Sin(age * tuning.Drift + seed) * age * tuning.Drift2;
                Vector2 point = anchor + new Vector2(side * (tuning.X + age * tuning.X2) + drift, -age * (tuning.Y + tuning.Y2 * frame.Unit(seed + 71)));
                float alpha = tuning.Alpha * MathF.Sin(age * MathF.PI);
                frame.Sprite(point, tuning.FlashWidth + frame.Unit(seed + 29) * tuning.FlashWidth2,
                    tuning.FlashHeight + frame.Unit(seed + 33) * tuning.FlashHeight2,
                    i % 4 == 0 ? tuning.FlashShape : tuning.FlashShape2, i % 2 == 0 ? rim : colour,
                    alpha, tuning.FlashLayer, side * tuning.FlashRotation, light: i % 6 == 0);
                if (i % 4 == 0)
                    frame.Sprite(point, tuning.WispWidth3 + age * tuning.WispWidth4, tuning.WispHeight3 + age * tuning.WispHeight4, tuning.WispShape2, colour,
                        tuning.WispAlpha3 * alpha, tuning.WispLayer2, drift * tuning.WispRotation);
            }
        }
    }

    private static void Pet(Frame frame, Vector2 feet, float progress, float time, bool aura)
    {
        var tuning = SpellVfxAssets.Current.Taoist.Pet;
        Vector2 centre = feet - Vector2.UnitY * (aura ? tuning.AuraCentre : tuning.ImpactCentre);
        if (!aura)
        {
            float open = tuning.Open + tuning.Open2 * Ease(progress * tuning.Open3);
            frame.Sprite(feet, tuning.OrbWidth * open, tuning.OrbHeight * open, tuning.OrbShape, Ochre, tuning.OrbAlpha, tuning.OrbLayer);
            frame.Clip(centre, tuning.FlameSequenceWidth * open, tuning.FlameSequenceHeight * open, tuning.FlameSequenceShape, tuning.FlameSequencePhase + progress * tuning.FlameSequencePhase2, Amber, tuning.FlameSequenceAlpha, light: tuning.FlameSequenceLight);
            frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx * open, tuning.SparksRy * open, progress, Gold, tuning.SparksRise);
            return;
        }
        // A small, shallow rune above the head: no giant vertical banner.
        float flicker = tuning.Flicker + tuning.Flicker2 * MathF.Sin(progress * MathF.Tau * tuning.Flicker3);
        frame.Sprite(centre, tuning.ArmourGlyphWidth, tuning.ArmourGlyphHeight, tuning.ArmourGlyphShape, Amber, tuning.ArmourGlyphAlpha * flicker);
        frame.Sprite(centre, tuning.ClawWidth, tuning.ClawHeight, tuning.ClawShape, Gold, tuning.ClawAlpha * flicker,
            tuning.ClawLayer, tuning.ClawRotation * MathF.Sin(time * tuning.X));
        frame.Sprite(centre, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Ivory, tuning.FlashAlpha * flicker,
            tuning.FlashLayer, light: tuning.FlashLight);
        frame.Sparks(centre, tuning.SparksCount2, tuning.SparksRx2, tuning.SparksRy2, progress, Ivory, tuning.SparksRise2);
    }

    private static void HealingCircle(Frame frame, Vector2 feet, float progress, float time, bool field, float fieldSize)
    {
        var tuning = SpellVfxAssets.Current.Taoist.HealingCircle;
        float opening = Ease((field ? time : progress) * tuning.Opening);
        float width = field ? fieldSize : tuning.Width * (tuning.Width2 + opening * tuning.Width3);
        float height = width * GroundAspect;
        float rotation = (field ? time : progress) * tuning.Rotation;
        Vector2 centre = feet - Vector2.UnitY * 1;
        float pulse = tuning.Pulse + tuning.Pulse2 * MathF.Sin(time * MathF.Tau / tuning.PulseDivisor);
        frame.Sprite(centre, width * tuning.WispWidth, height * tuning.WispHeight, tuning.WispShape, InkBlue, tuning.WispAlpha, tuning.WispLayer, light: tuning.WispLight);
        frame.FloorSeal(centre, width * tuning.TrigramsWidth, 0, tuning.TrigramsShape, tuning.TrigramsColour, tuning.TrigramsAlpha);
        frame.FloorSeal(centre, width, 0, tuning.TrigramsShape2, Blue, tuning.TrigramsAlpha2, light: tuning.TrigramsLight);
        frame.FloorSeal(centre, width * tuning.TrigramsWidth2, 0, tuning.TrigramsShape3, Ice, tuning.TrigramsAlpha3 * pulse);
        frame.FloorSeal(centre, width * tuning.YinYangWidth, rotation, tuning.YinYangShape, Ice, tuning.YinYangAlpha, light: tuning.YinYangLight);
        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float angle = i * MathF.PI / tuning.AngleDivisor + time * tuning.Angle;
            float age = Fract(time * tuning.Age + frame.Unit(i));
            Vector2 point = feet + Ellipse(angle, width * tuning.EllipseX, height * tuning.EllipseY) * (tuning.Point + age * tuning.Point2) -
                Vector2.UnitY * (age * tuning.Point3);
            frame.Sprite(point, tuning.SparkWidth + frame.Unit(i + 6) * tuning.SparkWidth2, tuning.SparkHeight, tuning.SparkShape, i % 3 == 0 ? White : Blue, tuning.SparkAlpha - age * tuning.SparkAlpha2,
                Depth(angle), -angle);
        }
    }
}
