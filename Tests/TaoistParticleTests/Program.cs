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
    // Authored sprite Size is stored before the renderer's 1.28 scale.
    Check(formed.Where(p => p.Shape == glyph).Any(p => p.Dimensions(0f, 1.28f).X >=
        (ward == Spell.SoulShield ? 360 : 320)), $"Ward glyph is too small for {ward}");
    Check(!formed.Any(p => p.Shape is SpellParticleShape.Halo or SpellParticleShape.Orb or
        SpellParticleShape.SmokeSequence), $"Ward has an unwanted oval/background effect: {ward}");
    Check(!formed.Any(p => p.Shape == SpellParticleShape.Wisp &&
        p.Dimensions(0f, 1.28f).X > 70), $"Ward has an oversized backdrop wisp: {ward}");
    Check(formed.Any(p => p.Shape == SpellParticleShape.FlameTongue && p.Emissive) &&
          burning.Any(p => p.Shape == SpellParticleShape.FlameTongue && p.Emissive),
        $"Ward needs animated flames following its strokes for {ward}");
    Check(formed.Any(p => p.Shape == SpellParticleShape.Spark && p.Emissive),
        $"Ward needs sparks emitted from its stroke lines: {ward}");
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
    Check(earlier.Length >= 8 && earlier.Length == later.Length,
        $"Protective rune {ward} needs several independent upward flames.");
    Check(earlier.Zip(later).All(pair => MathF.Abs(pair.First.Origin.X - pair.Second.Origin.X) < 8),
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

var curseAtAim = Render(Spell.Curse, SpellVisualPhase.Impact, 850, 2000);
Check(curseAtAim.Any(p => p.Shape == SpellParticleShape.CurseGlyph),
    "Curse must render a complete rune on the aimed map cell.");
Check(curseAtAim.Any(p => p.Shape == SpellParticleShape.SmokeSequence),
    "Curse glyph should release smoke.");
Console.WriteLine("Taoist particle composition tests passed.");
