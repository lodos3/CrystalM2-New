using System.Drawing;
using Server.MirDatabase;

namespace Server.MirObjects;

public partial class HumanObject
{
    private long _standFirmUntil, _secondWindUntil, _bracedFeetUntil, _drivingFootUntil;
    private long _reclaimedFocusUntil, _openAirUntil, _emergencyWardUntil, _groundingRuneUntil;
    private long _aetherUntil, _lastWizardSpellTime, _backdraftUntil, _mirrorWardUntil;
    private long _mercySealUntil, _cleanseCreditUntil;
    private long _incenseUntil, _poisonSenseUntil, _poisonSenseRecovery;
    private MapObject _poisonSenseTarget;
    private long _lastReadyStanceMove, _battleDrillUntil;
    private bool _manaSift, _openingBolt;

    private static bool IsPrimaryBolt(Spell spell) => spell is Spell.FireBall or Spell.GreatFireBall or Spell.FlameDisruptor or Spell.FrostCrunch or Spell.ThunderBolt;

    private void RecordClassSpellCast(Spell spell)
    {
        if (Class != MirClass.Wizard) return;
        if (IsPrimaryBolt(spell)) _openingBolt = Envir.Time - _lastWizardSpellTime >= 5000;
        else _manaSift = true;
        _lastWizardSpellTime = Envir.Time;
    }

    private void RecordWizardBolt()
    {
        if (Class != MirClass.Wizard) return;
        if (_manaSift && ClassTalent(1, 1) > 0) ChangeMP(1);
        _manaSift = false;
        if (_openingBolt && ClassTalent(3, 1, 1) > 0) ChangeMP(2);
        _openingBolt = false;
        if (MP * 4L < Stats[Stat.MP] && Envir.Time >= _aetherUntil && ClassTalent(6, 1) > 0)
        {
            _aetherUntil = Envir.Time + 30000;
            ChangeMP(4);
        }
    }

    private void OnClassRepulsion(UserMagic magic)
    {
        if (Class != MirClass.Wizard || magic.Spell != Spell.Repulsion) return;
        if (ClassTalent(1, 0) > 0) ApplyTalentStatus(this, BuffType.TalentStandard, 2500, value: 15);
        if (ClassTalent(5, 0) > 0) _backdraftUntil = Envir.Time + 3000;
        if (ClassTalent(5, 1) > 0 && Envir.Time >= _reclaimedFocusUntil)
        {
            _reclaimedFocusUntil = Envir.Time + 12000;
            ChangeMP(3);
        }
        if (ClassTalent(6, 0) > 0 && Envir.Time >= _openAirUntil)
        {
            _openAirUntil = Envir.Time + 20000;
            ShortenTalentCooldown(Spell.MagicShield, 1400);
        }
    }

    private void ActivateTalentMagicShield(UserMagic magic, int duration)
    {
        int percent = 100 + ClassTalent(2, 2) * 5;
        if (ClassTalent(3, 2, 0) > 0) percent += 20;
        if (ClassTalent(3, 2, 1) > 0) percent -= 15;
        var stats = new Stats { [Stat.DamageReductionPercent] = (magic.Level + 2) * 10 };
        if (ClassTalent(1, 2) > 0) stats[Stat.MaxMAC] = 2;
        AddBuff(BuffType.MagicShield, this, ScaleSignature(duration, percent), stats);
        if (ClassTalent(3, 2, 0) > 0) { magic.CastTime += 1500; SendMagicInfo(magic); }
        if (ClassTalent(3, 2, 1) > 0) ShortenTalentCooldown(Spell.MagicShield, 1500);
        if (ClassTalent(4, 2, 0) > 0 && Envir.Time >= _groundingRuneUntil && FindControlToBreak(TalentControl.Slow) is { } slow)
        {
            _groundingRuneUntil = Envir.Time + 20000;
            slow.Time = Math.Max(0, slow.Duration - 1);
            slow.TickTime = Math.Min(slow.TickTime, Envir.Time + 750);
        }
        if (ClassTalent(4, 2, 1) > 0) _mirrorWardUntil = Envir.Time + 5000;
    }

    private void WearTalentMagicShield(Buff shield, int damage, MapObject attacker = null)
    {
        int percent = 100;
        if (attacker != null && Functions.MaxDistance(CurrentLocation, attacker.CurrentLocation) > 1 && OwnsCombo(Spell.StormRelay))
            percent -= Talent(2, 2) * 5;
        int duration = (int)Math.Clamp(shield.ExpireTime - (long)damage * 60 * percent / 100, int.MinValue, int.MaxValue);
        AddBuff(BuffType.MagicShield, this, duration, null);
        if (duration <= 0) shield.Set("DepletedByDamage", true);
    }

    private void OnTalentMagicShieldRemoved(Buff shield)
    {
        if (Class != MirClass.Wizard) return;
        _mirrorWardUntil = 0;
        if (shield.Get<bool>("DepletedByDamage"))
        {
            if (ClassTalent(6, 2) == 0 || Envir.Time < _emergencyWardUntil) return;
            _emergencyWardUntil = Envir.Time + 45000;
            if (GrantSignatureWard(this, Spell.MagicShield, Math.Max(1, MaxHealth / 16), 2000))
                ApplyTalentStatus(this, BuffType.TalentIceglass, 2000);
        }
        else if (!shield.FlagForRemoval && ClassTalent(4, 1) > 0) ChangeMP(3);
    }

    internal int ClassIncomingProtection(MapObject attacker, DefenceType defence)
    {
        if (attacker == null) return 0;
        bool magical = defence is DefenceType.MAC or DefenceType.MACAgility;
        int distance = Functions.MaxDistance(CurrentLocation, attacker.CurrentLocation);
        if (Class == MirClass.Wizard)
        {
            if (!magical && distance == 1 && _backdraftUntil > Envir.Time) return 8;
            if (distance > 1 && _mirrorWardUntil > Envir.Time && HasBuff(BuffType.MagicShield))
            {
                _mirrorWardUntil = 0;
                return 12;
            }
        }
        if (magical || Class != MirClass.Warrior || distance != 1 || Direction != Functions.DirectionFromPoint(CurrentLocation, attacker.CurrentLocation)) return 0;
        if (ClassTalent(1, 2) > 0 && Envir.Time - _lastTalentMove >= 1200 && Envir.Time >= _bracedFeetUntil)
        {
            _bracedFeetUntil = Envir.Time + 4500;
            return 8;
        }
        return ClassTalent(5, 2) > 0 && TalentAllies(CurrentLocation, 1).Any(ally => ally != this) ? 6 : 0;
    }

    private void CheckSecondWind()
    {
        if (Class != MirClass.Warrior || Dead || PercentHealth >= 40 || Envir.Time < _secondWindUntil || ClassTalent(4, 2) == 0) return;
        _secondWindUntil = Envir.Time + 30000;
        HealAmount = (ushort)Math.Min(ushort.MaxValue, HealAmount + Math.Max(1, MaxHealth / 25));
    }

    protected int TalentPotionHealing(int amount) => Class == MirClass.Warrior && PercentHealth < 35 && ClassTalent(3, 2, 0) > 0
        ? ScaleSignature(amount, 115) : amount;

    internal int TalentRoarRadius => Class == MirClass.Warrior && ClassTalent(4, 1, 0) > 0 ? 3 : 2;
    internal void OnTalentRoarHit()
    {
        if (Class != MirClass.Warrior || ClassTalent(4, 1, 1) == 0) return;
        foreach (HumanObject ally in TalentAllies(CurrentLocation, 2)) ApplyTalentStatus(ally, BuffType.TalentStandard, 2500, value: 15);
    }

    private void OnTaoistHealing(HumanObject recipient, Spell spell)
    {
        if (Class != MirClass.Taoist || spell != Spell.Healing) return;
        if (ClassTalent(2, 1) > 0) ApplyTalentStatus(this, BuffType.TalentSteadySpirit, 5000, value: ClassTalent(2, 1));
        if (ClassTalent(6, 0) == 0 || recipient.PercentHealth >= 35 || Envir.Time < recipient._mercySealUntil) return;
        if (GrantSignatureWard(recipient, Spell.Healing, Math.Max(1, Stats[Stat.MaxSC] / 3), 3500))
        {
            recipient._mercySealUntil = Envir.Time + 12000;
            ApplyTalentStatus(recipient, BuffType.TalentQuietLantern, 3500);
        }
    }

    private void RefreshSustainingIncense()
    {
        if (Class != MirClass.Taoist || ClassTalent(4, 1, 1) == 0 || Envir.Time < LogTime || CurrentMap == null) return;
        foreach (HumanObject ally in TalentAllies(CurrentLocation, 2).Where(ally => ally != this && ally.HP < ally.MaxHealth && Envir.Time >= ally.LogTime))
        {
            if (Envir.Time < ally._incenseUntil) continue;
            ally._incenseUntil = Envir.Time + 6000;
            ally.ChangeHP(Math.Max(1, ally.MaxHealth / 100));
        }
    }

    internal bool ApplyTalentArmourRite(Spell spell, Point location, int duration)
    {
        bool wide = ClassTalent(6, 1) > 0;
        BuffType type = spell == Spell.SoulShield ? BuffType.SoulShield : BuffType.BlessedArmour;
        Stat stat = spell == Spell.SoulShield ? Stat.MaxMAC : Stat.MaxAC;
        bool applied = false;
        if (ClassTalent(5, 1) > 0) duration = ScaleSignature(duration, 120);
        foreach (HumanObject ally in TalentAllies(location, wide ? 4 : 3, wide ? 6 : 5))
        {
            int protection = ally.Level / 7 + 4;
            if (ally.HasBuff(type, out Buff old) && !old.FlagForRemoval && old.ExpireTime > 0 &&
                (old.Stats[stat] > protection || old.Stats[stat] == protection && old.ExpireTime > duration)) continue;
            ally.AddBuff(type, this, duration, new Stats { [stat] = protection });
            applied = true;
        }
        return applied;
    }

    private void PrimePoisonSense(MapObject target)
    {
        if (Class != MirClass.Taoist || ClassTalent(5, 2) == 0 || Envir.Time < _poisonSenseRecovery) return;
        _poisonSenseTarget = target;
        _poisonSenseUntil = Envir.Time + 4500;
    }

    private int TalentSignatureWindup(Spell spell, MapObject target, int windup)
    {
        if (spell != Spell.SpiritSpark || _poisonSenseTarget != target || Envir.Time >= _poisonSenseUntil) return windup;
        _poisonSenseTarget = null;
        _poisonSenseRecovery = Envir.Time + 6000;
        SpellTime = Math.Max(Envir.Time + 500, SpellTime - 200);
        return Math.Max(100, windup - 100);
    }

    private bool ShortenClassShove(MapObject pusher) => Class == MirClass.Warrior && Envir.Time >= _standFirmUntil &&
        ClassTalent(6, 2) > 0 && pusher is MonsterObject { Info.IsBoss: false, Master: null } && pusher.IsAttackTarget(this);
}
