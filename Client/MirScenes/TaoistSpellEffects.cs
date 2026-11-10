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
        var tuning = SpellVfxAssets.Current.Taoist.PacketDuration;
        var fields = SpellVfxAssets.Fields("Taoist", "PacketDuration");
        return visual.Spell switch
        {
            Spell.SoulFireBall => fields.Read(tuning.SoulFireBallDurationMs),
            Spell.Healing or Spell.Purification or Spell.MassHiding => fields.Read(tuning.HealingDurationMs),
            Spell.SoulShield or Spell.BlessedArmour => fields.Read(tuning.SoulShieldDurationMs),
            Spell.HealingCircle or Spell.Plague => fields.Read(tuning.HealingCircleDurationMs),
            Spell.Curse or Spell.Reincarnation => fields.Read(tuning.CurseDurationMs),
            Spell.EnergyShield => visual.Phase == SpellVisualPhase.Aura ? fields.Read(tuning.EnergyShieldAuraDurationMs) : fields.Read(tuning.EnergyShieldImpactDurationMs),
            Spell.PetEnhancer => visual.Phase == SpellVisualPhase.Aura ? fields.Read(tuning.PetEnhancerAuraDurationMs) : fields.Read(tuning.PetEnhancerImpactDurationMs),
            Spell.Revelation when visual.Phase == SpellVisualPhase.Prepare => fields.Read(tuning.RevelationDurationMs),
            _ => fields.Read(tuning.DefaultDurationMs)
        };
    }

    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = Frame.ReferenceSize, uint seed = 0, uint field = 0,
        float? fieldSize = null)
    {
        using var emitterTime = SpellVfxAssets.AtProgress((now - start) / (float)Math.Max(1, duration));
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Taoist.DrawFrame;
        var fields = SpellVfxAssets.Fields("Taoist", "DrawFrame");
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
            Spell.SoulShield => Envelope(progress, fields.Read(tuning.SoulShieldEnvelopeFadeStart), fields.Read(tuning.SoulShieldEnvelopeTail)),
            Spell.BlessedArmour => Envelope(progress, fields.Read(tuning.BlessedArmourEnvelopeFadeStart), fields.Read(tuning.BlessedArmourEnvelopeTail)),
            Spell.Curse => Envelope(progress, fields.Read(tuning.CurseEnvelopeFadeStart), fields.Read(tuning.CurseEnvelopeTail)),
            Spell.SoulFireBall => Envelope(progress, fields.Read(tuning.SoulFireBallEnvelopeFadeStart), fields.Read(tuning.SoulFireBallEnvelopeTail)),
            _ => sustained ? Envelope(progress, fields.Read(tuning.EnvelopeFadeStart), fields.Read(tuning.EnvelopeTail)) : Envelope(progress)
        };
        uint artSeed = seed ^ (uint)visual.Spell * 0x2d5bu;
        bool isRune = visual.Spell is Spell.SoulShield or Spell.BlessedArmour or Spell.Curse;
        ushort opacityNoiseSeed = isRune ? (ushort)(1 + ((artSeed ^ (uint)start * 0x9e3779b9u) & 1023)) : (ushort)0;
        var frame = new Frame(pool, feet, now, strength, size, artSeed, field,
            alwaysOnTop: isRune, opacityNoiseSeed: opacityNoiseSeed);
        if (visual.Phase == SpellVisualPhase.Expire)
        {
            frame.Sparks(feet, fields.Read(tuning.SparksCount), fields.Read(tuning.SparksRx), fields.Read(tuning.SparksRy), progress, Ice, fields.Read(tuning.SparksRise), emitter: "TaoistSpellEffects.DrawFrame.1");
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
            case Spell.SoulShield: BurningWard(frame, feet, progress, fields.Read(tuning.SoulShieldShape)); break;
            case Spell.Revelation: Reveal(frame, feet, progress, elapsed, prepare); break;
            case Spell.BlessedArmour: BurningWard(frame, feet, progress, fields.Read(tuning.BlessedArmourShape)); break;
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
            case Spell.PoisonCloud: Cloud(frame, feet, ground ? elapsed : progress * fields.Read(tuning.PoisonCloudTime)); break;
            case Spell.EnergyShield: Shield(frame, feet, progress, aura); break;
            case Spell.PetEnhancer: Pet(frame, feet, progress, elapsed, aura); break;
            case Spell.HealingCircle: HealingCircle(frame, feet, progress, elapsed, ground, fieldSize ?? fields.Read(tuning.HealingCircleDiameter)); break;
        }
    }

    internal static bool DrawProjectile(SpellParticlePool pool, Spell spell, Vector2 head, Vector2 heading,
        long now, int frameIndex, int remaining, bool leaveWake, float size)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Current.Taoist.DrawProjectile;
        var fields = SpellVfxAssets.Fields("Taoist", "DrawProjectile");
        if (spell is not (Spell.SoulFireBall or Spell.MassHiding or Spell.SoulShield or Spell.BlessedArmour or
            Spell.Curse or Spell.Hallucination or Spell.Plague or Spell.PoisonCloud)) return false;
        if (remaining <= 0) return true;
        var frame = new Frame(pool, head, now, 1, size, (uint)(spell.GetHashCode() * 719 + frameIndex), depthY: head.Y + fields.Read(tuning.DepthY));
        Vector2 side = new(-heading.Y, heading.X);
        float angle = MathF.Atan2(heading.Y, heading.X) + MathF.Sin(frameIndex * fields.Read(tuning.Angle)) * fields.Read(tuning.Angle2);
        int accent = spell switch
        {
            Spell.SoulShield or Spell.Plague => Blue,
            Spell.Curse or Spell.PoisonCloud => Jade,
            Spell.Hallucination or Spell.MassHiding => Violet,
            _ => Amber
        };
        frame.Sprite(head - heading * fields.Read(tuning.MistFilamentOffset), fields.Read(tuning.MistFilamentWidth), fields.Read(tuning.MistFilamentHeight), fields.Read(tuning.MistFilamentShape), accent, fields.Read(tuning.MistFilamentAlpha), fields.Read(tuning.MistFilamentLayer), angle, light: fields.Read(tuning.MistFilamentLight), emitter: "TaoistSpellEffects.DrawProjectile.1");
        frame.Sprite(head, fields.Read(tuning.TalismanWidth), fields.Read(tuning.TalismanHeight), fields.Read(tuning.TalismanShape), Amber, fields.Read(tuning.TalismanAlpha), rotation: angle, emitter: "TaoistSpellEffects.DrawProjectile.2");
        frame.Sprite(head + heading * fields.Read(tuning.FlashOffset), fields.Read(tuning.FlashWidth), fields.Read(tuning.FlashHeight), fields.Read(tuning.FlashShape), Ivory, fields.Read(tuning.FlashAlpha), fields.Read(tuning.FlashLayer), angle, emitter: "TaoistSpellEffects.DrawProjectile.3");
        for (int i = 0; i < fields.Read(tuning.MistFilamentCount); i++)
        {
            float drift = MathF.Sin(frameIndex * fields.Read(tuning.Drift) - i * fields.Read(tuning.Drift2));
            Vector2 point = head - heading * (fields.Read(tuning.Point) + i * fields.Read(tuning.Point2)) + side * drift * (fields.Read(tuning.Point3) + i);
            frame.Sprite(point, fields.Read(tuning.MistFilamentWidth2) - i * fields.Read(tuning.MistFilamentWidth3), fields.Read(tuning.MistFilamentHeight2) - i * fields.Read(tuning.MistFilamentHeight3), fields.Read(tuning.MistFilamentShape2), i % 2 == 0 ? accent : Amber,
                fields.Read(tuning.MistFilamentAlpha2) - i * fields.Read(tuning.MistFilamentAlpha3), rotation: angle + drift * fields.Read(tuning.MistFilamentRotation), emitter: "TaoistSpellEffects.DrawProjectile.4");
            frame.Sprite(point + side * (i % 2 == 0 ? fields.Read(tuning.SparkOffset) : fields.Read(tuning.SparkOffset2)), fields.Read(tuning.SparkWidth) + i % 3, fields.Read(tuning.SparkHeight), fields.Read(tuning.SparkShape),
                i % 2 == 0 ? Gold : Ice, fields.Read(tuning.SparkAlpha) - i * fields.Read(tuning.SparkAlpha2), fields.Read(tuning.SparkLayer), angle, emitter: "TaoistSpellEffects.DrawProjectile.5");
        }
        if (leaveWake)
        {
            int life = Math.Min(remaining, fields.Read(tuning.Life));
            Vector2 point = head - heading * fields.Read(tuning.Point4);
            SpellVfxAssets.Emit(pool, new SpellParticle(point, -heading * fields.Read(tuning.WispVelocity) + side * MathF.Sin(frameIndex) * fields.Read(tuning.WispVelocity2), now, life, fields.Read(tuning.WispSize),
                Tint(accent, fields.Read(tuning.Opacity)), fields.Read(tuning.WispShape), 0, fields.Read(tuning.WispAspect), angle, fields.Read(tuning.WispGrowth), fields.Read(tuning.WispLayer), CastsLight: fields.Read(tuning.WispCastsLight), DepthY: head.Y + fields.Read(tuning.WispDepthY)), "TaoistSpellEffects.DrawProjectile.6");
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
        frame.Sprite(feet, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, Teal, tuning.WispAlpha, tuning.WispLayer, emitter: "TaoistSpellEffects.Heal.1");
        frame.Sprite(body, tuning.WispWidth2, tuning.WispHeight2, tuning.WispShape2, Jade, tuning.WispAlpha2, tuning.WispLayer2, light: tuning.WispLight, emitter: "TaoistSpellEffects.Heal.2");
        HealMotes(frame, body, tuning.Radius, progress, tuning.Count);
        float trailAngle = prepare ? MathF.PI * tuning.TrailAngle : phase + MathF.PI * tuning.TrailAngle2;
        frame.Sprite(head, tuning.WispWidth3, tuning.WispHeight3, tuning.WispShape3, prepare ? Blue : Jade, tuning.WispAlpha3, tuning.WispLayer3, trailAngle, light: tuning.WispLight2, emitter: "TaoistSpellEffects.Heal.3");
        frame.Sprite(head, tuning.WispWidth4, tuning.WispHeight4, tuning.WispShape4, prepare ? Ice : Mint, tuning.WispAlpha4, tuning.WispLayer4, trailAngle, light: tuning.WispLight3, emitter: "TaoistSpellEffects.Heal.4");
        frame.Sprite(head, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha, tuning.FlashLayer, phase * tuning.FlashRotation, emitter: "TaoistSpellEffects.Heal.5");
        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float age = i / tuning.AgeDivisor;
            float turn = phase - age * tuning.Turn;
            Vector2 point = prepare ? head + new Vector2(MathF.Sin(turn * tuning.Point + i * tuning.Point2) * (tuning.X + age * tuning.X2), age * (tuning.Y + travel * tuning.Y2)) :
                body + Ellipse(turn - tuning.EllipseAngle2, tuning.EllipseX2 - age * tuning.EllipseX3, tuning.EllipseY2 + age * tuning.EllipseY3) + new Vector2((frame.Unit(i) - tuning.X3) * tuning.X4, age * tuning.Y3);
            Layer depth = prepare ? tuning.PrepareDepth : Depth(turn - tuning.Depth);
            frame.Sprite(point, tuning.SparkWidth + tuning.SparkWidth2 * frame.Unit(i + 7), tuning.SparkHeight + tuning.SparkHeight2 * frame.Unit(i + 1), tuning.SparkShape,
                i % 5 == 0 ? Ice : i % 2 == 0 ? Mint : Jade, tuning.SparkAlpha - age * tuning.SparkAlpha2, depth, turn, emitter: "TaoistSpellEffects.Heal.6");
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
                tuning.FlashLayer2, turn, light: i % 9 == 0, emitter: "TaoistSpellEffects.Heal.7");
            if (i % 8 == 0)
                frame.Sprite(point, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Jade, tuning.OrbAlpha * twinkle,
                    tuning.OrbLayer, light: tuning.OrbLight, emitter: "TaoistSpellEffects.Heal.8");
        }
    }

    private static void SpiritEdge(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Taoist.SpiritEdge);
        Vector2 centre = feet + facing * tuning.Centre - Vector2.UnitY * tuning.Centre2;
        frame.Sprite(centre, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Teal, tuning.OrbAlpha, light: tuning.OrbLight, emitter: "TaoistSpellEffects.SpiritEdge.1");
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            Vector2 point = centre + facing * (i * tuning.Point) + new Vector2(0, (frame.Unit(i) - tuning.Y) * tuning.Y2);
            frame.Thread(point, point + facing * (tuning.ThreadEnd + tuning.ThreadEnd2 * progress), tuning.ThreadStartWidth, tuning.ThreadEndWidth, i % 2 == 0 ? Ice : Jade, tuning.ThreadAlpha, emitter: "TaoistSpellEffects.SpiritEdge.2");
        }
        frame.Sparks(centre, tuning.SparksCount, tuning.SparksRx, tuning.SparksRy, progress, Mint, tuning.SparksRise, emitter: "TaoistSpellEffects.SpiritEdge.3");
    }
}
