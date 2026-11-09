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
        int cast = frames * interval;
        return spell switch
        {
            Spell.ThunderBolt => new(SpellVisualPhase.Prepare, 300),
            Spell.Revelation => new(SpellVisualPhase.Prepare, 1200),
            Spell.LionRoar or Spell.BattleCry => new(SpellVisualPhase.Impact, 1200),
            Spell.FlameField => new(SpellVisualPhase.Impact, 1800, AtCell: true),
            Spell.ThunderStorm => new(SpellVisualPhase.Impact, cast, AtCell: true),
            Spell.Fury => new(SpellVisualPhase.Impact, 10 * interval),
            Spell.ImmortalSkin => new(SpellVisualPhase.Impact, cast * 4),
            Spell.UltimateEnhancer => new(SpellVisualPhase.Prepare, 1000),
            Spell.MagicBooster => new(SpellVisualPhase.Impact, 9 * interval),
            Spell.PetEnhancer => new(SpellVisualPhase.Prepare, 8 * interval),
            Spell.TwinDrakeBlade => new(SpellVisualPhase.Prepare, 500),
            Spell.Thrusting => new(SpellVisualPhase.Swing, (frames - frames / 2) * interval, frames / 2 * interval),
            Spell.BladeAvalanche => new(SpellVisualPhase.Swing, 15 * interval, AtCell: true),
            Spell.SlashingBurst => new(SpellVisualPhase.Swing, 9 * interval, 300, AtCell: true),
            Spell.CounterAttack => new(SpellVisualPhase.Swing, 10 * interval),
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

    internal static int FieldInterval(Spell spell) => spell switch
    {
        Spell.HealingCircle => 80,
        Spell.FireWall or Spell.PoisonCloud => 120,
        _ => 100
    };

    private static Look Art(Form form, Shape shape, int size, int colour, int core) =>
        new(form, shape, size, Color.FromArgb(unchecked((int)0xff000000) | colour),
            Color.FromArgb(unchecked((int)0xff000000) | core));

    private static Look Profile(Spell spell) => spell switch
    {
        // Warrior. Use the angle and length of the hit to identify each weapon technique.
        Spell.Fencing => Art(Form.Cut, Shape.Spark, 32, 0xadc7d2, 0xf4f6ee),
        Spell.Slaying => Art(Form.Cut, Shape.Slash, 76, 0x5886ff, 0xebf6ff),
        Spell.Thrusting => Art(Form.Cut, Shape.Bolt, 118, 0x84d3ff, 0xe8f8ff),
        Spell.HalfMoon => Art(Form.Cleave, Shape.Slash, 134, 0x74d4f5, 0xe5fcff),
        Spell.ShoulderDash => Art(Form.Rush, Shape.Bolt, 100, 0xcb9f67, 0xffecb9),
        Spell.TwinDrakeBlade => Art(Form.Cleave, Shape.Claw, 132, 0x385dc9, 0xf2fbff),
        Spell.Entrapment => Art(Form.Bind, Shape.Bolt, 84, 0xd9af69, 0xffebbe),
        Spell.FlamingSword => Art(Form.Cleave, Shape.Slash, 126, 0xff7829, 0xffef84),
        Spell.LionRoar => Art(Form.Break, Shape.Halo, 186, 0xeeb054, 0xffe2a6),
        Spell.CrossHalfMoon => Art(Form.Cleave, Shape.Slash, 138, 0x8ec7ff, 0xf1faff),
        Spell.BladeAvalanche => Art(Form.Cleave, Shape.Shard, 128, 0xb0d6fa, 0xf5fcff),
        Spell.ProtectionField => Art(Form.Ward, Shape.Shield, 90, 0x77d0a9, 0xf4ffcf),
        Spell.Rage => Art(Form.Eruption, Shape.Claw, 93, 0xe74233, 0xffc37e),
        Spell.CounterAttack => Art(Form.Cut, Shape.Shield, 87, 0xff6330, 0xffe3a3),
        Spell.SlashingBurst => Art(Form.Cleave, Shape.Slash, 156, 0xff722a, 0xffedab),
        Spell.Fury => Art(Form.Eruption, Shape.Pillar, 110, 0xf25b30, 0xffe4a6),
        Spell.ImmortalSkin => Art(Form.Ward, Shape.Prism, 106, 0xf0be69, 0xfff6d6),
        Spell.BattleCry => Art(Form.Break, Shape.Banner, 119, 0xe2ab52, 0xffeabd),

        // Shared animations that previously used the same combat libraries.
        Spell.FatalSword or Spell.DoubleSlash or Spell.HeavenlySword or Spell.CrescentSlash or Spell.FlashDash
            => Art(Form.Cut, Shape.Slash, 86, 0xe4a0d0, 0xffe8ed),
        Spell.PoisonSword or Spell.PoisonShot or Spell.SummonSnakes => Art(Form.Veil, Shape.Claw, 72, 0x8ac154, 0xe1ffb3),
        Spell.Hemorrhage or Spell.VampireShot or Spell.SummonVampire or Spell.MPEater
            => Art(Form.Bolt, Shape.Shard, 70, 0xdc587c, 0xffc6d0),
        Spell.NapalmShot or Spell.ExplosiveTrap or Spell.DelayedExplosion or Spell.FireBurst
            => Art(Form.Eruption, Shape.Flash, 87, 0xff7a36, 0xffe586),
        Spell.ElementalBarrier or Spell.OneWithNature or Spell.ElementalShot
            => Art(Form.Ward, Shape.Prism, 82, 0x82e3b9, 0xe1ffeb),
        Spell.Trap or Spell.Stonetrap or Spell.BindingShot or Spell.CrippleShot or Spell.CatTongue
            => Art(Form.Bind, Shape.Glyph, 71, 0xc8a177, 0xffe8b1),
        Spell.MoonLight or Spell.MoonMist or Spell.DarkBody or Spell.Portal
            => Art(Form.Veil, Shape.Wisp, 93, 0xa38ad4, 0xe9deff),
        Spell.SummonToad => Art(Form.Totem, Shape.Wisp, 85, 0x78bc7d, 0xdbffd4),
        Spell.Haste or Spell.LightBody or Spell.SwiftFeet => Art(Form.Rush, Shape.Feather, 70, 0xa3e2d2, 0xf2fff0),
        Spell.Concentration or Spell.Focus => Art(Form.Ward, Shape.Glyph, 70, 0x8bccd5, 0xe6fcff),
        Spell.StraightShot or Spell.DoubleShot => Art(Form.Bolt, Shape.Bolt, 57, 0xceb283, 0xffedbf),
        _ => default
    };

    internal static bool HasLiveArt(Spell spell) =>
        WizardSpellEffects.HasArt(spell) || WarriorSpellEffects.HasArt(spell) || TaoistSpellEffects.HasArt(spell);

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
        if (remaining <= 0) return;
        var look = Profile(spell);
        if (look.Size == 0) return;
        if (WarriorSpellEffects.UsesWeaponArt(spell))
        {
            WarriorSpellEffects.WeaponSweep(pool, spell, look, pose, last, facing,
                feet ?? pose.Hilt + Vector2.UnitY * 40, now, remaining, interval, progress, previousProgress, followThrough);
            return;
        }
        var effect = new Effect(pool, look, now);
        int life = Math.Min(remaining, Math.Clamp(interval * 2, 65, 210));
        Vector2 blade = pose.Tip - pose.Hilt;
        Vector2 drift = last is { } previous ? pose.Tip - previous.Tip : blade * .2f;
        if (last is { } old && drift.LengthSquared() is > 4 and < 16000)
        {
            // Several tapered samples fill the area swept between the two real weapon poses.
            int pieces = Math.Clamp((int)drift.Length() / 9, 2, 7);
            for (int i = 0; i < pieces; i++)
            {
                float phase = (i + 1f) / pieces;
                Vector2 hilt = Vector2.Lerp(old.Hilt, pose.Hilt, phase);
                Vector2 tip = Vector2.Lerp(old.Tip, pose.Tip, phase);
                Vector2 edge = tip - hilt;
                int span = Math.Max(5, (int)edge.Length() + 8);
                effect.Add(hilt + edge * .58f, Vector2.Zero, Shape.BladeEdge, span, life, Layer.Emissive,
                    aspect: 26f / span, rotation: MathF.Atan2(edge.Y, edge.X),
                    alpha: (int)(92 + phase * 120));
            }

            int tipSpan = (int)drift.Length() + 9;
            effect.Add((old.Tip + pose.Tip) * .5f, Vector2.Zero, Shape.BladeEdge, tipSpan, life,
                Layer.Emissive, aspect: 17f / tipSpan,
                rotation: MathF.Atan2(drift.Y, drift.X), core: true, alpha: 206);
        }

        effect.Add(pose.Tip, drift * .16f, Shape.Flash, 23, life, Layer.Emissive, core: true);
    }

    internal static Vector2 ProjectileAnchor(Spell spell, Vector2 cell) => spell switch
    {
        Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch => cell - new Vector2(0, 47),
        Spell.SoulFireBall or Spell.SoulShield or Spell.BlessedArmour or Spell.MassHiding or
            Spell.Curse or Spell.Hallucination or Spell.Plague or Spell.PoisonCloud => cell - new Vector2(0, 36),
        _ => cell - new Vector2(0, 24)
    };

    internal static void ProjectileFrame(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frame, int remaining, bool leaveWake, float size = WizardSpellEffects.DefaultSize)
    {
        if (WizardSpellEffects.DrawProjectile(pool, spell, head, heading, now, frame, remaining, leaveWake, size)) return;
        if (TaoistSpellEffects.DrawProjectile(pool, spell, head, heading, now, frame, remaining, leaveWake, size)) return;
        var look = Profile(spell);
        if (look.Size == 0 || remaining <= 0) return;

        float angle = MathF.Atan2(heading.Y, heading.X);
        float turn = frame * .29f;
        var effect = new Effect(pool, look, now, frameOnly: true);
        switch (spell)
        {
            case Spell.StraightShot:
            case Spell.DoubleShot:
            case Spell.NapalmShot:
            case Spell.ElementalShot:
            case Spell.CatTongue:
                effect.Add(head, Vector2.Zero, Shape.BladeEdge, look.Size, remaining, Layer.Emissive,
                    aspect: .57f, rotation: angle, core: true);
                break;
            default:
                effect.Add(head, Vector2.Zero, Shape.Orb, look.Size * 4 / 5, remaining, Layer.Emissive, alpha: 135);
                effect.Add(head, Vector2.Zero, look.Shape, look.Size * 3 / 5, remaining,
                    Layer.Emissive, aspect: .86f, rotation: angle + turn, core: true);
                break;
        }

        if (!leaveWake) return;
        var wake = new Effect(pool, look, now);
        Vector2 side = new(-heading.Y, heading.X);
        float drift = (frame % 3 - 1) * 9;
        wake.Add(head - heading * 17 + side * drift, -heading * 10 + side * drift * .6f,
            Shape.Orb, look.Size / 2, Math.Min(remaining, 106), Layer.Emissive,
            aspect: .72f, rotation: angle, growth: -.29f, alpha: 96);
        wake.Add(head - heading * 22, -heading * 18 + side * drift, Shape.Spark, 16,
            Math.Min(remaining, 135), Layer.Emissive, rotation: angle + turn, core: true);
    }

    private static Look Profile(SpellVisual visual) => visual.Motif switch
    {
        ParticleMotif.Celebration => Art(Form.Heal, Shape.Banner, 115, 0xe3b04b, 0xfff7ba),
        ParticleMotif.Radiance => Art(Form.Ward, Shape.Feather, 115, 0x9dc4ff, 0xfff2d5),
        ParticleMotif.CraftSuccess => Art(Form.Eruption, Shape.Shard, 87, 0xffb34c, 0xffec9d),
        ParticleMotif.CraftFail => Art(Form.Veil, Shape.Wisp, 66, 0x937179, 0xffb0aa),
        ParticleMotif.Click => Art(Form.Bind, Shape.Halo, 38, 0xa8d99e, 0xeaffd9),
        _ => Profile(visual.Spell)
    };

    internal static void Emit(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing, long now,
        int duration = 0, long? sampleTime = null, uint fieldObjectID = 0)
    {
        var look = Profile(visual);
        if (look.Size == 0) return;

        int reference = visual.Phase switch
        {
            SpellVisualPhase.Prepare => 480,
            SpellVisualPhase.Aura => 520,
            SpellVisualPhase.Burn => 800,
            SpellVisualPhase.Swing or SpellVisualPhase.FollowThrough => 437,
            _ => 620
        };
        var effect = new Effect(pool, look, now, castsLight: visual.Motif == ParticleMotif.None,
            window: duration, referenceDuration: reference, sampleTime: sampleTime, fieldObjectID: fieldObjectID);
        Vector2 body = feet - Vector2.UnitY * 22;
        float angle = MathF.Atan2(facing.Y, facing.X);
        switch (visual.Phase)
        {
            case SpellVisualPhase.Prepare:
                Prepare(effect, look, body + facing * 14, angle);
                break;
            case SpellVisualPhase.Flight:
                effect.Add(body, facing * 8, Shape.Orb, look.Size * 3 / 4, 160, Layer.Emissive,
                    aspect: .84f, alpha: 139);
                effect.Add(body + facing * 6, facing * 9, look.Shape, look.Size / 2, 125, Layer.Emissive,
                    aspect: .84f, rotation: angle, core: true);
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
                effect.Scatter(body, 6, Shape.Shard, 55, 400);
                break;
            case SpellVisualPhase.Burn:
                effect.Add(feet, Vector2.Zero, Shape.Halo, 63, 760, Layer.Ground, aspect: .55f, alpha: 160);
                effect.Add(body + new Vector2(-9, 9), new Vector2(5, -41), Shape.Wisp, 51, 780,
                    Layer.Emissive, aspect: 1.28f, growth: .25f);
                effect.Scatter(feet, 6, Shape.Spark, 28, 710, rise: true);
                break;
            case SpellVisualPhase.Ward:
                effect.Ring(feet, look.Size, 650);
                effect.Add(body, -Vector2.UnitY * 8, Shape.Shield, 77, 570, Layer.Emissive,
                    aspect: 1.2f, growth: .18f, alpha: 182);
                break;
            default:
                Impact(effect, look, visual.Spell, feet, body, facing, angle);
                break;
        }
    }

    private static void Prepare(Effect effect, Look look, Vector2 hand, float angle)
    {
        effect.Add(hand, -Vector2.UnitY * 13, look.Shape, Math.Max(40, look.Size * 2 / 3),
            480, Layer.Emissive, aspect: .85f, rotation: angle * .3f, growth: .19f, alpha: 204);
        effect.Add(hand, Vector2.Zero, Shape.Orb, Math.Max(22, look.Size / 4), 240,
            Layer.Emissive, delay: 110, core: true);
        effect.Scatter(hand, 5, Shape.Spark, 28, 360, rise: true);
    }

    private static void Swing(Effect effect, Look look, SpellVisual visual, Vector2 feet, Vector2 body,
        Vector2 facing, float angle)
    {
        bool follow = visual.Phase == SpellVisualPhase.FollowThrough;
        if (visual.Spell == Spell.Thrusting)
        {
            effect.Add(body + facing * 53, facing * 28, Shape.Bolt, look.Size, 255, Layer.Emissive,
                aspect: .21f, rotation: angle, core: true);
            effect.Scatter(body + facing * 92, 6, Shape.Spark, 30, 380);
            return;
        }

        if (visual.Spell == Spell.BladeAvalanche)
        {
            for (int i = 0; i < 4; i++)
                effect.Add(body + facing * (28 + i * 35), facing * 16, Shape.Shard, 42 + i * 5, 400, Layer.Emissive,
                    aspect: 1.6f, rotation: angle + .45f, delay: i * 34, core: i % 2 == 0);
            return;
        }

        int cuts = visual.Spell switch { Spell.CrossHalfMoon => 4, Spell.TwinDrakeBlade => 2, _ => 1 };
        for (int i = 0; i < cuts; i++)
        {
            float direction = angle + i * (visual.Spell == Spell.CrossHalfMoon ? MathF.PI / 2 : .45f) + (follow ? .57f : -.2f);
            Vector2 travel = new(MathF.Cos(direction), MathF.Sin(direction));
            effect.Add(body + travel * 28, travel * 23, look.Shape, look.Size, 280, Layer.Emissive,
                aspect: .58f, rotation: direction, delay: i * 42, core: follow || i == 1);
        }
        if (visual.Spell is Spell.HalfMoon or Spell.CrossHalfMoon or Spell.SlashingBurst)
            effect.Ring(feet, look.Size, 380);
        if (visual.Spell == Spell.FlamingSword)
            effect.Add(body + facing * 34, facing * 11 - Vector2.UnitY * 30, Shape.Wisp, 70, 470, Layer.Emissive, growth: .37f);
        effect.Scatter(body + facing * 24, 7, Shape.Spark, look.Size * .45f, 410);
    }

    private static void Aura(Effect effect, Look look, Spell spell, Vector2 feet, Vector2 body)
    {
        if (spell is Spell.Poisoning or Spell.Plague or Spell.Curse or Spell.Hiding)
        {
            effect.Add(body, -Vector2.UnitY * 12, look.Shape, look.Size * 2 / 3, 500, Layer.World, alpha: 126, growth: .19f);
            effect.Add(feet, -Vector2.UnitY * 17, Shape.Spark, 17, 490, Layer.Emissive, core: true);
            return;
        }

        effect.Add(feet, Vector2.Zero, Shape.Halo, look.Size, 510, Layer.Ground, aspect: .48f, alpha: 170);
        effect.Add(body, -Vector2.UnitY * 4, look.Shape, look.Size * 3 / 4, 520, Layer.Emissive,
            aspect: 1.22f, alpha: 159);
        effect.Add(body + new Vector2(17, -12), new Vector2(-9, -16), Shape.Spark, 19, 460,
            Layer.Emissive, core: true);
    }

    private static void Field(Effect effect, Look look, Spell spell, Vector2 feet, Vector2 body, long now)
    {
        float drift = MathF.Sin(feet.X * .054f + feet.Y * .037f + now % 4096 * .008f) * 17;
        switch (spell)
        {
            case Spell.PoisonCloud:
                effect.Add(feet + new Vector2(drift, -13), new Vector2(15, -8), Shape.Wisp, 89, 810,
                    Layer.World, aspect: .52f, alpha: 151, growth: .19f);
                effect.Add(feet + new Vector2(-drift, -6), new Vector2(-16, -6), Shape.Wisp, 72, 700,
                    Layer.Emissive, aspect: .45f, alpha: 122, growth: .3f);
                break;
            case Spell.TrapHexagon:
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * MathF.PI / 3;
                    float next = angle + MathF.PI / 3;
                    Vector2 point = new(MathF.Cos(angle) * 29, MathF.Sin(angle) * 16);
                    Vector2 end = new(MathF.Cos(next) * 29, MathF.Sin(next) * 16);
                    Vector2 edge = end - point;
                    effect.Add(feet + (point + end) * .5f, Vector2.Zero, Shape.Bolt, (int)edge.Length() + 8,
                        520, Layer.Ground, aspect: .25f, rotation: MathF.Atan2(edge.Y, edge.X), alpha: 210);
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
        switch (look.Form)
        {
            case Form.Cut:
            case Form.Cleave:
                effect.Add(body, facing * 10, look.Shape, Math.Max(23, look.Size / 2), 230, Layer.Emissive,
                    aspect: .78f, rotation: angle - .28f, core: true);
                if (spell is not (Spell.Fencing or Spell.SpiritSword))
                    effect.Scatter(body, 5, Shape.Spark, 29, 380);
                break;
            case Form.Rush:
                effect.Add(body, facing * 35, Shape.Bolt, look.Size, 270, Layer.Emissive, aspect: .42f, rotation: angle);
                effect.Add(feet, facing * 20, Shape.Wisp, 64, 450, Layer.Ground, alpha: 156);
                effect.Scatter(body, 6, Shape.Shard, 35, 360);
                break;
            case Form.Heal:
            case Form.Totem:
            case Form.Ward:
                effect.Ring(feet, look.Size, 650);
                effect.Add(body, -Vector2.UnitY * 27, look.Shape, look.Size * 3 / 4, 540,
                    Layer.Emissive, aspect: 1.12f, growth: .12f);
                effect.Add(body, -Vector2.UnitY * 21, Shape.Flash, look.Size / 2, 235, Layer.Emissive, core: true);
                effect.Scatter(body, 7, look.Form == Form.Totem ? look.Shape : Shape.Feather, look.Size * .42f, 590, rise: true);
                break;
            case Form.Bind:
                effect.Ring(feet, look.Size, 640);
                if (spell == Spell.Entrapment)
                {
                    for (int i = 0; i < 3; i++)
                        effect.Add(body + new Vector2(0, i * 12 - 12), Vector2.Zero, Shape.Bolt, 55, 560,
                            Layer.Emissive, aspect: .23f, rotation: i == 1 ? -.45f : .45f, delay: i * 45, core: i == 1);
                }
                else
                {
                    effect.Add(body, -Vector2.UnitY * 18, look.Shape, look.Size * 3 / 4, 540,
                        Layer.Emissive, core: true);
                }
                break;
            case Form.Veil:
                effect.Ring(feet, look.Size, 520);
                for (int i = -1; i <= 1; i++)
                    effect.Add(body + new Vector2(i * 22, 7), new Vector2(i * 13, -19), Shape.Wisp, look.Size * 3 / 4,
                        650, Layer.World, growth: .38f, alpha: 182);
                effect.Add(body, -Vector2.UnitY * 15, look.Shape, look.Size / 2, 360, Layer.Emissive, core: true);
                break;
            case Form.Break:
                effect.Ring(feet, look.Size, 530);
                effect.Add(body, Vector2.Zero, look.Shape, look.Size, 410, Layer.Emissive,
                    aspect: .72f, growth: .56f, alpha: 188, core: true);
                effect.Scatter(body, 9, Shape.Feather, look.Size * .55f, 490);
                break;
            default:
                if (look.Form is Form.Eruption or Form.Hammer) effect.Ring(feet, look.Size, 510);
                effect.Add(body, -Vector2.UnitY * 26, look.Shape, look.Size * 3 / 4, 390, Layer.Emissive,
                    aspect: look.Shape == Shape.Pillar ? 1.55f : .93f, growth: .27f);
                effect.Add(body, -Vector2.UnitY * 12, Shape.Flash, look.Size * 2 / 3, 230, Layer.Emissive, core: true);
                effect.Scatter(body, 8, Shape.Spark, look.Size * .59f, 485);
                break;
        }
    }

}
