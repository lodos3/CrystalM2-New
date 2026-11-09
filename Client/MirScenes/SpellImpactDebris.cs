using System.Numerics;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static class SpellImpactDebris
{
    private readonly record struct Profile(Shape Parent, Shape Child, int Hot, int Cool, float Speed, float Lift, float Energy);
    private static SpellParticleColourCurve SoulEmber => SpellVfxAssets.Current.Debris.Palette.SoulEmber;

    internal static void Emit(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 heading,
        long start, int density, float gravity, float driftScale, uint seed)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Debris.Emit;
        if (density <= 0 || visual.Phase is not (SpellVisualPhase.Impact or SpellVisualPhase.Contact)) return;
        if (visual.Phase == SpellVisualPhase.Contact && visual.Spell is not (Spell.FlamingSword or Spell.TwinDrakeBlade)) return;
        Profile profile = visual.Spell switch
        {
            Spell.FlamingSword or Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.FireBang or
                Spell.MeteorShower or Spell.SoulFireBall or Spell.FlameDisruptor =>
                new(tuning.FlamingSwordShape, tuning.FlamingSwordShape2, tuning.FlamingSwordHot, tuning.FlamingSwordCool, tuning.FlamingSwordSpeed, tuning.FlamingSwordLift, tuning.FlamingSwordEnergy),
            Spell.ThunderBolt or Spell.TwinDrakeBlade =>
                new(tuning.ThunderBoltShape, tuning.ThunderBoltShape2, tuning.ThunderBoltHot, tuning.ThunderBoltCool, tuning.ThunderBoltSpeed, tuning.ThunderBoltLift, tuning.ThunderBoltEnergy),
            Spell.IceStorm or Spell.FrostCrunch =>
                new(tuning.IceStormShape, tuning.IceStormShape2, tuning.IceStormHot, tuning.IceStormCool, tuning.IceStormSpeed, tuning.IceStormLift, tuning.IceStormEnergy),
            Spell.Plague => new(tuning.PlagueShape, tuning.PlagueShape2, tuning.PlagueHot, tuning.PlagueCool, tuning.PlagueSpeed, tuning.PlagueLift, tuning.PlagueEnergy),
            _ => default
        };
        if (profile.Speed <= 0) return;
        var sample = new SpellParticleFrame(pool, feet, start, 1, ReferenceSize, seed);
        int count = Math.Clamp((int)MathF.Round(tuning.Count * density / tuning.CountDivisor), 1, 12);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + sample.Unit(i + 16)) * MathF.Tau / count;
            Vector2 ray = ProjectGround(Ellipse(angle, 1, 1));
            Vector2 root = feet + ray * (tuning.Root + tuning.Root2 * sample.Unit(i + 80));
            Vector2 drift = (ray + heading * tuning.Drift) * profile.Speed * driftScale * (tuning.Drift2 + tuning.Drift3 * sample.Unit(i + 43));
            float lift = profile.Lift * (tuning.Lift + tuning.Lift2 * sample.Unit(i + 69));
            var motion = new SpellParticleMotion(drift, -drift * tuning.Wind, tuning.Height + sample.Unit(i + 93) * tuning.Height2, lift, tuning.Gravity * gravity,
                (sample.Unit(i + 150) - tuning.Spin) * tuning.Spin2);
            long birth = start + i * tuning.Birth;
            var particle = new SpellParticle(root, Vector2.Zero, birth, motion.LandingMilliseconds + tuning.SpellParticleDuration,
                tuning.SpellParticleSize + (int)(sample.Unit(i + 170) * tuning.SpellParticleSize2), Tint(i % 3 == 0 ? profile.Hot : profile.Cool, tuning.Particle),
                profile.Parent, 0, Aspect: tuning.SpellParticleAspect, Rotation: angle, Growth: tuning.SpellParticleGrowth,
                Layer: i % 3 == 0 ? tuning.SpellParticleLayer : tuning.SpellParticleLayer2, CastsLight: i % 3 == 0, DepthY: root.Y,
                Motion: motion, Impact: new SpellParticleImpact(profile.Child, tuning.SpellParticleImpactCount, tuning.SpellParticleImpactSize, tuning.SpellParticleImpactDuration + i * tuning.SpellParticleImpactDuration2, tuning.SpellParticleImpactScatter * driftScale, Lights: i % 3 == 0),
                LightEnergy: profile.Energy, ColourCurve: visual.Spell == Spell.SoulFireBall ? SoulEmber : null,
                ColourCurveFullSpeed: tuning.SpellParticleColourCurveFullSpeed);
            pool.Schedule(particle);
        }
    }
}
