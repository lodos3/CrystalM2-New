using Server.MirDatabase;

namespace Server.MirObjects;

public abstract partial class MapObject
{
    private const int MaxOwnedDosesPerColour = 4;
    internal long VoluntaryDistance { get; private protected set; }

    protected bool IsSamePoisonSlot(Poison current, Poison incoming) => current.PType == incoming.PType &&
        (!(current.TalentDose || incoming.TalentDose) || current.Source == incoming.Source);

    protected bool CanAddOwnedDose(Poison incoming) => !incoming.TalentDose || PoisonList.Count(dose =>
        dose.TalentDose && dose.PType == incoming.PType && dose.Time < dose.Duration) < MaxOwnedDosesPerColour;

    internal int TakePoisonTickPower(Poison poison)
    {
        if (poison.PType == PoisonType.Green)
        {
            // Keep ownership and timers for every accepted dose. Only the two strongest deal damage.
            int index = PoisonList.IndexOf(poison), stronger = 0;
            for (int i = 0; i < PoisonList.Count; i++)
            {
                var other = PoisonList[i];
                if (other == poison || other.PType != PoisonType.Green || other.Time >= other.Duration ||
                    (other.Source is { } owner && owner.Node == null)) continue;
                if (other.Value > poison.Value || (other.Value == poison.Value && index >= 0 && i < index)) stronger++;
                if (stronger >= 2) return 0;
            }
        }
        int power = (int)Math.Clamp((long)poison.Value + poison.NextTickBonus, 0, int.MaxValue);
        poison.NextTickBonus = 0;
        return power;
    }

    protected bool IsExpiredTalentStatus(Buff buff) => TalentStatuses.TryGet(buff.Type, out _) &&
        (buff.Caster is not HumanObject source || source.CurrentMap != CurrentMap ||
         buff.Values is not { Length: >= 2 } || buff.Values[1] != source.TalentEffectGeneration);

    private static readonly BuffType[] AttackPenalties = [BuffType.TalentOffBalance, BuffType.TalentWinded,
        BuffType.TalentLanternsRebuke, BuffType.TalentDulledFervour, BuffType.TalentScoredMeridian, BuffType.TalentHounded];

    protected int ResolveTalentIncoming(MapObject attacker, int damage, DefenceType type)
    {
        if (damage <= 0 || attacker == null) return damage;
        bool magical = type is DefenceType.MAC or DefenceType.MACAgility;
        Buff strongest = null;
        foreach (BuffType effect in AttackPenalties)
        {
            if (!attacker.HasBuff(effect, out Buff debuff) || debuff.FlagForRemoval || attacker.IsExpiredTalentStatus(debuff) || debuff.Values.Length == 0 ||
                debuff.Values[0] <= (strongest?.Values[0] ?? 0)) continue;
            bool applies = effect switch
            {
                BuffType.TalentDulledFervour => magical,
                BuffType.TalentScoredMeridian => magical && debuff.Caster == this,
                BuffType.TalentLanternsRebuke => !magical,
                BuffType.TalentHounded => Master == debuff.Caster,
                _ => !magical && debuff.Caster == this
            };
            if (applies) strongest = debuff;
        }
        int cover = 0;
        if (this is HumanObject human)
        {
            cover = human.ClassIncomingProtection(attacker, type);
            if (Buffs.Count > 0) cover = Math.Max(cover, human.TalentFieldProtection(attacker, type, Math.Max(cover, strongest?.Values[0] ?? 0)));
        }
        if (strongest == null) return (int)((long)damage * (100 - cover) / 100);
        if (strongest.Values[0] < cover) return (int)((long)damage * (100 - cover) / 100);
        attacker.RemoveBuff(strongest.Type);
        return (int)((long)damage * (100 - Math.Clamp(strongest.Values[0], 0, 25)) / 100);
    }
}
