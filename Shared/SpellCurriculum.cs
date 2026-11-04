using System;
using System.Collections.Generic;

public enum CombatSpecialisation : byte
{
    Unchosen = 0,
    First = 1,
    Second = 2,
    Third = 3
}

// Source-of-truth catalog for default class spell entitlements.
// It excludes custom/equipment/event spells and unseeded FastMove.
public static class SpellCurriculum
{
    private static readonly Spell[] WarriorSpells =
    {
        Spell.Fencing,
        Spell.Slaying,
        Spell.Thrusting,
        Spell.HalfMoon,
        Spell.ShoulderDash,
        Spell.TwinDrakeBlade,
        Spell.Entrapment,
        Spell.FlamingSword,
        Spell.LionRoar,
        Spell.CrossHalfMoon,
        Spell.BladeAvalanche,
        Spell.ProtectionField,
        Spell.Rage,
        Spell.CounterAttack,
        Spell.SlashingBurst,
        Spell.Fury,
        Spell.ImmortalSkin,
    };

    private static readonly Spell[] WizardSpells =
    {
        Spell.FireBall,
        Spell.Repulsion,
        Spell.ElectricShock,
        Spell.GreatFireBall,
        Spell.HellFire,
        Spell.ThunderBolt,
        Spell.Teleport,
        Spell.FireBang,
        Spell.FireWall,
        Spell.Lightning,
        Spell.FrostCrunch,
        Spell.ThunderStorm,
        Spell.MagicShield,
        Spell.TurnUndead,
        Spell.Vampirism,
        Spell.IceStorm,
        Spell.FlameDisruptor,
        Spell.Mirroring,
        Spell.FlameField,
        Spell.Blizzard,
        Spell.MagicBooster,
        Spell.MeteorStrike,
        Spell.IceThrust,
        Spell.StormEscape,
    };

    private static readonly Spell[] TaoistSpells =
    {
        Spell.Healing,
        Spell.SpiritSword,
        Spell.Poisoning,
        Spell.SoulFireBall,
        Spell.SummonSkeleton,
        Spell.Hiding,
        Spell.MassHiding,
        Spell.SoulShield,
        Spell.Revelation,
        Spell.BlessedArmour,
        Spell.EnergyRepulsor,
        Spell.TrapHexagon,
        Spell.Purification,
        Spell.MassHealing,
        Spell.Hallucination,
        Spell.UltimateEnhancer,
        Spell.SummonShinsu,
        Spell.Reincarnation,
        Spell.SummonHolyDeva,
        Spell.Curse,
        Spell.Plague,
        Spell.PoisonCloud,
        Spell.EnergyShield,
        Spell.PetEnhancer,
        Spell.HealingCircle,
    };

    private static readonly Spell[] AssassinSpells =
    {
        Spell.FatalSword,
        Spell.DoubleSlash,
        Spell.Haste,
        Spell.FlashDash,
        Spell.LightBody,
        Spell.HeavenlySword,
        Spell.FireBurst,
        Spell.Trap,
        Spell.PoisonSword,
        Spell.MoonLight,
        Spell.MPEater,
        Spell.SwiftFeet,
        Spell.DarkBody,
        Spell.Hemorrhage,
        Spell.CrescentSlash,
        Spell.MoonMist,
        Spell.CatTongue,
    };

    private static readonly Spell[] ArcherSpells =
    {
        Spell.Focus,
        Spell.StraightShot,
        Spell.DoubleShot,
        Spell.ExplosiveTrap,
        Spell.DelayedExplosion,
        Spell.Meditation,
        Spell.BackStep,
        Spell.ElementalShot,
        Spell.Concentration,
        Spell.Stonetrap,
        Spell.ElementalBarrier,
        Spell.SummonVampire,
        Spell.VampireShot,
        Spell.SummonToad,
        Spell.PoisonShot,
        Spell.CrippleShot,
        Spell.SummonSnakes,
        Spell.NapalmShot,
        Spell.OneWithNature,
        Spell.BindingShot,
        Spell.MentalState,
    };

    public static IReadOnlyList<Spell> ForClass(MirClass actorClass) => actorClass switch
    {
        MirClass.Warrior => WarriorSpells,
        MirClass.Wizard => WizardSpells,
        MirClass.Taoist => TaoistSpells,
        MirClass.Assassin => AssassinSpells,
        MirClass.Archer => ArcherSpells,
        _ => Array.Empty<Spell>()
    };

    public static bool Owns(MirClass actorClass, Spell spell)
    {
        foreach (Spell known in ForClass(actorClass))
            if (known == spell) return true;
        return false;
    }

    public static byte UnlockLevel(Spell spell, byte rankOneLevel)
    {
        // The starter spells unlock at rank zero; original rank levels remain unchanged.
        switch (spell)
        {
            case Spell.Fencing:
            case Spell.FireBall:
            case Spell.Healing:
            case Spell.FatalSword:
            case Spell.StraightShot: return 1;
            default: return rankOneLevel == 0 ? (byte)1 : rankOneLevel;
        }
    }

    public static bool IsUnlocked(MirClass actorClass, Spell spell, ushort actorLevel, byte firstRankLevel) =>
        Owns(actorClass, spell) && actorLevel >= UnlockLevel(spell, firstRankLevel);

    public static bool ShouldGrant(MirClass actorClass, Spell spell, ushort actorLevel, byte firstRankLevel, bool alreadyOwned) =>
        !alreadyOwned && IsUnlocked(actorClass, spell, actorLevel, firstRankLevel);

    public static bool IsValid(CombatSpecialisation spec) =>
        spec == CombatSpecialisation.Unchosen ||
        spec == CombatSpecialisation.First ||
        spec == CombatSpecialisation.Second ||
        spec == CombatSpecialisation.Third;

    public static string Name(MirClass actorClass, CombatSpecialisation spec) =>
        (actorClass, spec) switch
        {
            (MirClass.Warrior, CombatSpecialisation.First) => "Iron Vow",
            (MirClass.Warrior, CombatSpecialisation.Second) => "Red Tempest",
            (MirClass.Warrior, CombatSpecialisation.Third) => "Stoneguard",
            (MirClass.Wizard, CombatSpecialisation.First) => "Ashweaver",
            (MirClass.Wizard, CombatSpecialisation.Second) => "Rimecaller",
            (MirClass.Wizard, CombatSpecialisation.Third) => "Stormwright",
            (MirClass.Taoist, CombatSpecialisation.First) => "Lanternkeeper",
            (MirClass.Taoist, CombatSpecialisation.Second) => "Venom Scribe",
            (MirClass.Taoist, CombatSpecialisation.Third) => "Spirit Binder",
            (MirClass.Assassin, CombatSpecialisation.First) => "Nightblade",
            (MirClass.Assassin, CombatSpecialisation.Second) => "Duelist",
            (MirClass.Assassin, CombatSpecialisation.Third) => "Saboteur",
            (MirClass.Archer, CombatSpecialisation.First) => "Sharpshooter",
            (MirClass.Archer, CombatSpecialisation.Second) => "Trapper",
            (MirClass.Archer, CombatSpecialisation.Third) => "Beastkeeper",
            (_, CombatSpecialisation.Unchosen) => "Unchosen",
            _ => "Unknown"
        };
}
