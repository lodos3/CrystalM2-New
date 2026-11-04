using System;
using System.Linq;
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
MirClass[] classes = { MirClass.Warrior, MirClass.Wizard, MirClass.Taoist, MirClass.Assassin, MirClass.Archer };
int[] counts = { 21, 27, 29, 21, 24 };
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
Check(SpellCurriculum.ShouldGrant(MirClass.Wizard, Spell.FireBall, 1, 7, false), "Fresh grant");
Check(!SpellCurriculum.ShouldGrant(MirClass.Wizard, Spell.FireBall, 1, 7, true), "Repeated migration should not duplicate");
Check(!SpellCurriculum.ShouldGrant(MirClass.Warrior, Spell.ShoulderDash, 29, 30, false), "Not yet entitled");
Check(!SpellCurriculum.Owns(MirClass.Wizard, Spell.FastMove), "Unseeded FastMove");
Check(!SpellCurriculum.Owns(MirClass.Wizard, Spell.Blink), "Equipment-only Blink");
Check(!SpellCurriculum.Owns(MirClass.Warrior, Spell.BattleCry), "Custom BattleCry");
Check(SpellCurriculum.Name(MirClass.Taoist, CombatSpecialisation.Third) == "Spirit Binder", "Spec mapping");
Check(!SpellCurriculum.IsValid((CombatSpecialisation)255), "Invalid spec value");
// These tests must fail to compile without the spec catalogue implementation.
Check(SpellCurriculum.SpecialisationUnlockLevel == 20, "Specialisation level threshold");
foreach (MirClass actorClass in classes)
{
    Check(!SpellCurriculum.CanSelect(actorClass, 19, CombatSpecialisation.Unchosen, CombatSpecialisation.First),
        "Reject early selection");
    Check(SpellCurriculum.CanSelect(actorClass, 20, CombatSpecialisation.Unchosen, CombatSpecialisation.First),
        "Accept initial choice");
    Check(!SpellCurriculum.CanSelect(actorClass, 90, CombatSpecialisation.First, CombatSpecialisation.Second),
        "No unplanned respec");
    Check(!SpellCurriculum.CanSelect(actorClass, 90, CombatSpecialisation.Unchosen, (CombatSpecialisation)255),
        "Reject invalid byte");
    Check(!SpellCurriculum.CanSelect(actorClass, 90, CombatSpecialisation.Unchosen, CombatSpecialisation.Unchosen),
        "Reject unchosen selection");

    var required = new int[4];
    foreach (Spell spell in SpellCurriculum.ForClass(actorClass))
    {
        var home = SpellCurriculum.RequiredSpecialisation(actorClass, spell);
        Check(SpellCurriculum.IsValid(home), "Every skill has a valid home");
        required[(int)home]++;
        for (int slot = 0; slot <= 3; slot++)
        {
            var selected = (CombatSpecialisation)slot;
            Check(SpellCurriculum.CanUse(actorClass, selected, spell) ==
                  (home == CombatSpecialisation.Unchosen || home == selected),
                "Strict class-specific combat loadout");
        }
    }
    for (int slot = 0; slot <= 3; slot++)
        Check(required[slot] > 0, "All three specs and a shared kit have abilities");

    for (int slot = 1; slot <= 3; slot++)
    {
        var spec = (CombatSpecialisation)slot;
        var ability = SpecialisationAbilities.Signature(actorClass, spec);
        Check(ability != null && ability.Windup > 0 && ability.Cooldown > ability.Windup,
            $"Missing usable signature for {actorClass} {spec}");
        Check(!SpellCurriculum.IsUnlocked(actorClass, ability!.Spell, 19, 25) &&
            SpellCurriculum.IsUnlocked(actorClass, ability.Spell, 20, 25), "Separate unlock and proficiency levels");
        var traits = SpecialisationAbilities.TraitsFor(ability.Spell);
        Check(traits.Select(t => t.Level).SequenceEqual(new byte[] { 20, 32, 45 }), "Three trait milestones");
        var stats = new Stats();
        SpecialisationAbilities.ApplyTraitStats(stats, actorClass, spec, 19);
        Check(stats.Equals(new Stats()), "Traits unlock with their spec");
        SpecialisationAbilities.ApplyTraitStats(stats, actorClass, spec, 20);
        Check(!stats.Equals(new Stats()), "Every specialisation applies a real stat trait");
    }
}
Check(SpellCurriculum.CanUse(MirClass.Warrior, CombatSpecialisation.Unchosen, Spell.None),
    "Unarmed default attack stays available");
Check(!SpellCurriculum.CanUse(MirClass.Warrior, CombatSpecialisation.First, Spell.ThunderBolt),
    "Cross-class spell must not bypass the loadout");
Check(SpellCurriculum.CanUse(MirClass.Wizard, CombatSpecialisation.Unchosen, Spell.Blink),
    "Existing non-curriculum equipment spells keep their existing rules");
Check(SpellCurriculum.RequiredSpecialisation(MirClass.Warrior, Spell.Slaying) == CombatSpecialisation.First,
    "Warrior passive belongs to Iron Vow");
Check(!SpellCurriculum.CanUse(MirClass.Warrior, CombatSpecialisation.Second, Spell.Slaying),
    "Red Tempest must not gain Iron Vow passive");

foreach (MirClass actorClass in new[] { MirClass.Warrior, MirClass.Wizard, MirClass.Taoist })
{
    var activeCounts = new int[3];
    for (int i = 0; i < activeCounts.Length; i++)
    {
        var spec = (CombatSpecialisation)(i + 1);
        var ability = SpecialisationAbilities.Signature(actorClass, spec)!;
        Check(SpellCombos.For(ability.Spell) != null && SpecialisationAbilities.TraitsFor(ability.Spell).Count == 3,
            $"Each {actorClass} loadout needs a combo and three traits");
        activeCounts[i] = SpellCurriculum.ForClass(actorClass).Count(spell =>
            SpellCurriculum.RequiredSpecialisation(actorClass, spell) == spec && !SpellCurriculum.IsPassive(spell));
    }
    Check(activeCounts.Max() - activeCounts.Min() <= 1, $"{actorClass} role kits should differ by at most one active");
}

foreach (Spell builder in new[] { Spell.Thrusting, Spell.HalfMoon, Spell.ProtectionField, Spell.GreatFireBall,
    Spell.FrostCrunch, Spell.ThunderBolt })
    Check(SpellCurriculum.UnlockLevel(builder, 35) == 20, "Core setup spell available at specialisation unlock");

var progress = new SpellComboProgress();
var embers = SpellCombos.For(Spell.AshSeal);
Check(progress.Build(embers, 100, 0) && !progress.Build(embers, 200, 0),
    "A multi-hit builder cannot award an extra stack inside its internal interval");
Check(progress.Build(embers, 1100, 0) && progress.Build(embers, 2100, 0) && progress.Count(2100) == 2,
    "Stacks are capped");
Check(progress.Count(10100) == 0 && !progress.Spend(10100, 0), "Expired stacks cannot be spent");
var rime = SpellCombos.For(Spell.GlacialGate);
Check(progress.Build(rime, 11000, 3) && !progress.Spend(11000, 4) && progress.Count(11000, 3) == 1,
    "A mismatched target does not consume a mark");
Check(progress.Spend(11000, 3) && progress.Count(11000, 3) == 0, "Matching targets consume their mark once");

Console.WriteLine("Spell curriculum tests passed.");
