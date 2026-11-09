using System.Text.RegularExpressions;

namespace Client.MirScenes;

public enum SpellVfxSource { Curriculum, Specialisation, Talent }

public sealed record SpellVfxEntry(Spell Spell, MirClass Class, string Name, SpellVfxSource Source, bool HasStandaloneCast);

/// <summary>Game-owned spell catalogue used by the particle editor.</summary>
public static partial class SpellVfxCatalogue
{
    private static readonly MirClass[] Classes = [MirClass.Warrior, MirClass.Wizard, MirClass.Taoist];

    public static IReadOnlyList<SpellVfxEntry> All { get; } = Array.AsReadOnly(Classes.SelectMany(ForClass).ToArray());

    private static IEnumerable<SpellVfxEntry> ForClass(MirClass @class)
    {
        foreach (Spell spell in SpellCurriculum.ForClass(@class))
        {
            bool standalone = spell is not (Spell.Fencing or Spell.Slaying or Spell.SpiritSword or
                Spell.Thrusting or Spell.HalfMoon or Spell.CrossHalfMoon);
            yield return new SpellVfxEntry(spell, @class, WordBoundary().Replace(spell.ToString(), " "),
                SpellVfxSource.Curriculum, standalone);
        }

        foreach (var ability in SpecialisationAbilities.All.Where(ability => ability.Class == @class))
            yield return new SpellVfxEntry(ability.Spell, @class, ability.Name, SpellVfxSource.Specialisation, true);

        foreach (var talent in TalentAbilities.All.Where(talent => talent.Spell.Class == @class))
            yield return new SpellVfxEntry(talent.Spell.Spell, @class, talent.Spell.Name, SpellVfxSource.Talent, true);
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}
