using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    private SpellComboProgress _combo;
    private List<HealComboCredit> _healComboCredits;
    private readonly object _comboLock = new();
    private readonly object _healComboLock = new();

    private readonly record struct HealComboCredit(HumanObject Healer, int Generation, int Behind, int Amount, Spell Spell, TalentCastCredit Credit);

    private bool OwnsCombo(Spell spender) => !Dead && Level >= SpellCurriculum.SpecialisationUnlockLevel &&
        SpecialisationAbilities.TryGet(spender, out var ability) && ability.Class == Info.Class &&
        ability.Spec == Info.Specialisation;

    private void BuildCombo(Spell spender, MapObject target = null)
    {
        if (!OwnsCombo(spender)) return;

        SpellCombo combo = SpellCombos.For(spender);
        if (spender == Spell.Notchbreaker && Talent(3, 1, 0) > 0)
            combo = combo with { Duration = 8000 };
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

    private void SpendCombo(Spell spender, MapObject target = null, Spell action = Spell.None, int variant = 0,
        MapObject ally = null, System.Drawing.Point? tile = null, int amount = int.MaxValue)
    {
        if (!OwnsCombo(spender)) return;
        byte stacks;
        lock (_comboLock)
        {
            stacks = (byte)Math.Min(amount, _combo.Count(Envir.Time, target?.ObjectID ?? 0));
            if (stacks == 0 || !_combo.Spend(Envir.Time, target?.ObjectID ?? 0, stacks)) return;
            SendCombo(spender);
        }
        ObserveRotation(RotationEvent.Spend, action == Spell.None ? spender : action, target, stacks, variant, ally, tile);
        TalentSpendModifiers(spender, target, action, stacks, variant);
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
        int detail = spell == Spell.Thrusting && Functions.MaxDistance(CurrentLocation, target.CurrentLocation) == 2
            ? TalentRotations.FarHit : 0;
        ObserveRotation(RotationEvent.Hit, spell, target, variant: detail);
        TalentHitModifiers(spell, target);
        FollowTalentHit(spell, target);
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
            case Spell.FlameDisruptor:
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

    internal void SpendRime(MapObject target, Spell action = Spell.GlacialGate, bool far = false) =>
        SpendCombo(Spell.GlacialGate, target, action, far ? TalentRotations.FarHit : 0);

    internal void ApplyRimeSlow(MapObject target) => SlowSignatureTarget(target, 2);

    internal static int ComboDamage(int power, int percent) =>
        (int)Math.Clamp((long)power * percent / 100, 0, int.MaxValue);

    internal void ApplyInkPoison(MapObject target, Poison poison, bool ignoreDefence = true, Spell action = Spell.Poisoning)
    {
        bool fresh = action == Spell.Poisoning && !target.PoisonList.Any(dose => dose.Source == this &&
            dose.PType is PoisonType.Red or PoisonType.Green);
        poison.TalentDose = Class == MirClass.Taoist;
        poison.DoseGeneration = _signatureGeneration;
        if (poison.TalentDose && action == Spell.Poisoning)
        {
            if (ClassTalent(1, 2) > 0) poison.Duration++;
            if (poison.PType == PoisonType.Red && ClassTalent(3, 2, 0) > 0)
            {
                poison.Duration += 2;
                poison.RedStrength = 90;
            }
            if (poison.PType == PoisonType.Green && poison.Duration > 2 && ClassTalent(3, 2, 1) > 0)
            {
                poison.Duration--;
                poison.NextTickBonus += poison.Value;
            }
        }
        if (poison.PType == PoisonType.Green && OwnsCombo(Spell.Distil) && Talent(1, 2) > 0)
            poison.NextTickBonus += Math.Max(1, poison.Value / 10);
        target.ApplyPoison(poison, this, ignoreDefence: ignoreDefence);
        if (fresh && target.PoisonList.Contains(poison))
        {
            PrimePoisonSense(target);
            if (ClassTalent(2, 2) > 0) ChangeMP(ClassTalent(2, 2));
            if (Envir.Time < _nextPoisonUntil && _lastPoisonTarget != target)
            {
                _nextPoisonUntil = 0;
                ChangeMP(2);
            }
        }
        if (OwnsCombo(Spell.Distil) && target.PoisonList.Contains(poison))
        {
            BuildCombo(Spell.Distil, target);
            ObserveRotation(RotationEvent.Poison, action, target, variant:
                poison.PType == PoisonType.Red ? TalentRotations.RedDose : TalentRotations.GreenDose);
        }
    }

    private void RecordGuard(int armour, int damage, MapObject attacker)
    {
        if (damage <= 0 || attacker == null) return;
        ApplyTalentGuardMitigation(attacker, damage, armour);
        if (armour > 0 && OwnsCombo(Spell.Interpose) && (HasBuff(BuffType.ProtectionField) || HasBuff(BuffType.ImmortalSkin)))
        {
            BuildCombo(Spell.Interpose);
            ObserveRotation(RotationEvent.Mitigate, Spell.ProtectionField, attacker);
        }
        if (HasBuff(BuffType.MagicShield)) ObserveRotation(RotationEvent.Mitigate, Spell.MagicShield, attacker);
    }

    internal void QueueSpellHealing(MapObject target, int power, Spell action = Spell.Healing, TalentCastCredit credit = null)
    {
        int echo = power > 0 && target.Health < target.MaxHealth ? GetMagic(action)?.Perk(SpellPerk.EchoHeal) ?? 0 : 0;
        if (echo > 0 && CurrentMap != null && target.CurrentMap == CurrentMap &&
            TalentAllies(target.CurrentLocation, 3).FirstOrDefault(ally => ally != target && ally.Health + ally.HealAmount < ally.MaxHealth) is { } other &&
            (credit == null || credit.ClaimHealEcho()))
        {
            QueueSpellHealing(other, Math.Max(1, ScaleSignature(power, 20 + echo * 10)), Spell.None);
            BroadcastLegacyImpact(action, other.CurrentLocation);
        }
        if (Class == MirClass.Taoist && action == Spell.Healing && target.PercentHealth < 35 && ClassTalent(1, 0) > 0)
            power = ScaleSignature(power, 112);
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
            if (amount == 0 || Class != MirClass.Taoist) return;
            var pending = recipient._healComboCredits ??= new List<HealComboCredit>(2);
            if (pending.Count < 8) pending.Add(new HealComboCredit(this, _signatureGeneration, before, amount, action, credit));
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
                credit.Healer.CreditTalentHealing(this, credit.Spell, Math.Min(credit.Amount, effective - credit.Behind), credit: credit.Credit);
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

    private void CreditTalentHealing(HumanObject recipient, Spell spell, int effective, bool grantReserve = true, TalentCastCredit credit = null)
    {
        if (effective <= 0 || credit?.Claim() == false) return;
        OnTaoistHealing(recipient, spell);
        if (spell == Spell.Healing && ClassTalent(2, 0) > 0 && _lastHealedTarget != recipient && Envir.Time >= _lastTalentRefund)
        {
            _lastTalentRefund = Envir.Time + 4000;
            ChangeMP(ClassTalent(2, 0));
        }
        if (spell == Spell.Healing) _lastHealedTarget = recipient;
        if (!OwnsCombo(Spell.LanternLink)) return;
        if (grantReserve) BuildCombo(Spell.LanternLink);
        ObserveRotation(RotationEvent.Heal, spell, recipient, effective, origin: credit?.Id ?? 0);
        if (spell is not (Spell.Healing or Spell.LanternLink) || Envir.Time >= _wickUntil ||
            _wickTarget is not HumanObject wick || wick == recipient || !CanShieldSignatureTarget(wick) ||
            !SignatureReach(wick.CurrentLocation, 7) || wick.HP >= wick.MaxHealth) return;
        _wickUntil = 0;
        _wickTarget = null;
        wick.RemoveBuff(BuffType.TalentWick);
        int transfer = HealTalentTarget(wick, Math.Max(1, effective * (25 + Talent(2, 0) * 4) / 100), Spell.GuidingWick);
        if (transfer > 0)
        {
            ObserveRotation(RotationEvent.Wick, Spell.GuidingWick, wick, transfer);
            if (Talent(8, 0) > 0 && TalentAllies(wick.CurrentLocation, 2).FirstOrDefault(ally => ally != wick && ally != recipient && ally.HP < ally.MaxHealth) is { } second)
                HealTalentTarget(second, Math.Max(1, transfer / 2), Spell.GuidingWick);
        }
    }

    private void FollowTalentHit(Spell spell, MapObject target)
    {
        if (Envir.Time >= _echoUntil || !CanHitSignatureTarget(target)) return;
        if (OwnsCombo(Spell.RedWake) && GetMagic(Spell.DrakesEcho) is { } drake &&
            (spell == Spell.None || SpellCurriculum.Owns(MirClass.Warrior, spell) || spell == Spell.GaleCleaver))
        {
            if (_echoTarget == null)
            {
                _echoTarget = target;
                ApplyTalentStatus(target, BuffType.TalentDrakeMark, (int)(_echoUntil - Envir.Time));
                ObserveRotation(RotationEvent.Hit, Spell.DrakesEcho, target);
                return;
            }
            MapObject marked = _echoTarget;
            if (marked == target || !CanHitSignatureTarget(marked) || !Functions.InRange(target.CurrentLocation, marked.CurrentLocation, 3)) return;
            _echoUntil = 0;
            _echoTarget = null;
            marked.RemoveBuff(BuffType.TalentDrakeMark);
            if (HitSignatureTarget(marked, Spell.DrakesEcho, Math.Max(1, Stats[Stat.MaxDC] * 3 / 5), DefenceType.AC))
            {
                ResolveSpellPerks(drake);
                RecordDrakeFollowup(Spell.DrakesEcho);
            }
        }
        if (OwnsCombo(Spell.SpiritCommand) && HasBuff(BuffType.TalentEchoFang, out var buff) && ComboStacks(Spell.SpiritCommand) > 0 &&
            BoundSpirit() is { } spirit && Functions.InRange(spirit.CurrentLocation, target.CurrentLocation, 6) && spirit.CanFly(target.CurrentLocation))
        {
            _echoUntil = 0;
            RemoveBuff(BuffType.TalentEchoFang);
            if (HitSignatureTarget(target, Spell.EchoFang, buff.Values[0], DefenceType.MAC))
                SpendCombo(Spell.SpiritCommand, amount: 1);
        }
    }
}
