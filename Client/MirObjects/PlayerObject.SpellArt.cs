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
        if (!Settings.Effect || Hidden || Dead) return;
        var map = GameScene.Scene.MapControl;
        if (SpellVfxPoseMasks.IsWeaponSwing(Spell) && CurrentAction == MirAction.Attack1)
        {
            map.EmitBladeAttack(this, Spell, SpellVisualPhase.Swing, FrameIndex, FrameIndex / 6f);
            return;
        }
        if (SpellVfxBladeFrames.Attack(Spell, CurrentAction) is not { } cue ||
            (uint)FrameIndex >= (uint)cue.FrameCount) return;

        float withinFrame = FrameInterval > 0
            ? 1 - Math.Clamp((NextMotion - CMain.Time) / (float)FrameInterval, 0, 1)
            : 0;
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
        WeaponMaskTrace? trace = WeaponTraceAt(FrameIndex);
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

    internal WeaponMaskTrace? EquippedWeaponTrace() => WeaponTraceAt(FrameIndex);

    internal WeaponMaskTrace? WeaponTraceAt(int pose)
    {
        if (Weapon < 0) return null;
        float progress = (Math.Max(0, pose) + .5f) / Math.Max(1, Frame?.Count ?? 6);
        float facing = ((int)Direction - 2) * MathF.PI / 4;
        bool striking = CurrentAction is MirAction.Attack1 or MirAction.Attack4;
        float angle = facing + (striking ? (progress - .5f) * 2.4f : .2f);
        Vector2 hand = new(DrawLocation.X + MapControl.CellWidth / 2, DrawLocation.Y - 24);
        hand += new Vector2(MathF.Cos(facing) * 6, MathF.Sin(facing) * 4);
        Vector2 tip = hand + new Vector2(MathF.Cos(angle) * 30, MathF.Sin(angle) * 20);
        return new WeaponMaskTrace(hand, tip);
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
        if ((coat & BodyCoat.Protection) != 0) map.EmitActorCoating(this, Spell.ProtectionField, 0x77BFFF);
        if ((coat & BodyCoat.ImmortalSkin) != 0) map.EmitActorCoating(this, Spell.ImmortalSkin, 0xFFD582);
        if ((coat & BodyCoat.Rage) != 0) map.EmitActorCoating(this, Spell.Rage, 0xFF7030);
        if ((coat & BodyCoat.Fury) != 0) map.EmitActorCoating(this, Spell.Fury, 0xFFAF55);

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
