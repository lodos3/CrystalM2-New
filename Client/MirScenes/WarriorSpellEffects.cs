using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static partial class WarriorSpellEffects
{
    private const int Steel = 0x719cff, Ice = 0x8dcaff, White = 0xeef8ff;
    private const int Flame = 0xff6b27, Ember = 0xed3516, Heat = 0xffdc8b;
    private const int Gold = 0xffcb61, PaleGold = 0xfff3b8, Smoke = 0xa49f93;

    internal static bool HasArt(Spell spell) => SpellCurriculum.Owns(MirClass.Warrior, spell);
    internal static bool UsesBodyMask(Spell spell) => spell is Spell.Rage or Spell.ImmortalSkin or Spell.ProtectionField or Spell.Fury;

    internal static int PacketDuration(SpellVisual visual) => visual.Phase switch
    {
        SpellVisualPhase.Prepare => 500,
        SpellVisualPhase.Burn => 800,
        SpellVisualPhase.Ward => 580,
        _ when visual.Spell == Spell.FlamingSword => 640,
        _ when visual.Spell == Spell.TwinDrakeBlade => 800,
        _ => visual.Spell is Spell.Fencing or Spell.ShoulderDash ? 270 : 390
    };

    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = ReferenceSize, uint seed = 0, Vector2? weaponTip = null)
    {
        if (duration <= 0 || now < start || now - start >= duration) return;
        float progress = (now - start) / (float)duration;
        if (visual is { Spell: Spell.Thrusting, Phase: SpellVisualPhase.Swing })
        {
            // Keep the source and reach in world pixels as the artwork size is adjusted.
            float strength = (.73f + .27f * Ease(progress * 4)) * (1 - Ease((progress - .84f) / .16f));
            var wind = new Frame(pool, feet, now, strength, size, seed, scalePositions: false);
            ThrustWind(wind, feet, facing, progress, visual.ExtraReach);
            return;
        }
        bool aura = visual.Phase == SpellVisualPhase.Aura;
        float cycle = aura ? now % duration / (float)duration : progress;
        var frame = new Frame(pool, feet, now, aura ? 1 : Envelope(progress), size, seed ^ (uint)visual.Spell * 34763);

        switch (visual.Phase)
        {
            case SpellVisualPhase.Contact:
                if (visual.Spell == Spell.FlamingSword) Engulf(frame, feet, progress, (now - start) / 1000f);
                else if (visual.Spell == Spell.TwinDrakeBlade) DrakeContact(frame, feet, progress, (now - start) / 1000f);
                else if (visual.Spell == Spell.Entrapment) Snare(frame, feet, progress, preparing: false);
                else Strike(frame, visual.Spell, feet, facing, progress);
                return;
            case SpellVisualPhase.Burn:
                BladeFlame(frame, feet - Vector2.UnitY * 21, progress, 52);
                return;
            case SpellVisualPhase.Ward:
                Skin(frame, feet, cycle, aura: true);
                return;
        }

        switch (visual.Spell)
        {
            case Spell.FlamingSword when visual.Phase == SpellVisualPhase.Prepare:
                Vector2 tip = weaponTip ?? feet + facing * 20 - Vector2.UnitY * 38;
                var blade = new Frame(pool, tip, now, Envelope(progress), size, seed, depthY: feet.Y);
                BladeFlame(blade, tip, progress, 47);
                break;
            case Spell.FlamingSword:
                Engulf(frame, feet, progress, (now - start) / 1000f);
                break;
            case Spell.TwinDrakeBlade when visual.Phase == SpellVisualPhase.Prepare:
                Vector2 crest = feet - Vector2.UnitY * (77 + 9 * Ease(progress * 2));
                frame.Sprite(crest, 108, 79, Shape.Orb, 0x2939a4, .67f, light: true);
                frame.Sprite(crest, 43, 47, Shape.Flash, Ice, .89f, Layer.EmissiveCore);
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 edge = feet + new Vector2(i * 33, -22 - Math.Abs(i) * 19);
                    WizardSpellEffects.Channel(frame, crest, edge, 13, 2, .73f, (now - start) / 1000f,
                        1, 317 + i * 67, colours: WizardSpellEffects.DrakeColours);
                }
                frame.Sparks(crest, 12, 45, 41, progress, Ice, 14);
                break;
            case Spell.TwinDrakeBlade:
                DrakeContact(frame, feet, progress, (now - start) / 1000f);
                break;
            case Spell.BladeAvalanche when visual.Phase == SpellVisualPhase.Swing:
                Avalanche(frame, feet, facing, progress);
                break;
            case Spell.SlashingBurst when visual.Phase == SpellVisualPhase.Swing:
                Burst(frame, feet, facing, progress);
                break;
            case Spell.CounterAttack when visual.Phase == SpellVisualPhase.Swing:
                Counter(frame, feet, facing, progress);
                break;
            case Spell.Entrapment:
                Snare(frame, feet, progress, visual.Phase == SpellVisualPhase.Prepare);
                break;
            case Spell.LionRoar:
            case Spell.BattleCry:
                Roar(frame, feet, progress, visual.Spell == Spell.BattleCry);
                break;
            case Spell.ProtectionField:
            case Spell.Rage:
                Surge(frame, feet, progress, visual.Spell == Spell.Rage);
                break;
            case Spell.Fury:
                Fury(frame, feet, cycle, aura);
                break;
            case Spell.ImmortalSkin:
                Skin(frame, feet, cycle, aura);
                break;
            default:
                Strike(frame, visual.Spell, feet, facing, progress);
                break;
        }
    }

    private static void Engulf(Frame frame, Vector2 feet, float progress, float time)
    {
        float ignition = .60f + .40f * Ease(progress * 5);
        for (int i = 0; i < 5; i++)
        {
            float angle = (i + frame.Unit(i + 13) * .37f) * MathF.Tau / 5;
            Vector2 root = feet + Ellipse(angle, 26, 15);
            float height = (74 + frame.Unit(i + 6) * 49) * ignition;
            WizardSpellEffects.Flames(frame, root, 22 + frame.Unit(i) * 11, height, time, .94f, i, Depth(angle));
        }
        frame.Clip(feet - Vector2.UnitY * 68, 87, 116, Shape.SmokeSequence, .15f + time * .48f,
            0x5d3126, .48f, Layer.Smoke);
        frame.Sparks(feet - Vector2.UnitY * 22, 17, 51, 30, progress, Heat, rise: 84, shape: Shape.WarriorSpark);
    }

    private static void DrakeContact(Frame frame, Vector2 feet, float progress, float time)
    {
        Vector2 body = feet - Vector2.UnitY * 38;
        frame.Sprite(body, 109, 118, Shape.Orb, 0x1b307e, .72f, light: true);
        frame.Sprite(feet, 73, 47, Shape.Orb, 0x425fdc, .53f, Layer.Ground, light: true);
        for (int i = -1; i <= 1; i += 2)
        {
            Vector2 end = body + new Vector2(i * (24 + progress * 29), -35 - progress * 18);
            WizardSpellEffects.Channel(frame, body + new Vector2(i * 4, 8), end, 19, 2.65f, .84f, time,
                3, i + 5, colours: WizardSpellEffects.DrakeColours);
            Vector2 ground = feet + new Vector2(i * (14 + progress * 10), i * 4);
            WizardSpellEffects.Channel(frame, body + new Vector2(i * 12, -16), ground, 11, 1.55f, .71f,
                time + .07f, 1, i + 38, colours: WizardSpellEffects.DrakeColours);
        }
        frame.Sprite(body, 34, 39, Shape.WarriorSpark, White, .86f * (1 - Ease(progress)), Layer.EmissiveCore, light: true);
        frame.Sparks(body, 21, 57, 46, progress, Ice, 14);
    }

    private static void Strike(Frame frame, Spell spell, Vector2 feet, Vector2 facing, float progress)
    {
        bool hot = spell is Spell.SlashingBurst or Spell.CounterAttack;
        bool heavy = spell is Spell.TwinDrakeBlade or Spell.BladeAvalanche or Spell.CrossHalfMoon or Spell.SlashingBurst;
        bool dash = spell == Spell.ShoulderDash;
        bool fencing = spell == Spell.Fencing;
        int colour = hot ? Flame : dash ? Gold : fencing ? 0xa9bfce : Steel;
        int core = hot || dash ? Heat : White;
        float radius = fencing ? 21 : heavy ? 75 : 43;
        float angle = MathF.Atan2(facing.Y, facing.X);
        Vector2 centre = feet - Vector2.UnitY * (dash ? 15 : 36);
        float bloom = .70f + .32f * Ease(progress * 4.5f);
        frame.Sprite(centre, radius * bloom, radius * bloom * .82f, Shape.WarriorImpact, colour, .83f,
            rotation: angle - .46f, light: true);
        frame.Sprite(centre, radius * .59f, radius * .77f, Shape.WarriorSpark, core, 1 - Ease(progress * 2.5f),
            Layer.EmissiveCore, angle + .33f, light: true);
        if (!fencing)
            frame.Sprite(centre, radius * 1.46f, radius * .97f, Shape.Orb, hot ? 0x933623 : dash ? 0x826c55 : 0x2849a2,
                .50f * bloom, Layer.Glow, angle, light: true);
        if (heavy)
            frame.Sprite(centre, radius * 1.2f, radius * .75f, Shape.Flash, colour, .45f * (1 - progress), light: true);
        if (dash || spell == Spell.BladeAvalanche)
            frame.Sprite(feet, 65 * bloom, 29 * bloom, Shape.WarriorDust, Smoke, .59f, Layer.Ground, angle);
        frame.Sparks(centre, fencing ? 3 : heavy ? 10 : 5, radius * .67f, radius * .43f,
            progress, hot ? Heat : White, rise: 17, shape: Shape.WarriorSpark);
    }

    internal static void Dash(SpellParticlePool pool, Vector2 from, Vector2 to, long now)
    {
        Vector2 delta = to - from;
        if (delta.LengthSquared() < 1) return;
        Vector2 forward = Vector2.Normalize(delta);
        var side = new Vector2(-forward.Y, forward.X);
        float angle = MathF.Atan2(delta.Y, delta.X);
        for (int i = 0; i < 4; i++)
        {
            float edge = (i % 2 == 0 ? -1 : 1) * (6 + i * 2);
            Vector2 foot = from + delta * (i / 4f) + side * edge;
            pool.TryAdd(new SpellParticle(foot, side * edge - forward * 9, now, 220 + i * 22, 44 + i * 3,
                Tint(0xb5a184, .54f), Shape.WarriorDust, 0, .58f, angle, .46f, Layer.Ground, CastsLight: false));
            pool.TryAdd(new SpellParticle(foot - Vector2.UnitY * (13 + i * 7), -forward * 24, now, 148, 38 + i * 5,
                Tint(i % 2 == 0 ? 0xe7e6d1 : 0x9daab1, .47f), Shape.BladeEdge, 0, .27f, angle + (i - 2) * .085f,
                Layer: Layer.World, CastsLight: false, DepthY: foot.Y));
        }
    }

    // Attack areas use the same 48 × 32 projection as the tiles they sweep through.
    private static Vector2 GroundDirection(Vector2 facing) =>
        facing / MathF.Max(MathF.Abs(facing.X), MathF.Abs(facing.Y) * 1.5f);

    private static void ThrustWind(Frame frame, Vector2 feet, Vector2 facing, float progress, int extraReach)
    {
        Vector2 source = feet - Vector2.UnitY * 36;
        Vector2 heading = GroundHeading(facing);
        var direction = (MirDirection)(((int)MathF.Round(MathF.Atan2(heading.Y, heading.X) * 4 / MathF.PI) + 2) & 7);
        float width = direction switch
        {
            MirDirection.Up or MirDirection.DownRight => 13,
            MirDirection.Down => 22,
            MirDirection.UpRight => 27,
            _ => 39
        };
        bool edgeOn = direction is MirDirection.Up or MirDirection.DownRight;
        float unfold = Ease(progress * 1.35f);
        float spread = width * (.24f + .76f * unfold);
        Vector2 travel = (GroundDirection(facing) * ((2 + extraReach) * 48) +
            new Vector2(0, facing.Y * 17)) * (.19f + .81f * unfold);
        Vector2 axis = Vector2.Normalize(travel);
        var side = new Vector2(-axis.Y, axis.X);
        Vector2 front = source + travel;

        // The first three legacy frames describe the weapon backswing. The next
        // three open a compact curl into a foreshortened, feathered bow of air.
        for (int lane = -1; lane <= 1; lane++)
        {
            float sway = MathF.Sin(progress * 10 - lane * 1.2f) * (2 + unfold * 4);
            float splay = lane * spread;
            Strand(source + side * (lane * 2),
                source + travel * .27f + side * (splay * .38f + sway),
                source + travel * .64f + side * (splay * .27f - sway * .61f),
                source + travel * .85f + side * (splay * .34f),
                6.8f + 3.1f * unfold, lane == 0 ? Ice : Steel, .50f - .19f * unfold, 9);
        }

        float bank = spread * (.09f + .09f * MathF.Sin(progress * 5.3f));
        Vector2 crown = front - side * bank;
        Vector2 opening = side * spread;
        if (edgeOn)
        {
            // Those two views look along the curl. Pitch the leading edge into the
            // flight path so its bright rim reads as a long wisp in profile.
            crown -= travel * .22f;
            opening += travel * (direction == MirDirection.Up ? .30f : -.30f);
        }
        Vector2 curlStart = crown - axis * (spread * .77f) - opening;
        Vector2 curlUpper = crown + axis * (spread * .31f) - opening * .71f;
        Vector2 curlLower = crown + axis * (spread * .21f) + opening * .73f;
        Vector2 curlEnd = crown - axis * (spread * .65f) + opening;
        Strand(curlStart, curlUpper, curlLower, curlEnd, 17 + unfold * 9, 0x3159a9, .68f, 15);
        Strand(curlStart, curlUpper, curlLower, curlEnd, 6.5f + unfold * 4, 0xb8deff, .76f, 15);

        // The curved wisps ride the pressure front. Their trailing ends peel inward
        // at different angles, leaving open air between the bands.
        for (int i = 0; i < 8; i++)
        {
            float t = .08f + i * .12f + .016f * MathF.Sin(progress * 11 - i * .82f);
            var (crest, _) = Cubic(curlStart, curlUpper, curlLower, curlEnd, t);
            float length = (12 + spread * .67f) * (.72f + .27f * MathF.Sin(t * MathF.PI));
            float roll = (t - .48f) * (9 + spread * .48f) - bank * .61f;
            Vector2 tail = crest - axis * length + side * roll;
            Strand(tail, tail + axis * (length * .72f) + side * (roll * .22f),
                crest - axis * (length * .43f) - side * (roll * .62f), crest - axis * 2,
                4.2f + unfold * 2.8f, i % 3 == 0 ? White : i % 3 == 1 ? 0xb8dfff : 0x84aefa, .70f, 7);
        }
        frame.Sprite(source + travel * .86f, 24 + spread * .84f, 16 + spread * .67f,
            Shape.Wisp, Ice, .43f, rotation: MathF.Atan2(axis.Y, axis.X), light: true);

        void Strand(Vector2 start, Vector2 pull, Vector2 bend, Vector2 end, float thickness, int colour, float alpha, int segments)
        {
            var (inner, outer) = Edge(0);
            for (int i = 1; i <= segments; i++)
            {
                var (nextInner, nextOuter) = Edge(i / (float)segments);
                frame.WindRibbon(new SpriteQuad(inner, nextInner, outer, nextOuter), colour, alpha);
                (inner, outer) = (nextInner, nextOuter);
            }

            (Vector2 Inner, Vector2 Outer) Edge(float t)
            {
                var (centre, tangent) = Cubic(start, pull, bend, end, t);
                float half = thickness * frame.Scale * (.045f + .455f * MathF.Sin(t * MathF.PI));
                Vector2 offset = new Vector2(-tangent.Y, tangent.X) * (half / MathF.Max(.01f, tangent.Length()));
                return (centre - offset, centre + offset);
            }
        }

        static (Vector2 Point, Vector2 Tangent) Cubic(Vector2 start, Vector2 pull, Vector2 bend, Vector2 end, float t)
        {
            Vector2 a = Vector2.Lerp(start, pull, t);
            Vector2 b = Vector2.Lerp(pull, bend, t);
            Vector2 c = Vector2.Lerp(bend, end, t);
            Vector2 before = Vector2.Lerp(a, b, t);
            Vector2 after = Vector2.Lerp(b, c, t);
            return (Vector2.Lerp(before, after, t), after - before);
        }
    }

    private static void Avalanche(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        Vector2 heading = GroundHeading(facing);
        heading /= MathF.Max(MathF.Abs(heading.X), MathF.Abs(heading.Y));
        var side = new Vector2(-heading.Y, heading.X);
        float angle = MathF.Atan2(facing.Y, facing.X);
        float turn = progress * MathF.Tau * 1.47f;
        Vector2 hand = feet - Vector2.UnitY * 36 + ProjectGround(heading * 13 + side * (MathF.Sin(turn) * 32));
        float windup = (1 - Ease((progress - .33f) * 9.1f));
        frame.Sprite(hand, 68, 47, Shape.WarriorTripleCut, Ice, .78f * windup,
            rotation: angle + turn, light: true);
        frame.Sprite(hand, 47, 30, Shape.Flash, White, .65f * windup, Layer.EmissiveCore);

        // Frame six of the original has a broad release at the player before
        // the thin ground lanes fan out and carry the spikes farther forward.
        float release = Ease((progress - .24f) / .16f) * (1 - Ease((progress - .51f) / .21f));
        for (int i = 0; i < 14; i++)
        {
            float direction = (frame.Unit(i + 23) - .5f) * 2.4f;
            Vector2 ray = ProjectGround(heading * (49 + frame.Unit(i + 43) * 122) + side * (direction * 42));
            Vector2 centre = feet + ray * .41f - Vector2.UnitY * (23 + frame.Unit(i + 57) * 39);
            frame.Sprite(centre, ray.Length() * .9f, 11 + frame.Unit(i) * 17, Shape.Wisp,
                i % 4 == 0 ? Ice : 0x2e51a6, .88f * release, Layer.Glow, MathF.Atan2(ray.Y, ray.X) - direction * .13f);
            frame.Thread(centre - ray * .52f, centre + ray * .48f, 2.8f, .46f,
                i % 3 == 0 ? White : Ice, .93f * release, Layer.EmissiveCore);
        }

        for (int lane = -1; lane <= 1; lane++)
        for (int step = 0; step < 5; step++)
        {
            float age = progress - (.37f + step * .066f + Math.Abs(lane) * .032f);
            if (age < 0 || age >= .42f) continue;
            int index = step + (lane + 1) * 5;
            float rise = Ease(age * 14);
            float fade = 1 - Ease((age - .15f) / .27f);
            float opacity = (.54f + .46f * rise) * fade;
            float stride = 19 + step * 31 + 11 * Ease(age * 4.1f);
            float breadth = lane * (6 + step * 11);
            Vector2 root = feet + ProjectGround(heading * stride + side * breadth);
            Vector2 next = root + ProjectGround(heading * 34 + side * (lane * 12));
            frame.Thread(root - ProjectGround(heading * 26), next, 3.2f + step * .41f, .56f,
                lane == 0 ? Ice : Steel, .83f * opacity, Layer.Ground, light: lane == 0, sortY: root.Y);
            frame.Sprite(root, 43 + step * 4, 24 + step * 3, Shape.WarriorDust, 0xaebfd1, .73f * opacity,
                Layer.Ground, angle, sortY: root.Y);
            frame.Sprite(root, 42 + step * 9, 25 + step * 4, Shape.Orb, 0x4359a0, .45f * opacity,
                Layer.Ground, angle, sortY: root.Y);

            for (int blade = -1; blade <= 1; blade++)
            {
                Vector2 foot = root + ProjectGround(side * (blade * (5 + step * 1.1f)) - heading * Math.Abs(blade) * 7);
                float height = (34 + step * 9 + frame.Unit(index * 3 + blade + 17) * 23 - Math.Abs(blade) * 11) * rise;
                Vector2 tip = foot + ProjectGround(heading * (height * .56f)) - Vector2.UnitY * height;
                Vector2 edge = tip - foot;
                float tilt = MathF.Atan2(edge.Y, edge.X) + MathF.PI / 2;
                float span = edge.Length();
                frame.Sprite((foot + tip) * .5f, 16 + step * 3, span, Shape.IceSpire, blade == 0 ? Ice : Steel,
                    .94f * opacity, rotation: tilt, light: blade == 0, sortY: foot.Y);
                frame.Sprite(Vector2.Lerp(foot, tip, .44f), 8 + step, span * .83f, Shape.IceSpire, White,
                    .85f * opacity, Layer.EmissiveCore, tilt, sortY: foot.Y);
                if (blade == 0)
                    frame.Sprite(tip + new Vector2(4, 2), 10 + step, 14, Shape.WarriorSpark, 0xacb6ff, .81f * opacity,
                        Layer.EmissiveCore, tilt + .2f, sortY: foot.Y);
            }
        }
    }

    private static void Burst(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        Vector2 forward = GroundDirection(facing);
        var side = new Vector2(-facing.Y, facing.X);
        float angle = MathF.Atan2(facing.Y, facing.X);
        Vector2 launch = feet - Vector2.UnitY * 28;
        Vector2 tip = launch + forward * (63 + 130 * Ease(progress * 1.65f));
        Vector2 centre = Vector2.Lerp(launch, tip, .55f);
        float span = Vector2.Distance(launch, tip);
        frame.Sprite(centre, span + 67, 87, Shape.Orb, Ember, .55f, rotation: angle, light: true);
        frame.Clip(centre - side * 12, span + 42, 72, Shape.SmokeSequence, .17f + progress * .76f,
            0x7d422b, .53f, Layer.Smoke, angle);
        frame.Clip(centre, 86, span + 42, Shape.FlameSequence, progress * 1.1f, Flame,
            .91f, rotation: angle + MathF.PI / 2, light: true);
        frame.Sprite(centre - side * 5, span + 37, 49, Shape.BladeEdge, Heat, .84f, Layer.EmissiveCore, angle - .05f);
        frame.Sprite(tip - forward * 17, 97, 77, Shape.WarriorImpact, Flame, .78f, rotation: angle + .28f, light: true);
        for (int i = 0; i < 6; i++)
        {
            float lift = (frame.Unit(i + 18) - .5f) * 71;
            Vector2 tail = Vector2.Lerp(launch, tip, frame.Unit(i) * .58f) + side * lift;
            frame.Line(tail, tip + side * (lift * .19f), 6 + frame.Unit(i + 34) * 11, Heat, .54f);
        }
        frame.Sparks(tip - forward * 28, 11, 95, 37, progress, Heat, rise: 21, shape: Shape.WarriorSpark);
    }

    private static void Counter(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        // The original two-frame flash occupies the first fifth of the ten-frame riposte.
        float flash = 1 - Ease(progress * 5);
        Vector2 body = feet - Vector2.UnitY * 64;
        frame.Sprite(body, 128, 198, Shape.FlameTongue, Ember, .86f * flash, light: true);
        frame.Sprite(body, 79, 142, Shape.WarriorImpact, Heat, .93f * flash, Layer.EmissiveCore, -.3f);
        frame.Clip(body + Vector2.UnitY * 20, 82, 156, Shape.FlameSequence, progress * 3.1f, Flame, flash * .91f);
        // The sword trace takes over through the swing. Carry a little heat between the flash and cut.
        float trail = Ease((progress - .17f) / .29f);
        float afterglow = (.18f + .63f * flash) * (1 - Ease((progress - .60f) / .40f));
        Vector2 edge = feet + facing * (42 + progress * 19) - Vector2.UnitY * 37;
        float rotation = MathF.Atan2(facing.Y, facing.X);
        frame.Sprite(body, 57, 117, Shape.Wisp, Ember, afterglow, Layer.Glow, light: true);
        frame.Clip(edge - facing * 16, 142, 49, Shape.FireSequence, progress * 1.8f, Ember, .77f * trail, rotation: rotation);
        frame.Sprite(edge, 112, 76, Shape.WarriorCrescent, Flame, .88f * trail,
            rotation: rotation - .44f, light: true);
        frame.Sparks(edge, 8, 76 * trail, 23, progress, Heat, 19, Shape.WarriorSpark);
    }

    private static void Snare(Frame frame, Vector2 feet, float progress, bool preparing)
    {
        float sweep = progress * MathF.Tau * (preparing ? .75f : 1.35f);
        float extent = .67f + .33f * Ease(progress * 3.8f);
        float release = preparing ? 1 : 1 - Ease((progress - .61f) / .19f);
        frame.Sprite(feet, 202 * extent, 112 * extent, Shape.Wisp, 0xb4c4ca, .53f, Layer.Floor, light: true);
        for (int i = 0; i < 10; i++)
        {
            float angle = sweep + i * MathF.Tau / 10;
            float edge = extent * (.62f + frame.Unit(i + 14) * .38f);
            Vector2 ground = feet + Ellipse(angle, 76, 42) * edge;
            float tangent = MathF.Atan2(MathF.Cos(angle) * 35, -MathF.Sin(angle) * 63);
            frame.Clip(ground - Vector2.UnitY * 4, 81, 39, Shape.SmokeSequence, progress + i * .17f,
                i % 2 == 0 ? 0xe9edf0 : 0xaebbc2, .84f * release, Layer.Floor, tangent);
            if (preparing) continue;
            float bind = 1 - Ease((progress - .49f) / .41f);
            float climb = Fract(progress * 1.31f + i * .13f);
            Vector2 point = feet + Ellipse(angle, 29 - climb * 19, 12) - Vector2.UnitY * (9 + climb * 104);
            Layer layer = MathF.Sin(angle) < 0 ? Layer.Ground : Layer.Smoke;
            frame.Clip(point, 66 - climb * 34, 43 - climb * 11, Shape.SmokeSequence, progress + i * .13f,
                i % 2 == 0 ? 0xf3f3ee : 0xb3c1c7, .92f * bind * (1 - climb * .44f), layer, tangent * .37f);
            frame.Sprite(point + Vector2.UnitY * 13, 36 - climb * 12, 49 - climb * 14, Shape.Wisp,
                0xcdd7d9, .53f * bind, layer, tangent * .46f);
        }
    }

    private static void Roar(Frame frame, Vector2 feet, float progress, bool rally)
    {
        int tint = rally ? 0xdabf91 : 0xb3c2d0;
        float spread = .31f + .69f * Ease(progress * 2.6f);
        Vector2 head = feet - Vector2.UnitY * 55;
        frame.Sprite(feet, 252 * spread, 138 * spread, Shape.WarriorRing, tint, .34f, Layer.Floor, light: true);
        frame.Sprite(head, 110 * spread, 112 * spread, Shape.WarriorImpact, tint, .83f, light: true);
        frame.Sprite(head, 162 * spread, 144 * spread, Shape.Wisp, rally ? 0xb4823d : 0x41607c, .56f, Layer.Glow);
        frame.Sprite(head, 60, 78, Shape.Flash, White, .97f * (1 - progress * .77f), Layer.EmissiveCore);
        for (int i = 0; i < 18; i++)
        {
            float angle = (i + frame.Unit(i) * .86f) * MathF.Tau / 18;
            Vector2 ray = Ellipse(angle, 136, 128) * (spread * (.49f + frame.Unit(i + 24) * .54f));
            Vector2 edge = head + ray;
            Vector2 bend = head + ray * .59f + Ellipse(angle + 1.57f, 8, 6) * (frame.Unit(i + 67) - .45f);
            Layer layer = Depth(angle);
            frame.Sprite(bend, ray.Length() * .91f, 17 + frame.Unit(i + 44) * 14,
                Shape.Wisp, tint, .64f, layer, MathF.Atan2(ray.Y, ray.X));
            frame.Thread(head + ray * .16f, bend, 1.1f, 2.3f, i % 3 == 0 ? White : tint, .87f, layer);
            frame.Thread(bend, edge, 2.3f, .33f, White, .84f, layer == Layer.Ground ? layer : Layer.EmissiveCore);
            if (i % 2 == 0)
                frame.Sprite(edge, 9 + frame.Unit(i) * 7, 12, Shape.Spark, White, .79f, layer, angle);
        }
    }

    private static void Surge(Frame frame, Vector2 feet, float progress, bool rage)
    {
        Vector2 centre = feet - Vector2.UnitY * (rage ? 47 : 40);
        int tint = rage ? Ember : Steel;
        int core = rage ? Heat : White;
        float spread = .43f + .57f * Ease(progress * 3);
        frame.Sprite(centre, (rage ? 251 : 191) * spread, (rage ? 96 : 138) * spread, Shape.Orb, tint,
            .72f, light: true);
        frame.Sprite(centre, 54 * spread, 89 * spread, Shape.Flash, core, .88f, Layer.EmissiveCore, light: true);
        for (int i = 0; i < 22; i++)
        {
            float angle = (i + frame.Unit(i) * .83f) * MathF.Tau / 22;
            float length = spread * (.54f + frame.Unit(i + 33) * .54f);
            Vector2 ray = Ellipse(angle, rage ? 165 : 127, rage ? 68 : 95) * length;
            frame.Sprite(centre + ray * .5f, ray.Length() + 12, 10 + frame.Unit(i + 11) * 14,
                Shape.BladeEdge, i % 3 == 0 ? core : tint, .66f, Depth(angle), MathF.Atan2(ray.Y, ray.X));
        }
        frame.Sparks(centre, 8, rage ? 86 : 72, 50, progress, core, rise: 15, shape: Shape.WarriorSpark);
    }

    private static void BladeFlame(Frame frame, Vector2 point, float progress, float width)
    {
        frame.Clip(point - Vector2.UnitY * 9, width, width * 1.32f, Shape.FireSequence, progress * 1.4f, Flame, .94f, light: true);
        frame.Clip(point, width * .70f, width * 1.11f, Shape.FireSequence, progress * 1.4f + .24f, Heat, .73f, Layer.EmissiveCore);
        frame.Sparks(point, 6, width * .65f, width * .41f, progress, Heat, rise: 30, shape: Shape.WarriorSpark);
    }

    private static void Fury(Frame frame, Vector2 feet, float progress, bool aura)
    {
        if (aura)
        {
            Vector2 eye = feet - Vector2.UnitY * 86;
            frame.Sprite(eye, 51, 47, Shape.WarriorRing, Flame, .83f, light: true);
            frame.Sprite(eye, 39, 23, Shape.SigilEye, Ember, .79f + .16f * MathF.Sin(progress * MathF.Tau), light: true);
            frame.Sprite(eye, 19, 16, Shape.WarriorSpark, Heat, .84f, Layer.EmissiveCore);
            frame.Clip(eye - Vector2.UnitY * 4, 54, 34, Shape.FireSequence, progress, Flame, .48f);
            return;
        }

        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 plume = feet + new Vector2(side * 27, -47);
            frame.Clip(plume, 75, 161, Shape.FlameSequence, progress + (side + 1) * .14f, Ember,
                .94f, side < 0 ? Layer.Ground : Layer.Emissive, side * .16f, light: true);
            frame.Clip(plume + new Vector2(-side * 6, 13), 46, 101, Shape.FireSequence, progress + .32f, Heat,
                .83f, Layer.EmissiveCore, side * .16f);
        }
        frame.Sparks(feet - Vector2.UnitY * 25, 9, 64, 40, progress, Heat, rise: 51, shape: Shape.WarriorSpark);
    }

    private static void Skin(Frame frame, Vector2 feet, float progress, bool aura)
    {
        Vector2 body = feet - Vector2.UnitY * 39;
        if (aura)
        {
            frame.Sprite(feet, 71, 34, Shape.WarriorRing, 0xc7cd7b, .48f, Layer.Ground);
            frame.Sprite(body, 48, 93, Shape.Orb, 0x98d55c, .48f, Layer.Ground, light: true);
            for (int i = -2; i <= 2; i++)
            {
                float wave = MathF.Sin((progress + i * .19f) * MathF.Tau);
                Vector2 rise = body + new Vector2(i * 16, -8 - Math.Abs(i) * 2);
                float tilt = i * .115f + wave * .10f;
                float height = 115 - Math.Abs(i) * 11;
                frame.Sprite(rise, 29, height, Shape.FlameTongue, i % 2 == 0 ? Gold : 0xacd572,
                    .65f + wave * .10f, i % 2 == 0 ? Layer.Ground : Layer.Emissive, tilt, light: true);
                frame.Sprite(rise + Vector2.UnitY * 7, 11, height * .73f, Shape.FlameTongue, PaleGold,
                    .64f + wave * .14f, Layer.EmissiveCore, tilt);
            }
            return;
        }

        Vector2 overhead = feet - Vector2.UnitY * (136 - 38 * Ease(progress * 2));
        frame.Sprite(overhead, 137, 101, Shape.WarriorImpact, Gold, .90f, rotation: .59f, light: true);
        frame.Sprite(overhead, 52, 62, Shape.Flash, PaleGold, .88f, Layer.EmissiveCore, light: true);
        for (int i = 0; i < 9; i++)
        {
            float spread = (i - 4) * 20;
            Vector2 end = feet + new Vector2(spread, -26 - frame.Unit(i) * 25);
            frame.Line(overhead + new Vector2(spread * .32f, 11), end, 16 + frame.Unit(i + 15) * 12,
                i % 3 == 0 ? PaleGold : Gold, .62f, i % 2 == 0 ? Layer.Ground : Layer.Emissive);
        }
        frame.Sprite(feet, 116, 67, Shape.WarriorRing, Gold, .71f, Layer.Ground);
    }
}
