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
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
static float Reach(SpellParticle[] particles) =>
    particles.Length == 0 ? 0 : particles.Max(p => MathF.Abs(p.Origin.X - 400));

foreach (Spell ward in new[] { Spell.SoulShield, Spell.BlessedArmour })
{
    var opening = Render(ward, SpellVisualPhase.Hit, 250);
    var formed = Render(ward, SpellVisualPhase.Hit, 700);
    Check(opening.Length > 0 && formed.Length > 0, $"Ward particles absent for {ward}");
    Check(formed.Any(p => p.Shape == (ward == Spell.SoulShield ? SpellParticleShape.SoulGlyph :
        SpellParticleShape.ArmourGlyph)), $"Ward glyph not formed for {ward}");
    Check(formed.Any(p => p.Layer == SpellParticleLayer.EmissiveCore), $"Ward lacks light for {ward}");
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
Check(shieldA.Length > 0 && shieldB.Length > 0 &&
    !shieldA.Select(p => p.Origin).SequenceEqual(shieldB.Select(p => p.Origin)),
    "Energy Shield aura must drift/pulse over time.");

foreach (Spell summon in new[] { Spell.SummonSkeleton, Spell.SummonHolyDeva, Spell.SummonShinsu })
    Check(Render(summon, SpellVisualPhase.Impact, 480).Any(p => p.Shape == SpellParticleShape.Spark),
        $"Summoning rite should produce flare particles: {summon}");

Check(Render(Spell.Curse, SpellVisualPhase.Hit, 850, 2000)
    .Any(p => p.Shape == SpellParticleShape.SmokeSequence),
    "Curse glyph should release smoke.");
Console.WriteLine("Taoist particle composition tests passed.");
