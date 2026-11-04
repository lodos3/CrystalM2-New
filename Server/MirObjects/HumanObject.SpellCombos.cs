using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    private SpellComboProgress _combo;
    private List<HealComboCredit> _healComboCredits;
    private readonly object _comboLock = new();
    private readonly object _healComboLock = new();

    private readonly record struct HealComboCredit(HumanObject Healer, int Generation, int Behind, int Amount);

    private bool OwnsCombo(Spell spender) => !Dead && Level >= SpellCurriculum.SpecialisationUnlockLevel &&
        SpecialisationAbilities.TryGet(spender, out var ability) && ability.Class == Info.Class &&
        ability.Spec == Info.Specialisation;

    private void BuildCombo(Spell spender, MapObject target = null)
    {
        if (!OwnsCombo(spender)) return;

        SpellCombo combo = SpellCombos.For(spender);
        if (combo.TargetBound && (target == null || target.Dead || target.Node == null || target.CurrentMap != CurrentMap))
            return;

        lock (_comboLock)
        {
            if (_combo.Build(combo, Envir.Time, target?.ObjectID ?? 0)) SendCombo(spender);
        }
    }

    private byte ComboStacks(Spell spender, MapObject target = null)
    {
        if (!OwnsCombo(spender)) return 0;
        lock (_comboLock)
            return _combo.Count(Envir.Time, target?.ObjectID ?? 0);
    }

    private void SpendCombo(Spell spender, MapObject target = null)
    {
        if (!OwnsCombo(spender)) return;
        lock (_comboLock)
        {
            if (_combo.Spend(Envir.Time, target?.ObjectID ?? 0)) SendCombo(spender);
        }
    }

    private void SendCombo(Spell spender)
    {
        // Heroes' normal Enqueue path forwards only selected packet types.
        var packet = new S.SpellComboState
        {
            ObjectID = ObjectID,
            Spender = spender,
            TargetID = _combo.TargetId,
            Stacks = _combo.Count(Envir.Time, _combo.TargetId),
            Remaining = (ushort)Math.Clamp(_combo.Expires - Envir.Time, 0, ushort.MaxValue)
        };
        if (this is HeroObject hero) hero.Owner?.Enqueue(packet);
        else Enqueue(packet);
    }

    private void ResetCombo()
    {
        lock (_comboLock)
        {
            bool wasActive = _combo.Count(Envir.Time, _combo.TargetId) > 0;
            _combo = default;
            if (wasActive) SendCombo(Spell.None);
        }
        lock (_healComboLock)
            _healComboCredits?.Clear();
    }

    internal void RecordComboHit(Spell spell, MapObject target)
    {
        if (target.CurrentMap != CurrentMap) return;
        switch (spell)
        {
            case Spell.Thrusting:
                if (Functions.MaxDistance(CurrentLocation, target.CurrentLocation) == 2)
                    BuildCombo(Spell.Notchbreaker, target);
                break;
            case Spell.FlamingSword:
                BuildCombo(Spell.Notchbreaker, target);
                break;
            case Spell.HalfMoon:
            case Spell.CrossHalfMoon:
                BuildCombo(Spell.RedWake);
                break;
            case Spell.GreatFireBall:
            case Spell.FireWall:
                BuildCombo(Spell.AshSeal);
                break;
            case Spell.FrostCrunch:
                BuildCombo(Spell.GlacialGate, target);
                break;
            case Spell.ThunderBolt:
            case Spell.Lightning:
                BuildCombo(Spell.StormRelay);
                break;
            case Spell.SoulFireBall:
            case Spell.SpiritSpark:
                if (OwnsCombo(Spell.SpiritCommand) && Pets.Exists(pet =>
                    IsSignatureCompanion(pet, Spell.SpiritCommand) &&
                    Functions.InRange(CurrentLocation, pet.CurrentLocation, 8)))
                    BuildCombo(Spell.SpiritCommand);
                break;
        }
    }

    internal bool HasRime(MapObject target) => ComboStacks(Spell.GlacialGate, target) > 0;

    internal void SpendRime(MapObject target) => SpendCombo(Spell.GlacialGate, target);

    internal void ApplyRimeSlow(MapObject target) => SlowSignatureTarget(target, 2);

    internal static int ComboDamage(int power, int percent) =>
        (int)Math.Clamp((long)power * percent / 100, 0, int.MaxValue);

    internal void ApplyInkPoison(MapObject target, Poison poison, bool ignoreDefence = true)
    {
        target.ApplyPoison(poison, this, ignoreDefence: ignoreDefence);
        if (OwnsCombo(Spell.Distil) && target.PoisonList.Contains(poison))
            BuildCombo(Spell.Distil, target);
    }

    private void RecordGuard(int armour, int damage)
    {
        if (damage > 0 && armour > 0 && OwnsCombo(Spell.Interpose) &&
            (HasBuff(BuffType.ProtectionField) || HasBuff(BuffType.ImmortalSkin)))
            BuildCombo(Spell.Interpose);
    }

    internal void QueueSpellHealing(MapObject target, int power)
    {
        if (target is not HumanObject recipient)
        {
            target.HealAmount = (ushort)Math.Clamp(target.HealAmount + (long)power, 0, ushort.MaxValue);
            return;
        }

        lock (recipient._healComboLock)
        {
            int before = recipient.HealAmount;
            int amount = Math.Min(Math.Max(0, power), ushort.MaxValue - before);
            recipient.HealAmount = (ushort)(before + amount);
            if (amount == 0 || !OwnsCombo(Spell.LanternLink)) return;
            var pending = recipient._healComboCredits ??= new List<HealComboCredit>(2);
            if (pending.Count < 8) pending.Add(new HealComboCredit(this, _signatureGeneration, before, amount));
        }
    }

    private int DrainSpellHealing(int healthRegen)
    {
        lock (_healComboLock)
        {
            int drained = Math.Min(HealAmount, 5 + Level / 10 + HealAmount / 10);
            int effective = Math.Clamp(MaxHealth - HP - healthRegen, 0, drained);
            HealAmount -= (ushort)drained;
            ResolveHealComboCredits(drained, effective);
            return drained;
        }
    }

    private void ClearSpellHealing()
    {
        lock (_healComboLock)
        {
            HealAmount = 0;
            _healComboCredits?.Clear();
        }
    }

    private void ResolveHealComboCredits(int drained, int effective)
    {
        if (_healComboCredits == null || drained <= 0) return;
        for (int i = _healComboCredits.Count - 1; i >= 0; i--)
        {
            HealComboCredit credit = _healComboCredits[i];
            if (credit.Behind < effective && credit.Healer.CurrentMap == CurrentMap && credit.Healer.Node != null &&
                credit.Healer._signatureGeneration == credit.Generation)
            {
                credit.Healer.BuildCombo(Spell.LanternLink);
                _healComboCredits.RemoveAt(i);
            }
            else if (credit.Behind + credit.Amount <= drained)
                _healComboCredits.RemoveAt(i);
            else
                _healComboCredits[i] = credit with
                {
                    Behind = Math.Max(0, credit.Behind - drained),
                    Amount = credit.Amount - Math.Max(0, drained - credit.Behind)
                };
        }
    }
}
