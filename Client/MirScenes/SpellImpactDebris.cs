using System.Numerics;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static class SpellImpactDebris
{
    private readonly record struct Profile(Shape Parent, Shape Child, int Hot, int Cool, float Speed, float Lift, float Energy);
    private static readonly SpellParticleColourCurve SoulEmber = new(
        (0, 0x87251b), (.32f, 0xcc361d), (.66f, 0xff8545), (1, 0xfff9de));

    internal static void Emit(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 heading,
        long start, int density, float gravity, float driftScale, uint seed)
    {
        if (density <= 0 || visual.Phase is not (SpellVisualPhase.Impact or SpellVisualPhase.Contact)) return;
        if (visual.Phase == SpellVisualPhase.Contact && visual.Spell is not (Spell.FlamingSword or Spell.TwinDrakeBlade)) return;
        Profile profile = visual.Spell switch
        {
            Spell.FlamingSword or Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.FireBang or
                Spell.MeteorShower or Spell.SoulFireBall or Spell.FlameDisruptor =>
                new(Shape.UserEmber, Shape.Spark, 0xffda8a, 0xc24e24, 102, 145, .42f),
            Spell.ThunderBolt or Spell.TwinDrakeBlade =>
                new(Shape.Spark, Shape.Flash, 0xeaf6ff, 0x5281eb, 140, 112, .34f),
            Spell.IceStorm or Spell.FrostCrunch =>
                new(Shape.FrostCore, Shape.Wisp, 0xdeefff, 0x709dc6, 83, 87, .16f),
            Spell.Plague => new(Shape.UserEmber, Shape.Wisp, 0x9de9ff, 0x314cbf, 74, 129, .22f),
            _ => default
        };
        if (profile.Speed <= 0) return;
        var sample = new SpellParticleFrame(pool, feet, start, 1, ReferenceSize, seed);
        int count = Math.Clamp((int)MathF.Round(7 * density / 100f), 1, 12);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + sample.Unit(i + 16)) * MathF.Tau / count;
            Vector2 ray = ProjectGround(Ellipse(angle, 1, 1));
            Vector2 root = feet + ray * (4 + 13 * sample.Unit(i + 80));
            Vector2 drift = (ray + heading * .21f) * profile.Speed * driftScale * (.32f + .68f * sample.Unit(i + 43));
            float lift = profile.Lift * (.61f + .51f * sample.Unit(i + 69));
            var motion = new SpellParticleMotion(drift, -drift * .62f, 5 + sample.Unit(i + 93) * 9, lift, 520 * gravity,
                (sample.Unit(i + 150) - .5f) * 12);
            long birth = start + i * 17;
            var particle = new SpellParticle(root, Vector2.Zero, birth, motion.LandingMilliseconds + 100,
                4 + (int)(sample.Unit(i + 170) * 8), Tint(i % 3 == 0 ? profile.Hot : profile.Cool, .89f),
                profile.Parent, 0, Aspect: 1.28f, Rotation: angle, Growth: -.26f,
                Layer: i % 3 == 0 ? Layer.EmissiveCore : Layer.Emissive, CastsLight: i % 3 == 0, DepthY: root.Y,
                Motion: motion, Impact: new SpellParticleImpact(profile.Child, 2, 8, 165 + i * 7, 43 * driftScale, Lights: i % 3 == 0),
                LightEnergy: profile.Energy, ColourCurve: visual.Spell == Spell.SoulFireBall ? SoulEmber : null,
                ColourCurveFullSpeed: 175);
            pool.Schedule(particle);
        }
    }
}
