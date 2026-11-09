using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;

namespace Server.MirObjects;

public partial class HumanObject
{
    private sealed record TalentField(Spell Spell, BuffType Status, Map Map, int Generation, Point Anchor, int Radius,
        long Until, int Power, MapObject Linked, HashSet<uint> Affected)
    {
        public Point Centre => Spell is Spell.RedWake or Spell.SpiritCommand ? Linked.CurrentLocation : Anchor;
    }

    private Dictionary<Spell, TalentField> _talentFields;

    private void StartTalentField(Spell spell, BuffType status, Point anchor, int radius, int duration, int power, MapObject linked = null)
    {
        if (GetMagic(spell) is not { } magic || CurrentMap == null) return;
        var field = new TalentField(spell, status, CurrentMap, _signatureGeneration, anchor, radius,
            Envir.Time + duration, power, linked, []);
        (_talentFields ??= [])[spell] = field;
        PulseTalentField(magic, field);
    }

    private bool ValidTalentField(TalentField field) => !Dead && CurrentMap == field.Map &&
        field.Generation == _signatureGeneration && Envir.Time < field.Until &&
        _talentFields?.GetValueOrDefault(field.Spell) == field &&
        (field.Linked == null || (!field.Linked.Dead && field.Linked.CurrentMap == field.Map));

    private void PulseTalentField(UserMagic magic, TalentField field)
    {
        if (!ValidTalentField(field)) return;
        Point centre = field.Centre;
        if (field.Spell is Spell.GlacialGate or Spell.RedWake)
        {
            foreach (MapObject enemy in GetSignatureTargets(centre, field.Radius, 5))
            {
                if (field.Affected.Count >= 5 || (field.Spell == Spell.RedWake && enemy is not MonsterObject { Master: null }) ||
                    !field.Affected.Add(enemy.ObjectID)) continue;
                if (field.Spell == Spell.GlacialGate) SlowSignatureTarget(enemy, 2);
                else HitSignatureTarget(enemy, field.Spell, field.Power, DefenceType.AC);
            }
        }
        else
        {
            foreach (HumanObject ally in TalentAllies(centre, field.Radius))
            {
                if (ally.GetTalentModifier(field.Status) is { } existing && existing.Caster != this &&
                    existing.Caster is HumanObject other && existing.Get<TalentField>("Field") is { } prior &&
                    other.ValidTalentField(prior) && Functions.InRange(ally.CurrentLocation, prior.Centre, prior.Radius) &&
                    (prior.Power > field.Power || (prior.Power == field.Power && other.ObjectID < ObjectID))) continue;
                ApplyTalentStatus(ally, field.Status, 800, value: field.Power)?.Set("Field", field);
            }
        }
        BroadcastSignatureEffect(field.Spell, centre, SpellEffect.SignatureArmed);
        if (Envir.Time + 600 < field.Until)
            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 600, magic, field));
    }

    private TalentField GetCoverField(BuffType status)
    {
        var buff = GetTalentModifier(status);
        if (buff?.Caster is not HumanObject source || buff.Get<TalentField>("Field") is not { } field ||
            !source.ValidTalentField(field) || !Functions.InRange(CurrentLocation, field.Centre, field.Radius)) return null;
        return field;
    }

    internal int TalentFieldProtection(MapObject attacker, DefenceType defence, int attackPenalty)
    {
        int percent = 0;
        bool magical = defence is DefenceType.MAC or DefenceType.MACAgility;
        if (GetCoverField(BuffType.TalentDuelCircle) is { } duel && attacker == duel.Linked) percent = duel.Power;
        if (magical && GetCoverField(BuffType.TalentGroundedCircuit) is { } ground) percent = Math.Max(percent, ground.Power);
        if (GetCoverField(BuffType.TalentAncestralForm) is { Linked: MonsterObject spirit } form &&
            (magical ? spirit.Info.Name == Settings.AngelName : spirit.Info.Name == Settings.ShinsuName)) percent = Math.Max(percent, form.Power);
        if (!magical && attacker is MonsterObject { Master: null, Info.IsBoss: false } &&
            GetCoverField(BuffType.TalentHoldLine) is { } hold && hold.Power > Math.Max(percent, attackPenalty) &&
            hold.Affected.Count < 8 && hold.Affected.Add(attacker.ObjectID)) percent = hold.Power;
        return percent;
    }

    private int AncestralSlowResistance() => GetCoverField(BuffType.TalentAncestralForm) is { Linked: MonsterObject spirit } &&
        spirit.Info.Name == Settings.SkeletonName ? 15 : 0;
}
