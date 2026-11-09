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
static SpellParticle[] RenderWarrior(Spell spell, SpellVisualPhase phase, long elapsed, int duration = 800,
    Vector2? origin = null)
{
    var type = typeof(SpellVisual).Assembly.GetType("Client.MirScenes.WarriorSpellEffects")
        ?? throw new InvalidOperationException("Warrior renderer missing.");
    var draw = type.GetMethod("DrawFrame", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Warrior draw method missing.");
    var pool = new SpellParticlePool(8192);
    draw.Invoke(null, [pool, new SpellVisual(spell, phase), origin ?? new Vector2(400, 400), Vector2.UnitX,
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

// Warrior VFX density/attachment regressions: these failed on the old
// graphics-settings snapshot after the 116-element combustion was replaced.
var remasteredFire = RenderWarrior(Spell.FlamingSword, SpellVisualPhase.Contact, 280);
var fireTongues = remasteredFire.Where(p => p.Shape == SpellParticleShape.UserFlame).ToArray();
Check(fireTongues.Length >= 90 &&
      fireTongues.All(p => p.Dimensions(0f, 1.28f).X <= 22 && p.Dimensions(0f, 1.28f).Y <= 42) &&
      remasteredFire.Count(p => p.Shape == SpellParticleShape.SmokeSequence) >= 20 &&
      remasteredFire.Count(p => p.Shape is SpellParticleShape.UserEmber or SpellParticleShape.WarriorSpark) >= 20,
    "Flaming Sword contact needs dense small fire, embers and separate smoke instead of giant tongues.");
var drakeCharge = RenderWarrior(Spell.TwinDrakeBlade, SpellVisualPhase.Prepare, 260, 500);
Check(drakeCharge.Count(p => p.Shape == SpellParticleShape.Filament) >= 28 &&
      drakeCharge.Count(p => p.Shape == SpellParticleShape.UserLightning) >= 8 &&
      drakeCharge.Count(p => p.Shape == SpellParticleShape.Spark) >= 14,
    "Twin Drake charge must contain multiple fine coiling filaments and small electrical sparks.");
var movedDrake = RenderWarrior(Spell.TwinDrakeBlade, SpellVisualPhase.Prepare, 260, 500,
    new Vector2(443, 427));
Check(drakeCharge.Length == movedDrake.Length &&
      drakeCharge.Zip(movedDrake).All(pair =>
          Vector2.Distance(pair.Second.Origin - pair.First.Origin, new Vector2(43, 27)) < .025f),
    "Twin Drake's charge must stay attached to a moving caster.");
var remasteredDrakeHit = RenderWarrior(Spell.TwinDrakeBlade, SpellVisualPhase.Contact, 280);
Check(remasteredDrakeHit.Count(p => p.Shape == SpellParticleShape.UserLightning) >= 30 &&
      remasteredDrakeHit.Count(p => p.Shape == SpellParticleShape.Filament) >= 8 &&
      !remasteredDrakeHit.Any(p => p.Shape is SpellParticleShape.Orb or SpellParticleShape.Halo),
    "Twin Drake impact needs target-wrapping discharge, not a mirrored X or circular plate.");

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

// Preserve Ice Storm's runic disc as a rigid, orthographically projected
// seal across preparation/impact. Only opacity is allowed to breathe.
var stormPrepareA = RenderWizard(Spell.IceStorm, SpellVisualPhase.Prepare, 230, 600);
var stormPrepareB = RenderWizard(Spell.IceStorm, SpellVisualPhase.Prepare, 430, 600);
var stormImpact = RenderWizard(Spell.IceStorm, SpellVisualPhase.Impact, 430, 600);
var sealA = stormPrepareA.Single(p => p.Shape == SpellParticleShape.SigilDisc);
var sealB = stormPrepareB.Single(p => p.Shape == SpellParticleShape.SigilDisc);
var sealImpact = stormImpact.Single(p => p.Shape == SpellParticleShape.SigilDisc);
Check(sealA.Origin == sealB.Origin && sealA.Origin == sealImpact.Origin &&
      sealA.Rotation == 0 && sealB.Rotation == 0 && sealImpact.Rotation == 0 &&
      sealA.Dimensions(0, 1.28f) == sealB.Dimensions(0, 1.28f) &&
      sealA.Dimensions(0, 1.28f) == sealImpact.Dimensions(0, 1.28f),
    "Ice Storm rune must stay geometrically fixed rather than rotating or skewing its elliptical footprint.");
foreach (var storm in new[] { stormPrepareB, stormImpact })
{
    var heads = storm.Where(p => p.Shape == SpellParticleShape.UserSlash && p.DepthY.HasValue).ToArray();
    var tails = storm.Where(p => p.Shape == SpellParticleShape.Filament).ToArray();
    Check(heads.Length >= 10 && tails.Length >= 10,
        "Ice Storm gusts need continuous curved trailing filaments, not detached radial spokes.");
    Check(heads.Count(p =>
    {
        Vector2 groundRadial = new(p.Origin.X - 400,
            (p.DepthY!.Value - 400) / (2f / 3f));
        Vector2 tangent = new(-groundRadial.Y, groundRadial.X * (2f / 3f));
        return tangent.LengthSquared() > 10 &&
            Vector2.Dot(Vector2.Normalize(tangent),
                new Vector2(MathF.Cos(p.Rotation), MathF.Sin(p.Rotation))) > .97f;
    }) >= heads.Length * .85f,
        "Ice Storm slash heads must face the orbital tangent, not away from the storm.");
}
var stormEarlyHead = stormPrepareA.Where(p => p.Shape == SpellParticleShape.UserSlash).ToArray();
var stormLateHead = stormPrepareB.Where(p => p.Shape == SpellParticleShape.UserSlash).ToArray();
Check(stormEarlyHead.Select(p => p.Size).Distinct().Count() >= 6 &&
      stormLateHead.Select(p => p.Size).Distinct().Count() >= 6 &&
      !stormEarlyHead.Select(p => p.Size).SequenceEqual(stormLateHead.Select(p => p.Size)),
    "Ice Storm gust sizes must breathe independently as they orbit.");

// Frost Crunch retains the existing textured frostfire emission but needs an
// unmistakable near-white centre inside multiple cool-blue shell layers.
foreach (var phase in new[] { SpellVisualPhase.Prepare, SpellVisualPhase.Impact })
{
    var frost = RenderWizard(Spell.FrostCrunch, phase, 280, 600);
    var bright = frost.Where(p => p.Layer == SpellParticleLayer.EmissiveCore &&
        p.Colour.R >= 175 && p.Colour.B >= 220 && p.Colour.A >= 170).ToArray();
    Check(bright.Length >= 8 &&
          frost.Count(p => p.Shape == SpellParticleShape.FrostCore) >= 4 &&
          frost.Any(p => p.Shape == SpellParticleShape.Flash && p.Layer == SpellParticleLayer.EmissiveCore) &&
          !frost.Any(p => p.Shape == SpellParticleShape.Orb),
        $"Frost Crunch {phase} needs multiple luminous white-blue core and icy shell layers without a flat orb.");
}

// Regression: the graphics-settings snapshot dropped the earlier Wizard fire
// and frost remaster. Exercise the real emitters, without test doubles or a GPU.
var frostPrepare = RenderWizard(Spell.FrostCrunch, SpellVisualPhase.Prepare, 280, 600);
var frostImpact = RenderWizard(Spell.FrostCrunch, SpellVisualPhase.Impact, 280, 600);
Check(frostPrepare.Count(p => p.Shape == SpellParticleShape.UserFlame) >= 20 &&
      frostImpact.Count(p => p.Shape == SpellParticleShape.UserFlame) >= 25 &&
      !frostPrepare.Any(p => p.Shape == SpellParticleShape.Orb) &&
      !frostImpact.Any(p => p.Shape == SpellParticleShape.Orb) &&
      frostImpact.Any(p => p.Shape == SpellParticleShape.SmokeSequence),
    "Frost Crunch needs dense cold-blue flames, ice flecks and vapour in cast and impact, not round plates.");

var wizardType = typeof(SpellVisual).Assembly.GetType("Client.MirScenes.WizardSpellEffects")
    ?? throw new InvalidOperationException("Wizard effects type missing.");
var projectileMethod = wizardType.GetMethod("DrawProjectile", BindingFlags.Static | BindingFlags.NonPublic)
    ?? throw new InvalidOperationException("Wizard projectile renderer missing.");
var projectilePool = new SpellParticlePool(2048);
bool projectileDrawn = (bool)(projectileMethod.Invoke(null,
    [projectilePool, Spell.FrostCrunch, new Vector2(400, 350), Vector2.UnitX,
        1000L, 5, 250, true, 1.28f]) ?? false);
var frostTravel = projectilePool.Active.ToArray();
Check(projectileDrawn && frostTravel.Count(p => p.Shape == SpellParticleShape.UserFlame) >= 18 &&
      !frostTravel.Any(p => p.Shape == SpellParticleShape.Orb) &&
      frostTravel.Any(p => p.Shape == SpellParticleShape.SmokeSequence),
    "Frost Crunch must remain textured blue frostfire in flight rather than a giant glowing orb.");

var wallMid = RenderWizard(Spell.FireWall, SpellVisualPhase.Prepare, 600, 1000);
var wallRelease = RenderWizard(Spell.FireWall, SpellVisualPhase.Prepare, 950, 1000);
Check(wallMid.Count(p => p.Shape == SpellParticleShape.UserFlame) >= 35 &&
      wallMid.Any(p => p.Shape == SpellParticleShape.UserFlame && p.Origin.Y < 315) &&
      wallRelease.Count(p => p.Shape == SpellParticleShape.UserEmber) >= 20 &&
      wallRelease.Any(p => p.Shape == SpellParticleShape.SmokeSequence) &&
      !wallMid.Any(p => p.Shape == SpellParticleShape.Orb),
    "Fire Wall casting must climb from the feet, condense overhead and eject many embers on release.");
Check(RenderWizard(Spell.FireWall, SpellVisualPhase.Field, 400)
    .Any(p => p.Shape == SpellParticleShape.FlameSequence && p.Ground),
    "Fire Wall field must still be rooted in its map tile.");

var smallFireball = RenderWizard(Spell.FireBall, SpellVisualPhase.Impact, 260, 600);
Check(smallFireball.Count(p => p.Shape == SpellParticleShape.UserFlame) >= 40 &&
      smallFireball.Count(p => p.Shape == SpellParticleShape.UserEmber) >= 9 &&
      smallFireball.Count(p => p.Shape == SpellParticleShape.SmokeSequence) >= 8 &&
      !smallFireball.Any(p => p.Shape == SpellParticleShape.Orb),
    "Fire Ball impact should be a dense spray of small flames, smoke and embers, never a few giant cards.");
var greatFireball = RenderWizard(Spell.GreatFireBall, SpellVisualPhase.Prepare, 300, 600);
var greatImpact = RenderWizard(Spell.GreatFireBall, SpellVisualPhase.Impact, 260, 600);
Check(greatFireball.Count(p => p.Shape == SpellParticleShape.UserFlame) >= 35 &&
      greatImpact.Count(p => p.Shape == SpellParticleShape.UserFlame) >= 40 &&
      greatImpact.Count(p => p.Shape == SpellParticleShape.UserEmber) >= 10,
    "Great Fire Ball should use layered tiny combustion sprites in casting and detonation.");

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
    // Animated clip crossfades can produce one or two sprite frames per plume.
    // Compare origins, not clip-frame counts or rendering order.
    Check(driftedSmoke.Length >= 4 && releasedSmoke.Any(before =>
        driftedSmoke.Any(after => MathF.Abs(after.Origin.X - before.Origin.X) < 3f &&
            before.Origin.Y - after.Origin.Y > .8f &&
            before.Origin.Y - after.Origin.Y < 8f)),
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
