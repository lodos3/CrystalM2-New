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
    private static readonly MirClass[] PlayableClasses =
    {
        MirClass.Warrior, MirClass.Wizard, MirClass.Taoist, MirClass.Assassin, MirClass.Archer
    };

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
        Spell.BattleCry,
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
        Spell.Blink,
        Spell.FireBounce,
        Spell.MeteorShower,
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
        if (SpecialisationAbilities.TryGet(spell, out var ability)) return ability.UnlockLevel;

        // Every character starts with their class's introductory ability.
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

    public static bool IsRetired(Spell spell) => SpecialisationAbilities.TryGet(spell, out _);

    public static bool CanUseLegacy(MirClass actorClass, Spell spell, ushort level, byte firstLevel, bool equipmentGrant) =>
        !IsRetired(spell) && (equipmentGrant || !PlayableClasses.Any(@class => Owns(@class, spell)) ||
            IsUnlocked(actorClass, spell, level, firstLevel));

    public static bool IsUnlocked(MirClass actorClass, Spell spell, ushort actorLevel, byte firstRankLevel) =>
        Owns(actorClass, spell) && actorLevel >= UnlockLevel(spell, firstRankLevel);

    public static bool ShouldGrant(MirClass actorClass, Spell spell, ushort actorLevel, byte firstRankLevel, bool alreadyOwned) =>
        !alreadyOwned && IsUnlocked(actorClass, spell, actorLevel, firstRankLevel);

    public static bool IsValid(CombatSpecialisation spec) =>
        spec == CombatSpecialisation.Unchosen ||
        spec == CombatSpecialisation.First ||
        spec == CombatSpecialisation.Second ||
        spec == CombatSpecialisation.Third;

    // Class curricula keep all learned records; only the selected combat loadout is usable.
    // Non-curriculum spells are still delegated to their existing equipment/event rules.
    public const ushort SpecialisationUnlockLevel = 20;

    public static bool CanSelect(MirClass actorClass, ushort level, CombatSpecialisation current,
        CombatSpecialisation requested) =>
        ForClass(actorClass).Count != 0 &&
        level >= SpecialisationUnlockLevel &&
        current == CombatSpecialisation.Unchosen &&
        requested != CombatSpecialisation.Unchosen &&
        IsValid(requested);

    public static CombatSpecialisation RequiredSpecialisation(MirClass actorClass, Spell spell)
    {
        if (!Owns(actorClass, spell)) return CombatSpecialisation.Unchosen;
        if (SpecialisationAbilities.TryGet(spell, out var ability)) return ability.Spec;

        return spell switch
        {
            // Warrior: focused melee, aggressive cleave, or protection.
            Spell.Slaying or Spell.Thrusting or Spell.FlamingSword or Spell.SlashingBurst or Spell.Rage
                => CombatSpecialisation.First,
            Spell.HalfMoon or Spell.TwinDrakeBlade or Spell.CrossHalfMoon or Spell.BladeAvalanche
                or Spell.Fury => CombatSpecialisation.Second,
            Spell.Entrapment or Spell.LionRoar or Spell.ProtectionField or Spell.CounterAttack
                or Spell.ImmortalSkin => CombatSpecialisation.Third,

            // Wizard: fire, frost, or storm.
            Spell.GreatFireBall or Spell.HellFire or Spell.FireBang or Spell.FireWall
                or Spell.FlameDisruptor or Spell.FlameField or Spell.MeteorStrike
                => CombatSpecialisation.First,
            Spell.FrostCrunch or Spell.IceStorm or Spell.Mirroring or Spell.Blizzard
                or Spell.IceThrust or Spell.Vampirism => CombatSpecialisation.Second,
            Spell.ElectricShock or Spell.ThunderBolt or Spell.Lightning or Spell.ThunderStorm
                or Spell.MagicBooster or Spell.StormEscape => CombatSpecialisation.Third,

            // Taoist: healing, curses, or companions.
            Spell.SoulShield or Spell.BlessedArmour or Spell.Purification or Spell.MassHealing
                or Spell.Reincarnation or Spell.EnergyShield or Spell.HealingCircle
                => CombatSpecialisation.First,
            Spell.Poisoning or Spell.Revelation or Spell.Hallucination or Spell.Curse
                or Spell.Plague or Spell.PoisonCloud => CombatSpecialisation.Second,
            Spell.SummonSkeleton or Spell.MassHiding or Spell.TrapHexagon
                or Spell.UltimateEnhancer or Spell.SummonShinsu or Spell.SummonHolyDeva
                or Spell.PetEnhancer => CombatSpecialisation.Third,

            // Assassin: stealth, weapon technique, or disruption.
            Spell.MoonLight or Spell.DarkBody or Spell.PoisonSword or Spell.Hemorrhage
                or Spell.MoonMist => CombatSpecialisation.First,
            Spell.DoubleSlash or Spell.Haste or Spell.HeavenlySword or Spell.FireBurst
                or Spell.CrescentSlash => CombatSpecialisation.Second,
            Spell.Trap or Spell.MPEater or Spell.CatTongue or Spell.FlashDash
                => CombatSpecialisation.Third,

            // Archer: precision, traps, or animal companions.
            Spell.DoubleShot or Spell.ElementalShot or Spell.Concentration or Spell.CrippleShot
                or Spell.BindingShot => CombatSpecialisation.First,
            Spell.ExplosiveTrap or Spell.DelayedExplosion or Spell.Stonetrap
                or Spell.ElementalBarrier or Spell.NapalmShot or Spell.PoisonShot
                => CombatSpecialisation.Second,
            Spell.SummonVampire or Spell.VampireShot or Spell.SummonToad
                or Spell.SummonSnakes or Spell.OneWithNature or Spell.MentalState
                => CombatSpecialisation.Third,

            // Every remaining native spell is part of the class-independent basic kit.
            _ => CombatSpecialisation.Unchosen
        };
    }

    public static bool IsPassive(Spell spell) => spell is Spell.Fencing or Spell.FatalSword or Spell.MPEater or
        Spell.Hemorrhage or Spell.SpiritSword or Spell.Slaying or Spell.Focus or Spell.Meditation;

    public static bool CanUse(MirClass actorClass, CombatSpecialisation selected, Spell spell)
    {
        if (spell == Spell.None) return true;

        if (Owns(actorClass, spell))
        {
            CombatSpecialisation required = RequiredSpecialisation(actorClass, spell);
            return required == CombatSpecialisation.Unchosen || required == selected;
        }

        // Never let equipment grants of another class's native spell bypass ownership.
        foreach (MirClass other in PlayableClasses)
            if (Owns(other, spell)) return false;

        return true; // Equipment/event/custom spells retain their own authorisation paths.
    }

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
