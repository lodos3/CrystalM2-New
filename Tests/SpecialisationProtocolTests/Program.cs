using System;
using System.Linq;

static void Check(bool ok, string description)
{
    if (!ok) throw new Exception(description);
}

static T RoundTrip<T>(Packet sent, bool receivingOnServer) where T : Packet
{
    Packet.IsServer = receivingOnServer;
    byte[] wire = sent.GetPacketBytes().ToArray();
    Packet? read = Packet.ReceivePacket(wire, out byte[] extra);
    Check(extra.Length == 0, "Entire packet consumed");
    Check(read is T, $"Packet type roundtrip: {typeof(T).Name}");
    return (T)read!;
}

Check((short)ClientPacketIds.SelectSpecialisation >
    (short)ClientPacketIds.RemoveStoragePassword, "Append client ID without renumbering");
Check((short)ServerPacketIds.SpecialisationResult >
    (short)ServerPacketIds.StoragePasswordResult, "Append server ID without renumbering");

SpellPerkPick[] spellPicks = [new(Spell.FlamingSword, SpellPerk.Splash, 1), new(Spell.FlamingSword, SpellPerk.Burn, 2)];
var perkDraft = RoundTrip<ClientPackets.SpellPerkDraft>(new ClientPackets.SpellPerkDraft
    { ObjectID = 123, Revision = 7, Apply = true, Picks = spellPicks }, true);
Check(perkDraft.ObjectID == 123 && perkDraft.Revision == 7 && perkDraft.Apply &&
    perkDraft.Picks.SequenceEqual(spellPicks), "Perk draft roundtrip");
var perkState = RoundTrip<ServerPackets.SpellPerkState>(new ServerPackets.SpellPerkState
    { ObjectID = 123, Revision = 8, Picks = spellPicks, Message = "Spell perks applied." }, false);
Check(perkState.ObjectID == 123 && perkState.Revision == 8 && perkState.Picks.SequenceEqual(spellPicks) &&
    perkState.Message == "Spell perks applied.", "Authoritative perk state roundtrip");
var temporary = RoundTrip<ServerPackets.NewMagic>(new ServerPackets.NewMagic
    { Magic = new ClientMagic { Name = "Blink", Spell = Spell.Blink, IsTempSpell = true, Key = 17 }, Hero = true }, false);
Check(temporary.Magic.IsTempSpell && temporary.Magic.Key == 17 && temporary.Hero, "Temporary spell flag preserves hero binding");

var request = RoundTrip<ClientPackets.SelectSpecialisation>(
    new ClientPackets.SelectSpecialisation { ForHero = true, Specialisation = CombatSpecialisation.Third }, true);
Check(request.ForHero && request.Specialisation == CombatSpecialisation.Third,
    "Hero selection request payload");

var result = RoundTrip<ServerPackets.SpecialisationResult>(
    new ServerPackets.SpecialisationResult { ForHero = false, Accepted = true,
        Specialisation = CombatSpecialisation.Second }, false);
Check(!result.ForHero && result.Accepted && result.Specialisation == CombatSpecialisation.Second,
    "Authoritative selection acknowledgement");

var character = RoundTrip<ServerPackets.UserInformation>(
    new ServerPackets.UserInformation { Name = "Test Player",
        Class = MirClass.Warrior, Specialisation = CombatSpecialisation.First }, false);
Check(character.Specialisation == CombatSpecialisation.First, "Player login state");

var hero = RoundTrip<ServerPackets.HeroInformation>(
    new ServerPackets.HeroInformation { Name = "Test Hero",
        Class = MirClass.Taoist, Specialisation = CombatSpecialisation.Third }, false);
Check(hero.Specialisation == CombatSpecialisation.Third, "Hero login state (distinct wire layout)");

foreach (SpellEffect phase in new[] { SpellEffect.SignatureWindup, SpellEffect.SignatureImpact, SpellEffect.SignatureArmed })
{
    var sent = new ServerPackets.MapEffect
    {
        Effect = phase, Location = new System.Drawing.Point(10, 20), Value = (byte)Spell.CinderMine
    };
    var received = RoundTrip<ServerPackets.MapEffect>(sent, false);
    Check(received.Effect == phase && received.Location == sent.Location && received.Value == (byte)Spell.CinderMine,
        "Timed signature stages use the existing map-effect packet shape");
}

var combo = RoundTrip<ServerPackets.SpellComboState>(new ServerPackets.SpellComboState
{
    ObjectID = 123, TargetID = 456, Spender = Spell.GlacialGate, Stacks = 1, Remaining = 6500
}, false);
Check(combo.ObjectID == 123 && combo.TargetID == 456 && combo.Spender == Spell.GlacialGate &&
    combo.Stacks == 1 && combo.Remaining == 6500, "Combo proc updates preserve actor, target and expiry");

var profile = new TalentProfile();
TalentPick picked = new(TalentCatalog.Tree(MirClass.Warrior).At(1, 0).Id, 1, 0);
profile.Apply(CombatSpecialisation.First, [picked], []);
profile.SaveBindings(CombatSpecialisation.First, [new(Spell.ArcaneHammer, 2)]);
var draft = RoundTrip<ClientPackets.TalentDraft>(new ClientPackets.TalentDraft
{
    ObjectID = 123, Revision = profile.Revision, Spec = CombatSpecialisation.First, ClassPicks = [picked]
}, true);
Check(draft.ObjectID == 123 && draft.Revision == 1 && draft.ClassPicks.Single() == picked, "Draft preserves identity and picks");
var talents = RoundTrip<ServerPackets.TalentState>(new ServerPackets.TalentState
{
    ObjectID = 123, Accepted = true, Spec = CombatSpecialisation.First, Profile = profile, ActiveBindings = [new(Spell.ArcaneHammer, 2)]
}, false);
Check(talents.Profile.Picks(CombatSpecialisation.Unchosen).Single() == picked && talents.ActiveBindings.Single().Slot == 2 &&
    talents.Profile.HasBindings(CombatSpecialisation.First), "Talent state includes picks and action slots");
var proc = RoundTrip<ServerPackets.RotationState>(new ServerPackets.RotationState
{
    ObjectID = 123, TargetID = 456, Finisher = Spell.Forgefall, Step = 2, Ready = true, Remaining = 6000
}, false);
Check(proc.Ready && proc.Step == 2 && proc.TargetID == 456 && proc.Finisher == Spell.Forgefall && proc.Remaining == 6000,
    "Proc window preserves stage and target");

Console.WriteLine("Specialisation and combo protocol round-trip tests passed.");
