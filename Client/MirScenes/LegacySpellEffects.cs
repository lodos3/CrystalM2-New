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
        var tuning = SpellVfxAssets.Current.Legacy.Windup;
        var fields = SpellVfxAssets.Fields("Legacy", "Windup");
        int cast = frames * interval;
        return spell switch
        {
            Spell.ThunderBolt => new(SpellVisualPhase.Prepare, fields.Read(tuning.ThunderBoltDuration)),
            Spell.Revelation => new(SpellVisualPhase.Prepare, fields.Read(tuning.RevelationDuration)),
            Spell.LionRoar or Spell.BattleCry => new(SpellVisualPhase.Impact, fields.Read(tuning.LionRoarDuration)),
            Spell.FlameField => new(SpellVisualPhase.Impact, fields.Read(tuning.FlameFieldDuration), AtCell: true),
            Spell.ThunderStorm => new(SpellVisualPhase.Impact, cast, AtCell: true),
            Spell.Fury => new(SpellVisualPhase.Impact, fields.Read(tuning.FuryDuration) * interval),
            Spell.ImmortalSkin => new(SpellVisualPhase.Impact, cast * fields.Read(tuning.ImmortalSkinDuration)),
            Spell.UltimateEnhancer => new(SpellVisualPhase.Prepare, fields.Read(tuning.UltimateEnhancerDuration)),
            Spell.MagicBooster => new(SpellVisualPhase.Impact, fields.Read(tuning.MagicBoosterDuration) * interval),
            Spell.PetEnhancer => new(SpellVisualPhase.Prepare, fields.Read(tuning.PetEnhancerDuration) * interval),
            Spell.TwinDrakeBlade => new(SpellVisualPhase.Prepare, fields.Read(tuning.TwinDrakeBladeDuration)),
            Spell.Thrusting => new(SpellVisualPhase.Swing, (frames - frames / fields.Read(tuning.ThrustingDurationDivisor)) * interval, frames / fields.Read(tuning.ThrustingDelayDivisor) * interval),
            Spell.BladeAvalanche => new(SpellVisualPhase.Swing, fields.Read(tuning.BladeAvalancheDuration) * interval, AtCell: true),
            Spell.SlashingBurst => new(SpellVisualPhase.Swing, fields.Read(tuning.SlashingBurstDuration) * interval, fields.Read(tuning.SlashingBurstDelay), AtCell: true),
            Spell.CounterAttack => new(SpellVisualPhase.Swing, fields.Read(tuning.CounterAttackDuration) * interval),
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
        var tuning = SpellVfxAssets.Current.Legacy.FieldInterval;
        var fields = SpellVfxAssets.Fields("Legacy", "FieldInterval");
        return spell switch
        {
            Spell.HealingCircle => fields.Read(tuning.HealingCircleIntervalMs),
            Spell.FireWall or Spell.PoisonCloud => fields.Read(tuning.FireWallIntervalMs),
            _ => fields.Read(tuning.DefaultIntervalMs)
        };
    }

    private static Look Art(Form form, Shape shape, int size, int colour, int core) =>
        new(form, shape, size, Color.FromArgb(unchecked((int)0xff000000) | colour),
            Color.FromArgb(unchecked((int)0xff000000) | core));

    private static Look Profile(Spell spell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Current.Legacy.Profile;
        var fields = SpellVfxAssets.Fields("Legacy", "Profile");
        return spell switch
        {
            // Warrior. Use the angle and length of the hit to identify each weapon technique.
            Spell.Fencing => Art(fields.Read(tuning.FencingForm), fields.Read(tuning.FencingShape), fields.Read(tuning.FencingSize), fields.Read(tuning.FencingColour), fields.Read(tuning.FencingCore)),
            Spell.Slaying => Art(fields.Read(tuning.SlayingForm), fields.Read(tuning.SlayingShape), fields.Read(tuning.SlayingSize), fields.Read(tuning.SlayingColour), fields.Read(tuning.SlayingCore)),
            Spell.Thrusting => Art(fields.Read(tuning.ThrustingForm), fields.Read(tuning.ThrustingShape), fields.Read(tuning.ThrustingSize), fields.Read(tuning.ThrustingColour), fields.Read(tuning.ThrustingCore)),
            Spell.HalfMoon => Art(fields.Read(tuning.HalfMoonForm), fields.Read(tuning.HalfMoonShape), fields.Read(tuning.HalfMoonSize), fields.Read(tuning.HalfMoonColour), fields.Read(tuning.HalfMoonCore)),
            Spell.ShoulderDash => Art(fields.Read(tuning.ShoulderDashForm), fields.Read(tuning.ShoulderDashShape), fields.Read(tuning.ShoulderDashSize), fields.Read(tuning.ShoulderDashColour), fields.Read(tuning.ShoulderDashCore)),
            Spell.TwinDrakeBlade => Art(fields.Read(tuning.TwinDrakeBladeForm), fields.Read(tuning.TwinDrakeBladeShape), fields.Read(tuning.TwinDrakeBladeSize), fields.Read(tuning.TwinDrakeBladeColour), fields.Read(tuning.TwinDrakeBladeCore)),
            Spell.Entrapment => Art(fields.Read(tuning.EntrapmentForm), fields.Read(tuning.EntrapmentShape), fields.Read(tuning.EntrapmentSize), fields.Read(tuning.EntrapmentColour), fields.Read(tuning.EntrapmentCore)),
            Spell.FlamingSword => Art(fields.Read(tuning.FlamingSwordForm), fields.Read(tuning.FlamingSwordShape), fields.Read(tuning.FlamingSwordSize), fields.Read(tuning.FlamingSwordColour), fields.Read(tuning.FlamingSwordCore)),
            Spell.LionRoar => Art(fields.Read(tuning.LionRoarForm), fields.Read(tuning.LionRoarShape), fields.Read(tuning.LionRoarSize), fields.Read(tuning.LionRoarColour), fields.Read(tuning.LionRoarCore)),
            Spell.CrossHalfMoon => Art(fields.Read(tuning.CrossHalfMoonForm), fields.Read(tuning.CrossHalfMoonShape), fields.Read(tuning.CrossHalfMoonSize), fields.Read(tuning.CrossHalfMoonColour), fields.Read(tuning.CrossHalfMoonCore)),
            Spell.BladeAvalanche => Art(fields.Read(tuning.BladeAvalancheForm), fields.Read(tuning.BladeAvalancheShape), fields.Read(tuning.BladeAvalancheSize), fields.Read(tuning.BladeAvalancheColour), fields.Read(tuning.BladeAvalancheCore)),
            Spell.ProtectionField => Art(fields.Read(tuning.ProtectionFieldForm), fields.Read(tuning.ProtectionFieldShape), fields.Read(tuning.ProtectionFieldSize), fields.Read(tuning.ProtectionFieldColour), fields.Read(tuning.ProtectionFieldCore)),
            Spell.Rage => Art(fields.Read(tuning.RageForm), fields.Read(tuning.RageShape), fields.Read(tuning.RageSize), fields.Read(tuning.RageColour), fields.Read(tuning.RageCore)),
            Spell.CounterAttack => Art(fields.Read(tuning.CounterAttackForm), fields.Read(tuning.CounterAttackShape), fields.Read(tuning.CounterAttackSize), fields.Read(tuning.CounterAttackColour), fields.Read(tuning.CounterAttackCore)),
            Spell.SlashingBurst => Art(fields.Read(tuning.SlashingBurstForm), fields.Read(tuning.SlashingBurstShape), fields.Read(tuning.SlashingBurstSize), fields.Read(tuning.SlashingBurstColour), fields.Read(tuning.SlashingBurstCore)),
            Spell.Fury => Art(fields.Read(tuning.FuryForm), fields.Read(tuning.FuryShape), fields.Read(tuning.FurySize), fields.Read(tuning.FuryColour), fields.Read(tuning.FuryCore)),
            Spell.ImmortalSkin => Art(fields.Read(tuning.ImmortalSkinForm), fields.Read(tuning.ImmortalSkinShape), fields.Read(tuning.ImmortalSkinSize), fields.Read(tuning.ImmortalSkinColour), fields.Read(tuning.ImmortalSkinCore)),
            Spell.BattleCry => Art(fields.Read(tuning.BattleCryForm), fields.Read(tuning.BattleCryShape), fields.Read(tuning.BattleCrySize), fields.Read(tuning.BattleCryColour), fields.Read(tuning.BattleCryCore)),

            // Shared animations that previously used the same combat libraries.
            Spell.FatalSword or Spell.DoubleSlash or Spell.HeavenlySword or Spell.CrescentSlash or Spell.FlashDash
                => Art(fields.Read(tuning.FatalSwordForm), fields.Read(tuning.FatalSwordShape), fields.Read(tuning.FatalSwordSize), fields.Read(tuning.FatalSwordColour), fields.Read(tuning.FatalSwordCore)),
            Spell.PoisonSword or Spell.PoisonShot or Spell.SummonSnakes => Art(fields.Read(tuning.PoisonSwordForm), fields.Read(tuning.PoisonSwordShape), fields.Read(tuning.PoisonSwordSize), fields.Read(tuning.PoisonSwordColour), fields.Read(tuning.PoisonSwordCore)),
            Spell.Hemorrhage or Spell.VampireShot or Spell.SummonVampire or Spell.MPEater
                => Art(fields.Read(tuning.HemorrhageForm), fields.Read(tuning.HemorrhageShape), fields.Read(tuning.HemorrhageSize), fields.Read(tuning.HemorrhageColour), fields.Read(tuning.HemorrhageCore)),
            Spell.NapalmShot or Spell.ExplosiveTrap or Spell.DelayedExplosion or Spell.FireBurst
                => Art(fields.Read(tuning.NapalmShotForm), fields.Read(tuning.NapalmShotShape), fields.Read(tuning.NapalmShotSize), fields.Read(tuning.NapalmShotColour), fields.Read(tuning.NapalmShotCore)),
            Spell.ElementalBarrier or Spell.OneWithNature or Spell.ElementalShot
                => Art(fields.Read(tuning.ElementalBarrierForm), fields.Read(tuning.ElementalBarrierShape), fields.Read(tuning.ElementalBarrierSize), fields.Read(tuning.ElementalBarrierColour), fields.Read(tuning.ElementalBarrierCore)),
            Spell.Trap or Spell.Stonetrap or Spell.BindingShot or Spell.CrippleShot or Spell.CatTongue
                => Art(fields.Read(tuning.TrapForm), fields.Read(tuning.TrapShape), fields.Read(tuning.TrapSize), fields.Read(tuning.TrapColour), fields.Read(tuning.TrapCore)),
            Spell.MoonLight or Spell.MoonMist or Spell.DarkBody or Spell.Portal
                => Art(fields.Read(tuning.MoonLightForm), fields.Read(tuning.MoonLightShape), fields.Read(tuning.MoonLightSize), fields.Read(tuning.MoonLightColour), fields.Read(tuning.MoonLightCore)),
            Spell.SummonToad => Art(fields.Read(tuning.SummonToadForm), fields.Read(tuning.SummonToadShape), fields.Read(tuning.SummonToadSize), fields.Read(tuning.SummonToadColour), fields.Read(tuning.SummonToadCore)),
            Spell.Haste or Spell.LightBody or Spell.SwiftFeet => Art(fields.Read(tuning.HasteForm), fields.Read(tuning.HasteShape), fields.Read(tuning.HasteSize), fields.Read(tuning.HasteColour), fields.Read(tuning.HasteCore)),
            Spell.Concentration or Spell.Focus => Art(fields.Read(tuning.ConcentrationForm), fields.Read(tuning.ConcentrationShape), fields.Read(tuning.ConcentrationSize), fields.Read(tuning.ConcentrationColour), fields.Read(tuning.ConcentrationCore)),
            Spell.StraightShot or Spell.DoubleShot => Art(fields.Read(tuning.StraightShotForm), fields.Read(tuning.StraightShotShape), fields.Read(tuning.StraightShotSize), fields.Read(tuning.StraightShotColour), fields.Read(tuning.StraightShotCore)),
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
        Vector2? feet = null, float previousProgress = 0, bool followThrough = false, bool motionOnly = false)
    {
        using var emitterTime = SpellVfxAssets.AtProgress(progress);
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Current.Legacy.WeaponSweep;
        var fields = SpellVfxAssets.Fields("Legacy", "WeaponSweep");
        if (remaining <= 0) return;
        var look = Profile(spell);
        if (look.Size == 0) return;
        if (WarriorSpellEffects.UsesWeaponArt(spell))
        {
            WarriorSpellEffects.WeaponSweep(pool, spell, look, pose, last, facing,
                feet ?? pose.Hilt + Vector2.UnitY * fields.Read(tuning.Offset), now, remaining, interval, progress,
                previousProgress, followThrough, motionOnly);
            return;
        }
        var effect = new Effect(pool, look, now);
        int life = Math.Min(remaining, Math.Clamp(interval * fields.Read(tuning.Life), 65, 210));
        Vector2 blade = pose.Tip - pose.Hilt;
        Vector2 drift = last is { } previous ? pose.Tip - previous.Tip : blade * fields.Read(tuning.Drift);
        if (last is { } old && drift.LengthSquared() is > 4 and < 16000)
        {
            // Several tapered samples fill the area swept between the two real weapon poses.
            int pieces = Math.Clamp((int)drift.Length() / fields.Read(tuning.PiecesDivisor), 2, 7);
            for (int i = 0; i < pieces; i++)
            {
                float phase = (i + 1f) / pieces;
                Vector2 hilt = Vector2.Lerp(old.Hilt, pose.Hilt, phase);
                Vector2 tip = Vector2.Lerp(old.Tip, pose.Tip, phase);
                Vector2 edge = tip - hilt;
                int span = Math.Max(fields.Read(tuning.Span), (int)edge.Length() + fields.Read(tuning.Span2));
                effect.Add(hilt + edge * fields.Read(tuning.BladeEdgeOffset), Vector2.Zero, fields.Read(tuning.BladeEdgeShape), span, life, fields.Read(tuning.BladeEdgeLayer),
                    aspect: fields.Read(tuning.BladeEdgeAspect) / span, rotation: MathF.Atan2(edge.Y, edge.X),
                    alpha: (int)(fields.Read(tuning.BladeEdgeAlpha) + phase * fields.Read(tuning.BladeEdgeAlpha2)), emitter: "LegacySpellEffects.WeaponSweep.1");
            }

            int tipSpan = (int)drift.Length() + fields.Read(tuning.TipSpan);
            effect.Add((old.Tip + pose.Tip) * fields.Read(tuning.BladeEdgeOffset2), Vector2.Zero, fields.Read(tuning.BladeEdgeShape2), tipSpan, life,
                fields.Read(tuning.BladeEdgeLayer2), aspect: fields.Read(tuning.BladeEdgeAspect2) / tipSpan,
                rotation: MathF.Atan2(drift.Y, drift.X), core: fields.Read(tuning.BladeEdgeCore), alpha: fields.Read(tuning.BladeEdgeAlpha3), emitter: "LegacySpellEffects.WeaponSweep.2");
        }

        effect.Add(pose.Tip, drift * fields.Read(tuning.FlashVelocity), fields.Read(tuning.FlashShape), fields.Read(tuning.FlashSize), life, fields.Read(tuning.FlashLayer), core: fields.Read(tuning.FlashCore), emitter: "LegacySpellEffects.WeaponSweep.3");
    }

    internal static Vector2 ProjectileAnchor(Spell spell, Vector2 cell, int direction = -1, int pose = 0)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Current.Legacy.ProjectileAnchor;
        var fields = SpellVfxAssets.Fields("Legacy", "ProjectileAnchor");
        cell += SpellVfxLegacyFrames.ProjectileOffset(spell, direction, pose);
        return spell switch
        {
            Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch => cell - new Vector2(0, fields.Read(tuning.FireBallY)),
            Spell.SoulFireBall or Spell.SoulShield or Spell.BlessedArmour or Spell.MassHiding or
                Spell.Curse or Spell.Hallucination or Spell.Plague or Spell.PoisonCloud => cell - new Vector2(0, fields.Read(tuning.SoulFireBallY)),
            _ => cell - new Vector2(0, fields.Read(tuning.Y))
        };
    }

    internal static (int TravelPerCellMs, int WakeIntervalMs) ProjectileTiming(Spell spell)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.ProjectileTiming);
        return (tuning.TravelPerCellMs, tuning.WakeIntervalMs);
    }

    internal static void ProjectileFrame(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frame, int remaining, bool leaveWake, float size = WizardSpellEffects.DefaultSize, int direction = -1)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.ProjectileFrame);
        int poses = spell switch
        {
            Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.CatTongue => 6,
            Spell.FrostCrunch => 4,
            Spell.SoulFireBall or Spell.MassHiding or Spell.SoulShield or Spell.BlessedArmour or Spell.Curse or
                Spell.Hallucination or Spell.Plague or Spell.PoisonCloud => 3,
            _ => 0
        };
        if (poses != 0)
        {
            if (remaining <= 0) return;
            int facing = direction >= 0 ? direction : SpellVfxPoseMasks.Facing(heading, 16);
            int height = poses == 3 ? 36 : spell == Spell.CatTongue ? 24 : 47;
            Vector2 feet = head + new Vector2(0, height) - SpellVfxLegacyFrames.ProjectileOffset(spell, facing, frame);
            var particles = new SpellParticleFrame(pool, feet, now, 1, size, (uint)frame, depthY: feet.Y);
            SpellVfxPoseMasks.Draw(particles, spell, feet, facing, frame % poses, 1);
            if (spell == Spell.CatTongue) return;
        }
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
                effect.Add(head, Vector2.Zero, tuning.CatTongueBladeEdgeShape, look.Size, remaining, tuning.CatTongueBladeEdgeLayer,
                    aspect: tuning.CatTongueBladeEdgeAspect, rotation: angle, core: tuning.CatTongueBladeEdgeCore, emitter: "LegacySpellEffects.ProjectileFrame.1");
                break;
            default:
                effect.Add(head, Vector2.Zero, tuning.OrbShape, look.Size * tuning.OrbSize / tuning.OrbSizeDivisor, remaining, tuning.OrbLayer, alpha: tuning.OrbAlpha, emitter: "LegacySpellEffects.ProjectileFrame.2");
                effect.Add(head, Vector2.Zero, look.Shape, look.Size * tuning.AddSize / tuning.AddSizeDivisor, remaining,
                    tuning.AddLayer, aspect: tuning.AddAspect, rotation: angle + turn, core: tuning.AddCore, emitter: "LegacySpellEffects.ProjectileFrame.3");
                break;
        }

        if (!leaveWake) return;
        var wake = new Effect(pool, look, now);
        Vector2 side = new(-heading.Y, heading.X);
        float drift = (frame % 3 - 1) * tuning.Drift;
        wake.Add(head - heading * tuning.OrbOffset + side * drift, -heading * tuning.OrbVelocity + side * drift * tuning.OrbVelocity2,
            tuning.OrbShape2, look.Size / 2, Math.Min(remaining, tuning.Val2), tuning.OrbLayer2,
            aspect: tuning.OrbAspect, rotation: angle, growth: tuning.OrbGrowth, alpha: tuning.OrbAlpha2, emitter: "LegacySpellEffects.ProjectileFrame.4");
        wake.Add(head - heading * tuning.SparkOffset, -heading * tuning.SparkVelocity + side * drift, tuning.SparkShape, tuning.SparkSize,
            Math.Min(remaining, tuning.Val22), tuning.SparkLayer, rotation: angle + turn, core: tuning.SparkCore, emitter: "LegacySpellEffects.ProjectileFrame.5");
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
        var tuning = SpellVfxAssets.Current.Legacy.Emit;
        var fields = SpellVfxAssets.Fields("Legacy", "Emit");
        var look = Profile(visual);
        if (look.Size == 0) return;

        int reference = visual.Phase switch
        {
            SpellVisualPhase.Prepare => fields.Read(tuning.PrepareFactor),
            SpellVisualPhase.Aura => fields.Read(tuning.AuraFactor),
            SpellVisualPhase.Burn => fields.Read(tuning.BurnFactor),
            SpellVisualPhase.Swing or SpellVisualPhase.FollowThrough => fields.Read(tuning.SwingFactor),
            _ => fields.Read(tuning.Factor)
        };
        var effect = new Effect(pool, look, now, castsLight: visual.Motif == ParticleMotif.None,
            window: duration, referenceDuration: reference, sampleTime: sampleTime, fieldObjectID: fieldObjectID);
        Vector2 body = feet - Vector2.UnitY * fields.Read(tuning.Body);
        float angle = MathF.Atan2(facing.Y, facing.X);
        switch (visual.Phase)
        {
            case SpellVisualPhase.Prepare:
                Prepare(effect, look, body + facing * fields.Read(tuning.PrepareHand), angle);
                break;
            case SpellVisualPhase.Flight:
                effect.Add(body, facing * fields.Read(tuning.FlightOrbVelocity), fields.Read(tuning.FlightOrbShape), look.Size * fields.Read(tuning.FlightOrbSize) / 4, fields.Read(tuning.FlightOrbDuration), fields.Read(tuning.FlightOrbLayer),
                    aspect: fields.Read(tuning.FlightOrbAspect), alpha: fields.Read(tuning.FlightOrbAlpha), emitter: "LegacySpellEffects.Emit.1");
                effect.Add(body + facing * fields.Read(tuning.FlightAddOffset), facing * fields.Read(tuning.FlightAddVelocity), look.Shape, look.Size / 2, fields.Read(tuning.FlightAddDuration), fields.Read(tuning.FlightAddLayer),
                    aspect: fields.Read(tuning.FlightAddAspect), rotation: angle, core: fields.Read(tuning.FlightAddCore), emitter: "LegacySpellEffects.Emit.2");
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
                effect.Scatter(body, fields.Read(tuning.ExpireShardCount), fields.Read(tuning.ExpireShardShape), fields.Read(tuning.ExpireShardSpread), fields.Read(tuning.ExpireShardDuration), emitter: "LegacySpellEffects.Emit.3");
                break;
            case SpellVisualPhase.Burn:
                effect.Add(feet, Vector2.Zero, fields.Read(tuning.BurnHaloShape), fields.Read(tuning.BurnHaloSize), fields.Read(tuning.BurnHaloDuration), fields.Read(tuning.BurnHaloLayer), aspect: fields.Read(tuning.BurnHaloAspect), alpha: fields.Read(tuning.BurnHaloAlpha), emitter: "LegacySpellEffects.Emit.4");
                effect.Add(body + new Vector2(fields.Read(tuning.BurnX), fields.Read(tuning.BurnY)), new Vector2(fields.Read(tuning.BurnX2), fields.Read(tuning.BurnY2)), fields.Read(tuning.BurnWispShape), fields.Read(tuning.BurnWispSize), fields.Read(tuning.BurnWispDuration),
                    fields.Read(tuning.BurnWispLayer), aspect: fields.Read(tuning.BurnWispAspect), growth: fields.Read(tuning.BurnWispGrowth), emitter: "LegacySpellEffects.Emit.5");
                effect.Scatter(feet, fields.Read(tuning.BurnSparkCount), fields.Read(tuning.BurnSparkShape), fields.Read(tuning.BurnSparkSpread), fields.Read(tuning.BurnSparkDuration), rise: true, emitter: "LegacySpellEffects.Emit.6");
                break;
            case SpellVisualPhase.Ward:
                effect.Ring(feet, look.Size, fields.Read(tuning.WardRingDuration), emitter: "LegacySpellEffects.Emit.7");
                effect.Add(body, -Vector2.UnitY * fields.Read(tuning.WardShieldVelocity), fields.Read(tuning.WardShieldShape), fields.Read(tuning.WardShieldSize), fields.Read(tuning.WardShieldDuration), fields.Read(tuning.WardShieldLayer),
                    aspect: fields.Read(tuning.WardShieldAspect), growth: fields.Read(tuning.WardShieldGrowth), alpha: fields.Read(tuning.WardShieldAlpha), emitter: "LegacySpellEffects.Emit.8");
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
            tuning.AddDuration, tuning.AddLayer, aspect: tuning.AddAspect, rotation: angle * tuning.AddRotation, growth: tuning.AddGrowth, alpha: tuning.AddAlpha, emitter: "LegacySpellEffects.Prepare.1");
        effect.Add(hand, Vector2.Zero, tuning.OrbShape, Math.Max(tuning.Val12, look.Size / 4), tuning.OrbDuration,
            tuning.OrbLayer, delay: tuning.OrbDelay, core: tuning.OrbCore, emitter: "LegacySpellEffects.Prepare.2");
        effect.Scatter(hand, tuning.SparkCount, tuning.SparkShape, tuning.SparkSpread, tuning.SparkDuration, rise: true, emitter: "LegacySpellEffects.Prepare.3");
    }

    private static void Swing(Effect effect, Look look, SpellVisual visual, Vector2 feet, Vector2 body,
        Vector2 facing, float angle)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Swing);
        bool follow = visual.Phase == SpellVisualPhase.FollowThrough;
        if (visual.Spell == Spell.Thrusting)
        {
            effect.Add(body + facing * tuning.BoltOffset, facing * tuning.BoltVelocity, tuning.BoltShape, look.Size, tuning.BoltDuration, tuning.BoltLayer,
                aspect: tuning.BoltAspect, rotation: angle, core: tuning.BoltCore, emitter: "LegacySpellEffects.Swing.1");
            effect.Scatter(body + facing * tuning.SparkOffset, tuning.SparkCount, tuning.SparkShape, tuning.SparkSpread, tuning.SparkDuration, emitter: "LegacySpellEffects.Swing.2");
            return;
        }

        if (visual.Spell == Spell.BladeAvalanche)
        {
            for (int i = 0; i < tuning.ShardCount; i++)
                effect.Add(body + facing * (tuning.ShardOffset + i * tuning.ShardOffset2), facing * tuning.ShardVelocity, tuning.ShardShape, tuning.ShardSize + i * tuning.ShardSize2, tuning.ShardDuration, tuning.ShardLayer,
                    aspect: tuning.ShardAspect, rotation: angle + tuning.ShardRotation, delay: i * tuning.ShardDelay, core: i % 2 == 0, emitter: "LegacySpellEffects.Swing.3");
            return;
        }

        int cuts = visual.Spell switch { Spell.CrossHalfMoon => tuning.CrossHalfMoonFactor, Spell.TwinDrakeBlade => tuning.TwinDrakeBladeFactor, _ => 1 };
        for (int i = 0; i < cuts; i++)
        {
            float direction = angle + i * (visual.Spell == Spell.CrossHalfMoon ? MathF.PI / 2 : tuning.Direction) + (follow ? tuning.Direction2 : tuning.Direction3);
            Vector2 travel = new(MathF.Cos(direction), MathF.Sin(direction));
            effect.Add(body + travel * tuning.AddOffset, travel * tuning.AddVelocity, look.Shape, look.Size, tuning.AddDuration, tuning.AddLayer,
                aspect: tuning.AddAspect, rotation: direction, delay: i * tuning.AddDelay, core: follow || i == 1, emitter: "LegacySpellEffects.Swing.4");
        }
        if (visual.Spell is Spell.HalfMoon or Spell.CrossHalfMoon or Spell.SlashingBurst)
            effect.Ring(feet, look.Size, tuning.RingDuration, emitter: "LegacySpellEffects.Swing.5");
        if (visual.Spell == Spell.FlamingSword)
            effect.Add(body + facing * tuning.WispOffset, facing * tuning.WispVelocity - Vector2.UnitY * tuning.WispVelocity2, tuning.WispShape, tuning.WispSize, tuning.WispDuration, tuning.WispLayer, growth: tuning.WispGrowth, emitter: "LegacySpellEffects.Swing.6");
        effect.Scatter(body + facing * tuning.SparkOffset2, tuning.SparkCount2, tuning.SparkShape2, look.Size * tuning.SparkSpread2, tuning.SparkDuration2, emitter: "LegacySpellEffects.Swing.7");
    }

    private static void Aura(Effect effect, Look look, Spell spell, Vector2 feet, Vector2 body)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Aura);
        if (spell is Spell.Poisoning or Spell.Plague or Spell.Curse or Spell.Hiding)
        {
            effect.Add(body, -Vector2.UnitY * tuning.AddVelocity, look.Shape, look.Size * tuning.AddSize / 3, tuning.AddDuration, tuning.AddLayer, alpha: tuning.AddAlpha, growth: tuning.AddGrowth, emitter: "LegacySpellEffects.Aura.1");
            effect.Add(feet, -Vector2.UnitY * tuning.SparkVelocity, tuning.SparkShape, tuning.SparkSize, tuning.SparkDuration, tuning.SparkLayer, core: tuning.SparkCore, emitter: "LegacySpellEffects.Aura.2");
            return;
        }

        effect.Add(feet, Vector2.Zero, tuning.HaloShape, look.Size, tuning.HaloDuration, tuning.HaloLayer, aspect: tuning.HaloAspect, alpha: tuning.HaloAlpha, emitter: "LegacySpellEffects.Aura.3");
        effect.Add(body, -Vector2.UnitY * tuning.AddVelocity2, look.Shape, look.Size * tuning.AddSize2 / 4, tuning.AddDuration2, tuning.AddLayer2,
            aspect: tuning.AddAspect, alpha: tuning.AddAlpha2, emitter: "LegacySpellEffects.Aura.4");
        effect.Add(body + new Vector2(tuning.X, tuning.Y), new Vector2(tuning.X2, tuning.Y2), tuning.SparkShape2, tuning.SparkSize2, tuning.SparkDuration2,
            tuning.SparkLayer2, core: tuning.SparkCore2, emitter: "LegacySpellEffects.Aura.5");
    }

    private static void Field(Effect effect, Look look, Spell spell, Vector2 feet, Vector2 body, long now)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Legacy.Field);
        float drift = MathF.Sin(feet.X * tuning.Drift + feet.Y * tuning.Drift2 + now % 4096 * tuning.Drift3) * tuning.Drift4;
        switch (spell)
        {
            case Spell.PoisonCloud:
                effect.Add(feet + new Vector2(drift, tuning.PoisonCloudY), new Vector2(tuning.PoisonCloudX, tuning.PoisonCloudY2), tuning.PoisonCloudWispShape, tuning.PoisonCloudWispSize, tuning.PoisonCloudWispDuration,
                    tuning.PoisonCloudWispLayer, aspect: tuning.PoisonCloudWispAspect, alpha: tuning.PoisonCloudWispAlpha, growth: tuning.PoisonCloudWispGrowth, emitter: "LegacySpellEffects.Field.1");
                effect.Add(feet + new Vector2(-drift, tuning.PoisonCloudY3), new Vector2(tuning.PoisonCloudX2, tuning.PoisonCloudY4), tuning.PoisonCloudWispShape2, tuning.PoisonCloudWispSize2, tuning.PoisonCloudWispDuration2,
                    tuning.PoisonCloudWispLayer2, aspect: tuning.PoisonCloudWispAspect2, alpha: tuning.PoisonCloudWispAlpha2, growth: tuning.PoisonCloudWispGrowth2, emitter: "LegacySpellEffects.Field.2");
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
                        tuning.TrapHexagonBoltDuration, tuning.TrapHexagonBoltLayer, aspect: tuning.TrapHexagonBoltAspect, rotation: MathF.Atan2(edge.Y, edge.X), alpha: tuning.TrapHexagonBoltAlpha, emitter: "LegacySpellEffects.Field.3");
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
                    aspect: tuning.CleaveAddAspect, rotation: angle - tuning.CleaveAddRotation, core: tuning.CleaveAddCore, emitter: "LegacySpellEffects.Impact.1");
                if (spell is not (Spell.Fencing or Spell.SpiritSword))
                    effect.Scatter(body, tuning.CleaveSparkCount, tuning.CleaveSparkShape, tuning.CleaveSparkSpread, tuning.CleaveSparkDuration, emitter: "LegacySpellEffects.Impact.2");
                break;
            case Form.Rush:
                effect.Add(body, facing * tuning.RushBoltVelocity, tuning.RushBoltShape, look.Size, tuning.RushBoltDuration, tuning.RushBoltLayer, aspect: tuning.RushBoltAspect, rotation: angle, emitter: "LegacySpellEffects.Impact.3");
                effect.Add(feet, facing * tuning.RushWispVelocity, tuning.RushWispShape, tuning.RushWispSize, tuning.RushWispDuration, tuning.RushWispLayer, alpha: tuning.RushWispAlpha, emitter: "LegacySpellEffects.Impact.4");
                effect.Scatter(body, tuning.RushShardCount, tuning.RushShardShape, tuning.RushShardSpread, tuning.RushShardDuration, emitter: "LegacySpellEffects.Impact.5");
                break;
            case Form.Heal:
            case Form.Totem:
            case Form.Ward:
                effect.Ring(feet, look.Size, tuning.WardRingDuration, emitter: "LegacySpellEffects.Impact.6");
                effect.Add(body, -Vector2.UnitY * tuning.WardAddVelocity, look.Shape, look.Size * tuning.WardAddSize / 4, tuning.WardAddDuration,
                    tuning.WardAddLayer, aspect: tuning.WardAddAspect, growth: tuning.WardAddGrowth, emitter: "LegacySpellEffects.Impact.7");
                effect.Add(body, -Vector2.UnitY * tuning.WardFlashVelocity, tuning.WardFlashShape, look.Size / 2, tuning.WardFlashDuration, tuning.WardFlashLayer, core: tuning.WardFlashCore, emitter: "LegacySpellEffects.Impact.8");
                effect.Scatter(body, tuning.WardFeatherCount, look.Form == Form.Totem ? look.Shape : tuning.WardFeatherShape, look.Size * tuning.WardFeatherSpread, tuning.WardFeatherDuration, rise: true, emitter: "LegacySpellEffects.Impact.9");
                break;
            case Form.Bind:
                effect.Ring(feet, look.Size, tuning.BindRingDuration, emitter: "LegacySpellEffects.Impact.10");
                if (spell == Spell.Entrapment)
                {
                    for (int i = 0; i < tuning.BindBoltCount; i++)
                        effect.Add(body + new Vector2(0, i * tuning.BindY - tuning.BindY2), Vector2.Zero, tuning.BindBoltShape, tuning.BindBoltSize, tuning.BindBoltDuration,
                            tuning.BindBoltLayer, aspect: tuning.BindBoltAspect, rotation: i == 1 ? tuning.BindBoltRotation : tuning.BindBoltRotation2, delay: i * tuning.BindBoltDelay, core: i == 1, emitter: "LegacySpellEffects.Impact.11");
                }
                else
                {
                    effect.Add(body, -Vector2.UnitY * tuning.BindAddVelocity, look.Shape, look.Size * tuning.BindAddSize / 4, tuning.BindAddDuration,
                        tuning.BindAddLayer, core: tuning.BindAddCore, emitter: "LegacySpellEffects.Impact.12");
                }
                break;
            case Form.Veil:
                effect.Ring(feet, look.Size, tuning.VeilRingDuration, emitter: "LegacySpellEffects.Impact.13");
                for (int i = -1; i <= tuning.VeilWispCount; i++)
                    effect.Add(body + new Vector2(i * tuning.VeilX, tuning.VeilY), new Vector2(i * tuning.VeilX2, tuning.VeilY2), tuning.VeilWispShape, look.Size * tuning.VeilWispSize / 4,
                        tuning.VeilWispDuration, tuning.VeilWispLayer, growth: tuning.VeilWispGrowth, alpha: tuning.VeilWispAlpha, emitter: "LegacySpellEffects.Impact.14");
                effect.Add(body, -Vector2.UnitY * tuning.VeilAddVelocity, look.Shape, look.Size / 2, tuning.VeilAddDuration, tuning.VeilAddLayer, core: tuning.VeilAddCore, emitter: "LegacySpellEffects.Impact.15");
                break;
            case Form.Break:
                effect.Ring(feet, look.Size, tuning.BreakRingDuration, emitter: "LegacySpellEffects.Impact.16");
                effect.Add(body, Vector2.Zero, look.Shape, look.Size, tuning.BreakAddDuration, tuning.BreakAddLayer,
                    aspect: tuning.BreakAddAspect, growth: tuning.BreakAddGrowth, alpha: tuning.BreakAddAlpha, core: tuning.BreakAddCore, emitter: "LegacySpellEffects.Impact.17");
                effect.Scatter(body, tuning.BreakFeatherCount, tuning.BreakFeatherShape, look.Size * tuning.BreakFeatherSpread, tuning.BreakFeatherDuration, emitter: "LegacySpellEffects.Impact.18");
                break;
            default:
                if (look.Form is Form.Eruption or Form.Hammer) effect.Ring(feet, look.Size, tuning.RingDuration, emitter: "LegacySpellEffects.Impact.19");
                effect.Add(body, -Vector2.UnitY * tuning.PillarVelocity, look.Shape, look.Size * tuning.PillarSize / 4, tuning.PillarDuration, tuning.PillarLayer,
                    aspect: look.Shape == Shape.Pillar ? tuning.PillarAspect : tuning.PillarAspect2, growth: tuning.PillarGrowth, emitter: "LegacySpellEffects.Impact.20");
                effect.Add(body, -Vector2.UnitY * tuning.FlashVelocity, tuning.FlashShape, look.Size * tuning.FlashSize / 3, tuning.FlashDuration, tuning.FlashLayer, core: tuning.FlashCore, emitter: "LegacySpellEffects.Impact.21");
                effect.Scatter(body, tuning.SparkCount, tuning.SparkShape, look.Size * tuning.SparkSpread, tuning.SparkDuration, emitter: "LegacySpellEffects.Impact.22");
                break;
        }
    }

}
