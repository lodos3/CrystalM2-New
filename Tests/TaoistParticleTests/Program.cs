using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Client.MirScenes;
using CrystalM2.Rendering.Core;

// Exercise the actual authored Taoist renderer without requiring an active game scene or GPU.
static SpellParticle[] Render(Spell spell, SpellVisualPhase phase, long elapsed, int duration = 1200)
{
    var type = typeof(SpellVisual).Assembly.GetType("Client.MirScenes.TaoistSpellEffects")
        ?? throw new InvalidOperationException("Taoist renderer missing.");
    var draw = type.GetMethod("DrawFrame", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Taoist draw method missing.");
    var pool = new SpellParticlePool(8192);
    draw.Invoke(null, [pool, new SpellVisual(spell, phase), new Vector2(400, 400), Vector2.UnitX,
        elapsed, 0L, duration, 1.28f, 42u, 0u, 354f]);
    return pool.Active.ToArray();
}
static SpellParticle[] RenderWizard(Spell spell, SpellVisualPhase phase, long elapsed, int duration = 800)
{
    var type = typeof(SpellVisual).Assembly.GetType("Client.MirScenes.WizardSpellEffects")
        ?? throw new InvalidOperationException("Wizard renderer missing.");
    var draw = type.GetMethod("DrawFrame", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Wizard draw method missing.");
    var pool = new SpellParticlePool(8192);
    draw.Invoke(null, [pool, new SpellVisual(spell, phase), new Vector2(400, 400), Vector2.UnitX,
        elapsed, 0L, duration, 1.28f, 42u, 0u]);
    return pool.Active.ToArray();
}
static SpellParticle[] RenderWarrior(Spell spell, SpellVisualPhase phase, long elapsed, int duration = 800)
{
    var type = typeof(SpellVisual).Assembly.GetType("Client.MirScenes.WarriorSpellEffects")
        ?? throw new InvalidOperationException("Warrior renderer missing.");
    var draw = type.GetMethod("DrawFrame", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Warrior draw method missing.");
    var pool = new SpellParticlePool(8192);
    draw.Invoke(null, [pool, new SpellVisual(spell, phase), new Vector2(400, 400), Vector2.UnitX,
        elapsed, 0L, duration, 1.28f, 42u, null]);
    return pool.Active.ToArray();
}
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
static float Reach(SpellParticle[] particles) =>
    particles.Length == 0 ? 0 : particles.Max(p => MathF.Abs(p.Origin.X - 400));

foreach (Spell ward in new[] { Spell.SoulShield, Spell.BlessedArmour })
{
    // Wards now render a single, full-sized impact at the aimed map cell,
    // not a compact hit on each buff recipient.
    SpellParticleShape glyph = ward == Spell.SoulShield
        ? SpellParticleShape.SoulGlyph : SpellParticleShape.ArmourGlyph;
    var opening = Render(ward, SpellVisualPhase.Impact, 250);
    var formed = Render(ward, SpellVisualPhase.Impact, 700);
    var burning = Render(ward, SpellVisualPhase.Impact, 900);
    Check(opening.Length > 0 && formed.Length > 0, $"Ward particles absent for {ward}");
    var earlyFire = opening.Where(p => p.Shape == SpellParticleShape.FlameTongue).ToArray();
    var fullFire = formed.Where(p => p.Shape == SpellParticleShape.FlameTongue).ToArray();
    Check(opening.All(p => p.Shape != glyph) && earlyFire.Length > 0 && earlyFire.Length < fullFire.Length &&
        earlyFire.Average(p => p.Origin.X + p.Origin.Y) < fullFire.Average(p => p.Origin.X + p.Origin.Y) - 18,
        $"The fire must trace {ward} from the upper corner before revealing the complete glyph.");
    // Authored sprite Size is stored before the renderer's 1.28 scale.
    Check(formed.Where(p => p.Shape == glyph).Any(p => ward == Spell.SoulShield
        ? p.Dimensions(0f, 1.28f).X is >= 320 and <= 340 : p.Dimensions(0f, 1.28f).X is >= 285 and <= 307),
        $"Ward glyph must stay within the aimed cast's footprint for {ward}");
    Check(!formed.Any(p => p.Shape is SpellParticleShape.Halo or SpellParticleShape.Orb ||
        p.Shape == SpellParticleShape.SmokeSequence && p.Dimensions(0f, 1.28f).X > 32),
        $"Ward has an unwanted oval/background effect: {ward}");
    Check(!formed.Any(p => p.Shape == SpellParticleShape.Wisp &&
        p.Dimensions(0f, 1.28f).X > 70), $"Ward has an oversized backdrop wisp: {ward}");
    Check(formed.Any(p => p.Shape == SpellParticleShape.FlameTongue && p.Emissive) &&
          burning.Any(p => p.Shape == SpellParticleShape.FlameTongue && p.Emissive),
        $"Ward needs animated flames following its strokes for {ward}");
    Check(formed.Any(p => (p.Shape is SpellParticleShape.Spark or SpellParticleShape.UserEmber) && p.Emissive),
        $"Ward needs textured sparks emitted from its stroke lines: {ward}");
    Check(formed.Any(p => p.Shape == SpellParticleShape.Flash && p.Emissive),
        $"Ward needs local flares along its stroke lines: {ward}");
    Check(formed.Any(p => p.Layer == SpellParticleLayer.EmissiveCore), $"Ward lacks light for {ward}");
}

// These three rune spells are always visible above players and monsters,
// regardless of the scenery depth/occlusion mask. Nearby casts still show.
foreach (Spell aimedRune in new[] { Spell.SoulShield, Spell.BlessedArmour, Spell.Curse })
{
    var visible = Render(aimedRune, SpellVisualPhase.Impact, 680,
        aimedRune == Spell.Curse ? 2000 : 1600);
    Check(visible.Length > 0 && visible.All(p => p.AlwaysOnTop),
        $"Aimed rune {aimedRune} must draw above actor/scenery occlusion.");
}
Check(!Render(Spell.Healing, SpellVisualPhase.Impact, 400, 800).Any(p => p.AlwaysOnTop),
    "Normal spells must continue respecting scenery depth.");

foreach (Spell ward in new[] { Spell.SoulShield, Spell.BlessedArmour })
{
    // Between neighbouring frames the flame roots keep their horizontal
    // positions. The tongues rise and fade vertically instead of sliding
    // sideways along the rune strokes.
    var earlier = Render(ward, SpellVisualPhase.Impact, 600, 1600)
        .Where(p => p.Shape == SpellParticleShape.FlameTongue).ToArray();
    var later = Render(ward, SpellVisualPhase.Impact, 630, 1600)
        .Where(p => p.Shape == SpellParticleShape.FlameTongue).ToArray();
    Check(earlier.Length >= 8 && later.Length >= 8,
        $"Protective rune {ward} needs several independent upward flames.");
    // Individual tongues may fade out between frames. Compare their nearby
    // horizontal roots rather than assuming fixed array indices.
    Check(later.Count(flame => earlier.Any(prior =>
        MathF.Abs(prior.Origin.X - flame.Origin.X) < 6)) >= later.Length * .85f,
        $"Protective rune {ward} flames must not travel horizontally along glyph strokes.");
}

var massHealCast = Render(Spell.MassHealing, SpellVisualPhase.Impact, 450, 1000);
var massHealRecipient = Render(Spell.MassHealing, SpellVisualPhase.Hit, 450, 1000);
Check(massHealCast.Length > 0 && massHealRecipient.Length > 0 &&
    Reach(massHealRecipient) < Reach(massHealCast),
    "Each Mass Healing recipient needs a distinct compact bloom.");

var hideCast = Render(Spell.MassHiding, SpellVisualPhase.Impact, 460, 800);
var hideRecipient = Render(Spell.MassHiding, SpellVisualPhase.Hit, 460, 800);
Check(hideRecipient.Any(p => p.Shape == SpellParticleShape.SmokeSequence) &&
    Reach(hideRecipient) < Reach(hideCast),
    "Mass Hiding hit should disperse around each recipient rather than replaying the whole area.");

var shieldA = Render(Spell.EnergyShield, SpellVisualPhase.Aura, 160, 800);
var shieldB = Render(Spell.EnergyShield, SpellVisualPhase.Aura, 470, 800);
// The exterior shell is immobile relative to the actor; only electricity moves.
var shellA = shieldA.Where(p => p.Shape == SpellParticleShape.Filament &&
    p.Layer == SpellParticleLayer.Emissive).Select(p => (p.Origin, p.Ribbon)).ToArray();
var shellB = shieldB.Where(p => p.Shape == SpellParticleShape.Filament &&
    p.Layer == SpellParticleLayer.Emissive).Select(p => (p.Origin, p.Ribbon)).ToArray();
var electricA = shieldA.Where(p => p.Shape == SpellParticleShape.Filament &&
    p.Layer == SpellParticleLayer.EmissiveCore).Select(p => p.Origin).ToArray();
var electricB = shieldB.Where(p => p.Shape == SpellParticleShape.Filament &&
    p.Layer == SpellParticleLayer.EmissiveCore).Select(p => p.Origin).ToArray();
Check(shellA.Length >= 24 && shellA.SequenceEqual(shellB),
    "Energy Shield shell must remain stationary across the aura cycle.");
Check(electricA.Length >= 24 && !electricA.SequenceEqual(electricB),
    "Energy Shield electricity must animate along its fixed shell.");
Check(shieldA.Any(p => p.Shape == SpellParticleShape.Spark),
    "Energy Shield must shed electric sparks from its side lines.");

var petAura = Render(Spell.PetEnhancer, SpellVisualPhase.Aura, 400);
var petRune = petAura.Where(p => p.Shape == SpellParticleShape.ArmourGlyph).ToArray();
Check(petRune.Length == 1 && petRune[0].Dimensions(0, 1.28f).X <= 42 &&
    petRune[0].Dimensions(0, 1.28f).Y <= 44 &&
    petRune[0].Origin.Y < 400 - 65,
    "Pet Enhancer overhead glyph must be compact, short and above the head.");

foreach (Spell spell in new[] { Spell.FlamingSword, Spell.TwinDrakeBlade })
{
    var contact = RenderWarrior(spell, SpellVisualPhase.Contact, 270);
    Check(contact.Length > 0, $"Missing warrior hit visuals for {spell}");
    Check(!contact.Any(p => p.Shape is SpellParticleShape.Orb or SpellParticleShape.Halo or
        SpellParticleShape.WarriorRing or SpellParticleShape.WarriorImpact),
        $"Warrior hit {spell} must not draw circular or oval background effects.");
    if (spell == Spell.FlamingSword)
        Check(contact.Any(p => p.Shape == SpellParticleShape.SmokeSequence) &&
              contact.Any(p => p.Shape == SpellParticleShape.FlameSequence) &&
              contact.Any(p => p.Shape == SpellParticleShape.FlameTongue) &&
              contact.Any(p => p.Shape == SpellParticleShape.WarriorSpark),
            "Flaming Sword impact must use smoke, fire, burning tongues and embers.");
    else
        Check(contact.Any(p => p.Shape == SpellParticleShape.Filament &&
              p.Layer == SpellParticleLayer.EmissiveCore) &&
              contact.Any(p => p.Shape == SpellParticleShape.Spark ||
                  p.Shape == SpellParticleShape.WarriorSpark) &&
              !contact.Any(p => p.Shape is SpellParticleShape.FlameSequence or
                  SpellParticleShape.FlameTongue or SpellParticleShape.SmokeSequence),
            "Twin Drake Blade impact must be electric arcs and sparks without flames.");
}
Check(!RenderWarrior(Spell.TwinDrakeBlade, SpellVisualPhase.Prepare, 270, 500)
    .Any(p => p.Shape is SpellParticleShape.Orb or SpellParticleShape.Halo),
    "Twin Drake Blade preparation must have no floating oval backdrop.");

foreach (Spell summon in new[] { Spell.SummonSkeleton, Spell.SummonHolyDeva, Spell.SummonShinsu })
    Check(Render(summon, SpellVisualPhase.Impact, 480).Any(p => p.Shape == SpellParticleShape.Spark),
        $"Summoning rite should produce flare particles: {summon}");

foreach (Spell ward in new[] { Spell.SoulShield, Spell.BlessedArmour })
{
    var textured = Render(ward, SpellVisualPhase.Impact, 680, 1600);
    Check(textured.Any(p => p.Shape == SpellParticleShape.UserFlame) &&
          textured.Any(p => p.Shape == SpellParticleShape.UserEmber),
        $"Protective rune {ward} must use uploaded flame and ember coverage.");
}
Check(RenderWarrior(Spell.FlamingSword, SpellVisualPhase.Contact, 280)
    .Any(p => p.Shape == SpellParticleShape.UserFlame),
    "Flaming Sword hit should use imported directional flame artwork.");
Check(RenderWarrior(Spell.TwinDrakeBlade, SpellVisualPhase.Contact, 280)
    .Any(p => p.Shape == SpellParticleShape.UserLightning),
    "Twin Drake impact should use imported lightning artwork.");
Check(RenderWizard(Spell.FireWall, SpellVisualPhase.Field, 340)
    .Any(p => p.Shape == SpellParticleShape.UserFlame),
    "Fire Wall flames should use imported flame artwork.");
Check(RenderWizard(Spell.ThunderBolt, SpellVisualPhase.Impact, 340)
    .Any(p => p.Shape == SpellParticleShape.UserLightning),
    "Thunder Bolt should use imported lightning artwork.");
Check(RenderWizard(Spell.MeteorStrike, SpellVisualPhase.Field, 660, 1200)
    .Any(p => p.Shape == SpellParticleShape.UserRock),
    "Meteor Strike should carry textured debris, not only glowing orbs.");
// Weapon contacts and fireball impacts use the imported detailed silhouettes,
// avoiding the previous large round orb/billboard effects.
var halfMoonImpact = RenderWarrior(Spell.HalfMoon, SpellVisualPhase.Contact, 230, 390);
Check(halfMoonImpact.Any(p => p.Shape == SpellParticleShape.UserSlash) &&
      !halfMoonImpact.Any(p => p.Shape == SpellParticleShape.Orb),
    "Physical warrior contact needs textured blade detail without a round backdrop.");
var fireballImpact = RenderWizard(Spell.FireBall, SpellVisualPhase.Impact, 270, 600);
Check(fireballImpact.Any(p => p.Shape == SpellParticleShape.UserFlame &&
      p.Emissive) && !fireballImpact.Any(p => p.Shape == SpellParticleShape.Orb),
    "Wizard Fire Ball should burst as layered textured fire instead of a glowing orb.");

// Electrical effects should rely on branching paths rather than bright
// circular backdrops. Spell-specific field/movement phases are preserved.
foreach (var phaseTest in new[]
    {
        (Spell.ThunderBolt, SpellVisualPhase.Impact),
        (Spell.ThunderStorm, SpellVisualPhase.Impact),
        (Spell.StormEscape, SpellVisualPhase.Prepare),
        (Spell.StormEscape, SpellVisualPhase.Impact)
    })
{
    var particles = RenderWizard(phaseTest.Item1, phaseTest.Item2, 270);
    Check(particles.Any(p => p.Shape == SpellParticleShape.UserLightning),
        $"{phaseTest.Item1}/{phaseTest.Item2} must use uploaded electric textures.");
    Check(!particles.Any(p => p.Shape == SpellParticleShape.Orb),
        $"{phaseTest.Item1}/{phaseTest.Item2} should not use large round glow billboards.");
}

var curseAtAim = Render(Spell.Curse, SpellVisualPhase.Impact, 850, 2000);
Check(curseAtAim.Any(p => p.Shape == SpellParticleShape.CurseGlyph),
    "Curse must render a complete rune on the aimed map cell.");
Check(curseAtAim.Any(p => p.Shape == SpellParticleShape.SmokeSequence),
    "Curse glyph should release smoke.");
Check(curseAtAim.Any(p => p.Shape == SpellParticleShape.MistFilament && p.Size > 70),
    "Curse must fan spectral rays above the brush strokes at its peak.");
var curseAfterglow = Render(Spell.Curse, SpellVisualPhase.Impact, 1999, 2000);
Check(curseAfterglow.Any(p => p.Shape == SpellParticleShape.SmokeSequence) &&
      curseAfterglow.All(p => p.Shape != SpellParticleShape.CurseGlyph),
    "The late curse should break up into residue once the glyph has disappeared.");
// Protective ward release smoke must come from the brush, rise and drift instead
// of materialising as an oval backdrop or appearing before the first strokes.
foreach (Spell ward in new[] { Spell.SoulShield, Spell.BlessedArmour })
{
    var opening = Render(ward, SpellVisualPhase.Impact, 250, 1600);
    var released = Render(ward, SpellVisualPhase.Impact, 1370, 1600);
    var drifted = Render(ward, SpellVisualPhase.Impact, 1410, 1600);
    SpellParticleShape glyph = ward == Spell.SoulShield
        ? SpellParticleShape.SoulGlyph : SpellParticleShape.ArmourGlyph;
    var fullyFormed = Render(ward, SpellVisualPhase.Impact, 700, 1600);
    Check(released.Where(p => p.Shape == glyph).Sum(p => p.Colour.A) <
          fullyFormed.Where(p => p.Shape == glyph).Sum(p => p.Colour.A) * .65f,
        $"Protective ward glyph must dissipate behind its release smoke: {ward}");
    var earlySmoke = opening.Where(p => p.Shape == SpellParticleShape.SmokeSequence).ToArray();
    var releasedSmoke = released.Where(p => p.Shape == SpellParticleShape.SmokeSequence).ToArray();
    var driftedSmoke = drifted.Where(p => p.Shape == SpellParticleShape.SmokeSequence).ToArray();
    Check(earlySmoke.Length == 0, $"Ward {ward} should not smoke before its stroke lines ignite.");
    Check(releasedSmoke.Length >= 4 && releasedSmoke.All(p => p.Layer == SpellParticleLayer.Smoke &&
        p.Dimensions(0f, 1.28f).X <= 32),
        $"Protective ward release smoke must be multiple small textured stroke plumes: {ward}");
    Check(releasedSmoke.Length == driftedSmoke.Length &&
        releasedSmoke.Zip(driftedSmoke).Any(pair =>
            Vector2.Distance(pair.First.Origin, pair.Second.Origin) > .8f &&
            pair.Second.Origin.Y < pair.First.Origin.Y),
        $"Protective ward release smoke must continue drifting upward: {ward}");

    var firstFlames = Render(ward, SpellVisualPhase.Impact, 1000, 1600)
        .Where(p => p.Shape == SpellParticleShape.FlameTongue).ToArray();
    var nextFlames = Render(ward, SpellVisualPhase.Impact, 1040, 1600)
        .Where(p => p.Shape == SpellParticleShape.FlameTongue).ToArray();
    Check(firstFlames.Length > 8 && firstFlames.Length == nextFlames.Length &&
        firstFlames.Zip(nextFlames).Count(pair =>
            pair.Second.Origin.Y < pair.First.Origin.Y - .4f) > firstFlames.Length * .55f,
        $"Ward flames should rise off the fixed stroke roots instead of remaining static: {ward}");
}

Console.WriteLine("Taoist particle composition tests passed.");
