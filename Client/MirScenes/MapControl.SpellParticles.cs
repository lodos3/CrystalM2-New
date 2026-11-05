using System.Numerics;
using Client.MirGraphics;
using CrystalM2.Rendering.Core;

namespace Client.MirScenes;

public sealed partial class MapControl
{
    private readonly SpellParticlePool _spellParticles = new(512);
    internal int SpellParticleMapGeneration { get; private set; }

    private void ResetSpellParticles()
    {
        if (!_spellParticles.Active.IsEmpty) TextureValid = false;
        _spellParticles.Clear();
        SpellParticleMapGeneration++;
    }

    internal static bool HasSpellTrail(Spell spell) => spell is Spell.FireBall or Spell.GreatFireBall or
        Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch or Spell.SoulFireBall or Spell.Plague or
        Spell.StraightShot or Spell.DoubleShot or Spell.ElementalShot or Spell.BindingShot or Spell.NapalmShot;

    internal void EmitSpellCast(Spell spell, Point location, Point target, MirDirection direction)
    {
        if (!CanEmitSpellParticles(location)) return;

        Vector2 centre = SpellWorldPoint(location);
        float angle = ((int)direction - 2) * MathF.PI / 4;
        Vector2 heading = new(MathF.Cos(angle), MathF.Sin(angle));
        if (SpecialisationAbilities.TryGet(spell, out var ability))
        {
            Color colour = ThemeColour(ability.Theme);
            if (ability.Theme is AbilityTheme.Steel or AbilityTheme.Blood or AbilityTheme.Shadow or AbilityTheme.Arrow)
                EmitAccent(centre + heading * 14, heading * 8, colour, SpellParticleShape.Slash,
                    56, Math.Max(ability.Windup, (ushort)200), (byte)direction);
            else
                EmitAccent(centre, -Vector2.UnitY * 8, colour, SpellParticleShape.Wisp, 39, ability.Windup);

            if (ability.Target == AbilityTarget.Enemy && CanEmitSpellParticles(target) &&
                ability.Theme is AbilityTheme.Arrow or AbilityTheme.Spirit or AbilityTheme.Storm)
            {
                Vector2 destination = SpellWorldPoint(target);
                for (int step = 1; step <= 5; step++)
                {
                    Vector2 point = Vector2.Lerp(centre, destination, step / 6f) - Vector2.UnitY * 15;
                    EmitAccent(point, heading * 10, colour, SpellParticleShape.Wisp,
                        18 + step * 2, 170, delay: ability.Windup * step / 6);
                }
            }
            return;
        }

        switch (spell)
        {
            case Spell.BladeAvalanche:
            case Spell.SlashingBurst:
            case Spell.CounterAttack:
            case Spell.CrescentSlash:
            case Spell.HeavenlySword:
            case Spell.FlashDash:
            case Spell.Lightning:
                centre += heading * 24;
                EmitAccent(centre, heading * 6, Palette(spell), SpellParticleShape.Slash, 78, 270, (byte)direction);
                EmitAccent(centre, heading * 15, Palette(spell), SpellParticleShape.Slash, 90, 210, (byte)direction, delay: 65);
                EmitBurst(spell, centre, heading, 12, delay: 85);
                break;
            case Spell.ProtectionField:
            case Spell.ImmortalSkin:
            case Spell.MagicShield:
            case Spell.SoulShield:
            case Spell.BlessedArmour:
            case Spell.EnergyShield:
            case Spell.ElementalBarrier:
            case Spell.SummonSkeleton:
            case Spell.SummonShinsu:
            case Spell.SummonHolyDeva:
                EmitAccent(centre, Vector2.Zero, Palette(spell), SpellParticleShape.Halo, 78, 650);
                EmitAccent(centre, Vector2.Zero, Palette(spell), SpellParticleShape.Halo, 104, 480, delay: 140);
                EmitBurst(spell, centre - new Vector2(0, 8), -Vector2.UnitY, 10, delay: 95);
                break;
            default:
                if (HasSpellTrail(spell)) EmitBurst(spell, centre - new Vector2(0, 16), heading, 6);
                break;
        }
    }

    internal void EmitSignatureEffect(Spell spell, Point location, SpellEffect phase)
    {
        if (!CanEmitSpellParticles(location) || !SpecialisationAbilities.TryGet(spell, out var ability)) return;

        Vector2 centre = SpellWorldPoint(location);
        Color colour = ThemeColour(ability.Theme);
        SpellParticleShape fragment = ability.Theme switch
        {
            AbilityTheme.Frost or AbilityTheme.Storm or AbilityTheme.Arrow => SpellParticleShape.Shard,
            AbilityTheme.Venom or AbilityTheme.Shadow or AbilityTheme.Spirit => SpellParticleShape.Wisp,
            _ => SpellParticleShape.Spark
        };

        if (phase == SpellEffect.SignatureArmed)
        {
            EmitAccent(centre, Vector2.Zero, colour, SpellParticleShape.Halo, 66, 1300);
            EmitAccent(centre - new Vector2(0, 5), -Vector2.UnitY * 7, colour, SpellParticleShape.Spark, 18, 550, delay: 140);
            return;
        }

        if (phase == SpellEffect.SignatureWindup)
        {
            int width = (ability.Target is AbilityTarget.Ground or AbilityTarget.Self) && spell != Spell.GlacialGate ? 105 : 53;
            int windupTime = ability.Windup;
            EmitAccent(centre, Vector2.Zero, colour, SpellParticleShape.Halo, width, windupTime);
            EmitAccent(centre - new Vector2(0, 6), new Vector2(0, -15), colour, SpellParticleShape.Wisp,
                width / 2, windupTime / 2, delay: windupTime / 3);
            return;
        }

        int radius = ability.Target is AbilityTarget.Ground or AbilityTarget.Self ? 88 : 61;
        int glowHeight = ability.Theme == AbilityTheme.Storm ? 30 : 16;
        Vector2 core = centre - new Vector2(0, glowHeight);
        if (ability.Theme is AbilityTheme.Ward or AbilityTheme.Frost or AbilityTheme.Flame or AbilityTheme.Blood)
        {
            EmitAccent(centre, Vector2.Zero, colour, SpellParticleShape.Halo, radius, 500);
            EmitAccent(centre, Vector2.Zero, colour, SpellParticleShape.Halo, radius + 24, 365, delay: 115);
        }
        EmitAccent(core, Vector2.Zero, Color.FromArgb(237, 255, 238, 216), SpellParticleShape.Spark, 33, 120);
        EmitAccent(core, -Vector2.UnitY * 14, colour, fragment, 44, 245, delay: 30);
        EmitAccent(core, -Vector2.UnitY * 26, colour, SpellParticleShape.Wisp, 66, 425, delay: 115);
        if (_spellParticles.Active.Length < 448)
        {
            _spellParticles.Burst(core, -Vector2.UnitY, colour, CMain.Time, 8, fragment, delay: 70);
            _spellParticles.Burst(core, -Vector2.UnitY * .6f, colour, CMain.Time, 5, fragment, delay: 180);
        }
    }

    private static Color ThemeColour(AbilityTheme theme) => theme switch
    {
        AbilityTheme.Steel => Color.FromArgb(227, 241, 228, 195),
        AbilityTheme.Blood => Color.FromArgb(233, 251, 69, 49),
        AbilityTheme.Ward => Color.FromArgb(221, 255, 214, 125),
        AbilityTheme.Flame => Color.FromArgb(224, 255, 112, 48),
        AbilityTheme.Frost => Color.FromArgb(221, 118, 216, 255),
        AbilityTheme.Storm => Color.FromArgb(229, 172, 163, 255),
        AbilityTheme.Spirit => Color.FromArgb(213, 106, 236, 184),
        AbilityTheme.Venom => Color.FromArgb(218, 134, 226, 81),
        AbilityTheme.Shadow => Color.FromArgb(220, 179, 112, 237),
        AbilityTheme.Arrow => Color.FromArgb(228, 254, 205, 121),
        _ => Color.White
    };

    // For spells whose baked effect resolves directly at the target without a missile.
    internal void EmitDirectSpellEffect(Spell spell, Point caster, Point groundTarget, Point? actorTarget)
    {
        switch (spell)
        {
            case Spell.ThunderStorm:
                EmitSpellImpact(spell, caster);
                break;
            case Spell.FireBang:
            case Spell.IceStorm:
            case Spell.MassHealing:
                EmitSpellImpact(spell, groundTarget);
                break;
            case Spell.ThunderBolt:
            case Spell.Healing:
            case Spell.Purification:
            case Spell.FlameDisruptor:
                EmitSpellImpact(spell, actorTarget ?? groundTarget);
                break;
            case Spell.Poisoning:
            case Spell.Curse:
                if (actorTarget.HasValue) EmitSpellImpact(spell, actorTarget.Value);
                break;
        }
    }

    internal void EmitSpellImpact(Spell spell, Point location)
    {
        if (!CanEmitSpellParticles(location)) return;

        Vector2 centre = SpellWorldPoint(location);
        Color colour = Palette(spell);
        if (spell is Spell.Healing or Spell.MassHealing or Spell.Purification or Spell.ExplosiveTrap)
            EmitAccent(centre, Vector2.Zero, colour, SpellParticleShape.Halo, 68, 540);

        centre.Y -= 14;
        EmitAccent(centre, Vector2.Zero, Color.FromArgb(230, 255, 242, 214), SpellParticleShape.Spark, 29, 120);
        EmitAccent(centre, new Vector2(0, -9), colour, SpellParticleShape.Wisp, 54, 320, delay: 45);
        EmitBurst(spell, centre, -Vector2.UnitY, 13, delay: 65);
    }

    internal void EmitSpellTrail(Spell spell, Point source, Point destination, float progress)
    {
        if (!Settings.Effect || User == null || M2CellInfo == null) return;

        Vector2 origin = SpellWorldPoint(source);
        Vector2 target = SpellWorldPoint(destination);
        Vector2 point = Vector2.Lerp(origin, target, progress);
        var cell = new Point((int)point.X / CellWidth, (int)point.Y / CellHeight);
        if (!CanEmitSpellParticles(cell)) return;

        Vector2 drift = (origin - target) * .12f;
        if (drift.LengthSquared() > 24 * 24) drift = Vector2.Normalize(drift) * 24;
        point.Y -= 16;
        Color colour = Palette(spell);
        EmitAccent(point, drift, colour, SpellParticleShape.Wisp, 22, 290);
        EmitAccent(point, drift - new Vector2(0, 8), colour, Fragment(spell), 12, 230);
    }

    internal static bool HasGroundSpellParticles(Spell spell) => spell is Spell.FireWall or Spell.PoisonCloud or
        Spell.Blizzard or Spell.MeteorStrike or Spell.HealingCircle or Spell.TrapHexagon;

    internal void EmitGroundSpellParticles(Spell spell, Point location)
    {
        // Reserve room for casts and impacts during overlapping ground effects.
        if (_spellParticles.Active.Length >= 384 || !CanEmitSpellParticles(location)) return;

        Vector2 centre = SpellWorldPoint(location);
        Color colour = Palette(spell);
        if (spell is Spell.HealingCircle or Spell.TrapHexagon)
            EmitAccent(centre, Vector2.Zero, colour, SpellParticleShape.Halo, 50, 520);
        else
            EmitBurst(spell, centre, -Vector2.UnitY, 3);
    }

    private static SpellParticleShape Fragment(Spell spell) => spell switch
    {
        Spell.FrostCrunch or Spell.IceStorm or Spell.Blizzard or Spell.IceThrust or
        Spell.ThunderBolt or Spell.Lightning or Spell.ThunderStorm => SpellParticleShape.Shard,
        Spell.Poisoning or Spell.PoisonCloud or Spell.PoisonSword or Spell.Curse or Spell.Plague => SpellParticleShape.Wisp,
        _ => SpellParticleShape.Spark
    };

    private static Color Palette(Spell spell) => spell switch
    {
        Spell.FireBall or Spell.GreatFireBall or Spell.FireBang or Spell.FireWall or Spell.FlameDisruptor or
        Spell.FireBounce or Spell.FlameField or Spell.MeteorStrike or Spell.MeteorShower or
        Spell.FlamingSword or Spell.ExplosiveTrap or Spell.NapalmShot => Color.FromArgb(224, 255, 111, 46),
        Spell.FrostCrunch or Spell.IceStorm or Spell.Blizzard or Spell.IceThrust or
        Spell.MagicShield or Spell.ElementalBarrier => Color.FromArgb(214, 129, 216, 255),
        Spell.ThunderBolt or Spell.Lightning or Spell.ThunderStorm => Color.FromArgb(228, 173, 170, 255),
        Spell.Poisoning or Spell.PoisonCloud or Spell.PoisonSword or Spell.Curse or Spell.Plague => Color.FromArgb(205, 127, 221, 83),
        Spell.SoulFireBall or Spell.SummonSkeleton or Spell.SummonShinsu or Spell.SummonHolyDeva or
        Spell.TrapHexagon => Color.FromArgb(210, 121, 236, 196),
        Spell.FatalSword or Spell.DoubleSlash or Spell.FlashDash or Spell.CrescentSlash or
        Spell.HeavenlySword => Color.FromArgb(213, 206, 139, 252),
        _ => Color.FromArgb(217, 255, 216, 139)
    };

    private static Vector2 SpellWorldPoint(Point cell) =>
        new(cell.X * CellWidth + CellWidth / 2, cell.Y * CellHeight + CellHeight / 2);

    private bool CanEmitSpellParticles(Point cell) => Settings.Effect && User != null && M2CellInfo != null &&
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height &&
        Math.Abs(cell.X - User.Movement.X) <= ViewRangeX + 2 &&
        Math.Abs(cell.Y - User.Movement.Y) <= ViewRangeY + 2;

    private void EmitAccent(Vector2 centre, Vector2 velocity, Color colour, SpellParticleShape shape,
        int size, int duration, byte direction = 0, int delay = 0)
    {
        var particle = new SpellParticle(centre, velocity, CMain.Time + delay, duration, size, colour, shape, direction);
        if (delay > 0) _spellParticles.Schedule(particle);
        else if (_spellParticles.TryAdd(particle))
            TextureValid = false;
    }

    private void EmitBurst(Spell spell, Vector2 centre, Vector2 heading, int count, int delay = 0)
    {
        if (_spellParticles.Burst(centre, heading, Palette(spell), CMain.Time, count, Fragment(spell), delay) && delay == 0)
            TextureValid = false;
    }

    private void DrawSpellParticles(bool ground)
    {
        if (!Settings.Effect || _spellParticles.Active.IsEmpty) return;

        Point camera = ToMouseLocation(Point.Empty);
        DXManager.Device.SpellParticles.Draw(_spellParticles.Active, new Vector2(camera.X, camera.Y), CMain.Time, ground,
            ground ? 1f : Settings.SpellBlendIntensityPercent / 100f);
    }

    private void DrawSpellParticleLights(float intensity)
    {
        if (_spellParticles.Active.IsEmpty) return;
        Point position = ToMouseLocation(Point.Empty);
        Vector2 camera = new(position.X, position.Y);
        DXManager.Device.SpellParticles.Draw(_spellParticles.Active, camera, CMain.Time, ground: true, intensity, emission: true);
        DXManager.Device.SpellParticles.Draw(_spellParticles.Active, camera, CMain.Time, ground: false, intensity, emission: true);
    }
}
