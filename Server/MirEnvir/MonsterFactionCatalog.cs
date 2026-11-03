using System;
using System.Collections.Generic;
using System.IO;

namespace Server.MirEnvir
{
    // Optional configuration, loaded once. Absence of the file preserves legacy AI behaviour.
    internal static class MonsterFactionCatalog
    {
        private static readonly Lazy<Dictionary<string, int>> Factions =
            new Lazy<Dictionary<string, int>>(Load);

        private static Dictionary<string, int> Load()
        {
            string path = Path.Combine(Settings.EnvirPath, "MonsterFactionList.txt");
            return File.Exists(path)
                ? MonsterFactionRules.Parse(File.ReadLines(path))
                : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        internal static int GetFaction(string monsterName)
        {
            return monsterName != null && Factions.Value.TryGetValue(monsterName, out int faction)
                ? faction
                : 0;
        }
    }
}
