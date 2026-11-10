using System.Numerics;
using CrystalM2.Rendering.Core;
using Effect = Client.MirScenes.SignatureSpellEffects.Effect;
using Form = Client.MirScenes.SignatureSpellEffects.Form;
using Look = Client.MirScenes.SignatureSpellEffects.Look;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

public enum SpellVisualPhase { Impact, Prepare, Swing, FollowThrough, Aura, Field, Flight, Expire, Burn, Ward, Hit, Departure, Arrival, Return, Contact }
public enum ParticleMotif { None, Celebration, Radiance, CraftSuccess, CraftFail, Click }

public readonly record struct SpellVisual(Spell Spell, SpellVisualPhase Phase = SpellVisualPhase.Impact,
    ParticleMotif Motif = ParticleMotif.None, int ExtraReach = 0)
{
    public SpellVisual(ParticleMotif motif) : this(Spell.None, Motif: motif) { }
    internal bool HasArt => Spell != Spell.None || Motif != ParticleMotif.None;
}

internal static class LegacySpellEffects
{
    internal readonly record struct WindupCue(SpellVisualPhase Phase, int Duration, int Delay = 0, bool AtCell = false);

    internal static WindupCue Windup(Spell spell, int frames, int interval)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Windup);
        int cast = frames * interval;
        return spell switch
        {
            Spell.ThunderBolt => new(SpellVisualPhase.Prepare, tuning.ThunderBoltDuration),
            Spell.Revelation => new(SpellVisualPhase.Prepare, tuning.RevelationDuration),
            Spell.LionRoar or Spell.BattleCry => new(SpellVisualPhase.Impact, tuning.LionRoarDuration),
            Spell.FlameField => new(SpellVisualPhase.Impact, tuning.FlameFieldDuration, AtCell: true),
            Spell.ThunderStorm => new(SpellVisualPhase.Impact, cast, AtCell: true),
            Spell.Fury => new(SpellVisualPhase.Impact, tuning.FuryDuration * interval),
            Spell.ImmortalSkin => new(SpellVisualPhase.Impact, cast * tuning.ImmortalSkinDuration),
            Spell.UltimateEnhancer => new(SpellVisualPhase.Prepare, tuning.UltimateEnhancerDuration),
            Spell.MagicBooster => new(SpellVisualPhase.Impact, tuning.MagicBoosterDuration * interval),
            Spell.PetEnhancer => new(SpellVisualPhase.Prepare, tuning.PetEnhancerDuration * interval),
            Spell.TwinDrakeBlade => new(SpellVisualPhase.Prepare, tuning.TwinDrakeBladeDuration),
            Spell.Thrusting => new(SpellVisualPhase.Swing, (frames - frames / tuning.ThrustingDurationDivisor) * interval, frames / tuning.ThrustingDelayDivisor * interval),
            Spell.BladeAvalanche => new(SpellVisualPhase.Swing, tuning.BladeAvalancheDuration * interval, AtCell: true),
            Spell.SlashingBurst => new(SpellVisualPhase.Swing, tuning.SlashingBurstDuration * interval, tuning.SlashingBurstDelay, AtCell: true),
            Spell.CounterAttack => new(SpellVisualPhase.Swing, tuning.CounterAttackDuration * interval),
            Spell.Repulsion or Spell.EnergyRepulsor or Spell.Rage or Spell.ProtectionField or Spell.Hiding
                => new(SpellVisualPhase.Impact, cast),
            Spell.FireBall or Spell.GreatFireBall or Spell.FrostCrunch or Spell.Healing or Spell.ElectricShock or
                Spell.Poisoning or Spell.HellFire or Spell.SummonSkeleton or Spell.StormEscape or Spell.Teleport or Spell.Blink or
                Spell.FireBang or Spell.FireWall or Spell.HealingCircle or Spell.TrapHexagon or Spell.FlameDisruptor or
                Spell.SummonShinsu or Spell.Purification or Spell.MassHealing or Spell.IceStorm or Spell.MagicShield or
                Spell.TurnUndead or Spell.Vampirism or Spell.Entrapment or Spell.Mirroring or Spell.Blizzard or
                Spell.MeteorStrike or Spell.FireBounce or Spell.MeteorShower => new(SpellVisualPhase.Prepare, cast),
            _ => default
        };
    }

    internal static int FieldInterval(Spell spell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.FieldInterval);
        return spell switch
        {
            Spell.HealingCircle => tuning.HealingCircleIntervalMs,
            Spell.FireWall or Spell.PoisonCloud => tuning.FireWallIntervalMs,
            _ => tuning.DefaultIntervalMs
        };
    }

    private static Look Art(Form form, Shape shape, int size, int colour, int core) =>
        new(form, shape, size, Color.FromArgb(unchecked((int)0xff000000) | colour),
            Color.FromArgb(unchecked((int)0xff000000) | core));

    private static Look Profile(Spell spell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Profile);
        return spell switch
        {
            // Warrior. Use the angle and length of the hit to identify each weapon technique.
            Spell.Fencing => Art(tuning.FencingForm, tuning.FencingShape, tuning.FencingSize, tuning.FencingColour, tuning.FencingCore),
            Spell.Slaying => Art(tuning.SlayingForm, tuning.SlayingShape, tuning.SlayingSize, tuning.SlayingColour, tuning.SlayingCore),
            Spell.Thrusting => Art(tuning.ThrustingForm, tuning.ThrustingShape, tuning.ThrustingSize, tuning.ThrustingColour, tuning.ThrustingCore),
            Spell.HalfMoon => Art(tuning.HalfMoonForm, tuning.HalfMoonShape, tuning.HalfMoonSize, tuning.HalfMoonColour, tuning.HalfMoonCore),
            Spell.ShoulderDash => Art(tuning.ShoulderDashForm, tuning.ShoulderDashShape, tuning.ShoulderDashSize, tuning.ShoulderDashColour, tuning.ShoulderDashCore),
            Spell.TwinDrakeBlade => Art(tuning.TwinDrakeBladeForm, tuning.TwinDrakeBladeShape, tuning.TwinDrakeBladeSize, tuning.TwinDrakeBladeColour, tuning.TwinDrakeBladeCore),
            Spell.Entrapment => Art(tuning.EntrapmentForm, tuning.EntrapmentShape, tuning.EntrapmentSize, tuning.EntrapmentColour, tuning.EntrapmentCore),
            Spell.FlamingSword => Art(tuning.FlamingSwordForm, tuning.FlamingSwordShape, tuning.FlamingSwordSize, tuning.FlamingSwordColour, tuning.FlamingSwordCore),
            Spell.LionRoar => Art(tuning.LionRoarForm, tuning.LionRoarShape, tuning.LionRoarSize, tuning.LionRoarColour, tuning.LionRoarCore),
            Spell.CrossHalfMoon => Art(tuning.CrossHalfMoonForm, tuning.CrossHalfMoonShape, tuning.CrossHalfMoonSize, tuning.CrossHalfMoonColour, tuning.CrossHalfMoonCore),
            Spell.BladeAvalanche => Art(tuning.BladeAvalancheForm, tuning.BladeAvalancheShape, tuning.BladeAvalancheSize, tuning.BladeAvalancheColour, tuning.BladeAvalancheCore),
            Spell.ProtectionField => Art(tuning.ProtectionFieldForm, tuning.ProtectionFieldShape, tuning.ProtectionFieldSize, tuning.ProtectionFieldColour, tuning.ProtectionFieldCore),
            Spell.Rage => Art(tuning.RageForm, tuning.RageShape, tuning.RageSize, tuning.RageColour, tuning.RageCore),
            Spell.CounterAttack => Art(tuning.CounterAttackForm, tuning.CounterAttackShape, tuning.CounterAttackSize, tuning.CounterAttackColour, tuning.CounterAttackCore),
            Spell.SlashingBurst => Art(tuning.SlashingBurstForm, tuning.SlashingBurstShape, tuning.SlashingBurstSize, tuning.SlashingBurstColour, tuning.SlashingBurstCore),
            Spell.Fury => Art(tuning.FuryForm, tuning.FuryShape, tuning.FurySize, tuning.FuryColour, tuning.FuryCore),
            Spell.ImmortalSkin => Art(tuning.ImmortalSkinForm, tuning.ImmortalSkinShape, tuning.ImmortalSkinSize, tuning.ImmortalSkinColour, tuning.ImmortalSkinCore),
            Spell.BattleCry => Art(tuning.BattleCryForm, tuning.BattleCryShape, tuning.BattleCrySize, tuning.BattleCryColour, tuning.BattleCryCore),

            // Shared animations that previously used the same combat libraries.
            Spell.FatalSword or Spell.DoubleSlash or Spell.HeavenlySword or Spell.CrescentSlash or Spell.FlashDash
                => Art(tuning.FatalSwordForm, tuning.FatalSwordShape, tuning.FatalSwordSize, tuning.FatalSwordColour, tuning.FatalSwordCore),
            Spell.PoisonSword or Spell.PoisonShot or Spell.SummonSnakes => Art(tuning.PoisonSwordForm, tuning.PoisonSwordShape, tuning.PoisonSwordSize, tuning.PoisonSwordColour, tuning.PoisonSwordCore),
            Spell.Hemorrhage or Spell.VampireShot or Spell.SummonVampire or Spell.MPEater
                => Art(tuning.HemorrhageForm, tuning.HemorrhageShape, tuning.HemorrhageSize, tuning.HemorrhageColour, tuning.HemorrhageCore),
            Spell.NapalmShot or Spell.ExplosiveTrap or Spell.DelayedExplosion or Spell.FireBurst
                => Art(tuning.NapalmShotForm, tuning.NapalmShotShape, tuning.NapalmShotSize, tuning.NapalmShotColour, tuning.NapalmShotCore),
            Spell.ElementalBarrier or Spell.OneWithNature or Spell.ElementalShot
                => Art(tuning.ElementalBarrierForm, tuning.ElementalBarrierShape, tuning.ElementalBarrierSize, tuning.ElementalBarrierColour, tuning.ElementalBarrierCore),
            Spell.Trap or Spell.Stonetrap or Spell.BindingShot or Spell.CrippleShot or Spell.CatTongue
                => Art(tuning.TrapForm, tuning.TrapShape, tuning.TrapSize, tuning.TrapColour, tuning.TrapCore),
            Spell.MoonLight or Spell.MoonMist or Spell.DarkBody or Spell.Portal
                => Art(tuning.MoonLightForm, tuning.MoonLightShape, tuning.MoonLightSize, tuning.MoonLightColour, tuning.MoonLightCore),
            Spell.SummonToad => Art(tuning.SummonToadForm, tuning.SummonToadShape, tuning.SummonToadSize, tuning.SummonToadColour, tuning.SummonToadCore),
            Spell.Haste or Spell.LightBody or Spell.SwiftFeet => Art(tuning.HasteForm, tuning.HasteShape, tuning.HasteSize, tuning.HasteColour, tuning.HasteCore),
            Spell.Concentration or Spell.Focus => Art(tuning.ConcentrationForm, tuning.ConcentrationShape, tuning.ConcentrationSize, tuning.ConcentrationColour, tuning.ConcentrationCore),
            Spell.StraightShot or Spell.DoubleShot => Art(tuning.StraightShotForm, tuning.StraightShotShape, tuning.StraightShotSize, tuning.StraightShotColour, tuning.StraightShotCore),
            _ => default
        };
    }

    internal static bool HasLiveArt(Spell spell) =>
        WizardSpellEffects.HasArt(spell) || WarriorSpellEffects.HasArt(spell) || TaoistSpellEffects.HasArt(spell);

    internal static SpellVisualPhase ImpactPhase(Spell spell) => spell switch
    {
        Spell.MassHealing or Spell.MassHiding => SpellVisualPhase.Hit,
        _ when WarriorSpellEffects.HasArt(spell) || spell is Spell.FireBang or Spell.IceStorm or Spell.IceThrust or
            Spell.FlameField or Spell.ThunderStorm or Spell.StormEscape => SpellVisualPhase.Contact,
        _ => SpellVisualPhase.Impact
    };

    internal static int PacketDuration(SpellVisual visual)
    {
        if (WarriorSpellEffects.HasArt(visual.Spell)) return WarriorSpellEffects.PacketDuration(visual);
        if (TaoistSpellEffects.HasArt(visual.Spell)) return TaoistSpellEffects.PacketDuration(visual);
        return WizardSpellEffects.PacketDuration(visual);
    }

    internal static bool HasProfile(Spell spell) => HasLiveArt(spell) || Profile(spell).Size != 0;

    internal static bool UsesWeaponMask(Spell spell) => WarriorSpellEffects.UsesWeaponArt(spell) ||
        spell is Spell.DoubleSlash or Spell.HeavenlySword or Spell.CrescentSlash;

    internal static (Color Colour, Color Core) WeaponColours(Spell spell)
    {
        var look = Profile(spell);
        return (look.Colour, look.Core);
    }

    internal static void WeaponSweep(SpellParticlePool pool, Spell spell, WeaponMaskTrace pose, WeaponMaskTrace? last,
        long now, int remaining, int interval, Vector2 facing = default, float progress = .58f,
        Vector2? feet = null, float previousProgress = 0, bool followThrough = false)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.WeaponSweep);
        if (remaining <= 0) return;
        var look = Profile(spell);
        if (look.Size == 0) return;
        if (WarriorSpellEffects.UsesWeaponArt(spell))
        {
            WarriorSpellEffects.WeaponSweep(pool, spell, look, pose, last, facing,
                feet ?? pose.Hilt + Vector2.UnitY * tuning.Offset, now, remaining, interval, progress, previousProgress, followThrough);
            return;
        }
        var effect = new Effect(pool, look, now);
        int life = Math.Min(remaining, Math.Clamp(interval * tuning.Life, 65, 210));
        Vector2 blade = pose.Tip - pose.Hilt;
        Vector2 drift = last is { } previous ? pose.Tip - previous.Tip : blade * tuning.Drift;
        if (last is { } old && drift.LengthSquared() is > 4 and < 16000)
        {
            // Several tapered samples fill the area swept between the two real weapon poses.
            int pieces = Math.Clamp((int)drift.Length() / tuning.PiecesDivisor, 2, 7);
            for (int i = 0; i < pieces; i++)
            {
                float phase = (i + 1f) / pieces;
                Vector2 hilt = Vector2.Lerp(old.Hilt, pose.Hilt, phase);
                Vector2 tip = Vector2.Lerp(old.Tip, pose.Tip, phase);
                Vector2 edge = tip - hilt;
                int span = Math.Max(tuning.Span, (int)edge.Length() + tuning.Span2);
                effect.Add(hilt + edge * tuning.BladeEdgeOffset, Vector2.Zero, tuning.BladeEdgeShape, span, life, tuning.BladeEdgeLayer,
                    aspect: tuning.BladeEdgeAspect / span, rotation: MathF.Atan2(edge.Y, edge.X),
                    alpha: (int)(tuning.BladeEdgeAlpha + phase * tuning.BladeEdgeAlpha2));
            }

            int tipSpan = (int)drift.Length() + tuning.TipSpan;
            effect.Add((old.Tip + pose.Tip) * tuning.BladeEdgeOffset2, Vector2.Zero, tuning.BladeEdgeShape2, tipSpan, life,
                tuning.BladeEdgeLayer2, aspect: tuning.BladeEdgeAspect2 / tipSpan,
                rotation: MathF.Atan2(drift.Y, drift.X), core: tuning.BladeEdgeCore, alpha: tuning.BladeEdgeAlpha3);
        }

        effect.Add(pose.Tip, drift * tuning.FlashVelocity, tuning.FlashShape, tuning.FlashSize, life, tuning.FlashLayer, core: tuning.FlashCore);
    }

    internal static Vector2 ProjectileAnchor(Spell spell, Vector2 cell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.ProjectileAnchor);
        return spell switch
        {
            Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch => cell - new Vector2(0, tuning.FireBallY),
            Spell.SoulFireBall or Spell.SoulShield or Spell.BlessedArmour or Spell.MassHiding or
                Spell.Curse or Spell.Hallucination or Spell.Plague or Spell.PoisonCloud => cell - new Vector2(0, tuning.SoulFireBallY),
            _ => cell - new Vector2(0, tuning.Y)
        };
    }

    internal static (int TravelPerCellMs, int WakeIntervalMs) ProjectileTiming(Spell spell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.ProjectileTiming);
        return (tuning.TravelPerCellMs, tuning.WakeIntervalMs);
    }

    internal static void ProjectileFrame(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frame, int remaining, bool leaveWake, float size = WizardSpellEffects.DefaultSize)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.ProjectileFrame);
        if (WizardSpellEffects.DrawProjectile(pool, spell, head, heading, now, frame, remaining, leaveWake, size)) return;
        if (TaoistSpellEffects.DrawProjectile(pool, spell, head, heading, now, frame, remaining, leaveWake, size)) return;
        var look = Profile(spell);
        if (look.Size == 0 || remaining <= 0) return;

        float angle = MathF.Atan2(heading.Y, heading.X);
        float turn = frame * tuning.Turn;
        var effect = new Effect(pool, look, now, frameOnly: true);
        switch (spell)
        {
            case Spell.StraightShot:
            case Spell.DoubleShot:
            case Spell.NapalmShot:
            case Spell.ElementalShot:
            case Spell.CatTongue:
                effect.Add(head, Vector2.Zero, tuning.CatTongueBladeEdgeShape, look.Size, remaining, tuning.CatTongueBladeEdgeLayer,
                    aspect: tuning.CatTongueBladeEdgeAspect, rotation: angle, core: tuning.CatTongueBladeEdgeCore);
                break;
            default:
                effect.Add(head, Vector2.Zero, tuning.OrbShape, look.Size * tuning.OrbSize / tuning.OrbSizeDivisor, remaining, tuning.OrbLayer, alpha: tuning.OrbAlpha);
                effect.Add(head, Vector2.Zero, look.Shape, look.Size * tuning.AddSize / tuning.AddSizeDivisor, remaining,
                    tuning.AddLayer, aspect: tuning.AddAspect, rotation: angle + turn, core: tuning.AddCore);
                break;
        }

        if (!leaveWake) return;
        var wake = new Effect(pool, look, now);
        Vector2 side = new(-heading.Y, heading.X);
        float drift = (frame % 3 - 1) * tuning.Drift;
        wake.Add(head - heading * tuning.OrbOffset + side * drift, -heading * tuning.OrbVelocity + side * drift * tuning.OrbVelocity2,
            tuning.OrbShape2, look.Size / 2, Math.Min(remaining, tuning.Val2), tuning.OrbLayer2,
            aspect: tuning.OrbAspect, rotation: angle, growth: tuning.OrbGrowth, alpha: tuning.OrbAlpha2);
        wake.Add(head - heading * tuning.SparkOffset, -heading * tuning.SparkVelocity + side * drift, tuning.SparkShape, tuning.SparkSize,
            Math.Min(remaining, tuning.Val22), tuning.SparkLayer, rotation: angle + turn, core: tuning.SparkCore);
    }

    private static Look Profile(SpellVisual visual)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.MotifProfile);
        return visual.Motif switch
        {
            ParticleMotif.Celebration => Art(tuning.CelebrationForm, tuning.CelebrationShape, tuning.CelebrationSize, tuning.CelebrationColour, tuning.CelebrationCore),
            ParticleMotif.Radiance => Art(tuning.RadianceForm, tuning.RadianceShape, tuning.RadianceSize, tuning.RadianceColour, tuning.RadianceCore),
            ParticleMotif.CraftSuccess => Art(tuning.CraftSuccessForm, tuning.CraftSuccessShape, tuning.CraftSuccessSize, tuning.CraftSuccessColour, tuning.CraftSuccessCore),
            ParticleMotif.CraftFail => Art(tuning.CraftFailForm, tuning.CraftFailShape, tuning.CraftFailSize, tuning.CraftFailColour, tuning.CraftFailCore),
            ParticleMotif.Click => Art(tuning.ClickForm, tuning.ClickShape, tuning.ClickSize, tuning.ClickColour, tuning.ClickCore),
            _ => Profile(visual.Spell)
        };
    }

    internal static void Emit(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing, long now,
        int duration = 0, long? sampleTime = null, uint fieldObjectID = 0)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Emit);
        var look = Profile(visual);
        if (look.Size == 0) return;

        int reference = visual.Phase switch
        {
            SpellVisualPhase.Prepare => tuning.PrepareFactor,
            SpellVisualPhase.Aura => tuning.AuraFactor,
            SpellVisualPhase.Burn => tuning.BurnFactor,
            SpellVisualPhase.Swing or SpellVisualPhase.FollowThrough => tuning.SwingFactor,
            _ => tuning.Factor
        };
        var effect = new Effect(pool, look, now, castsLight: visual.Motif == ParticleMotif.None,
            window: duration, referenceDuration: reference, sampleTime: sampleTime, fieldObjectID: fieldObjectID);
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        float angle = MathF.Atan2(facing.Y, facing.X);
        switch (visual.Phase)
        {
            case SpellVisualPhase.Prepare:
                Prepare(effect, look, body + facing * tuning.PrepareHand, angle);
                break;
            case SpellVisualPhase.Flight:
                effect.Add(body, facing * tuning.FlightOrbVelocity, tuning.FlightOrbShape, look.Size * tuning.FlightOrbSize / 4, tuning.FlightOrbDuration, tuning.FlightOrbLayer,
                    aspect: tuning.FlightOrbAspect, alpha: tuning.FlightOrbAlpha);
                effect.Add(body + facing * tuning.FlightAddOffset, facing * tuning.FlightAddVelocity, look.Shape, look.Size / 2, tuning.FlightAddDuration, tuning.FlightAddLayer,
                    aspect: tuning.FlightAddAspect, rotation: angle, core: tuning.FlightAddCore);
                break;
            case SpellVisualPhase.Swing:
            case SpellVisualPhase.FollowThrough:
                Swing(effect, look, visual, feet, body, facing, angle);
                break;
            case SpellVisualPhase.Aura:
                Aura(effect, look, visual.Spell, feet, body);
                break;
            case SpellVisualPhase.Field:
                Field(effect, look, visual.Spell, feet, body, now);
                break;
            case SpellVisualPhase.Expire:
                effect.Scatter(body, tuning.ExpireShardCount, tuning.ExpireShardShape, tuning.ExpireShardSpread, tuning.ExpireShardDuration);
                break;
            case SpellVisualPhase.Burn:
                effect.Add(feet, Vector2.Zero, tuning.BurnHaloShape, tuning.BurnHaloSize, tuning.BurnHaloDuration, tuning.BurnHaloLayer, aspect: tuning.BurnHaloAspect, alpha: tuning.BurnHaloAlpha);
                effect.Add(body + new Vector2(tuning.BurnX, tuning.BurnY), new Vector2(tuning.BurnX2, tuning.BurnY2), tuning.BurnWispShape, tuning.BurnWispSize, tuning.BurnWispDuration,
                    tuning.BurnWispLayer, aspect: tuning.BurnWispAspect, growth: tuning.BurnWispGrowth);
                effect.Scatter(feet, tuning.BurnSparkCount, tuning.BurnSparkShape, tuning.BurnSparkSpread, tuning.BurnSparkDuration, rise: true);
                break;
            case SpellVisualPhase.Ward:
                effect.Ring(feet, look.Size, tuning.WardRingDuration);
                effect.Add(body, -Vector2.UnitY * tuning.WardShieldVelocity, tuning.WardShieldShape, tuning.WardShieldSize, tuning.WardShieldDuration, tuning.WardShieldLayer,
                    aspect: tuning.WardShieldAspect, growth: tuning.WardShieldGrowth, alpha: tuning.WardShieldAlpha);
                break;
            default:
                Impact(effect, look, visual.Spell, feet, body, facing, angle);
                break;
        }
    }

    private static void Prepare(Effect effect, Look look, Vector2 hand, float angle)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Prepare);
        effect.Add(hand, -Vector2.UnitY * tuning.AddVelocity, look.Shape, Math.Max(tuning.Val1, look.Size * tuning.Val2 / 3),
            tuning.AddDuration, tuning.AddLayer, aspect: tuning.AddAspect, rotation: angle * tuning.AddRotation, growth: tuning.AddGrowth, alpha: tuning.AddAlpha);
        effect.Add(hand, Vector2.Zero, tuning.OrbShape, Math.Max(tuning.Val12, look.Size / 4), tuning.OrbDuration,
            tuning.OrbLayer, delay: tuning.OrbDelay, core: tuning.OrbCore);
        effect.Scatter(hand, tuning.SparkCount, tuning.SparkShape, tuning.SparkSpread, tuning.SparkDuration, rise: true);
    }

    private static void Swing(Effect effect, Look look, SpellVisual visual, Vector2 feet, Vector2 body,
        Vector2 facing, float angle)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Swing);
        bool follow = visual.Phase == SpellVisualPhase.FollowThrough;
        if (visual.Spell == Spell.Thrusting)
        {
            effect.Add(body + facing * tuning.BoltOffset, facing * tuning.BoltVelocity, tuning.BoltShape, look.Size, tuning.BoltDuration, tuning.BoltLayer,
                aspect: tuning.BoltAspect, rotation: angle, core: tuning.BoltCore);
            effect.Scatter(body + facing * tuning.SparkOffset, tuning.SparkCount, tuning.SparkShape, tuning.SparkSpread, tuning.SparkDuration);
            return;
        }

        if (visual.Spell == Spell.BladeAvalanche)
        {
            for (int i = 0; i < tuning.ShardCount; i++)
                effect.Add(body + facing * (tuning.ShardOffset + i * tuning.ShardOffset2), facing * tuning.ShardVelocity, tuning.ShardShape, tuning.ShardSize + i * tuning.ShardSize2, tuning.ShardDuration, tuning.ShardLayer,
                    aspect: tuning.ShardAspect, rotation: angle + tuning.ShardRotation, delay: i * tuning.ShardDelay, core: i % 2 == 0);
            return;
        }

        int cuts = visual.Spell switch { Spell.CrossHalfMoon => tuning.CrossHalfMoonFactor, Spell.TwinDrakeBlade => tuning.TwinDrakeBladeFactor, _ => 1 };
        for (int i = 0; i < cuts; i++)
        {
            float direction = angle + i * (visual.Spell == Spell.CrossHalfMoon ? MathF.PI / 2 : tuning.Direction) + (follow ? tuning.Direction2 : tuning.Direction3);
            Vector2 travel = new(MathF.Cos(direction), MathF.Sin(direction));
            effect.Add(body + travel * tuning.AddOffset, travel * tuning.AddVelocity, look.Shape, look.Size, tuning.AddDuration, tuning.AddLayer,
                aspect: tuning.AddAspect, rotation: direction, delay: i * tuning.AddDelay, core: follow || i == 1);
        }
        if (visual.Spell is Spell.HalfMoon or Spell.CrossHalfMoon or Spell.SlashingBurst)
            effect.Ring(feet, look.Size, tuning.RingDuration);
        if (visual.Spell == Spell.FlamingSword)
            effect.Add(body + facing * tuning.WispOffset, facing * tuning.WispVelocity - Vector2.UnitY * tuning.WispVelocity2, tuning.WispShape, tuning.WispSize, tuning.WispDuration, tuning.WispLayer, growth: tuning.WispGrowth);
        effect.Scatter(body + facing * tuning.SparkOffset2, tuning.SparkCount2, tuning.SparkShape2, look.Size * tuning.SparkSpread2, tuning.SparkDuration2);
    }

    private static void Aura(Effect effect, Look look, Spell spell, Vector2 feet, Vector2 body)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Aura);
        if (spell is Spell.Poisoning or Spell.Plague or Spell.Curse or Spell.Hiding)
        {
            effect.Add(body, -Vector2.UnitY * tuning.AddVelocity, look.Shape, look.Size * tuning.AddSize / 3, tuning.AddDuration, tuning.AddLayer, alpha: tuning.AddAlpha, growth: tuning.AddGrowth);
            effect.Add(feet, -Vector2.UnitY * tuning.SparkVelocity, tuning.SparkShape, tuning.SparkSize, tuning.SparkDuration, tuning.SparkLayer, core: tuning.SparkCore);
            return;
        }

        effect.Add(feet, Vector2.Zero, tuning.HaloShape, look.Size, tuning.HaloDuration, tuning.HaloLayer, aspect: tuning.HaloAspect, alpha: tuning.HaloAlpha);
        effect.Add(body, -Vector2.UnitY * tuning.AddVelocity2, look.Shape, look.Size * tuning.AddSize2 / 4, tuning.AddDuration2, tuning.AddLayer2,
            aspect: tuning.AddAspect, alpha: tuning.AddAlpha2);
        effect.Add(body + new Vector2(tuning.X, tuning.Y), new Vector2(tuning.X2, tuning.Y2), tuning.SparkShape2, tuning.SparkSize2, tuning.SparkDuration2,
            tuning.SparkLayer2, core: tuning.SparkCore2);
    }

    private static void Field(Effect effect, Look look, Spell spell, Vector2 feet, Vector2 body, long now)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Field);
        float drift = MathF.Sin(feet.X * tuning.Drift + feet.Y * tuning.Drift2 + now % 4096 * tuning.Drift3) * tuning.Drift4;
        switch (spell)
        {
            case Spell.PoisonCloud:
                effect.Add(feet + new Vector2(drift, tuning.PoisonCloudY), new Vector2(tuning.PoisonCloudX, tuning.PoisonCloudY2), tuning.PoisonCloudWispShape, tuning.PoisonCloudWispSize, tuning.PoisonCloudWispDuration,
                    tuning.PoisonCloudWispLayer, aspect: tuning.PoisonCloudWispAspect, alpha: tuning.PoisonCloudWispAlpha, growth: tuning.PoisonCloudWispGrowth);
                effect.Add(feet + new Vector2(-drift, tuning.PoisonCloudY3), new Vector2(tuning.PoisonCloudX2, tuning.PoisonCloudY4), tuning.PoisonCloudWispShape2, tuning.PoisonCloudWispSize2, tuning.PoisonCloudWispDuration2,
                    tuning.PoisonCloudWispLayer2, aspect: tuning.PoisonCloudWispAspect2, alpha: tuning.PoisonCloudWispAlpha2, growth: tuning.PoisonCloudWispGrowth2);
                break;
            case Spell.TrapHexagon:
                for (int i = 0; i < tuning.TrapHexagonBoltCount; i++)
                {
                    float angle = i * MathF.PI / 3;
                    float next = angle + MathF.PI / 3;
                    Vector2 point = new(MathF.Cos(angle) * tuning.TrapHexagonPoint, MathF.Sin(angle) * tuning.TrapHexagonPoint2);
                    Vector2 end = new(MathF.Cos(next) * tuning.TrapHexagonEnd, MathF.Sin(next) * tuning.TrapHexagonEnd2);
                    Vector2 edge = end - point;
                    effect.Add(feet + (point + end) * tuning.TrapHexagonBoltOffset, Vector2.Zero, tuning.TrapHexagonBoltShape, (int)edge.Length() + tuning.TrapHexagonBoltSize,
                        tuning.TrapHexagonBoltDuration, tuning.TrapHexagonBoltLayer, aspect: tuning.TrapHexagonBoltAspect, rotation: MathF.Atan2(edge.Y, edge.X), alpha: tuning.TrapHexagonBoltAlpha);
                }
                break;
            default:
                Aura(effect, look, spell, feet, body);
                break;
        }
    }

    private static void Impact(Effect effect, Look look, Spell spell, Vector2 feet, Vector2 body,
        Vector2 facing, float angle)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Impact);
        switch (look.Form)
        {
            case Form.Cut:
            case Form.Cleave:
                effect.Add(body, facing * tuning.CleaveAddVelocity, look.Shape, Math.Max(tuning.CleaveVal1, look.Size / 2), tuning.CleaveAddDuration, tuning.CleaveAddLayer,
                    aspect: tuning.CleaveAddAspect, rotation: angle - tuning.CleaveAddRotation, core: tuning.CleaveAddCore);
                if (spell is not (Spell.Fencing or Spell.SpiritSword))
                    effect.Scatter(body, tuning.CleaveSparkCount, tuning.CleaveSparkShape, tuning.CleaveSparkSpread, tuning.CleaveSparkDuration);
                break;
            case Form.Rush:
                effect.Add(body, facing * tuning.RushBoltVelocity, tuning.RushBoltShape, look.Size, tuning.RushBoltDuration, tuning.RushBoltLayer, aspect: tuning.RushBoltAspect, rotation: angle);
                effect.Add(feet, facing * tuning.RushWispVelocity, tuning.RushWispShape, tuning.RushWispSize, tuning.RushWispDuration, tuning.RushWispLayer, alpha: tuning.RushWispAlpha);
                effect.Scatter(body, tuning.RushShardCount, tuning.RushShardShape, tuning.RushShardSpread, tuning.RushShardDuration);
                break;
            case Form.Heal:
            case Form.Totem:
            case Form.Ward:
                effect.Ring(feet, look.Size, tuning.WardRingDuration);
                effect.Add(body, -Vector2.UnitY * tuning.WardAddVelocity, look.Shape, look.Size * tuning.WardAddSize / 4, tuning.WardAddDuration,
                    tuning.WardAddLayer, aspect: tuning.WardAddAspect, growth: tuning.WardAddGrowth);
                effect.Add(body, -Vector2.UnitY * tuning.WardFlashVelocity, tuning.WardFlashShape, look.Size / 2, tuning.WardFlashDuration, tuning.WardFlashLayer, core: tuning.WardFlashCore);
                effect.Scatter(body, tuning.WardFeatherCount, look.Form == Form.Totem ? look.Shape : tuning.WardFeatherShape, look.Size * tuning.WardFeatherSpread, tuning.WardFeatherDuration, rise: true);
                break;
            case Form.Bind:
                effect.Ring(feet, look.Size, tuning.BindRingDuration);
                if (spell == Spell.Entrapment)
                {
                    for (int i = 0; i < tuning.BindBoltCount; i++)
                        effect.Add(body + new Vector2(0, i * tuning.BindY - tuning.BindY2), Vector2.Zero, tuning.BindBoltShape, tuning.BindBoltSize, tuning.BindBoltDuration,
                            tuning.BindBoltLayer, aspect: tuning.BindBoltAspect, rotation: i == 1 ? tuning.BindBoltRotation : tuning.BindBoltRotation2, delay: i * tuning.BindBoltDelay, core: i == 1);
                }
                else
                {
                    effect.Add(body, -Vector2.UnitY * tuning.BindAddVelocity, look.Shape, look.Size * tuning.BindAddSize / 4, tuning.BindAddDuration,
                        tuning.BindAddLayer, core: tuning.BindAddCore);
                }
                break;
            case Form.Veil:
                effect.Ring(feet, look.Size, tuning.VeilRingDuration);
                for (int i = -1; i <= tuning.VeilWispCount; i++)
                    effect.Add(body + new Vector2(i * tuning.VeilX, tuning.VeilY), new Vector2(i * tuning.VeilX2, tuning.VeilY2), tuning.VeilWispShape, look.Size * tuning.VeilWispSize / 4,
                        tuning.VeilWispDuration, tuning.VeilWispLayer, growth: tuning.VeilWispGrowth, alpha: tuning.VeilWispAlpha);
                effect.Add(body, -Vector2.UnitY * tuning.VeilAddVelocity, look.Shape, look.Size / 2, tuning.VeilAddDuration, tuning.VeilAddLayer, core: tuning.VeilAddCore);
                break;
            case Form.Break:
                effect.Ring(feet, look.Size, tuning.BreakRingDuration);
                effect.Add(body, Vector2.Zero, look.Shape, look.Size, tuning.BreakAddDuration, tuning.BreakAddLayer,
                    aspect: tuning.BreakAddAspect, growth: tuning.BreakAddGrowth, alpha: tuning.BreakAddAlpha, core: tuning.BreakAddCore);
                effect.Scatter(body, tuning.BreakFeatherCount, tuning.BreakFeatherShape, look.Size * tuning.BreakFeatherSpread, tuning.BreakFeatherDuration);
                break;
            default:
                if (look.Form is Form.Eruption or Form.Hammer) effect.Ring(feet, look.Size, tuning.RingDuration);
                effect.Add(body, -Vector2.UnitY * tuning.PillarVelocity, look.Shape, look.Size * tuning.PillarSize / 4, tuning.PillarDuration, tuning.PillarLayer,
                    aspect: look.Shape == Shape.Pillar ? tuning.PillarAspect : tuning.PillarAspect2, growth: tuning.PillarGrowth);
                effect.Add(body, -Vector2.UnitY * tuning.FlashVelocity, tuning.FlashShape, look.Size * tuning.FlashSize / 3, tuning.FlashDuration, tuning.FlashLayer, core: tuning.FlashCore);
                effect.Scatter(body, tuning.SparkCount, tuning.SparkShape, look.Size * tuning.SparkSpread, tuning.SparkDuration);
                break;
        }
    }

}
