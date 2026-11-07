public enum AbilityTarget : byte { Enemy, Ground, Self, Ally, Companion }

public enum AbilityTheme : byte { Steel, Blood, Ward, Flame, Frost, Storm, Spirit, Venom, Shadow, Arrow }

public sealed record SpecialisationAbility(Spell Spell, MirClass Class, CombatSpecialisation Spec,
    string Name, AbilityTarget Target, AbilityTheme Theme, byte Range, byte Mana, ushort Cooldown,
    ushort Windup, ushort Power, float Scaling, byte Icon, string Description, byte RequiredLevel = 0)
{
    public byte UnlockLevel => RequiredLevel != 0 ? RequiredLevel :
        Spec == CombatSpecialisation.Unchosen ? (byte)1 : (byte)SpellCurriculum.SpecialisationUnlockLevel;

    public int CastRecovery => TalentCatalog.Supports(Class) ? Math.Max(600, Windup + 120) : 1800;
}

public sealed record SpecialisationTrait(string Name, byte Level, string Description);

public static class SpecialisationAbilities
{
    private const CombatSpecialisation Core = CombatSpecialisation.Unchosen;
    private const CombatSpecialisation First = CombatSpecialisation.First;
    private const CombatSpecialisation Second = CombatSpecialisation.Second;
    private const CombatSpecialisation Third = CombatSpecialisation.Third;

    public const byte AdvancedTraitLevel = 32;
    public const byte MasterTraitLevel = 45;

    private static readonly SpecialisationAbility[] Definitions =
    {
        new(Spell.SteelCut, MirClass.Warrior, Core, "Steel Cut", AbilityTarget.Enemy, AbilityTheme.Steel, 1, 3, 2200, 120, 3, .90f, 6,
            "Deliver a weapon strike to an adjacent enemy."),
        new(Spell.SpiritSpark, MirClass.Taoist, Core, "Spirit Spark", AbilityTarget.Enemy, AbilityTheme.Spirit, 6, 4, 2400, 230, 4, .83f, 4,
            "Strike one enemy with spirit energy. Requires mana only."),
        new(Spell.QuickCut, MirClass.Assassin, Core, "Quick Cut", AbilityTarget.Enemy, AbilityTheme.Shadow, 1, 3, 2200, 110, 3, .88f, 6,
            "Deliver a quick slash to an adjacent enemy."),
        new(Spell.Notchbreaker, MirClass.Warrior, First, "Notchbreaker", AbilityTarget.Enemy, AbilityTheme.Steel, 2, 12, 6400, 260, 10, 1.30f, 11,
            "Deliver a committed thrust. The far tile deals more damage."),
        new(Spell.RedWake, MirClass.Warrior, Second, "Red Wake", AbilityTarget.Self, AbilityTheme.Blood, 0, 15, 8300, 340, 9, 1.05f, 18,
            "Sweep nearby enemies. The first target takes the full strike. The rest take reduced damage."),
        new(Spell.Interpose, MirClass.Warrior, Third, "Interpose", AbilityTarget.Ally, AbilityTheme.Ward, 5, 15, 11500, 250, 20, .75f, 2,
            "Give an ally or yourself a four-second shield. The stronger ward takes precedence."),
        new(Spell.AshSeal, MirClass.Wizard, First, "Ash Seal", AbilityTarget.Ground, AbilityTheme.Flame, 7, 19, 10500, 760, 10, 1.30f, 4,
            "Mark a tile. It erupts three times, damaging nearby enemies who stay in the seal."),
        new(Spell.GlacialGate, MirClass.Wizard, Second, "Glacial Gate", AbilityTarget.Ground, AbilityTheme.Frost, 7, 16, 9000, 540, 10, .96f, 3,
            "Raise a three-tile strip of frost across the cast direction. Enemies hit are slowed."),
        new(Spell.StormRelay, MirClass.Wizard, Third, "Storm Relay", AbilityTarget.Enemy, AbilityTheme.Storm, 8, 17, 8200, 290, 10, 1.24f, 8,
            "Strike an enemy, then chain to up to two distinct nearby enemies for reduced damage."),
        new(Spell.LanternLink, MirClass.Taoist, First, "Lantern Link", AbilityTarget.Ally, AbilityTheme.Ward, 7, 17, 10000, 350, 18, .86f, 2,
            "Restore an ally's missing health and grant a short absorption ward."),
        new(Spell.Distil, MirClass.Taoist, Second, "Distil", AbilityTarget.Enemy, AbilityTheme.Venom, 7, 13, 7400, 280, 8, 1.05f, 7,
            "Consume your red or green poison on an enemy to deal damage. A fresh poison yields a stronger dose."),
        new(Spell.SpiritCommand, MirClass.Taoist, Third, "Spirit Command", AbilityTarget.Companion, AbilityTheme.Spirit, 8, 12, 9000, 220, 12, .70f, 4,
            "Mend one bound spirit. Target an enemy to order focus or cast without an enemy to recall the spirit toward you."),
        new(Spell.VeilStrike, MirClass.Assassin, First, "Veil Strike", AbilityTarget.Enemy, AbilityTheme.Shadow, 2, 13, 7200, 210, 10, 1.25f, 6,
            "Strike an enemy. Attacking from concealment adds damage and opens a short bleed."),
        new(Spell.Riposte, MirClass.Assassin, Second, "Riposte", AbilityTarget.Enemy, AbilityTheme.Steel, 1, 12, 6800, 150, 8, 1.10f, 11,
            "Counter with a close strike. A landed hit grants a short personal ward."),
        new(Spell.SmokeSnare, MirClass.Assassin, Third, "Smoke Snare", AbilityTarget.Ground, AbilityTheme.Shadow, 5, 16, 10000, 560, 8, .85f, 7,
            "Release smoke around the marked tile. Enemies caught in the cloud take damage and are slowed."),
        new(Spell.Pinpoint, MirClass.Archer, First, "Pinpoint", AbilityTarget.Enemy, AbilityTheme.Arrow, 9, 14, 7400, 430, 10, 1.35f, 8,
            "Draw and release a precise shot. Distant targets take more damage."),
        new(Spell.CinderMine, MirClass.Archer, Second, "Cinder Mine", AbilityTarget.Ground, AbilityTheme.Flame, 6, 17, 10500, 770, 10, 1.15f, 4,
            "Arm a visible mine. It detonates once when an enemy approaches, then expires if undisturbed."),
        new(Spell.PackSignal, MirClass.Archer, Third, "Pack Signal", AbilityTarget.Companion, AbilityTheme.Spirit, 8, 12, 8500, 220, 9, .65f, 4,
            "Heal your nearby combat beast. Command it to attack your enemy or call it back to you.")
    };

    private static readonly IReadOnlyDictionary<Spell, SpecialisationAbility> BySpell =
        Definitions.ToDictionary(x => x.Spell);

    public static IReadOnlyList<SpecialisationAbility> All { get; } = Array.AsReadOnly(Definitions);
    public static bool TryGet(Spell spell, out SpecialisationAbility ability)
    {
        if (BySpell.TryGetValue(spell, out ability)) return true;
        if (!TalentAbilities.TryGet(spell, out var talent)) return false;
        ability = talent.Spell;
        return true;
    }

    public static SpecialisationAbility Signature(MirClass @class, CombatSpecialisation spec) =>
        spec == Core ? null : Array.Find(Definitions, a => a.Class == @class && a.Spec == spec);

    private static readonly IReadOnlyDictionary<Spell, IReadOnlyList<SpecialisationTrait>> TraitSets =
        new Dictionary<Spell, IReadOnlyList<SpecialisationTrait>>
    {
        [Spell.Notchbreaker] = Traits("Measured Grip", "Gain +4 accuracy.",
            "Tempered Edge", "Notchbreaker's far-tile bonus increases.",
            "Finish the Line", "A landed far-tile Notchbreaker restores 4 MP."),
        [Spell.RedWake] = Traits("Rising Heat", "Gain +3 maximum DC.",
            "Wide Arc", "Red Wake can hit a fifth target.",
            "Cool Blood", "Red Wake restores 4 MP after hitting at least three enemies."),
        [Spell.Interpose] = Traits("Set Feet", "Gain +3 minimum and maximum AC.",
            "Brace Line", "Interpose absorbs 25% more damage.",
            "Stand Together", "An Interpose ward placed on someone else also grants you a smaller ward."),
        [Spell.AshSeal] = Traits("Kindling", "Gain +3 maximum MC.",
            "Banked Heat", "Ash Seal's final pulse deals extra damage.",
            "Cauterise", "Restore 4 MP if the last pulse hits an enemy."),
        [Spell.GlacialGate] = Traits("Cold Snap", "Gain +3 maximum MAC.",
            "Fracture", "Glacial Gate deals extra damage to an already slowed enemy.",
            "Winter Margin", "Glacial Gate grants a brief personal ward when it lands."),
        [Spell.StormRelay] = Traits("Conductive Path", "Gain +4 accuracy.",
            "Return Current", "Storm Relay can chain to a fourth distinct enemy.",
            "Grounded", "Restore 4 MP when Storm Relay hits three enemies."),
        [Spell.LanternLink] = Traits("Tended Flame", "Gain +3 maximum SC.",
            "Quiet Ward", "Effective healing from Lantern Link adds to its ward.",
            "Clean Hands", "Lantern Link removes one green or red poison from its recipient."),
        [Spell.Distil] = Traits("Fresh Ink", "Gain +3 maximum SC.",
            "Measured Dose", "Distil gains more power from the remaining poison duration.",
            "Spillover", "A successful Distil transfers a short, weaker dose to one nearby enemy."),
        [Spell.SpiritCommand] = Traits("Bound Thread", "Gain +3 maximum MAC.",
            "Command Bond", "Spirit Command mends your spirit for 25% more.",
            "Safe Return", "Calling your spirit back also restores some of your missing health."),
        [Spell.VeilStrike] = Traits("Silent Approach", "Gain +4 agility.",
            "Open Wound", "A concealed Veil Strike bleeds for an additional tick in PvE.",
            "Vanishing Point", "A successful concealed Veil Strike restores 4 MP."),
        [Spell.Riposte] = Traits("Edge Control", "Gain +4 accuracy.",
            "Second Guard", "A landed Riposte grants a stronger ward.",
            "Duel Rhythm", "Landing Riposte shortens its next cooldown by one second."),
        [Spell.SmokeSnare] = Traits("Powder Sense", "Gain +3 maximum AC.",
            "Lingering Smoke", "Smoke Snare slows PvE enemies for one additional second.",
            "Chain Disruption", "Restore 4 MP if the smoke hits at least two enemies."),
        [Spell.Pinpoint] = Traits("Steady Draw", "Gain +4 accuracy.",
            "Long Mark", "Pinpoint's damage bonus against distant enemies increases.",
            "Measured Breath", "A landed long-range Pinpoint restores 4 MP."),
        [Spell.CinderMine] = Traits("Fusecraft", "Gain +3 maximum DC.",
            "Hot Shrapnel", "Cinder Mine can hit a fifth target.",
            "Recovered Charge", "Recover 4 MP if Cinder Mine hits at least two enemies."),
        [Spell.PackSignal] = Traits("Keen Senses", "Gain +4 agility.",
            "Pack Recovery", "Pack Signal heals your beast for 25% more.",
            "Shared Instinct", "Calling your beast back also restores some of your missing health.")
    };

    public static IReadOnlyList<SpecialisationTrait> TraitsFor(Spell signature) =>
        TraitSets.TryGetValue(signature, out var traits) ? traits : Array.Empty<SpecialisationTrait>();

    private static IReadOnlyList<SpecialisationTrait> Traits(string first, string firstText,
        string second, string secondText, string third, string thirdText) =>
        Array.AsReadOnly(new SpecialisationTrait[]
        {
            new(first, (byte)SpellCurriculum.SpecialisationUnlockLevel, firstText),
            new(second, AdvancedTraitLevel, secondText),
            new(third, MasterTraitLevel, thirdText)
        });

    public static void ApplyTraitStats(Stats stats, MirClass @class, CombatSpecialisation spec, ushort level)
    {
        if (level < SpellCurriculum.SpecialisationUnlockLevel || TalentCatalog.Supports(@class)) return;
        Spell spell = Signature(@class, spec)?.Spell ?? Spell.None;
        switch (spell)
        {
            case Spell.Notchbreaker:
            case Spell.StormRelay:
            case Spell.Riposte:
            case Spell.Pinpoint:
                stats[Stat.Accuracy] += 4;
                break;
            case Spell.RedWake:
            case Spell.CinderMine:
                stats[Stat.MaxDC] += 3;
                break;
            case Spell.Interpose:
                stats[Stat.MinAC] += 3;
                stats[Stat.MaxAC] += 3;
                break;
            case Spell.AshSeal:
                stats[Stat.MaxMC] += 3;
                break;
            case Spell.GlacialGate:
            case Spell.SpiritCommand:
                stats[Stat.MaxMAC] += 3;
                break;
            case Spell.LanternLink:
            case Spell.Distil:
                stats[Stat.MaxSC] += 3;
                break;
            case Spell.VeilStrike:
            case Spell.PackSignal:
                stats[Stat.Agility] += 4;
                break;
            case Spell.SmokeSnare:
                stats[Stat.MaxAC] += 3;
                break;
        }
    }
}
