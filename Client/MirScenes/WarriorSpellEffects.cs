using System.Numerics;
using CrystalM2.Rendering.Core;
using Frame = Client.MirScenes.SpellParticleFrame;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static partial class WarriorSpellEffects
{
    private static int Steel => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).Steel;
    private static int Ice => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).Ice;
    private static int White => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).White;
    private static int Flame => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).Flame;
    private static int Ember => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).Ember;
    private static int Heat => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).Heat;
    private static int Gold => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).Gold;
    private static int PaleGold => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).PaleGold;
    private static int Smoke => SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Palette).Smoke;

    internal static bool HasArt(Spell spell) => SpellCurriculum.Owns(MirClass.Warrior, spell);
    internal static bool UsesBodyMask(Spell spell) => spell is Spell.Rage or Spell.ImmortalSkin or Spell.ProtectionField or Spell.Fury;

    internal static int PacketDuration(SpellVisual visual)
    {
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Warrior.PacketDuration;
        var fields = SpellVfxAssets.Fields("Warrior", "PacketDuration");
        return visual.Phase switch
        {
            SpellVisualPhase.Prepare => fields.Read(tuning.PrepareDurationMs),
            SpellVisualPhase.Burn => fields.Read(tuning.BurnDurationMs),
            SpellVisualPhase.Ward => fields.Read(tuning.WardDurationMs),
            _ when visual.Spell == Spell.FlamingSword => fields.Read(tuning.FlamingSwordDurationMs),
            _ when visual.Spell == Spell.TwinDrakeBlade => fields.Read(tuning.TwinDrakeBladeDurationMs),
            _ => visual.Spell is Spell.Fencing or Spell.ShoulderDash ? fields.Read(tuning.FencingOrDashDurationMs) : fields.Read(tuning.DefaultDurationMs)
        };
    }

    internal static void DrawFrame(SpellParticlePool pool, SpellVisual visual, Vector2 feet, Vector2 facing,
        long now, long start, int duration, float size = ReferenceSize, uint seed = 0, Vector2? weaponTip = null)
    {
        using var emitterTime = SpellVfxAssets.AtProgress((now - start) / (float)Math.Max(1, duration));
        using var vfxScope = SpellVfxAssets.For(visual.Spell);
        var tuning = SpellVfxAssets.Current.Warrior.DrawFrame;
        var fields = SpellVfxAssets.Fields("Warrior", "DrawFrame");
        if (visual.Spell == Spell.CrossHalfMoon &&
            visual.Phase is SpellVisualPhase.Contact or SpellVisualPhase.Hit or SpellVisualPhase.Impact) return;
        if (duration <= 0 || now < start || now - start >= duration) return;
        float progress = (now - start) / (float)duration;
        if (visual is { Spell: Spell.Thrusting, Phase: SpellVisualPhase.Swing })
        {
            // Keep the source and reach in world pixels as the artwork size is adjusted.
            float windStrength = (fields.Read(tuning.WindStrength) + fields.Read(tuning.WindStrength2) * Ease(progress * fields.Read(tuning.WindStrength3))) * (1 - fields.Read(tuning.WindStrength4) * Ease((progress - fields.Read(tuning.WindStrength5)) / fields.Read(tuning.WindStrengthDivisor)));
            var wind = new Frame(pool, feet, now, windStrength, size, seed, scalePositions: false);
            ThrustWind(wind, feet, facing, progress, visual.ExtraReach);
            return;
        }
        bool aura = visual.Phase == SpellVisualPhase.Aura;
        float cycle = aura ? now % duration / (float)duration : progress;
        float strength = aura ? 1 : visual.Spell is Spell.BladeAvalanche or Spell.Entrapment or Spell.TwinDrakeBlade
            ? Envelope(progress, fields.Read(tuning.EnvelopeFadeStart), fields.Read(tuning.EnvelopeTail)) : Envelope(progress);
        var frame = new Frame(pool, feet, now, strength, size, seed ^ (uint)visual.Spell * 34763);

        switch (visual.Phase)
        {
            case SpellVisualPhase.Contact:
                if (visual.Spell == Spell.FlamingSword) Engulf(frame, feet, progress, (now - start) / 1000f);
                else if (visual.Spell == Spell.TwinDrakeBlade) DrakeContact(frame, feet, progress, (now - start) / 1000f);
                else if (visual.Spell == Spell.Entrapment) Snare(frame, feet, progress, preparing: false);
                else Strike(frame, visual.Spell, feet, facing, progress);
                return;
            case SpellVisualPhase.Burn:
                BladeFlame(frame, feet - Vector2.UnitY * fields.Read(tuning.BurnOffset), progress, fields.Read(tuning.BurnWidth));
                return;
            case SpellVisualPhase.Ward:
                Skin(frame, feet, cycle, aura: true);
                return;
        }

        switch (visual.Spell)
        {
            case Spell.FlamingSword when visual.Phase == SpellVisualPhase.Prepare:
                Vector2 tip = weaponTip ?? feet + facing * fields.Read(tuning.FlamingSwordTip) - Vector2.UnitY * fields.Read(tuning.FlamingSwordTip2);
                var blade = new Frame(pool, tip, now, Envelope(progress), size, seed, depthY: feet.Y);
                BladeFlame(blade, tip, progress, fields.Read(tuning.FlamingSwordWidth));
                break;
            case Spell.FlamingSword:
                Engulf(frame, feet, progress, (now - start) / 1000f);
                break;
            case Spell.TwinDrakeBlade when visual.Phase == SpellVisualPhase.Prepare:
                DrakeCharge(frame, feet, weaponTip ?? feet + facing * fields.Read(tuning.TwinDrakeBladeHand) - Vector2.UnitY * fields.Read(tuning.TwinDrakeBladeHand2),
                    progress, (now - start) / 1000f);
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

    private static void DrakeCharge(Frame frame, Vector2 feet, Vector2 hand, float progress, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.DrakeCharge);
        // Actor-following four-strand spiral, rising from the weapon around
        // the torso before returning to the blade. Root Y is ground depth;
        // screen-space lift never changes which side of the actor owns a mote.
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        Vector2 overhead = feet - Vector2.UnitY * tuning.Overhead;
        for (int strand = 0; strand < tuning.StrandCount; strand++)
        {
            float phase = progress * MathF.Tau * tuning.Phase + strand * tuning.Phase2;
            float radius = (tuning.Radius + tuning.Radius2 * MathF.Sin(progress * MathF.PI)) *
                (tuning.Radius3 + frame.Unit(strand + 18) * tuning.Radius4);
            Vector2 target = body + ProjectGround(Ellipse(phase, radius, radius)) -
                Vector2.UnitY * (progress * tuning.Target);
            target = Vector2.Lerp(hand, target, Ease(progress * tuning.Value));
            float departure = Ease((progress - tuning.Departure) / tuning.DepartureDivisor);
            Vector2 finalPoint = Vector2.Lerp(overhead, hand, Ease((progress - tuning.FinalPoint) / tuning.FinalPointDivisor));
            target = Vector2.Lerp(target, finalPoint, departure);
            Vector2 previous = hand;
            for (int j = 1; j <= tuning.UserLightningCount; j++)
            {
                float t = j / tuning.TDivisor;
                float angle = phase - t * tuning.Angle;
                Vector2 trail = Vector2.Lerp(hand, target, t) +
                    ProjectGround(Ellipse(angle, radius * tuning.EllipseX, radius * tuning.EllipseY)) *
                    MathF.Sin(t * MathF.PI);
                float groundY = feet.Y + ProjectGround(Ellipse(angle, radius, radius)).Y;
                bool behind = groundY < feet.Y;
                Layer layer = behind ? tuning.Layer : tuning.Layer2;
                float flicker = tuning.Flicker + tuning.Flicker2 * MathF.Sin(time * tuning.Flicker3 + j * tuning.Flicker4 + strand);
                // Fine arcs stay below a few pixels wide: no solid streak slabs.
                frame.Thread(previous, trail, tuning.ThreadStartWidth, tuning.ThreadEndWidth, j % 3 != 1 ? 0xF5FCFF : 0x318FFF,
                    flicker * tuning.ThreadAlpha, layer, light: j == 4 && strand == 0, sortY: groundY, emitter: "WarriorSpellEffects.DrakeCharge.1");
                if (j == 1 || j % 2 == 0)
                {
                    Vector2 delta = trail - previous;
                    float direction = MathF.Atan2(delta.Y, delta.X);
                    frame.Sprite((previous + trail) * tuning.UserLightningOffset, tuning.UserLightningWidth + delta.Length() * tuning.UserLightningWidth2,
                        tuning.UserLightningHeight, tuning.UserLightningShape, j % 2 == 0 ? 0xF5FCFF : 0x429CFF,
                        tuning.UserLightningAlpha * flicker, layer, direction, sortY: groundY, emitter: "WarriorSpellEffects.DrakeCharge.2");
                }
                if (j == 5 && strand % 2 == 0)
                    frame.Sprite(trail, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, tuning.WispColour,
                        tuning.WispAlpha * flicker, tuning.WispLayer, sortY: groundY, emitter: "WarriorSpellEffects.DrakeCharge.3");
                previous = trail;
            }
        }

        for (int i = 0; i < tuning.SparkCount; i++)
        {
            float age = Fract(progress * tuning.Age + frame.Unit(i + 173));
            float phase = progress * MathF.Tau * tuning.Phase3 +
                frame.Unit(i + 71) * MathF.Tau;
            Vector2 ground = feet + ProjectGround(Ellipse(phase, tuning.EllipseX2, tuning.EllipseY2));
            Vector2 point = ground - Vector2.UnitY *
                (tuning.Point + progress * tuning.Point2 + age * tuning.Point3);
            float fade = (tuning.Fade - age * tuning.Fade2) *
                (tuning.Fade3 + tuning.Fade4 * MathF.Sin(age * MathF.PI));
            Layer layer = ground.Y < feet.Y ? tuning.Layer3 : tuning.Layer4;
            frame.Sprite(point, tuning.SparkWidth + frame.Unit(i + 13) * tuning.SparkWidth2,
                tuning.SparkHeight + frame.Unit(i + 14) * tuning.SparkHeight2,
                tuning.SparkShape, i % 3 != 1 ? 0xF5FCFF : 0x318FFF,
                fade, layer, phase * tuning.SparkRotation, sortY: ground.Y, emitter: "WarriorSpellEffects.DrakeCharge.4");
        }
    }

    private static void Engulf(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Engulf);
        // Small individual tongues form a hot wound-shaped contact zone.
        // No vertical pillars, static opaque fire blobs or floor discs.
        float durationFade = 1f - Ease((progress - tuning.DurationFade) / tuning.DurationFadeDivisor);
        frame.Sprite(feet - Vector2.UnitY * 21, 46, 61, Shape.Wisp, 0xFF7C23, .84f * durationFade,
            Layer.EmissiveCore, light: true);
        frame.Clip(feet + new Vector2(-6, -6), 23, 20, Shape.FlameSequence, time * 2, 0xF98827,
            .77f * durationFade, Layer.Ground, light: true);
        frame.Clip(feet + new Vector2(4, -43), 17, 28, Shape.FlameSequence, time * 2 + .37f,
            0xFFC558, .74f * durationFade, Layer.EmissiveCore);
        for (int i = 0; i < tuning.UserFlameCount; i++)
        {
            int seed = i * 29 + 71;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 17) * tuning.Age2) +
                frame.Unit(seed + 25));
            float lateral = (frame.Unit(seed) - tuning.Lateral) * (tuning.Lateral2 + progress * tuning.Lateral3);
            float depth = frame.Unit(seed + 3);
            float rise = age * (tuning.Rise + tuning.Rise2 * frame.Unit(seed + 7));
            float flicker = tuning.Flicker + tuning.Flicker2 * MathF.Sin(time * tuning.Flicker3 + seed * tuning.Flicker4);
            float fade = durationFade * MathF.Sin(age * MathF.PI) * flicker;
            Vector2 root = feet + new Vector2(lateral, tuning.Y - depth * tuning.Y2);
            Vector2 tongue = root + new Vector2((frame.Unit(seed + 33) - tuning.X) * age * tuning.X2,
                -rise);
            float smallWidth = tuning.SmallWidth + frame.Unit(seed + 35) * tuning.SmallWidth2;
            float smallHeight = tuning.SmallHeight + frame.Unit(seed + 42) * tuning.SmallHeight2;
            int colour = i % 6 == 0 ? Heat : i % 3 == 0 ? Ember : Flame;
            frame.Sprite(tongue - Vector2.UnitY * smallHeight * tuning.UserFlameOffset,
                smallWidth, smallHeight, tuning.UserFlameShape, colour, fade,
                i % 5 == 0 ? tuning.UserFlameLayer : tuning.UserFlameLayer2,
                (frame.Unit(seed + 49) - tuning.UserFlameRotation) * tuning.UserFlameRotation2, light: i % 17 == 0, emitter: "WarriorSpellEffects.Engulf.1");
            if (i % 7 == 0)
                frame.Clip(tongue, smallWidth, smallHeight,
                    tuning.FlameSequenceShape, time * tuning.FlameSequencePhase + age * tuning.FlameSequencePhase2,
                    colour, fade * tuning.FlameSequenceAlpha, tuning.FlameSequenceLayer, emitter: "WarriorSpellEffects.Engulf.2");
            if (i % 8 == 0)
                frame.Sprite(tongue, smallWidth * tuning.FlameTongueWidth, smallHeight * tuning.FlameTongueHeight,
                    tuning.FlameTongueShape, Heat, fade * tuning.FlameTongueAlpha,
                    tuning.FlameTongueLayer, emitter: "WarriorSpellEffects.Engulf.3");
            if (i % 4 == 0)
                frame.Sprite(root + new Vector2(lateral * tuning.X3, -rise * tuning.Y3),
                    tuning.WarriorSparkWidth + frame.Unit(seed + 90) * tuning.WarriorSparkWidth2,
                    tuning.WarriorSparkHeight + frame.Unit(seed + 91) * tuning.WarriorSparkHeight2,
                    i % 8 == 0 ? tuning.WarriorSparkShape : tuning.WarriorSparkShape2, Heat,
                    fade * tuning.WarriorSparkAlpha, tuning.WarriorSparkLayer, emitter: "WarriorSpellEffects.Engulf.4");
            if (i % 5 == 0)
            {
                float smokeAge = Fract(progress * tuning.SmokeAge + frame.Unit(seed + 61));
                Vector2 plume = root + new Vector2(
                    (frame.Unit(seed + 65) - tuning.X4) * smokeAge * tuning.X5,
                    tuning.Y4 - tuning.Y5 * smokeAge);
                frame.Clip(plume, tuning.SmokeSequenceWidth + smokeAge * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + smokeAge * tuning.SmokeSequenceHeight2,
                    tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase + smokeAge,
                    i % 2 == 0 ? tuning.SmokeSequenceColour : Smoke,
                    tuning.SmokeSequenceAlpha * (1 - smokeAge) * durationFade, tuning.SmokeSequenceLayer, emitter: "WarriorSpellEffects.Engulf.5");
            }
        }
    }

    private static void DrakeContact(Frame frame, Vector2 feet, float progress, float time)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.DrakeContact);
        // The impact originates on the struck actor's actual map cell. The
        // electrical silhouette winds around the target, but the burst and
        // rising lightning spikes root in the ground BELOW that actor.
        Vector2 centre = feet - Vector2.UnitY * tuning.Centre;
        float dissipate = 1f - Ease((progress - tuning.Dissipate) / tuning.DissipateDivisor);
        float impact = dissipate * (tuning.Impact + tuning.Impact2 * MathF.Sin(MathF.Min(1, progress * tuning.Impact3) * MathF.PI));
        for (int i = 0; i < tuning.UserLightningCount; i++)
        {
            float offset = frame.Unit(233 + i * 17) * MathF.Tau;
            float startAngle = offset + time * (tuning.StartAngle + frame.Unit(i + 32) * tuning.StartAngle2);
            float endAngle = startAngle + tuning.EndAngle + frame.Unit(i + 13) * tuning.EndAngle2;
            float radiusX = tuning.RadiusX + tuning.RadiusX2 * frame.Unit(i + 25);
            float radiusY = tuning.RadiusY + tuning.RadiusY2 * frame.Unit(i + 29);
            Vector2 from = centre + Ellipse(startAngle, radiusX, radiusY);
            Vector2 to = centre + Ellipse(endAngle, radiusX, radiusY);
            float groundY = feet.Y + ProjectGround(Ellipse(startAngle, radiusX, radiusX)).Y;
            Layer layer = groundY < feet.Y ? tuning.Layer : tuning.Layer2;
            float flicker = tuning.Flicker + tuning.Flicker2 * MathF.Sin(time * tuning.Flicker3 + i);
            WizardSpellEffects.Channel(frame, from, to, tuning.Spread, tuning.Width,
                impact * flicker * tuning.Alpha, time, 1, i * tuning.Offset + tuning.Offset2,
                layer: layer, colours: WizardSpellEffects.DrakeColours,
                depths: new Vector2(groundY, groundY));
            float direction = MathF.Atan2(to.Y - from.Y, to.X - from.X);
            for (int j = 0; j < tuning.UserLightningCount2; j++)
            {
                Vector2 point = Vector2.Lerp(from, to, (j + tuning.Point) / 4f);
                frame.Sprite(point, tuning.UserLightningWidth + frame.Unit(i * 13 + j + 3) * tuning.UserLightningWidth2,
                    tuning.UserLightningHeight + frame.Unit(i * 19 + j + 9) * tuning.UserLightningHeight2,
                    tuning.UserLightningShape, j == 2 ? 0x3599FF : 0xF4FCFF,
                    impact * (tuning.UserLightningAlpha + tuning.UserLightningAlpha2 * flicker), layer, direction,
                    light: i % 6 == 0 && j == 0, sortY: groundY, emitter: "WarriorSpellEffects.DrakeContact.1");
            }
        }

        // Ground spikes are branching, short electric cuts, not solid ice
        // meshes or opaque pillar sprites. Their root controls world-row depth.
        for (int i = 0; i < tuning.UserLightningCount3; i++)
        {
            int seed = 821 + i * 29;
            float angle = (i + frame.Unit(seed + 3) * tuning.Angle) *
                MathF.Tau / tuning.AngleDivisor;
            float radius = tuning.Radius + frame.Unit(seed + 5) * tuning.Radius2;
            Vector2 root = feet + ProjectGround(Ellipse(angle, radius, radius));
            float height = (tuning.Height + frame.Unit(seed + 7) * tuning.Height2) *
                (tuning.Height3 + tuning.Height4 * impact);
            Vector2 middle = root + new Vector2(
                (frame.Unit(seed + 9) - tuning.X) * tuning.X2, -height * tuning.Y);
            Vector2 tip = root + new Vector2(
                (frame.Unit(seed + 11) - tuning.X3) * tuning.X4, -height);
            bool rear = root.Y < feet.Y;
            Layer layer = rear ? tuning.Layer3 : tuning.Layer4;
            int tint = i % 3 == 1 ? 0x399EFF : 0xF4FCFF;
            frame.Thread(root, middle, tuning.ThreadStartWidth, tuning.ThreadEndWidth, tint,
                impact * tuning.ThreadAlpha, layer, sortY: root.Y, emitter: "WarriorSpellEffects.DrakeContact.2");
            frame.Thread(middle, tip, tuning.ThreadStartWidth2, tuning.ThreadEndWidth2, i % 4 == 1 ? 0x2367D4 : 0xF4FCFF,
                impact * tuning.ThreadAlpha2, layer, sortY: root.Y, emitter: "WarriorSpellEffects.DrakeContact.3");
            if (i % 2 == 0)
            {
                Vector2 fork = Vector2.Lerp(middle, tip, tuning.Fork) +
                    new Vector2((frame.Unit(seed + 17) - tuning.X5) * tuning.X6, tuning.Y2);
                frame.Thread(middle, fork, tuning.ThreadStartWidth3, tuning.ThreadEndWidth3, Ice,
                    impact * tuning.ThreadAlpha3, layer, sortY: root.Y, emitter: "WarriorSpellEffects.DrakeContact.4");
                frame.Sprite(fork, tuning.UserLightningWidth3, tuning.UserLightningHeight3, tuning.UserLightningShape2,
                    White, impact * tuning.UserLightningAlpha3, layer, sortY: root.Y, emitter: "WarriorSpellEffects.DrakeContact.5");
            }
        }

        // Micro-embers eject laterally from the ground contact. Only a few
        // receive scene light; the F12 glow pass remains independent.
        for (int i = 0; i < tuning.WarriorSparkCount; i++)
        {
            int seed = 1747 + i * 23;
            float angle = (i + frame.Unit(seed + 3)) * MathF.Tau / tuning.AngleDivisor2;
            float age = Fract(progress * (tuning.Age + frame.Unit(seed + 5)) +
                frame.Unit(seed + 7));
            float range = (tuning.Range + frame.Unit(seed + 11) * tuning.Range2) *
                (tuning.Range3 + tuning.Range4 * Ease(progress * tuning.Range5));
            Vector2 root = feet + ProjectGround(Ellipse(angle, range, range));
            Vector2 point = root - Vector2.UnitY *
                (tuning.Point2 + age * (tuning.Point3 + frame.Unit(seed + 13) * tuning.Point4));
            Layer layer = root.Y < feet.Y ? tuning.Layer5 : tuning.Layer6;
            frame.Sprite(point, tuning.WarriorSparkWidth + frame.Unit(seed + 17) * tuning.WarriorSparkWidth2,
                tuning.WarriorSparkHeight + frame.Unit(seed + 19) * tuning.WarriorSparkHeight2,
                i % 5 == 0 ? tuning.WarriorSparkShape : tuning.WarriorSparkShape2,
                i % 4 == 1 ? 0x399EFF : 0xF4FCFF,
                impact * (tuning.WarriorSparkAlpha + tuning.WarriorSparkAlpha2 * (1 - age)), layer,
                angle, light: i % 17 == 0, sortY: root.Y, emitter: "WarriorSpellEffects.DrakeContact.6");
        }

        // Thin cold-blue smoke appears just after contact and expands across
        // the floor. The opacity stays modest so characters remain legible.
        for (int i = 0; i < tuning.SmokeSequenceCount; i++)
        {
            int seed = 2251 + i * 19;
            float angle = frame.Unit(seed + 3) * MathF.Tau;
            float age = Fract(progress * tuning.Age2 + frame.Unit(seed + 5) * tuning.Age3);
            float radius = tuning.Radius3 + progress * tuning.Radius4 + frame.Unit(seed + 7) * tuning.Radius5;
            Vector2 root = feet + ProjectGround(Ellipse(angle, radius, radius));
            Vector2 plume = root - Vector2.UnitY *
                (tuning.Plume + age * (tuning.Plume2 + frame.Unit(seed + 11) * tuning.Plume3));
            Layer layer = i % 4 == 0 ? tuning.Layer7 : tuning.Layer8;
            frame.Clip(plume, tuning.SmokeSequenceWidth + age * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + age * tuning.SmokeSequenceHeight2,
                tuning.SmokeSequenceShape, time * tuning.SmokeSequencePhase + frame.Unit(seed + 13),
                i % 3 == 0 ? tuning.SmokeSequenceColour : tuning.SmokeSequenceColour2,
                impact * tuning.SmokeSequenceAlpha * (1 - age * tuning.SmokeSequenceAlpha2),
                layer, rotation: (frame.Unit(seed + 17) - tuning.SmokeSequenceRotation) * tuning.SmokeSequenceRotation2,
                sortY: root.Y, emitter: "WarriorSpellEffects.DrakeContact.7");
        }
    }

    private static void Strike(Frame frame, Spell spell, Vector2 feet, Vector2 facing, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Strike);
        // The shared physical strike has a blade-shaped silhouette, not a round
        // explosion. Variant styling follows the hit mechanism: steel slash,
        // hot riposte, or a dusty shoulder collision.
        bool hot = spell is Spell.SlashingBurst or Spell.CounterAttack;
        bool heavy = spell is Spell.TwinDrakeBlade or Spell.BladeAvalanche or Spell.CrossHalfMoon or Spell.SlashingBurst;
        bool dash = spell == Spell.ShoulderDash;
        bool fencing = spell == Spell.Fencing;
        int colour = hot ? Flame : dash ? tuning.ColourColour : fencing ? tuning.ColourColour2 : Steel;
        int core = hot || dash ? Heat : White;
        float radius = fencing ? tuning.Radius : heavy ? tuning.Radius2 : tuning.Radius3;
        float angle = MathF.Atan2(facing.Y, facing.X);
        Vector2 centre = feet - Vector2.UnitY * (dash ? tuning.Centre : tuning.Centre2);
        float spread = tuning.Spread + tuning.Spread2 * Ease(progress * tuning.Spread3);
        float fade = 1f - Ease((progress - tuning.Fade) / tuning.FadeDivisor);
        var side = new Vector2(-facing.Y, facing.X);

        frame.Sprite(centre + facing * (radius * tuning.UserSlashOffset), radius * tuning.UserSlashWidth * spread,
            radius * tuning.UserSlashHeight, tuning.UserSlashShape, colour, tuning.UserSlashAlpha * fade,
            tuning.UserSlashLayer, angle - tuning.UserSlashRotation, light: heavy, emitter: "WarriorSpellEffects.Strike.1");
        frame.Sprite(centre + facing * (radius * tuning.UserSlashOffset2) - side * tuning.UserSlashOffset3,
            radius * tuning.UserSlashWidth2 * spread, radius * tuning.UserSlashHeight2,
            tuning.UserSlashShape2, core, tuning.UserSlashAlpha2 * fade,
            tuning.UserSlashLayer2, angle - tuning.UserSlashRotation2, light: tuning.UserSlashLight, emitter: "WarriorSpellEffects.Strike.2");

        if (hot)
            frame.Sprite(centre + facing * tuning.UserFlameOffset, radius * tuning.UserFlameWidth, radius * tuning.UserFlameHeight,
                tuning.UserFlameShape, Ember, tuning.UserFlameAlpha * fade,
                tuning.UserFlameLayer, angle * tuning.UserFlameRotation, emitter: "WarriorSpellEffects.Strike.3");
        if (dash)
            frame.Sprite(feet + facing * tuning.UserRockOffset, radius * tuning.UserRockWidth, radius * tuning.UserRockHeight,
                tuning.UserRockShape, tuning.UserRockColour, tuning.UserRockAlpha * fade,
                tuning.UserRockLayer, angle, emitter: "WarriorSpellEffects.Strike.4");

        int fragments = fencing ? tuning.Fragments : heavy ? tuning.Fragments2 : tuning.Fragments3;
        for (int i = 0; i < fragments; i++)
        {
            int seed = i * 19 + 83;
            float age = Fract(progress * tuning.Age + frame.Unit(seed));
            float along = frame.Unit(seed + 2);
            float scatter = (frame.Unit(seed + 5) - tuning.Scatter) * radius * tuning.Scatter2;
            Vector2 point = centre + facing * (along * radius * tuning.Point + age * tuning.Point2) +
                side * scatter - Vector2.UnitY * (age * (tuning.Point3 + frame.Unit(seed + 7) * tuning.Point4));
            frame.Sprite(point, tuning.UserEmberWidth + frame.Unit(seed + 11) * tuning.UserEmberWidth2,
                tuning.UserEmberHeight + frame.Unit(seed + 13) * tuning.UserEmberHeight2,
                hot ? tuning.UserEmberShape : dash ? tuning.UserEmberShape2 : tuning.UserEmberShape3,
                i % 3 == 0 ? core : colour, tuning.UserEmberAlpha * fade * (1f - age * tuning.UserEmberAlpha2),
                i % 3 == 0 ? tuning.UserEmberLayer : tuning.UserEmberLayer2,
                angle + (frame.Unit(seed + 21) - tuning.UserEmberRotation) * tuning.UserEmberRotation2,
                light: i % 5 == 0, emitter: "WarriorSpellEffects.Strike.5");
        }
    }

    internal static void Dash(SpellParticlePool pool, Vector2 from, Vector2 to, long now)
    {
        using var vfxScope = SpellVfxAssets.For(Spell.ShoulderDash);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Dash);
        Vector2 delta = to - from;
        if (delta.LengthSquared() < 1) return;
        Vector2 forward = Vector2.Normalize(delta);
        var side = new Vector2(-forward.Y, forward.X);
        float angle = MathF.Atan2(delta.Y, delta.X);
        for (int i = 0; i < tuning.WarriorDustCount; i++)
        {
            float edge = (i % 2 == 0 ? -1 : 1) * (tuning.Edge + i * tuning.Edge2);
            Vector2 foot = from + delta * (i / 4f) + side * edge;
            SpellVfxAssets.Emit(pool, new SpellParticle(foot, side * edge - forward * tuning.WarriorDustVelocity, now, tuning.WarriorDustDuration + i * tuning.WarriorDustDuration2, tuning.WarriorDustSize + i * tuning.WarriorDustSize2,
                Tint(tuning.Colour, tuning.Opacity), tuning.WarriorDustShape, 0, tuning.WarriorDustAspect, angle, tuning.WarriorDustGrowth, tuning.WarriorDustLayer, CastsLight: tuning.WarriorDustCastsLight), "WarriorSpellEffects.Dash.1");
            SpellVfxAssets.Emit(pool, new SpellParticle(foot - Vector2.UnitY * (tuning.BladeEdgeOrigin + i * tuning.BladeEdgeOrigin2), -forward * tuning.BladeEdgeVelocity, now, tuning.BladeEdgeDuration, tuning.BladeEdgeSize + i * tuning.BladeEdgeSize2,
                Tint(i % 2 == 0 ? tuning.Colour2 : tuning.Colour3, tuning.Opacity2), tuning.BladeEdgeShape, 0, tuning.BladeEdgeAspect, angle + (i - tuning.BladeEdgeRotation) * tuning.BladeEdgeRotation2,
                Layer: tuning.BladeEdgeLayer, CastsLight: tuning.BladeEdgeCastsLight, DepthY: foot.Y), "WarriorSpellEffects.Dash.2");
        }
    }

    // Attack areas use the same 48 Ã— 32 projection as the tiles they sweep through.
    private static Vector2 GroundDirection(Vector2 facing) =>
        facing / MathF.Max(MathF.Abs(facing.X), MathF.Abs(facing.Y) * SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.GroundDirection).Y);

    private static void ThrustWind(Frame frame, Vector2 feet, Vector2 facing, float progress, int extraReach)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.ThrustWind);
        Vector2 source = feet - Vector2.UnitY * tuning.Source;
        Vector2 heading = GroundHeading(facing);
        var direction = (MirDirection)(((int)MathF.Round(MathF.Atan2(heading.Y, heading.X) * tuning.Direction / MathF.PI) + tuning.Direction2) & 7);
        float width = direction switch
        {
            MirDirection.Up or MirDirection.DownRight => tuning.Factor,
            MirDirection.Down => tuning.Factor2,
            MirDirection.UpRight => tuning.Factor3,
            _ => tuning.Factor4
        };
        bool edgeOn = direction is MirDirection.Up or MirDirection.DownRight;
        float unfold = Ease(progress * tuning.Unfold);
        float spread = width * (tuning.Spread + tuning.Spread2 * unfold);
        Vector2 travel = (GroundDirection(facing) * ((tuning.Travel + extraReach) * tuning.Travel2) +
            new Vector2(0, facing.Y * tuning.Y)) * (tuning.Travel3 + tuning.Travel4 * unfold);
        Vector2 axis = Vector2.Normalize(travel);
        var side = new Vector2(-axis.Y, axis.X);
        Vector2 front = source + travel;

        // The first three legacy frames describe the weapon backswing. The next
        // three open a compact curl into a foreshortened, feathered bow of air.
        for (int lane = -1; lane <= tuning.LaneCount; lane++)
        {
            float sway = MathF.Sin(progress * tuning.Sway - lane * tuning.Sway2) * (tuning.Sway3 + unfold * tuning.Sway4);
            float splay = lane * spread;
            Strand(source + side * (lane * tuning.Start),
                source + travel * tuning.Pull + side * (splay * tuning.Pull2 + sway),
                source + travel * tuning.Bend + side * (splay * tuning.Bend2 - sway * tuning.Bend3),
                source + travel * tuning.End + side * (splay * tuning.End2),
                tuning.Thickness + tuning.Thickness2 * unfold, lane == 0 ? Ice : Steel, tuning.Alpha - tuning.Alpha2 * unfold, tuning.Segments);
        }

        float bank = spread * (tuning.Bank + tuning.Bank2 * MathF.Sin(progress * tuning.Bank3));
        Vector2 crown = front - side * bank;
        Vector2 opening = side * spread;
        if (edgeOn)
        {
            // Those two views look along the curl. Pitch the leading edge into the
            // flight path so its bright rim reads as a long wisp in profile.
            crown -= travel * tuning.Factor5;
            opening += travel * (direction == MirDirection.Up ? tuning.UpActive : tuning.UpInactive);
        }
        Vector2 curlStart = crown - axis * (spread * tuning.CurlStart) - opening;
        Vector2 curlUpper = crown + axis * (spread * tuning.CurlUpper) - opening * tuning.CurlUpper2;
        Vector2 curlLower = crown + axis * (spread * tuning.CurlLower) + opening * tuning.CurlLower2;
        Vector2 curlEnd = crown - axis * (spread * tuning.CurlEnd) + opening;
        Strand(curlStart, curlUpper, curlLower, curlEnd, tuning.Thickness3 + unfold * tuning.Thickness4, tuning.Colour, tuning.Alpha3, tuning.Segments2);
        Strand(curlStart, curlUpper, curlLower, curlEnd, tuning.Thickness5 + unfold * tuning.Thickness6, tuning.Colour2, tuning.Alpha4, tuning.Segments3);

        // The curved wisps ride the pressure front. Their trailing ends peel inward
        // at different angles, leaving open air between the bands.
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float t = tuning.T + i * tuning.T2 + tuning.T3 * MathF.Sin(progress * tuning.T4 - i * tuning.T5);
            var (crest, _) = Cubic(curlStart, curlUpper, curlLower, curlEnd, t);
            float length = (tuning.Length + spread * tuning.Length2) * (tuning.Length3 + tuning.Length4 * MathF.Sin(t * MathF.PI));
            float roll = (t - tuning.Roll) * (tuning.Roll2 + spread * tuning.Roll3) - bank * tuning.Roll4;
            Vector2 tail = crest - axis * length + side * roll;
            Strand(tail, tail + axis * (length * tuning.Pull3) + side * (roll * tuning.Pull4),
                crest - axis * (length * tuning.Bend4) - side * (roll * tuning.Bend5), crest - axis * tuning.End3,
                tuning.Thickness7 + unfold * tuning.Thickness8, i % 3 == 0 ? White : i % 3 == 1 ? tuning.Colour3 : tuning.Colour4, tuning.Alpha5, tuning.Segments4);
        }
        frame.Sprite(source + travel * tuning.WispOffset, tuning.WispWidth + spread * tuning.WispWidth2, tuning.WispHeight + spread * tuning.WispHeight2,
            tuning.WispShape, Ice, tuning.WispAlpha, rotation: MathF.Atan2(axis.Y, axis.X), light: tuning.WispLight, emitter: "WarriorSpellEffects.ThrustWind.1");

        void Strand(Vector2 start, Vector2 pull, Vector2 bend, Vector2 end, float thickness, int colour, float alpha, int segments)
        {
            var (inner, outer) = Edge(0);
            for (int i = 1; i <= segments; i++)
            {
                var (nextInner, nextOuter) = Edge(i / (float)segments);
                frame.WindRibbon(new SpriteQuad(inner, nextInner, outer, nextOuter), colour, alpha, emitter: "WarriorSpellEffects.ThrustWind.2");
                (inner, outer) = (nextInner, nextOuter);
            }

            (Vector2 Inner, Vector2 Outer) Edge(float t)
            {
                var (centre, tangent) = Cubic(start, pull, bend, end, t);
                float half = thickness * frame.Scale * (tuning.StrandTipThickness + tuning.StrandMidThickness * MathF.Sin(t * MathF.PI));
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
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Avalanche);
        Vector2 heading = GroundHeading(facing);
        heading /= MathF.Max(MathF.Abs(heading.X), MathF.Abs(heading.Y));
        var side = new Vector2(-heading.Y, heading.X);
        float angle = MathF.Atan2(facing.Y, facing.X);
        // A compact frost pulse replaces the long pre-swing streaks. It
        // stays around the caster and dissipates before the ground blades rise.
        float release = Ease((progress - tuning.Release) / tuning.ReleaseDivisor) *
            (1 - Ease((progress - tuning.Release2) / tuning.ReleaseDivisor2));
        Vector2 frost = feet + ProjectGround(heading * 15) - Vector2.UnitY * 22;
        float anticipation = Ease(progress * 7) * (1 - Ease((progress - .28f) / .22f));
        frame.Sprite(frost, 26 + release * 10, 19 + release * 7, Shape.Flash, 0xD7F4FF,
            anticipation * .48f + release * .32f, Layer.EmissiveCore, light: true,
            emitter: "WarriorSpellEffects.Avalanche.FrostPulse");
        for (int i = 0; i < 5; i++)
        {
            float offset = (i - 2) * 7f;
            frame.Clip(frost + ProjectGround(side * offset), 24 + i * 2, 15,
                Shape.SmokeSequence, progress * 1.4f + i * .19f,
                i % 2 == 0 ? 0xC6EEFF : 0x8C9CE8, anticipation * .18f + release * .25f,
                Layer.Smoke, sortY: feet.Y, emitter: "WarriorSpellEffects.Avalanche.FrostRelease");
        }

        for (int lane = -1; lane <= tuning.LaneCount; lane++)
        for (int step = 0; step < tuning.StepCount; step++)
        {
            float age = progress - (tuning.Age + step * tuning.Age2 + Math.Abs(lane) * tuning.Age3);
            if (age < 0 || age >= .55f) continue;
            int index = step + (lane + 1) * tuning.Index;
            float rise = Ease(age * tuning.Rise);
            float fade = 1 - Ease((age - tuning.Fade) / tuning.FadeDivisor);
            float opacity = (tuning.Opacity + tuning.Opacity2 * rise) * fade;
            float stride = tuning.Stride + step * tuning.Stride2 + tuning.Stride3 * Ease(age * tuning.Stride4);
            float breadth = lane * (tuning.Breadth + step * tuning.Breadth2);
            Vector2 root = feet + ProjectGround(heading * stride + side * breadth);
            frame.Sprite(root, tuning.WarriorDustWidth + step * tuning.WarriorDustWidth2, tuning.WarriorDustHeight + step * tuning.WarriorDustHeight2, tuning.WarriorDustShape, tuning.WarriorDustColour, tuning.WarriorDustAlpha * opacity,
                tuning.WarriorDustLayer, angle, sortY: root.Y, emitter: "WarriorSpellEffects.Avalanche.6");
            frame.Sprite(root, tuning.WispWidth2 + step * tuning.WispWidth3, tuning.WispHeight3 + step * tuning.WispHeight4, tuning.WispShape2, tuning.WispColour2, tuning.WispAlpha2 * opacity,
                tuning.WispLayer2, angle, sortY: root.Y, emitter: "WarriorSpellEffects.Avalanche.7");

            // Soft floor mist trails each eruption; no drawn guide lanes.
            for (int fog = 0; fog < 2; fog++)
            {
                float drift = age * (12 + fog * 8);
                Vector2 plume = root + ProjectGround(side * ((fog == 0 ? -1 : 1) * (5 + drift))) -
                    Vector2.UnitY * (3 + age * 8);
                frame.Clip(plume, 20 + age * 30 + step * 2, 10 + age * 12,
                    Shape.SmokeSequence, age * 2.2f + frame.Unit(index * 17 + fog),
                    fog == 0 ? 0xB4E8FF : 0xD5ECFF, opacity * .30f,
                    Layer.Smoke, sortY: root.Y, emitter: "WarriorSpellEffects.Avalanche.IceFog");
            }

            for (int blade = -1; blade <= tuning.BladeCount; blade++)
            {
                Vector2 foot = root + ProjectGround(side * (blade * (tuning.Foot + step * tuning.Foot2)) - heading * Math.Abs(blade) * tuning.Foot3);
                float height = (tuning.Height + step * tuning.Height2 + frame.Unit(index * 3 + blade + 17) * tuning.Height3 - Math.Abs(blade) * tuning.Height4) * rise;
                Vector2 tip = foot - ProjectGround(heading * (height * tuning.Tip)) - Vector2.UnitY * height;
                Vector2 edge = tip - foot;
                float tilt = MathF.Atan2(edge.Y, edge.X) + MathF.PI / 2;
                float span = edge.Length();
                frame.Sprite((foot + tip) * tuning.IceSpireOffset, tuning.IceSpireWidth + step * tuning.IceSpireWidth2, span, tuning.IceSpireShape, blade == 0 ? 0x87DFFF : blade % 2 == 0 ? 0x679BD9 : 0xA5CFFF,
                    tuning.IceSpireAlpha * opacity, rotation: tilt, light: blade == 0, sortY: foot.Y, emitter: "WarriorSpellEffects.Avalanche.8");
                frame.Sprite(Vector2.Lerp(foot, tip, tuning.Amount), tuning.IceSpireWidth3 + step, span * tuning.IceSpireHeight, tuning.IceSpireShape2, White,
                    tuning.IceSpireAlpha2 * opacity, tuning.IceSpireLayer, tilt, sortY: foot.Y, emitter: "WarriorSpellEffects.Avalanche.9");
                // Use the same pointed texture aligned to the upper third:
                // the violet cap follows the growing spire instead of floating.
                if (span > 3)
                    frame.Sprite(Vector2.Lerp(foot, tip, .82f),
                        (tuning.IceSpireWidth + step * tuning.IceSpireWidth2) * .55f, span * .36f,
                        tuning.IceSpireShape, (index + blade) % 2 == 0 ? 0xA295FF : 0x796BDB,
                        opacity * .76f, tuning.IceSpireLayer, tilt, sortY: foot.Y,
                        emitter: "WarriorSpellEffects.Avalanche.VioletTips");
                if (blade == 0)
                    frame.Sprite(tip + new Vector2(tuning.X, tuning.Y), tuning.WarriorSparkWidth + step, tuning.WarriorSparkHeight, tuning.WarriorSparkShape, tuning.WarriorSparkColour, tuning.WarriorSparkAlpha * opacity,
                        tuning.WarriorSparkLayer, tilt + tuning.WarriorSparkRotation, sortY: foot.Y, emitter: "WarriorSpellEffects.Avalanche.10");
            }
        }
    }

    private static void Burst(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Burst);
        Vector2 forward = GroundDirection(facing);
        var side = new Vector2(-facing.Y, facing.X);
        float angle = MathF.Atan2(facing.Y, facing.X);
        Vector2 launch = feet - Vector2.UnitY * tuning.Launch;
        Vector2 tip = launch + forward * (tuning.Tip + tuning.Tip2 * Ease(progress * tuning.Tip3));
        Vector2 centre = Vector2.Lerp(launch, tip, tuning.Centre);
        float span = Vector2.Distance(launch, tip);
        frame.Sprite(centre, span + tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, Ember, tuning.OrbAlpha, rotation: angle, light: tuning.OrbLight, emitter: "WarriorSpellEffects.Burst.1");
        frame.Clip(centre - side * tuning.SmokeSequenceOffset, span + tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, tuning.SmokeSequencePhase + progress * tuning.SmokeSequencePhase2,
            tuning.SmokeSequenceColour, tuning.SmokeSequenceAlpha, tuning.SmokeSequenceLayer, angle, emitter: "WarriorSpellEffects.Burst.2");
        frame.Clip(centre, tuning.FlameSequenceWidth, span + tuning.FlameSequenceHeight, tuning.FlameSequenceShape, progress * tuning.FlameSequencePhase, Flame,
            tuning.FlameSequenceAlpha, rotation: angle + MathF.PI / 2, light: tuning.FlameSequenceLight, emitter: "WarriorSpellEffects.Burst.3");
        frame.Sprite(centre - side * tuning.BladeEdgeOffset, span + tuning.BladeEdgeWidth, tuning.BladeEdgeHeight, tuning.BladeEdgeShape, Heat, tuning.BladeEdgeAlpha, tuning.BladeEdgeLayer, angle - tuning.BladeEdgeRotation, emitter: "WarriorSpellEffects.Burst.4");
        frame.Sprite(tip - forward * tuning.WarriorImpactOffset, tuning.WarriorImpactWidth, tuning.WarriorImpactHeight, tuning.WarriorImpactShape, Flame, tuning.WarriorImpactAlpha, rotation: angle + tuning.WarriorImpactRotation, light: tuning.WarriorImpactLight, emitter: "WarriorSpellEffects.Burst.5");
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float lift = (frame.Unit(i + 18) - tuning.Lift) * tuning.Lift2;
            Vector2 tail = Vector2.Lerp(launch, tip, frame.Unit(i) * tuning.Tail) + side * lift;
            frame.Line(tail, tip + side * (lift * tuning.LineEnd), tuning.LineWidth + frame.Unit(i + 34) * tuning.LineWidth2, Heat, tuning.LineAlpha, emitter: "WarriorSpellEffects.Burst.6");
        }
        frame.Sparks(tip - forward * tuning.WarriorSparkOffset, tuning.WarriorSparkCount, tuning.WarriorSparkRx, tuning.WarriorSparkRy, progress, Heat, rise: tuning.WarriorSparkRise, shape: tuning.WarriorSparkShape, emitter: "WarriorSpellEffects.Burst.7");
    }

    private static void Counter(Frame frame, Vector2 feet, Vector2 facing, float progress)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Counter);
        // The original two-frame flash occupies the first fifth of the ten-frame riposte.
        float flash = 1 - Ease(progress * tuning.Flash);
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        frame.Sprite(body, tuning.FlameTongueWidth, tuning.FlameTongueHeight, tuning.FlameTongueShape, Ember, tuning.FlameTongueAlpha * flash, light: tuning.FlameTongueLight, emitter: "WarriorSpellEffects.Counter.1");
        frame.Sprite(body, tuning.WarriorImpactWidth, tuning.WarriorImpactHeight, tuning.WarriorImpactShape, Heat, tuning.WarriorImpactAlpha * flash, tuning.WarriorImpactLayer, tuning.WarriorImpactRotation, emitter: "WarriorSpellEffects.Counter.2");
        frame.Clip(body + Vector2.UnitY * tuning.FlameSequenceOffset, tuning.FlameSequenceWidth, tuning.FlameSequenceHeight, tuning.FlameSequenceShape, progress * tuning.FlameSequencePhase, Flame, flash * tuning.FlameSequenceAlpha, emitter: "WarriorSpellEffects.Counter.3");
        // The sword trace takes over through the swing. Carry a little heat between the flash and cut.
        float trail = Ease((progress - tuning.Trail) / tuning.TrailDivisor);
        float afterglow = (tuning.Afterglow + tuning.Afterglow2 * flash) * (1 - Ease((progress - tuning.Afterglow3) / tuning.AfterglowDivisor));
        Vector2 edge = feet + facing * (tuning.Edge + progress * tuning.Edge2) - Vector2.UnitY * tuning.Edge3;
        float rotation = MathF.Atan2(facing.Y, facing.X);
        frame.Sprite(body, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, Ember, afterglow, tuning.WispLayer, light: tuning.WispLight, emitter: "WarriorSpellEffects.Counter.4");
        frame.Clip(edge - facing * tuning.FireSequenceOffset, tuning.FireSequenceWidth, tuning.FireSequenceHeight, tuning.FireSequenceShape, progress * tuning.FireSequencePhase, Ember, tuning.FireSequenceAlpha * trail, rotation: rotation, emitter: "WarriorSpellEffects.Counter.5");
        frame.Sprite(edge, tuning.WarriorCrescentWidth, tuning.WarriorCrescentHeight, tuning.WarriorCrescentShape, Flame, tuning.WarriorCrescentAlpha * trail,
            rotation: rotation - tuning.WarriorCrescentRotation, light: tuning.WarriorCrescentLight, emitter: "WarriorSpellEffects.Counter.6");
        frame.Sparks(edge, tuning.WarriorSparkCount, tuning.WarriorSparkRx * trail, tuning.WarriorSparkRy, progress, Heat, tuning.WarriorSparkRise, tuning.WarriorSparkShape, emitter: "WarriorSpellEffects.Counter.7");
    }

    private static void Snare(Frame frame, Vector2 feet, float progress, bool preparing)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Snare);
        float sweep = progress * MathF.Tau * (preparing ? tuning.Sweep : tuning.Sweep2);
        float extent = tuning.Extent + tuning.Extent2 * Ease(progress * tuning.Extent3);
        float release = preparing ? 1 : 1 - Ease((progress - tuning.Release) / tuning.ReleaseDivisor);
        frame.Sprite(feet, tuning.WispWidth * extent, tuning.WispHeight * extent, tuning.WispShape, tuning.WispColour, tuning.WispAlpha, tuning.WispLayer, light: tuning.WispLight, emitter: "WarriorSpellEffects.Snare.1");
        for (int i = 0; i < tuning.SmokeSequenceCount; i++)
        {
            float angle = sweep + i * MathF.Tau / tuning.AngleDivisor;
            float edge = extent * (tuning.Edge + frame.Unit(i + 14) * tuning.Edge2);
            Vector2 ground = feet + Ellipse(angle, tuning.EllipseX, tuning.EllipseY) * edge;
            float tangent = MathF.Atan2(MathF.Cos(angle) * tuning.Tangent, -MathF.Sin(angle) * tuning.Tangent2);
            frame.Clip(ground - Vector2.UnitY * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight, tuning.SmokeSequenceShape, progress + i * tuning.SmokeSequencePhase,
                i % 2 == 0 ? tuning.SmokeSequenceColour : tuning.SmokeSequenceColour2, tuning.SmokeSequenceAlpha * release, tuning.SmokeSequenceLayer, tangent, emitter: "WarriorSpellEffects.Snare.2");
            if (preparing) continue;
            float bind = 1 - Ease((progress - tuning.Bind) / tuning.BindDivisor);
            float climb = Fract(progress * tuning.Climb + i * tuning.Climb2);
            Vector2 point = feet + Ellipse(angle, tuning.EllipseX2 - climb * tuning.EllipseX3, tuning.EllipseY2) - Vector2.UnitY * (tuning.Point + climb * tuning.Point2);
            Layer layer = MathF.Sin(angle) < 0 ? tuning.Layer : tuning.Layer2;
            frame.Clip(point, tuning.SmokeSequenceWidth2 - climb * tuning.SmokeSequenceWidth3, tuning.SmokeSequenceHeight2 - climb * tuning.SmokeSequenceHeight3, tuning.SmokeSequenceShape2, progress + i * tuning.SmokeSequencePhase2,
                i % 2 == 0 ? tuning.SmokeSequenceColour3 : tuning.SmokeSequenceColour4, tuning.SmokeSequenceAlpha2 * bind * (1 - climb * tuning.SmokeSequenceAlpha3), layer, tangent * tuning.SmokeSequenceRotation, emitter: "WarriorSpellEffects.Snare.3");
            frame.Sprite(point + Vector2.UnitY * tuning.WispOffset, tuning.WispWidth2 - climb * tuning.WispWidth3, tuning.WispHeight2 - climb * tuning.WispHeight3, tuning.WispShape2,
                tuning.WispColour2, tuning.WispAlpha2 * bind, layer, tangent * tuning.WispRotation, emitter: "WarriorSpellEffects.Snare.4");
        }
    }

    private static void Roar(Frame frame, Vector2 feet, float progress, bool rally)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Roar);
        int tint = rally ? tuning.TintColour : tuning.TintColour2;
        float spread = tuning.Spread + tuning.Spread2 * Ease(progress * tuning.Spread3);
        Vector2 head = feet - Vector2.UnitY * tuning.Head;
        frame.Sprite(feet, tuning.WarriorRingWidth * spread, tuning.WarriorRingHeight * spread, tuning.WarriorRingShape, tint, tuning.WarriorRingAlpha, tuning.WarriorRingLayer, light: tuning.WarriorRingLight, emitter: "WarriorSpellEffects.Roar.1");
        frame.Sprite(head, tuning.WarriorImpactWidth * spread, tuning.WarriorImpactHeight * spread, tuning.WarriorImpactShape, tint, tuning.WarriorImpactAlpha, light: tuning.WarriorImpactLight, emitter: "WarriorSpellEffects.Roar.2");
        frame.Sprite(head, tuning.WispWidth * spread, tuning.WispHeight * spread, tuning.WispShape, rally ? tuning.WispColour : tuning.WispColour2, tuning.WispAlpha, tuning.WispLayer, emitter: "WarriorSpellEffects.Roar.3");
        frame.Sprite(head, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, White, tuning.FlashAlpha * (1 - progress * tuning.FlashAlpha2), tuning.FlashLayer, emitter: "WarriorSpellEffects.Roar.4");
        for (int i = 0; i < tuning.WispCount; i++)
        {
            float angle = (i + frame.Unit(i) * tuning.Angle) * MathF.Tau / tuning.AngleDivisor;
            Vector2 ray = Ellipse(angle, tuning.EllipseX, tuning.EllipseY) * (spread * (tuning.Ray + frame.Unit(i + 24) * tuning.Ray2));
            Vector2 edge = head + ray;
            Vector2 bend = head + ray * tuning.Bend + Ellipse(angle + tuning.EllipseAngle, tuning.EllipseX2, tuning.EllipseY2) * (frame.Unit(i + 67) - tuning.Bend2);
            Layer layer = Depth(angle);
            frame.Sprite(bend, ray.Length() * tuning.WispWidth2, tuning.WispHeight2 + frame.Unit(i + 44) * tuning.WispHeight3,
                tuning.WispShape2, tint, tuning.WispAlpha2, layer, MathF.Atan2(ray.Y, ray.X), emitter: "WarriorSpellEffects.Roar.5");
            frame.Thread(head + ray * tuning.ThreadStart, bend, tuning.ThreadStartWidth, tuning.ThreadEndWidth, i % 3 == 0 ? White : tint, tuning.ThreadAlpha, layer, emitter: "WarriorSpellEffects.Roar.6");
            frame.Thread(bend, edge, tuning.ThreadStartWidth2, tuning.ThreadEndWidth2, White, tuning.ThreadAlpha2, layer == Layer.Ground ? layer : tuning.ThreadLayer, emitter: "WarriorSpellEffects.Roar.7");
            if (i % 2 == 0)
                frame.Sprite(edge, tuning.SparkWidth + frame.Unit(i) * tuning.SparkWidth2, tuning.SparkHeight, tuning.SparkShape, White, tuning.SparkAlpha, layer, angle, emitter: "WarriorSpellEffects.Roar.8");
        }
    }

    private static void Surge(Frame frame, Vector2 feet, float progress, bool rage)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Surge);
        Vector2 centre = feet - Vector2.UnitY * (rage ? tuning.Centre : tuning.Centre2);
        int tint = rage ? Ember : Steel;
        int core = rage ? Heat : White;
        float spread = tuning.Spread + tuning.Spread2 * Ease(progress * tuning.Spread3);
        frame.Sprite(centre, (rage ? tuning.OrbWidth : tuning.OrbWidth2) * spread, (rage ? tuning.OrbHeight : tuning.OrbHeight2) * spread, tuning.OrbShape, tint,
            tuning.OrbAlpha, light: tuning.OrbLight, emitter: "WarriorSpellEffects.Surge.1");
        frame.Sprite(centre, tuning.FlashWidth * spread, tuning.FlashHeight * spread, tuning.FlashShape, core, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight, emitter: "WarriorSpellEffects.Surge.2");
        for (int i = 0; i < tuning.BladeEdgeCount; i++)
        {
            float angle = (i + frame.Unit(i) * tuning.Angle) * MathF.Tau / tuning.AngleDivisor;
            float length = spread * (tuning.Length + frame.Unit(i + 33) * tuning.Length2);
            Vector2 ray = Ellipse(angle, rage ? tuning.EllipseX : tuning.EllipseX2, rage ? tuning.EllipseY : tuning.EllipseY2) * length;
            frame.Sprite(centre + ray * tuning.BladeEdgeOffset, ray.Length() + tuning.BladeEdgeWidth, tuning.BladeEdgeHeight + frame.Unit(i + 11) * tuning.BladeEdgeHeight2,
                tuning.BladeEdgeShape, i % 3 == 0 ? core : tint, tuning.BladeEdgeAlpha, Depth(angle), MathF.Atan2(ray.Y, ray.X), emitter: "WarriorSpellEffects.Surge.3");
        }
        frame.Sparks(centre, tuning.WarriorSparkCount, rage ? tuning.WarriorSparkRx : tuning.WarriorSparkRx2, tuning.WarriorSparkRy, progress, core, rise: tuning.WarriorSparkRise, shape: tuning.WarriorSparkShape, emitter: "WarriorSpellEffects.Surge.4");
    }

    private static void BladeFlame(Frame frame, Vector2 point, float progress, float width)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.BladeFlame);
        frame.Clip(point - Vector2.UnitY * tuning.FireSequenceOffset, width, width * tuning.FireSequenceHeight, tuning.FireSequenceShape, progress * tuning.FireSequencePhase, Flame, tuning.FireSequenceAlpha, light: tuning.FireSequenceLight, emitter: "WarriorSpellEffects.BladeFlame.1");
        frame.Clip(point, width * tuning.FireSequenceWidth, width * tuning.FireSequenceHeight2, tuning.FireSequenceShape2, progress * tuning.FireSequencePhase2 + tuning.FireSequencePhase3, Heat, tuning.FireSequenceAlpha2, tuning.FireSequenceLayer, emitter: "WarriorSpellEffects.BladeFlame.2");
        frame.Sparks(point, tuning.WarriorSparkCount, width * tuning.WarriorSparkRx, width * tuning.WarriorSparkRy, progress, Heat, rise: tuning.WarriorSparkRise, shape: tuning.WarriorSparkShape, emitter: "WarriorSpellEffects.BladeFlame.3");
    }

    private static void Fury(Frame frame, Vector2 feet, float progress, bool aura)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Fury);
        if (aura)
        {
            Vector2 eye = feet - Vector2.UnitY * tuning.Eye;
            frame.Sprite(eye, tuning.WarriorRingWidth, tuning.WarriorRingHeight, tuning.WarriorRingShape, Flame, tuning.WarriorRingAlpha, light: tuning.WarriorRingLight, emitter: "WarriorSpellEffects.Fury.1");
            frame.Sprite(eye, tuning.SigilEyeWidth, tuning.SigilEyeHeight, tuning.SigilEyeShape, Ember, tuning.SigilEyeAlpha + tuning.SigilEyeAlpha2 * MathF.Sin(progress * MathF.Tau), light: tuning.SigilEyeLight, emitter: "WarriorSpellEffects.Fury.2");
            frame.Sprite(eye, tuning.WarriorSparkWidth, tuning.WarriorSparkHeight, tuning.WarriorSparkShape, Heat, tuning.WarriorSparkAlpha, tuning.WarriorSparkLayer, emitter: "WarriorSpellEffects.Fury.3");
            frame.Clip(eye - Vector2.UnitY * tuning.FireSequenceOffset, tuning.FireSequenceWidth, tuning.FireSequenceHeight, tuning.FireSequenceShape, progress, Flame, tuning.FireSequenceAlpha, emitter: "WarriorSpellEffects.Fury.4");
            return;
        }

        for (int side = -1; side <= tuning.SideCount; side += tuning.SampleStride)
        {
            Vector2 plume = feet + new Vector2(side * tuning.X, tuning.Y);
            frame.Clip(plume, tuning.FlameSequenceWidth, tuning.FlameSequenceHeight, tuning.FlameSequenceShape, progress + (side + 1) * tuning.FlameSequencePhase, Ember,
                tuning.FlameSequenceAlpha, side < 0 ? tuning.FlameSequenceLayer : tuning.FlameSequenceLayer2, side * tuning.FlameSequenceRotation, light: tuning.FlameSequenceLight, emitter: "WarriorSpellEffects.Fury.5");
            frame.Clip(plume + new Vector2(-side * tuning.X2, tuning.Y2), tuning.FireSequenceWidth2, tuning.FireSequenceHeight2, tuning.FireSequenceShape2, progress + tuning.FireSequencePhase, Heat,
                tuning.FireSequenceAlpha2, tuning.FireSequenceLayer, side * tuning.FireSequenceRotation, emitter: "WarriorSpellEffects.Fury.6");
        }
        frame.Sparks(feet - Vector2.UnitY * tuning.WarriorSparkOffset, tuning.WarriorSparkCount, tuning.WarriorSparkRx, tuning.WarriorSparkRy, progress, Heat, rise: tuning.WarriorSparkRise, shape: tuning.WarriorSparkShape2, emitter: "WarriorSpellEffects.Fury.7");
    }

    private static void Skin(Frame frame, Vector2 feet, float progress, bool aura)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.Skin);
        Vector2 body = feet - Vector2.UnitY * tuning.Body;
        if (aura)
        {
            frame.Sprite(feet, tuning.WarriorRingWidth, tuning.WarriorRingHeight, tuning.WarriorRingShape, tuning.WarriorRingColour, tuning.WarriorRingAlpha, tuning.WarriorRingLayer, emitter: "WarriorSpellEffects.Skin.1");
            frame.Sprite(body, tuning.OrbWidth, tuning.OrbHeight, tuning.OrbShape, tuning.OrbColour, tuning.OrbAlpha, tuning.OrbLayer, light: tuning.OrbLight, emitter: "WarriorSpellEffects.Skin.2");
            for (int i = tuning.I; i <= tuning.FlameTongueCount; i++)
            {
                float wave = MathF.Sin((progress + i * tuning.Wave) * MathF.Tau);
                Vector2 rise = body + new Vector2(i * tuning.X, tuning.Y - Math.Abs(i) * tuning.Y2);
                float tilt = i * tuning.Tilt + wave * tuning.Tilt2;
                float height = tuning.Height - Math.Abs(i) * tuning.Height2;
                frame.Sprite(rise, tuning.FlameTongueWidth, height, tuning.FlameTongueShape, i % 2 == 0 ? Gold : tuning.FlameTongueColour,
                    tuning.FlameTongueAlpha + wave * tuning.FlameTongueAlpha2, i % 2 == 0 ? tuning.FlameTongueLayer : tuning.FlameTongueLayer2, tilt, light: tuning.FlameTongueLight, emitter: "WarriorSpellEffects.Skin.3");
                frame.Sprite(rise + Vector2.UnitY * tuning.FlameTongueOffset, tuning.FlameTongueWidth2, height * tuning.FlameTongueHeight, tuning.FlameTongueShape2, PaleGold,
                    tuning.FlameTongueAlpha3 + wave * tuning.FlameTongueAlpha4, tuning.FlameTongueLayer3, tilt, emitter: "WarriorSpellEffects.Skin.4");
            }
            return;
        }

        Vector2 overhead = feet - Vector2.UnitY * (tuning.Overhead - tuning.Overhead2 * Ease(progress * tuning.Overhead3));
        frame.Sprite(overhead, tuning.WarriorImpactWidth, tuning.WarriorImpactHeight, tuning.WarriorImpactShape, Gold, tuning.WarriorImpactAlpha, rotation: tuning.WarriorImpactRotation, light: tuning.WarriorImpactLight, emitter: "WarriorSpellEffects.Skin.5");
        frame.Sprite(overhead, tuning.FlashWidth, tuning.FlashHeight, tuning.FlashShape, PaleGold, tuning.FlashAlpha, tuning.FlashLayer, light: tuning.FlashLight, emitter: "WarriorSpellEffects.Skin.6");
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            float spread = (i - tuning.Spread) * tuning.Spread2;
            Vector2 end = feet + new Vector2(spread, tuning.Y3 - frame.Unit(i) * tuning.Y4);
            frame.Line(overhead + new Vector2(spread * tuning.X2, tuning.Y5), end, tuning.LineWidth + frame.Unit(i + 15) * tuning.LineWidth2,
                i % 3 == 0 ? PaleGold : Gold, tuning.LineAlpha, i % 2 == 0 ? tuning.LineLayer : tuning.LineLayer2, emitter: "WarriorSpellEffects.Skin.7");
        }
        frame.Sprite(feet, tuning.WarriorRingWidth2, tuning.WarriorRingHeight2, tuning.WarriorRingShape2, Gold, tuning.WarriorRingAlpha2, tuning.WarriorRingLayer2, emitter: "WarriorSpellEffects.Skin.8");
    }
}
