using Server.MirEnvir;

var table = MonsterFactionRules.Parse(new[]
{
    "; HeroesM2-style monster faction rows",
    "",
    "Deer 1",
    "Wolf 2",
    "wolf 3",
    "Guard 3"
});

if (table.Count != 3 || table["DEER"] != 1 || table["wolf"] != 3)
    throw new Exception("Monster faction parsing or case-insensitive names failed.");
if (!MonsterFactionRules.AreFriendly(3, 3) ||
    MonsterFactionRules.AreFriendly(0, 0) ||
    MonsterFactionRules.AreFriendly(1, 2))
    throw new Exception("Monster faction friendship is wrong.");
if (!MonsterFactionRules.AreHostile(1, 2) ||
    MonsterFactionRules.AreHostile(1, 1) ||
    MonsterFactionRules.AreHostile(0, 2))
    throw new Exception("Unassigned mobs must keep the legacy targeting behaviour.");
foreach (string invalid in new[] { "Wolf", "Wolf bad", "Wolf 0", "Wolf -3", "Wolf 2 garbage" })
{
    try
    {
        MonsterFactionRules.Parse(new[] { invalid });
        throw new Exception($"Invalid row was accepted: {invalid}");
    }
    catch (FormatException) { }
}
Console.WriteLine("Monster faction parsing and relationship tests passed.");
