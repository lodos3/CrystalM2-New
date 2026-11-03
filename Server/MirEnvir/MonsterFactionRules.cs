using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server.MirEnvir
{
    // Minimal, database-independent interpretation of HeroesM2's MonsterFactionList.txt.
    internal static class MonsterFactionRules
    {
        internal static Dictionary<string, int> Parse(IEnumerable<string> lines)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int number = 0;
            foreach (string raw in lines)
            {
                number++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";"))
                    continue;

                string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 || !int.TryParse(parts[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out int faction) || faction <= 0)
                    throw new FormatException("Invalid MonsterFactionList.txt line " + number +
                        ": expected 'MonsterName PositiveFactionId'.");

                // Later assignments override previous ones, matching common text-config behaviour.
                result[parts[0]] = faction;
            }

            return result;
        }

        internal static bool AreFriendly(int first, int second)
        {
            return first > 0 && first == second;
        }

        internal static bool AreHostile(int first, int second)
        {
            return first > 0 && second > 0 && first != second;
        }
    }
}
