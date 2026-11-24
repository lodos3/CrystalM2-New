using System.Numerics;
using Client.MirScenes;
using Client.MirObjects;
using CrystalM2.Rendering.Core;

namespace CrystalM2.ParticleStudio;

internal readonly record struct PreviewRig(Vector2 Centre, Vector2 Facing, MirDirection Direction)
{
    public Func<float, WeaponMaskTrace?>? ReferenceWeapon { get; init; }
    public bool WeaponImbue { get; init; }
    public bool WeaponTrail { get; init; }

    public Vector2 Caster => Centre - Facing * 78;
    public Vector2 Target => Centre + Facing * 78;

    public WeaponMaskTrace Weapon(float progress)
    {
        if (ReferenceWeapon?.Invoke(progress) is { } trace) return trace;
        float angle = MathF.Atan2(Facing.Y, Facing.X) + (progress - .5f) * 1.9f;
        Vector2 hilt = Centre + Facing * 15 - Vector2.UnitY * 32;
        return new WeaponMaskTrace(hilt, hilt + new Vector2(MathF.Cos(angle) * 44, MathF.Sin(angle) * 34 - 9));
    }
}

internal sealed record PreviewStage(string Name, int Duration, int Delay,
    Action<SpellParticlePool, PreviewRig, int> Sample, MirAction ActorAction, Action? InspectTiming)
{
    public BladePoseCue? BladePose { get; init; }
    public bool InFullSequence { get; init; } = true;
}

internal sealed class PreviewSequence
{
    private const uint Seed = 1729;
    private const int FieldWindow = 2400;
    private readonly Spell _spell;
    private readonly List<PreviewStage> _stages = [];

    public PreviewSequence(SpellVfxEntry entry)
    {
        _spell = entry.Spell;
        if (SpellVfxBladeFrames.Supports(_spell)) AddBladeParticles();
        else if (SpecialisationAbilities.TryGet(_spell, out var ability)) AddSignature(ability);
        else AddStandard(entry.HasStandaloneCast);
    }

    public IReadOnlyList<PreviewStage> Stages => _stages;

    private void AddBladeParticles()
    {
        if (_spell == Spell.TwinDrakeBlade)
            _stages.Add(new PreviewStage("Activation", SpellVfxBladeFrames.DrakePrepare.Duration, 0,
                (pool, rig, elapsed) => SampleBladeBase(pool, rig, elapsed, SpellVfxBladeFrames.DrakePrepare, SpellVisualPhase.Prepare),
                MirAction.Standing, null) { BladePose = SpellVfxBladeFrames.DrakePrepare });

        AddAttack("Attack 1", MirAction.Attack1, inFullSequence: true);
        AddAttack(_spell == Spell.TwinDrakeBlade ? "Attack 4 (follow-through)" : "Attack 4 (alternate)",
            MirAction.Attack4, inFullSequence: _spell == Spell.TwinDrakeBlade);

        if (_spell == Spell.TwinDrakeBlade)
            _stages.Add(new PreviewStage("Stun proc (target)", SpellVfxBladeFrames.DrakeStun.Duration, 0,
                (pool, rig, elapsed) => SampleBladeBase(pool, rig, elapsed, SpellVfxBladeFrames.DrakeStun, SpellVisualPhase.Hit),
                MirAction.Standing, null)
                { BladePose = SpellVfxBladeFrames.DrakeStun, InFullSequence = false });
        else
            AddFrame("Burn perk (target)", SpellVisualPhase.Burn, inFullSequence: false);

        _stages.Add(new PreviewStage("Readied weapon (bonus)", 700, 0, SampleReadyBlade, MirAction.Standing, null)
            { InFullSequence = false });

        void AddAttack(string name, MirAction action, bool inFullSequence)
        {
            if (SpellVfxBladeFrames.Attack(_spell, action) is not { } cue) return;
            var phase = action == MirAction.Attack4 ? SpellVisualPhase.FollowThrough : SpellVisualPhase.Swing;
            _stages.Add(new PreviewStage(name, cue.Duration, 0, (pool, rig, elapsed) =>
            {
                SampleBladeExtras(pool, rig, elapsed, cue);
                if (_spell == Spell.FlamingSword)
                {
                    int lastElapsed = -1;
                    SpellVfxBladeParticles.EmitFlameMotion(pool, rig.Centre, rig.Facing, rig.Direction,
                        pose => rig.ReferenceWeapon?.Invoke((pose + .5f) / cue.FrameCount),
                        elapsed, elapsed, cue.FrameInterval, ref lastElapsed, SpellParticleFrame.ReferenceSize, Seed);
                }
                SampleBladeBase(pool, rig, elapsed, cue, phase);
            }, MirAction.Attack1, null) { BladePose = cue, InFullSequence = inFullSequence });
        }
    }

    private void SampleReadyBlade(SpellParticlePool pool, PreviewRig rig, int elapsed)
    {
        if (rig.WeaponImbue && rig.ReferenceWeapon?.Invoke(0) is { } pose)
            WarriorSpellEffects.DrawWeaponEnchant(pool, _spell, pose, rig.Facing, elapsed,
                SpellParticleFrame.ReferenceSize, Seed, false, rig.Centre.Y);
    }

    private void SampleBladeBase(SpellParticlePool pool, PreviewRig rig, int elapsed, BladePoseCue cue, SpellVisualPhase phase)
    {
        int pose = cue.FrameAt(elapsed);
        var weapon = _spell == Spell.FlamingSword && pose is 1 or 2
            ? rig.ReferenceWeapon?.Invoke((pose + .5f) / cue.FrameCount)
            : null;
        SpellVfxBladeParticles.Draw(pool, _spell, phase, rig.Centre, rig.Direction, pose,
            elapsed / (float)cue.Duration, elapsed, SpellParticleFrame.ReferenceSize, Seed, weapon);
    }

    private void SampleBladeExtras(SpellParticlePool pool, PreviewRig rig, int elapsed, BladePoseCue cue)
    {
        if (rig.ReferenceWeapon is not { } weapon) return;

        if (rig.WeaponTrail)
        {
            WeaponMaskTrace? previous = null;
            for (int tick = 0; tick <= elapsed; tick += cue.FrameInterval)
            {
                pool.Advance(tick);
                float progress = (cue.FrameAt(tick) + .5f) / cue.FrameCount;
                var pose = weapon(progress);
                if (pose is { } current && previous is { } prior)
                    LegacySpellEffects.WeaponSweep(pool, _spell, current, prior, tick, cue.Duration - tick,
                        cue.FrameInterval, rig.Facing, progress, rig.Centre, motionOnly: true);
                previous = pose;
            }
            pool.Advance(elapsed);
        }

        if (rig.WeaponImbue && weapon((cue.FrameAt(elapsed) + .5f) / cue.FrameCount) is { } blade)
            WarriorSpellEffects.DrawWeaponEnchant(pool, _spell, blade, rig.Facing, elapsed,
                SpellParticleFrame.ReferenceSize, Seed, true, rig.Centre.Y);
    }

    private void AddStandard(bool standalone)
    {
        Frame cast = FrameSet.Player[MirAction.Spell];
        var cue = LegacySpellEffects.Windup(_spell, cast.Count, cast.Interval);
        if (standalone && cue.Duration > 0)
            AddFrame(cue.Phase == SpellVisualPhase.Prepare ? "Prepare" : "Cast", cue.Phase, cue.Duration, cue.Delay);

        if (WarriorSpellEffects.UsesWeaponArt(_spell))
        {
            Add("Weapon swing", 600, (pool, rig, elapsed) => SampleWeapon(pool, rig, elapsed, followThrough: false), action: MirAction.Attack1);
            if (_spell == Spell.CrossHalfMoon)
                Add("Follow-through", 600, (pool, rig, elapsed) => SampleWeapon(pool, rig, elapsed, followThrough: true), action: MirAction.Attack4);
        }
        else if (_spell == Spell.ShoulderDash)
            Add("Dash", 500, (pool, rig, elapsed) =>
            {
                WarriorSpellEffects.Dash(pool, rig.Caster, rig.Target, 0);
                pool.Advance(elapsed);
            });

        if (HasProjectile(_spell))
        {
            var (travel, _) = LegacySpellEffects.ProjectileTiming(_spell);
            Add("Projectile", 4 * travel, SampleProjectile);
        }

        if (_spell is Spell.Lightning or Spell.ThunderBolt or Spell.Vampirism)
        {
            int duration = _spell == Spell.Lightning ? WizardSpellEffects.LightningDuration : WizardSpellEffects.RelayDuration(_spell);
            Add("Relay arc", duration, (pool, rig, elapsed) =>
                WizardSpellEffects.DrawArc(pool, _spell, rig.Caster, rig.Target, elapsed, 0, duration,
                    SpellParticleFrame.ReferenceSize, Seed));
        }

        if (_spell is Spell.Teleport or Spell.Blink or Spell.StormEscape)
        {
            AddFrame("Departure", SpellVisualPhase.Departure);
            AddFrame("Arrival", SpellVisualPhase.Arrival, atTarget: true);
        }
        else if (standalone || WarriorSpellEffects.UsesWeaponArt(_spell))
        {
            var phase = LegacySpellEffects.ImpactPhase(_spell);
            AddFrame(phase == SpellVisualPhase.Contact ? "Contact" : "Impact", phase,
                atTarget: HasProjectile(_spell));
        }

        if (_spell is Spell.SoulFireBall or Spell.FireWall or Spell.FireBall or Spell.GreatFireBall)
            AddFrame("Burn", SpellVisualPhase.Burn);
        if (_spell is Spell.MagicShield or Spell.EnergyShield or Spell.ProtectionField or Spell.Rage or Spell.Fury or
            Spell.ImmortalSkin or Spell.Hiding or Spell.PetEnhancer)
            AddFrame("Aura", SpellVisualPhase.Aura);
        if (MapControl.HasGroundSpellParticles(_spell))
            AddFrame("Ground field", SpellVisualPhase.Field, FieldWindow);
        if (_spell is Spell.Mirroring or Spell.EnergyShield or Spell.HealingCircle)
            AddFrame("Expire", SpellVisualPhase.Expire);
    }

    private void AddSignature(SpecialisationAbility ability)
    {
        Add("Cast", Math.Max(400, ability.Windup + 250), (pool, rig, elapsed) =>
        {
            SignatureSpellEffects.Cast(pool, ability, rig.Caster,
                ability.Target == AbilityTarget.Self ? rig.Caster : rig.Target, rig.Direction, 0);
            pool.Advance(elapsed);
        }, action: MirAction.Spell);
        if (ability.Windup > 0 && ability.Target != AbilityTarget.Self)
            AddCell("Target warning", SpellEffect.SignatureWindup, ability.Windup + 65);
        if (ability.Target == AbilityTarget.Ground || ability.Theme == AbilityTheme.Ward)
            AddCell("Armed marker", SpellEffect.SignatureArmed, 1350);
        AddCell("Impact", SpellEffect.SignatureImpact, 920);

        void AddCell(string name, SpellEffect phase, int duration) =>
            Add(name, duration, (pool, rig, elapsed) =>
            {
                SignatureSpellEffects.Cell(pool, ability,
                    ability.Target == AbilityTarget.Self ? rig.Centre : rig.Target, phase, 0);
                pool.Advance(elapsed);
            });
    }

    private void AddFrame(string name, SpellVisualPhase phase, int? duration = null, int delay = 0, bool atTarget = false, bool inFullSequence = true)
    {
        var visual = new SpellVisual(_spell, phase);
        int window = duration ?? LegacySpellEffects.PacketDuration(visual);
        Add(name, window, (pool, rig, elapsed) =>
        {
            Vector2 feet = atTarget ? rig.Target : rig.Centre;
            if (phase is SpellVisualPhase.Contact or SpellVisualPhase.Impact)
            {
                SpellImpactDebris.Emit(pool, visual, feet, rig.Facing, 0, 100, 1, 1, Seed);
                pool.Advance(elapsed);
            }
            if (WarriorSpellEffects.HasArt(_spell))
                WarriorSpellEffects.DrawFrame(pool, visual, feet, rig.Facing, elapsed, 0, window,
                    seed: Seed, weaponTip: rig.Weapon(.32f).Tip);
            else if (_spell == Spell.PoisonCloud && phase == SpellVisualPhase.Field)
                TaoistSpellEffects.DrawPoisonField(pool, feet, elapsed, 0, SpellParticleFrame.ReferenceSize, Seed, 0, Vector2.Zero, 2);
            else if (TaoistSpellEffects.HasArt(_spell))
                TaoistSpellEffects.DrawFrame(pool, visual, feet, rig.Facing, elapsed, 0, window, seed: Seed);
            else
                WizardSpellEffects.DrawFrame(pool, visual, feet, rig.Facing, elapsed, 0, window, seed: Seed);
        }, delay, phase switch
        {
            SpellVisualPhase.Prepare => MirAction.Spell,
            SpellVisualPhase.Swing => MirAction.Attack1,
            _ => MirAction.Standing
        }, duration == null ? () => LegacySpellEffects.PacketDuration(visual) : null, inFullSequence);
    }

    private void SampleWeapon(SpellParticlePool pool, PreviewRig rig, int elapsed, bool followThrough)
    {
        int step = FrameSet.Player[followThrough ? MirAction.Attack4 : MirAction.Attack1].Interval;
        // The live Attack1 path draws these spells through DrawSwing, not
        // the older WeaponSweep presentation. Preview that same path.
        if (!followThrough && SpellVfxPoseMasks.IsWeaponSwing(_spell))
        {
            pool.Advance(elapsed);
            using var profile = SpellVfxAssets.For(_spell);
            var frame = new SpellParticleFrame(pool, rig.Centre, elapsed, 1,
                SpellParticleFrame.ReferenceSize, Seed, depthY: rig.Centre.Y);
            int pose = Math.Clamp(elapsed / Math.Max(1, step), 0, 5);
            SpellVfxPoseMasks.DrawSwing(frame, _spell, rig.Centre, (int)rig.Direction, pose / 6f);
            return;
        }
        for (int tick = 0; tick <= elapsed; tick += step)
        {
            pool.Advance(tick);
            float progress = tick / 600f;
            var current = rig.Weapon(progress);
            WeaponMaskTrace? previous = tick == 0 ? null : rig.Weapon((tick - step) / 600f);
            LegacySpellEffects.WeaponSweep(pool, _spell, current, previous, tick, 600 - tick, step, rig.Facing,
                progress, rig.Centre, tick == 0 ? 0 : (tick - step) / 600f, followThrough);
        }
        pool.Advance(elapsed);
    }

    private void SampleProjectile(SpellParticlePool pool, PreviewRig rig, int elapsed)
    {
        var (travel, wake) = LegacySpellEffects.ProjectileTiming(_spell);
        var timing = new LegacyProjectileTimeline(0, 4, 30) { TravelMillisecondsPerCell = travel };
        int interval = Math.Max(1, wake);
        int first = Math.Max(0, (elapsed - 240) / interval * interval);
        for (int tick = first; tick < elapsed; tick += interval)
        {
            pool.Advance(tick);
            Draw(tick, true);
        }
        pool.Advance(elapsed);
        Draw(elapsed, elapsed % interval == 0);

        void Draw(int tick, bool leaveWake)
        {
            var head = LegacySpellEffects.ProjectileAnchor(_spell, Vector2.Lerp(rig.Caster, rig.Target, timing.Progress(tick)));
            LegacySpellEffects.ProjectileFrame(pool, _spell, head, rig.Facing, tick, timing.Frame(tick),
                (int)(timing.Impact - tick), leaveWake);
        }
    }

    private void Add(string name, int duration, Action<SpellParticlePool, PreviewRig, int> sample,
        int delay = 0, MirAction action = MirAction.Standing, Action? inspectTiming = null, bool inFullSequence = true)
    {
        if (duration > 0)
            _stages.Add(new PreviewStage(name, duration, Math.Max(0, delay), sample, action, inspectTiming)
                { InFullSequence = inFullSequence });
    }

    private static bool HasProjectile(Spell spell) => spell is
        Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch or
        Spell.SoulFireBall or Spell.MassHiding or Spell.SoulShield or Spell.BlessedArmour or Spell.Curse or
        Spell.Hallucination or Spell.Plague or Spell.PoisonCloud;
}
