using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static partial class TaoistSpellEffects
{
    private static int White => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).White;
    private static int Jade => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Jade;
    private static int Teal => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Teal;
    private static int Mint => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Mint;
    private static int InkBlue => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).InkBlue;
    private static int Blue => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Blue;
    private static int Ice => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Ice;
    private static int Violet => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Violet;
    private static int Ochre => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Ochre;
    private static int Amber => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Amber;
    private static int Gold => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Gold;
    private static int Ivory => SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Palette).Ivory;

    internal static bool HasArt(Spell spell) => SpellCurriculum.Owns(MirClass.Taoist, spell);

    internal static int PacketDuration(SpellVisual visual)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.PacketDuration);
        return visual.Spell switch
        {
            Spell.SoulFireBall => tuning.SoulFireBallDurationMs,
            Spell.Healing or Spell.Purification or Spell.MassHiding => tuning.HealingDurationMs,
            Spell.SoulShield or Spell.BlessedArmour => tuning.SoulShieldDurationMs,
            Spell.HealingCircle or Spell.Plague => tuning.HealingCircleDurationMs,
            Spell.Curse or Spell.Reincarnation => tuning.CurseDurationMs,
            Spell.EnergyShield => visual.Phase == SpellVisualPhase.Aura ? tuning.EnergyShieldAuraDurationMs : tuning.EnergyShieldImpactDurationMs,
            Spell.PetEnhancer => visual.Phase == SpellVisualPhase.Aura ? tuning.PetEnhancerAuraDurationMs : tuning.PetEnhancerImpactDurationMs,
            Spell.Revelation when visual.Phase == SpellVisualPhase.Prepare => tuning.RevelationDurationMs,
            _ => tuning.DefaultDurationMs
        };
    }

    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = Frame.ReferenceSize, uint seed = 0, uint field = 0,
        float? fieldSize = null)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.DrawFrame);
        if (duration <= 0 || now < start || now - start >= duration) return;
        float elapsed = (now - start) / 1000f;
        float progress = (now - start) / (float)duration;
        bool aura = visual.Phase == SpellVisualPhase.Aura;
        bool ground = visual.Phase == SpellVisualPhase.Field;
        bool prepare = visual.Phase == SpellVisualPhase.Prepare;
        bool hit = visual.Phase == SpellVisualPhase.Hit;
        if (aura) progress = now % duration / (float)duration;
        bool sustained = visual.Spell is Spell.Healing or Spell.MassHealing or
            Spell.Hiding or Spell.MassHiding or Spell.SummonHolyDeva or Spell.SummonSkeleton or Spell.SummonShinsu;
        float strength = aura || ground ? 1 : visual.Spell switch
        {
            Spell.SoulShield => Envelope(progress, tuning.SoulShieldEnvelopeFadeStart, tuning.SoulShieldEnvelopeTail),
            Spell.BlessedArmour => Envelope(progress, tuning.BlessedArmourEnvelopeFadeStart, tuning.BlessedArmourEnvelopeTail),
            Spell.Curse => Envelope(progress, tuning.CurseEnvelopeFadeStart, tuning.CurseEnvelopeTail),
            Spell.SoulFireBall => Envelope(progress, tuning.SoulFireBallEnvelopeFadeStart, tuning.SoulFireBallEnvelopeTail),
            _ => sustained ? Envelope(progress, tuning.EnvelopeFadeStart, tuning.EnvelopeTail) : Envelope(progress)
        };
        uint artSeed = seed ^ (uint)visual.Spell * 0x2d5bu;
        bool isRune = visual.Spell is Spell.SoulShield or Spell.BlessedArmour or Spell.Curse;
        ushort opacityNoiseSeed = isRune ? (ushort)(1 + ((artSeed ^ (uint)start * 0x9e3779b9u) & 1023)) : (ushort)0;
        var frame = new Frame(pool, feet, now, strength, size, artSeed, field,
            alwaysOnTop: isRune, opacityNoiseSeed: opacityNoiseSeed);
        if (visual.Phase == SpellVisualPhase.Expire)
        {
            frame.Sparks(feet, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Ice, tuning.SparksRise);
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
            case Spell.SoulShield: BurningWard(frame, feet, progress, tuning.SoulShieldShape); break;
            case Spell.Revelation: Reveal(frame, feet, progress, elapsed, prepare); break;
            case Spell.BlessedArmour: BurningWard(frame, feet, progress, tuning.BlessedArmourShape); break;
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
            case Spell.Curse: CurseRune(frame, feet, progress); break;
            case Spell.Plague: Plague(frame, feet, progress, elapsed); break;
            case Spell.PoisonCloud: Cloud(frame, feet, ground ? elapsed : progress * tuning.PoisonCloudTime); break;
            case Spell.EnergyShield: Shield(frame, feet, progress, aura); break;
            case Spell.PetEnhancer: Pet(frame, feet, progress, elapsed, aura); break;
            case Spell.HealingCircle: HealingCircle(frame, feet, progress, elapsed, ground, fieldSize ?? tuning.HealingCircleDiameter); break;
        }
    }

    internal static bool DrawProjectile(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frameIndex, int remaining, bool leaveWake, float size)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.DrawProjectile);
        if (spell is not (Spell.SoulFireBall or Spell.MassHiding or Spell.SoulShield or Spell.BlessedArmour or
            Spell.Curse or Spell.Hallucination or Spell.Plague or Spell.PoisonCloud)) return false;
        if (remaining <= 0) return true;
        var frame = new Frame(pool, head, now, 1, size, (uint)(spell.GetHashCode() * 719 + frameIndex), depthY: head.Y + tuning.DepthY);
        Vector2 side = new(-heading.Y, heading.X);
        float angle = MathF.Atan2(heading.Y, heading.X) + MathF.Sin(frameIndex * tuning.Angle) * tuning.Angle2;
        int accent = spell switch
        {
            Spell.SoulShield or Spell.Plague => Blue,
            Spell.Curse or Spell.PoisonCloud => Jade,
            Spell.Hallucination or Spell.MassHiding => Violet,
            _ => Amber
        };
        frame.Sprite(head - heading * tuning.MistFilamentOffset, tuning.MistFilamentWidth, tuning.MistFilamentHeight, tuning.MistFilamentShape, accent, tuning.MistFilamentAlpha, tuning.MistFilamentLayer, angle, light: tuning.MistFilamentLight);
        frame.Sprite(head, tuning.TalismanWidth, tuning.TalismanHeight, tuning.TalismanShape, Amber, tuning.TalismanAlpha, rotation: angle);
        frame.Sprite(head + heading * tuning.FlashOffset, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, Ivory, tuning.FlashAlpha, tuning.FlashLayer, angle);
        for (int i = 0; i < tuning.MistFilamentCount; i++)
        {
            float drift = MathF.Sin(frameIndex * tuning.Drift - i * tuning.Drift2);
            Vector2 point = head - heading * (tuning.Point + i * tuning.Point2) + side * drift * (tuning.Point3 + i);
            frame.Sprite(point, tuning.MistFilamentWidth2 - i * tuning.MistFilamentWidth3, tuning.MistFilamentHeight2 - i * tuning.MistFilamentHeight3, tuning.MistFilamentShape2, i % 2 == 0 ? accent : Amber,
                tuning.MistFilamentAlpha2 - i * tuning.MistFilamentAlpha3, rotation: angle + drift * tuning.MistFilamentRotation);
            frame.Sprite(point + side * (i % 2 == 0 ? tuning.SparkOffset : tuning.SparkOffset2), tuning.SparkWidth + i % 3, tuning.SparkHeight, tuning.SparkShape,
                i % 2 == 0 ? Gold : Ice, tuning.SparkAlpha - i * tuning.SparkAlpha2, tuning.SparkLayer, angle);
        }
        if (leaveWake)
        {
            int life = Math.Min(remaining, tuning.Life);
            Vector2 point = head - heading * tuning.Point4;
            pool.TryAdd(new SpellParticle(point, -heading * tuning.WispVelocity + side * MathF.Sin(frameIndex) * tuning.WispVelocity2, now, life, tuning.WispSize,
                Tint(accent, tuning.Opacity), tuning.WispShape, 0, tuning.WispAspect, angle, tuning.WispGrowth, tuning.WispLayer, CastsLight: tuning.WispCastsLight, DepthY: head.Y + tuning.WispDepthY));
        }
        return true;
    }

    private static void Heal(Frame frame, Vector2 feet, float progress, bool prepare)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.Heal);
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        float travel = Ease(progress);
        float phase = progress * (prepare ? tuning.PreparePhase : MathF.Tau * tuning.Phase);
        Vector2 head = prepare ? feet - Vector2.UnitY * (tuning.Head + travel * tuning.Head2) :
            body + Ellipse(phase - tuning.EllipseAngle, tuning.EllipseX, tuning.EllipseY);
        frame.Sprite(feet, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, Teal, tuning.WispAlpha, tuning.WispLayer);
        frame.Sprite(body, tuning.WispWidth2, tuning.WispHeight2, tuning.WispShape2, Jade, tuning.WispAlpha2, tuning.WispLayer2, light: tuning.WispLight);
        HealMotes(frame, body, tuning.Radius, progress, tuning.Count);
        float trailAngle = prepare ? MathF.PI * tuning.TrailAngle : phase + MathF.PI * tuning.TrailAngle2;
        frame.Sprite(head, tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape3, prepare ? Blue : Jade, tuning.WispAlpha3, tuning.WispLayer3, trailAngle, light: tuning.WispLight2);
        frame.Sprite(head, tuning.WispWidth4, tuning.WispHeight4, tuning.WispShape4, prepare ? Ice : Mint, tuning.WispAlpha4, tuning.WispLayer4, trailAngle, light: tuning.WispLight3);
        frame.Sprite(head, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer, phase * tuning.FlashRotation);
        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float age = i / tuning.AgeDivisor;
            float turn = phase - age * tuning.Turn;
            Vector2 point = prepare ? head + new Vector2(MathF.Sin(turn * tuning.Point + i * tuning.Point2) * (tuning.X + age * tuning.X2), age * (tuning.Y + travel * tuning.Y2)) :
                body + Ellipse(turn - tuning.EllipseAngle2, tuning.EllipseX2 - age * tuning.EllipseX3, tuning.EllipseY2 + age * tuning.EllipseY3) + new Vector2((frame.Unit(i) - tuning.X3) * tuning.X4, age * tuning.Y3);
            Layer depth = prepare ? tuning.PrepareDepth : Depth(turn - tuning.Depth);
            frame.Sprite(point, tuning.SparkWidth + tuning.SparkWidth2 * frame.Unit(i + 7), tuning.SparkHeight + tuning.SparkHeight2 * frame.Unit(i + 1), tuning.SparkShape,
                i % 5 == 0 ? Ice : i % 2 == 0 ? Mint : Jade, tuning.SparkAlpha - age * tuning.SparkAlpha2, depth, turn);
        }
        // Fine sparks rise through the larger healing ribbons. Each has its own
        // deterministic phase so they glitter instead of moving as one ring.
        for (int i = 0; i < tuning.FlashCount; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i + 261));
            float turn = i * tuning.Turn2 + age * tuning.Turn3;
            Vector2 point = body + Ellipse(turn, tuning.EllipseX4 + tuning.EllipseX5 * frame.Unit(i + 17),
                tuning.EllipseY4 + tuning.EllipseY5 * frame.Unit(i + 63)) - Vector2.UnitY * (age * (tuning.Point3 + tuning.Point4 * frame.Unit(i + 111)));
            float twinkle = MathF.Sin(age * MathF.PI);
            int colour = i % 5 == 0 ? Ivory : i % 2 == 0 ? Mint : Ice;
            frame.Sprite(point, tuning.FlashWidth2 + frame.Unit(i + 35) * tuning.FlashWidth3, tuning.FlashHeight2 + frame.Unit(i + 83) * tuning.FlashHeight3,
                i % 6 == 0 ? tuning.FlashShape2 : tuning.FlashShape3, colour, tuning.FlashAlpha2 * twinkle,
                tuning.FlashLayer2, turn, light: i % 9 == 0);
            if (i % 8 == 0)
                frame.Sprite(point, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Jade, tuning.OrbAlpha * twinkle,
                    tuning.OrbLayer, light: tuning.OrbLight);
        }
    }

    private static void SpiritEdge(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.SpiritEdge);
        Vector2 centre = feet + facing * tuning.Centre - Vector2.UnitY * tuning.Centre2;
        frame.Sprite(centre, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Teal, tuning.OrbAlpha, light: tuning.OrbLight);
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            Vector2 point = centre + facing * (i * tuning.Point) + new Vector2(0, (frame.Unit(i) - tuning.Y) * tuning.Y2);
            frame.Thread(point, point + facing * (tuning.ThreadEnd + tuning.ThreadEnd2 * progress), tuning.ThreadStartWidth, tuning.ThreadEndWidth, i % 2 == 0 ? Ice : Jade, tuning.ThreadAlpha);
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Mint, tuning.SparksRise);
    }
}
