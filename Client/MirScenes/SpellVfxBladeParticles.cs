using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static partial class SpellVfxBladeParticles
{
    internal static void DrawAttack(SpellParticlePool pool, Spell spell, SpellVisualPhase phase, Vector2 feet,
        MirDirection direction, int pose, float progress, long now, float size, uint seed, WeaponMaskTrace? weapon)
    {
        var mask = new Frame(pool, feet, now, 1, size, seed, depthY: feet.Y);
        SpellVfxPoseMasks.Draw(mask, spell, feet, (int)direction, pose, .67f, phase == SpellVisualPhase.FollowThrough);
        Draw(pool, spell, phase, feet, direction, pose, progress, now, size, seed, weapon, .40f);
    }

    internal static void Draw(SpellParticlePool pool, Spell spell, SpellVisualPhase phase, Vector2 feet,
        MirDirection direction, int pose, float progress, long now, float size, uint seed, WeaponMaskTrace? weapon = null,
        float detailStrength = 1)
    {
        using var profile = SpellVfxAssets.For(spell);
        using var timeline = SpellVfxAssets.AtProgress(progress);
        var motion = SpellVfxAssets.Current.Warrior.BladeMotion;
        int index = (int)direction * 6 + pose;
        float strength = detailStrength * SpellVfxLegacyFrames.BladeStrength(spell, phase, index);
        var frame = new Frame(pool, feet, now, strength, size, seed ^ (uint)pose * 69069, depthY: feet.Y);

        switch (phase)
        {
            case SpellVisualPhase.Prepare:
                Activation(frame, feet, Guide.Read(motion.Activation[pose]), pose, progress);
                break;
            case SpellVisualPhase.Hit:
                TargetLoop(frame, feet, Guide.Read(motion.TargetLoop[pose]), pose, progress);
                break;
            case SpellVisualPhase.Swing:
            case SpellVisualPhase.FollowThrough:
                if (spell == Spell.FlamingSword)
                {
                    if (pose == 0)
                        frame.Sprite(feet + SpellVfxLegacyFrames.FlameIgnition[(int)direction], 10, 19,
                            Shape.Wisp, 0xCD632E, .48f, Layer.EmissiveCore, emitter: "Blade.Ignition");
                    FlameSweep(frame, feet, Guide.Read(motion.HotCore[index]), Guide.Read(motion.Wake[index]), pose, progress, weapon);
                }
                else
                {
                    ArcSweep(frame, feet, Guide.Read(motion.Sweep[index]), motion.SweepOutline[index], pose);
                    if (phase == SpellVisualPhase.FollowThrough)
                    {
                        var charge = new Frame(pool, feet, now, strength, size,
                            seed ^ ((uint)pose * 69069) ^ ((uint)(progress * 15) * 18311), depthY: feet.Y);
                        ChargedSweep(pool, charge, feet, Guide.Read(motion.ChargedSweep[index]), motion.BodyAxis[index],
                            direction, pose, progress);
                    }
                }
                break;
        }
    }

    private static void FlameSweep(Frame frame, Vector2 feet, Guide core, Guide wake, int pose, float progress, WeaponMaskTrace? weapon)
    {
        float phase = progress * 2.3f;
        float fade = pose == 0 ? .09f : 1 - .53f * Frame.Ease((progress - .79f) / .21f);
        float spread = .16f + .84f * Frame.Ease((progress - .22f) / .48f);
        if (pose >= 2)
        {
            // Diffuse fire and torn streamers spread well outside the incandescent blade.
            for (int i = 0; i < 13; i++)
            {
                float along = (i - 6) / 6f;
                float across = MathF.Sin(i * 2.4f + phase) * wake.Width * .85f;
                Vector2 point = feet + wake.At(along * 1.2f, across);
                float width = (38 + wake.Width * 1.3f) * (1 - .35f * MathF.Abs(along));
                frame.Sprite(point, width * 1.65f, width * 1.2f, Shape.Wisp, 0xB94314, .40f * fade,
                    Layer.EmissiveCore, wake.Angle, emitter: "Blade.Wake-glow");
                frame.Clip(point, width, width * .60f, Shape.FireSequence, phase * .36f + i * .19f,
                    i % 3 == 0 ? 0xC34814 : 0x7C260E, .42f * fade, Layer.EmissiveCore, wake.Angle + across * .018f,
                    emitter: "Blade.Wake-glow");
            }
        }
        if (pose is 1 or 2 && weapon is { } blade && (blade.Tip - blade.Hilt).LengthSquared() > 9)
        {
            Vector2 edge = blade.Tip - blade.Hilt;
            float angle = MathF.Atan2(edge.Y, edge.X);
            Vector2 from = blade.Hilt + edge * .28f;
            Ribbon(frame, from, blade.Tip, 1.65f, .62f, 0xFF9B36, fade * .7f, "Blade.Ignition");
            for (int i = 0; i < 8; i++)
            {
                Vector2 point = Vector2.Lerp(from, blade.Tip, (i + .5f) / 8);
                frame.Clip(point, 5 + frame.Unit(i + 35) * 5, 1.8f + frame.Unit(i + 70) * 2.2f,
                    Shape.FlameSequence, phase * 2 + i * .13f, i % 3 == 0 ? 0xFFD578 : 0xFF8727,
                    fade * .73f, Layer.EmissiveCore, angle, emitter: "Blade.Ignition");
            }
            return;
        }
        for (int i = 0; i < 9; i++)
        {
            float u = (i - 4) * .25f;
            float width = (12 + wake.Width * .65f) * (1 - .46f * MathF.Abs(u)) * spread;
            Vector2 point = feet + wake.At(u, (frame.Unit(i + 19) - .5f) * wake.Width * .72f * spread);
            frame.Sprite(point, width * 1.5f, width, Shape.Flame, i % 3 == 0 ? 0xEF4D17 : 0xA92C13,
                fade * .29f, Layer.Emissive, wake.Angle + .53f * MathF.Sin(phase + u * 2), emitter: "Blade.Wake-glow");
        }

        if (core.Length < 1) return;
        for (int i = 0; i < 9; i++)
        {
            float u = (i - 4) / 4.4f;
            float taper = 1 - MathF.Abs(u) * .62f;
            Vector2 point = feet + core.At(u, (frame.Unit(i + 317) - .5f) * core.Width * .85f);
            float pulse = .83f + .17f * MathF.Sin(progress * 18 + i * 1.31f);
            frame.Clip(point, (9 + core.Length * .39f) * taper * pulse, (5 + core.Width * 1.96f) * taper * pulse * spread,
                Shape.FireSequence, phase + i * .137f, i % 3 == 0 ? 0xFFDE73 : i % 3 == 1 ? 0xFF9A23 : 0xF55410,
                fade * taper * .76f, Layer.EmissiveCore, core.Angle + u * .31f + .23f * MathF.Sin(phase * 3 + i), light: i == 4,
                emitter: "Blade.Hot-core");
            if (i != 0)
            {
                Vector2 prior = feet + core.At(u - 1 / 4.4f, 0);
                Vector2 current = feet + core.At(u, 0);
                float thickness = (5 + core.Width * 1.3f) * taper * spread;
                Ribbon(frame, prior, current, thickness, thickness * .93f, 0xFF4F0C, fade * taper * .62f, "Blade.Heat-sheet");
                Ribbon(frame, prior, current, thickness * .43f, thickness * .4f, 0xFFAA29, fade * taper * .68f, "Blade.Heat-sheet");
            }
        }

    }

    private static void ArcSweep(Frame frame, Vector2 feet, Guide guide, float[][] outline, int pose)
    {
        int segments = outline.Length - 1;
        float opening = Vector2.Dot(guide.Centre + new Vector2(0, 39), guide.Axis) >= 0 ? 1 : -1;
        Vector2 axis = guide.Axis * opening;
        Vector2 normal = new(-axis.Y, axis.X);
        float fade = pose is 0 or 5 ? .63f : .95f;
        Span<Vector2> low = stackalloc Vector2[outline.Length];
        Span<Vector2> high = stackalloc Vector2[outline.Length];
        Span<Vector2> spine = stackalloc Vector2[outline.Length];
        for (int i = 0; i <= segments; i++)
        {
            // Longitudinal position and three cross-section offsets, in guide units.
            var row = outline[i];
            float bend = guide.Curve * opening * (row[0] * row[0] - .27f);
            Vector2 centre = feet + guide.Centre + axis * (row[0] * guide.Length) + normal * bend;
            low[i] = centre + normal * (row[1] * guide.Width);
            high[i] = centre + normal * (row[2] * guide.Width);
            spine[i] = centre + normal * (row[3] * guide.Width);
        }

        for (int i = 1; i <= segments; i++)
        {
            Vector2 uv = new((i - 1f) / segments, i / (float)segments);
            var veil = new SpriteQuad(low[i - 1], low[i], high[i - 1], high[i]);
            var glow = new SpriteQuad(Vector2.Lerp(spine[i - 1], low[i - 1], 1.55f), Vector2.Lerp(spine[i], low[i], 1.55f),
                Vector2.Lerp(spine[i - 1], high[i - 1], 1.55f), Vector2.Lerp(spine[i], high[i], 1.55f));
            frame.TexturedRibbon(glow, uv, Shape.DirectionalSweep, 0x719AD0, fade * .39f, "Blade.Sweep-soft-edge");
            frame.TexturedRibbon(veil, uv, Shape.DirectionalSweep, 0xA7C8EA, fade * .85f, "Blade.Sweep-filaments");
            float taper = MathF.Sin(MathF.PI * (i - .5f) / segments);
            frame.Thread(spine[i - 1], spine[i], 3.4f * taper, 2.5f * taper, 0xD6EEFF,
                fade * .59f * taper, Layer.EmissiveCore, emitter: "Blade.Sweep-rim");
        }

        if (pose == 4)
        {
            // The release folds over the forward lip and feathers back into the wind.
            float side = outline[7][2] - outline[7][3] > outline[7][3] - outline[7][1] ? 1 : -1;
            Vector2 inner = side > 0 ? high[7] : low[7];
            Vector2 lip = spine[^1];
            Vector2 bow = axis * 12 - normal * side * 2.5f;
            Vector2 previous = spine[^2];
            for (int i = 1; i <= 9; i++)
            {
                float t = i / 9f;
                Vector2 point = t <= .33f ? Vector2.Lerp(spine[^2], lip, t / .33f)
                    : BoltPoint(lip, inner - axis * 9, bow, (t - .33f) / .67f);
                frame.Thread(previous, point, 4.4f * (1 - t), 2.7f * (1 - t), 0xB7DCFF,
                    fade * .62f * (1 - t * .85f), Layer.Emissive, emitter: "Blade.Sweep-flick");
                previous = point;
            }
        }
    }
    private static void ChargedSweep(SpellParticlePool pool, Frame frame, Vector2 feet, Guide guide,
        ReadOnlySpan<float> bodyAxis, MirDirection direction, int pose, float progress)
    {
        float fade = 1 - progress * .57f;
        // The reference body bands follow the player pose, separately from the weapon arc.
        Vector2 torso = feet + new Vector2(bodyAxis[1], -39);
        Vector2 head = feet + new Vector2(bodyAxis[0], -65);
        Vector2 crown = feet + new Vector2(bodyAxis[0] + (frame.Unit(842) - .5f) * 18, -124 - 12 * frame.Unit(843));
        const int dischargePoses = 3;
        float poseTime = Math.Clamp(progress * 6 - pose, 0, 1);
        float dischargeTime = Math.Clamp((pose + poseTime) / dischargePoses, 0, 1);
        float rise = Frame.Ease(dischargeTime * 3);
        float dissipation = Frame.Ease((dischargeTime - .64f) / .36f);
        float bodyFade = 1 - dissipation;
        float topY = feet.Y - 20 + (crown.Y - feet.Y + 20) * rise;
        float bottomY = feet.Y + 2 - dissipation * 57;
        if (pose < 5)
        {
            float afterglow = 1 - Frame.Ease((progress - .5f) * 3);
            frame.Sprite(torso, 42, 110, Shape.Wisp, 0x72AEFF, afterglow, Layer.EmissiveCore, emitter: "Blade.Body-glow");
            frame.Sprite(torso, 24, 85, Shape.Wisp, 0xBEDFFF, afterglow, Layer.EmissiveCore, emitter: "Blade.Body-glow");
            if (pose == 0)
                Bolt(frame, head, crown, new Vector2(-3, 0), .66f * (1 - poseTime), 891, "Blade.Body-glow");
        }
        if (pose < dischargePoses)
            BodyDischarge(frame, feet, torso, head, crown, bodyAxis[2], bodyFade, topY, bottomY);

        float yaw = ((int)direction - 2) * (MathF.PI / 4);
        Vector2 facing = Frame.Ellipse(yaw, 1, .68f);
        Vector2 centre = feet + guide.Centre;
        float reachSign = Vector2.Dot(centre - torso, guide.Axis) >= 0 ? 1 : -1;
        Vector2 knot = feet + guide.At(reachSign * .83f, 0);
        if (pose == 4) knot = torso - facing * 34 - Vector2.UnitY * 12;
        knot -= Vector2.UnitY * 5;
        Vector2 overhead = Vector2.Lerp(head, crown, .37f);
        bool overheadActive = pose < dischargePoses && topY <= overhead.Y && bodyFade > .045f;
        Vector2 leadingEdge = Vector2.Lerp(torso + facing * 47 - Vector2.UnitY * 3, centre, .22f);
        Bolt(frame, torso - Vector2.UnitY * 5, knot, new Vector2(0, -12),
            fade * .88f, 920, "Blade.Branch-lightning", bright: true);
        Span<DischargeFork> forks = stackalloc DischargeFork[overheadActive ? 12 : 10];
        for (int i = 0; i < forks.Length; i++)
        {
            int spoke = !overheadActive && i >= 8 ? i + 2 : i;
            bool outer = spoke < 8;
            bool front = spoke >= 10;
            float brightness = outer || front ? fade : bodyFade * .58f;
            Vector2 source = outer ? knot : front ? leadingEdge : overhead + Vector2.UnitY * ((spoke - 8) * 12);
            int random = 1200 + spoke * 83;
            float angle = outer ? spoke * MathF.Tau / 8 : front ? yaw + (spoke == 10 ? -.48f : .36f)
                : (spoke == 8 ? -2.35f : -.68f);
            angle += (frame.Unit(random) - .5f) * .46f;
            Vector2 heading = Frame.Ellipse(angle, 1, front ? .59f : .81f);
            float radius = (outer ? 19 : front ? 13 : 12) + frame.Unit(random + 1) * (outer ? 37 : front ? 16 : 20);
            if (outer && pose is 0 or 1) radius *= .78f;
            Vector2 tip = source + heading * radius - Vector2.UnitY * (outer ? 7 : front ? 0 : 6);
            Vector2 bow = new(-heading.Y, heading.X);
            bow *= (spoke % 2 == 0 ? 1 : -1) * (6 + frame.Unit(random + 2) * 7);
            bow -= Vector2.UnitY * 7;
            string layer = outer ? "Blade.Branch-lightning" : front ? "Blade.Front-forks" : "Blade.Overhead-forks";
            Bolt(frame, source, tip, bow, brightness * (outer ? .77f : front ? .41f : .82f), random + 12, layer);
            Vector2 fork = BoltPoint(source, tip, bow, .41f + frame.Unit(random + 3) * .16f);
            float forkAngle = angle + (spoke % 2 == 0 ? 1 : -1) * (.6f + frame.Unit(random + 4) * .55f);
            Vector2 forkTip = fork + Frame.Ellipse(forkAngle, 1, .82f) * radius * .61f - Vector2.UnitY * 4;
            Vector2 forkBow = bow * -.52f - Vector2.UnitY * 3;
            Bolt(frame, fork, forkTip, forkBow, brightness * (outer ? .53f : .37f), random + 37, layer);
            forks[i] = new DischargeFork(source, tip, bow, fork, forkTip, forkBow, random, brightness);
        }

        // Reserve capacity for the bolt geometry before adding pinpoints.
        int sparksPerFork = Math.Clamp((pool.Capacity - pool.Active.Length - 96) / forks.Length, 0, 600 / forks.Length);
        for (int spoke = 0; spoke < forks.Length; spoke++)
        {
            var fork = forks[spoke];
            for (int i = 0; i < sparksPerFork; i++)
            {
                int index = fork.Seed + i * 59;
                float distance = frame.Unit(index + 7);
                Vector2 along = i % 3 == 0
                    ? BoltPoint(fork.Split, fork.SplitTip, fork.SplitBow, distance)
                    : BoltPoint(fork.Source, fork.Tip, fork.Bow, distance);
                float spread = 1.4f + distance * 5.3f + (i % 7 == 0 ? 7 : 0);
                Vector2 jitter = Frame.Ellipse(frame.Unit(index + 8) * MathF.Tau, 1, .85f) *
                    (frame.Unit(index + 9) * spread);
                float sparkSize = 1.25f + frame.Unit(index + 10) * 1.45f;
                float alpha = fork.Brightness * (1 - distance * .37f) * (.57f + frame.Unit(index + 11) * .42f);
                frame.Sprite(along + jitter, sparkSize, sparkSize, Shape.Flash,
                    i % 5 < 3 ? 0xF5FAFF : 0xADCBFF, alpha, Layer.EmissiveCore,
                    emitter: "Blade.Lightning-sparks");
            }
        }
    }

    private readonly record struct DischargeFork(Vector2 Source, Vector2 Tip, Vector2 Bow,
        Vector2 Split, Vector2 SplitTip, Vector2 SplitBow, int Seed, float Brightness);

    private static void BodyDischarge(Frame frame, Vector2 feet, Vector2 torso, Vector2 head, Vector2 crown,
        float hipX, float alpha, float topY, float bottomY)
    {
        if (alpha <= .01f || bottomY <= topY) return;
        Vector2 foot = feet + new Vector2(hipX * .66f - 2, 2);
        Vector2 hip = feet + new Vector2(hipX, -24);
        Vector2 neck = Vector2.Lerp(torso, head, .52f);
        Vector2 above = Vector2.Lerp(head, crown, .55f) + new Vector2(-5, 0);
        Span<Vector2> spine = stackalloc Vector2[] { foot, hip, torso, neck, head, above, crown };
        for (int section = 1; section < spine.Length; section++)
        {
            Vector2 from = spine[section - 1];
            Vector2 to = spine[section];
            int seed = 803 + section * 47;
            float level = alpha * (section > 4 ? .87f : 1);
            Vector2 bow = new((section % 2 == 0 ? 1 : -1) * (2 + frame.Unit(seed + 1) * 4), 0);
            Trace(from, to, bow, level, seed, "Blade.Body-lightning", bright: true);

            if (section == spine.Length - 1) continue;
            float side = section % 2 == 0 ? -1 : 1;
            Vector2 joint = Vector2.Lerp(from, to, .54f);
            Vector2 fork = joint + new Vector2(side * (13 + frame.Unit(seed + 4) * 16), -13 - frame.Unit(seed + 5) * 8);
            Trace(joint, fork, new Vector2(side * 4, -5), level * .67f, seed + 9, "Blade.Body-forks");
            Trace(Vector2.Lerp(joint, fork, .61f), fork + new Vector2(side * 7, -11),
                new Vector2(side * -4, -2), level * .42f, seed + 23, "Blade.Body-forks");
        }

        // Two short electrical braids cross the torso. The glow stays on the filaments.
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 root = hip + new Vector2(side * 5, 5);
            Vector2 crossing = torso + new Vector2(side * 2, 1);
            Vector2 top = neck - new Vector2(side * 4, 2);
            Trace(root, crossing, new Vector2(-side * 4, 1), alpha * .77f,
                1681 + side * 37, "Blade.Body-lightning", bright: true);
            Trace(crossing, top, new Vector2(side * 3, -2), alpha * .81f,
                1749 + side * 43, "Blade.Body-lightning", bright: true);
        }

        // The side arcs leave the hip and meet the shoulders again.
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 around = Vector2.Lerp(hip, neck, .53f) + new Vector2(side * 17, -1);
            Trace(hip, around, new Vector2(side * 8, 1), alpha * .66f, 1889 + side * 57, "Blade.Body-wrap");
            Trace(around, neck, new Vector2(side * 7, -6), alpha * .62f, 1964 + side * 61, "Blade.Body-wrap");
        }

        void Trace(Vector2 from, Vector2 to, Vector2 bow, float level, int seed, string layer, bool bright = false)
        {
            if (Math.Max(from.Y, to.Y) < topY || Math.Min(from.Y, to.Y) > bottomY) return;
            float deltaY = to.Y - from.Y;
            if (MathF.Abs(deltaY) > .01f)
            {
                Vector2 origin = from;
                from = Vector2.Lerp(origin, to, (Math.Clamp(from.Y, topY, bottomY) - origin.Y) / deltaY);
                to = Vector2.Lerp(origin, to, (Math.Clamp(to.Y, topY, bottomY) - origin.Y) / deltaY);
            }
            Bolt(frame, from, to, bow, level, seed, layer, bright);
        }
    }

    private static void Activation(Frame frame, Vector2 feet, Guide guide, int pose, float progress)
    {
        float gather = Frame.Ease(progress * 1.55f);
        Vector2 centre = feet + new Vector2(guide.Centre.X, -72 + (guide.Centre.Y + 56) * .61f);
        float radius = 25 * (1 - gather) + 6;
        float burst = Frame.Ease(progress * 3.9f) * (1 - .56f * Frame.Ease((progress - .57f) / .43f));
        frame.Sprite(centre, 69 + radius, 80 + radius * 1.24f, Shape.Orb, 0x226DE0, burst * .79f,
            Layer.Emissive, light: true, emitter: "Blade.Activation-halo");

        for (int i = 0; i < 4; i++)
        {
            float angle = i * MathF.Tau / 4 + progress * 2;
            Vector2 point = centre + Frame.Ellipse(angle, radius, radius * .78f);
            float alpha = i == 0 ? .89f : .64f * (1 - gather * .62f);
            frame.Sprite(point, 9 + gather * 7, 14 + gather * 7, Shape.Orb, 0x77B9FF, alpha,
                Layer.Emissive, emitter: "Blade.Activation-orbs");
            frame.Sprite(point, 4 + gather * 4, 7 + gather * 4, Shape.Flash, 0xC7EBFF, alpha,
                Layer.EmissiveCore, emitter: "Blade.Activation-orbs");
        }

        for (int i = 0; i < 36; i++)
        {
            float angle = i * MathF.Tau / 36 + frame.Unit(i + 80) * .17f;
            Vector2 radial = Frame.Ellipse(angle, 1, 1);
            float length = (31 + frame.Unit(i + 210) * 28) * (1 - .37f * gather);
            Vector2 start = centre + radial * (4 + radius * .24f);
            Vector2 end = centre + radial * length;
            frame.Thread(start, end, 2.8f, .32f, i % 4 == 0 ? 0xA6D7FF : 0x4C97F5,
                burst * (.56f + frame.Unit(i + 387) * .43f), Layer.Emissive, emitter: "Blade.Activation-rays");
        }

        if (pose > 0)
        {
            Vector2 core = centre - new Vector2(0, 5);
            frame.Sprite(core, 18 + gather * 11, 22 + gather * 15, Shape.Flash, 0xE5F7FF, burst * .98f,
                Layer.EmissiveCore, emitter: "Blade.Activation-core");
            Lightning(frame, core, feet + new Vector2(1, -8), 12, 0xA6D7FF,
                burst * .98f, 57, "Blade.Activation-discharge");
            Lightning(frame, core + new Vector2(-7, 5), feet + new Vector2(-16, -5), 10, 0x74BAFF,
                burst * .83f, 192, "Blade.Activation-discharge");
        }
    }

    private static void TargetLoop(Frame frame, Vector2 feet, Guide guide, int pose, float progress)
    {
        Vector2 centre = feet + guide.Centre + new Vector2(0, 6);
        float fade = (.59f + .36f * MathF.Sin(MathF.PI * progress)) * (1 - .42f * Frame.Ease((progress - .78f) / .22f));
        for (int band = 0; band < 2; band++)
        {
            float rotation = pose * .49f + band * .67f;
            Vector2 previous = Loop(0);
            for (int i = 1; i <= 9; i++)
            {
                Vector2 point = Loop(i);
                Lightning(frame, previous, point, 2.6f, band == 0 ? 0xAACBFF : 0x6496E9,
                    fade * (band == 0 ? .78f : .49f), i * 17 + band * 89, "Blade.Target-arcs");
                previous = point;
            }

            Vector2 Loop(int i) => centre + Frame.Ellipse(rotation + i * MathF.Tau / 9,
                16 + band * 5, 12 + band * 3) + new Vector2(0, band * 9 + (frame.Unit(i % 9 + band * 21) - .5f) * 6);
        }
        frame.Sprite(centre + new Vector2(0, 9), 44, 27, Shape.Halo, 0x4D87DF, fade * .33f,
            Layer.Emissive, emitter: "Blade.Target-glow");
    }

    private static void Ribbon(Frame frame, Vector2 from, Vector2 to, float firstWidth, float lastWidth,
        int colour, float alpha, string layer)
    {
        Vector2 delta = to - from;
        if (delta.LengthSquared() < .5f) return;
        Vector2 tangent = Vector2.Normalize(delta);
        Vector2 normal = new(-tangent.Y, tangent.X);
        from -= tangent * .7f;
        to += tangent * .7f;
        frame.WindRibbon(new SpriteQuad(from - normal * firstWidth, to - normal * lastWidth,
            from + normal * firstWidth, to + normal * lastWidth), colour, alpha, layer);
    }

    private static Vector2 BoltPoint(Vector2 from, Vector2 to, Vector2 bow, float t) =>
        Vector2.Lerp(from, to, t) + bow * (4 * t * (1 - t));

    private static void Bolt(Frame frame, Vector2 from, Vector2 to, Vector2 bow, float alpha, int seed, string layer,
        bool bright = false)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length < 1) return;

        Vector2 normal = new Vector2(-delta.Y, delta.X) / length;
        int count = Math.Clamp((int)MathF.Ceiling(length / 4.6f), 4, 19);
        bool body = bright && layer == "Blade.Body-lightning";
        Vector2 previous = from;
        for (int i = 1; i <= count; i++)
        {
            float t = i / (float)count;
            float crackle = (frame.Unit(seed + i * 17) - .5f) * (bright ? 6.9f : 7.4f) +
                (i % 2 == 0 ? 1.15f : -1.15f);
            Vector2 point = BoltPoint(from, to, bow, t) + normal * (crackle * MathF.Sin(t * MathF.PI));
            float falloff = 1 - t * (body ? .42f : .68f);
            float voltage = alpha * falloff;
            frame.Thread(previous, point, (body ? 8.8f : bright ? 6f : 4.2f) * falloff,
                (body ? 7 : 3.1f) * falloff, body ? 0x287BFF : 0x337EEA,
                voltage * (body ? .74f : .42f), Layer.Emissive, emitter: layer);
            if (body)
                frame.Thread(previous, point, 4.9f * falloff, 3.5f * falloff, 0x7CB9FF,
                    voltage * .84f, Layer.EmissiveCore, emitter: layer);
            float width = body ? 2.8f : bright ? 2.45f : 1.65f;
            frame.Thread(previous, point, width * falloff, width * .74f * falloff,
                body ? 0xFAFDFF : bright ? 0xD5ECFF : 0xAFCFFF, voltage, Layer.EmissiveCore, emitter: layer);
            previous = point;
        }
    }

    private static void Lightning(Frame frame, Vector2 from, Vector2 to, float jitter, int colour, float alpha, int seed, string layer)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length < 1) return;
        Vector2 side = new Vector2(-delta.Y, delta.X) / length;
        int segments = Math.Clamp((int)(length / 7), 2, 10);
        Vector2 previous = from;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = from + delta * t + side * ((frame.Unit(seed + i * 13) - .5f) * jitter * MathF.Sin(t * MathF.PI));
            frame.Thread(previous, point, 2.1f - t * .83f, 1.02f, colour,
                alpha * (1 - .29f * t), Layer.Emissive, emitter: layer);
            previous = point;
        }
    }

    // Feet-relative guide: centre X/Y, half length, cross spread, axis angle and curvature.
    private readonly record struct Guide(Vector2 Centre, float Length, float Width, float Angle, float Curve)
    {
        public Vector2 Axis => new(MathF.Cos(Angle), MathF.Sin(Angle));
        public Vector2 Normal => new(-MathF.Sin(Angle), MathF.Cos(Angle));
        public Vector2 At(float u, float cross) => Centre + Axis * (u * Length) + Normal * (cross + Curve * (u * u - .27f));

        public static Guide Read(float[] values) =>
            new(new Vector2(values[0], values[1]), values[2], values[3], values[4], values[5]);
    }
}
