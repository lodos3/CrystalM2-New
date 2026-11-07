using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record TalentOption(string Name, string Description, Spell Grant, Spell Replaces);

public sealed record TalentNode(ushort Id, byte Row, byte Column, string Name, string Description, TalentOption[] Options)
{
    public byte MaxRank => Row == 2 ? (byte)2 : (byte)1;
}

public sealed record TalentTree(MirClass Class, CombatSpecialisation Spec, string Title, string[] Columns, TalentNode[] Nodes)
{
    public bool Shared => Spec == CombatSpecialisation.Unchosen;
    public int LastRow => Shared ? 6 : 8;
    public TalentNode At(int row, int column) => Nodes[(row - 1) * 3 + column];
}

public readonly record struct TalentPick(ushort Id, byte Rank, byte Option);
public readonly record struct TalentBinding(Spell Spell, byte Slot);

public sealed class TalentLoadout
{
    private readonly Dictionary<ushort, TalentPick> _picks = new();
    private readonly HashSet<Spell> _grants = new();
    private readonly HashSet<Spell> _replaced = new();

    public MirClass Class { get; }
    public CombatSpecialisation Spec { get; }
    public int Engine { get; }

    public TalentLoadout(MirClass @class, CombatSpecialisation spec, TalentPick[] shared, TalentPick[] specialised)
    {
        Class = @class;
        Spec = spec;
        Engine = -1;
        Add(TalentCatalog.Tree(@class), shared);
        if (spec != CombatSpecialisation.Unchosen)
        {
            Add(TalentCatalog.Tree(@class, spec), specialised);
            for (int column = 0; column < 3; column++)
                if (Rank(false, 4, column) != 0) Engine = column;
        }
    }

    private void Add(TalentTree tree, TalentPick[] picks)
    {
        if (tree == null || picks == null) return;
        foreach (TalentPick pick in picks)
        {
            TalentNode node = tree.Nodes.FirstOrDefault(node => node.Id == pick.Id);
            if (node == null || pick.Option >= node.Options.Length || pick.Rank == 0 || pick.Rank > node.MaxRank) continue;
            _picks.TryAdd(pick.Id, pick);
            TalentOption option = node.Options[pick.Option];
            if (option.Grant != Spell.None) _grants.Add(option.Grant);
            if (option.Replaces != Spell.None) _replaced.Add(option.Replaces);
        }
    }

    public int Rank(bool shared, int row, int column, int option = -1) =>
        _picks.TryGetValue(TalentCatalog.NodeId(Class, shared ? CombatSpecialisation.Unchosen : Spec, row, column), out var pick) &&
        (option < 0 || pick.Option == option) ? pick.Rank : 0;

    public bool CanUse(ushort level, Spell spell)
    {
        if (_replaced.Contains(spell) || (spell == Spell.FireBall && Spec != CombatSpecialisation.Unchosen)) return false;
        if (TalentAbilities.TryGet(spell, out var talent))
        {
            SpecialisationAbility ability = talent.Spell;
            return ability.Class == Class && level >= ability.UnlockLevel &&
                (ability.Spec == CombatSpecialisation.Unchosen || ability.Spec == Spec) &&
                (talent.Kind != TalentAbilityKind.Talent || _grants.Contains(spell));
        }
        if (!TalentCatalog.Supports(Class)) return SpellCurriculum.CanUse(Class, Spec, spell);
        if (TalentCatalog.TryGetClass(spell, out MirClass owner))
            return owner == Class && (_grants.Contains(spell) || TalentCatalog.BaseGrant(Class, Spec, level, spell));
        return true;
    }
}

public static class TalentCatalog
{
    public const int MaxNodes = 24;
    private static readonly IReadOnlyDictionary<(MirClass, CombatSpecialisation), TalentTree> Trees = Load();
    private static readonly IReadOnlyDictionary<Spell, MirClass> Owners = Enum.GetValues<MirClass>()
        .SelectMany(@class => SpellCurriculum.ForClass(@class).Select(spell => (spell, @class)))
        .ToDictionary(entry => entry.spell, entry => entry.@class);

    private static readonly Dictionary<Spell, (MirClass Class, byte Level)> ClassSpells = new()
    {
        [Spell.SteelCut] = (MirClass.Warrior, 1), [Spell.Fencing] = (MirClass.Warrior, 1),
        [Spell.ShoulderDash] = (MirClass.Warrior, 10), [Spell.Entrapment] = (MirClass.Warrior, 12),
        [Spell.LionRoar] = (MirClass.Warrior, 16),
        [Spell.FireBall] = (MirClass.Wizard, 1), [Spell.Repulsion] = (MirClass.Wizard, 8),
        [Spell.MagicShield] = (MirClass.Wizard, 10), [Spell.Teleport] = (MirClass.Wizard, 19),
        [Spell.Healing] = (MirClass.Taoist, 1), [Spell.SpiritSpark] = (MirClass.Taoist, 1),
        [Spell.SpiritSword] = (MirClass.Taoist, 1), [Spell.Hiding] = (MirClass.Taoist, 10),
        [Spell.Poisoning] = (MirClass.Taoist, 10), [Spell.SoulFireBall] = (MirClass.Taoist, 18),
        [Spell.EnergyRepulsor] = (MirClass.Taoist, 20), [Spell.Purification] = (MirClass.Taoist, 20)
    };

    private static readonly Dictionary<Spell, CombatSpecialisation> SpecSpells = new()
    {
        [Spell.Notchbreaker] = CombatSpecialisation.First, [Spell.Thrusting] = CombatSpecialisation.First,
        [Spell.FlamingSword] = CombatSpecialisation.First,
        [Spell.RedWake] = CombatSpecialisation.Second, [Spell.HalfMoon] = CombatSpecialisation.Second,
        [Spell.TwinDrakeBlade] = CombatSpecialisation.Second,
        [Spell.Interpose] = CombatSpecialisation.Third, [Spell.ProtectionField] = CombatSpecialisation.Third,
        [Spell.AshSeal] = CombatSpecialisation.First, [Spell.GreatFireBall] = CombatSpecialisation.First,
        [Spell.FireWall] = CombatSpecialisation.First,
        [Spell.GlacialGate] = CombatSpecialisation.Second, [Spell.FrostCrunch] = CombatSpecialisation.Second,
        [Spell.IceStorm] = CombatSpecialisation.Second,
        [Spell.StormRelay] = CombatSpecialisation.Third, [Spell.ThunderBolt] = CombatSpecialisation.Third,
        [Spell.Lightning] = CombatSpecialisation.Third,
        [Spell.LanternLink] = CombatSpecialisation.First, [Spell.MassHealing] = CombatSpecialisation.First,
        [Spell.Distil] = CombatSpecialisation.Second, [Spell.Curse] = CombatSpecialisation.Second,
        [Spell.SpiritCommand] = CombatSpecialisation.Third, [Spell.SummonSkeleton] = CombatSpecialisation.Third
    };

    public static bool Supports(MirClass @class) => @class is MirClass.Warrior or MirClass.Wizard or MirClass.Taoist;

    public static TalentTree Tree(MirClass @class, CombatSpecialisation spec = CombatSpecialisation.Unchosen) =>
        Trees.GetValueOrDefault((@class, spec));

    public static ushort NodeId(MirClass @class, CombatSpecialisation spec, int row, int column) =>
        (ushort)(((int)@class * 4 + (int)spec) * 32 + (row - 1) * 3 + column + 1);

    public static int Points(ushort level, bool shared) => shared
        ? (level < 10 ? 0 : Math.Min(15, 1 + (level - 10) / 3))
        : (level < 20 ? 0 : Math.Min(18, 1 + (level - 20) / 3));

    public static bool TryGetClass(Spell spell, out MirClass @class) => Owners.TryGetValue(spell, out @class);

    public static IEnumerable<Spell> NativeSpells(MirClass @class) => SpellCurriculum.ForClass(@class).Concat(
        TalentAbilities.All.Where(talent => talent.Spell.Class == @class).Select(talent => talent.Spell.Spell));

    public static bool BaseGrant(MirClass @class, CombatSpecialisation spec, ushort level, Spell spell) =>
        ClassSpells.TryGetValue(spell, out var grant) ? grant.Class == @class && level >= grant.Level :
        level >= 20 && TryGetClass(spell, out MirClass owner) && owner == @class &&
        SpecSpells.TryGetValue(spell, out var required) && required == spec;

    public static Spell Family(Spell spell) => spell switch
    {
        Spell.Oathbrand => Spell.SteelCut,
        Spell.DrakesEcho => Spell.TwinDrakeBlade,
        Spell.CrossHalfMoon => Spell.HalfMoon,
        Spell.ImmortalSkin => Spell.ProtectionField,
        Spell.GreatFireBall or Spell.FlameDisruptor or Spell.FrostCrunch or Spell.ThunderBolt => Spell.FireBall,
        Spell.MeteorStrike => Spell.AshSeal,
        Spell.IceThrust => Spell.FractureSpear,
        Spell.CausticScript => Spell.SoulFireBall,
        Spell.SummonShinsu or Spell.SummonHolyDeva => Spell.SummonSkeleton,
        Spell.SlashingBurst => Spell.Oathstep,
        Spell.FireBang => Spell.KilnBurst,
        Spell.FlameField => Spell.PhoenixShroud,
        Spell.HealingCircle => Spell.TendingBell,
        Spell.Plague => Spell.PoisonCloud,
        Spell.RedSlip or Spell.BastionBreak or Spell.ShedCinders or Spell.Thawskin or Spell.GroundingStep or
            Spell.UnbindKnot or Spell.AntivenomVeil or Spell.SoulUnshackle => Spell.CrossguardBreak,
        _ => spell
    };

    public static bool Validate(TalentTree tree, ushort level, IReadOnlyList<TalentPick> picks, out string reason)
    {
        if (tree == null)
        {
            reason = picks.Count == 0 ? null : "Choose a specialisation first.";
            return reason == null;
        }
        if (picks.Count > tree.Nodes.Length || picks.Sum(pick => (int)pick.Rank) > Points(level, tree.Shared))
        {
            reason = "Too many points spent.";
            return false;
        }

        var byId = new Dictionary<ushort, TalentPick>();
        foreach (TalentPick pick in picks)
        {
            TalentNode node = tree.Nodes.FirstOrDefault(node => node.Id == pick.Id);
            if (node == null || pick.Option >= node.Options.Length || pick.Rank == 0 || pick.Rank > node.MaxRank ||
                !byId.TryAdd(pick.Id, pick))
            {
                reason = "Invalid talent, option or rank.";
                return false;
            }
        }

        if (CountRow(tree.LastRow) > 1 || (!tree.Shared && CountRow(4) > 1))
        {
            reason = "Choose one rotation engine and one capstone per tree.";
            return false;
        }
        foreach (TalentNode node in tree.Nodes)
        {
            if (!byId.ContainsKey(node.Id)) continue;
            int earlier = tree.Nodes.Where(other => other.Row < node.Row).Sum(other => byId.GetValueOrDefault(other.Id).Rank);
            if (earlier < Gate(tree.Shared, node.Row) || !HasParents(node))
            {
                reason = $"{node.Name} needs its prerequisites and {Gate(tree.Shared, node.Row)} points in earlier rows.";
                return false;
            }
        }

        reason = null;
        return true;

        bool Full(int row, int column)
        {
            TalentNode parent = tree.At(row, column);
            return byId.GetValueOrDefault(parent.Id).Rank == parent.MaxRank;
        }

        int CountRow(int row) => tree.Nodes.Count(node => node.Row == row && byId.ContainsKey(node.Id));

        bool HasParents(TalentNode node)
        {
            int row = node.Row, column = node.Column;
            if (row == 1) return true;
            if (row == 3 && column == 1) return Full(2, 0) || Full(2, 1) || Full(2, 2);
            if (tree.Shared) return Full(row - 1, column) || (row == 5 && Full(4, 1));
            if (row == 4) return CountRow(3) > 0;
            if (row == 5) return CountRow(4) == 1 && Full(3, column);
            return Full(row - 1, column) || (row == 6 && Full(5, 1));
        }
    }

    private static int Gate(bool shared, int row) => shared
        ? row switch { 3 or 4 => 4, 5 => 9, 6 => 12, _ => 0 }
        : row switch { 3 => 4, 4 => 5, 5 => 6, 6 => 10, 7 => 12, 8 => 14, _ => 0 };

    private static IReadOnlyDictionary<(MirClass, CombatSpecialisation), TalentTree> Load()
    {
        using var stream = typeof(TalentCatalog).Assembly.GetManifestResourceStream("Crystal.TalentTrees.json");
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Deserialize<TalentTree[]>(stream, options).ToDictionary(tree => (tree.Class, tree.Spec));
    }
}
