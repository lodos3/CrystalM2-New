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
            return SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.ArcTiming).LightningDuration;
        }
    }

    internal static int RelayDuration(Spell spell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var timing = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.ArcTiming);
        return spell == Spell.Vampirism ? timing.VampirismDurationMs : timing.RelayDurationMs;
    }

    private const float FloorAspect = 2f / 3f;
    private static int Gold => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).Gold;
    private static int Fire => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).Fire;
    private static int Ember => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).Ember;
    private static int Hot => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).Hot;
    private static int Blue => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).Blue;
    private static int PaleBlue => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).PaleBlue;
    private static int White => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).White;
    private static int Ice => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).Ice;
    private static int IceWhite => SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Palette).IceWhite;

    internal static bool HasArt(Spell spell) => SpellCurriculum.Owns(MirClass.Wizard, spell);

    internal static int PacketDuration(SpellVisual visual)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Wizard.PacketDuration;
        var fields = SpellVfxAssets.Fields("Wizard", "PacketDuration");
        return visual.Phase switch
        {
            SpellVisualPhase.Contact => fields.Read(tuning.ContactDurationMs),
            SpellVisualPhase.Ward => fields.Read(tuning.WardDurationMs),
            SpellVisualPhase.Burn => fields.Read(tuning.BurnDurationMs),
            _ => visual.Spell == Spell.IceThrust ? fields.Read(tuning.IceThrustDurationMs) : fields.Read(tuning.DefaultDurationMs)
        };
    }

    // The legacy offsets use the upper left of a 48 × 32 tile. A live caster supplies the
    // interpolated position of that same sprite origin. Artwork here is authored in world pixels.
    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = DefaultSize, uint seed = 0, uint field = 0)
    {
        using var emitterTime = SpellVfxAssets.AtProgress((now - start) / (float)Math.Max(1, duration));
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Wizard.DrawFrame;
        var fields = SpellVfxAssets.Fields("Wizard", "DrawFrame");
        if (duration <= 0 || now < start || now - start >= duration) return;
        float progress = (now - start) / (float)duration;
        float time = (now - start) / 1000f;
        float strength = visual.Phase switch
        {
            SpellVisualPhase.Aura or SpellVisualPhase.Field or SpellVisualPhase.Hit => 1,
            SpellVisualPhase.Prepare => fields.Read(tuning.PrepareFactor) + fields.Read(tuning.PrepareFactor2) * Ease(progress * fields.Read(tuning.PrepareValue)),
            _ when visual.Spell == Spell.ThunderBolt => Envelope(progress, fields.Read(tuning.EnvelopeFadeStart), fields.Read(tuning.EnvelopeTail)),
            _ when visual.Spell is Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or
                Spell.ElectricShock or Spell.FlameField => Envelope(progress, fields.Read(tuning.EnvelopeFadeStart2), fields.Read(tuning.EnvelopeTail2)),
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
            frame.Sprite(feet - Vector2.UnitY * fields.Read(tuning.FlashOffset), fields.Read(tuning.FlashWidth), fields.Read(tuning.FlashHeight), fields.Read(tuning.FlashShape), cold ? IceWhite : Hot, fields.Read(tuning.FlashAlpha), fields.Read(tuning.FlashLayer), light: fields.Read(tuning.FlashLight), emitter: "WizardSpellEffects.DrawFrame.1");
            frame.Sparks(feet - Vector2.UnitY * fields.Read(tuning.SparksOffset), fields.Read(tuning.SparksCount), fields.Read(tuning.SparksRx), fields.Read(tuning.SparksRy), progress, cold ? Ice : Fire, rise: fields.Read(tuning.SparksRise), emitter: "WizardSpellEffects.DrawFrame.2");
            return;
        }
        if (visual.Phase == SpellVisualPhase.Burn)
        {
            Flames(frame, feet, fields.Read(tuning.Width), fields.Read(tuning.Height), time, fields.Read(tuning.Alpha));
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
                else FireBurst(frame, feet + new Vector2(fields.Read(tuning.FireBallX), fields.Read(tuning.FireBallY)), progress, fields.Read(tuning.FireBallDiameter));
                break;
            case Spell.GreatFireBall:
            case Spell.FireBounce:
            case Spell.MeteorShower:
                int accent = visual.Spell == Spell.FireBounce ? fields.Read(tuning.MeteorShowerAccentColour) : visual.Spell == Spell.MeteorShower ? fields.Read(tuning.MeteorShowerAccentColour2) : Gold;
                if (prepare) GreatFireCharge(frame, feet, progress, accent);
                else SearingImpact(frame, feet - Vector2.UnitY * fields.Read(tuning.MeteorShowerOffset), progress, accent);
                break;
            case Spell.Repulsion:
                Repel(frame, feet, progress);
                break;
            case Spell.ElectricShock:
                ElectricWrap(frame, feet, progress, time, prepare);
                break;
            case Spell.HellFire:
                if (prepare) HellfireCharge(frame, feet, progress);
                else FireBurst(frame, feet + new Vector2(fields.Read(tuning.HellFireX), fields.Read(tuning.HellFireY)), progress, fields.Read(tuning.HellFireDiameter), fierce: true);
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
                Lightning(frame, feet - Vector2.UnitY * fields.Read(tuning.LightningSource), feet - Vector2.UnitY * fields.Read(tuning.LightningDestination) + facing * fields.Read(tuning.LightningDestination2),
                    time, new(feet.Y, feet.Y + facing.Y * fields.Read(tuning.LightningY)));
                break;
            case Spell.FrostCrunch:
                if (prepare) FrostCharge(frame, feet, progress);
                else FrostFireImpact(frame, feet - Vector2.UnitY * fields.Read(tuning.FrostCrunchOffset), progress);
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
                if (prepare) IceBreak(frame, feet - Vector2.UnitY * fields.Read(tuning.BlizzardOffset), progress, fields.Read(tuning.BlizzardRadius), fields.Read(tuning.BlizzardCount));
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
        var fields = SpellVfxAssets.Fields("Wizard", "DrawProjectile");
        if (spell is not (Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch)) return false;
        if (remaining <= 0) return true;

        var frame = new Frame(pool, head, now, .34f, size, (uint)spell, depthY: head.Y + fields.Read(tuning.DepthY));
        float angle = MathF.Atan2(heading.Y, heading.X);
        Vector2 side = new(-heading.Y, heading.X);
        bool frost = spell == Spell.FrostCrunch;
        int rim = frost ? fields.Read(tuning.RimColour) : spell == Spell.FireBounce ? fields.Read(tuning.RimColour2) : Fire;
        int mantle = frost ? fields.Read(tuning.MantleColour) : spell == Spell.MeteorShower ? fields.Read(tuning.MantleColour2) : Gold;
        int core = frost ? IceWhite : Hot;
        float phase = frameIndex * fields.Read(tuning.Phase);
        if (frost) frame.Clip(head, 22, 17, Shape.FireSequence, phase * .24f, Ice, .8f, Layer.EmissiveCore, angle);

        // The projectile core is made of individual directional flame tongues,
        // never a single 100px orb. Frost Crunch uses the same cold-fire flow.
        for (int i = 0; i < fields.Read(tuning.UserFlameCount); i++)
        {
            int seed = i * 37 + 67;
            float age = Fract(frameIndex * (fields.Read(tuning.Age) + frame.Unit(seed + 7) * fields.Read(tuning.Age2)) +
                frame.Unit(seed + 13));
            float along = fields.Read(tuning.Along) + age * (fields.Read(tuning.Along2) + frame.Unit(seed + 19) * fields.Read(tuning.Along3));
            Vector2 point = head - heading * along +
                side * ((frame.Unit(seed + 23) - fields.Read(tuning.Point)) * (fields.Read(tuning.Point2) + age * fields.Read(tuning.Point3)));
            float alive = MathF.Sin(age * MathF.PI);
            frame.Sprite(point, fields.Read(tuning.UserFlameWidth) + frame.Unit(seed + 29) * fields.Read(tuning.UserFlameWidth2),
                fields.Read(tuning.UserFlameHeight) + frame.Unit(seed + 31) * fields.Read(tuning.UserFlameHeight2),
                fields.Read(tuning.UserFlameShape), i % 5 == 0 ? core : i % 3 == 0 ? mantle : rim,
                fields.Read(tuning.UserFlameAlpha) * alive, i % 6 == 0 ? fields.Read(tuning.UserFlameLayer) : fields.Read(tuning.UserFlameLayer2),
                angle - MathF.PI / 2 + (frame.Unit(seed + 37) - fields.Read(tuning.UserFlameRotation)) * fields.Read(tuning.UserFlameRotation2),
                light: i % 17 == 0, emitter: "WizardSpellEffects.DrawProjectile.1");
            if (i % 4 == 0)
                frame.Sprite(point - heading * fields.Read(tuning.SparkOffset), fields.Read(tuning.SparkWidth) + frame.Unit(seed + 41) * fields.Read(tuning.SparkWidth2),
                    fields.Read(tuning.SparkHeight) + frame.Unit(seed + 43) * fields.Read(tuning.SparkHeight2),
                    frost ? fields.Read(tuning.SparkShape) : fields.Read(tuning.SparkShape2),
                    i % 2 == 0 ? core : mantle, fields.Read(tuning.SparkAlpha) * alive, fields.Read(tuning.SparkLayer), emitter: "WizardSpellEffects.DrawProjectile.2");
            if (i % 7 == 0)
                frame.Clip(point - heading * fields.Read(tuning.SmokeSequenceOffset), fields.Read(tuning.SmokeSequenceWidth) + frame.Unit(seed + 47) * fields.Read(tuning.SmokeSequenceWidth2),
                    fields.Read(tuning.SmokeSequenceHeight) + frame.Unit(seed + 49) * fields.Read(tuning.SmokeSequenceHeight2),
                    fields.Read(tuning.SmokeSequenceShape), phase + frame.Unit(seed + 51),
                    frost ? fields.Read(tuning.SmokeSequenceColour) : fields.Read(tuning.SmokeSequenceColour2),
                    fields.Read(tuning.SmokeSequenceAlpha) * alive, fields.Read(tuning.SmokeSequenceLayer), emitter: "WizardSpellEffects.DrawProjectile.3");
        }
        frame.Sprite(head, fields.Read(tuning.UserFlameWidth3), fields.Read(tuning.UserFlameHeight3), fields.Read(tuning.UserFlameShape2), core, fields.Read(tuning.UserFlameAlpha2),
            fields.Read(tuning.UserFlameLayer3), angle - MathF.PI / 2, light: fields.Read(tuning.UserFlameLight), emitter: "WizardSpellEffects.DrawProjectile.4");
        if (frost)
        {
            // The moving projectile needs a luminous spherical icefire head
            // in front of its existing cool-flame trail and snow wake.
            // Small independent textures overlap; no giant opaque orb.
            for (int i = 0; i < fields.Read(tuning.FrostCoreCount); i++)
            {
                int seed = 839 + i * 31;
                float theta = frame.Unit(seed) * MathF.Tau +
                    phase * (fields.Read(tuning.Theta) + frame.Unit(seed + 3) * fields.Read(tuning.Theta2));
                float radius = fields.Read(tuning.Radius) + frame.Unit(seed + 5) * fields.Read(tuning.Radius2);
                Vector2 point = head + Ellipse(theta, radius, radius * fields.Read(tuning.EllipseY));
                float pulse = fields.Read(tuning.Pulse) + fields.Read(tuning.Pulse2) *
                    MathF.Sin(phase * fields.Read(tuning.Pulse3) + frame.Unit(seed + 9) * MathF.Tau);
                frame.Sprite(point, fields.Read(tuning.FrostCoreWidth) + frame.Unit(seed + 11) * fields.Read(tuning.FrostCoreWidth2),
                    fields.Read(tuning.FrostCoreHeight) + frame.Unit(seed + 13) * fields.Read(tuning.FrostCoreHeight2),
                    i % 3 == 0 ? fields.Read(tuning.FrostCoreShape) : fields.Read(tuning.FrostCoreShape2),
                    i % 5 == 0 ? IceWhite : i % 2 == 0 ? PaleBlue : Ice,
                    fields.Read(tuning.FrostCoreAlpha) * pulse,
                    i % 3 == 0 ? fields.Read(tuning.FrostCoreLayer) : fields.Read(tuning.FrostCoreLayer2),
                    angle - MathF.PI / 2, light: i % 11 == 0, emitter: "WizardSpellEffects.DrawProjectile.5");
                if (i % 4 == 0)
                    frame.Sprite(point, fields.Read(tuning.FrostCoreWidth3) + frame.Unit(seed + 15) * fields.Read(tuning.FrostCoreWidth4),
                        fields.Read(tuning.FrostCoreHeight3) + frame.Unit(seed + 17) * fields.Read(tuning.FrostCoreHeight4),
                        fields.Read(tuning.FrostCoreShape3), IceWhite, fields.Read(tuning.FrostCoreAlpha2) * pulse,
                        fields.Read(tuning.FrostCoreLayer3), light: i % 8 == 0, emitter: "WizardSpellEffects.DrawProjectile.6");
            }
            frame.Sprite(head, fields.Read(tuning.FrostCoreWidth5), fields.Read(tuning.FrostCoreHeight5), fields.Read(tuning.FrostCoreShape4), PaleBlue,
                fields.Read(tuning.FrostCoreAlpha3), fields.Read(tuning.FrostCoreLayer4), light: fields.Read(tuning.FrostCoreLight), emitter: "WizardSpellEffects.DrawProjectile.7");
            frame.Sprite(head - Vector2.UnitY * fields.Read(tuning.FlashOffset), fields.Read(tuning.FlashWidth), fields.Read(tuning.FlashHeight), fields.Read(tuning.FlashShape),
                IceWhite, fields.Read(tuning.FlashAlpha), fields.Read(tuning.FlashLayer), light: fields.Read(tuning.FlashLight), emitter: "WizardSpellEffects.DrawProjectile.8");
            SnowballWake(frame, pool, head, heading, now, frameIndex, remaining, wake);
            return true;
        }
        if (wake)
        {
            Vector2 drift = side * (frameIndex % 5 - fields.Read(tuning.Drift)) * fields.Read(tuning.Drift2);
            SpellVfxAssets.Emit(pool, new SpellParticle(head - heading * fields.Read(tuning.UserEmberOrigin) + drift,
                -heading * fields.Read(tuning.UserEmberVelocity) + drift, now, Math.Min(remaining, fields.Read(tuning.Val2)), fields.Read(tuning.UserEmberSize),
                Tint(Gold, fields.Read(tuning.Opacity)), fields.Read(tuning.UserEmberShape), 0,
                Rotation: angle, Growth: fields.Read(tuning.UserEmberGrowth), Layer: fields.Read(tuning.UserEmberLayer),
                CastsLight: fields.Read(tuning.UserEmberCastsLight), DepthY: head.Y + fields.Read(tuning.UserEmberDepthY)), "WizardSpellEffects.DrawProjectile.9");
        }
        return true;
    }

    private static void Shield(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress, long now)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Shield);
        float hit = phase is SpellVisualPhase.Hit or SpellVisualPhase.Ward ? MathF.Sin(progress * MathF.PI) : 0;
        bool prepare = phase == SpellVisualPhase.Prepare;
        float unfold = prepare ? Ease(progress * tuning.Unfold) : 1;
        float shimmer = tuning.Shimmer + tuning.Shimmer2 * MathF.Sin(now % 1200 * MathF.Tau / tuning.ShimmerDivisor);
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + tuning.Centre2 * (1 - unfold));
        float width = tuning.Width * (tuning.Width2 + tuning.Width3 * unfold) + tuning.Width4 * hit;
        float height = tuning.Height * (tuning.Height2 + tuning.Height3 * unfold) + tuning.Height4 * hit;
        frame.Sprite(centre, width, height, tuning.WardBackShape, tuning.WardBackColour, tuning.WardBackAlpha, tuning.WardBackLayer, emitter: "WizardSpellEffects.Shield.1");
        frame.Sprite(centre, width, height, tuning.WardFrontShape, tuning.WardFrontColour, shimmer, light: tuning.WardFrontLight, emitter: "WizardSpellEffects.Shield.2");
        frame.Sprite(centre + Vector2.UnitY * tuning.WispOffset * unfold, width * tuning.WispWidth, height * tuning.WispHeight, tuning.WispShape, tuning.WispColour,
            tuning.WispAlpha + tuning.WispAlpha2 * hit, tuning.WispLayer, light: tuning.WispLight, emitter: "WizardSpellEffects.Shield.3");
        frame.Sprite(centre + new Vector2(tuning.X, tuning.Y) * unfold, width * tuning.WispWidth2, height * tuning.WispHeight2, tuning.WispShape2, tuning.WispColour2,
            tuning.WispAlpha3 * unfold, tuning.WispLayer2, emitter: "WizardSpellEffects.Shield.4");
        frame.Sprite(centre + new Vector2(tuning.X2, tuning.Y2) * unfold, tuning.FlashWidth + tuning.FlashWidth2 * hit, tuning.FlashHeight + tuning.FlashHeight2 * hit, tuning.FlashShape, Hot,
            tuning.FlashAlpha + tuning.FlashAlpha2 * hit, tuning.FlashLayer, tuning.FlashRotation, emitter: "WizardSpellEffects.Shield.5");
        frame.Sprite(feet - Vector2.UnitY * tuning.WispOffset2, width * tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape3, tuning.WispColour3, tuning.WispAlpha4 * unfold, tuning.WispLayer3, emitter: "WizardSpellEffects.Shield.6");
        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float orbit = now % 1800 / tuning.OrbitDivisor * MathF.Tau + i * MathF.Tau / tuning.OrbitDivisor2;
            Vector2 mote = centre + Ellipse(orbit, width * tuning.EllipseX, height * tuning.EllipseY);
            frame.Sprite(mote, tuning.SparkWidth, tuning.SparkHeight, tuning.SparkShape, i % 3 == 0 ? tuning.SparkColour : Gold, tuning.SparkAlpha * unfold,
                Depth(orbit), orbit, emitter: "WizardSpellEffects.Shield.7");
        }
        if (hit > 0)
        {
            frame.Sprite(centre + new Vector2(tuning.X3, tuning.Y3), width * tuning.WardFrontWidth, height * tuning.WardFrontHeight, tuning.WardFrontShape2,
                Hot, hit * tuning.WardFrontAlpha, tuning.WardFrontLayer, MathF.Sin(progress * tuning.X4) * tuning.WardFrontRotation, emitter: "WizardSpellEffects.Shield.8");
            frame.Sprite(centre, tuning.OrbWidth + hit * tuning.OrbWidth2, tuning.OrbHeight + hit * tuning.OrbHeight2, tuning.OrbShape, Hot, hit * tuning.OrbAlpha, tuning.OrbLayer, emitter: "WizardSpellEffects.Shield.9");
            frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Gold, rise: tuning.SparksRise, emitter: "WizardSpellEffects.Shield.10");
        }
    }

    private static void BlinkRing(Frame frame, Vector2 feet, float progress, bool descending)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.BlinkRing);
        float lift = tuning.Lift + tuning.Lift2 * (descending ? 1 - Ease(progress * tuning.Lift3) : Ease(progress * tuning.Lift4));
        Vector2 centre = feet - Vector2.UnitY * lift;
        float width = tuning.Width + tuning.Width2 * MathF.Sin(progress * MathF.PI);
        frame.Orbit(centre, width / 2, tuning.OrbitY, 0, MathF.Tau, Blue, tuning.OrbitWidth, tuning.OrbitAlpha, emitter: "WizardSpellEffects.BlinkRing.1");
        frame.Orbit(centre, width * tuning.OrbitX, tuning.OrbitY2, tuning.OrbitAngle, tuning.OrbitSweep, PaleBlue, tuning.OrbitWidth2, tuning.OrbitAlpha2, emitter: "WizardSpellEffects.BlinkRing.2");
        frame.Sprite(centre, width * tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Blue, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "WizardSpellEffects.BlinkRing.3");
        frame.Clip(centre + Vector2.UnitY * tuning.SmokeSequenceOffset, width * tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase,
            tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer, emitter: "WizardSpellEffects.BlinkRing.4");
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float angle = i * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle;
            Vector2 root = centre + Ellipse(angle, width * tuning.EllipseX, tuning.EllipseY);
            Vector2 end = root - new Vector2(MathF.Cos(angle) * tuning.X, tuning.Y + frame.Unit(i + 33) * tuning.Y2);
            frame.Line(root, end, tuning.LineWidth, i % 3 == 0 ? White : Blue, tuning.LineAlpha, Depth(angle), emitter: "WizardSpellEffects.BlinkRing.5");
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, PaleBlue, rise: tuning.SparksRise, emitter: "WizardSpellEffects.BlinkRing.6");
    }

    private static void TeleportLight(Frame frame, Vector2 feet, float progress, bool departure)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.TeleportLight);
        float spread = departure ? 1 - progress * tuning.Spread : tuning.Spread2 + tuning.Spread3 * Ease(progress * tuning.Spread4);
        Vector2 centre = feet - Vector2.UnitY * (tuning.Centre + tuning.Centre2 * MathF.Sin(progress * MathF.PI));
        float pulse = tuning.Pulse + tuning.Pulse2 * MathF.Sin(progress * MathF.PI);
        frame.Sprite(centre, tuning.OrbWidth * spread, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "WizardSpellEffects.TeleportLight.1");
        frame.Sprite(centre, tuning.OrbWidth2 * spread, tuning.OrbHeight2, tuning.OrbShape2, White, pulse, tuning.OrbLayer, light: tuning.OrbLight2, emitter: "WizardSpellEffects.TeleportLight.2");
        frame.Sprite(feet, tuning.OrbWidth3 * spread, tuning.OrbHeight3 * spread, tuning.OrbShape3, tuning.OrbColour2, tuning.OrbAlpha2, tuning.OrbLayer2, light: tuning.OrbLight3, emitter: "WizardSpellEffects.TeleportLight.3");
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float x = (frame.Unit(i) - tuning.X) * tuning.X2 * spread;
            float depth = frame.Unit(i + 20) * tuning.Depth - tuning.Depth2;
            float height = tuning.Height + frame.Unit(i + 30) * tuning.Height2;
            float y = departure ? depth - tuning.Y * progress : depth - tuning.Y2 * (1 - progress);
            Vector2 end = feet + new Vector2(x, y);
            frame.Line(end - Vector2.UnitY * height, end, tuning.LineWidth + frame.Unit(i + 40) * tuning.LineWidth2, i % 3 == 0 ? PaleBlue : White,
                tuning.LineAlpha, depth < 0 ? tuning.LineLayer : tuning.LineLayer2, emitter: "WizardSpellEffects.TeleportLight.4");
        }
    }

    private static void TurnUndead(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.TurnUndead);
        Vector2 centre = feet - Vector2.UnitY * (prepare ? tuning.PrepareCentre : tuning.ReleaseCentre);
        if (prepare)
        {
            float size = tuning.Size + progress * tuning.Size2;
            frame.Sprite(centre, size, size, tuning.OrbShape, Ember, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "WizardSpellEffects.TurnUndead.1");
            frame.Sprite(centre, size * tuning.GlyphWidth, size * tuning.GlyphHeight, tuning.GlyphShape, Fire, tuning.GlyphAlpha, rotation: progress * tuning.GlyphRotation, emitter: "WizardSpellEffects.TurnUndead.2");
            frame.Sprite(centre, size * tuning.HaloWidth, size * tuning.HaloHeight, tuning.HaloShape, Gold, tuning.HaloAlpha, emitter: "WizardSpellEffects.TurnUndead.3");
            frame.Sprite(centre, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Hot, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight, emitter: "WizardSpellEffects.TurnUndead.4");
            frame.Orbit(centre, tuning.OrbitX, tuning.OrbitY, -progress * tuning.OrbitAngle, tuning.OrbitSweep, tuning.OrbitColour, tuning.OrbitWidth, tuning.OrbitAlpha, emitter: "WizardSpellEffects.TurnUndead.5");
            frame.Line(centre - Vector2.UnitX * tuning.LineStart, centre + Vector2.UnitX * tuning.LineEnd, tuning.LineWidth, PaleBlue, tuning.LineAlpha, tuning.LineLayer, emitter: "WizardSpellEffects.TurnUndead.6");
            frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Fire, emitter: "WizardSpellEffects.TurnUndead.7");
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
            frame.Sprite(point, width * tuning.OrbWidth, width * tuning.OrbHeight, tuning.OrbShape2, tint, tuning.OrbAlpha2, Depth(angle), light: tuning.OrbLight2, emitter: "WizardSpellEffects.TurnUndead.8");
            frame.Sprite(point, width, width, tuning.OrbShape3, tint == Fire ? Hot : White, tuning.OrbAlpha3, tuning.OrbLayer, emitter: "WizardSpellEffects.TurnUndead.9");
            frame.Sprite(point, width * tuning.FlashWidth2, width * tuning.FlashHeight2, tuning.FlashShape2, White, tuning.FlashAlpha2, tuning.FlashLayer2, angle, emitter: "WizardSpellEffects.TurnUndead.10");
            frame.Line(point, point + Ellipse(angle - tuning.EllipseAngle, tuning.EllipseX, tuning.EllipseY2), tuning.LineWidth2, tint, tuning.LineAlpha2, Depth(angle), emitter: "WizardSpellEffects.TurnUndead.11");
        }
        frame.Sparks(centre, tuning.SparksCount2, tuning.SparksRx2, tuning.SparksRy2, progress, Gold, emitter: "WizardSpellEffects.TurnUndead.12");
        frame.Clip(centre - Vector2.UnitY * (progress * tuning.SmokeSequenceOffset), tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase,
            tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha * progress, tuning.SmokeSequenceLayer, emitter: "WizardSpellEffects.TurnUndead.13");
    }

    private static void Vampirism(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Vampirism);
        Vector2 centre = feet - Vector2.UnitY * (phase == SpellVisualPhase.Prepare ? tuning.Centre : tuning.Centre2);
        if (phase == SpellVisualPhase.Return)
        {
            frame.Sprite(feet - Vector2.UnitY * tuning.OrbOffset, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight, emitter: "WizardSpellEffects.Vampirism.1");
            for (int i = 0; i < tuning.ParticleCount; i++)
            {
                float angle = i * MathF.Tau / tuning.AngleDivisor + progress * tuning.Angle;
                Vector2 point = feet + Ellipse(angle, tuning.EllipseX + tuning.EllipseX2 * (1 - progress), tuning.EllipseY);
                frame.Line(point - new Vector2(tuning.X, tuning.Y * (1 - progress) + tuning.Y2), point, tuning.LineWidth, tuning.LineColour, tuning.LineAlpha, Depth(angle), light: i % 2 == 0, emitter: "WizardSpellEffects.Vampirism.2");
            }
            frame.Sprite(centre - Vector2.UnitY * tuning.OrbOffset2, tuning.OrbWidth2, tuning.OrbHeight2, tuning.OrbShape2, tuning.OrbColour2, tuning.OrbAlpha2, tuning.OrbLayer2, light: tuning.OrbLight2, emitter: "WizardSpellEffects.Vampirism.3");
            frame.Sprite(centre + Vector2.UnitY * tuning.FlameOffset, tuning.FlameWidth, tuning.FlameHeight, tuning.FlameShape, tuning.FlameColour, tuning.FlameAlpha, rotation: tuning.FlameRotation, light: tuning.FlameLight, emitter: "WizardSpellEffects.Vampirism.4");
            return;
        }

        float spin = progress * (phase == SpellVisualPhase.Prepare ? tuning.Spin : tuning.Spin2);
        frame.Clip(centre, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress * tuning.SmokeSequencePhase, tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer, emitter: "WizardSpellEffects.Vampirism.5");
        for (int i = 0; i < tuning.FlameTongueCount; i++)
        {
            float angle = spin + i * MathF.Tau / tuning.AngleDivisor2;
            Vector2 point = centre + Ellipse(angle, tuning.EllipseX3 + frame.Unit(i + 8) * tuning.EllipseX4, tuning.EllipseY2 + i * tuning.EllipseY3);
            Vector2 wing = Ellipse(angle + tuning.EllipseAngle, tuning.EllipseX5, tuning.EllipseY4);
            frame.Sprite(point, tuning.FlameTongueWidth, tuning.FlameTongueHeight, tuning.FlameTongueShape, tuning.FlameTongueColour, tuning.FlameTongueAlpha, Depth(angle), angle + tuning.FlameTongueRotation, light: tuning.FlameTongueLight, emitter: "WizardSpellEffects.Vampirism.6");
            frame.Line(point, point + wing, tuning.LineWidth2, i % 2 == 0 ? tuning.LineColour2 : tuning.LineColour3, tuning.LineAlpha2, Depth(angle), emitter: "WizardSpellEffects.Vampirism.7");
            frame.Line(point + wing * tuning.LineStart, point + wing + Ellipse(angle + tuning.EllipseAngle2, tuning.EllipseX6, tuning.EllipseY5), tuning.LineWidth3, tuning.LineColour4, tuning.LineAlpha3, Depth(angle), emitter: "WizardSpellEffects.Vampirism.8");
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, tuning.SparksColour, rise: tuning.SparksRise, emitter: "WizardSpellEffects.Vampirism.9");
    }

    private static void Mirroring(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Mirroring);
        bool fade = phase == SpellVisualPhase.Expire;
        bool prepare = phase == SpellVisualPhase.Prepare;
        float lift = prepare ? progress * tuning.Lift : tuning.Lift2 + tuning.Lift3 * progress;
        float spread = fade ? 1 - progress * tuning.Spread : tuning.Spread2 + progress * tuning.Spread3;
        Vector2 centre = feet - Vector2.UnitY * lift;
        frame.Sprite(centre, tuning.SmokeWidth * spread, tuning.SmokeHeight, tuning.SmokeShape, Ice, tuning.SmokeAlpha, tuning.SmokeLayer, emitter: "WizardSpellEffects.Mirroring.1");
        if (prepare)
        {
            for (int i = 0; i < tuning.ParticleCount; i++)
                frame.Orbit(centre - Vector2.UnitY * i * tuning.OrbitOffset, (tuning.OrbitX + i * tuning.OrbitX2) * spread, tuning.OrbitY,
                    progress * tuning.OrbitAngle + i * tuning.OrbitAngle2, tuning.OrbitSweep, i == 0 ? PaleBlue : Blue, tuning.OrbitWidth, tuning.OrbitAlpha, emitter: "WizardSpellEffects.Mirroring.2");
            frame.Sprite(centre, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Blue, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "WizardSpellEffects.Mirroring.3");
        }
        else
        {
            frame.Sprite(feet - Vector2.UnitY * tuning.SpiritFigureOffset, tuning.SpiritFigureWidth * spread, tuning.SpiritFigureHeight, tuning.SpiritFigureShape, tuning.SpiritFigureColour,
                fade ? tuning.SpiritFigureAlpha : tuning.SpiritFigureAlpha2, rotation: tuning.SpiritFigureRotation + progress * tuning.SpiritFigureRotation2, emitter: "WizardSpellEffects.Mirroring.4");
            for (int i = 0; i < tuning.SmokeCount; i++)
            {
                float height = (i + tuning.Height) / tuning.HeightDivisor;
                Vector2 point = feet + new Vector2((frame.Unit(i + 15) - tuning.X) * tuning.X2 * spread, tuning.Y - height * tuning.Y2 - progress * tuning.Y3);
                frame.Sprite(point, (tuning.SmokeWidth2 + tuning.SmokeWidth3 * frame.Unit(i)) * spread, tuning.SmokeHeight2 + frame.Unit(i + 7) * tuning.SmokeHeight3,
                    tuning.SmokeShape2, i % 2 == 0 ? PaleBlue : IceWhite, tuning.SmokeAlpha2, rotation: progress + i, light: i % 3 == 0, emitter: "WizardSpellEffects.Mirroring.5");
            }
            frame.Sprite(feet - Vector2.UnitY * tuning.OrbOffset, tuning.OrbWidth2 * spread, tuning.OrbHeight2, tuning.OrbShape2, White, fade ? tuning.OrbAlpha2 : tuning.OrbAlpha3, tuning.OrbLayer, light: tuning.OrbLight2, emitter: "WizardSpellEffects.Mirroring.6");
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx * spread, tuning.SparksRy, progress, IceWhite, rise: fade ? tuning.SparksRise : tuning.SparksRise2, emitter: "WizardSpellEffects.Mirroring.7");
    }

    private static void Booster(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress, long now)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Wizard.Booster);
        bool active = phase == SpellVisualPhase.Aura;
        Vector2 eye = feet - Vector2.UnitY * (tuning.Eye + (active ? 0 : tuning.Eye2 * MathF.Sin(progress * MathF.PI)));
        frame.Sprite(eye, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "WizardSpellEffects.Booster.1");
        frame.Sprite(eye, tuning.SigilEyeWidth, tuning.SigilEyeHeight, tuning.SigilEyeShape, Fire, tuning.SigilEyeAlpha, emitter: "WizardSpellEffects.Booster.2");
        frame.Sprite(eye, tuning.SigilEyeWidth2, tuning.SigilEyeHeight2, tuning.SigilEyeShape2, Gold, tuning.SigilEyeAlpha2 + tuning.SigilEyeAlpha3 * MathF.Sin(now % 950 * MathF.Tau / tuning.XDivisor), tuning.SigilEyeLayer, emitter: "WizardSpellEffects.Booster.3");
        frame.Sprite(eye, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Hot, tuning.FlashAlpha, tuning.FlashLayer, emitter: "WizardSpellEffects.Booster.4");
        frame.Sparks(eye, active ? tuning.SparksCount : tuning.SparksCount2, tuning.SparksRx, tuning.SparksRy, now % 950 / tuning.SparksProgressDivisor, Gold, tuning.SparksRise, emitter: "WizardSpellEffects.Booster.5");
        if (!active)
        {
            frame.Sprite(feet - Vector2.UnitY * tuning.FlameTongueOffset, tuning.FlameTongueWidth, tuning.FlameTongueHeight, tuning.FlameTongueShape, Ember, tuning.FlameTongueAlpha, light: tuning.FlameTongueLight, emitter: "WizardSpellEffects.Booster.6");
            frame.Line(eye, feet - Vector2.UnitY * tuning.LineEnd, tuning.LineWidth, Fire, tuning.LineAlpha, light: tuning.LineLight, emitter: "WizardSpellEffects.Booster.7");
            for (int i = 0; i < tuning.FeatherCount; i++)
            {
                float offset = (frame.Unit(i) - tuning.Offset) * tuning.Offset2;
                float y = tuning.Y + tuning.Y2 * Fract(progress + frame.Unit(i + 11) * tuning.Y3);
                frame.Sprite(feet + new Vector2(offset, y), tuning.FeatherWidth, tuning.FeatherHeight, tuning.FeatherShape, Fire, tuning.FeatherAlpha, tuning.FeatherLayer, offset * tuning.FeatherRotation, emitter: "WizardSpellEffects.Booster.8");
            }
        }
    }
}
