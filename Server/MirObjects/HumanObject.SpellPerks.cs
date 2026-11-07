using System.Drawing;
using Server.MirDatabase;
using C = ClientPackets;
using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    internal S.SpellPerkState SpellPerkState(string message = "") => new()
    {
        ObjectID = ObjectID,
        Revision = Info.SpellPerks.Revision,
        Picks = Info.SpellPerks.Picks,
        Message = message
    };

    protected void SendSpellPerkState()
    {
        if (!SpellPerks.Supports(Class)) return;
        if (this is HeroObject hero) hero.Owner?.Enqueue(SpellPerkState());
        else Enqueue(SpellPerkState());
    }

    internal bool KnowsPerkSpell(Spell spell) => Info.Magics.Any(magic => magic.Spell == spell && !magic.IsTempSpell && CanUseLegacyMagic(magic));

    internal bool CanLearnLegacyMagic(UserMagic magic)
    {
        if (!CanUseLegacyMagic(magic)) return false;
        magic.Perks = !magic.IsTempSpell && SpellPerks.BelongsTo(Class, magic.Spell) ? Info.SpellPerks : null;
        return true;
    }

    private void ReconcileSpellPerks(bool notifyClient)
    {
        // The v121 record may contain a key for a talent replacement. Recover the corresponding legacy spell.
        for (int i = Info.Magics.Count - 1; i >= 0; i--)
        {
            UserMagic magic = Info.Magics[i];
            if (!SpellCurriculum.IsRetired(magic.Spell)) continue;
            Spell legacy = magic.Spell switch
            {
                Spell.DrakesEcho => Spell.TwinDrakeBlade,
                Spell.CausticScript => Spell.SoulFireBall,
                _ => Spell.None
            };
            var replacement = Info.Magics.Find(candidate => candidate.Spell == legacy && candidate.Key == 0 && CanUseLegacyMagic(candidate));
            Info.Magics.RemoveAt(i);
            if (notifyClient) Enqueue(new S.RemoveMagic { PlaceId = i });
            if (replacement == null || !IsTalentSlot(magic.Key) || Info.Magics.Any(candidate => candidate.Key == magic.Key)) continue;
            replacement.Key = magic.Key;
            if (notifyClient) SendMagicInfo(replacement);
        }
        Info.Specialisation = CombatSpecialisation.Unchosen;
        Info.Talents = new TalentProfile();
        foreach (UserMagic magic in Info.Magics)
            magic.Perks = !magic.IsTempSpell && SpellPerks.BelongsTo(Class, magic.Spell) ? Info.SpellPerks : null;
        var picks = Info.SpellPerks.Picks;
        if (picks.Length > 0 && !SpellPerks.Validate(Class, Level, picks, KnowsPerkSpell, out _))
        {
            var retained = picks.Where(pick => KnowsPerkSpell(pick.Spell)).ToArray();
            Info.SpellPerks.Apply(SpellPerks.Validate(Class, Level, retained, KnowsPerkSpell, out _) ? retained : []);
        }
    }

    internal void ApplySpellPerkPicks(SpellPerkPick[] picks)
    {
        Info.SpellPerks.Apply(picks);
        RefreshStats();
        foreach (UserMagic magic in Info.Magics.Where(CanUseLegacyMagic)) SendMagicInfo(magic);
    }

    private readonly record struct PerkDamage(MapObject Target, Spell Spell, int Damage, DefenceType Defence, int Ticks, bool IsBurn = false);

    internal void ApplyLandedSpellPerks(UserMagic magic, MapObject target, int damage, DefenceType defence, bool secondaryBlade = false)
    {
        if (magic?.Perks == null || damage <= 0 || CurrentMap == null || target.CurrentMap != CurrentMap) return;
        if (magic.Perk(SpellPerk.Interrupt) > 0 && target is MonsterObject { Info.IsBoss: false, Master: null } && !target.Dead)
            SlowSignatureTarget(target, 1 + magic.Perk(SpellPerk.Interrupt));

        int splash = magic.Perk(SpellPerk.Splash), chain = magic.Perk(SpellPerk.Chain), cleave = magic.Perk(SpellPerk.SlayingCleave);
        if (splash + chain + cleave > 0)
        {
            int cap = splash > 0 ? 1 + splash : Math.Max(chain, cleave);
            int percent = 18 + 10 * Math.Max(splash, Math.Max(chain, cleave));
            foreach (MapObject enemy in GetSignatureTargets(target.CurrentLocation, 1 + (chain > 0 ? 1 : 0), 8, [target])
                .OfType<MonsterObject>().Where(enemy => enemy.Master == null && !enemy.Info.IsBoss && CanFly(enemy.CurrentLocation)).Take(cap))
            {
                int extra = Math.Max(1, ScaleSignature(damage, percent));
                if (enemy.Attacked(this, extra, defence, false) <= 0) continue;
                if (chain > 0)
                    CurrentMap.Broadcast(new S.ObjectProjectile { Spell = magic.Spell, Source = target.ObjectID, Destination = enemy.ObjectID }, enemy.CurrentLocation);
                else
                    BroadcastLegacyImpact(magic.Spell, enemy.CurrentLocation);
                int burn = magic.Perk(SpellPerk.Burn);
                if (burn > 0 && !enemy.Dead)
                {
                    BroadcastBurn(magic.Spell, enemy.CurrentLocation);
                    QueuePerkDamage(new PerkDamage(enemy, magic.Spell, Math.Max(1, ScaleSignature(damage, 8 + 3 * burn)), DefenceType.MAC, 1 + burn, true), 850);
                }
            }
        }

        int echo = magic.Perk(SpellPerk.DrakeEcho);
        if (secondaryBlade && echo > 0 && !target.Dead)
            QueuePerkDamage(new PerkDamage(target, magic.Spell, Math.Max(1, ScaleSignature(damage, 25 + 10 * echo)), defence, 1), 260);
    }

    private void QueuePerkDamage(PerkDamage impact, int delay) =>
        ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + delay, GetMagic(impact.Spell), impact));

    private void ResolvePerkDamage(PerkDamage impact)
    {
        if (Dead || !CanHitSignatureTarget(impact.Target) || !Functions.InRange(CurrentLocation, impact.Target.CurrentLocation, 10)) return;
        if (impact.Target.Attacked(this, impact.Damage, impact.Defence, false) > 0)
        {
            if (impact.IsBurn) BroadcastBurn(impact.Spell, impact.Target.CurrentLocation);
            else BroadcastLegacyImpact(impact.Spell, impact.Target.CurrentLocation);
        }
        if (impact.Ticks > 1 && !impact.Target.Dead) QueuePerkDamage(impact with { Ticks = impact.Ticks - 1 }, 850);
    }

    internal void BroadcastLegacyImpact(Spell spell, Point location) => CurrentMap.Broadcast(new S.MapEffect
        { Location = location, Effect = SpellEffect.WeaponSpellImpact, Value = (byte)spell }, location);

    private void BroadcastBurn(Spell spell, Point location) => CurrentMap.Broadcast(new S.MapEffect
        { Location = location, Effect = SpellEffect.SpellPerkBurn, Value = (byte)spell }, location);

    internal void ApplyLegacyShelter(UserMagic magic, MapObject target)
    {
        int rank = magic.Perk(SpellPerk.Shelter);
        if (rank == 0 || target is not HumanObject ally || !CanShieldSignatureTarget(ally)) return;
        GrantSignatureWard(ally, magic.Spell, Math.Max(1, ally.MaxHealth * (3 + 2 * rank) / 100), 3500);
    }

    internal void ApplyCompanionPerk(UserMagic magic, MonsterObject pet)
    {
        int rank = magic.Perk(SpellPerk.Companion);
        if (rank == 0) return;
        pet.AddBuff(BuffType.TalentPackFocus, this, int.MaxValue, new Stats
        {
            [Stat.HP] = Math.Max(1, pet.MaxHealth * rank / 10),
            [Stat.MaxDC] = Math.Max(1, pet.Stats[Stat.MaxDC] * rank / 12)
        }, values: [0, _signatureGeneration]);
        pet.SetHP(pet.MaxHealth);
    }
}

public partial class PlayerObject
{
    internal void ApplySpellPerkDraft(C.SpellPerkDraft request)
    {
        HumanObject actor = request.ObjectID == ObjectID ? this : HeroSpawned && Hero?.ObjectID == request.ObjectID ? Hero : null;
        if (actor == null || !SpellPerks.Supports(actor.Class)) return;
        string message = "";
        if (request.Apply)
        {
            if (request.Revision != actor.Info.SpellPerks.Revision) message = "Your spell perks changed. Review the current picks.";
            else if (Dead || actor.Dead || Envir.Time < LogTime || Envir.Time < actor.LogTime)
                message = "Leave combat before applying spell perks.";
            else if (SpellPerks.Validate(actor.Class, actor.Level, request.Picks, actor.KnowsPerkSpell, out message))
            {
                actor.ApplySpellPerkPicks(request.Picks);
                message = "Spell perks applied.";
            }
        }
        Enqueue(actor.SpellPerkState(message));
    }
}
