using System.Numerics;
using Frame = Client.MirScenes.SpellParticleFrame;
using static Client.MirScenes.SpellParticleFrame;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static partial class WizardSpellEffects
{
    internal const float DefaultSize = SpellParticleFrame.ReferenceSize;
    internal static int LightningDuration
    {
        get
        {
            using var vfxScope = SpellVfxAssets.For(Spell.Lightning);
            return SpellVfxAssets.Current.Wizard.ArcTiming.LightningDuration;
        }
    }

    internal static int RelayDuration(Spell spell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var timing = SpellVfxAssets.Current.Wizard.ArcTiming;
        return spell == Spell.Vampirism ? timing.VampirismDurationMs : timing.RelayDurationMs;
    }

    private const float FloorAspect = 2f / 3f;
    private static int Gold => SpellVfxAssets.Current.Wizard.Palette.Gold;
    private static int Fire => SpellVfxAssets.Current.Wizard.Palette.Fire;
    private static int Ember => SpellVfxAssets.Current.Wizard.Palette.Ember;
    private static int Hot => SpellVfxAssets.Current.Wizard.Palette.Hot;
    private static int Blue => SpellVfxAssets.Current.Wizard.Palette.Blue;
    private static int PaleBlue => SpellVfxAssets.Current.Wizard.Palette.PaleBlue;
    private static int White => SpellVfxAssets.Current.Wizard.Palette.White;
    private static int Ice => SpellVfxAssets.Current.Wizard.Palette.Ice;
    private static int IceWhite => SpellVfxAssets.Current.Wizard.Palette.IceWhite;

    internal static bool HasArt(Spell spell) => SpellCurriculum.Owns(MirClass.Wizard, spell);

    internal static int PacketDuration(SpellVisual visual)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Wizard.PacketDuration;
        return visual.Phase switch
        {
            SpellVisualPhase.Contact => tuning.ContactDurationMs,
            SpellVisualPhase.Ward => tuning.WardDurationMs,
            SpellVisualPhase.Burn => tuning.BurnDurationMs,
            _ => visual.Spell == Spell.IceThrust ? tuning.IceThrustDurationMs : tuning.DefaultDurationMs
        };
    }

    // The legacy offsets use the upper left of a 48 × 32 tile. A live caster supplies the
    // interpolated position of that same sprite origin. Artwork here is authored in world pixels.
    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = DefaultSize, uint seed = 0, uint field = 0)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Wizard.DrawFrame;
        if (duration <= 0 || now < start || now - start >= duration) return;
        float progress = (now - start) / (float)duration;
        float time = (now - start) / 1000f;
        float strength = visual.Phase switch
        {
            SpellVisualPhase.Aura or SpellVisualPhase.Field or SpellVisualPhase.Hit => 1,
            SpellVisualPhase.Prepare => tuning.PrepareFactor + tuning.PrepareFactor2 * Ease(progress * tuning.PrepareValue),
            _ when visual.Spell == Spell.ThunderBolt => Envelope(progress, tuning.EnvelopeFadeStart, tuning.EnvelopeTail),
            _ when visual.Spell is Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or
                Spell.ElectricShock or Spell.FlameField => Envelope(progress, tuning.EnvelopeFadeStart2, tuning.EnvelopeTail2),
            _ => Envelope(progress)
        };
        var frame = new Frame(pool, feet, now, strength, size, seed ^ (uint)visual.Spell * 17191, field);
        bool prepare = visual.Phase == SpellVisualPhase.Prepare;

        if (visual.Phase == SpellVisualPhase.Contact)
        {
            if (visual.Spell is Spell.IceThrust or Spell.IceStorm)
            {
                // Ice Storm's contact must retain the runic seal and swirling
                // wind/ice; SnowContact is the separate Ice Thrust impact.
                if (visual.Spell == Spell.IceStorm)
                    IceStorm(frame, feet, progress, prepare: false);
                else
                    SnowContact(frame, feet, progress, true);
                return;
            }
            bool cold = visual.Spell == Spell.ThunderStorm;
            frame.Sprite(feet - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, cold ? IceWhite : Hot, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight);
            frame.Sparks(feet - Vector2.UnitY * tuning.SparksOffset, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, cold ? Ice : Fire, rise: tuning.SparksRise);
            return;
        }
        if (visual.Phase == SpellVisualPhase.Burn)
        {
            Flames(frame, feet, tuning.Width, tuning.Height, time, tuning.Alpha);
            return;
        }
        if (visual.Phase == SpellVisualPhase.Ward && visual.Spell != Spell.MagicShield)
        {
            Shield(frame, feet, SpellVisualPhase.Hit, progress, now);
            return;
        }

        switch (visual.Spell)
        {
            case Spell.FireBall:
                if (prepare) FireCharge(frame, feet, progress, facing);
                else FireBurst(frame, feet + new Vector2(tuning.FireBallX, tuning.FireBallY), progress, tuning.FireBallDiameter);
                break;
            case Spell.GreatFireBall:
            case Spell.FireBounce:
            case Spell.MeteorShower:
                int accent = visual.Spell == Spell.FireBounce ? tuning.MeteorShowerAccentColour : visual.Spell == Spell.MeteorShower ? tuning.MeteorShowerAccentColour2 : Gold;
                if (prepare) GreatFireCharge(frame, feet, progress, accent);
                else SearingImpact(frame, feet - Vector2.UnitY * tuning.MeteorShowerOffset, progress, accent);
                break;
            case Spell.Repulsion:
                Repel(frame, feet, progress);
                break;
            case Spell.ElectricShock:
                ElectricWrap(frame, feet, progress, time, prepare);
                break;
            case Spell.HellFire:
                if (prepare) HellfireCharge(frame, feet, progress);
                else FireBurst(frame, feet + new Vector2(tuning.HellFireX, tuning.HellFireY), progress, tuning.HellFireDiameter, fierce: true);
                break;
            case Spell.ThunderBolt:
                ThunderStrike(frame, feet, progress, time, prepare);
                break;
            case Spell.Teleport:
                if (prepare) BlinkRing(frame, feet, progress, descending: false);
                else TeleportLight(frame, feet, progress, visual.Phase == SpellVisualPhase.Departure);
                break;
            case Spell.FireBang:
                FireBang(frame, feet, progress, time, prepare);
                break;
            case Spell.FireWall:
                if (prepare) FireWallCharge(frame, feet, progress);
                else FireWall(frame, feet, time);
                break;
            case Spell.Lightning:
                Lightning(frame, feet - Vector2.UnitY * tuning.LightningSource, feet - Vector2.UnitY * tuning.LightningDestination + facing * tuning.LightningDestination2,
                    time, new(feet.Y, feet.Y + facing.Y * tuning.LightningY));
                break;
            case Spell.FrostCrunch:
                if (prepare) FrostCharge(frame, feet, progress);
                else FrostFireImpact(frame, feet - Vector2.UnitY * tuning.FrostCrunchOffset, progress);
                break;
            case Spell.ThunderStorm:
                ThunderStorm(frame, feet, progress, time);
                break;
            case Spell.MagicShield:
                Shield(frame, feet, visual.Phase, progress, now);
                break;
            case Spell.TurnUndead:
                TurnUndead(frame, feet, progress, prepare);
                break;
            case Spell.Vampirism:
                Vampirism(frame, feet, visual.Phase, progress);
                break;
            case Spell.IceStorm:
                IceStorm(frame, feet, progress, prepare);
                break;
            case Spell.FlameDisruptor:
                Disruptor(frame, feet, progress, time, prepare);
                break;
            case Spell.Mirroring:
                Mirroring(frame, feet, visual.Phase, progress);
                break;
            case Spell.FlameField:
                FlameField(frame, feet, progress, time);
                break;
            case Spell.Blizzard:
                if (prepare) IceBreak(frame, feet - Vector2.UnitY * tuning.BlizzardOffset, progress, tuning.BlizzardRadius, tuning.BlizzardCount);
                else Blizzard(frame, feet, time);
                break;
            case Spell.MagicBooster:
                Booster(frame, feet, visual.Phase, progress, now);
                break;
            case Spell.MeteorStrike:
                if (prepare) MeteorCharge(frame, feet, progress);
                else if (visual.Phase == SpellVisualPhase.Field) MeteorStrike(frame, feet, time);
                else MeteorSeal(frame, feet, progress);
                break;
            case Spell.IceThrust:
                IceThrust(frame, feet, facing, progress, visual.ExtraReach);
                break;
            case Spell.StormEscape:
                if (prepare) StormEscape(frame, feet, progress, time);
                else StormArrival(frame, feet, progress, time);
                break;
            case Spell.Blink:
                BlinkRing(frame, feet, progress, descending: !prepare);
                break;
        }
    }

    internal static bool DrawProjectile(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frameIndex, int remaining, bool wake, float size)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Current.Wizard.DrawProjectile;
        if (spell is not (Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch)) return false;
        if (remaining <= 0) return true;

        var frame = new Frame(pool, head, now, 1, size, (uint)spell, depthY: head.Y + tuning.DepthY);
        float angle = MathF.Atan2(heading.Y, heading.X);
        Vector2 side = new(-heading.Y, heading.X);
        bool frost = spell == Spell.FrostCrunch;
        int rim = frost ? tuning.RimColour : spell == Spell.FireBounce ? tuning.RimColour2 : Fire;
        int mantle = frost ? tuning.MantleColour : spell == Spell.MeteorShower ? tuning.MantleColour2 : Gold;
        int core = frost ? IceWhite : Hot;
        float phase = frameIndex * tuning.Phase;

        // The projectile core is made of individual directional flame tongues,
        // never a single 100px orb. Frost Crunch uses the same cold-fire flow.
        for (int i = 0; i < tuning.UserFlameCount; i++)
        {
            int seed = i * 37 + 67;
            float age = Fract(frameIndex * (tuning.Age + frame.Unit(seed + 7) * tuning.Age2) +
                frame.Unit(seed + 13));
            float along = tuning.Along + age * (tuning.Along2 + frame.Unit(seed + 19) * tuning.Along3);
            Vector2 point = head - heading * along +
                side * ((frame.Unit(seed + 23) - tuning.Point) * (tuning.Point2 + age * tuning.Point3));
            float alive = MathF.Sin(age * MathF.PI);
            frame.Sprite(point, tuning.UserFlameWidth + frame.Unit(seed + 29) * tuning.UserFlameWidth2,
                tuning.UserFlameHeight + frame.Unit(seed + 31) * tuning.UserFlameHeight2,
                tuning.UserFlameShape, i % 5 == 0 ? core : i % 3 == 0 ? mantle : rim,
                tuning.UserFlameAlpha * alive, i % 6 == 0 ? tuning.UserFlameLayer : tuning.UserFlameLayer2,
                angle - MathF.PI / 2 + (frame.Unit(seed + 37) - tuning.UserFlameRotation) * tuning.UserFlameRotation2,
                light: i % 17 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - heading * tuning.SparkOffset, tuning.SparkWidth + frame.Unit(seed + 41) * tuning.SparkWidth2,
                    tuning.SparkHeight + frame.Unit(seed + 43) * tuning.SparkHeight2,
                    frost ? tuning.SparkShape : tuning.SparkShape2,
                    i % 2 == 0 ? core : mantle, tuning.SparkAlpha * alive, tuning.SparkLayer);
            if (i % 7 == 0)
                frame.Clip(point - heading * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth + frame.Unit(seed + 47) * tuning.SmokeSequenceWidth2,
                    tuning.SmokeSequenceHeight + frame.Unit(seed + 49) * tuning.SmokeSequenceHeight2,
                    tuning.SmokeSequenceShape, phase + frame.Unit(seed + 51),
                    frost ? tuning.SmokeSequenceColour : tuning.SmokeSequenceColour2,
                    tuning.SmokeSequenceAlpha * alive, tuning.SmokeSequenceLayer);
        }
        frame.Sprite(head, tuning.UserFlameWidth3, tuning.UserFlameHeight3, tuning.UserFlameShape2, core, tuning.UserFlameAlpha2,
            tuning.UserFlameLayer3, angle - MathF.PI / 2, light: tuning.UserFlameLight);
        if (frost)
        {
            // The moving projectile needs a luminous spherical icefire head
            // in front of its existing cool-flame trail and snow wake.
            // Small independent textures overlap; no giant opaque orb.
            for (int i = 0; i < tuning.FrostCoreCount; i++)
            {
                int seed = 839 + i * 31;
                float theta = frame.Unit(seed) * MathF.Tau +
                    phase * (tuning.Theta + frame.Unit(seed + 3) * tuning.Theta2);
                float radius = tuning.Radius + frame.Unit(seed + 5) * tuning.Radius2;
                Vector2 point = head + Ellipse(theta, radius, radius * tuning.EllipseY);
                float pulse = tuning.Pulse + tuning.Pulse2 *
                    MathF.Sin(phase * tuning.Pulse3 + frame.Unit(seed + 9) * MathF.Tau);
                frame.Sprite(point, tuning.FrostCoreWidth + frame.Unit(seed + 11) * tuning.FrostCoreWidth2,
                    tuning.FrostCoreHeight + frame.Unit(seed + 13) * tuning.FrostCoreHeight2,
                    i % 3 == 0 ? tuning.FrostCoreShape : tuning.FrostCoreShape2,
                    i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice,
                    tuning.FrostCoreAlpha * pulse,
                    i % 3 == 0 ? tuning.FrostCoreLayer : tuning.FrostCoreLayer2,
                    angle - MathF.PI / 2, light: i % 11 == 0);
                if (i % 4 == 0)
                    frame.Sprite(point, tuning.FrostCoreWidth3 + frame.Unit(seed + 15) * tuning.FrostCoreWidth4,
                        tuning.FrostCoreHeight3 + frame.Unit(seed + 17) * tuning.FrostCoreHeight4,
                        tuning.FrostCoreShape3, IceWhite, tuning.FrostCoreAlpha2 * pulse,
                        tuning.FrostCoreLayer3, light: i % 8 == 0);
            }
            frame.Sprite(head, tuning.FrostCoreWidth5, tuning.FrostCoreHeight5, tuning.FrostCoreShape4, PaleBlue,
                tuning.FrostCoreAlpha3, tuning.FrostCoreLayer4, light: tuning.FrostCoreLight);
            frame.Sprite(head - Vector2.UnitY * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape,
                IceWhite, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight);
            SnowballWake(frame, pool, head, heading, now, frameIndex, remaining, wake);
            return true;
        }
        if (wake)
        {
            Vector2 drift = side * (frameIndex % 5 - tuning.Drift) * tuning.Drift2;
            pool.TryAdd(new SpellParticle(head - heading * tuning.UserEmberOrigin + drift,
                -heading * tuning.UserEmberVelocity + drift, now, Math.Min(remaining, tuning.Val2), tuning.UserEmberSize,
                Tint(Gold, tuning.Opacity), tuning.UserEmberShape, 0,
                Rotation: angle, Growth: tuning.UserEmberGrowth, Layer: tuning.UserEmberLayer,
                CastsLight: tuning.UserEmberCastsLight, DepthY: head.Y + tuning.UserEmberDepthY));
        }
        return true;
    }

    private static void Shield(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress, long now)
    {
        var tuning = SpellVfxAssets.Current.Wizard.Shield;
        float hit = phase is SpellVisualPhase.Hit or SpellVisualPhase.Ward ? MathF.Sin(progress * MathF.PI) : 0;
        bool prepare = phase == SpellVisualPhase.Prepare;
        float unfold = prepare ? Ease(progress * tuning.Unfold) : 1;
        float shimmer = tuning.Shimmer + tuning.Shimmer2 * MathF.Sin(now % 1200 * MathF.Tau / tuning.ShimmerDivisor);
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + tuning.Centre2 * (1 - unfold));
        float width = tuning.Width * (tuning.Width2 + tuning.Width3 * unfold) + tuning.Width4 * hit;
        float height = tuning.Height * (tuning.Height2 + tuning.Height3 * unfold) + tuning.Height4 * hit;
        frame.Sprite(centre, width, height, tuning.WardBackShape, tuning.WardBackColour, tuning.WardBackAlpha, tuning.WardBackLayer);
        frame.Sprite(centre, width, height, tuning.WardFrontShape, tuning.WardFrontColour, shimmer, light: tuning.WardFrontLight);
        frame.Sprite(centre + Vector2.UnitY * tuning.WispOffset * unfold, width * tuning.WispWidth, height * tuning.WispHeight, tuning.WispShape, tuning.WispColour,
            tuning.WispAlpha + tuning.WispAlpha2 * hit, tuning.WispLayer, light: tuning.WispLight);
        frame.Sprite(centre + new Vector2(tuning.X, tuning.Y) * unfold, width * tuning.WispWidth2, height * tuning.WispHeight2, tuning.WispShape2, tuning.WispColour2,
            tuning.WispAlpha3 * unfold, tuning.WispLayer2);
        frame.Sprite(centre + new Vector2(tuning.X2, tuning.Y2) * unfold, tuning.FlashWidth + tuning.FlashWidth2 * hit, tuning.FlashHeight + tuning.FlashHeight2 * hit, tuning.FlashShape, Hot,
            tuning.FlashAlpha + tuning.FlashAlpha2 * hit, tuning.FlashLayer, tuning.FlashRotation);
        frame.Sprite(feet - Vector2.UnitY * tuning.WispOffset2, width * tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape3, tuning.WispColour3, tuning.WispAlpha4 * unfold, tuning.WispLayer3);
        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float orbit = now % 1800 / tuning.OrbitDivisor * MathF.Tau + i * MathF.Tau / tuning.OrbitDivisor2;
            Vector2 mote = centre + Ellipse(orbit, width * tuning.EllipseX, height * tuning.EllipseY);
            frame.Sprite(mote, tuning.SparkWidth, tuning.SparkHeight, tuning.SparkShape, i % 3 == 0 ? tuning.SparkColour : Gold, tuning.SparkAlpha * unfold,
                Depth(orbit), orbit);
        }
        if (hit > 0)
        {
            frame.Sprite(centre + new Vector2(tuning.X3, tuning.Y3), width * tuning.WardFrontWidth, height * tuning.WardFrontHeight, tuning.WardFrontShape2,
                Hot, hit * tuning.WardFrontAlpha, tuning.WardFrontLayer, MathF.Sin(progress * tuning.X4) * tuning.WardFrontRotation);
            frame.Sprite(centre, tuning.OrbWidth + hit * tuning.OrbWidth2, tuning.OrbHeight + hit * tuning.OrbHeight2, tuning.OrbShape, Hot, hit * tuning.OrbAlpha, tuning.OrbLayer);
            frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Gold, rise: tuning.SparksRise);
        }
    }

    private static void BlinkRing(Frame frame, Vector2 feet, float progress, bool descending)
    {
        var tuning = SpellVfxAssets.Current.Wizard.BlinkRing;
        float lift = tuning.Lift + tuning.Lift2 * (descending ? 1 - Ease(progress * tuning.Lift3) : Ease(progress * tuning.Lift4));
        Vector2 centre = feet - Vector2.UnitY * lift;
        float width = tuning.Width + tuning.Width2 * MathF.Sin(progress * MathF.PI);
        frame.Orbit(centre, width / 2, tuning.OrbitY, 0, MathF.Tau, Blue, tuning.OrbitWidth, tuning.OrbitAlpha);
        frame.Orbit(centre, width * tuning.OrbitX, tuning.OrbitY2, tuning.OrbitAngle, tuning.OrbitSweep, PaleBlue, tuning.OrbitWidth2, tuning.OrbitAlpha2);
        frame.Sprite(centre, width * tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Blue, tuning.OrbAlpha, light: tuning.OrbLight);
        frame.Clip(centre + Vector2.UnitY * tuning.SmokeSequenceOffset, width * tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase,
            tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer);
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float angle = i * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle;
            Vector2 root = centre + Ellipse(angle, width * tuning.EllipseX, tuning.EllipseY);
            Vector2 end = root - new Vector2(MathF.Cos(angle) * tuning.X, tuning.Y + frame.Unit(i + 33) * tuning.Y2);
            frame.Line(root, end, tuning.LineWidth, i % 3 == 0 ? White : Blue, tuning.LineAlpha, Depth(angle));
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, PaleBlue, rise: tuning.SparksRise);
    }

    private static void TeleportLight(Frame frame, Vector2 feet, float progress, bool departure)
    {
        var tuning = SpellVfxAssets.Current.Wizard.TeleportLight;
        float spread = departure ? 1 - progress * tuning.Spread : tuning.Spread2 + tuning.Spread3 * Ease(progress * tuning.Spread4);
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + tuning.Centre2 * MathF.Sin(progress * MathF.PI));
        float pulse = tuning.Pulse + tuning.Pulse2 * MathF.Sin(progress * MathF.PI);
        frame.Sprite(centre, tuning.OrbWidth * spread, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, light: tuning.OrbLight);
        frame.Sprite(centre, tuning.OrbWidth2 * spread, tuning.OrbHeight2, tuning.OrbShape2, White, pulse, tuning.OrbLayer, light: tuning.OrbLight2);
        frame.Sprite(feet, tuning.OrbWidth3 * spread, tuning.OrbHeight3 * spread, tuning.OrbShape3, tuning.OrbColour2, tuning.OrbAlpha2, tuning.OrbLayer2, light: tuning.OrbLight3);
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float x = (frame.Unit(i) - tuning.X) * tuning.X2 * spread;
            float depth = frame.Unit(i + 20) * tuning.Depth - tuning.Depth2;
            float height = tuning.Height + frame.Unit(i + 30) * tuning.Height2;
            float y = departure ? depth - tuning.Y * progress : depth - tuning.Y2 * (1 - progress);
            Vector2 end = feet + new Vector2(x, y);
            frame.Line(end - Vector2.UnitY * height, end, tuning.LineWidth + frame.Unit(i + 40) * tuning.LineWidth2, i % 3 == 0 ? PaleBlue : White,
                tuning.LineAlpha, depth < 0 ? tuning.LineLayer : tuning.LineLayer2);
        }
    }

    private static void TurnUndead(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Current.Wizard.TurnUndead;
        Vector2 centre = feet - Vector2.UnitY * (prepare ? tuning.PrepareCentre : tuning.ReleaseCentre);
        if (prepare)
        {
            float size = tuning.Size + progress * tuning.Size2;
            frame.Sprite(centre, size, size, tuning.OrbShape, Ember, tuning.OrbAlpha, light: tuning.OrbLight);
            frame.Sprite(centre, size * tuning.GlyphWidth, size * tuning.GlyphHeight, tuning.GlyphShape, Fire, tuning.GlyphAlpha, rotation: progress * tuning.GlyphRotation);
            frame.Sprite(centre, size * tuning.HaloWidth, size * tuning.HaloHeight, tuning.HaloShape, Gold, tuning.HaloAlpha);
            frame.Sprite(centre, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Hot, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight);
            frame.Orbit(centre, tuning.OrbitX, tuning.OrbitY, -progress * tuning.OrbitAngle, tuning.OrbitSweep, tuning.OrbitColour, tuning.OrbitWidth, tuning.OrbitAlpha);
            frame.Line(centre - Vector2.UnitX * tuning.LineStart, centre + Vector2.UnitX * tuning.LineEnd, tuning.LineWidth, PaleBlue, tuning.LineAlpha, tuning.LineLayer);
            frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Fire);
            return;
        }

        int count = tuning.Count;
        float radius = tuning.Radius + progress * tuning.Radius2;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathF.Tau / count + progress * tuning.Angle;
            Vector2 point = centre + Ellipse(angle, radius, radius * tuning.EllipseY) - Vector2.UnitY * (frame.Unit(i + 7) - tuning.Point) * tuning.Point2;
            int tint = i % 3 == 0 ? Fire : PaleBlue;
            float width = tuning.Width + tuning.Width2 * frame.Unit(i);
            frame.Sprite(point, width * tuning.OrbWidth, width * tuning.OrbHeight, tuning.OrbShape2, tint, tuning.OrbAlpha2, Depth(angle), light: tuning.OrbLight2);
            frame.Sprite(point, width, width, tuning.OrbShape3, tint == Fire ? Hot : White, tuning.OrbAlpha3, tuning.OrbLayer);
            frame.Sprite(point, width * tuning.FlashWidth2, width * tuning.FlashHeight2, tuning.FlashShape2, White, tuning.FlashAlpha2, tuning.FlashLayer2, angle);
            frame.Line(point, point + Ellipse(angle - tuning.EllipseAngle, tuning.EllipseX, tuning.EllipseY2), tuning.LineWidth2, tint, tuning.LineAlpha2, Depth(angle));
        }
        frame.Sparks(centre, tuning.SparksCount2, tuning.SparksRx2, tuning.SparksRy2, progress, Gold);
        frame.Clip(centre - Vector2.UnitY * (progress * tuning.SmokeSequenceOffset), tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase,
            tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * progress, tuning.SmokeSequenceLayer);
    }

    private static void Vampirism(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress)
    {
        var tuning = SpellVfxAssets.Current.Wizard.Vampirism;
        Vector2 centre = feet - Vector2.UnitY * (phase == SpellVisualPhase.Prepare ? tuning.Centre : tuning.Centre2);
        if (phase == SpellVisualPhase.Return)
        {
            frame.Sprite(feet - Vector2.UnitY * tuning.OrbOffset, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight);
            for (int i = 0; i < tuning.ParticleCount; i++)
            {
                float angle = i * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle;
                Vector2 point = feet + Ellipse(angle, tuning.EllipseX + tuning.EllipseX2 * (1 - progress), tuning.EllipseY);
                frame.Line(point - new Vector2(tuning.X, tuning.Y * (1 - progress) + tuning.Y2), point, tuning.LineWidth, tuning.LineColour, tuning.LineAlpha, Depth(angle), light: i % 2 == 0);
            }
            frame.Sprite(centre - Vector2.UnitY * tuning.OrbOffset2, tuning.OrbWidth2, tuning.OrbHeight2, tuning.OrbShape2, tuning.OrbColour2, tuning.OrbAlpha2, tuning.OrbLayer2, light: tuning.OrbLight2);
            frame.Sprite(centre + Vector2.UnitY * tuning.FlameOffset, tuning.FlameWidth, tuning.FlameHeight, tuning.FlameShape, tuning.FlameColour, tuning.FlameAlpha, rotation: tuning.FlameRotation, light: tuning.FlameLight);
            return;
        }

        float spin = progress * (phase == SpellVisualPhase.Prepare ? tuning.Spin : tuning.Spin2);
        frame.Clip(centre, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer);
        for (int i = 0; i < tuning.FlameTongueCount; i++)
        {
            float angle = spin + i * MathF.Tau / tuning.AngleDivisor2;
            Vector2 point = centre + Ellipse(angle, tuning.EllipseX3 + frame.Unit(i + 8) * tuning.EllipseX4, tuning.EllipseY2 + i * tuning.EllipseY3);
            Vector2 wing = Ellipse(angle + tuning.EllipseAngle, tuning.EllipseX5, tuning.EllipseY4);
            frame.Sprite(point, tuning.FlameTongueWidth, tuning.FlameTongueHeight, tuning.FlameTongueShape, tuning.FlameTongueColour, tuning.FlameTongueAlpha, Depth(angle), angle + tuning.FlameTongueRotation, light: tuning.FlameTongueLight);
            frame.Line(point, point + wing, tuning.LineWidth2, i % 2 == 0 ? tuning.LineColour2 : tuning.LineColour3, tuning.LineAlpha2, Depth(angle));
            frame.Line(point + wing * tuning.LineStart, point + wing + Ellipse(angle + tuning.EllipseAngle2, tuning.EllipseX6, tuning.EllipseY5), tuning.LineWidth3, tuning.LineColour4, tuning.LineAlpha3, Depth(angle));
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, tuning.SparksColour, rise: tuning.SparksRise);
    }

    private static void Mirroring(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress)
    {
        var tuning = SpellVfxAssets.Current.Wizard.Mirroring;
        bool fade = phase == SpellVisualPhase.Expire;
        bool prepare = phase == SpellVisualPhase.Prepare;
        float lift = prepare ? progress * tuning.Lift : tuning.Lift2 + tuning.Lift3 * progress;
        float spread = fade ? 1 - progress * tuning.Spread : tuning.Spread2 + progress * tuning.Spread3;
        Vector2 centre = feet - Vector2.UnitY * lift;
        frame.Sprite(centre, tuning.SmokeWidth * spread, tuning.SmokeHeight, tuning.SmokeShape, Ice, tuning.SmokeAlpha, tuning.SmokeLayer);
        if (prepare)
        {
            for (int i = 0; i < tuning.ParticleCount; i++)
                frame.Orbit(centre - Vector2.UnitY * i * tuning.OrbitOffset, (tuning.OrbitX + i * tuning.OrbitX2) * spread, tuning.OrbitY,
                    progress * tuning.OrbitAngle + i * tuning.OrbitAngle2, tuning.OrbitSweep, i == 0 ? PaleBlue : Blue, tuning.OrbitWidth, tuning.OrbitAlpha);
            frame.Sprite(centre, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Blue, tuning.OrbAlpha, light: tuning.OrbLight);
        }
        else
        {
            frame.Sprite(feet - Vector2.UnitY * tuning.SpiritFigureOffset, tuning.SpiritFigureWidth * spread, tuning.SpiritFigureHeight, tuning.SpiritFigureShape, tuning.SpiritFigureColour,
                fade ? tuning.SpiritFigureAlpha : tuning.SpiritFigureAlpha2, rotation: tuning.SpiritFigureRotation + progress * tuning.SpiritFigureRotation2);
            for (int i = 0; i < tuning.SmokeCount; i++)
            {
                float height = (i + tuning.Height) / tuning.HeightDivisor;
                Vector2 point = feet + new Vector2((frame.Unit(i + 15) - tuning.X) * tuning.X2 * spread, tuning.Y - height * tuning.Y2 - progress * tuning.Y3);
                frame.Sprite(point, (tuning.SmokeWidth2 + tuning.SmokeWidth3 * frame.Unit(i)) * spread, tuning.SmokeHeight2 + frame.Unit(i + 7) * tuning.SmokeHeight3,
                    tuning.SmokeShape2, i % 2 == 0 ? PaleBlue : IceWhite, tuning.SmokeAlpha2, rotation: progress + i, light: i % 3 == 0);
            }
            frame.Sprite(feet - Vector2.UnitY * tuning.OrbOffset, tuning.OrbWidth2 * spread, tuning.OrbHeight2, tuning.OrbShape2, White, fade ? tuning.OrbAlpha2 : tuning.OrbAlpha3, tuning.OrbLayer, light: tuning.OrbLight2);
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx * spread, tuning.SparksRy, progress, IceWhite, rise: fade ? tuning.SparksRise : tuning.SparksRise2);
    }

    private static void Booster(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress, long now)
    {
        var tuning = SpellVfxAssets.Current.Wizard.Booster;
        bool active = phase == SpellVisualPhase.Aura;
        Vector2 eye = feet - Vector2.UnitY * (tuning.Eye + (active ? 0 : tuning.Eye2 * MathF.Sin(progress * MathF.PI)));
        frame.Sprite(eye, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, light: tuning.OrbLight);
        frame.Sprite(eye, tuning.SigilEyeWidth, tuning.SigilEyeHeight, tuning.SigilEyeShape, Fire, tuning.SigilEyeAlpha);
        frame.Sprite(eye, tuning.SigilEyeWidth2, tuning.SigilEyeHeight2, tuning.SigilEyeShape2, Gold, tuning.SigilEyeAlpha2 + tuning.SigilEyeAlpha3 * MathF.Sin(now % 950 * MathF.Tau / tuning.XDivisor), tuning.SigilEyeLayer);
        frame.Sprite(eye, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Hot, tuning.FlashAlpha, tuning.FlashLayer);
        frame.Sparks(eye, active ? tuning.SparksCount : tuning.SparksCount2, tuning.SparksRx, tuning.SparksRy, now % 950 / tuning.SparksProgressDivisor, Gold, tuning.SparksRise);
        if (!active)
        {
            frame.Sprite(feet - Vector2.UnitY * tuning.FlameTongueOffset, tuning.FlameTongueWidth, tuning.FlameTongueHeight, tuning.FlameTongueShape, Ember, tuning.FlameTongueAlpha, light: tuning.FlameTongueLight);
            frame.Line(eye, feet - Vector2.UnitY * tuning.LineEnd, tuning.LineWidth, Fire, tuning.LineAlpha, light: tuning.LineLight);
            for (int i = 0; i < tuning.FeatherCount; i++)
            {
                float offset = (frame.Unit(i) - tuning.Offset) * tuning.Offset2;
                float y = tuning.Y + tuning.Y2 * Fract(progress + frame.Unit(i + 11) * tuning.Y3);
                frame.Sprite(feet + new Vector2(offset, y), tuning.FeatherWidth, tuning.FeatherHeight, tuning.FeatherShape, Fire, tuning.FeatherAlpha, tuning.FeatherLayer, offset * tuning.FeatherRotation);
            }
        }
    }
}
