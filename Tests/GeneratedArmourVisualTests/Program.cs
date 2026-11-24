using System;

static void Check(bool ok, string description)
{
    if (!ok) throw new Exception(description);
}

Check(GeneratedArmourVisual.TryParseId("g01", out byte first) && first == 1, "g01 must be valid.");
Check(GeneratedArmourVisual.TryParseId("G99", out byte last) && last == 99, "G99 must be valid.");
foreach (string invalid in new[] {"g00", "g1", "g100", "g-1", "gXX", "01", "off", ""})
    Check(!GeneratedArmourVisual.TryParseId(invalid, out _), $"Unexpected ID accepted: {invalid}");
Check(!GeneratedArmourVisual.TryParseId(null, out _), "Null IDs must be rejected.");

for (byte id = 1; id <= 99; id++)
{
    short transmitted = GeneratedArmourVisual.Encode(12, id);
    Check(transmitted == -id, $"Incorrect encoded look: {id}");
    GeneratedArmourVisual.Decode(transmitted, out short baseArmour, out byte decoded);
    Check(baseArmour == 0 && decoded == id, $"Generated armour {id} must draw on 00.");
}

foreach (short original in new short[] {0, 1, 12, 32767})
{
    short transmitted = GeneratedArmourVisual.Encode(original, 0);
    GeneratedArmourVisual.Decode(transmitted, out short baseArmour, out byte decoded);
    Check(original == transmitted && baseArmour == original && decoded == 0,
        "Legacy armour look must be preserved without an overlay.");
}

GeneratedArmourVisual.Decode(-100, out short fallback, out byte none);
Check(fallback == 0 && none == 0, "Invalid negative armour must not index out of bounds.");
bool threw = false;
try { GeneratedArmourVisual.Encode(4, 100); }
catch (ArgumentOutOfRangeException) { threw = true; }
Check(threw, "Out-of-range IDs must not be encoded.");
Console.WriteLine("Generated armour ID parsing and wire compatibility checks passed.");

var command = new ServerPackets.GeneratedArmourAction { Id = 1, Build = true };
bool wasServer = Packet.IsServer;
try
{
    Packet.IsServer = false;
    var decoded = Packet.ReceivePacket(command.GetPacketBytes().ToArray(), out byte[] extra);
    Check(decoded is ServerPackets.GeneratedArmourAction action && action.Id == 1 && action.Build && extra.Length == 0,
        "Generated armour export instruction must round-trip.");
}
finally { Packet.IsServer = wasServer; }

for (int id = 10; id < 20; id++)
{
    Check(ArmourWings.TrySelect(id.ToString(), out byte? effect) && effect == id + 108 &&
        ArmourWings.IsParticle(effect.Value), $"New wing {id} must resolve on the server.");
    try
    {
        Packet.IsServer = false;
        var update = new ServerPackets.PlayerUpdate { ObjectID = 42, WingEffect = effect.Value };
        var decoded = Packet.ReceivePacket(update.GetPacketBytes().ToArray(), out byte[] extra);
        Check(decoded is ServerPackets.PlayerUpdate wing && wing.WingEffect == effect.Value && extra.Length == 0,
            $"Wing {id} must survive the actual appearance packet.");
    }
    finally { Packet.IsServer = wasServer; }
}
