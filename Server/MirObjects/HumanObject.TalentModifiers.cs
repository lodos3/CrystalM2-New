using System.Drawing;
using Server.MirDatabase;

namespace Server.MirObjects;

public partial class HumanObject
{
    private long _lastTalentMove, _lastTalentRefund, _lastTalentGuard, _lastTalentHaste;
    private long _nextCrescentUntil, _nextIceStormUntil, _nextRecallUntil, _nextPoisonUntil;
    private MapObject _lastBoltTarget, _nextIceStormTarget, _lastPoisonTarget, _lastHealedTarget;
    private long _lastBoltHit;

    private Buff GetTalentModifier(BuffType status) => HasBuff(status, out Buff buff) &&
        !buff.FlagForRemoval && buff.ExpireTime > 0 && buff.Values.Length > 0 && !IsExpiredTalentStatus(buff) ? buff : null;

    private int TalentMagicCost(Spell spell, int cost)
    {
        BuffType status = TalentStatuses.ManaDiscount(spell);
        if (status != BuffType.None && GetTalentModifier(status) is { } modifier) cost -= modifier.Values[0];
        return Math.Max(0, cost);
    }

    private void SpendTalentMagicCost(Spell spell, int cost)
    {
        ChangeMP(-cost);
        RecordClassSpellCast(spell);
        BuffType status = TalentStatuses.ManaDiscount(spell);
        if (status != BuffType.None && GetTalentModifier(status) != null) RemoveBuff(status);
    }

    private void ApplyTalentRecovery(Spell spell)
    {
        BuffType status = spell switch
        {
            Spell.SteelCut or Spell.Oathbrand => BuffType.TalentHammerPoise,
            Spell.GreatFireBall or Spell.FlameDisruptor => BuffType.TalentFlashpoint,
            Spell.FrostCrunch => BuffType.TalentClearCrystal,
            Spell.Poisoning => BuffType.TalentInkskin,
            Spell.SpiritPounce => BuffType.TalentSpiritTempo,
            Spell.ProtectionField or Spell.ImmortalSkin => BuffType.TalentAnchorPoise,
            _ => BuffType.None
        };
        if (status == BuffType.None || GetTalentModifier(status) is not { } buff) return;
        int recovery = buff.Values[0];
        SpellTime = Math.Max(Envir.Time + 250, SpellTime - recovery);
        ActionTime = Math.Max(Envir.Time, ActionTime - recovery);
        RemoveBuff(status);
    }

    private void TalentHitModifiers(Spell spell, MapObject target)
    {
        if (IsPrimaryBolt(spell)) RecordWizardBolt();
        int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
        switch (spell)
        {
            case Spell.SteelCut:
            case Spell.Oathbrand:
                if (Envir.Time < _battleDrillUntil)
                {
                    _battleDrillUntil = 0;
                    ChangeMP(2);
                }
                if (ClassTalent(2, 2) > 0 && Class == MirClass.Warrior)
                    ApplyTalentStatus(this, BuffType.TalentAnchorPoise, 2000, Stat.MaxAC, ClassTalent(2, 2));
                if (OwnsCombo(Spell.Notchbreaker) && Talent(2, 2) > 0)
                    ApplyTalentStatus(this, BuffType.TalentFarFocus, 3500, Stat.Accuracy, Talent(2, 2) * 2);
                break;
            case Spell.Thrusting:
                if (distance == 2 && OwnsCombo(Spell.Notchbreaker))
                {
                    if (Talent(1, 2) > 0 && Envir.Time >= _lastTalentRefund) { ChangeMP(1); _lastTalentRefund = Envir.Time + 2500; }
                    if (Talent(6, 2) > 0) ApplyTalentStatus(this, BuffType.TalentStandard, 2200, value: 15);
                    RemoveBuff(BuffType.TalentFarFocus);
                }
                break;
            case Spell.FlamingSword:
                if (OwnsCombo(Spell.Notchbreaker) && Talent(5, 0, 0) > 0 && !target.Dead)
                    target.ApplyPoison(new Poison { Owner = this, PType = PoisonType.Bleeding, Duration = 2, TickSpeed = 1000,
                        TickTime = Envir.Time + 1000, Value = Math.Max(1, Stats[Stat.MaxDC] / 12) }, this);
                break;
            case Spell.HalfMoon:
            case Spell.CrossHalfMoon:
                if (OwnsCombo(Spell.RedWake) && ComboStacks(Spell.RedWake) == 0 && Talent(1, 1) > 0)
                    ApplyTalentStatus(this, BuffType.TalentRushingBlood, 2200, Stat.MaxDC, 2);
                if (GetTalentModifier(BuffType.TalentRedRhythm) is { } rhythm)
                {
                    ChangeMP(rhythm.Values[0]);
                    RemoveBuff(BuffType.TalentRedRhythm);
                }
                break;
            case Spell.GreatFireBall:
            case Spell.FlameDisruptor:
                _lastBoltTarget = target;
                _lastBoltHit = Envir.Time;
                break;
            case Spell.FrostCrunch:
                if (OwnsCombo(Spell.GlacialGate) && Talent(1, 2) > 0 && HasRime(target))
                    ApplyTalentStatus(this, BuffType.TalentRimeWard, 2200, Stat.MaxMAC, 2);
                break;
            case Spell.SpiritSpark:
            case Spell.SoulFireBall:
                if (OwnsCombo(Spell.SpiritCommand) && BoundSpirit() is { } pet && Talent(1, 2) > 0)
                    ApplyTalentStatus(pet, BuffType.TalentPackFocus, 3000, Stat.Accuracy, 3);
                break;
        }
    }

    private void RecordDrakeFollowup(Spell spell)
    {
        if (!OwnsCombo(Spell.RedWake) || Envir.Time < _lastTalentHaste) return;
        _lastTalentHaste = Envir.Time + 850;
        if (Talent(2, 2) > 0 && GetMagic(spell) is { } twin) ShortenTalentCooldown(spell, twin.GetDelay() * Talent(2, 2) / 20);
        if (Talent(6, 2) > 0) BuildCombo(Spell.RedWake);
        if (Talent(7, 1) > 0) ApplyTalentStatus(this, BuffType.TalentDrakeBreath, 4000, value: 3);
    }

    internal void OnTalentFireWallPlaced()
    {
        if (!OwnsCombo(Spell.AshSeal)) return;
        if (Talent(6, 2) > 0 || (Talent(2, 2) > 0 && HasBuff(BuffType.MagicShield)))
            ApplyTalentStatus(this, BuffType.TalentHeatSink, 5000, value: Talent(6, 2) * 3 + Talent(2, 2));
    }

    internal void OnTalentFireWallHit(MapObject target, int power)
    {
        RecordComboHit(Spell.FireWall, target);
        if (OwnsCombo(Spell.AshSeal) && Talent(6, 0) > 0 && GetMagic(Spell.FireWall) is { } wall)
            StartTalentTicks(wall, target, BuffType.TalentSmoulder, power / 4, 1, 1200);
    }

    internal int TalentCurseDuration(MapObject target, int duration) => OwnsCombo(Spell.Distil) && target is MonsterObject { Info.IsBoss: false }
        ? ScaleSignature(duration, 100 + Talent(2, 1) * 5) : duration;

    private void TalentSpendModifiers(Spell spender, MapObject target, Spell action, byte stacks, int variant)
    {
        switch (spender)
        {
            case Spell.Notchbreaker:
                if (Talent(2, 1) > 0 && Envir.Time >= _lastTalentRefund)
                {
                    ChangeMP(Talent(2, 1));
                    _lastTalentRefund = Envir.Time + 6000;
                }
                if (action is Spell.None or Spell.Notchbreaker)
                {
                    if (Talent(3, 1, 1) > 0) ShortenTalentCooldown(Spell.Notchbreaker, 850);
                    if (Talent(8, 2) > 0 && Envir.Time - _lastTalentMove <= 3000 && target != null &&
                        Functions.MaxDistance(CurrentLocation, target.CurrentLocation) == 2) ShortenTalentCooldown(Spell.ShoulderDash, 2200);
                }
                break;
            case Spell.RedWake:
                if (stacks == 2 && Talent(6, 1) > 0) _nextCrescentUntil = Envir.Time + 4000;
                if (Talent(5, 1, 1) > 0) ApplyTalentStatus(this, BuffType.TalentRedRhythm, 4000, value: 2);
                break;
            case Spell.GlacialGate:
                if (Talent(2, 2) > 0 && HasBuff(BuffType.MagicShield)) ChangeMP(Talent(2, 2));
                if (Talent(6, 0) > 0 && action is Spell.FractureSpear or Spell.IceThrust)
                {
                    _nextIceStormTarget = target;
                    _nextIceStormUntil = Envir.Time + 5000;
                }
                if (Talent(6, 2) > 0 && action == Spell.GlacialGate) ApplyTalentStatus(this, BuffType.TalentStandard, 3000, value: 18);
                if (Talent(5, 1, 1) > 0 && action == Spell.GlacialGate) ShortenTalentCooldown(Spell.GlacialGate, 1300);
                break;
            case Spell.StormRelay:
                if (Talent(6, 2) > 0) GrantSignatureWard(this, Spell.StormRelay, Math.Max(1, Stats[Stat.MaxMC] / 4), 2500);
                if (stacks == 2 && Talent(8, 2) > 0)
                    StartTalentField(Spell.StormRelay, BuffType.TalentGroundedCircuit, CurrentLocation, 2, 3000, 9);
                if (Talent(5, 1, 1) > 0) ApplyTalentStatus(this, BuffType.TalentChargedHands, 4000, value: 3);
                break;
            case Spell.LanternLink:
                if (Talent(6, 1) > 0 && target?.FindControlToBreak(TalentControl.Slow) is { } slow)
                    slow.TickTime = Math.Min(slow.TickTime, Envir.Time + 500);
                break;
            case Spell.Distil:
                if (Talent(6, 0) > 0) ShortenTalentCooldown(Spell.SerpentNeedle, 900);
                if (variant == TalentRotations.GreenDose)
                {
                    if (Talent(2, 2) > 0) ChangeMP(Talent(2, 2));
                    if (Talent(6, 2) > 0) { _lastPoisonTarget = target; _nextPoisonUntil = Envir.Time + 5000; }
                }
                break;
        }
    }

    internal void TalentLegacyAreaHit(Spell spell, MapObject target, int hits)
    {
        if (hits == 0) return;
        ObserveRotation(RotationEvent.Hit, spell, target, hits);
        if (spell == Spell.Lightning && OwnsCombo(Spell.StormRelay) && Talent(2, 1) > 0) ChangeMP(Talent(2, 1));
        if (spell == Spell.IceStorm && hits >= 2 && OwnsCombo(Spell.GlacialGate) && Talent(2, 1) > 0 && GetMagic(spell) is { } ice)
            ShortenTalentCooldown(spell, ice.GetDelay() * Talent(2, 1) / 20);
    }

    internal int TalentIceStormPower(MapObject target, int power) => target == _nextIceStormTarget && Envir.Time < _nextIceStormUntil
        ? ScaleSignature(power, 115) : power;

    private int TalentMarkedPower(Spell spell, MapObject target, int power, out BuffType used)
    {
        used = BuffType.None;
        if (target == null) return power;
        BuffType[] candidates = spell switch
        {
            Spell.CinderLance or Spell.GreatFireBall or Spell.FlameDisruptor or Spell.AshSeal => [BuffType.TalentHeatFracture],
            Spell.FractureSpear or Spell.FrostCrunch or Spell.GlacialGate or Spell.WinterglassVolley => [BuffType.TalentBrittle],
            Spell.GaleCleaver => [BuffType.TalentExposedFlank, BuffType.TalentDented],
            Spell.SpiritSpark or Spell.SoulFireBall or Spell.AncestorsRend => [BuffType.TalentThreadbreak],
            Spell.ArcJavelin or Spell.ThunderBolt or Spell.StormRelay or Spell.Lightning or Spell.Skywire => [BuffType.TalentFusedArmour],
            Spell.SteelCut or Spell.Oathbrand or Spell.Notchbreaker or Spell.ArcaneHammer => [BuffType.TalentDented],
            _ => []
        };
        foreach (BuffType candidate in candidates)
        {
            if (!target.HasBuff(candidate, out Buff buff) || buff.FlagForRemoval || buff.Caster != this || buff.Values.Length == 0) continue;
            used = candidate;
            return ScaleSignature(power, 100 + Math.Max(0, buff.Values[0]));
        }
        return power;
    }

    private int TalentSpellDamage(MapObject target, Spell spell, int power, DefenceType defence)
    {
        if (spell is Spell.SteelCut or Spell.Oathbrand && Class == MirClass.Warrior)
        {
            if (Envir.Time < _drivingFootUntil)
            {
                _drivingFootUntil = 0;
                power = ScaleSignature(power, 115);
            }
            if (ClassTalent(1, 0) > 0 && _lastTalentMove != _lastReadyStanceMove && Envir.Time - _lastTalentMove < 2400)
            {
                _lastReadyStanceMove = _lastTalentMove;
                defence = DefenceType.AC;
            }
        }
        int dealt = target.Attacked(this, TalentMarkedPower(spell, target, power, out BuffType spent), defence, false);
        if (dealt > 0 && spent != BuffType.None) target.RemoveBuff(spent);
        return dealt;
    }

    private void ApplyTalentGuardMitigation(MapObject attacker, int damage, int armour)
    {
        if (armour <= 0 || damage <= 0 || !OwnsCombo(Spell.Interpose) || Talent(6, 1) == 0 ||
            Envir.Time < _lastTalentGuard || damage < Math.Max(12, MaxHealth / 8)) return;
        if (!HasBuff(BuffType.ProtectionField, out Buff guard) && !HasBuff(BuffType.ImmortalSkin, out guard)) return;
        if (guard.Get<bool>("ExtendedByGuard")) return;
        _lastTalentGuard = Envir.Time + 2000;
        guard.ExpireTime += 2000;
        guard.Set("ExtendedByGuard", true);
    }

    private void ResetTalentModifiers()
    {
        _nextCrescentUntil = _nextIceStormUntil = _nextRecallUntil = _nextPoisonUntil = _lastBoltHit = 0;
        _lastBoltTarget = _nextIceStormTarget = _lastPoisonTarget = _lastHealedTarget = null;
        _manaSift = _openingBolt = false;
        _poisonSenseTarget = null;
        _battleDrillUntil = _drivingFootUntil = _backdraftUntil = _mirrorWardUntil = 0;
    }
}
