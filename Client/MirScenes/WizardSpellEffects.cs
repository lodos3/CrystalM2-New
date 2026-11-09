using System.Numerics;
using Frame = Client.MirScenes.SpellParticleFrame;
using static Client.MirScenes.SpellParticleFrame;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static partial class WizardSpellEffects
{
    internal const float DefaultSize = SpellParticleFrame.ReferenceSize;
    internal const int LightningDuration = 600;
    private const float FloorAspect = 2f / 3f;
    private const int Gold = 0xffad28, Fire = 0xff6316, Ember = 0xd63208, Hot = 0xfff1b3;
    private const int Blue = 0x6c9fff, PaleBlue = 0xb8d7ff, White = 0xf5faff;
    private const int Ice = 0x68bbf1, IceWhite = 0xe0f9ff;

    internal static bool HasArt(Spell spell) => SpellCurriculum.Owns(MirClass.Wizard, spell);

    internal static int PacketDuration(SpellVisual visual) => visual.Phase switch
    {
        SpellVisualPhase.Contact => 320,
        SpellVisualPhase.Ward => 580,
        SpellVisualPhase.Burn => 800,
        _ => visual.Spell == Spell.IceThrust ? 1000 : 600
    };

    // The legacy offsets use the upper left of a 48 × 32 tile. A live caster supplies the
    // interpolated position of that same sprite origin. Artwork here is authored in world pixels.
    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = DefaultSize, uint seed = 0, uint field = 0)
    {
        if (duration <= 0 || now < start || now - start >= duration) return;
        float progress = (now - start) / (float)duration;
        float time = (now - start) / 1000f;
        float strength = visual.Phase switch
        {
            SpellVisualPhase.Aura or SpellVisualPhase.Field or SpellVisualPhase.Hit => 1,
            SpellVisualPhase.Prepare => .67f + .33f * Ease(progress * 5.2f),
            _ when visual.Spell == Spell.ThunderBolt => Envelope(progress, .79f, .34f),
            _ when visual.Spell is Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or
                Spell.ElectricShock or Spell.FlameField => Envelope(progress, .78f, .08f),
            _ => Envelope(progress)
        };
        var frame = new Frame(pool, feet, now, strength, size, seed ^ (uint)visual.Spell * 17191, field);
        bool prepare = visual.Phase == SpellVisualPhase.Prepare;

        if (visual.Phase == SpellVisualPhase.Contact)
        {
            if (visual.Spell is Spell.IceThrust or Spell.IceStorm)
            {
                // Ice Storm's contact must retain the runic seal and swirling
                // wind/ice; SnowContact is the separate Ice Thrust impact.
                if (visual.Spell == Spell.IceStorm)
                    IceStorm(frame, feet, progress, prepare: false);
                else
                    SnowContact(frame, feet, progress, true);
                return;
            }
            bool cold = visual.Spell == Spell.ThunderStorm;
            frame.Sprite(feet - Vector2.UnitY * 37, 35, 44, Shape.Flash, cold ? IceWhite : Hot, .74f, Layer.EmissiveCore, light: true);
            frame.Sparks(feet - Vector2.UnitY * 23, 5, 26, 21, progress, cold ? Ice : Fire, rise: 12);
            return;
        }
        if (visual.Phase == SpellVisualPhase.Burn)
        {
            Flames(frame, feet, 25, 53, time, .67f);
            return;
        }
        if (visual.Phase == SpellVisualPhase.Ward && visual.Spell != Spell.MagicShield)
        {
            Shield(frame, feet, SpellVisualPhase.Hit, progress, now);
            return;
        }

        switch (visual.Spell)
        {
            case Spell.FireBall:
                if (prepare) FireCharge(frame, feet, progress, facing);
                else FireBurst(frame, feet + new Vector2(4, -37), progress, 91);
                break;
            case Spell.GreatFireBall:
            case Spell.FireBounce:
            case Spell.MeteorShower:
                int accent = visual.Spell == Spell.FireBounce ? 0xff7f42 : visual.Spell == Spell.MeteorShower ? 0xffe19b : Gold;
                if (prepare) GreatFireCharge(frame, feet, progress, accent);
                else SearingImpact(frame, feet - Vector2.UnitY * 44, progress, accent);
                break;
            case Spell.Repulsion:
                Repel(frame, feet, progress);
                break;
            case Spell.ElectricShock:
                ElectricWrap(frame, feet, progress, time, prepare);
                break;
            case Spell.HellFire:
                if (prepare) HellfireCharge(frame, feet, progress);
                else FireBurst(frame, feet + new Vector2(5, -38), progress, 99, fierce: true);
                break;
            case Spell.ThunderBolt:
                ThunderStrike(frame, feet, progress, time, prepare);
                break;
            case Spell.Teleport:
                if (prepare) BlinkRing(frame, feet, progress, descending: false);
                else TeleportLight(frame, feet, progress, visual.Phase == SpellVisualPhase.Departure);
                break;
            case Spell.FireBang:
                FireBang(frame, feet, progress, time, prepare);
                break;
            case Spell.FireWall:
                if (prepare) FireWallCharge(frame, feet, progress);
                else FireWall(frame, feet, time);
                break;
            case Spell.Lightning:
                Lightning(frame, feet - Vector2.UnitY * 43, feet - Vector2.UnitY * 43 + facing * 288,
                    time, new(feet.Y, feet.Y + facing.Y * 288));
                break;
            case Spell.FrostCrunch:
                if (prepare) FrostCharge(frame, feet, progress);
                else FrostFireImpact(frame, feet - Vector2.UnitY * 43, progress);
                break;
            case Spell.ThunderStorm:
                ThunderStorm(frame, feet, progress, time);
                break;
            case Spell.MagicShield:
                Shield(frame, feet, visual.Phase, progress, now);
                break;
            case Spell.TurnUndead:
                TurnUndead(frame, feet, progress, prepare);
                break;
            case Spell.Vampirism:
                Vampirism(frame, feet, visual.Phase, progress);
                break;
            case Spell.IceStorm:
                IceStorm(frame, feet, progress, prepare);
                break;
            case Spell.FlameDisruptor:
                Disruptor(frame, feet, progress, time, prepare);
                break;
            case Spell.Mirroring:
                Mirroring(frame, feet, visual.Phase, progress);
                break;
            case Spell.FlameField:
                FlameField(frame, feet, progress, time);
                break;
            case Spell.Blizzard:
                if (prepare) IceBreak(frame, feet - Vector2.UnitY * 41, progress, 27, 6);
                else Blizzard(frame, feet, time);
                break;
            case Spell.MagicBooster:
                Booster(frame, feet, visual.Phase, progress, now);
                break;
            case Spell.MeteorStrike:
                if (prepare) MeteorCharge(frame, feet, progress);
                else if (visual.Phase == SpellVisualPhase.Field) MeteorStrike(frame, feet, time);
                else MeteorSeal(frame, feet, progress);
                break;
            case Spell.IceThrust:
                IceThrust(frame, feet, facing, progress, visual.ExtraReach);
                break;
            case Spell.StormEscape:
                if (prepare) StormEscape(frame, feet, progress, time);
                else StormArrival(frame, feet, progress, time);
                break;
            case Spell.Blink:
                BlinkRing(frame, feet, progress, descending: !prepare);
                break;
        }
    }

    internal static bool DrawProjectile(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frameIndex, int remaining, bool wake, float size)
    {
        if (spell is not (Spell.FireBall or Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower or Spell.FrostCrunch)) return false;
        if (remaining <= 0) return true;

        var frame = new Frame(pool, head, now, 1, size, (uint)spell, depthY: head.Y + 47);
        float angle = MathF.Atan2(heading.Y, heading.X);
        Vector2 side = new(-heading.Y, heading.X);
        bool frost = spell == Spell.FrostCrunch;
        int rim = frost ? 0x4674d5 : spell == Spell.FireBounce ? 0xec421f : Fire;
        int mantle = frost ? 0x8cdaff : spell == Spell.MeteorShower ? 0xffdb77 : Gold;
        int core = frost ? IceWhite : Hot;
        float phase = frameIndex * .091f;

        // The projectile core is made of individual directional flame tongues,
        // never a single 100px orb. Frost Crunch uses the same cold-fire flow.
        for (int i = 0; i < 22; i++)
        {
            int seed = i * 37 + 67;
            float age = Fract(frameIndex * (.11f + frame.Unit(seed + 7) * .08f) +
                frame.Unit(seed + 13));
            float along = 5 + age * (27 + frame.Unit(seed + 19) * 29);
            Vector2 point = head - heading * along +
                side * ((frame.Unit(seed + 23) - .5f) * (12 + age * 19));
            float alive = MathF.Sin(age * MathF.PI);
            frame.Sprite(point, 5 + frame.Unit(seed + 29) * 11,
                9 + frame.Unit(seed + 31) * 16,
                Shape.UserFlame, i % 5 == 0 ? core : i % 3 == 0 ? mantle : rim,
                .77f * alive, i % 6 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                angle - MathF.PI / 2 + (frame.Unit(seed + 37) - .5f) * .4f,
                light: i % 17 == 0);
            if (i % 4 == 0)
                frame.Sprite(point - heading * 8, 3 + frame.Unit(seed + 41) * 5,
                    5 + frame.Unit(seed + 43) * 8,
                    frost ? Shape.Spark : Shape.UserEmber,
                    i % 2 == 0 ? core : mantle, .75f * alive, Layer.EmissiveCore);
            if (i % 7 == 0)
                frame.Clip(point - heading * 13, 12 + frame.Unit(seed + 47) * 9,
                    15 + frame.Unit(seed + 49) * 12,
                    Shape.SmokeSequence, phase + frame.Unit(seed + 51),
                    frost ? 0x516d91 : 0x65402b,
                    .29f * alive, Layer.Smoke);
        }
        frame.Sprite(head, 19, 29, Shape.UserFlame, core, .92f,
            Layer.EmissiveCore, angle - MathF.PI / 2, light: true);
        if (frost)
        {
            SnowballWake(frame, pool, head, heading, now, frameIndex, remaining, wake);
            return true;
        }
        if (wake)
        {
            Vector2 drift = side * (frameIndex % 5 - 2) * 3;
            pool.TryAdd(new SpellParticle(head - heading * 17 + drift,
                -heading * 13 + drift, now, Math.Min(remaining, 102), 6,
                Tint(Gold, .66f), Shape.UserEmber, 0,
                Rotation: angle, Growth: -.22f, Layer: Layer.EmissiveCore,
                CastsLight: false, DepthY: head.Y + 47));
        }
        return true;
    }

    private static void Shield(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress, long now)
    {
        float hit = phase is SpellVisualPhase.Hit or SpellVisualPhase.Ward ? MathF.Sin(progress * MathF.PI) : 0;
        bool prepare = phase == SpellVisualPhase.Prepare;
        float unfold = prepare ? Ease(progress * 1.45f) : 1;
        float shimmer = .93f + .07f * MathF.Sin(now % 1200 * MathF.Tau / 1200);
        Vector2 centre = feet - Vector2.UnitY * (29 + 39 * (1 - unfold));
        float width = 67 * (.44f + .56f * unfold) + 11 * hit;
        float height = 94 * (.28f + .72f * unfold) + 9 * hit;
        frame.Sprite(centre, width, height, Shape.WardBack, 0xd69c23, .85f, Layer.Ground);
        frame.Sprite(centre, width, height, Shape.WardFront, 0xffcc42, shimmer, light: true);
        frame.Sprite(centre + Vector2.UnitY * 8 * unfold, width * .73f, height * .73f, Shape.Wisp, 0xffdd60,
            .38f + .31f * hit, Layer.Emissive, light: true);
        frame.Sprite(centre + new Vector2(4, 17) * unfold, width * .50f, height * .41f, Shape.Wisp, 0x72d8b8,
            .42f * unfold, Layer.Glow);
        frame.Sprite(centre + new Vector2(-18, -18) * unfold, 13 + 9 * hit, 19 + 8 * hit, Shape.Flash, Hot,
            .22f + .66f * hit, Layer.EmissiveCore, -.36f);
        frame.Sprite(feet - Vector2.UnitY * 5, width * .63f, 18, Shape.Wisp, 0xb2b850, .56f * unfold, Layer.Ground);
        for (int i = 0; i < 6; i++)
        {
            float orbit = now % 1800 / 1800f * MathF.Tau + i * MathF.Tau / 6;
            Vector2 mote = centre + Ellipse(orbit, width * .42f, height * .43f);
            frame.Sprite(mote, 4.2f, 6.3f, Shape.Spark, i % 3 == 0 ? 0xc9ffe9 : Gold, .49f * unfold,
                Depth(orbit), orbit);
        }
        if (hit > 0)
        {
            frame.Sprite(centre + new Vector2(2, 3), width * 1.09f, height * 1.06f, Shape.WardFront,
                Hot, hit * .67f, Layer.EmissiveCore, MathF.Sin(progress * 13) * .12f);
            frame.Sprite(centre, 45 + hit * 23, 59 + hit * 19, Shape.Orb, Hot, hit * .74f, Layer.EmissiveCore);
            frame.Sparks(centre, 14, 40, 49, progress, Gold, rise: 9);
        }
    }

    private static void BlinkRing(Frame frame, Vector2 feet, float progress, bool descending)
    {
        float lift = 5 + 39 * (descending ? 1 - Ease(progress * 1.14f) : Ease(progress * 1.16f));
        Vector2 centre = feet - Vector2.UnitY * lift;
        float width = 56 + 10 * MathF.Sin(progress * MathF.PI);
        frame.Orbit(centre, width / 2, 17, 0, MathF.Tau, Blue, 9, .83f);
        frame.Orbit(centre, width * .48f, 16, .45f, 5.65f, PaleBlue, 3, .86f);
        frame.Sprite(centre, width * 1.16f, 34, Shape.Orb, Blue, .55f, light: true);
        frame.Clip(centre + Vector2.UnitY * 4, width * 1.51f, 36, Shape.SmokeSequence, progress * .62f,
            0x4e5b98, .39f, Layer.Smoke);
        for (int i = 0; i < 13; i++)
        {
            float angle = i * MathF.Tau / 13 + progress * 1.9f;
            Vector2 root = centre + Ellipse(angle, width * .46f, 15);
            Vector2 end = root - new Vector2(MathF.Cos(angle) * 4, 8 + frame.Unit(i + 33) * 28);
            frame.Line(root, end, 6, i % 3 == 0 ? White : Blue, .79f, Depth(angle));
        }
        frame.Sparks(centre, 9, 34, 16, progress, PaleBlue, rise: 14);
    }

    private static void TeleportLight(Frame frame, Vector2 feet, float progress, bool departure)
    {
        float spread = departure ? 1 - progress * .58f : .36f + .64f * Ease(progress * 2.7f);
        Vector2 centre = feet - Vector2.UnitY * (35 + 11 * MathF.Sin(progress * MathF.PI));
        float pulse = .48f + .49f * MathF.Sin(progress * MathF.PI);
        frame.Sprite(centre, 99 * spread, 135, Shape.Orb, 0xbed9e5, .75f, light: true);
        frame.Sprite(centre, 55 * spread, 108, Shape.Orb, White, pulse, Layer.EmissiveCore, light: true);
        frame.Sprite(feet, 111 * spread, 57 * spread, Shape.Orb, 0x5578a6, .67f, Layer.Ground, light: true);
        for (int i = 0; i < 12; i++)
        {
            float x = (frame.Unit(i) - .5f) * 81 * spread;
            float depth = frame.Unit(i + 20) * 29 - 12;
            float height = 22 + frame.Unit(i + 30) * 92;
            float y = departure ? depth - 34 * progress : depth - 47 * (1 - progress);
            Vector2 end = feet + new Vector2(x, y);
            frame.Line(end - Vector2.UnitY * height, end, 3 + frame.Unit(i + 40) * 5, i % 3 == 0 ? PaleBlue : White,
                .60f, depth < 0 ? Layer.Ground : Layer.EmissiveCore);
        }
    }

    private static void TurnUndead(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        Vector2 centre = feet - Vector2.UnitY * (prepare ? 67 : 35);
        if (prepare)
        {
            float size = 96 + progress * 38;
            frame.Sprite(centre, size, size, Shape.Orb, Ember, .43f, light: true);
            frame.Sprite(centre, size * .76f, size * .76f, Shape.Glyph, Fire, .82f, rotation: progress * 1.8f);
            frame.Sprite(centre, size * .51f, size * .51f, Shape.Halo, Gold, .87f);
            frame.Sprite(centre, 37, 37, Shape.Flash, Hot, .96f, Layer.EmissiveCore, light: true);
            frame.Orbit(centre, 54, 39, -progress * 4.3f, 4.4f, 0xf64820, 3.1f, .67f);
            frame.Line(centre - Vector2.UnitX * 67, centre + Vector2.UnitX * 67, 3.8f, PaleBlue, .77f, Layer.EmissiveCore);
            frame.Sparks(centre, 12, 55, 50, progress, Fire);
            return;
        }

        const int count = 6;
        float radius = 21 + progress * 64;
        for (int i = 0; i < count; i++)
        {
            float angle = i * MathF.Tau / count + progress * 3.2f;
            Vector2 point = centre + Ellipse(angle, radius, radius * .67f) - Vector2.UnitY * (frame.Unit(i + 7) - .5f) * 48;
            int tint = i % 3 == 0 ? Fire : PaleBlue;
            float width = 39 + 16 * frame.Unit(i);
            frame.Sprite(point, width * 1.7f, width * 1.7f, Shape.Orb, tint, .46f, Depth(angle), light: true);
            frame.Sprite(point, width, width, Shape.Orb, tint == Fire ? Hot : White, .92f, Layer.EmissiveCore);
            frame.Sprite(point, width * .93f, width * .93f, Shape.Flash, White, .68f, Layer.EmissiveCore, angle);
            frame.Line(point, point + Ellipse(angle - 1.3f, 27, 16), 8, tint, .48f, Depth(angle));
        }
        frame.Sparks(centre, 13, 72, 57, progress, Gold);
        frame.Clip(centre - Vector2.UnitY * (progress * 22), 111, 100, Shape.SmokeSequence, progress * .78f,
            0x7a7877, .48f * progress, Layer.Smoke);
    }

    private static void Vampirism(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress)
    {
        Vector2 centre = feet - Vector2.UnitY * (phase == SpellVisualPhase.Prepare ? 81 : 53);
        if (phase == SpellVisualPhase.Return)
        {
            frame.Sprite(feet - Vector2.UnitY * 5, 105, 64, Shape.Orb, 0xe63a3e, .79f, Layer.Ground, light: true);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.Tau / 8 + progress * 3;
                Vector2 point = feet + Ellipse(angle, 14 + 31 * (1 - progress), 17);
                frame.Line(point - new Vector2(5, 76 * (1 - progress) + 18), point, 7, 0xf13e42, .66f, Depth(angle), light: i % 2 == 0);
            }
            frame.Sprite(centre - Vector2.UnitY * 23, 30, 34, Shape.Orb, 0xffa1a4, .9f, Layer.EmissiveCore, light: true);
            frame.Sprite(centre + Vector2.UnitY * 23, 55, 69, Shape.Flame, 0xff8986, .83f, rotation: -.18f, light: true);
            return;
        }

        float spin = progress * (phase == SpellVisualPhase.Prepare ? 5.2f : 8.3f);
        frame.Clip(centre, 134, 107, Shape.SmokeSequence, progress * .83f, 0x5c2131, .51f, Layer.Smoke);
        for (int i = 0; i < 5; i++)
        {
            float angle = spin + i * MathF.Tau / 5;
            Vector2 point = centre + Ellipse(angle, 26 + frame.Unit(i + 8) * 35, 16 + i * 3);
            Vector2 wing = Ellipse(angle + 1.24f, 40, 28);
            frame.Sprite(point, 54, 23, Shape.FlameTongue, 0xde170d, .85f, Depth(angle), angle + .49f, light: true);
            frame.Line(point, point + wing, 9, i % 2 == 0 ? 0xde241c : 0xa21c32, .75f, Depth(angle));
            frame.Line(point + wing * .47f, point + wing + Ellipse(angle + 1.8f, 14, 7), 3.5f, 0xff3b2e, .77f, Depth(angle));
        }
        frame.Sparks(centre, 22, 68, 49, progress, 0xee211d, rise: 15);
    }

    private static void Mirroring(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress)
    {
        bool fade = phase == SpellVisualPhase.Expire;
        bool prepare = phase == SpellVisualPhase.Prepare;
        float lift = prepare ? progress * 94 : 44 + 23 * progress;
        float spread = fade ? 1 - progress * .6f : .59f + progress * .41f;
        Vector2 centre = feet - Vector2.UnitY * lift;
        frame.Sprite(centre, 94 * spread, 117, Shape.Smoke, Ice, .44f, Layer.Smoke);
        if (prepare)
        {
            for (int i = 0; i < 2; i++)
                frame.Orbit(centre - Vector2.UnitY * i * 17, (30 + i * 11) * spread, 28,
                    progress * 6 + i * 1.57f, 4.8f, i == 0 ? PaleBlue : Blue, 6, .86f);
            frame.Sprite(centre, 45, 71, Shape.Orb, Blue, .72f, light: true);
        }
        else
        {
            frame.Sprite(feet - Vector2.UnitY * 40, 83 * spread, 130, Shape.SpiritFigure, 0x8dc7f0,
                fade ? .44f : .82f, rotation: -.12f + progress * .16f);
            for (int i = 0; i < 10; i++)
            {
                float height = (i + .5f) / 10;
                Vector2 point = feet + new Vector2((frame.Unit(i + 15) - .5f) * 59 * spread, 4 - height * 103 - progress * 13);
                frame.Sprite(point, (25 + 22 * frame.Unit(i)) * spread, 29 + frame.Unit(i + 7) * 22,
                    Shape.Smoke, i % 2 == 0 ? PaleBlue : IceWhite, .63f, rotation: progress + i, light: i % 3 == 0);
            }
            frame.Sprite(feet - Vector2.UnitY * 43, 38 * spread, 91, Shape.Orb, White, fade ? .46f : .82f, Layer.EmissiveCore, light: true);
        }
        frame.Sparks(centre, 18, 55 * spread, 46, progress, IceWhite, rise: fade ? 23 : 39);
    }

    private static void Booster(Frame frame, Vector2 feet, SpellVisualPhase phase, float progress, long now)
    {
        bool active = phase == SpellVisualPhase.Aura;
        Vector2 eye = feet - Vector2.UnitY * (104 + (active ? 0 : 8 * MathF.Sin(progress * MathF.PI)));
        frame.Sprite(eye, 97, 53, Shape.Orb, 0xf14b05, .48f, light: true);
        frame.Sprite(eye, 94, 51, Shape.SigilEye, Fire, .83f);
        frame.Sprite(eye, 84, 41, Shape.SigilEye, Gold, .85f + .09f * MathF.Sin(now % 950 * MathF.Tau / 950), Layer.EmissiveCore);
        frame.Sprite(eye, 14, 28, Shape.Flash, Hot, .75f, Layer.EmissiveCore);
        frame.Sparks(eye, active ? 6 : 13, 51, 22, now % 950 / 950f, Gold, 11);
        if (!active)
        {
            frame.Sprite(feet - Vector2.UnitY * 47, 49, 119, Shape.FlameTongue, Ember, .62f, light: true);
            frame.Line(eye, feet - Vector2.UnitY * 12, 20, Fire, .60f, light: true);
            for (int i = 0; i < 9; i++)
            {
                float offset = (frame.Unit(i) - .5f) * 121;
                float y = -100 + 114 * Fract(progress + frame.Unit(i + 11) * .46f);
                frame.Sprite(feet + new Vector2(offset, y), 8, 29, Shape.Feather, Fire, .85f, Layer.EmissiveCore, offset * .008f);
            }
        }
    }
}
