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
        int life = Math.Min(remaining, Math.Clamp(interval * 2, 55, 190));
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
            for (int i = 0; i < 12; i++)
            {
                float along = .12f + i * .078f;
                Vector2 root = pose.Hilt + blade * along;
                Vector2 end = root + facing * (8 + returnCut.Unit(i + 19) * 16) +
                    normal * ((returnCut.Unit(i + 41) - .5f) * 17);
                bool rear = i % 4 == 0;
                Layer layer = rear ? Layer.Ground : Layer.EmissiveCore;
                float groundY = feet.Y + (rear ? -8 : 8);
                returnCut.Thread(root, end, .71f, .22f, i % 3 == 0 ? White : Ice,
                    .75f, layer, sortY: groundY);
                returnCut.Sprite(root, 3 + returnCut.Unit(i + 61) * 3,
                    4 + returnCut.Unit(i + 63) * 4, Shape.Spark,
                    i % 3 == 0 ? White : Ice, .79f, layer, sortY: groundY);
            }
        }
        bool heavy = spell is Spell.FlamingSword or Spell.CrossHalfMoon or Spell.BladeAvalanche or Spell.SlashingBurst;
        bool moon = spell is Spell.HalfMoon or Spell.CrossHalfMoon;
        if (previous is { } old)
        {
            Vector2 delta = pose.Tip - old.Tip;
            if (delta.LengthSquared() is > 3 and < 19600 && Vector2.DistanceSquared(old.Hilt, pose.Hilt) < 3600)
            {
                float inner = moon ? .76f : spell == Spell.TwinDrakeBlade ? .78f :
                    spell is Spell.Slaying or Spell.Thrusting ? .64f : .28f;
                float outer = moon || spell == Spell.TwinDrakeBlade ? 1.07f : heavy ? 1.21f : 1.13f;
                Layer depth = facing.Y < -.27f && (old.Tip.Y + pose.Tip.Y) * .5f < pose.Hilt.Y - 9
                    ? Layer.Ground : Layer.Emissive;
                if (spell == Spell.TwinDrakeBlade)
                    DrakeEdgeSweep(pool, old, pose, feet.Y, now, life);
                else
                {
                    SweepRibbon(pool, old, pose, inner, outer, depth, feet.Y, look.Colour,
                        now, life, moon ? 123 : heavy ? 220 : 175);
                    if (!moon && heavy)
                        SweepRibbon(pool, old, pose, .74f, 1.14f, depth, feet.Y, look.Core,
                            now, Math.Min(life, 98), 184);
                }
            }
        }

        if (progress < .23f || progress > .87f) return;
        bool cut = previousProgress < .38f && progress >= .38f;
        Vector2 forwardTip = pose.Hilt + facing * MathF.Max(19, Vector2.Dot(blade, facing));
        switch (spell)
        {
            case Spell.Slaying:
                effect.Add(forwardTip, facing * 7, Shape.Orb, 83, life, Layer.Glow, aspect: .67f, alpha: 150);
                effect.Add(forwardTip + facing * 7, facing * 9, Shape.WarriorTripleCut, 59, life,
                    Layer.Emissive, aspect: .69f, rotation: angle + .51f, core: true, alpha: 211);
                effect.Scatter(forwardTip, 5, Shape.Spark, 36, life);
                break;
            case Spell.HalfMoon when cut:
                ProjectCrescent(pool, look, feet, facing, 0, 30, 97, 29, now, life);
                break;
            case Spell.CrossHalfMoon when cut:
                ProjectCrescent(pool, look, feet, facing, -.28f, 24, 119, 42, now, life, sweep: 3.25f, stacked: true);
                ProjectCrescent(pool, look, feet, facing, MathF.PI - .17f, 32, 111, 38, now, life, sweep: 3.32f, stacked: true);
                effect.Add(feet + facing * 73 - Vector2.UnitY * 33, facing * 12, Shape.WarriorSpark, 33, life,
                    Layer.Emissive, aspect: .86f, rotation: angle, core: true, alpha: 157);
                break;
            case Spell.TwinDrakeBlade when cut:
                // The attack peak is a fragmented electrical cut throughout
                // the moving weapon's length. Fine tendrils keep its outline
                // without solid lines, rings or a tip-only lightning blob.
                var strike = new Frame(pool, feet, now, 1, Frame.ReferenceSize, (uint)now,
                    scalePositions: false, depthY: feet.Y);
                Vector2 bladeNormal = Vector2.Normalize(new Vector2(-blade.Y, blade.X));
                for (int i = 0; i < 14; i++)
                {
                    float along = .10f + .88f * i / 13;
                    Vector2 root = pose.Hilt + blade * along;
                    Vector2 tendril = root + facing * (7 + strike.Unit(i + 13) * 13) +
                        bladeNormal * ((strike.Unit(i + 29) - .5f) * 18);
                    bool rear = i % 4 == 0;
                    Layer layer = rear ? Layer.Ground : Layer.EmissiveCore;
                    float groundY = feet.Y + (rear ? -7 : 7);
                    strike.Thread(root, tendril, .88f, .20f,
                        i % 4 == 0 ? White : Ice, .87f, layer,
                        light: i == 7, sortY: groundY);
                    if (i % 2 == 0)
                    {
                        float rotation = MathF.Atan2(tendril.Y - root.Y, tendril.X - root.X);
                        strike.Sprite(Vector2.Lerp(root, tendril, .58f), 10, 3.4f,
                            Shape.UserLightning, White, .61f, layer,
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
                effect.Add(pose.Tip, facing * 8, Shape.WarriorCrescent, 96, life,
                    Layer.Emissive, aspect: .79f, rotation: angle - .44f, core: true, alpha: 205);
                break;
        }

        if (spell != Spell.Thrusting)
            effect.Add(pose.Tip, facing * 8, Shape.WarriorSpark, heavy ? 26 : 19, Math.Min(life, 95),
                Layer.Emissive, aspect: .85f, rotation: angle, alpha: 187, core: true);
    }

    private static void ProjectCrescent(SpellParticlePool pool, Look look, Vector2 feet, Vector2 facing,
        float yaw, float lift, float radius, float width, long now, int life, float sweep = 2.65f,
        bool stacked = false)
    {
        Vector2 forward = GroundHeading(facing);
        float heading = MathF.Atan2(forward.Y, forward.X);
        Vector2 centre = feet - Vector2.UnitY * lift;
        const int segments = 32;

        // World depth follows the point on the angled ground plane. The visible
        // crescent is independently raised and rolled in height: using its
        // screen-space Y as its ground depth would make front/back occlusion
        // jump as the slash lifts off the floor.
        var (inner, outer, previousDepth) = Edge(0);
        for (int i = 1; i <= segments; i++)
        {
            var (nextInner, nextOuter, nextDepth) = Edge(i / (float)segments);
            float segmentDepth = (previousDepth + nextDepth) * .5f;
            bool rear = segmentDepth < feet.Y;
            Layer layer = rear ? Layer.Ground : Layer.Emissive;

            // Receding, darker rear edge and forward luminous face make the
            // swing wrap around the actor instead of reading as one flat ring.
            Color body = rear
                ? Color.FromArgb(look.Colour.R * 3 / 5, look.Colour.G * 2 / 3, look.Colour.B * 3 / 4)
                : look.Colour;
            Color shadow = rear
                ? Color.FromArgb(look.Colour.R / 3, look.Colour.G / 3, look.Colour.B / 2)
                : Color.FromArgb(look.Colour.R / 2, look.Colour.G / 2, look.Colour.B * 3 / 4);
            Color core = rear
                ? Color.FromArgb(look.Core.R * 2 / 3, look.Core.G * 3 / 4, look.Core.B * 4 / 5)
                : look.Core;

            AddRibbon(Vector2.Lerp(outer, inner, 1.48f), Vector2.Lerp(inner, outer, 1.18f),
                Vector2.Lerp(nextOuter, nextInner, 1.48f), Vector2.Lerp(nextInner, nextOuter, 1.18f),
                layer, shadow, rear ? 127 : 172, segmentDepth);
            AddRibbon(inner, outer, nextInner, nextOuter, layer, body, 235, segmentDepth);
            if (i > 2 && i < segments - 1)
                AddRibbon(Vector2.Lerp(inner, outer, .73f), Vector2.Lerp(inner, outer, .98f),
                    Vector2.Lerp(nextInner, nextOuter, .73f), Vector2.Lerp(nextInner, nextOuter, .98f),
                    rear ? Layer.Ground : Layer.EmissiveCore, core, rear ? 139 : 201, segmentDepth);

            (inner, outer, previousDepth) = (nextInner, nextOuter, nextDepth);
        }

        // Emit small texture accents only after the ribbons so that the GPU
        // can batch the shader-driven MoonRibbon geometry without repeatedly
        // switching coverage modes.
        for (int i = 1; i < segments - 2; i += 3)
        {
            var (a, b, aDepth) = Edge(i / (float)segments);
            var (c, d, bDepth) = Edge((i + 1f) / segments);
            Vector2 tangent = (c + d) * .5f - (a + b) * .5f;
            Vector2 accent = (a + b + c + d) * .25f;
            float depth = (aDepth + bDepth) * .5f;
            float length = 9 + 10 * MathF.Sin(i * MathF.PI / segments);
            Layer layer = depth < feet.Y ? Layer.Ground : Layer.EmissiveCore;
            pool.TryAdd(new SpellParticle(accent, Vector2.Zero, now, Math.Min(life, 115),
                Math.Max(1, (int)length), Color.FromArgb(depth < feet.Y ? 100 : 157,
                    i % 4 == 1 ? look.Core : look.Colour),
                Shape.UserSlash, 0, Aspect: .42f,
                Rotation: MathF.Atan2(tangent.Y, tangent.X), Layer: layer,
                CastsLight: false, DepthY: depth));
        }

        // The detached glints remain small and follow each fragment's
        // ground depth. Avoid returning to oversized flat arc sprites.
        for (int i = 3; i < segments - 1; i += 3)
        {
            var (_, outerA, depthA) = Edge(i / (float)segments);
            var (_, outerB, depthB) = Edge((i + 1f) / segments);
            Vector2 edge = (outerA + outerB) * .5f;
            Vector2 drift = (outerB - outerA) * .66f - Vector2.UnitY * 3;
            float depth = (depthA + depthB) * .5f;
            Layer layer = depth < feet.Y ? Layer.Ground : Layer.EmissiveCore;
            pool.TryAdd(new SpellParticle(edge, drift, now, Math.Min(life, 125),
                5 + i % 4, Color.FromArgb(depth < feet.Y ? 112 : 189, look.Core),
                Shape.WarriorSpark, 0, Aspect: .68f,
                Rotation: MathF.Atan2(drift.Y, drift.X), Layer: layer,
                CastsLight: false, DepthY: depth));
        }

        // A few thin trailing flecks extend in the tangent direction and
        // dissipate without adding another circular billboard behind the arc.
        for (int i = 2; i < segments - 2; i += 5)
        {
            float sample = (i + .38f) / segments;
            var (innerEdge, outerEdge, depth) = Edge(sample);
            var (nextInner, nextOuter, _) = Edge((i + 1.3f) / segments);
            Vector2 point = Vector2.Lerp(innerEdge, outerEdge, .79f);
            Vector2 tangent = (nextInner + nextOuter - innerEdge - outerEdge) * .5f;
            Layer layer = depth < feet.Y ? Layer.Ground : Layer.EmissiveCore;
            pool.TryAdd(new SpellParticle(point, tangent * .52f - Vector2.UnitY * 2,
                now, Math.Min(life, 110), 6 + i % 3,
                Color.FromArgb(depth < feet.Y ? 83 : 140, look.Core),
                Shape.Wisp, 0, Aspect: 1.9f,
                Rotation: MathF.Atan2(tangent.Y, tangent.X), Layer: layer,
                CastsLight: false, DepthY: depth));
        }

        if (stacked)
        {
            // Cross Half Moon has multiple short cutting strokes at different
            // elevations instead of a single uniformly flat crescent sheet.
            // Reuse the same projected arc so every stroke inherits the right
            // front/back row; Half Moon keeps its original single-volume cut.
            for (int i = 2; i < segments - 2; i += 5)
            {
                float t = (i + .35f) / segments;
                var (innerA, outerA, depthA) = Edge(t);
                var (innerB, outerB, depthB) = Edge(MathF.Min(1, t + .05f));
                Vector2 centreStroke = (innerA + outerA + innerB + outerB) * .25f;
                Vector2 tangent = (innerB + outerB - innerA - outerA) * .5f;
                if (tangent.LengthSquared() < .01f) continue;
                Vector2 normal = Vector2.Normalize(new Vector2(-tangent.Y, tangent.X));
                float orientation = MathF.Atan2(tangent.Y, tangent.X);
                float depth = (depthA + depthB) * .5f;
                for (int tier = 0; tier < 3; tier++)
                {
                    float vertical = tier switch { 0 => -15f, 1 => 0f, _ => 12f };
                    float lateral = (tier - 1) * 7f + ((i % 3) - 1) * 2f;
                    Vector2 point = centreStroke + Vector2.UnitY * vertical +
                        normal * lateral + tangent * ((tier - 1) * 2f);
                    float strokeDepth = depth + (tier - 1) * 2.5f;
                    bool rear = strokeDepth < feet.Y;
                    float length = 12 + tier * 3 + 7 * MathF.Sin(t * MathF.PI);
                    int alpha = tier == 1 ? 190 : rear ? 104 : 145;
                    pool.TryAdd(new SpellParticle(point,
                        tangent * (.09f + tier * .035f), now, Math.Min(life, 128),
                        (int)MathF.Round(length), Color.FromArgb(alpha,
                            tier == 1 ? look.Core : look.Colour),
                        Shape.UserSlash, 0, Aspect: tier == 1 ? .27f : .22f,
                        Rotation: orientation + (tier - 1) * .075f,
                        Layer: rear ? Layer.Ground : Layer.EmissiveCore,
                        CastsLight: false, DepthY: strokeDepth));
                }
            }
        }

        (Vector2 Inner, Vector2 Outer, float GroundDepth) Edge(float t)
        {
            float angle = heading + yaw + (t - .5f) * sweep;
            Vector2 ray = Ellipse(angle, 1, 1);
            float crest = MathF.Sin(t * MathF.PI);
            float profile = MathF.Pow(MathF.Max(0, crest), .65f);
            float taper = .4f + width * profile * (1.30f - .77f * t);

            // The original angled ground projection remains authoritative for
            // sorting. Rear surfaces are higher; the near surface drops into
            // the foreground, with a rolled cross-section instead of a plane.
            float groundDepth = feet.Y + ProjectGround(ray * radius).Y;
            float rise = lift + 11 * crest * crest + 8 * (1 - ray.Y);
            float bevel = (1.6f + 3.8f * crest) * (.6f + .4f * MathF.Abs(ray.Y));
            Vector2 inside = feet + ProjectGround(ray * (radius - taper * .83f))
                - Vector2.UnitY * (rise + bevel);
            Vector2 outside = feet + ProjectGround(ray * (radius + taper * .25f))
                - Vector2.UnitY * (rise - bevel);
            return (inside, outside, groundDepth);
        }

        void AddRibbon(Vector2 fromInner, Vector2 fromOuter, Vector2 toInner, Vector2 toOuter,
            Layer layer, Color tint, int alpha, float groundDepth)
        {
            var shape = new SpriteQuad(fromInner - centre, toInner - centre, fromOuter - centre, toOuter - centre);
            pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, life, 1, Color.FromArgb(alpha, tint),
                Shape.MoonRibbon, 0, Layer: layer, CastsLight: false,
                Ribbon: shape, DepthY: groundDepth));
        }
    }

    // Swept electrical cuts follow the *whole* moving blade, from near the
    // handle to the tip, using both real weapon poses. Two narrow depth
    // planes retain a sense of volume without a solid BladeRibbon curtain.
    private static void DrakeEdgeSweep(SpellParticlePool pool, WeaponMaskTrace from,
        WeaponMaskTrace to, float feetY, long now, int life)
    {
        Vector2 previousBlade = from.Tip - from.Hilt;
        Vector2 currentBlade = to.Tip - to.Hilt;
        if (previousBlade.LengthSquared() < 9 || currentBlade.LengthSquared() < 9) return;
        Vector2 normal = Vector2.Normalize(new Vector2(-currentBlade.Y, currentBlade.X));
        var frame = new Frame(pool, to.Hilt, now, 1, Frame.ReferenceSize, (uint)(now / 73),
            scalePositions: false, depthY: feetY);
        for (int plane = 0; plane < 2; plane++)
        {
            bool behind = plane == 0;
            Layer layer = behind ? Layer.Ground : Layer.EmissiveCore;
            float groundY = feetY + (behind ? -8 : 8);
            Vector2 previousPoint = Vector2.Zero;
            for (int i = 0; i <= 14; i++)
            {
                float along = .10f + .88f * i / 14;
                Vector2 oldPoint = from.Hilt + previousBlade * along;
                Vector2 currentPoint = to.Hilt + currentBlade * along;
                float offset = (frame.Unit(plane * 283 + i * 29 + 15) - .5f) *
                    (behind ? 3.1f : 4.6f);
                Vector2 point = currentPoint + normal * (offset + (behind ? -1.7f : 1.4f));
                oldPoint += normal * ((behind ? -1.7f : 1.4f) + offset * .40f);
                int tint = i % 4 == 0 ? White : Ice;
                if (i > 0)
                    frame.Thread(previousPoint, point, .78f, .46f, tint,
                        behind ? .53f : .84f, layer, sortY: groundY);
                frame.Thread(oldPoint, point, .71f, .22f,
                    i % 3 == 0 ? White : Steel,
                    behind ? .47f : .72f, layer,
                    light: !behind && i == 7, sortY: groundY);
                if (i % 3 == 1)
                {
                    Vector2 delta = point - oldPoint;
                    frame.Sprite((oldPoint + point) * .5f,
                        5 + delta.Length(), 2.9f, Shape.UserLightning,
                        tint, behind ? .37f : .62f, layer,
                        MathF.Atan2(delta.Y, delta.X), sortY: groundY);
                }
                if (i % 4 == 0)
                    frame.Sprite(point, 2.8f + frame.Unit(i + 53) * 2.2f,
                        3.2f + frame.Unit(i + 57) * 3.5f,
                        Shape.Spark, White, behind ? .44f : .81f,
                        layer, sortY: groundY);
                previousPoint = point;
            }
        }
    }

    private static void SweepRibbon(SpellParticlePool pool, WeaponMaskTrace from, WeaponMaskTrace to, float inner, float outer,
        Layer layer, float depthY, Color colour, long now, int duration, int alpha)
    {
        Vector2 oldBlade = from.Tip - from.Hilt, newBlade = to.Tip - to.Hilt;
        float oldLength = oldBlade.Length(), newLength = newBlade.Length();
        if (oldLength < 3 || newLength < 3) return;
        float oldAngle = MathF.Atan2(oldBlade.Y, oldBlade.X);
        float turn = MathF.IEEERemainder(MathF.Atan2(newBlade.Y, newBlade.X) - oldAngle, MathF.Tau);
        int strips = Math.Clamp((int)MathF.Ceiling(MathF.Abs(turn) / .43f), 1, 6);
        Vector2 hilt = from.Hilt, blade = oldBlade;
        for (int i = 1; i <= strips; i++)
        {
            float step = i / (float)strips;
            Vector2 nextHilt = Vector2.Lerp(from.Hilt, to.Hilt, step);
            float angle = oldAngle + turn * step;
            Vector2 nextBlade = i == strips ? newBlade : new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                (oldLength + (newLength - oldLength) * step);
            Vector2 centre = (hilt + nextHilt + (blade + nextBlade) * .65f) * .5f;
            var quad = new SpriteQuad(hilt + blade * inner - centre, nextHilt + nextBlade * inner - centre,
                hilt + blade * outer - centre, nextHilt + nextBlade * outer - centre);
            pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, duration, 1,
                Color.FromArgb(alpha, colour), Shape.BladeRibbon, 0,
                Layer: layer, CastsLight: false, Ribbon: quad, DepthY: depthY));
            hilt = nextHilt;
            blade = nextBlade;
        }
    }

    internal static void DrawWeaponEnchant(SpellParticlePool pool, Spell spell, WeaponMaskTrace pose,
        Vector2 facing, long now, float size, uint seed, bool striking, float? feetY = null)
    {
        Vector2 blade = pose.Tip - pose.Hilt;
        if (blade.LengthSquared() < 9) return;
        float time = now % 10400 / 1000f;
        var frame = new Frame(pool, pose.Hilt, now, 1, size, seed, scalePositions: false, depthY: feetY ?? pose.Hilt.Y + 36);
        if (spell == Spell.FlamingSword)
        {
            // Position samples along the real weapon trace, not four large blobs.
            for (int i = 0; i < 64; i++)
            {
                int sample = 101 + i * 23;
                float age = Fract(time * (.69f + frame.Unit(sample + 7) * .59f) +
                    frame.Unit(sample + 13));
                float along = .19f + frame.Unit(sample + 19) * .78f;
                Vector2 root = pose.Hilt + blade * along;
                Vector2 point = root + new Vector2(
                    (frame.Unit(sample + 31) - .5f) * 7,
                    -age * (9 + frame.Unit(sample + 41) * 21));
                float alpha = .79f * (1 - age * .68f);
                frame.Sprite(point, 3 + frame.Unit(sample + 47) * 8,
                    6 + frame.Unit(sample + 51) * 16,
                    i % 4 == 0 ? Shape.UserEmber : Shape.UserFlame,
                    i % 5 == 0 ? Heat : i % 2 == 0 ? Flame : Ember,
                    alpha, i % 7 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                    (frame.Unit(sample + 57) - .5f) * .31f,
                    light: i % 19 == 0);
                if (i % 8 == 0)
                    frame.Clip(point - Vector2.UnitY * 9, 13, 18,
                        Shape.SmokeSequence, age + time * .24f, Smoke,
                        .28f * (1 - age), Layer.Smoke);
            }
            return;
        }

        // Continuous charge runs from the handle along the entire blade.
        // The two near-parallel, irregular circuits are not broad glowing
        // plates. The rear one sorts behind the player; the bright edge is
        // a foreground contour with only a few selectively lit micro-motes.
        float groundBase = feetY ?? pose.Hilt.Y + 36;
        Vector2 normal = Vector2.Normalize(new Vector2(-blade.Y, blade.X));
        int segments = striking ? 16 : 12;
        for (int plane = 0; plane < 2; plane++)
        {
            bool behind = plane == 0;
            Layer layer = behind ? Layer.Ground : Layer.EmissiveCore;
            float depth = groundBase + (behind ? -8 : 8);
            Vector2 previous = pose.Hilt + blade * .10f +
                normal * (behind ? -1.6f : 1.3f);
            for (int step = 1; step <= segments; step++)
            {
                float along = .10f + .89f * step / segments;
                float jitter = (frame.Unit(plane * 311 + step * 23 + 617) - .5f) *
                    (striking ? 5.0f : 3.7f);
                Vector2 point = pose.Hilt + blade * along +
                    normal * (jitter + (behind ? -1.6f : 1.3f));
                int colour = step % 4 == 0 ? White : Ice;
                frame.Thread(previous, point, .80f, .42f, colour,
                    behind ? .57f : .88f, layer,
                    light: !behind && step == segments / 2, sortY: depth);
                if (step % 3 == 1)
                {
                    Vector2 delta = point - previous;
                    frame.Sprite((point + previous) * .5f, 5 + delta.Length(),
                        2.9f, Shape.UserLightning,
                        colour, behind ? .37f : .64f, layer,
                        MathF.Atan2(delta.Y, delta.X), sortY: depth);
                }
                previous = point;
            }
        }

        for (int i = 0; i < (striking ? 30 : 22); i++)
        {
            int sample = 113 + i * 29;
            float age = Fract(time * (1.31f + frame.Unit(sample + 3) * .89f) +
                frame.Unit(sample + 7));
            float along = .10f + frame.Unit(sample + 11) * .89f;
            Vector2 root = pose.Hilt + blade * along;
            Vector2 point = root + normal *
                ((frame.Unit(sample + 13) - .5f) * 8) -
                Vector2.UnitY * (age * (6 + frame.Unit(sample + 17) * 13));
            bool behind = i % 4 == 0;
            float depth = groundBase + (behind ? -8 : 8);
            Layer layer = behind ? Layer.Ground : Layer.EmissiveCore;
            float fade = .83f * (1 - age * .65f);
            frame.Sprite(point, 2.6f + frame.Unit(sample + 19) * 3,
                3.2f + frame.Unit(sample + 23) * 4.1f, Shape.Spark,
                i % 3 == 0 ? White : Ice, fade, layer,
                sortY: depth);
            if (i % 9 == 0)
                frame.Sprite(root, 12, 6, Shape.Wisp, 0x467bd9,
                    .19f * fade, Layer.Glow, sortY: depth);
        }

        if (!striking) return;
        // The impact-follow-through energy peels off every portion of the
        // blade rather than emitting one solitary lightning bolt at the tip.
        for (int i = 0; i < 12; i++)
        {
            int sample = 511 + i * 23;
            float along = .12f + i * .077f;
            Vector2 root = pose.Hilt + blade * along;
            Vector2 flick = root + normal *
                ((frame.Unit(sample + 5) - .5f) * 22) -
                Vector2.UnitY * (3 + frame.Unit(sample + 7) * 9);
            bool behind = i % 5 == 0;
            Layer layer = behind ? Layer.Ground : Layer.EmissiveCore;
            float depth = groundBase + (behind ? -8 : 8);
            frame.Thread(root, flick, .82f, .22f,
                i % 3 == 0 ? White : Ice, .68f,
                layer, sortY: depth);
        }
    }

    private static void AddBladeFire(SpellParticlePool pool, WeaponMaskTrace pose, Look look,
        long now, int life, float progress, float feetY)
    {
        Vector2 blade = pose.Tip - pose.Hilt;
        float angle = MathF.Atan2(blade.Y, blade.X);
        var stroke = new Frame(pool, pose.Hilt, now, 1, Frame.ReferenceSize,
            (uint)(now / 53), scalePositions: false, depthY: feetY);
        for (int i = 0; i < 58; i++)
        {
            int seed = i * 31 + 211;
            float age = Fract(progress * (1.4f + stroke.Unit(seed + 5)) +
                stroke.Unit(seed + 7));
            float along = .25f + stroke.Unit(seed + 9) * .76f;
            Vector2 source = pose.Hilt + blade * along;
            Vector2 point = source + new Vector2(
                (stroke.Unit(seed + 13) - .5f) * age * 9,
                -age * (9 + stroke.Unit(seed + 17) * 22));
            int tint = i % 5 == 0 ? Heat : i % 2 == 0 ? Flame : Ember;
            float alpha = .78f * (1 - age * .76f);
            stroke.Sprite(point, 4 + stroke.Unit(seed + 19) * 8,
                9 + stroke.Unit(seed + 23) * 17,
                i % 4 == 0 ? Shape.UserEmber : Shape.UserFlame,
                tint, alpha, i % 6 == 0 ? Layer.EmissiveCore : Layer.Emissive,
                angle * .17f, light: i % 16 == 0);
            if (i % 6 == 0)
                stroke.Clip(point - Vector2.UnitY * 8,
                    12 + stroke.Unit(seed + 27) * 9, 16 + stroke.Unit(seed + 29) * 15,
                    Shape.SmokeSequence, progress + stroke.Unit(seed + 33),
                    Smoke, .32f * (1 - age), Layer.Smoke);
        }
    }
}
