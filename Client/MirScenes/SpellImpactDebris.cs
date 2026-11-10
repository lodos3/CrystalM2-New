using System.Numerics;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static class SpellImpactDebris
{
    private readonly record struct Profile(Shape Parent, Shape Child, int Hot, int Cool, float Speed, float Lift, float Energy);
    private static SpellParticleColourCurve SoulEmber => SpellVfxAssets.Observe(SpellVfxAssets.Current.Debris.Palette).SoulEmber;

    internal static void Emit(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 heading,
        long start, int density, float gravity, float driftScale, uint seed)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Debris.Emit;
        var fields = SpellVfxAssets.Fields("Debris", "Emit");
        if (density <= 0 || visual.Phase is not (SpellVisualPhase.Impact or SpellVisualPhase.Contact)) return;
        if (visual.Phase == SpellVisualPhase.Contact && visual.Spell is not (Spell.FlamingSword or Spell.TwinDrakeBlade)) return;
        Profile profile = visual.Spell switch
        {
            Spell.FlamingSword or Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.FireBang or
                Spell.MeteorShower or Spell.SoulFireBall or Spell.FlameDisruptor =>
                new(fields.Read(tuning.FlamingSwordShape), fields.Read(tuning.FlamingSwordShape2), fields.Read(tuning.FlamingSwordHot), fields.Read(tuning.FlamingSwordCool), fields.Read(tuning.FlamingSwordSpeed), fields.Read(tuning.FlamingSwordLift), fields.Read(tuning.FlamingSwordEnergy)),
            Spell.ThunderBolt or Spell.TwinDrakeBlade =>
                new(fields.Read(tuning.ThunderBoltShape), fields.Read(tuning.ThunderBoltShape2), fields.Read(tuning.ThunderBoltHot), fields.Read(tuning.ThunderBoltCool), fields.Read(tuning.ThunderBoltSpeed), fields.Read(tuning.ThunderBoltLift), fields.Read(tuning.ThunderBoltEnergy)),
            Spell.IceStorm or Spell.FrostCrunch =>
                new(fields.Read(tuning.IceStormShape), fields.Read(tuning.IceStormShape2), fields.Read(tuning.IceStormHot), fields.Read(tuning.IceStormCool), fields.Read(tuning.IceStormSpeed), fields.Read(tuning.IceStormLift), fields.Read(tuning.IceStormEnergy)),
            Spell.Plague => new(fields.Read(tuning.PlagueShape), fields.Read(tuning.PlagueShape2), fields.Read(tuning.PlagueHot), fields.Read(tuning.PlagueCool), fields.Read(tuning.PlagueSpeed), fields.Read(tuning.PlagueLift), fields.Read(tuning.PlagueEnergy)),
            _ => default
        };
        if (profile.Speed <= 0) return;
        var sample = new SpellParticleFrame(pool, feet, start, 1, ReferenceSize, seed);
        int count = Math.Clamp((int)MathF.Round(fields.Read(tuning.Count) * density / fields.Read(tuning.CountDivisor)), 1, 12);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + sample.Unit(i + 16)) * MathF.Tau / count;
            Vector2 ray = ProjectGround(Ellipse(angle, 1, 1));
            Vector2 root = feet + ray * (fields.Read(tuning.Root) + fields.Read(tuning.Root2) * sample.Unit(i + 80));
            Vector2 drift = (ray + heading * fields.Read(tuning.Drift)) * profile.Speed * driftScale * (fields.Read(tuning.Drift2) + fields.Read(tuning.Drift3) * sample.Unit(i + 43));
            float lift = profile.Lift * (fields.Read(tuning.Lift) + fields.Read(tuning.Lift2) * sample.Unit(i + 69));
            var motion = new SpellParticleMotion(drift, -drift * fields.Read(tuning.Wind), fields.Read(tuning.Height) + sample.Unit(i + 93) * fields.Read(tuning.Height2), lift, fields.Read(tuning.Gravity) * gravity,
                (sample.Unit(i + 150) - fields.Read(tuning.Spin)) * fields.Read(tuning.Spin2));
            long birth = start + i * fields.Read(tuning.Birth);
            var particle = new SpellParticle(root, Vector2.Zero, birth, motion.LandingMilliseconds + fields.Read(tuning.SpellParticleDuration),
                fields.Read(tuning.SpellParticleSize) + (int)(sample.Unit(i + 170) * fields.Read(tuning.SpellParticleSize2)), Tint(i % 3 == 0 ? profile.Hot : profile.Cool, fields.Read(tuning.Particle)),
                profile.Parent, 0, Aspect: fields.Read(tuning.SpellParticleAspect), Rotation: angle, Growth: fields.Read(tuning.SpellParticleGrowth),
                Layer: i % 3 == 0 ? fields.Read(tuning.SpellParticleLayer) : fields.Read(tuning.SpellParticleLayer2), CastsLight: i % 3 == 0, DepthY: root.Y,
                Motion: motion, Impact: new SpellParticleImpact(profile.Child, fields.Read(tuning.SpellParticleImpactCount), fields.Read(tuning.SpellParticleImpactSize), fields.Read(tuning.SpellParticleImpactDuration) + i * fields.Read(tuning.SpellParticleImpactDuration2), fields.Read(tuning.SpellParticleImpactScatter) * driftScale, Lights: i % 3 == 0),
                LightEnergy: profile.Energy, ColourCurve: visual.Spell == Spell.SoulFireBall ? SoulEmber : null,
                ColourCurveFullSpeed: fields.Read(tuning.SpellParticleColourCurveFullSpeed));
            pool.Schedule(particle);
        }
    }
}
