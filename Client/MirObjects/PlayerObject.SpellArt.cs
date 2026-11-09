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

    [Flags]
    private enum BodyCoat { None = 0, Protection = 1, Rage = 2, ImmortalSkin = 4, Fury = 8 }

    private Spell BladeSpell => Settings.Effect && LegacySpellEffects.UsesWeaponMask(Spell) &&
        (CurrentAction is MirAction.Attack1 or MirAction.Attack4 ||
            CurrentAction == MirAction.Spell && Cast) ? Spell : Spell.None;

    private Spell ReadiedBlade => this is UserObject actor
        ? actor.FlamingSword ? Spell.FlamingSword : actor.TwinDrakeBlade ? Spell.TwinDrakeBlade : Spell.None
        : Spell.None;

    private void ProcessWeaponSpellVisual()
    {
        Spell spell = BladeSpell;
        if (spell == Spell.None || Frame == null)
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
        if (trace == null && WarriorSpellEffects.UsesWeaponArt(spell))
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

    internal WeaponMaskTrace? EquippedWeaponTrace()
    {
        var hand = new Vector2(DrawLocation.X + MapControl.CellWidth / 2, DrawLocation.Y - 24);
        return WeaponTrace(DrawFrame + WeaponOffSet)?.Orient(hand, null);
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

        if (!Settings.Effect || Dead || RidingMount || Hidden) return;
        Spell swing = BladeSpell;
        bool drake = swing == Spell.TwinDrakeBlade && Frame != null &&
            (FrameIndex + .5f) / Math.Max(1, Frame.Count) is > .23f and < .89f;
        Spell enchant = drake ? Spell.TwinDrakeBlade : ReadiedBlade;
        if (enchant == Spell.None) return;
        if (drake && _lastWeaponPose is { } world)
            map.EmitWeaponEnchant(enchant, world, this, striking: true);
        else if (EquippedWeaponTrace() is { } screen)
            map.EmitWeaponEnchant(enchant, map.WeaponPoseInWorld(screen), this, striking: false);

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
            library.DrawSpellMask(index, DrawLocation, Color.FromArgb(0x31, 0x51, 0xc6), Color.FromArgb(0xb7, 0xed, 0xff),
                intensity * .52f, SpellMaskStyle.Storm);
        if ((_bodyCoat & BodyCoat.ImmortalSkin) != 0)
            library.DrawSpellMask(index, DrawLocation, Color.FromArgb(0x89, 0xb8, 0x49), Color.FromArgb(0xff, 0xeb, 0x8f),
                intensity * .54f, SpellMaskStyle.Frost);
        if ((_bodyCoat & BodyCoat.Rage) != 0)
            library.DrawSpellMask(index, DrawLocation, Color.FromArgb(0xf4, 0x42, 0x22), Color.FromArgb(0xff, 0xca, 0x68),
                intensity * .57f, SpellMaskStyle.Fire);
        if ((_bodyCoat & BodyCoat.Fury) != 0)
            library.DrawSpellMask(index, DrawLocation, Color.FromArgb(0x99, 0x25, 0x16), Color.FromArgb(0xff, 0xe6, 0x98),
                intensity * .52f, SpellMaskStyle.Sweep);
    }

    private void DrawWeaponSpellVisual(int index)
    {
        if (!Settings.Effect || Dead) return;
        Spell swing = BladeSpell;
        Spell ready = ReadiedBlade;
        Spell spell = swing is Spell.FlamingSword or Spell.TwinDrakeBlade || ready == Spell.None ? swing : ready;
        if (spell == Spell.None) return;
        var (colour, core) = LegacySpellEffects.WeaponColours(spell);
        float phase = Frame == null ? .5f : (FrameIndex + .5f) / Math.Max(1, Frame.Count);
        float pulse = spell == swing ? .29f + .59f * MathF.Sin(MathF.PI * phase) : .64f;
        SpellMaskStyle style = spell switch
        {
            Spell.FlamingSword => SpellMaskStyle.Fire,
            Spell.TwinDrakeBlade => SpellMaskStyle.Storm,
            _ => SpellMaskStyle.Sweep
        };
        WeaponLibrary1.DrawSpellMask(index, DrawLocation, colour, core,
            pulse * DXManager.Opacity * Settings.SpellBlendIntensityPercent / 100f, style);
    }

    private void BeginLegacySpellVisual(Spell spell)
    {
        if (Frame == null) return;
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
