public enum SpellPerk : byte
{
    Power = 1, Economy, Recharge, Reach, Duration, Radius, Splash, Burn, Chain, EchoHeal, Shelter,
    ThrustReach, CrescentReach, DrakeEcho, DashDistance, DashImpact, DashCrowd, AvalancheWidth,
    SlayingChance, SlayingCleave, Accuracy, Guard, Companion, Poison, Interrupt
}

public readonly record struct SpellPerkPick(Spell Spell, SpellPerk Perk, byte Rank);

public sealed record SpellPerkOption(SpellPerk Perk, string Name, string Description, byte MaxRank = 2,
    SpellPerk Requires = 0, byte ExclusiveGroup = 0);

public static class SpellPerks
{
    private static readonly SpellPerkOption Power = new(SpellPerk.Power, "Power", "Increase this spell's damage or healing by 8% per point.");
    private static readonly SpellPerkOption Economy = new(SpellPerk.Economy, "Efficiency", "Reduce the mana cost by 10% per point, with a minimum saving of 1 MP.");
    private static readonly SpellPerkOption Recharge = new(SpellPerk.Recharge, "Quick Recovery", "Reduce this spell's cooldown and recovery time by 10% per point.");
    private static readonly SpellPerkOption Reach = new(SpellPerk.Reach, "Reach", "Add one tile of cast range per point.");
    private static readonly SpellPerkOption Duration = new(SpellPerk.Duration, "Lingering Effect", "Extend the buff, field or control by 15% per point.");
    private static readonly SpellPerkOption Radius = new(SpellPerk.Radius, "Wide Field", "Extend the affected area by one tile per point.", 1);
    private static readonly SpellPerkOption Splash = new(SpellPerk.Splash, "Searing Reach", "A confirmed hit deals a reduced splash hit to up to three nearby normal monsters.");
    private static readonly SpellPerkOption Burn = new(SpellPerk.Burn, "Burning Wake", "Enemies struck by Searing Reach burn for a short time.", 2, SpellPerk.Splash);
    private static readonly SpellPerkOption Chain = new(SpellPerk.Chain, "Conductive Strike", "A confirmed hit can arc to up to two nearby normal monsters.");
    private static readonly SpellPerkOption EchoHeal = new(SpellPerk.EchoHeal, "Shared Remedy", "A heal on a wounded ally echoes to another injured ally nearby.");
    private static readonly SpellPerkOption Shelter = new(SpellPerk.Shelter, "Shelter", "Apply a small ward to the recipient when this spell succeeds.");
    private static readonly SpellPerkOption Companion = new(SpellPerk.Companion, "Bonded Companion", "Summoned companions start with extra health and weapon power.");
    private static readonly SpellPerkOption Interrupt = new(SpellPerk.Interrupt, "Stagger", "A confirmed hit briefly slows normal monsters.");

    private static readonly IReadOnlyDictionary<Spell, SpellPerkOption[]> Layouts = new Dictionary<Spell, SpellPerkOption[]>
    {
        [Spell.Fencing] = [new(SpellPerk.Accuracy, "Measured Strike", "Gain +2 accuracy per point."),
            new(SpellPerk.Guard, "Sword Guard", "Gain +2 maximum physical armour per point.")],
        [Spell.Slaying] = [new(SpellPerk.SlayingChance, "Killing Rhythm", "Trigger Slaying more often on ordinary weapon attacks."),
            new(SpellPerk.SlayingCleave, "Follow Through", "A landed Slaying hit also cuts up to two nearby normal monsters.")],
        [Spell.Thrusting] = [Power, new(SpellPerk.ThrustReach, "Long Point", "Reach one extra tile along the thrust line.", 1)],
        [Spell.HalfMoon] = [Power, new(SpellPerk.CrescentReach, "Wide Crescent", "Extend the crescent to the two outer front tiles.", 1), Economy],
        [Spell.CrossHalfMoon] = [Power, new(SpellPerk.CrescentReach, "Extended Cross", "Strike one extra tile along each cardinal direction.", 1), Economy],
        [Spell.TwinDrakeBlade] = [Power, new(SpellPerk.DrakeEcho, "Third Fang", "A confirmed second blade leaves a weaker delayed third strike."), Economy],
        [Spell.FlamingSword] = [Splash, Burn, Recharge],
        [Spell.ShoulderDash] = [new(SpellPerk.DashDistance, "Long Stride", "Dash two additional tiles.", 1, ExclusiveGroup: 1),
            new(SpellPerk.DashImpact, "Battering Dash", "Dash up to two tiles and hit harder on the first collision.", 1, ExclusiveGroup: 1),
            new(SpellPerk.DashCrowd, "Crowdbreaker", "Push up to three eligible monsters in a line.", 1, ExclusiveGroup: 1), Recharge],
        [Spell.BladeAvalanche] = [Power, new(SpellPerk.AvalancheWidth, "Wide Avalanche", "Add an outer lane on each side of the blade wave.", 1), Interrupt],
        [Spell.Entrapment] = [Reach, Recharge],
        [Spell.LionRoar] = [Radius, Duration, Recharge],
        [Spell.ProtectionField] = [Duration, Recharge, Shelter],
        [Spell.Rage] = [Duration, Recharge],
        [Spell.CounterAttack] = [Power, Duration, Recharge],
        [Spell.SlashingBurst] = [Power, Recharge, Interrupt],
        [Spell.Fury] = [Duration, Recharge],
        [Spell.ImmortalSkin] = [Duration, Recharge, Shelter],
        [Spell.BattleCry] = [Radius, Recharge],

        [Spell.FireBall] = [Power, Reach, Splash],
        [Spell.Repulsion] = [Radius, Recharge],
        [Spell.ElectricShock] = [Reach, Recharge, Companion],
        [Spell.GreatFireBall] = [Splash, Burn, Power],
        [Spell.HellFire] = [Power, Reach, Recharge],
        [Spell.ThunderBolt] = [Chain, Reach, Power],
        [Spell.Teleport] = [Economy, Recharge],
        [Spell.FireBang] = [Radius, Power, Economy],
        [Spell.FireWall] = [Duration, Radius, Power],
        [Spell.Lightning] = [Power, Reach, Interrupt],
        [Spell.FrostCrunch] = [Power, Reach, Interrupt],
        [Spell.ThunderStorm] = [Radius, Power, Recharge],
        [Spell.MagicShield] = [Duration, Shelter, Recharge],
        [Spell.TurnUndead] = [Reach, Recharge],
        [Spell.Vampirism] = [Power, Reach, Economy],
        [Spell.IceStorm] = [Radius, Interrupt, Power],
        [Spell.FlameDisruptor] = [Splash, Burn, Reach],
        [Spell.Mirroring] = [Economy, Recharge],
        [Spell.FlameField] = [Radius, Power, Economy],
        [Spell.Blizzard] = [Duration, Power, Economy],
        [Spell.MagicBooster] = [Duration, Economy, Recharge],
        [Spell.MeteorStrike] = [Duration, Power, Radius],
        [Spell.IceThrust] = [Power, Reach, Interrupt],
        [Spell.StormEscape] = [Reach, Recharge, Economy],
        [Spell.Blink] = [Reach, Recharge],
        [Spell.FireBounce] = [Splash, Burn, Power],
        [Spell.MeteorShower] = [Power, Reach, Recharge],

        [Spell.Healing] = [Power, EchoHeal, Reach],
        [Spell.SpiritSword] = [new(SpellPerk.Accuracy, "Steady Spirit", "Gain +2 accuracy per point."),
            new(SpellPerk.Guard, "Spirit Guard", "Gain +2 maximum magical armour per point.")],
        [Spell.Poisoning] = [new(SpellPerk.Poison, "Refined Dose", "Red poison lasts longer. Green poison deals more damage per tick."), Reach, Economy],
        [Spell.SoulFireBall] = [Power, Splash, Reach],
        [Spell.SummonSkeleton] = [Companion, Economy, Recharge],
        [Spell.Hiding] = [Duration, Economy, Recharge],
        [Spell.MassHiding] = [Duration, Radius, Economy],
        [Spell.SoulShield] = [Duration, Radius, Shelter],
        [Spell.Revelation] = [Duration, Reach, Recharge],
        [Spell.BlessedArmour] = [Duration, Radius, Shelter],
        [Spell.EnergyRepulsor] = [Radius, Recharge, Economy],
        [Spell.TrapHexagon] = [Duration, Reach, Recharge],
        [Spell.Purification] = [Shelter, Reach, Recharge],
        [Spell.MassHealing] = [Radius, Power, EchoHeal],
        [Spell.Hallucination] = [Duration, Reach, Economy],
        [Spell.UltimateEnhancer] = [Duration, Reach, Shelter],
        [Spell.SummonShinsu] = [Companion, Economy, Recharge],
        [Spell.Reincarnation] = [Reach, Economy, Recharge],
        [Spell.SummonHolyDeva] = [Companion, Economy, Recharge],
        [Spell.Curse] = [Duration, Radius, Reach],
        [Spell.Plague] = [Radius, Power, Economy],
        [Spell.PoisonCloud] = [Duration, Radius, Power],
        [Spell.EnergyShield] = [Duration, Reach, Shelter],
        [Spell.PetEnhancer] = [Duration, Reach, Recharge],
        [Spell.HealingCircle] = [Duration, Radius, Power]
    };

    public static bool Supports(MirClass actorClass) => actorClass is MirClass.Warrior or MirClass.Wizard or MirClass.Taoist;
    public static int Points(ushort level) => Math.Clamp(level - 9, 0, 255);
    public static IReadOnlyList<SpellPerkOption> For(Spell spell) => Layouts.GetValueOrDefault(spell) ?? [];

    public static bool BelongsTo(MirClass actorClass, Spell spell) => Supports(actorClass) && SpellCurriculum.Owns(actorClass, spell);

    public static bool Validate(MirClass actorClass, ushort level, IReadOnlyList<SpellPerkPick> picks,
        Func<Spell, bool> learned, out string reason)
    {
        if (picks.Count > SpellPerkProfile.MaxPicks || picks.Sum(pick => (int)pick.Rank) > Points(level))
        {
            reason = "You have spent more spell points than you have earned.";
            return false;
        }
        var ranks = new Dictionary<(Spell, SpellPerk), byte>();
        var choices = new HashSet<(Spell, byte)>();
        foreach (var pick in picks)
        {
            var option = For(pick.Spell).FirstOrDefault(option => option.Perk == pick.Perk);
            if (option == null || !BelongsTo(actorClass, pick.Spell) || !learned(pick.Spell) ||
                pick.Rank == 0 || pick.Rank > option.MaxRank || !ranks.TryAdd((pick.Spell, pick.Perk), pick.Rank))
            {
                reason = "This build contains an invalid spell or perk rank.";
                return false;
            }
            if (option.ExclusiveGroup != 0 && !choices.Add((pick.Spell, option.ExclusiveGroup)))
            {
                reason = "Choose only one version of this spell.";
                return false;
            }
        }
        foreach (var pick in picks)
        {
            var option = For(pick.Spell).First(option => option.Perk == pick.Perk);
            if (option.Requires != 0 && !ranks.ContainsKey((pick.Spell, option.Requires)))
            {
                reason = $"{option.Name} requires {For(pick.Spell).First(dependency => dependency.Perk == option.Requires).Name}.";
                return false;
            }
        }
        reason = "";
        return true;
    }
}

public sealed class SpellPerkProfile
{
    public const int MaxPicks = 255;
    private Dictionary<(Spell, SpellPerk), byte> _ranks = [];
    public int Revision { get; private set; }
    public SpellPerkPick[] Picks => _ranks.Select(pair => new SpellPerkPick(pair.Key.Item1, pair.Key.Item2, pair.Value)).ToArray();
    public int Rank(Spell spell, SpellPerk perk) => _ranks.GetValueOrDefault((spell, perk));

    public void Apply(IEnumerable<SpellPerkPick> picks)
    {
        _ranks = picks.ToDictionary(pick => (pick.Spell, pick.Perk), pick => pick.Rank);
        Revision++;
    }

    public static SpellPerkProfile Read(BinaryReader reader)
    {
        int revision = reader.ReadInt32();
        if (revision < 0 || revision == int.MaxValue) throw new InvalidDataException("Invalid spell perk revision.");
        var result = new SpellPerkProfile();
        foreach (var pick in ReadPicks(reader))
            if (!result._ranks.TryAdd((pick.Spell, pick.Perk), pick.Rank)) throw new InvalidDataException("Duplicate spell perk.");
        result.Revision = revision;
        return result;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Revision);
        WritePicks(writer, Picks);
    }

    public static SpellPerkPick[] ReadPicks(BinaryReader reader)
    {
        var picks = new SpellPerkPick[reader.ReadByte()];
        for (int i = 0; i < picks.Length; i++)
            picks[i] = new SpellPerkPick((Spell)reader.ReadByte(), (SpellPerk)reader.ReadByte(), reader.ReadByte());
        return picks;
    }

    public static void WritePicks(BinaryWriter writer, IReadOnlyList<SpellPerkPick> picks)
    {
        if (picks.Count > MaxPicks) throw new InvalidDataException("Too many spell perks.");
        writer.Write((byte)picks.Count);
        foreach (var pick in picks)
        {
            writer.Write((byte)pick.Spell);
            writer.Write((byte)pick.Perk);
            writer.Write(pick.Rank);
        }
    }
}
