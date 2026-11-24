using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static partial class SpellVfxBladeParticles
{
    private const int FlameEmissionInterval = 28;
    private static readonly SpellParticleColourCurve FireCooling = new(
        (0, 0xFFF4C8), (.18f, 0xFFC04A), (.52f, 0xF34A14), (1, 0x731909));
    private static readonly SpellParticleColourCurve SmokeCooling = new(
        (0, 0xCE5030), (.4f, 0x8B301C), (1, 0x421E1A));

    internal static void EmitFlameMotion(SpellParticlePool pool, Vector2 feet, Vector2 facing,
        MirDirection direction, Func<int, WeaponMaskTrace?> weaponAt, long now, int elapsed,
        int interval, ref int lastElapsed, float scale, uint seed)
    {
        using var profile = SpellVfxAssets.For(Spell.FlamingSword);
        var wake = SpellVfxAssets.Current.Warrior.BladeMotion.Wake;
        long start = now - elapsed;
        int ignition = interval;
        int release = interval * 4;
        int end = interval * 6;
        int currentTick = Math.Min(elapsed, end - 1) / FlameEmissionInterval;
        scale /= Frame.ReferenceSize;
        for (int tick = Math.Max(lastElapsed / FlameEmissionInterval + 1, ignition / FlameEmissionInterval + 1); tick <= currentTick; tick++)
        {
            int time = tick * FlameEmissionInterval;
            int pose = time / interval;
            float inPose = time % interval / (float)interval;
            var guide = Guide.Read(wake[(int)direction * 6 + pose]);
            var random = new Frame(pool, feet, now, 1, Frame.ReferenceSize, seed ^ ((uint)tick * 747796405u));
            Vector2 centre = feet + guide.Centre * scale;
            Vector2 flow = guide.Axis;
            if (Vector2.Dot(flow, facing) > 0) flow = -flow;
            float spread = .17f + .83f * Frame.Ease((pose + inPose - 2.25f) / 1.65f);
            int wakeCount = pose <= 2 ? 2 : 7;
            for (int i = 0; i < wakeCount; i++)
            {
                float along = random.Unit(17 + i * 13) * 1.65f - .83f;
                float across = (random.Unit(18 + i * 13) - .5f) * guide.Width * 2 * spread;
                Vector2 origin = feet + guide.At(along, across) * scale;
                float angle = guide.Angle + (random.Unit(19 + i * 13) - .5f) * 1.4f;
                Vector2 drift = flow * (14 + random.Unit(20 + i * 13) * 67) - Vector2.UnitY * (8 + random.Unit(i + 63) * 35);
                EmitMoving(pool, origin, drift, start + time, now, 146 + (int)(random.Unit(i + 145) * 94),
                    (6 + random.Unit(i + 86) * 13) * scale, (.52f + random.Unit(i + 92) * .66f) * spread, angle,
                    (random.Unit(i + 302) - .5f) * 5.6f, .37f, 0xFF7629, .39f + random.Unit(i + 291) * .24f,
                    Shape.FlameSequence, Layer.Emissive, feet.Y, FireCooling, "Blade.Fire-wake", i + tick);
                if (pose > 2 && i % 2 == 0)
                    EmitMoving(pool, origin, drift * .55f - Vector2.UnitY * 21, start + time, now, 250 + i * 14,
                        (7 + random.Unit(i + 119) * 11) * scale, .9f, angle, (random.Unit(i + 202) - .5f) * 3,
                        .64f, 0x9B3A23, .42f, Shape.SmokeSequence, Layer.Smoke, feet.Y, SmokeCooling, "Blade.Smoke", i + tick);
            }

            if (weaponAt(pose) is { } blade)
            {
                WeaponMaskTrace prior = weaponAt(pose - 1) ?? blade;
                Vector2 hilt = Vector2.Lerp(prior.Hilt, blade.Hilt, inPose);
                Vector2 tip = Vector2.Lerp(prior.Tip, blade.Tip, inPose);
                Vector2 edge = tip - hilt;
                if (edge.LengthSquared() >= 9)
                {
                    float angle = MathF.Atan2(edge.Y, edge.X);
                    int trailCount = pose <= 2 ? 3 : 7;
                    for (int i = 0; i < trailCount; i++)
                    {
                        float along = .28f + random.Unit(i + 487) * .74f;
                        Vector2 origin = Vector2.Lerp(hilt, tip, along);
                        Vector2 lag = Vector2.Lerp(prior.Hilt - blade.Hilt, prior.Tip - blade.Tip, along) * 1.65f;
                        EmitMoving(pool, origin, lag - Vector2.UnitY * (10 + random.Unit(i + 131) * 25), start + time, now,
                            104 + (int)(random.Unit(i + 338) * 98), (3 + random.Unit(i + 306) * 7) * scale, .61f * (.36f + .64f * spread),
                            angle + (random.Unit(i + 208) - .5f) * .8f, (random.Unit(i + 397) - .5f) * 6,
                            .28f, 0xFFBA4B, .68f, Shape.FlameSequence, Layer.EmissiveCore, feet.Y,
                            FireCooling, "Blade.Blade-trail", i + tick);
                    }
                    centre = tip;
                }
            }

            int emberCount = pose <= 2 ? 2 : 9;
            for (int i = 0; i < emberCount; i++)
            {
                Vector2 scatter = Frame.Ellipse(random.Unit(i + 344) * MathF.Tau, 1, .7f);
                Vector2 drift = scatter * (20 + random.Unit(i + 345) * 96) + flow * 22 - Vector2.UnitY * 22;
                EmitMoving(pool, centre + scatter * (3 + random.Unit(i + 343) * 12), drift, start + time, now,
                    140 + (int)(random.Unit(i + 346) * 178), (2 + random.Unit(i + 347) * 2.3f) * scale, .65f,
                    MathF.Atan2(drift.Y, drift.X), (random.Unit(i + 348) - .5f) * 11, -.52f,
                    0xFFD05C, .68f, Shape.UserEmber, Layer.Emissive, feet.Y, FireCooling, "Blade.Embers", i + tick);
            }
        }

        if (elapsed >= release && lastElapsed < release)
        {
            var blade = weaponAt(4);
            EmitRearBurst(pool, feet, facing, blade, start + release, now, scale, seed);
            EmitCarriedBurst(pool, feet, facing, weaponAt(3), blade, start + release, now, scale, seed);
        }

        lastElapsed = Math.Max(lastElapsed, elapsed);
    }

    private static void EmitRearBurst(SpellParticlePool pool, Vector2 feet, Vector2 facing, WeaponMaskTrace? blade,
        long start, long now, float scale, uint seed)
    {
        var random = new Frame(pool, feet, now, 1, Frame.ReferenceSize, seed ^ 2097593u);
        Vector2 source = (blade?.Hilt ?? feet - Vector2.UnitY * 32) - facing * 9;
        Vector2 back = -facing;
        Vector2 side = new(-back.Y, back.X);
        for (int i = 0; i < 56; i++)
        {
            float cone = (random.Unit(i * 19 + 23) - .5f) * 1.8f;
            Vector2 heading = Vector2.Normalize(back + side * cone);
            Vector2 drift = heading * (112 + random.Unit(i * 19 + 24) * 166) - Vector2.UnitY * (9 + random.Unit(i * 19 + 25) * 37);
            bool flame = i < 36;
            EmitMoving(pool, source + heading * (random.Unit(i + 87) * 9), drift, start, now,
                165 + (int)(random.Unit(i + 88) * 150), (flame ? 6 + random.Unit(i + 89) * 12 : 2 + random.Unit(i + 89) * 3) * scale,
                flame ? .58f : .47f, MathF.Atan2(heading.Y, heading.X), (random.Unit(i + 91) - .5f) * 8,
                flame ? .62f : -.4f, flame ? 0xFF8B33 : 0xFFE08A, flame ? .73f : .8f,
                flame ? Shape.FlameSequence : Shape.UserEmber, Layer.Emissive, feet.Y, FireCooling,
                flame ? "Blade.Rear-flames" : "Blade.Rear-embers", i);
        }
    }

    private static void EmitCarriedBurst(SpellParticlePool pool, Vector2 feet, Vector2 facing, WeaponMaskTrace? previous,
        WeaponMaskTrace? current, long start, long now, float scale, uint seed)
    {
        if (current is not { } blade) return;
        Vector2 delta = blade.Tip - (previous?.Tip ?? blade.Hilt);
        Vector2 heading = delta.LengthSquared() > 4 ? Vector2.Normalize(delta) : facing;
        if (Vector2.Dot(heading, facing) < .2f) heading = Vector2.Normalize(heading * .4f + facing * .8f);
        Vector2 side = new(-heading.Y, heading.X);
        var random = new Frame(pool, feet, now, 1, Frame.ReferenceSize, seed ^ 981737u);
        for (int i = 0; i < 24; i++)
        {
            float along = .48f + random.Unit(i * 7 + 14) * .61f;
            float lateral = (random.Unit(i * 7 + 15) - .5f) * 2;
            Vector2 source = Vector2.Lerp(blade.Hilt, blade.Tip, along) + side * (lateral * 3);
            Vector2 drift = heading * (77 + random.Unit(i * 7 + 17) * 140) + side * (lateral * 42) - Vector2.UnitY * 12;
            EmitMoving(pool, source, drift, start, now, 136 + (int)(random.Unit(i * 7 + 19) * 119),
                (4 + random.Unit(i * 7 + 20) * 10) * scale, .4f + random.Unit(i * 7 + 21) * .22f,
                MathF.Atan2(heading.Y, heading.X) + lateral * .38f, lateral * 5, .47f,
                0xFFC158, .71f, Shape.FlameSequence, Layer.EmissiveCore, feet.Y, FireCooling, "Blade.Carry-flames", i);
        }
    }

    private static void EmitMoving(SpellParticlePool pool, Vector2 origin, Vector2 drift, long start, long now,
        int duration, float size, float aspect, float angle, float spin, float growth, int colour, float alpha,
        Shape shape, Layer layer, float depth, SpellParticleColourCurve gradient, string emitter, int index)
    {
        var motion = new SpellParticleMotion(drift, -drift * 1.8f - Vector2.UnitY * 18, 0, 0, 0, spin);
        SpellVfxAssets.Emit(pool, new SpellParticle(origin, Vector2.Zero, start, duration, Math.Max(1, (int)MathF.Round(size)),
            Frame.Tint(colour, alpha), shape, 0, aspect, angle, Growth: growth, Layer: layer, CastsLight: false,
            Frame: (byte)(index % SpellParticleAtlas.ClipFrames), DepthY: depth, Motion: motion, ColourCurve: gradient),
            emitter, sampleTime: now, randomKey: (uint)index * 27017);
    }
}
