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

Console.WriteLine("Specialisation and combo protocol round-trip tests passed.");
