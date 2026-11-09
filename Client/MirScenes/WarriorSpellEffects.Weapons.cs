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
            Vector2 contact = pose.Hilt + blade * .78f;
            var returnCut = new Frame(pool, feet, now, 1, Frame.ReferenceSize, (uint)now,
                scalePositions: false, depthY: feet.Y);
            WizardSpellEffects.Channel(returnCut, contact, contact + facing * 29 - Vector2.UnitY * 62,
                22, 2.8f, .92f, now / 1000f, 4, 211, colours: WizardSpellEffects.DrakeColours);
            effect.Add(contact, -Vector2.UnitY * 10, Shape.UserLightning, 67, life, Layer.Emissive,
                aspect: 1.3f, rotation: angle + .27f, alpha: 188);
            effect.Add(contact, Vector2.Zero, Shape.Flash, 28, life, Layer.EmissiveCore, core: true, alpha: 218);
            effect.Scatter(contact, 8, Shape.WarriorSpark, 43, life, rise: true);
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
                SweepRibbon(pool, old, pose, inner, outer, depth, feet.Y, look.Colour, now, life, moon ? 123 : heavy ? 220 : 175);
                if (!moon && (heavy || spell == Spell.TwinDrakeBlade))
                    SweepRibbon(pool, old, pose, spell == Spell.TwinDrakeBlade ? .91f : .74f,
                        spell == Spell.TwinDrakeBlade ? 1.02f : 1.14f, depth, feet.Y, look.Core, now, Math.Min(life, 98), 184);
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
                effect.Add(feet + facing * 83 - Vector2.UnitY * 25, facing * 7, Shape.Wisp, 71, life,
                    Layer.Glow, aspect: .83f, alpha: 141);
                break;
            case Spell.CrossHalfMoon when cut:
                ProjectCrescent(pool, look, feet, facing, -.34f, 23, 88, 22, now, life, sweep: 3.68f);
                ProjectCrescent(pool, look, feet, facing, MathF.PI - .22f, 36, 79, 22, now, life, sweep: 3.78f);
                effect.Add(feet + facing * 73 - Vector2.UnitY * 33, facing * 12, Shape.WarriorSpark, 33, life,
                    Layer.Emissive, aspect: .86f, rotation: angle, core: true, alpha: 157);
                break;
            case Spell.TwinDrakeBlade when cut:
                // The old double crescent read as expanding rings. The hit is
                // now a jagged electric discharge anchored to the real blade.
                var strike = new Frame(pool, feet, now, 1, Frame.ReferenceSize, (uint)now,
                    scalePositions: false, depthY: feet.Y);
                WizardSpellEffects.Channel(strike, pose.Hilt + blade * .16f,
                    forwardTip + facing * 27 - Vector2.UnitY * 10,
                    21, 3.0f, .96f, now / 1000f, 4, 113,
                    colours: WizardSpellEffects.DrakeColours);
                WizardSpellEffects.Channel(strike, pose.Tip,
                    pose.Tip + facing * 43 - Vector2.UnitY * 20,
                    17, 2.4f, .87f, now / 1000f + .05f, 3, 239,
                    colours: WizardSpellEffects.DrakeColours);
                effect.Add(pose.Hilt - Vector2.UnitY * 9, Vector2.Zero, Shape.Flash, 32, life,
                    Layer.EmissiveCore, aspect: 1.21f, rotation: angle, core: true, alpha: 213);
                break;
            case Spell.FlamingSword:
                AddBladeFire(pool, pose, look, now, life, progress, feet.Y);
                // Follow the cut with tongues and smoke rather than a circular impact.
                effect.Add(forwardTip + facing * 22, facing * 12 - Vector2.UnitY * 13,
                    Shape.FlameTongue, 63, life, Layer.Emissive,
                    aspect: 1.34f, rotation: angle - .30f, alpha: 194);
                if (cut)
                {
                    effect.Add(feet + facing * 39 - Vector2.UnitY * 34, facing * 19,
                        Shape.FireSequence, 128, life, Layer.Emissive,
                        aspect: .53f, rotation: angle, alpha: 201);
                    effect.Add(forwardTip - Vector2.UnitY * 24,
                        facing * 7 - Vector2.UnitY * 18, Shape.SmokeSequence, 69,
                        life, Layer.Smoke, aspect: 1.10f, rotation: angle * .23f, alpha: 140);
                }
                effect.Scatter(forwardTip, 7, Shape.WarriorSpark, 58, life, rise: true);
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
        float yaw, float lift, float radius, float width, long now, int life, float sweep = 2.65f)
    {
        Vector2 forward = GroundHeading(facing);
        float heading = MathF.Atan2(forward.Y, forward.X);
        Vector2 centre = feet - Vector2.UnitY * lift;
        const int segments = 14;
        var (inner, outer) = Edge(0);
        for (int i = 1; i <= segments; i++)
        {
            var (nextInner, nextOuter) = Edge(i / (float)segments);
            Layer layer = (outer.Y + nextOuter.Y) * .5f < centre.Y ? Layer.Ground : Layer.Emissive;
            Color shadow = Color.FromArgb(look.Colour.R / 2, look.Colour.G / 2, look.Colour.B * 3 / 4);
            AddRibbon(Vector2.Lerp(outer, inner, 1.48f), Vector2.Lerp(inner, outer, 1.18f),
                Vector2.Lerp(nextOuter, nextInner, 1.48f), Vector2.Lerp(nextInner, nextOuter, 1.18f),
                layer, shadow, 177);
            AddRibbon(inner, outer, nextInner, nextOuter, layer, look.Colour, 235);
            if (i > 2 && i < segments - 1)
            {
                AddRibbon(Vector2.Lerp(inner, outer, .73f), Vector2.Lerp(inner, outer, .98f),
                    Vector2.Lerp(nextInner, nextOuter, .73f), Vector2.Lerp(nextInner, nextOuter, .98f),
                    layer == Layer.Ground ? layer : Layer.EmissiveCore, look.Core, 197);
                if (i % 3 == 0)
                {
                    Vector2 edge = (outer + nextOuter) * .5f;
                    Vector2 drift = (outer - nextOuter) * .85f;
                    pool.TryAdd(new SpellParticle(edge, drift, now, life, 22, Color.FromArgb(158, look.Core),
                        Shape.WarriorSpark, 0, .61f, heading, Layer: layer, CastsLight: false, DepthY: feet.Y));
                }
            }
            (inner, outer) = (nextInner, nextOuter);
        }

        (Vector2 Inner, Vector2 Outer) Edge(float t)
        {
            float turn = (t - .5f) * sweep;
            Vector2 ray = Ellipse(heading + yaw + turn, 1, 1);
            float taper = .5f + width * MathF.Pow(MathF.Max(0, MathF.Sin(t * MathF.PI)), 1.28f);
            return (centre + ProjectGround(ray * (radius - taper * .83f)),
                centre + ProjectGround(ray * (radius + taper * .25f)));
        }

        void AddRibbon(Vector2 fromInner, Vector2 fromOuter, Vector2 toInner, Vector2 toOuter, Layer layer, Color tint, int alpha)
        {
            var shape = new SpriteQuad(fromInner - centre, toInner - centre, fromOuter - centre, toOuter - centre);
            pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, life, 1, Color.FromArgb(alpha, tint),
                Shape.MoonRibbon, 0, Layer: layer, CastsLight: false, Ribbon: shape, DepthY: feet.Y));
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
            for (int i = 0; i < 4; i++)
            {
                float lift = Fract(time * (.61f + i * .08f) + frame.Unit(i));
                Vector2 point = pose.Hilt + blade * (.39f + .19f * i) - Vector2.UnitY * (3 + 9 * lift);
                frame.Clip(point, 23 + i * 4, 31 + i * 3, Shape.FlameSequence, time * .84f + i * .23f,
                    i == 1 ? Heat : i == 2 ? Ember : Flame, .67f, rotation: (frame.Unit(i + 13) - .5f) * .72f, light: i == 3);
                frame.Sprite(point - Vector2.UnitY * (5 + lift * 15), 4, 7, Shape.WarriorSpark, Heat, .83f * (1 - lift),
                    Layer.EmissiveCore);
            }
            return;
        }

        Vector2 bladeRoot = pose.Hilt + blade * .23f;
        var colours = WizardSpellEffects.DrakeColours;
        // Electricity is shaped by the blade, without a large oval behind it.
        WizardSpellEffects.Channel(frame, bladeRoot, pose.Tip, striking ? 13 : 7, striking ? 3.2f : 1.85f,
            striking ? .98f : .73f, time, striking ? 3 : 1, 17, colours: colours);
        for (int i = 0; i < (striking ? 11 : 5); i++)
        {
            float age = Fract(time * 1.7f + frame.Unit(i + 107));
            Vector2 point = pose.Hilt + blade * (.28f + frame.Unit(i + 33) * .69f) +
                new Vector2((frame.Unit(i + 41) - .5f) * 8, -age * (8 + frame.Unit(i + 57) * 22));
            frame.Sprite(point, 3 + frame.Unit(i + 71) * 4, 7 + frame.Unit(i + 79) * 8,
                Shape.WarriorSpark, i % 3 == 0 ? White : Ice, .79f * (1 - age * .57f),
                Layer.EmissiveCore, light: i % 5 == 0);
        }
        if (!striking) return;
        Vector2 edge = Vector2.Dot(blade, facing) >= 0 ? pose.Tip : pose.Hilt;
        Vector2 end = edge + facing * 49 - Vector2.UnitY * 28;
        WizardSpellEffects.Channel(frame, edge, end, 22, 2.9f, .94f, time + .12f, 3, 93, colours: colours);
        WizardSpellEffects.Channel(frame, pose.Hilt + blade * .61f, end - Vector2.UnitY * 21 - facing * 11,
            17, 2.15f, .84f, time + .07f, 2, 181, colours: colours);
        Vector2 ground = pose.Hilt + facing * 18 + Vector2.UnitY * (feetY.GetValueOrDefault(pose.Hilt.Y + 36) - pose.Hilt.Y);
        WizardSpellEffects.Channel(frame, pose.Hilt, ground, 10, 2.5f, .93f, time, 2, 238, colours: colours);
        for (int i = 0; i < 9; i++)
        {
            float age = Fract(time * 2.9f + frame.Unit(i + 271));
            Vector2 point = Vector2.Lerp(pose.Hilt, ground, frame.Unit(i + 304)) +
                new Vector2((frame.Unit(i + 319) - .5f) * 11, -age * 16);
            frame.Sprite(point, 4 + frame.Unit(i + 336) * 4, 9,
                Shape.WarriorSpark, White, .77f * (1 - age * .64f),
                Layer.EmissiveCore);
        }
        frame.Sprite(end - facing * 12, 23, 32, Shape.Flash, White, .83f, Layer.EmissiveCore, time * .7f);
    }

    private static void AddBladeFire(SpellParticlePool pool, WeaponMaskTrace pose, Look look, long now, int life, float progress,
        float feetY)
    {
        Vector2 blade = pose.Tip - pose.Hilt;
        byte first = (byte)((int)(progress * SpellParticleAtlas.ClipFrames) % SpellParticleAtlas.ClipFrames);
        for (int i = 0; i < 3; i++)
        {
            Vector2 point = pose.Hilt + blade * (.49f + .24f * i);
            byte frame = (byte)((first + i * 6) % SpellParticleAtlas.ClipFrames);
            pool.TryAdd(new SpellParticle(point, new Vector2(2, -12 - i * 5), now, life, 36 + i * 6,
                Color.FromArgb(205 - i * 13, i == 0 ? look.Core : look.Colour), Shape.FlameSequence, 0, Aspect: 1.27f,
                Rotation: MathF.Atan2(blade.Y, blade.X) * .34f, Growth: .28f, Layer: i == 0 ? Layer.EmissiveCore : Layer.Emissive,
                Frame: frame, DepthY: feetY));
        }
    }
}
