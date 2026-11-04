using System;
using System.Linq;
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
MirClass[] classes = { MirClass.Warrior, MirClass.Wizard, MirClass.Taoist, MirClass.Assassin, MirClass.Archer };
int[] counts = { 17, 24, 25, 17, 21 };
for (int i = 0; i < classes.Length; i++)
{
    var skills = SpellCurriculum.ForClass(classes[i]);
    Check(skills.Count == counts[i], "Class catalog count");
    Check(skills.Distinct().Count() == skills.Count, "Duplicate spell");
    foreach (Spell spell in skills)
    {
        Check(SpellCurriculum.Owns(classes[i], spell), "Missing owner");
        foreach (MirClass other in classes.Where(c => c != classes[i]))
            Check(!SpellCurriculum.Owns(other, spell), "Cross-class entitlement");
    }
}
Check(SpellCurriculum.IsUnlocked(MirClass.Wizard, Spell.FireBall, 1, 7), "Starter wizard cast");
Check(SpellCurriculum.IsUnlocked(MirClass.Taoist, Spell.Healing, 1, 7), "Starter healer cast");
Check(SpellCurriculum.IsUnlocked(MirClass.Archer, Spell.StraightShot, 1, 9), "Starter archer cast");
Check(!SpellCurriculum.IsUnlocked(MirClass.Warrior, Spell.ShoulderDash, 29, 30), "Premature grant");
Check(SpellCurriculum.IsUnlocked(MirClass.Warrior, Spell.ShoulderDash, 30, 30), "Exact level gate");
Check(SpellCurriculum.IsUnlocked(MirClass.Warrior, Spell.ShoulderDash, 90, 30), "Skipping levels");
Check(!SpellCurriculum.IsUnlocked(MirClass.Warrior, Spell.FireBall, 90, 7), "Foreign spell");
Check(!SpellCurriculum.Owns(MirClass.Wizard, Spell.FastMove), "Unseeded FastMove");
Check(!SpellCurriculum.Owns(MirClass.Wizard, Spell.Blink), "Equipment-only Blink");
Check(!SpellCurriculum.Owns(MirClass.Warrior, Spell.BattleCry), "Custom BattleCry");
Check(SpellCurriculum.Name(MirClass.Taoist, CombatSpecialisation.Third) == "Spirit Binder", "Spec mapping");
Check(!SpellCurriculum.IsValid((CombatSpecialisation)255), "Invalid spec value");
Console.WriteLine("Spell curriculum tests passed.");
