using System.Numerics;
using CrystalM2.Rendering.Core;
using Effect = Client.MirScenes.SignatureSpellEffects.Effect;
using Frame = Client.MirScenes.SpellParticleFrame;
using Look = Client.MirScenes.SignatureSpellEffects.Look;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using static Client.MirScenes.SpellParticleFrame;

namespace Client.MirScenes;

internal static partial class WarriorSpellEffects
{
    internal static bool UsesWeaponArt(Spell spell) => spell is Spell.Slaying or Spell.Thrusting or Spell.HalfMoon or
        Spell.TwinDrakeBlade or Spell.CrossHalfMoon or Spell.FlamingSword or Spell.BladeAvalanche or Spell.CounterAttack or Spell.SlashingBurst;

    internal static void WeaponSweep(SpellParticlePool pool, Spell spell, Look look, WeaponMaskTrace pose, WeaponMaskTrace? previous,
        Vector2 facing, Vector2 feet, long now, int remaining, int interval, float progress, float previousProgress,
        bool followThrough)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.WeaponSweep);
        int life = Math.Min(remaining, Math.Clamp(interval * tuning.Life, 55, 190));
        if (life <= 0) return;
        Vector2 blade = pose.Tip - pose.Hilt;
        if (blade.LengthSquared() < 9) return;
        if (facing == Vector2.Zero) facing = Vector2.Normalize(blade);
        float angle = MathF.Atan2(facing.Y, facing.X);
        var effect = new Effect(pool, look, now, window: life, referenceDuration: life, depthY: feet.Y);
        if (spell == Spell.TwinDrakeBlade && followThrough && previousProgress == 0)
        {
            // Releasing the stored charge: several short, hairline arcs peel
            // from the blade itself, rather than one huge 67px lightning card.
            var returnCut = new Frame(pool, feet, now, 1, Frame.ReferenceSize, (uint)now,
                scalePositions: false, depthY: feet.Y);
            Vector2 normal = Vector2.Normalize(new Vector2(-blade.Y, blade.X));
            for (int i = 0; i < tuning.SparkCount; i++)
            {
                float along = tuning.Along + i * tuning.Along2;
                Vector2 root = pose.Hilt + blade * along;
                Vector2 end = root + facing * (tuning.End + returnCut.Unit(i + 19) * tuning.End2) +
                    normal * ((returnCut.Unit(i + 41) - tuning.End3) * tuning.End4);
                bool rear = i % 4 == 0;
                Layer layer = rear ? tuning.Layer : tuning.Layer2;
                float groundY = feet.Y + (rear ? tuning.GroundY : tuning.GroundY2);
                returnCut.Thread(root, end, tuning.ThreadStartWidth, tuning.ThreadEndWidth, i % 3 == 0 ? White : Ice,
                    tuning.ThreadAlpha, layer, sortY: groundY);
                returnCut.Sprite(root, tuning.SparkWidth + returnCut.Unit(i + 61) * tuning.SparkWidth2,
                    tuning.SparkHeight + returnCut.Unit(i + 63) * tuning.SparkHeight2, tuning.SparkShape,
                    i % 3 == 0 ? White : Ice, tuning.SparkAlpha, layer, sortY: groundY);
            }
        }
        bool heavy = spell is Spell.FlamingSword or Spell.CrossHalfMoon or Spell.BladeAvalanche or Spell.SlashingBurst;
        bool moon = spell is Spell.HalfMoon or Spell.CrossHalfMoon;
        if (previous is { } old)
        {
            Vector2 delta = pose.Tip - old.Tip;
            if (delta.LengthSquared() is > 3 and < 19600 && Vector2.DistanceSquared(old.Hilt, pose.Hilt) < 3600)
            {
                float inner = moon ? tuning.Inner : spell == Spell.TwinDrakeBlade ? tuning.Inner2 :
                    spell is Spell.Slaying or Spell.Thrusting ? tuning.Inner3 : tuning.Inner4;
                float outer = moon || spell == Spell.TwinDrakeBlade ? tuning.Outer : heavy ? tuning.Outer2 : tuning.Outer3;
                Layer depth = facing.Y < -.27f && (old.Tip.Y + pose.Tip.Y) * tuning.Depth < pose.Hilt.Y - tuning.Depth2
                    ? tuning.Depth3 : tuning.Depth4;
                if (spell == Spell.TwinDrakeBlade)
                    DrakeEdgeSweep(pool, old, pose, feet.Y, now, life);
                else
                {
                    SweepRibbon(pool, old, pose, inner, outer, depth, feet.Y, look.Colour,
                        now, life, moon ? tuning.Alpha : heavy ? tuning.Alpha2 : tuning.Alpha3);
                    if (!moon && heavy)
                        SweepRibbon(pool, old, pose, tuning.Inner5, tuning.Outer4, depth, feet.Y, look.Core,
                            now, Math.Min(life, tuning.Val2), tuning.Alpha4);
                }
            }
        }

        if (progress < .23f || progress > .87f) return;
        bool cut = previousProgress < .38f && progress >= .38f;
        Vector2 forwardTip = pose.Hilt + facing * MathF.Max(tuning.ForwardTip, Vector2.Dot(blade, facing));
        switch (spell)
        {
            case Spell.Slaying:
                effect.Add(forwardTip, facing * tuning.SlayingOrbVelocity, tuning.SlayingOrbShape, tuning.SlayingOrbSize, life, tuning.SlayingOrbLayer, aspect: tuning.SlayingOrbAspect, alpha: tuning.SlayingOrbAlpha);
                effect.Add(forwardTip + facing * tuning.SlayingWarriorTripleCutOffset, facing * tuning.SlayingWarriorTripleCutVelocity, tuning.SlayingWarriorTripleCutShape, tuning.SlayingWarriorTripleCutSize, life,
                    tuning.SlayingWarriorTripleCutLayer, aspect: tuning.SlayingWarriorTripleCutAspect, rotation: angle + tuning.SlayingWarriorTripleCutRotation, core: tuning.SlayingWarriorTripleCutCore, alpha: tuning.SlayingWarriorTripleCutAlpha);
                effect.Scatter(forwardTip, tuning.SlayingSparkCount, tuning.SlayingSparkShape, tuning.SlayingSparkSpread, life);
                break;
            case Spell.HalfMoon when cut:
                ProjectCrescent(pool, look, feet, facing, 0, tuning.HalfMoonLift, tuning.HalfMoonRadius, tuning.HalfMoonWidth, now, life);
                break;
            case Spell.CrossHalfMoon when cut:
                ProjectCrescent(pool, look, feet, facing, tuning.CrossHalfMoonYaw, tuning.CrossHalfMoonLift, tuning.CrossHalfMoonRadius, tuning.CrossHalfMoonWidth, now, life, sweep: tuning.CrossHalfMoonSweep, stacked: true);
                ProjectCrescent(pool, look, feet, facing, MathF.PI - tuning.CrossHalfMoonYaw2, tuning.CrossHalfMoonLift2, tuning.CrossHalfMoonRadius2, tuning.CrossHalfMoonWidth2, now, life, sweep: tuning.CrossHalfMoonSweep2, stacked: true);
                effect.Add(feet + facing * tuning.CrossHalfMoonWarriorSparkOffset - Vector2.UnitY * tuning.CrossHalfMoonWarriorSparkOffset2, facing * tuning.CrossHalfMoonWarriorSparkVelocity, tuning.CrossHalfMoonWarriorSparkShape, tuning.CrossHalfMoonWarriorSparkSize, life,
                    tuning.CrossHalfMoonWarriorSparkLayer, aspect: tuning.CrossHalfMoonWarriorSparkAspect, rotation: angle, core: tuning.CrossHalfMoonWarriorSparkCore, alpha: tuning.CrossHalfMoonWarriorSparkAlpha);
                break;
            case Spell.TwinDrakeBlade when cut:
                // The attack peak is a fragmented electrical cut throughout
                // the moving weapon's length. Fine tendrils keep its outline
                // without solid lines, rings or a tip-only lightning blob.
                var strike = new Frame(pool, feet, now, 1, Frame.ReferenceSize, (uint)now,
                    scalePositions: false, depthY: feet.Y);
                Vector2 bladeNormal = Vector2.Normalize(new Vector2(-blade.Y, blade.X));
                for (int i = 0; i < tuning.TwinDrakeBladeUserLightningCount; i++)
                {
                    float along = tuning.TwinDrakeBladeAlong + tuning.TwinDrakeBladeAlong2 * i / tuning.TwinDrakeBladeAlongDivisor;
                    Vector2 root = pose.Hilt + blade * along;
                    Vector2 tendril = root + facing * (tuning.TwinDrakeBladeTendril + strike.Unit(i + 13) * tuning.TwinDrakeBladeTendril2) +
                        bladeNormal * ((strike.Unit(i + 29) - tuning.TwinDrakeBladeTendril3) * tuning.TwinDrakeBladeTendril4);
                    bool rear = i % 4 == 0;
                    Layer layer = rear ? tuning.TwinDrakeBladeLayer : tuning.TwinDrakeBladeLayer2;
                    float groundY = feet.Y + (rear ? tuning.TwinDrakeBladeGroundY : tuning.TwinDrakeBladeGroundY2);
                    strike.Thread(root, tendril, tuning.TwinDrakeBladeThreadStartWidth, tuning.TwinDrakeBladeThreadEndWidth,
                        i % 4 == 0 ? White : Ice, tuning.TwinDrakeBladeThreadAlpha, layer,
                        light: i == 7, sortY: groundY);
                    if (i % 2 == 0)
                    {
                        float rotation = MathF.Atan2(tendril.Y - root.Y, tendril.X - root.X);
                        strike.Sprite(Vector2.Lerp(root, tendril, tuning.TwinDrakeBladeAmount), tuning.TwinDrakeBladeUserLightningWidth, tuning.TwinDrakeBladeUserLightningHeight,
                            tuning.TwinDrakeBladeUserLightningShape, White, tuning.TwinDrakeBladeUserLightningAlpha, layer,
                            rotation, sortY: groundY);
                    }
                }
                break;
            case Spell.FlamingSword:
                AddBladeFire(pool, pose, look, now, life, progress, feet.Y);
                // The cut is now drawn by numerous narrow weapon-tracking
                // tongues and smoke fragments. Do not add a second 128px blob.
                break;
            case Spell.CounterAttack:
                effect.Add(pose.Tip, facing * tuning.CounterAttackWarriorCrescentVelocity, tuning.CounterAttackWarriorCrescentShape, tuning.CounterAttackWarriorCrescentSize, life,
                    tuning.CounterAttackWarriorCrescentLayer, aspect: tuning.CounterAttackWarriorCrescentAspect, rotation: angle - tuning.CounterAttackWarriorCrescentRotation, core: tuning.CounterAttackWarriorCrescentCore, alpha: tuning.CounterAttackWarriorCrescentAlpha);
                break;
        }

        if (spell != Spell.Thrusting)
            effect.Add(pose.Tip, facing * tuning.WarriorSparkVelocity, tuning.WarriorSparkShape, heavy ? tuning.WarriorSparkSize : tuning.WarriorSparkSize2, Math.Min(life, tuning.Val22),
                tuning.WarriorSparkLayer, aspect: tuning.WarriorSparkAspect, rotation: angle, alpha: tuning.WarriorSparkAlpha, core: tuning.WarriorSparkCore);
    }

    private static void ProjectCrescent(SpellParticlePool pool, Look look, Vector2 feet, Vector2 facing,
        float yaw, float lift, float radius, float width, long now, int life, float sweep = 2.65f,
        bool stacked = false)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.ProjectCrescent);
        Vector2 forward = GroundHeading(facing);
        float heading = MathF.Atan2(forward.Y, forward.X);
        Vector2 centre = feet - Vector2.UnitY * lift;
        int segments = tuning.Segments;

        // World depth follows the point on the angled ground plane. The visible
        // crescent is independently raised and rolled in height: using its
        // screen-space Y as its ground depth would make front/back occlusion
        // jump as the slash lifts off the floor.
        var (inner, outer, previousDepth) = Edge(0);
        for (int i = 1; i <= segments; i++)
        {
            var (nextInner, nextOuter, nextDepth) = Edge(i / (float)segments);
            float segmentDepth = (previousDepth + nextDepth) * tuning.SegmentDepth;
            bool rear = segmentDepth < feet.Y;
            Layer layer = rear ? tuning.Layer : tuning.Layer2;

            // Receding, darker rear edge and forward luminous face make the
            // swing wrap around the actor instead of reading as one flat ring.
            Color body = rear
                ? Color.FromArgb(look.Colour.R * 3 / tuning.BodyDivisor, look.Colour.G * 2 / 3, look.Colour.B * 3 / 4)
                : look.Colour;
            Color shadow = rear
                ? Color.FromArgb(look.Colour.R / 3, look.Colour.G / 3, look.Colour.B / 2)
                : Color.FromArgb(look.Colour.R / 2, look.Colour.G / 2, look.Colour.B * 3 / 4);
            Color core = rear
                ? Color.FromArgb(look.Core.R * 2 / 3, look.Core.G * 3 / 4, look.Core.B * 4 / tuning.CoreDivisor)
                : look.Core;

            AddRibbon(Vector2.Lerp(outer, inner, tuning.Amount), Vector2.Lerp(inner, outer, tuning.Amount2),
                Vector2.Lerp(nextOuter, nextInner, tuning.Amount3), Vector2.Lerp(nextInner, nextOuter, tuning.Amount4),
                layer, shadow, rear ? tuning.Alpha : tuning.Alpha2, segmentDepth);
            AddRibbon(inner, outer, nextInner, nextOuter, layer, body, tuning.Alpha3, segmentDepth);
            if (i > 2 && i < segments - 1)
                AddRibbon(Vector2.Lerp(inner, outer, tuning.Amount5), Vector2.Lerp(inner, outer, tuning.Amount6),
                    Vector2.Lerp(nextInner, nextOuter, tuning.Amount7), Vector2.Lerp(nextInner, nextOuter, tuning.Amount8),
                    rear ? tuning.Layer3 : tuning.Layer4, core, rear ? tuning.Alpha4 : tuning.Alpha5, segmentDepth);

            (inner, outer, previousDepth) = (nextInner, nextOuter, nextDepth);
        }

        // Emit small texture accents only after the ribbons so that the GPU
        // can batch the shader-driven MoonRibbon geometry without repeatedly
        // switching coverage modes.
        for (int i = 1; i < segments - tuning.UserSlashCount; i += tuning.SampleStride)
        {
            var (a, b, aDepth) = Edge(i / (float)segments);
            var (c, d, bDepth) = Edge((i + 1f) / segments);
            Vector2 tangent = (c + d) * tuning.Tangent - (a + b) * tuning.Tangent2;
            Vector2 accent = (a + b + c + d) * tuning.Accent;
            float depth = (aDepth + bDepth) * tuning.Depth;
            float length = tuning.Length + tuning.Length2 * MathF.Sin(i * MathF.PI / segments);
            Layer layer = depth < feet.Y ? tuning.Layer5 : tuning.Layer6;
            pool.TryAdd(new SpellParticle(accent, Vector2.Zero, now, Math.Min(life, tuning.Val2),
                Math.Max(1, (int)length), Color.FromArgb(depth < feet.Y ? tuning.Alpha6 : tuning.Alpha7,
                    i % 4 == 1 ? look.Core : look.Colour),
                tuning.UserSlashShape, 0, Aspect: tuning.UserSlashAspect,
                Rotation: MathF.Atan2(tangent.Y, tangent.X), Layer: layer,
                CastsLight: tuning.UserSlashCastsLight, DepthY: depth));
        }

        // The detached glints remain small and follow each fragment's
        // ground depth. Avoid returning to oversized flat arc sprites.
        for (int i = tuning.I; i < segments - tuning.WarriorSparkCount; i += tuning.SampleStride2)
        {
            var (_, outerA, depthA) = Edge(i / (float)segments);
            var (_, outerB, depthB) = Edge((i + 1f) / segments);
            Vector2 edge = (outerA + outerB) * tuning.Edge;
            Vector2 drift = (outerB - outerA) * tuning.Drift - Vector2.UnitY * tuning.Drift2;
            float depth = (depthA + depthB) * tuning.Depth2;
            Layer layer = depth < feet.Y ? tuning.Layer7 : tuning.Layer8;
            pool.TryAdd(new SpellParticle(edge, drift, now, Math.Min(life, tuning.Val22),
                tuning.WarriorSparkSize + i % 4, Color.FromArgb(depth < feet.Y ? tuning.Alpha8 : tuning.Alpha9, look.Core),
                tuning.WarriorSparkShape, 0, Aspect: tuning.WarriorSparkAspect,
                Rotation: MathF.Atan2(drift.Y, drift.X), Layer: layer,
                CastsLight: tuning.WarriorSparkCastsLight, DepthY: depth));
        }

        // A few thin trailing flecks extend in the tangent direction and
        // dissipate without adding another circular billboard behind the arc.
        for (int i = tuning.I2; i < segments - tuning.WispCount; i += tuning.SampleStride3)
        {
            float sample = (i + tuning.Sample) / segments;
            var (innerEdge, outerEdge, depth) = Edge(sample);
            var (nextInner, nextOuter, _) = Edge((i + tuning.T) / segments);
            Vector2 point = Vector2.Lerp(innerEdge, outerEdge, tuning.Point);
            Vector2 tangent = (nextInner + nextOuter - innerEdge - outerEdge) * tuning.Tangent3;
            Layer layer = depth < feet.Y ? tuning.Layer9 : tuning.Layer10;
            pool.TryAdd(new SpellParticle(point, tangent * tuning.WispVelocity - Vector2.UnitY * tuning.WispVelocity2,
                now, Math.Min(life, tuning.Val23), tuning.WispSize + i % 3,
                Color.FromArgb(depth < feet.Y ? tuning.Alpha10 : tuning.Alpha11, look.Core),
                tuning.WispShape, 0, Aspect: tuning.WispAspect,
                Rotation: MathF.Atan2(tangent.Y, tangent.X), Layer: layer,
                CastsLight: tuning.WispCastsLight, DepthY: depth));
        }

        if (stacked)
        {
            // Cross Half Moon has multiple short cutting strokes at different
            // elevations instead of a single uniformly flat crescent sheet.
            // Reuse the same projected arc so every stroke inherits the right
            // front/back row; Half Moon keeps its original single-volume cut.
            for (int i = tuning.I3; i < segments - tuning.UserSlashCount2; i += tuning.SampleStride4)
            {
                float t = (i + tuning.T2) / segments;
                var (innerA, outerA, depthA) = Edge(t);
                var (innerB, outerB, depthB) = Edge(MathF.Min(1, t + tuning.Y));
                Vector2 centreStroke = (innerA + outerA + innerB + outerB) * tuning.CentreStroke;
                Vector2 tangent = (innerB + outerB - innerA - outerA) * tuning.Tangent4;
                if (tangent.LengthSquared() < .01f) continue;
                Vector2 normal = Vector2.Normalize(new Vector2(-tangent.Y, tangent.X));
                float orientation = MathF.Atan2(tangent.Y, tangent.X);
                float depth = (depthA + depthB) * tuning.Depth3;
                for (int tier = 0; tier < tuning.TierCount; tier++)
                {
                    float vertical = tier switch { 0 => tuning.Factor5, 1 => 0f, _ => tuning.Factor6 };
                    float lateral = (tier - 1) * tuning.Lateral + ((i % 3) - 1) * tuning.Lateral2;
                    Vector2 point = centreStroke + Vector2.UnitY * vertical +
                        normal * lateral + tangent * ((tier - 1) * tuning.Point2);
                    float strokeDepth = depth + (tier - 1) * tuning.StrokeDepth;
                    bool rear = strokeDepth < feet.Y;
                    float length = tuning.Length3 + tier * tuning.Length4 + tuning.Length5 * MathF.Sin(t * MathF.PI);
                    int alpha = tier == 1 ? tuning.Alpha12 : rear ? tuning.Alpha13 : tuning.Alpha14;
                    pool.TryAdd(new SpellParticle(point,
                        tangent * (tuning.UserSlashVelocity + tier * tuning.UserSlashVelocity2), now, Math.Min(life, tuning.Val24),
                        (int)MathF.Round(length), Color.FromArgb(alpha,
                            tier == 1 ? look.Core : look.Colour),
                        tuning.UserSlashShape2, 0, Aspect: tier == 1 ? tuning.UserSlashAspect2 : tuning.UserSlashAspect3,
                        Rotation: orientation + (tier - 1) * tuning.UserSlashRotation,
                        Layer: rear ? tuning.UserSlashLayer : tuning.UserSlashLayer2,
                        CastsLight: tuning.UserSlashCastsLight2, DepthY: strokeDepth));
                }
            }
        }

        (Vector2 Inner, Vector2 Outer, float GroundDepth) Edge(float t)
        {
            float angle = heading + yaw + (t - .5f) * sweep;
            Vector2 ray = Ellipse(angle, 1, 1);
            float crest = MathF.Sin(t * MathF.PI);
            float profile = MathF.Pow(MathF.Max(0, crest), tuning.CrestPower);
            float taper = tuning.EdgeTipWidth + width * profile * (tuning.EdgeWidthScale - tuning.EdgeTaper * t);

            // The original angled ground projection remains authoritative for
            // sorting. Rear surfaces are higher; the near surface drops into
            // the foreground, with a rolled cross-section instead of a plane.
            float groundDepth = feet.Y + ProjectGround(ray * radius).Y;
            float rise = lift + tuning.CrestLift * crest * crest + tuning.NearSideLift * (1 - ray.Y);
            float bevel = (tuning.BevelBase + tuning.BevelCrest * crest) * (tuning.BevelFloor + tuning.BevelNearSide * MathF.Abs(ray.Y));
            Vector2 inside = feet + ProjectGround(ray * (radius - taper * tuning.InnerRadiusScale))
                - Vector2.UnitY * (rise + bevel);
            Vector2 outside = feet + ProjectGround(ray * (radius + taper * tuning.OuterRadiusScale))
                - Vector2.UnitY * (rise - bevel);
            return (inside, outside, groundDepth);
        }

        void AddRibbon(Vector2 fromInner, Vector2 fromOuter, Vector2 toInner, Vector2 toOuter,
            Layer layer, Color tint, int alpha, float groundDepth)
        {
            var shape = new SpriteQuad(fromInner - centre, toInner - centre, fromOuter - centre, toOuter - centre);
            pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, life, 1, Color.FromArgb(alpha, tint),
                tuning.RibbonShape, 0, Layer: layer, CastsLight: tuning.RibbonCastsLight,
                Ribbon: shape, DepthY: groundDepth));
        }
    }

    // Swept electrical cuts follow the *whole* moving blade, from near the
    // handle to the tip, using both real weapon poses. Two narrow depth
    // planes retain a sense of volume without a solid BladeRibbon curtain.
    private static void DrakeEdgeSweep(SpellParticlePool pool, WeaponMaskTrace from,
        WeaponMaskTrace to, float feetY, long now, int life)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.DrakeEdgeSweep);
        Vector2 previousBlade = from.Tip - from.Hilt;
        Vector2 currentBlade = to.Tip - to.Hilt;
        if (previousBlade.LengthSquared() < 9 || currentBlade.LengthSquared() < 9) return;
        Vector2 normal = Vector2.Normalize(new Vector2(-currentBlade.Y, currentBlade.X));
        var frame = new Frame(pool, to.Hilt, now, 1, Frame.ReferenceSize, (uint)(now / 73),
            scalePositions: false, depthY: feetY);
        for (int plane = 0; plane < tuning.PlaneCount; plane++)
        {
            bool behind = plane == 0;
            Layer layer = behind ? tuning.Layer : tuning.Layer2;
            float groundY = feetY + (behind ? tuning.GroundY : tuning.GroundY2);
            Vector2 previousPoint = Vector2.Zero;
            for (int i = 0; i <= tuning.UserLightningCount; i++)
            {
                float along = tuning.Along + tuning.Along2 * i / tuning.AlongDivisor;
                Vector2 oldPoint = from.Hilt + previousBlade * along;
                Vector2 currentPoint = to.Hilt + currentBlade * along;
                float offset = (frame.Unit(plane * 283 + i * 29 + 15) - tuning.Offset) *
                    (behind ? tuning.Offset2 : tuning.Offset3);
                Vector2 point = currentPoint + normal * (offset + (behind ? tuning.Point : tuning.Point2));
                oldPoint += normal * ((behind ? tuning.BehindActive : tuning.BehindInactive) + offset * tuning.Factor);
                int tint = i % 4 == 0 ? White : Ice;
                if (i > 0)
                    frame.Thread(previousPoint, point, tuning.ThreadStartWidth, tuning.ThreadEndWidth, tint,
                        behind ? tuning.ThreadAlpha : tuning.ThreadAlpha2, layer, sortY: groundY);
                frame.Thread(oldPoint, point, tuning.ThreadStartWidth2, tuning.ThreadEndWidth2,
                    i % 3 == 0 ? White : Steel,
                    behind ? tuning.ThreadAlpha3 : tuning.ThreadAlpha4, layer,
                    light: !behind && i == 7, sortY: groundY);
                if (i % 3 == 1)
                {
                    Vector2 delta = point - oldPoint;
                    frame.Sprite((oldPoint + point) * tuning.UserLightningOffset,
                        tuning.UserLightningWidth + delta.Length(), tuning.UserLightningHeight, tuning.UserLightningShape,
                        tint, behind ? tuning.UserLightningAlpha : tuning.UserLightningAlpha2, layer,
                        MathF.Atan2(delta.Y, delta.X), sortY: groundY);
                }
                if (i % 4 == 0)
                    frame.Sprite(point, tuning.SparkWidth + frame.Unit(i + 53) * tuning.SparkWidth2,
                        tuning.SparkHeight + frame.Unit(i + 57) * tuning.SparkHeight2,
                        tuning.SparkShape, White, behind ? tuning.SparkAlpha : tuning.SparkAlpha2,
                        layer, sortY: groundY);
                previousPoint = point;
            }
        }
    }

    private static void SweepRibbon(SpellParticlePool pool, WeaponMaskTrace from, WeaponMaskTrace to, float inner, float outer,
        Layer layer, float depthY, Color colour, long now, int duration, int alpha)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.SweepRibbon);
        Vector2 oldBlade = from.Tip - from.Hilt, newBlade = to.Tip - to.Hilt;
        float oldLength = oldBlade.Length(), newLength = newBlade.Length();
        if (oldLength < 3 || newLength < 3) return;
        float oldAngle = MathF.Atan2(oldBlade.Y, oldBlade.X);
        float turn = MathF.IEEERemainder(MathF.Atan2(newBlade.Y, newBlade.X) - oldAngle, MathF.Tau);
        int strips = Math.Clamp((int)MathF.Ceiling(MathF.Abs(turn) / tuning.StripsDivisor), 1, 6);
        Vector2 hilt = from.Hilt, blade = oldBlade;
        for (int i = 1; i <= strips; i++)
        {
            float step = i / (float)strips;
            Vector2 nextHilt = Vector2.Lerp(from.Hilt, to.Hilt, step);
            float angle = oldAngle + turn * step;
            Vector2 nextBlade = i == strips ? newBlade : new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                (oldLength + (newLength - oldLength) * step);
            Vector2 centre = (hilt + nextHilt + (blade + nextBlade) * tuning.Centre) * tuning.Centre2;
            var quad = new SpriteQuad(hilt + blade * inner - centre, nextHilt + nextBlade * inner - centre,
                hilt + blade * outer - centre, nextHilt + nextBlade * outer - centre);
            pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, duration, 1,
                Color.FromArgb(alpha, colour), tuning.BladeRibbonShape, 0,
                Layer: layer, CastsLight: tuning.BladeRibbonCastsLight, Ribbon: quad, DepthY: depthY));
            hilt = nextHilt;
            blade = nextBlade;
        }
    }

    internal static void DrawWeaponEnchant(SpellParticlePool pool, Spell spell, WeaponMaskTrace pose,
        Vector2 facing, long now, float size, uint seed, bool striking, float? feetY = null)
    {
        using var vfxScope = SpellVfxAssets.For(spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.DrawWeaponEnchant);
        Vector2 blade = pose.Tip - pose.Hilt;
        if (blade.LengthSquared() < 9) return;
        float time = now % 10400 / 1000f;
        var frame = new Frame(pool, pose.Hilt, now, 1, size, seed, scalePositions: false, depthY: feetY ?? pose.Hilt.Y + tuning.DepthY);
        if (spell == Spell.FlamingSword)
        {
            // Position samples along the real weapon trace, not four large blobs.
            for (int i = 0; i < tuning.UserEmberCount; i++)
            {
                int sample = tuning.Sample + i * tuning.Sample2;
                float age = Fract(time * (tuning.Age + frame.Unit(sample + 7) * tuning.Age2) +
                    frame.Unit(sample + 13));
                float along = tuning.Along + frame.Unit(sample + 19) * tuning.Along2;
                Vector2 root = pose.Hilt + blade * along;
                Vector2 point = root + new Vector2(
                    (frame.Unit(sample + 31) - tuning.X) * tuning.X2,
                    -age * (tuning.Y + frame.Unit(sample + 41) * tuning.Y2));
                float alpha = tuning.Alpha * (1 - age * tuning.Alpha2);
                frame.Sprite(point, tuning.UserEmberWidth + frame.Unit(sample + 47) * tuning.UserEmberWidth2,
                    tuning.UserEmberHeight + frame.Unit(sample + 51) * tuning.UserEmberHeight2,
                    i % 4 == 0 ? tuning.UserEmberShape : tuning.UserEmberShape2,
                    i % 5 == 0 ? Heat : i % 2 == 0 ? Flame : Ember,
                    alpha, i % 7 == 0 ? tuning.UserEmberLayer : tuning.UserEmberLayer2,
                    (frame.Unit(sample + 57) - tuning.UserEmberRotation) * tuning.UserEmberRotation2,
                    light: i % 19 == 0);
                if (i % 8 == 0)
                    frame.Clip(point - Vector2.UnitY * tuning.SmokeSequenceOffset, tuning.SmokeSequenceWidth, tuning.SmokeSequenceHeight,
                        tuning.SmokeSequenceShape, age + time * tuning.SmokeSequencePhase, Smoke,
                        tuning.SmokeSequenceAlpha * (1 - age), tuning.SmokeSequenceLayer);
            }
            return;
        }

        // Continuous charge runs from the handle along the entire blade.
        // The two near-parallel, irregular circuits are not broad glowing
        // plates. The rear one sorts behind the player; the bright edge is
        // a foreground contour with only a few selectively lit micro-motes.
        float groundBase = feetY ?? pose.Hilt.Y + tuning.GroundBase;
        Vector2 normal = Vector2.Normalize(new Vector2(-blade.Y, blade.X));
        int segments = striking ? tuning.Segments : tuning.Segments2;
        for (int plane = 0; plane < tuning.PlaneCount; plane++)
        {
            bool behind = plane == 0;
            Layer layer = behind ? tuning.Layer : tuning.Layer2;
            float depth = groundBase + (behind ? tuning.Depth : tuning.Depth2);
            Vector2 previous = pose.Hilt + blade * tuning.Previous +
                normal * (behind ? tuning.Previous2 : tuning.Previous3);
            for (int step = 1; step <= segments; step++)
            {
                float along = tuning.Along3 + tuning.Along4 * step / segments;
                float jitter = (frame.Unit(plane * 311 + step * 23 + 617) - tuning.Jitter) *
                    (striking ? tuning.Jitter2 : tuning.Jitter3);
                Vector2 point = pose.Hilt + blade * along +
                    normal * (jitter + (behind ? tuning.Point : tuning.Point2));
                int colour = step % 4 == 0 ? White : Ice;
                frame.Thread(previous, point, tuning.ThreadStartWidth, tuning.ThreadEndWidth, colour,
                    behind ? tuning.ThreadAlpha : tuning.ThreadAlpha2, layer,
                    light: !behind && step == segments / 2, sortY: depth);
                if (step % 3 == 1)
                {
                    Vector2 delta = point - previous;
                    frame.Sprite((point + previous) * tuning.UserLightningOffset, tuning.UserLightningWidth + delta.Length(),
                        tuning.UserLightningHeight, tuning.UserLightningShape,
                        colour, behind ? tuning.UserLightningAlpha : tuning.UserLightningAlpha2, layer,
                        MathF.Atan2(delta.Y, delta.X), sortY: depth);
                }
                previous = point;
            }
        }

        for (int i = 0; i < (striking ? tuning.SparkCount : tuning.SparkCount2); i++)
        {
            int sample = tuning.Sample3 + i * tuning.Sample4;
            float age = Fract(time * (tuning.Age3 + frame.Unit(sample + 3) * tuning.Age4) +
                frame.Unit(sample + 7));
            float along = tuning.Along5 + frame.Unit(sample + 11) * tuning.Along6;
            Vector2 root = pose.Hilt + blade * along;
            Vector2 point = root + normal *
                ((frame.Unit(sample + 13) - tuning.Point3) * tuning.Point4) -
                Vector2.UnitY * (age * (tuning.Point5 + frame.Unit(sample + 17) * tuning.Point6));
            bool behind = i % 4 == 0;
            float depth = groundBase + (behind ? tuning.Depth3 : tuning.Depth4);
            Layer layer = behind ? tuning.Layer3 : tuning.Layer4;
            float fade = tuning.Fade * (1 - age * tuning.Fade2);
            frame.Sprite(point, tuning.SparkWidth + frame.Unit(sample + 19) * tuning.SparkWidth2,
                tuning.SparkHeight + frame.Unit(sample + 23) * tuning.SparkHeight2, tuning.SparkShape,
                i % 3 == 0 ? White : Ice, fade, layer,
                sortY: depth);
            if (i % 9 == 0)
                frame.Sprite(root, tuning.WispWidth, tuning.WispHeight, tuning.WispShape, tuning.WispColour,
                    tuning.WispAlpha * fade, tuning.WispLayer, sortY: depth);
        }

        if (!striking) return;
        // The impact-follow-through energy peels off every portion of the
        // blade rather than emitting one solitary lightning bolt at the tip.
        for (int i = 0; i < tuning.ParticleCount; i++)
        {
            int sample = tuning.Sample5 + i * tuning.Sample6;
            float along = tuning.Along7 + i * tuning.Along8;
            Vector2 root = pose.Hilt + blade * along;
            Vector2 flick = root + normal *
                ((frame.Unit(sample + 5) - tuning.Flick) * tuning.Flick2) -
                Vector2.UnitY * (tuning.Flick3 + frame.Unit(sample + 7) * tuning.Flick4);
            bool behind = i % 5 == 0;
            Layer layer = behind ? tuning.Layer5 : tuning.Layer6;
            float depth = groundBase + (behind ? tuning.Depth5 : tuning.Depth6);
            frame.Thread(root, flick, tuning.ThreadStartWidth2, tuning.ThreadEndWidth2,
                i % 3 == 0 ? White : Ice, tuning.ThreadAlpha3,
                layer, sortY: depth);
        }
    }

    private static void AddBladeFire(SpellParticlePool pool, WeaponMaskTrace pose, Look look,
        long now, int life, float progress, float feetY)
    {
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Warrior.AddBladeFire);
        Vector2 blade = pose.Tip - pose.Hilt;
        float angle = MathF.Atan2(blade.Y, blade.X);
        var stroke = new Frame(pool, pose.Hilt, now, 1, Frame.ReferenceSize,
            (uint)(now / 53), scalePositions: false, depthY: feetY);
        for (int i = 0; i < tuning.UserEmberCount; i++)
        {
            int seed = i * 31 + 211;
            float age = Fract(progress * (tuning.Age + stroke.Unit(seed + 5)) +
                stroke.Unit(seed + 7));
            float along = tuning.Along + stroke.Unit(seed + 9) * tuning.Along2;
            Vector2 source = pose.Hilt + blade * along;
            Vector2 point = source + new Vector2(
                (stroke.Unit(seed + 13) - tuning.X) * age * tuning.X2,
                -age * (tuning.Y + stroke.Unit(seed + 17) * tuning.Y2));
            int tint = i % 5 == 0 ? Heat : i % 2 == 0 ? Flame : Ember;
            float alpha = tuning.Alpha * (1 - age * tuning.Alpha2);
            stroke.Sprite(point, tuning.UserEmberWidth + stroke.Unit(seed + 19) * tuning.UserEmberWidth2,
                tuning.UserEmberHeight + stroke.Unit(seed + 23) * tuning.UserEmberHeight2,
                i % 4 == 0 ? tuning.UserEmberShape : tuning.UserEmberShape2,
                tint, alpha, i % 6 == 0 ? tuning.UserEmberLayer : tuning.UserEmberLayer2,
                angle * tuning.UserEmberRotation, light: i % 16 == 0);
            if (i % 6 == 0)
                stroke.Clip(point - Vector2.UnitY * tuning.SmokeSequenceOffset,
                    tuning.SmokeSequenceWidth + stroke.Unit(seed + 27) * tuning.SmokeSequenceWidth2, tuning.SmokeSequenceHeight + stroke.Unit(seed + 29) * tuning.SmokeSequenceHeight2,
                    tuning.SmokeSequenceShape, progress + stroke.Unit(seed + 33),
                    Smoke, tuning.SmokeSequenceAlpha * (1 - age), tuning.SmokeSequenceLayer);
        }
    }
}
