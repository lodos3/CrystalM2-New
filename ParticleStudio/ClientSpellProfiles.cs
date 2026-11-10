using System.IO;
using Client.MirScenes;

namespace CrystalM2.ParticleStudio;

internal static class ClientSpellProfiles
{
    internal static string Apply(string sourceDirectory, string clientDirectory, Spell spell)
    {
        string destination = Path.Combine(clientDirectory, "Data", "SpellVfx");
        if (!File.Exists(Path.Combine(clientDirectory, "Client.exe")) ||
            !SpellVfxAssets.Groups.All(group => File.Exists(Path.Combine(destination, group + ".json"))))
            throw new InvalidDataException("Choose a client build that already supports editable spell profiles.");

        var saved = SpellVfxAssets.ReadSpell(sourceDirectory, spell);
        SpellVfxAssets.SaveSpell(destination, spell, saved);
        return destination;
    }
}
