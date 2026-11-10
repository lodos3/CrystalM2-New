using System.Numerics;
using Frame = Client.MirScenes.SpellParticleFrame;
using static Client.MirScenes.SpellParticleFrame;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static partial class WizardSpellEffects
{
    internal readonly record struct ArcColours(int Shadow, int Glow, int Filament, int Core);
    private static ArcColours StormColours => new(SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).StormColoursShadow, SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).StormColoursGlow, SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).StormColoursFilament, White);
    internal static ArcColours DrakeColours => new(SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).DrakeColoursShadow, SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).DrakeColoursGlow, SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).DrakeColoursFilament, SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).DrakeColoursCore);

    internal static void DrawArc(SpellParticlePool pool, Spell spell, Vector2 from, Vector2 to,
        long now, long start, int duration, float size, uint seed)
    {
        using var emitterTime = SpellVfxAssets.AtProgress((now - start) / (float)Math.Max(1, duration));
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.DrawArc);
        if (duration <= 0 || now < start || now - start >= duration) return;
        float progress = (now - start) / (float)duration;
        float time = (now - start) / 1000f;
        var frame = new Frame(pool, from, now, Envelope(progress), size, seed, scalePositions: false);
        Vector2 source = from - Vector2.UnitY * tuning.Source;
        Vector2 destination = to - Vector2.UnitY * tuning.Destination;
        if (spell == Spell.Vampirism)
        {
            Vector2 head = Vector2.Lerp(source, destination, Ease(progress)) - Vector2.UnitY * (tuning.Head * MathF.Sin(progress * MathF.PI));
            float depth = Vector2.Lerp(from, to, Ease(progress)).Y;
            frame.Sprite(head, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, light: tuning.OrbLight, sortY: depth, emitter: "WizardSpellEffects.Electricity.DrawArc.1");
            frame.Sprite(head, tuning.FireCurlWidth, tuning.FireCurlHeight, tuning.FireCurlShape, tuning.FireCurlColour, tuning.FireCurlAlpha, rotation: time * tuning.FireCurlRotation, sortY: depth, emitter: "WizardSpellEffects.Electricity.DrawArc.2");
            frame.Sprite(head, tuning.OrbWidth2, tuning.OrbHeight2, tuning.OrbShape2, tuning.OrbColour2, tuning.OrbAlpha2, tuning.OrbLayer, sortY: depth, emitter: "WizardSpellEffects.Electricity.DrawArc.3");
            for (int i = 1; i <= tuning.WispCount; i++)
            {
                float age = Math.Max(0, progress - i * tuning.Age);
                Vector2 tail = Vector2.Lerp(source, destination, Ease(age)) - Vector2.UnitY * (tuning.Tail * MathF.Sin(age * MathF.PI));
                frame.Sprite(tail, tuning.WispWidth - i * tuning.WispWidth2, tuning.WispHeight - i * tuning.WispHeight2, tuning.WispShape, i % 2 == 0 ? tuning.WispColour : tuning.WispColour2,
                    tuning.WispAlpha - i * tuning.WispAlpha2, sortY: Vector2.Lerp(from, to, Ease(age)).Y, emitter: "WizardSpellEffects.Electricity.DrawArc.4");
            }
        }
        else if (spell == Spell.ThunderBolt)
        {
            Channel(frame, source, destination, tuning.Spread, tuning.Width, tuning.Alpha, time, tuning.Forks, tuning.Offset, depths: new(from.Y, to.Y));
            frame.Sprite(destination, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight, sortY: to.Y, emitter: "WizardSpellEffects.Electricity.DrawArc.5");
        }
        else
            Lightning(frame, source, destination, time, new(from.Y, to.Y));
    }

    private static void Lightning(Frame frame, Vector2 source, Vector2 destination, float time, Vector2? depths = null)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Lightning);
        Channel(frame, source, destination, tuning.Spread, tuning.Width, tuning.Alpha, time, tuning.Forks, 0, depths: depths);
        Vector2 direction = destination - source;
        if (direction.LengthSquared() >= 36)
        {
            Vector2 normal = Vector2.Normalize(new Vector2(-direction.Y, direction.X));
            Channel(frame, source + normal * tuning.Source, destination - normal * tuning.End, tuning.Spread2, tuning.Width2, tuning.Alpha2, time + tuning.Time, tuning.Forks2, tuning.Offset, depths: depths);
            Channel(frame, source - normal * tuning.Source2, Vector2.Lerp(source, destination, tuning.Amount) + normal * tuning.End2,
                tuning.Spread3, tuning.Width3, tuning.Alpha3, time + tuning.Time2, tuning.Forks3, tuning.Offset2, depths: depths);
        }
        frame.Sprite(source, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Blue, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "WizardSpellEffects.Electricity.Lightning.1");
        frame.Sprite(source, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer, emitter: "WizardSpellEffects.Electricity.Lightning.2");
    }

    // The 2D path, tapered branches and filtered halo adapt ARCFLUX Lite by gennai (MIT).
    // See Data/ParticleCredits.txt. Coarse points survive each flicker, with new fine detail.
    internal static void Channel(Frame frame, Vector2 source, Vector2 end, float spread, float width, float alpha,
        float time, int forks, int offset, Layer layer = Layer.Emissive, ArcColours? colours = null, Vector2? depths = null,
        float corona = 0)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Channel);
        Vector2 delta = end - source;
        float length = delta.Length();
        if (length < 2) return;
        Vector2 heading = delta / length;
        int count = 1 << Math.Clamp((int)MathF.Ceiling(MathF.Log2(length / tuning.CountDivisor)), 2, 5);
        int flash = (int)(time * tuning.Flash);
        Span<Vector2> points = stackalloc Vector2[count + 1];
        Displace(frame, points, source, end, spread, offset, flash);
        ArcColours palette = colours ?? StormColours;
        Layer core = layer == Layer.Ground ? tuning.Core : tuning.Core2;
        float thread = Math.Clamp(width * tuning.Thread, .9f, 3.2f);
        for (int i = 1; i <= count; i++)
        {
            float step = i / (float)count;
            float decay = (1 - tuning.Decay * step) * (tuning.Decay2 + frame.Unit(offset + i * 3 + flash * 113) * tuning.Decay3);
            float? sortY = depths is { } path ? path.X + (path.Y - path.X) * ((i - tuning.SortY) / count) : null;
            Vector2 segment = points[i] - points[i - 1];
            Vector2 centre = (points[i - 1] + points[i]) * tuning.Centre;
            float angle = MathF.Atan2(segment.Y, segment.X);
            float glowSize = (tuning.GlowSize + width * tuning.GlowSize2) * decay;
            if (corona > 0)
            {
                frame.Sprite(centre, segment.Length() + tuning.WispWidth, tuning.WispHeight + tuning.WispHeight2 * corona, tuning.WispShape,
                    tuning.WispColour, corona * tuning.WispAlpha, tuning.WispLayer, angle, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Channel.1");
                frame.Sprite(centre, segment.Length() + tuning.WispWidth2, tuning.WispHeight3 + tuning.WispHeight4 * corona, tuning.WispShape2,
                    palette.Filament, corona * tuning.WispAlpha2, tuning.WispLayer2, angle, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Channel.2");
            }
            frame.Sprite(centre, segment.Length() + glowSize * tuning.WispWidth3, glowSize * tuning.WispHeight5,
                tuning.WispShape3, palette.Shadow, alpha * tuning.WispAlpha3, layer == Layer.Ground ? layer : tuning.WispLayer3, angle, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Channel.3");
            frame.Sprite(centre, segment.Length() + glowSize * tuning.WispWidth4, glowSize,
                tuning.WispShape4, palette.Glow, alpha * (tuning.WispAlpha4 + tuning.WispAlpha5 * decay), layer, angle, light: i % 3 == 1, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Channel.4");
            float thickness = thread * (tuning.Thickness + decay * tuning.Thickness2);
            frame.Thread(points[i - 1], points[i], thickness, thickness * (tuning.ThreadEndWidth + tuning.ThreadEndWidth2 * decay),
                i % 4 == 0 ? palette.Filament : palette.Core, alpha * (tuning.ThreadAlpha + tuning.ThreadAlpha2 * decay), core, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Channel.5");
            if (i % 3 == 0)
                // Fine raster lightning detail rides each jagged section.
                // The bright authored core stays readable at low F12 glow.
                frame.Sprite(centre, segment.Length() + tuning.UserLightningWidth, tuning.UserLightningHeight + width * tuning.UserLightningHeight2,
                    tuning.UserLightningShape, i % 2 == 0 ? palette.Core : palette.Filament,
                    alpha * tuning.UserLightningAlpha * decay, core, angle, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Channel.6");
            if (i % 3 == 1)
            {
                float shift = (frame.Unit(i + offset + flash * 51) - tuning.Shift) * tuning.Shift2;
                Vector2 spark = points[i] + new Vector2(-heading.Y, heading.X) * shift;
                frame.Sprite(spark, tuning.SparkWidth + tuning.SparkWidth2 * decay, tuning.SparkHeight + tuning.SparkHeight2 * decay, tuning.SparkShape, palette.Filament,
                    alpha * (tuning.SparkAlpha + decay * tuning.SparkAlpha2), core, angle, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Channel.7");
            }
        }

        for (int i = 0; i < forks; i++)
        {
            int seed = offset + 1391 + i * 37;
            float distribution = (i + tuning.Distribution + frame.Unit(seed) * tuning.Distribution2) / forks;
            int index = Math.Clamp((int)(distribution * count), 1, count - 1);
            float remaining = length * (1 - index / (float)count);
            Vector2 tangent = Vector2.Normalize(points[index + 1] - points[index - 1]);
            Vector2 forward = Vector2.Normalize(heading * tuning.Forward + tangent * tuning.Forward2);
            Vector2 side = new(-forward.Y, forward.X);
            float reach = Math.Min(remaining * tuning.Reach, tuning.Reach2);
            if (reach < 3) continue;
            float? sortY = depths is { } path ? path.X + (path.Y - path.X) * (index / (float)count) : null;
            Fork(frame, points[index], forward, side, reach, width * tuning.Width, alpha * tuning.Alpha, seed, layer,
                split: width >= 4.5f && i % 2 == 0, colours: palette, sortY: sortY);
        }
    }

    private static void Fork(Frame frame, Vector2 source, Vector2 heading, Vector2 side, float length, float width,
        float alpha, int seed, Layer layer, bool split = false, ArcColours? colours = null, float? sortY = null)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Fork);
        float angle = (tuning.Angle + tuning.Angle2 * frame.Unit(seed + 1)) * (frame.Unit(seed) < .5f ? -1 : 1);
        Vector2 forward = heading * MathF.Cos(angle) + side * MathF.Sin(angle);
        float reach = length * (tuning.Reach + tuning.Reach2 * frame.Unit(seed + 2));
        Span<Vector2> points = stackalloc Vector2[5];
        Displace(frame, points, source, source + forward * reach, reach * tuning.Spread, seed + 3, 0);
        ArcColours palette = colours ?? StormColours;
        for (int j = 1; j < points.Length; j++)
        {
            float progress = (j - tuning.Progress) / (points.Length - 1);
            float taper = 1 - progress * progress * tuning.Taper;
            float thickness = Math.Clamp(width * tuning.Thickness * taper, .55f, 2.25f);
            frame.Thread(points[j - 1], points[j], thickness * tuning.ThreadStartWidth, thickness * tuning.ThreadEndWidth,
                palette.Glow, alpha * tuning.ThreadAlpha * taper, layer, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Fork.1");
            frame.Thread(points[j - 1], points[j], thickness, thickness * tuning.ThreadEndWidth2,
                palette.Filament, alpha * taper, layer, sortY: sortY, emitter: "WizardSpellEffects.Electricity.Fork.2");
        }

        if (split && reach > 22)
            Fork(frame, points[2], forward, new Vector2(-forward.Y, forward.X), reach * tuning.Length, width * tuning.Width,
                alpha * tuning.Alpha, seed + 617, layer, colours: palette, sortY: sortY);
    }

    private static void Displace(Frame frame, Span<Vector2> points, Vector2 source, Vector2 end, float spread, int seed, int flash)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Displace);
        int segments = points.Length - 1;
        points[0] = source;
        points[segments] = end;
        Vector2 delta = end - source;
        float length = delta.Length();
        Vector2 side = new Vector2(-delta.Y, delta.X) / length;
        float chaos = Math.Clamp(spread * tuning.Chaos / length, .10f, .89f);
        for (int stride = segments, level = 0; stride > tuning.StrideCount; stride >>= 1, level++)
        {
            float variation = chaos * (tuning.Variation + level * tuning.Variation2);
            int detail = stride <= 4 ? flash * tuning.Detail : 0;
            for (int start = 0; start < segments; start += stride)
            {
                int index = start + stride / 2;
                Vector2 a = points[start], b = points[start + stride];
                float random = frame.Unit(seed + index * 19 + detail) + frame.Unit(seed + index * 19 + detail + 1) - 1;
                float bend = Math.Min(spread, Vector2.Distance(a, b) * variation);
                points[index] = (a + b) * tuning.Factor + side * (random * bend);
            }
        }
    }

    private static void ElectricWrap(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.ElectricWrap);
        float radius = prepare ? tuning.PrepareRadius : tuning.ReleaseRadius;
        float height = prepare ? tuning.PrepareHeight : tuning.ReleaseHeight;
        float drop = (prepare ? tuning.PrepareDrop : tuning.ReleaseDrop) * Ease(progress * tuning.Drop);
        float startHeight = height + tuning.StartHeight - drop;
        int flash = (int)(time * tuning.Flash);
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float sweep = (prepare ? tuning.PrepareSweep : tuning.ReleaseSweep) * MathF.Tau * (tuning.Sweep + frame.Unit(i + 9) * tuning.Sweep2);
            float spin = i * MathF.Tau / 3 + time * (prepare ? tuning.PrepareSpin : tuning.ReleaseSpin) + frame.Unit(i + 18) * tuning.Spin;
            Vector2 from = Point(0, spin);
            for (int j = 1; j <= tuning.WispCount2; j++)
            {
                float step = j / tuning.StepDivisor;
                float angle = spin + step * sweep;
                Vector2 to = Point(step, angle);
                Layer layer = Depth(angle - sweep / tuning.LayerDivisor);
                float opacity = (tuning.Opacity - step * tuning.Opacity2) * (tuning.Opacity3 + frame.Unit(j + i * 47 + flash * 21) * tuning.Opacity4);
                Vector2 middle = (from + to) * tuning.Middle;
                frame.Sprite(middle, Vector2.Distance(from, to) + tuning.WispWidth, tuning.WispHeight + opacity * tuning.WispHeight2, tuning.WispShape, j % 4 == 0 ? tuning.WispColour : Blue,
                    opacity * tuning.WispAlpha, layer, MathF.Atan2(to.Y - from.Y, to.X - from.X), light: j % 7 == 1, emitter: "WizardSpellEffects.Electricity.ElectricWrap.1");
                frame.Thread(from, to, tuning.ThreadStartWidth - step * tuning.ThreadStartWidth2, tuning.ThreadEndWidth - step * tuning.ThreadEndWidth2, j % 3 == 0 ? PaleBlue : White,
                    opacity, layer == Layer.Ground ? layer : tuning.ThreadLayer, emitter: "WizardSpellEffects.Electricity.ElectricWrap.2");
                if (j == 8 || j == 16)
                {
                    Fork(frame, to, Vector2.UnitY, Vector2.UnitX, tuning.Length, tuning.Width, tuning.Alpha, i * 29 + j + flash * 197, layer);
                    frame.Sprite(to, tuning.FlashWidth + frame.Unit(flash * 23 + i) * tuning.FlashWidth2, tuning.FlashHeight, tuning.FlashShape, PaleBlue, tuning.FlashAlpha,
                        layer == Layer.Ground ? layer : tuning.FlashLayer, emitter: "WizardSpellEffects.Electricity.ElectricWrap.3");
                }
                from = to;
            }

            Vector2 Point(float step, float angle)
            {
                float cross = .69f + .33f * MathF.Sin(step * MathF.PI);
                float jitter = frame.Unit(flash * 181 + (int)MathF.Round(step * 23) + i * 37) - .5f;
                float lift = MathF.Max(2, startHeight - step * (height * (.82f + frame.Unit(i + 7) * .16f)));
                return feet + Ellipse(angle, radius * cross + jitter * 20, radius * .34f) - Vector2.UnitY * (lift + jitter * 11);
            }
        }
        frame.Sprite(feet - Vector2.UnitY * (startHeight * tuning.WispOffset), radius * tuning.WispWidth2, height * tuning.WispHeight3, tuning.WispShape2,
            tuning.WispColour2, tuning.WispAlpha2, light: tuning.WispLight, emitter: "WizardSpellEffects.Electricity.ElectricWrap.4");
        frame.Sparks(feet - Vector2.UnitY * (startHeight * tuning.SparksOffset), tuning.SparksCount, radius * tuning.SparksRx, height * tuning.SparksRy, progress, PaleBlue, emitter: "WizardSpellEffects.Electricity.ElectricWrap.5");
    }

    private static void ThunderStrike(Frame frame, Vector2 feet, float progress, float time, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.ThunderStrike);
        if (prepare)
        {
            Vector2 bottom = feet - Vector2.UnitY * tuning.Bottom;
            Channel(frame, bottom + new Vector2(tuning.X, tuning.Y), bottom, tuning.Spread, tuning.Width, tuning.Alpha, time, tuning.Forks, tuning.Offset);
            frame.Sprite(bottom - Vector2.UnitY * tuning.UserLightningOffset, tuning.UserLightningWidth, tuning.UserLightningHeight,
                tuning.UserLightningShape, Blue, tuning.UserLightningAlpha, tuning.UserLightningLayer, light: tuning.UserLightningLight, emitter: "WizardSpellEffects.Electricity.ThunderStrike.1");
            frame.Sprite(bottom - Vector2.UnitY * tuning.WispOffset, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, tuning.WispColour,
                tuning.WispAlpha, tuning.WispLayer, tuning.WispRotation, emitter: "WizardSpellEffects.Electricity.ThunderStrike.2");
            return;
        }

        float power = 1 - Ease((progress - tuning.Power) * tuning.Power2);
        if (power > .015f)
        {
            Vector2 head = feet + new Vector2(tuning.X2, tuning.Y2);
            float peak = power * (tuning.Peak + tuning.Peak2 * MathF.Sin(MathF.PI * progress));
            Channel(frame, feet - Vector2.UnitY * tuning.Source, head, tuning.Spread2, tuning.Width2, power, time, tuning.Forks2, tuning.Offset2, corona: peak);
            Channel(frame, feet + new Vector2(tuning.X3, tuning.Y3), head + new Vector2(tuning.X4, 0), tuning.Spread3, tuning.Width3, power * tuning.Alpha2, time + tuning.Time, tuning.Forks3, tuning.Offset3);
            Channel(frame, feet + new Vector2(tuning.X5, tuning.Y4), head - new Vector2(tuning.X6, 0), tuning.Spread4, tuning.Width4, power * tuning.Alpha3, time, tuning.Forks4, tuning.Offset4);
            for (int i = 0; i < tuning.IonCoronaCount; i++)
            {
                float height = tuning.Height + i * tuning.Height2;
                Vector2 corona = feet + new Vector2((frame.Unit(i + 26) - tuning.X7) * tuning.X8, -height);
                frame.Sprite(corona, tuning.IonCoronaWidth - i * tuning.IonCoronaWidth2, tuning.IonCoronaHeight + i * tuning.IonCoronaHeight2, tuning.IonCoronaShape, i % 2 == 0 ? tuning.IonCoronaColour : tuning.IonCoronaColour2,
                    peak * (tuning.IonCoronaAlpha - i * tuning.IonCoronaAlpha2), tuning.IonCoronaLayer, tuning.IonCoronaRotation + i * tuning.IonCoronaRotation2, light: i < 2, emitter: "WizardSpellEffects.Electricity.ThunderStrike.3");
                frame.Sprite(corona + new Vector2(tuning.X9 - i * tuning.X10, 0), tuning.UserLightningWidth2 + i * tuning.UserLightningWidth3, tuning.UserLightningHeight2, tuning.UserLightningShape2, PaleBlue,
                    peak * (tuning.UserLightningAlpha2 - i * tuning.UserLightningAlpha3), tuning.UserLightningLayer2, tuning.UserLightningRotation + i * tuning.UserLightningRotation2, emitter: "WizardSpellEffects.Electricity.ThunderStrike.4");
            }
            frame.Sprite(feet - Vector2.UnitY * tuning.UserLightningOffset2, tuning.UserLightningWidth4, tuning.UserLightningHeight3,
                tuning.UserLightningShape3, Blue, power * tuning.UserLightningAlpha4, tuning.UserLightningLayer3, light: tuning.UserLightningLight2, emitter: "WizardSpellEffects.Electricity.ThunderStrike.5");
            frame.Sprite(feet - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, power * tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight, emitter: "WizardSpellEffects.Electricity.ThunderStrike.6");
        }
        frame.Sprite(feet - Vector2.UnitY * tuning.UserLightningOffset3, tuning.UserLightningWidth5, tuning.UserLightningHeight4,
            tuning.UserLightningShape4, Blue, tuning.UserLightningAlpha5, tuning.UserLightningLayer4, light: tuning.UserLightningLight3, emitter: "WizardSpellEffects.Electricity.ThunderStrike.7");
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float angle = (i + frame.Unit(i)) * MathF.Tau / tuning.AngleDivisor;
            Vector2 target = feet + Ellipse(angle, tuning.EllipseX, tuning.EllipseY) * (tuning.Target + progress * tuning.Target2);
            Channel(frame, feet, target, tuning.Spread5, tuning.Width5, tuning.Alpha4, time, 1, tuning.Offset5 * i, tuning.Layer);
            frame.Sprite(Vector2.Lerp(feet, target, tuning.Amount), tuning.WispWidth2, tuning.WispHeight2, tuning.WispShape2, tuning.WispColour2,
                tuning.WispAlpha2 + tuning.WispAlpha3 * Ease(progress * tuning.Value), tuning.WispLayer2, angle, emitter: "WizardSpellEffects.Electricity.ThunderStrike.8");
        }
        frame.Sparks(feet, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, PaleBlue, rise: tuning.SparksRise, emitter: "WizardSpellEffects.Electricity.ThunderStrike.9");
    }

    private static void ThunderStorm(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.ThunderStorm);
        float radius = tuning.Radius * (tuning.Radius2 + tuning.Radius3 * Ease(progress * tuning.Radius4));
        // Grounded strikes are made of independent bolts; never fill the
        // entire radius with an opaque blue disc.
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float angle = (i + frame.Unit(i)) * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle;
            Vector2 spread = Ellipse(angle, radius, radius * FloorAspect);
            Channel(frame, feet + spread * tuning.Source, feet + spread * (tuning.End + frame.Unit(i + 40) * tuning.End2),
                tuning.Spread, tuning.Width, tuning.Alpha, time, tuning.Forks, tuning.Offset * i, tuning.Layer);
            Vector2 root = feet + spread * (tuning.Root + tuning.Root2 * frame.Unit(i + 17));
            Vector2 flare = feet + Ellipse(angle + tuning.EllipseAngle, radius * tuning.EllipseX, radius * tuning.EllipseY) - Vector2.UnitY * (tuning.Flare + tuning.Flare2 * frame.Unit(i + 31));
            Channel(frame, root, flare, tuning.Spread2, tuning.Width2, tuning.Alpha2, time, 1, tuning.Offset2 + i * tuning.Offset3, Depth(angle));
        }
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        frame.Sprite(body, tuning.UserLightningWidth, tuning.UserLightningHeight, tuning.UserLightningShape, PaleBlue,
            tuning.UserLightningAlpha, tuning.UserLightningLayer, rotation: progress * tuning.UserLightningRotation, light: tuning.UserLightningLight, emitter: "WizardSpellEffects.Electricity.ThunderStorm.1");
        frame.Sprite(body + new Vector2(tuning.X, tuning.Y), tuning.UserLightningWidth2, tuning.UserLightningHeight2,
            tuning.UserLightningShape2, White, tuning.UserLightningAlpha2 * (1 - progress * tuning.UserLightningAlpha3), tuning.UserLightningLayer2, emitter: "WizardSpellEffects.Electricity.ThunderStorm.2");
        frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount, radius, radius * FloorAspect, progress, PaleBlue, rise: tuning.SparksRise, emitter: "WizardSpellEffects.Electricity.ThunderStorm.3");
    }

    private static void StormEscape(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.StormEscape);
        float radius = tuning.Radius + tuning.Radius2 * Ease(progress * tuning.Radius3);
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        for (int i = 0; i < tuning.UserLightningCount; i++)
        {
            float angle = i * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle;
            Vector2 from = centre + Ellipse(angle, radius, radius * tuning.EllipseY);
            Vector2 to = centre + Ellipse(angle + tuning.EllipseAngle, radius, radius * tuning.EllipseY2);
            Layer layer = Depth(angle + tuning.Layer);
            Channel(frame, from, to, tuning.Spread, tuning.Width, tuning.Alpha, time, 1, i * tuning.Offset, layer);
            frame.Sprite(from, tuning.UserLightningWidth, tuning.UserLightningHeight, tuning.UserLightningShape, i % 2 == 0 ? PaleBlue : Blue,
                tuning.UserLightningAlpha, layer, angle, light: i % 2 == 0, emitter: "WizardSpellEffects.Electricity.StormEscape.1");
            Vector2 edge = Ellipse(angle + frame.Unit(i) * tuning.EllipseAngle2, tuning.EllipseX + tuning.EllipseX2 * frame.Unit(i + 19), tuning.EllipseY3);
            Channel(frame, from, from + edge, tuning.Spread2, tuning.Width2, tuning.Alpha2, time, 1, tuning.Offset2 + i * tuning.Offset3, layer);
        }
        // No orbit/ring billboard; moving arcs already define the storm. 
        frame.Sparks(centre, tuning.SparksCount, radius * tuning.SparksRx, radius * tuning.SparksRy, progress, PaleBlue, rise: tuning.SparksRise, emitter: "WizardSpellEffects.Electricity.StormEscape.2");
    }

    private static void StormArrival(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.StormArrival);
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        float radius = tuning.Radius + tuning.Radius2 * Ease(progress * tuning.Radius3);
        frame.Sprite(centre, tuning.UserLightningWidth, tuning.UserLightningHeight, tuning.UserLightningShape, Blue, tuning.UserLightningAlpha,
            tuning.UserLightningLayer, rotation: progress * tuning.UserLightningRotation, light: tuning.UserLightningLight, emitter: "WizardSpellEffects.Electricity.StormArrival.1");
        frame.Sprite(centre, tuning.UserLightningWidth2, tuning.UserLightningHeight2, tuning.UserLightningShape2, PaleBlue, tuning.UserLightningAlpha2,
            tuning.UserLightningLayer2, rotation: tuning.UserLightningRotation2 + progress * tuning.UserLightningRotation3, emitter: "WizardSpellEffects.Electricity.StormArrival.2");
        frame.Sprite(centre, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha * (1 - progress), tuning.FlashLayer, light: tuning.FlashLight, emitter: "WizardSpellEffects.Electricity.StormArrival.3");
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float angle = (i + frame.Unit(i) * tuning.Angle) * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle2;
            Channel(frame, centre, centre + Ellipse(angle, radius, radius * tuning.EllipseY), tuning.Spread, tuning.Width, tuning.Alpha, time, 1, i * tuning.Offset);
        }
    }
}
