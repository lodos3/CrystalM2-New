using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static partial class TaoistSpellEffects
{
    private const int White = 0xf6fffa, Jade = 0x38e49e, Teal = 0x137a73, Mint = 0xacf9d8;
    private const int InkBlue = 0x224c9f, Blue = 0x46a9f5, Ice = 0xbceaff, Violet = 0x8177c4;
    private const int Ochre = 0x8b492b, Amber = 0xf89738, Gold = 0xffd26b, Ivory = 0xfff0be;

    internal static bool HasArt(Spell spell) => SpellCurriculum.Owns(MirClass.Taoist, spell);

    internal static int PacketDuration(SpellVisual visual) => visual.Spell switch
    {
        Spell.SoulFireBall => 600,
        Spell.Healing or Spell.Purification or Spell.MassHiding => 800,
        Spell.SoulShield or Spell.BlessedArmour or Spell.HealingCircle or Spell.Plague => 1200,
        Spell.Curse or Spell.Reincarnation => 2000,
        Spell.EnergyShield => visual.Phase == SpellVisualPhase.Aura ? 800 : 900,
        Spell.PetEnhancer => visual.Phase == SpellVisualPhase.Aura ? 1200 : 800,
        Spell.Revelation when visual.Phase == SpellVisualPhase.Prepare => 1200,
        _ => 1000
    };

    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = Frame.ReferenceSize, uint seed = 0, uint field = 0,
        float fieldSize = 354)
    {
        if (duration <= 0 || now < start || now - start >= duration) return;
        float elapsed = (now - start) / 1000f;
        float progress = (now - start) / (float)duration;
        bool aura = visual.Phase == SpellVisualPhase.Aura;
        bool ground = visual.Phase == SpellVisualPhase.Field;
        bool prepare = visual.Phase == SpellVisualPhase.Prepare;
        bool hit = visual.Phase == SpellVisualPhase.Hit;
        if (aura) progress = now % duration / (float)duration;
        float strength = aura || ground ? 1 : Envelope(progress);
        var frame = new Frame(pool, feet, now, strength, size, seed ^ (uint)visual.Spell * 0x2d5bu, field);
        if (visual.Phase == SpellVisualPhase.Expire)
        {
            frame.Sparks(feet, 8, 25, 13, progress, Ice, 23);
            return;
        }

        switch (visual.Spell)
        {
            case Spell.Healing: Heal(frame, feet, progress, prepare); break;
            case Spell.SpiritSword: SpiritEdge(frame, feet, facing, progress); break;
            case Spell.Poisoning: Poison(frame, feet, progress, elapsed, prepare); break;
            case Spell.SoulFireBall: PaperBurst(frame, feet, progress); break;
            case Spell.SummonSkeleton: SummonStars(frame, feet, progress, deva: false); break;
            case Spell.Hiding: Veil(frame, feet, progress, elapsed, mass: false); break;
            case Spell.MassHiding: Veil(frame, feet, progress, elapsed, mass: !hit); break;
            case Spell.SoulShield: Rune(frame, feet, progress, Shape.SoulGlyph, hit ? 188 : 306, hit ? 151 : 204, InkBlue, Blue, Ice); break;
            case Spell.Revelation: Reveal(frame, feet, progress, elapsed, prepare); break;
            case Spell.BlessedArmour: Rune(frame, feet, progress, Shape.ArmourGlyph, hit ? 175 : 236, hit ? 146 : 178, Ochre, Amber, Gold); break;
            case Spell.EnergyRepulsor: Repulse(frame, feet, progress); break;
            case Spell.TrapHexagon: Trap(frame, feet, prepare ? progress : Fract(elapsed), prepare); break;
            case Spell.Purification: Cleanse(frame, feet, progress, prepare); break;
            case Spell.MassHealing:
                if (hit) Heal(frame, feet, progress, prepare: false);
                else MassHeal(frame, feet, progress, prepare);
                break;
            case Spell.Hallucination: Phantom(frame, feet, progress, elapsed); break;
            case Spell.UltimateEnhancer: Enhance(frame, feet, progress); break;
            case Spell.SummonShinsu: Shinsu(frame, feet, progress); break;
            case Spell.Reincarnation: Revive(frame, feet, ground ? Fract(elapsed) : progress); break;
            case Spell.SummonHolyDeva: SummonStars(frame, feet, progress, deva: true); break;
            case Spell.Curse: Rune(frame, feet, progress, Shape.CurseGlyph, 352, 228, 0x426958, Mint, White); break;
            case Spell.Plague: Plague(frame, feet, progress, elapsed); break;
            case Spell.PoisonCloud: Cloud(frame, feet, ground ? elapsed : progress * 2.4f); break;
            case Spell.EnergyShield: Shield(frame, feet, progress, aura); break;
            case Spell.PetEnhancer: Pet(frame, feet, progress, elapsed, aura); break;
            case Spell.HealingCircle: HealingCircle(frame, feet, progress, elapsed, ground, fieldSize); break;
        }
    }

    internal static bool DrawProjectile(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frameIndex, int remaining, bool leaveWake, float size)
    {
        if (spell is not (Spell.SoulFireBall or Spell.MassHiding or Spell.SoulShield or Spell.BlessedArmour or
            Spell.Curse or Spell.Hallucination or Spell.Plague or Spell.PoisonCloud)) return false;
        if (remaining <= 0) return true;
        var frame = new Frame(pool, head, now, 1, size, (uint)(spell.GetHashCode() * 719 + frameIndex), depthY: head.Y + 36);
        Vector2 side = new(-heading.Y, heading.X);
        float angle = MathF.Atan2(heading.Y, heading.X) + MathF.Sin(frameIndex * 1.2f) * .2f;
        int accent = spell switch
        {
            Spell.SoulShield or Spell.Plague => Blue,
            Spell.Curse or Spell.PoisonCloud => Jade,
            Spell.Hallucination or Spell.MassHiding => Violet,
            _ => Amber
        };
        frame.Sprite(head - heading * 6, 58, 36, Shape.Orb, accent, .49f, light: true);
        frame.Sprite(head, 37, 24, Shape.Talisman, Amber, .94f, rotation: angle);
        frame.Sprite(head + heading * 5, 13, 11, Shape.Flash, Ivory, .91f, Layer.EmissiveCore, angle);
        for (int i = 0; i < 5; i++)
        {
            float drift = MathF.Sin(frameIndex * .67f - i * .8f);
            Vector2 point = head - heading * (13 + i * 7) + side * drift * (3 + i);
            frame.Sprite(point, 26 - i * 3, 18 - i * 2, Shape.Wisp, i % 2 == 0 ? accent : Amber, .86f - i * .13f);
            frame.Sprite(point + side * (i % 2 == 0 ? -5 : 5), 4 + i % 3, 4, Shape.Spark,
                i % 2 == 0 ? Gold : Ice, .94f - i * .09f, Layer.EmissiveCore, angle);
        }
        if (leaveWake)
        {
            int life = Math.Min(remaining, 112);
            Vector2 point = head - heading * 31;
            pool.TryAdd(new SpellParticle(point, -heading * 13 + side * MathF.Sin(frameIndex) * 6, now, life, 18,
                Tint(accent, .45f), Shape.Wisp, 0, .8f, angle, -.31f, Layer.Emissive, CastsLight: false, DepthY: head.Y + 36));
        }
        return true;
    }

    private static void Heal(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        Vector2 body = feet - Vector2.UnitY * 36;
        float travel = Ease(progress);
        float phase = progress * (prepare ? 3.1f : MathF.Tau * 1.35f);
        Vector2 head = prepare ? feet - Vector2.UnitY * (14 + travel * 80) :
            body + Ellipse(phase - .55f, 36, 22);
        frame.Sprite(feet, 83, 43, Shape.Wisp, Teal, .84f, Layer.Ground);
        HealMotes(frame, body, 33, progress, 48);
        frame.Sprite(head, 86, 79, Shape.Wisp, prepare ? InkBlue : Teal, .55f, Layer.Glow, light: true);
        frame.Sprite(head, 52, 56, Shape.Orb, prepare ? Blue : Jade, .72f, light: true);
        frame.Sprite(head, 16, 20, Shape.Flash, White, .98f, Layer.EmissiveCore, phase * .37f);
        for (int i = 0; i < 28; i++)
        {
            float age = i / 28f;
            float turn = phase - age * 2.45f;
            Vector2 point = prepare ? head + new Vector2(MathF.Sin(turn * 3 + i * 1.8f) * (3 + age * 11), age * (13 + travel * 58)) :
                body + Ellipse(turn - .55f, 36 - age * 5, 22 + age * 4) + new Vector2((frame.Unit(i) - .5f) * 15, age * 12);
            Layer depth = prepare ? Layer.Emissive : Depth(turn - .55f);
            frame.Sprite(point, 4 + 8 * frame.Unit(i + 7), 5 + 7 * frame.Unit(i + 1), Shape.Spark,
                i % 5 == 0 ? Ice : i % 2 == 0 ? Mint : Jade, .86f - age * .58f, depth, turn);
        }
    }

    private static void SpiritEdge(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        Vector2 centre = feet + facing * 22 - Vector2.UnitY * 29;
        frame.Sprite(centre, 39, 27, Shape.Orb, Teal, .65f, light: true);
        for (int i = 0; i < 4; i++)
        {
            Vector2 point = centre + facing * (i * 8) + new Vector2(0, (frame.Unit(i) - .5f) * 19);
            frame.Thread(point, point + facing * (8 + 8 * progress), 2.6f, .42f, i % 2 == 0 ? Ice : Jade, .88f);
        }
        frame.Sparks(centre, 5, 21, 10, progress, Mint, 11);
    }
}
