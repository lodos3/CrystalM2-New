using System;
using System.Linq;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

MirClass[] classes = [MirClass.Warrior, MirClass.Wizard, MirClass.Taoist, MirClass.Assassin, MirClass.Archer];
int[] counts = [18, 27, 25, 17, 21];
for (int i = 0; i < classes.Length; i++)
{
    MirClass actorClass = classes[i];
    var skills = SpellCurriculum.ForClass(actorClass);
    Check(skills.Distinct().Count() == counts[i] && skills.Count == counts[i], $"Unique curriculum for {actorClass}");
    foreach (Spell spell in skills)
    {
        Check(!SpellCurriculum.IsRetired(spell), $"Exclude retired ability {spell}");
        Check(SpellCurriculum.ShouldGrant(actorClass, spell, 100, 20, false), $"Auto-learn {spell} for its owner");
        Check(!SpellCurriculum.ShouldGrant(actorClass, spell, 100, 20, true), $"Reconcile {spell} without a duplicate");
        if (SpellPerks.Supports(actorClass))
            Check(SpellPerks.For(spell).Count >= 2 && SpellPerks.For(spell).Select(option => option.Perk).Distinct().Count() == SpellPerks.For(spell).Count,
                $"Provide at least two distinct choices for {spell}");
        foreach (MirClass other in classes.Where(c => c != actorClass))
            Check(!SpellCurriculum.CanUseLegacy(other, spell, 100, 20, false), $"Reject native {spell} on {other}");
    }
}

Check(SpellCurriculum.IsUnlocked(MirClass.Wizard, Spell.FireBall, 1, 7), "Starter Wizard ability");
Check(SpellCurriculum.IsUnlocked(MirClass.Taoist, Spell.Healing, 1, 7), "Starter Taoist ability");
Check(SpellCurriculum.IsUnlocked(MirClass.Warrior, Spell.Fencing, 1, 7), "Starter Warrior ability");
Check(!SpellCurriculum.IsUnlocked(MirClass.Warrior, Spell.ShoulderDash, 29, 30), "Shoulder Dash requires character level 30");
Check(SpellCurriculum.IsUnlocked(MirClass.Warrior, Spell.ShoulderDash, 30, 30), "Grant the spell at its defined level");
Check(!SpellCurriculum.CanUseLegacy(MirClass.Warrior, Spell.ArcaneHammer, 90, 20, true), "Retired spells cannot be granted by equipment");
Check(SpellCurriculum.CanUseLegacy(MirClass.Taoist, Spell.Blink, 10, 19, true), "Explicit equipment can grant a legacy ability");
Check(SpellPerks.Points(9) == 0 && SpellPerks.Points(10) == 1 && SpellPerks.Points(23) == 14, "Point budget follows character level");

SpellPerkPick splash = new(Spell.FlamingSword, SpellPerk.Splash, 1);
SpellPerkPick burn = new(Spell.FlamingSword, SpellPerk.Burn, 1);
Check(SpellPerks.Validate(MirClass.Warrior, 11, [splash, burn], _ => true, out _), "Valid splash-to-burn path");
Check(!SpellPerks.Validate(MirClass.Warrior, 11, [burn], _ => true, out _), "Burn requires splash");
Check(!SpellPerks.Validate(MirClass.Warrior, 10, [splash, burn], _ => true, out _), "Reject overspending");
Check(!SpellPerks.Validate(MirClass.Warrior, 50, [splash, splash], _ => true, out _), "Reject duplicate ranks");
Check(!SpellPerks.Validate(MirClass.Warrior, 50, [splash], _ => false, out _), "Reject a perk for an unlearned spell");
Check(!SpellPerks.Validate(MirClass.Wizard, 50, [splash], _ => true, out _), "Reject foreign spell perks");
Check(!SpellPerks.Validate(MirClass.Warrior, 50,
    [new(Spell.ShoulderDash, SpellPerk.DashImpact, 1), new(Spell.ShoulderDash, SpellPerk.DashDistance, 1)],
    _ => true, out _), "Dash choices are exclusive");

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

Check(TalentAbilities.All.Count == 57 && TalentAbilities.All.Select(a => a.Spell.Spell).Distinct().Count() == 57,
    "All original actions have distinct registered spell IDs");
Check(TalentCatalog.Points(10, true) == 1 && TalentCatalog.Points(62, false) == 15 && TalentCatalog.Points(71, false) == 18,
    "Independent level-derived talent budgets");
foreach (MirClass actorClass in new[] { MirClass.Warrior, MirClass.Wizard, MirClass.Taoist })
foreach (var spec in new[] { CombatSpecialisation.First, CombatSpecialisation.Second, CombatSpecialisation.Third })
{
    TalentTree tree = TalentCatalog.Tree(actorClass, spec);
    TalentAbility[] six = TalentAbilities.All.Where(a => a.Spell.Class == actorClass && a.Spell.Spec == spec).ToArray();
    Check(tree.Nodes.Length == 24 && six.Length == 6 && six.Count(a => a.Kind == TalentAbilityKind.Control) == 1 &&
        six.Count(a => a.Kind == TalentAbilityKind.Counter) == 1, "Every spec receives a full talent and spell catalogue");
    for (int engine = 0; engine < 3; engine++)
    {
        TalentPick[] picks = tree.Nodes.Where(node => node.Row switch
        {
            1 or 2 => true,
            3 or 5 or 6 => node.Column is 0 or 2,
            4 => node.Column == engine,
            7 or 8 => node.Column == 0,
            _ => false
        }).Select(node => new TalentPick(node.Id, node.MaxRank,
            (byte)(actorClass == MirClass.Wizard && spec == CombatSpecialisation.Second && node.Row == 5 && node.Column == 0 ? 1 : 0))).ToArray();
        Check(TalentCatalog.Validate(tree, 71, picks, out string? reason), reason ?? "Both optional spells fit an off-column engine and capstone");
        var build = new TalentLoadout(actorClass, spec, [], picks);
        Check(six.All(a => build.CanUse(71, a.Spell.Spell)) && build.Engine == engine,
            $"{actorClass}/{spec} engine {build.Engine}/{engine}, missing {string.Join(", ", six.Where(a => !build.CanUse(71, a.Spell.Spell)).Select(a => a.Spell.Name))}");
        var recipe = TalentRotations.Get(actorClass, spec, engine);
        Spell action = TalentRotations.ActionForFinisher(recipe.Finisher, engine);
        Check(TalentRotations.IsFinisher(recipe.Finisher) && !TalentRotations.IsFinisher(action) &&
            build.CanUse(71, action), "Every chosen rotation resolves to a bindable action in that spec");
        Check(!TalentCatalog.Validate(tree, 71, [..picks, new(tree.At(4, (engine + 1) % 3).Id, 1, 0)], out _), "A second engine is rejected");
    }
}
Check(!new TalentLoadout(MirClass.Assassin, CombatSpecialisation.First, [], []).CanUse(71, Spell.ArcaneHammer),
    "Legacy classes cannot cast a foreign talent ability even if a record exists");

Console.WriteLine("Spell curriculum tests passed.");
