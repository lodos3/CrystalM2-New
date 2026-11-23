using System.Numerics;
using Client.MirGraphics;
using Client.MirScenes;
using CrystalM2.Rendering.Core;

namespace Client.MirObjects;

public partial class PlayerObject
{
    private WeaponMaskTrace? _lastWeaponPose;
    private Spell _weaponPoseSpell;
    private MirAction _weaponPoseAction;
    private int _weaponPoseFrame = -1, _weaponPoseIndex = -1, _weaponPoseMap = -1;
    private BodyCoat _bodyCoat;
    private int _lastFlameElapsed = -1;

    [Flags]
    private enum BodyCoat { None = 0, Protection = 1, Rage = 2, ImmortalSkin = 4, Fury = 8 }

    private Spell BladeSpell => Settings.Effect && LegacySpellEffects.UsesWeaponMask(Spell) &&
        (CurrentAction is MirAction.Attack1 or MirAction.Attack4 ||
            CurrentAction == MirAction.Spell && Cast) ? Spell : Spell.None;

    private Spell ReadiedBlade => this is UserObject actor
        ? actor.FlamingSword ? Spell.FlamingSword : actor.TwinDrakeBlade ? Spell.TwinDrakeBlade : Spell.None
        : Spell.None;

    private void ProcessBladeSpell()
    {
        if (!Settings.Effect || Hidden || Dead ||
            SpellVfxBladeFrames.Attack(Spell, CurrentAction) is not { } cue ||
            (uint)FrameIndex >= (uint)cue.FrameCount) return;

        float withinFrame = FrameInterval > 0
            ? 1 - Math.Clamp((NextMotion - CMain.Time) / (float)FrameInterval, 0, 1)
            : 0;
        var map = GameScene.Scene.MapControl;
        map.EmitBladeAttack(this, Spell,
            CurrentAction == MirAction.Attack4 ? SpellVisualPhase.FollowThrough : SpellVisualPhase.Swing,
            FrameIndex, (FrameIndex + withinFrame) / cue.FrameCount);
        if (Spell == Spell.FlamingSword)
            map.EmitFlamingSwordMotion(this, (int)((FrameIndex + withinFrame) * FrameInterval),
                Math.Max(1, FrameInterval), ref _lastFlameElapsed);
    }

    private void ProcessWeaponSpellVisual()
    {
        Spell spell = BladeSpell;
        bool legacyBlade = SpellVfxBladeFrames.Supports(spell);
        if (spell == Spell.None || Frame == null || (legacyBlade &&
            (!Settings.WeaponSpellTrail || !SpellVfxBladeFrames.Attack(spell, CurrentAction).HasValue)))
        {
            _lastWeaponPose = null;
            _weaponPoseFrame = -1;
            return;
        }

        MapControl map = GameScene.Scene.MapControl;
        if (_weaponPoseMap != map.SpellParticleMapGeneration)
        {
            _weaponPoseMap = map.SpellParticleMapGeneration;
            _weaponPoseFrame = -1;
            _lastWeaponPose = null;
        }
        int index = DrawFrame + WeaponOffSet;
        if (_weaponPoseFrame == index && _weaponPoseAction == CurrentAction && _weaponPoseSpell == spell) return;
        if (_weaponPoseAction != CurrentAction || _weaponPoseSpell != spell || FrameIndex <= _weaponPoseIndex)
            _lastWeaponPose = null;
        float previousProgress = _lastWeaponPose.HasValue ? (_weaponPoseIndex + .5f) / Math.Max(1, Frame.Count) : 0;
        _weaponPoseFrame = index;
        _weaponPoseIndex = FrameIndex;
        _weaponPoseAction = CurrentAction;
        _weaponPoseSpell = spell;

        var hand = new Vector2(DrawLocation.X + MapControl.CellWidth / 2, DrawLocation.Y - 24);
        WeaponMaskTrace? trace = WeaponTrace(index);
        float progress = (FrameIndex + .5f) / Math.Max(1, Frame.Count);
        if (trace == null && !legacyBlade && WarriorSpellEffects.UsesWeaponArt(spell))
        {
            float angle = ((int)Direction - 2) * MathF.PI / 4 + (progress - .5f) * 1.55f;
            trace = new WeaponMaskTrace(hand, hand + new Vector2(MathF.Cos(angle) * 27, MathF.Sin(angle) * 18));
        }
        if (trace == null)
        {
            _lastWeaponPose = null;
            return;
        }

        WeaponMaskTrace? prior = _lastWeaponPose is { } world ? map.WeaponPoseOnScreen(world) : null;
        int remaining = (int)Math.Max(1, NextMotion - CMain.Time + Math.Max(0, Frame.Count - FrameIndex - 1) * FrameInterval);
        Point feet = new(DrawLocation.X + MapControl.CellWidth / 2, DrawLocation.Y + MapControl.CellHeight / 2);
        map.EmitWeaponSweep(spell, trace.Value.Orient(hand, prior), _lastWeaponPose, feet, Direction, progress,
            previousProgress, remaining, FrameInterval, out var pose, CurrentAction == MirAction.Attack4);
        _lastWeaponPose = pose;
    }

    private WeaponMaskTrace? WeaponTrace(int index) =>
        Weapon >= 0 ? WeaponLibrary1?.GetWeaponTrace(index, DrawLocation) : null;

    internal WeaponMaskTrace? EquippedWeaponTrace() => WeaponTraceAt(FrameIndex);

    internal WeaponMaskTrace? WeaponTraceAt(int pose)
    {
        var hand = new Vector2(DrawLocation.X + MapControl.CellWidth / 2, DrawLocation.Y - 24);
        return WeaponTrace(DrawFrame - FrameIndex + pose + WeaponOffSet)?.Orient(hand, null);
    }

    private void ProcessSpellCoatings()
    {
        var map = GameScene.Scene.MapControl;
        BodyCoat coat = BodyCoat.None;
        if (Settings.Effect && !Dead && Class == MirClass.Warrior)
        {
            long now = CMain.Time;
            var buffs = this == User ? GameScene.Scene.BuffsDialog?.Buffs :
                this == GameScene.Hero ? GameScene.Scene.HeroBuffsDialog?.Buffs : null;
            if (buffs != null)
            {
                foreach (ClientBuff buff in buffs)
                    if (!buff.Paused && (buff.Infinite || buff.ExpireTime > now))
                        coat |= Coat(buff.Type);
            }
            else
            {
                foreach (BuffType buff in Buffs) coat |= Coat(buff);
            }

            foreach (Effect effect in Effects)
            {
                if (effect is BuffEffect || now < effect.Start || now - effect.Start >= effect.Duration) continue;
                coat |= effect.Art.Visual.Spell switch
                {
                    Spell.ProtectionField => BodyCoat.Protection,
                    Spell.Rage => BodyCoat.Rage,
                    Spell.ImmortalSkin => BodyCoat.ImmortalSkin,
                    Spell.Fury => BodyCoat.Fury,
                    _ => BodyCoat.None
                };
            }
        }
        if (coat != BodyCoat.None || coat != _bodyCoat) map.TextureValid = false;
        _bodyCoat = coat;

        if (!Settings.Effect || !Settings.WeaponSpellImbue || Dead || RidingMount || Hidden) return;
        Spell swing = BladeSpell;
        bool striking = SpellVfxBladeFrames.Attack(swing, CurrentAction).HasValue;
        Spell enchant = striking ? swing : ReadiedBlade;
        if (enchant != Spell.None && EquippedWeaponTrace() is { } screen)
            map.EmitWeaponEnchant(enchant, map.WeaponPoseInWorld(screen), this, striking);

        static BodyCoat Coat(BuffType type) => type switch
        {
            BuffType.ProtectionField => BodyCoat.Protection,
            BuffType.Rage => BodyCoat.Rage,
            BuffType.ImmortalSkin => BodyCoat.ImmortalSkin,
            BuffType.Fury => BodyCoat.Fury,
            _ => BodyCoat.None
        };
    }

    private void DrawBodySpellVisual(MLibrary library, int index)
    {
        float intensity = DXManager.Opacity * Settings.SpellBlendIntensityPercent / 100f;
        if ((_bodyCoat & BodyCoat.Protection) != 0)
        {
            using var vfxScope = SpellVfxAssets.For(Spell.ProtectionField);
            var tuning = SpellVfxAssets.Current.Warrior.ActorMasks;
            library.DrawSpellMask(index, DrawLocation, tuning.ProtectionFieldColour, tuning.ProtectionFieldCore,
                intensity * tuning.ProtectionFieldIntensity, SpellMaskStyle.Storm);
        }
        if ((_bodyCoat & BodyCoat.ImmortalSkin) != 0)
        {
            using var vfxScope = SpellVfxAssets.For(Spell.ImmortalSkin);
            var tuning = SpellVfxAssets.Current.Warrior.ActorMasks;
            library.DrawSpellMask(index, DrawLocation, tuning.ImmortalSkinColour, tuning.ImmortalSkinCore,
                intensity * tuning.ImmortalSkinIntensity, SpellMaskStyle.Frost);
        }
        if ((_bodyCoat & BodyCoat.Rage) != 0)
        {
            using var vfxScope = SpellVfxAssets.For(Spell.Rage);
            var tuning = SpellVfxAssets.Current.Warrior.ActorMasks;
            library.DrawSpellMask(index, DrawLocation, tuning.RageColour, tuning.RageCore,
                intensity * tuning.RageIntensity, SpellMaskStyle.Fire);
        }
        if ((_bodyCoat & BodyCoat.Fury) != 0)
        {
            using var vfxScope = SpellVfxAssets.For(Spell.Fury);
            var tuning = SpellVfxAssets.Current.Warrior.ActorMasks;
            library.DrawSpellMask(index, DrawLocation, tuning.FuryColour, tuning.FuryCore,
                intensity * tuning.FuryIntensity, SpellMaskStyle.Sweep);
        }
    }

    private void DrawWeaponSpellVisual(int index)
    {
        if (!Settings.Effect || Dead) return;
        Spell spell = BladeSpell;
        if (spell == Spell.None || SpellVfxBladeFrames.Supports(spell)) return;
        var (colour, core) = LegacySpellEffects.WeaponColours(spell);
        float phase = Frame == null ? .5f : (FrameIndex + .5f) / Math.Max(1, Frame.Count);
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Current.Warrior.ActorMasks;
        float pulse = tuning.SwingPulseBase + tuning.SwingPulseGain * MathF.Sin(MathF.PI * phase);
        WeaponLibrary1.DrawSpellMask(index, DrawLocation, colour, core,
            pulse * DXManager.Opacity * Settings.SpellBlendIntensityPercent / 100f, SpellMaskStyle.Sweep);
    }

    private void BeginLegacySpellVisual(Spell spell)
    {
        if (Frame == null || SpellVfxBladeFrames.Supports(spell)) return;
        var cue = LegacySpellEffects.Windup(spell, Frame.Count, FrameInterval);
        if (cue.Duration <= 0) return;

        int reach = spell == Spell.Thrusting && this is UserObject actor ? actor.PerkRank(spell, SpellPerk.ThrustReach) : 0;
        var visual = new SpellVisual(spell, cue.Phase, ExtraReach: reach);
        long start = CMain.Time + cue.Delay;
        if (cue.AtCell)
        {
            var effect = new Effect(visual, cue.Duration, CurrentLocation, start) { ParticleDirection = Direction };
            MapControl.Effects.Add(effect);
        }
        else
        {
            Effects.Add(new Effect(visual, cue.Duration, this, start) { ParticleDirection = Direction });
        }
    }
}
