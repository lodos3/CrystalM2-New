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

    internal void EmitSpellCast(Spell spell, Point location, MirDirection direction)
    {
        if (!CanEmitSpellParticles(location)) return;

        Vector2 centre = SpellWorldPoint(location);
        float angle = ((int)direction - 2) * MathF.PI / 4;
        Vector2 heading = new(MathF.Cos(angle), MathF.Sin(angle));
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
                EmitAccent(centre, Vector2.Zero, Palette(spell), SpellParticleShape.Slash, 78, 390, (byte)direction);
                EmitBurst(spell, centre, heading, 12);
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
                EmitBurst(spell, centre - new Vector2(0, 8), -Vector2.UnitY, 10);
                break;
            default:
                if (HasSpellTrail(spell)) EmitBurst(spell, centre - new Vector2(0, 16), heading, 6);
                break;
        }
    }

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
        EmitAccent(centre, Vector2.Zero, colour, SpellParticleShape.Wisp, 46, 250);
        EmitBurst(spell, centre, -Vector2.UnitY, 13);
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
        int size, int duration, byte direction = 0)
    {
        if (_spellParticles.TryAdd(new SpellParticle(centre, velocity, CMain.Time, duration, size, colour, shape, direction)))
            TextureValid = false;
    }

    private void EmitBurst(Spell spell, Vector2 centre, Vector2 heading, int count)
    {
        if (_spellParticles.Burst(centre, heading, Palette(spell), CMain.Time, count, Fragment(spell)))
            TextureValid = false;
    }

    private void DrawSpellParticles(bool ground)
    {
        if (!Settings.Effect || _spellParticles.Active.IsEmpty) return;

        Point camera = ToMouseLocation(Point.Empty);
        DXManager.Device.SpellParticles.Draw(_spellParticles.Active, new Vector2(camera.X, camera.Y), CMain.Time, ground);
    }
}
