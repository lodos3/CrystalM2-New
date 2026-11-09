using Server.MirDatabase;
using C = ClientPackets;
using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    public TalentLoadout Build => Info.Talents.Active(Info.Class, Info.Specialisation);

    public bool CanUseBuildSpell(Spell spell) => Info.Magics.Any(magic => magic.Spell == spell && CanUseLegacyMagic(magic));

    private bool CanUseLegacyMagic(UserMagic magic) => magic.Info != null &&
        SpellCurriculum.CanUseLegacy(Info.Class, magic.Spell, Level, magic.Info.Level1, magic.IsTempSpell);

    private void ReconcileTalentSpells(bool notifyClient)
    {
        TalentLoadout build = Build;
        foreach (Spell spell in TalentCatalog.NativeSpells(Info.Class))
        {
            if (!build.CanUse(Level, spell)) continue;
            UserMagic existing = Info.Magics.Find(magic => magic.Spell == spell);
            if (existing != null)
            {
                existing.IsTempSpell = false;
                continue;
            }
            var learned = new UserMagic(spell);
            if (learned.Info == null) continue;
            Info.Magics.Add(learned);
            if (notifyClient) SendMagicInfo(learned);
        }
        foreach (UserMagic finisher in Info.Magics)
        {
            if (!IsTalentSlot(finisher.Key)) continue;
            Spell action = TalentRotations.ActionForFinisher(finisher.Spell, build.Engine);
            if (action == Spell.None) continue;
            Spell family = TalentCatalog.Family(action);
            UserMagic baseSpell = Info.Magics.Find(magic => TalentCatalog.Family(magic.Spell) == family && build.CanUse(Level, magic.Spell));
            if (baseSpell == null) continue;
            if (baseSpell.Key == 0) baseSpell.Key = finisher.Key;
            finisher.Key = 0;
        }
    }

    internal S.TalentState BuildTalentState(bool accepted = true, string message = "") => new()
    {
        ObjectID = ObjectID,
        Spec = Info.Specialisation,
        Profile = Info.Talents.Snapshot(),
        ActiveBindings = CurrentTalentBindings(),
        Accepted = accepted,
        Message = message
    };

    private TalentBinding[] CurrentTalentBindings() => Info.Magics
        .Where(magic => IsTalentSlot(magic.Key) && CanUseBuildSpell(magic.Spell) && !SpellCurriculum.IsPassive(magic.Spell))
        .GroupBy(magic => magic.Key).Select(group => new TalentBinding(group.First().Spell, group.Key)).ToArray();

    private bool IsTalentSlot(byte slot) => this is HeroObject ? slot is >= 17 and <= 24 : slot is >= 1 and <= 16;

    internal void CommitTalentBuild(C.TalentDraft draft)
    {
        var profile = Info.Talents;
        var oldSpec = Info.Specialisation;
        var active = CurrentTalentBindings();
        var assignedFamilies = Info.Magics.Where(magic => CanUseBuildSpell(magic.Spell))
            .Select(magic => TalentCatalog.Family(magic.Spell)).ToHashSet();
        var archived = profile.Bindings(oldSpec).Where(binding => !assignedFamilies.Contains(TalentCatalog.Family(binding.Spell)));
        profile.SaveBindings(oldSpec, active.Concat(archived).DistinctBy(binding => TalentCatalog.Family(binding.Spell)).Take(24).ToArray());
        var slots = profile.HasBindings(draft.Spec) ? profile.Bindings(draft.Spec) : active;
        var cooldowns = Info.Magics.Where(magic => magic.Info != null && magic.CastTime != 0)
            .GroupBy(magic => TalentCatalog.Family(magic.Spell))
            .ToDictionary(group => group.Key, group => group.Max(magic => magic.CastTime + magic.GetDelay()));

        Info.Specialisation = draft.Spec;
        profile.Apply(draft.Spec, draft.ClassPicks, draft.SpecPicks);
        ResetSignatureState();
        ResetArmedTalentAttacks();
        ActionList.RemoveAll(action => action.Type == DelayedType.Magic ||
            (action.Type == DelayedType.Damage && action.Params.Length > 4 && action.Params[4] is UserMagic));
        ActiveBlizzard = ActiveReincarnation = false;
        ReconcileCurriculumSpells(true);
        ClearReplacedSpirits();

        var eligible = Info.Magics.Where(magic => magic.Info != null && CanUseBuildSpell(magic.Spell) &&
            !SpellCurriculum.IsPassive(magic.Spell)).ToList();
        foreach (UserMagic magic in Info.Magics) magic.Key = 0;
        var occupied = new HashSet<byte>();
        foreach (TalentBinding binding in slots.OrderBy(binding => TalentRotations.IsFinisher(binding.Spell)))
        {
            if (!IsTalentSlot(binding.Slot) || occupied.Contains(binding.Slot)) continue;
            Spell action = TalentRotations.ActionForFinisher(binding.Spell, Build.Engine);
            Spell family = TalentCatalog.Family(action == Spell.None ? binding.Spell : action);
            UserMagic magic = eligible.Find(m => m.Key == 0 && TalentCatalog.Family(m.Spell) == family);
            if (magic == null) continue;
            magic.Key = binding.Slot;
            occupied.Add(binding.Slot);
        }

        foreach (UserMagic magic in eligible)
        {
            if (cooldowns.TryGetValue(TalentCatalog.Family(magic.Spell), out long expiry) && expiry > Envir.Time)
            {
                magic.CastTime = expiry - magic.GetDelay();
                SendMagicInfo(magic);
            }
        }
        RefreshStats();
    }

    private void ResetArmedTalentAttacks()
    {
        Info.Thrusting = Info.HalfMoon = Info.CrossHalfMoon = Info.DoubleSlash = false;
        FlamingSword = TwinDrakeBlade = Slaying = CounterAttack = false;
        FlamingSwordTime = CounterAttackTime = 0;
    }

    private void ClearReplacedSpirits()
    {
        if (Class != MirClass.Taoist) return;
        for (int i = Pets.Count - 1; i >= 0; i--)
        {
            MonsterObject pet = Pets[i];
            Spell form = pet.Info.Name == Settings.SkeletonName ? Spell.SummonSkeleton :
                pet.Info.Name == Settings.ShinsuName ? Spell.SummonShinsu : pet.Info.Name == Settings.AngelName ? Spell.SummonHolyDeva : Spell.None;
            if (form == Spell.None || CanUseBuildSpell(form)) continue;
            Pets.RemoveAt(i);
            if (pet.Node == null) continue;
            if (!pet.Dead)
            {
                Envir.MonsterCount--;
                pet.CurrentMap.MonsterCount--;
            }
            pet.CurrentMap.RemoveObject(pet);
            pet.Despawn();
        }
    }
}

public partial class PlayerObject
{
    internal bool ApplyTalentDraft(C.TalentDraft draft)
    {
        HumanObject actor = draft.ObjectID == ObjectID ? this :
            HeroSpawned && Hero?.ObjectID == draft.ObjectID ? Hero : null;
        if (actor == null || !TalentCatalog.Supports(actor.Class)) return false;
        if (draft.Query)
        {
            Enqueue(actor.BuildTalentState());
            return false;
        }

        string reason = ValidateDraft(actor, draft);
        if (reason != null)
        {
            Enqueue(actor.BuildTalentState(false, reason));
            return false;
        }

        actor.CommitTalentBuild(draft);
        Enqueue(actor.BuildTalentState(true, "Talents applied."));
        return true;
    }

    private string ValidateDraft(HumanObject actor, C.TalentDraft draft)
    {
        TalentProfile profile = actor.Info.Talents;
        CombatSpecialisation current = actor.Info.Specialisation;
        if (draft.Revision != profile.Revision) return "This build changed. Review the current talents and try again.";
        if (!SpellCurriculum.IsValid(draft.Spec) || (draft.Spec == CombatSpecialisation.Unchosen && current != draft.Spec) ||
            (draft.Spec != CombatSpecialisation.Unchosen && actor.Level < SpellCurriculum.SpecialisationUnlockLevel))
            return "Specialisations unlock at level 20.";
        if (Dead || actor.Dead || LogTime > Envir.Time || actor.LogTime > Envir.Time ||
            actor.Pets.Any(pet => pet.Target is { Dead: false } && pet.Target.CurrentMap == CurrentMap))
            return "Leave combat before applying talents.";
        bool firstSpec = current == CombatSpecialisation.Unchosen && draft.Spec != current &&
            draft.ClassPicks.SequenceEqual(profile.Picks(CombatSpecialisation.Unchosen));
        if (!firstSpec && (!InSafeZone || !actor.InSafeZone || CurrentMap == null ||
            !CurrentMap.NPCs.Any(npc => Functions.InRange(CurrentLocation, npc.CurrentLocation, 5))))
            return "Stand near a town NPC in a safe area to apply talents.";
        if (!TalentCatalog.Validate(TalentCatalog.Tree(actor.Class), actor.Level, draft.ClassPicks, out string reason) ||
            !TalentCatalog.Validate(draft.Spec == CombatSpecialisation.Unchosen ? null : TalentCatalog.Tree(actor.Class, draft.Spec),
                actor.Level, draft.SpecPicks, out reason))
            return reason;
        return null;
    }
}
