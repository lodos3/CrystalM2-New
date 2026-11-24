using System.Numerics;
using CrystalM2.Rendering.Core;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static class ArmourWingParticles
{
    // Eight strips, at most two interpolated colour frames and one low-energy
    // depth shell per strip; both wings plus their motes fit without eviction.
    internal const int ParticleCapacity = 160;

    internal static int NearSide(MirDirection direction) =>
        direction is MirDirection.UpRight or MirDirection.Right or MirDirection.DownRight ? 1 : -1;

    private const float RearDepthScale = .8f;

    internal static Vector2 BackAttachment(Vector2 shoulder, MirDirection direction, float twist = 0, float roll = 0)
    {
        // Anchor in the unrotated body basis: only distal geometry twists.
        float yaw = (int)direction * MathF.PI / 4;
        float profile = MathF.Abs(MathF.Sin(yaw));
        return shoulder + new Vector2(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f) *
            ((7f + profile * profile) * RearDepthScale);
    }

    private static Vector2 Rotate(Vector2 v, float angle)
    {
        if (angle == 0) return v;
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    internal static Vector2 Shoulder(byte effect, bool mounted) =>
        new(24, (mounted ? -42 : -24) - (effect is ArmourWings.FelFire or ArmourWings.Archangel or ArmourWings.DawnSeraph ? 8 : 0));

    internal static Color LightColour(byte effect) => Color.FromArgb(effect switch
    {
        1 => 0xA9CCFF, 2 => 0xFFAF48, ArmourWings.Phoenix => 0xFF7635,
        ArmourWings.Frost => 0x68DEFF, ArmourWings.CelestialElectric => 0xA5ACFF,
        ArmourWings.FelFire => 0x9CF438, ArmourWings.DemonicBone => 0xB34241,
        ArmourWings.Archangel => 0xCBDFFF, ArmourWings.DawnSeraph => 0xFFD28B,
        ArmourWings.MagmaWyrm => 0xFF7430, ArmourWings.TidalLeviathan => 0x52CDE5,
        ArmourWings.GeodeTitan => 0x96B7C6, ArmourWings.Tempest => 0xB7F1E8,
        ArmourWings.VerdantMonarch => 0x83D052, ArmourWings.Thunderstorm => 0x82A9FF,
        ArmourWings.Venom => 0xB9EA47, ArmourWings.SolarAscendant => 0xFFD36B,
        ArmourWings.Eclipse => 0x9466DC, ArmourWings.AstralConvergence => 0x9CA8FF, _ => 0xB78AFF
    } | unchecked((int)0xFF000000));

    internal static void Emit(SpellParticlePool pool, byte effect, int bank, int pose, Vector2 shoulder,
        MirDirection direction, long now, uint seed, int onlySide = 0, float torsoYaw = 0, float torsoRoll = 0)
    {
        pool.Clear();
        if (effect is 1 or 2)
        {
            var art = WingPoseAtlas.GetPose(bank, pose);
            if (art.Width == 0) return;
            // Bank offsets use the top-left of the map cell, before body or equipment offsets.
            var centre = new Vector2(art.X + art.Width / 2f, art.Y + art.Height / 2f);
            for (int c = 0; c < 3; c++)
                pool.TryAdd(new SpellParticle(centre, Vector2.Zero, now, 100, art.Width,
                    Color.FromArgb(unchecked((int)0xff000000) | (255 << (c * 8))), Shape.Wisp, 0,
                    Aspect: art.Height / (float)art.Width, Layer: SpellParticleLayer.EmissiveCore, FrameOnly: true)
                    { CoveragePlane = WingPoseAtlas.Handle(bank, pose, c) });
            return;
        }
        if (!ArmourWings.IsParticle(effect)) return;
        if (ArmourWings.IsAura(effect))
        {
            EmitElemental(pool, effect, shoulder, direction, now, seed, onlySide, torsoYaw, torsoRoll);
            return;
        }

        float yaw = (int)direction * (MathF.PI / 4) + torsoYaw;
        Vector2 across = new(MathF.Cos(yaw), MathF.Sin(yaw) * .49f);
        Vector2 back = new(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f);
        float profile = MathF.Abs(MathF.Sin(yaw));
        Vector2 attachment = BackAttachment(shoulder, direction, torsoYaw, torsoRoll);
        float phase = now % 1800 * (MathF.Tau / 1800) + (seed % 97) * .065f;
        float flap = MathF.Sin(phase);
        int design = effect - ArmourWings.Phoenix;
        int designBank = WingPoseAtlas.DesignBank + design;
        var membrane = WingPoseAtlas.GetPose(designBank, 0);
        int count = WingPoseAtlas.FrameCount(designBank);
        int artWidth = membrane.Width + 2 * membrane.X;
        bool felFire = effect == ArmourWings.FelFire;
        bool seraph = effect is ArmourWings.Archangel or ArmourWings.DawnSeraph;
        float north = MathF.Max(0, MathF.Cos(yaw));
        float tipLift = felFire ? 36 * north * north : seraph ? 9 + 14 * north * north : 0;
        bool elemental = ArmourWings.IsElemental(effect);
        int segments = felFire || seraph || elemental ? 8 : 6;
        float thickness = effect switch
        {
            ArmourWings.Phoenix => 9f, ArmourWings.Frost => 8f,
            ArmourWings.Moth => 6f, ArmourWings.CelestialElectric => 7f,
            ArmourWings.FelFire => 11f, ArmourWings.DemonicBone => 8.5f,
            ArmourWings.Archangel => 9.5f, ArmourWings.DawnSeraph => 10f,
            ArmourWings.MagmaWyrm => 9f, ArmourWings.GeodeTitan => 10f,
            ArmourWings.TidalLeviathan => 7.5f, ArmourWings.Tempest => 6.5f, _ => 8f
        };
        int edge = LightColour(effect).ToArgb();
        foreach (int side in Sides)
        {
            if (onlySide != 0 && side != onlySide) continue;
            bool near = profile < .001f || side == NearSide(direction);
            float baseYaw = (int)direction * MathF.PI / 4;
            Vector2 root = attachment + new Vector2(MathF.Cos(baseYaw), MathF.Sin(baseYaw) * .49f) * (side * 5.5f);
            int opacity = (int)(218 + 12 * MathF.Sin(yaw) * side + 8 * flap);

            // Sweep the actual wing behind the actor as it turns edge-on.
            // The near wing extends farther back than the far wing, so they
            // do not collapse into the same line at a 90-degree facing.
            float spanLength = seraph ? 66 + 2 * flap : (felFire ? 60 : elemental ? 54 : 48) + 6 * flap;
            float sweepLength = (19 + 6 * flap + profile * profile * (near ? 23 : 12)) * RearDepthScale;
            float sweepCurve = (5 + 7 * profile) * RearDepthScale;
            // On a diagonal one wing's transverse and rear axes can cancel.
            // Keep its projected extent nonzero rather than flattening the art.
            float endpoint = across.X * side * spanLength + back.X * (sweepLength + sweepCurve);
            if (MathF.Abs(endpoint) < 13 && MathF.Abs(back.X) > .25f)
                sweepLength += (13 - endpoint * MathF.Sign(back.X)) / MathF.Abs(back.X);

            Vector2 Project(float u, float y, float shell = 0)
            {
                float t = Math.Clamp(u, 0, 1);
                float sweep = u * sweepLength + t * t * sweepCurve;
                float span = u * spanLength;
                float height = y - (design == 2 ? 52 : 42);
                // Cup the wing into a three-dimensional cross-section.
                // Upper/lower feathers recede at different depths instead of
                // all rows occupying a single flat projected ribbon.
                float cross = Math.Clamp(height / 78f, -1f, 1f);
                float camber = MathF.Sin(t * MathF.PI * .86f) *
                    (2.5f + 5.5f * cross * cross + 2.5f * cross);
                float twist = 3.5f * cross * t * MathF.Sin(phase + t * 1.8f);
                // Rounded root and mid-span, tapering toward the tips.
                // The small terminal thickness also covers padded glow strips.
                float depth = thickness * (.34f + .66f * (1 - t) + .24f * MathF.Sin(t * MathF.PI)) *
                    (1 + .32f * profile) * (1 - .20f * MathF.Abs(cross));
                float arch = tipLift * t * t;
                // The raised outer rim widens slightly as well as receding.
                // Match the vertical thickness offset to the projected rear
                // axis so a south-facing wing cannot cancel its own depth.
                float rearSign = MathF.Cos(yaw) < 0 ? -1f : 1f;
                // Distal deformation with a fixed root and a minimum rear
                // half-plane in profile. Rotation cannot swing the socket.
                float flex = t * t * (3 - 2 * t);
                float localYaw = baseYaw + torsoYaw * flex;
                Vector2 localAcross = new(MathF.Cos(localYaw), MathF.Sin(localYaw) * .49f);
                Vector2 localBack = new(-MathF.Sin(localYaw), MathF.Cos(localYaw) * .49f);
                Vector2 projected = Rotate(localAcross * (side * (span + shell * depth * .23f * flex)) +
                    localBack * (sweep + (camber + twist + shell * depth * (.25f + .75f * flex)) * RearDepthScale) +
                    Vector2.UnitY * (height * .55f - 4 * u * flap + 4 * t * t - arch +
                    shell * depth * .30f * rearSign * flex), torsoRoll * flex);
                if ((direction is MirDirection.Left or MirDirection.Right) && u >= 0)
                {
                    float sign = direction == MirDirection.Right ? -1f : 1f;
                    projected.X = sign * Math.Max(0, sign * projected.X);
                }
                return projected;
            }

            int firstFrame = 0, nextFrame = 0;
            float mix = 0;
            if (count > 1)
            {
                int period = seraph ? 2400 : felFire ? 1200 : 960;
                float frame = ((now + seed % 773 + (side + 1) * 97) % period) * count / (float)period;
                int index = (int)frame;
                firstFrame = index;
                nextFrame = (index + 1) % count;
                mix = frame - index;
            }

            // Project the padded glow with the same curvature and attachment as the quills.
            for (int segment = 0; segment < segments; segment++)
            {
                float a = MathF.Round(segment * membrane.Width / (float)segments) / membrane.Width;
                float b = MathF.Round((segment + 1) * membrane.Width / (float)segments) / membrane.Width;
                float left = (membrane.X + membrane.Width * a) / artWidth;
                float right = (membrane.X + membrane.Width * b) / artWidth;
                int top = membrane.Y, bottom = top + membrane.Height;
                var quad = new SpriteQuad(Project(left, top), Project(right, top), Project(left, bottom), Project(right, bottom));
                var underside = new SpriteQuad(Project(left, top, 1), Project(right, top, 1),
                    Project(left, bottom, 1), Project(right, bottom, 1));
                // A dimmer rear surface creates a visible thickness edge
                // without doubling the bright additive front surface. Use
                // the dominant animation sample for this low-energy shell.
                int shellFrame = mix > .5f ? nextFrame : firstFrame;
                float shellEnergy = .29f + .10f * profile;
                AddFrame(shellFrame, shellEnergy, underside, SpellParticleLayer.Emissive);
                float mid = Math.Clamp((left + right) * .5f, 0, 1);
                float light = .82f + .12f * MathF.Sin(mid * MathF.PI);
                if (!near) light *= .88f;
                AddFrame(firstFrame, (1 - mix) * light, quad, SpellParticleLayer.EmissiveCore);
                if (mix > 0) AddFrame(nextFrame, mix * light, quad, SpellParticleLayer.EmissiveCore);

                void AddFrame(int index, float weight, SpriteQuad surface, SpellParticleLayer layer)
                {
                    int alpha = Math.Clamp((int)MathF.Round(opacity * weight), 0, 255);
                    if (alpha == 0) return;
                    for (int channel = 0; channel < 3; channel++)
                        pool.TryAdd(new SpellParticle(root, Vector2.Zero, now, 100, 1,
                            Color.FromArgb((alpha << 24) | (255 << (channel * 8))),
                            Shape.Wisp, 0, Layer: layer, Ribbon: surface,
                            RibbonTextureU: new Vector2(a, b), FrameOnly: true)
                            { CoveragePlane = WingPoseAtlas.Handle(designBank, index, channel) });
                }
            }
            int motes = effect == ArmourWings.DemonicBone ? 2 : seraph ? 3 : elemental ? 6 : 7;
            for (int i = 0; i < motes; i++)
            {
                float age = (phase / MathF.Tau + i * .381966f + (side + 1) * .18f) % 1;
                float span = .22f + i * .096f;
                float y = effect == ArmourWings.FelFire ? 44 - 17 * span - age * 34 :
                    120 - 106 * span + age * (design == 0 ? -24 : 18);
                var mote = root + Project(span, y);
                Shape accent = effect switch
                {
                    ArmourWings.MagmaWyrm => Shape.FlameTongue, ArmourWings.TidalLeviathan or ArmourWings.Venom => Shape.Orb,
                    ArmourWings.GeodeTitan => Shape.Shard, ArmourWings.VerdantMonarch => Shape.Feather,
                    ArmourWings.Tempest => Shape.MistFilament, ArmourWings.Thunderstorm => Shape.Flash, _ => Shape.Spark
                };
                Sprite(mote, 4, design == 1 ? 6 : elemental ? 6 : 4, accent, edge,
                    Math.Clamp((int)(190 * MathF.Sin(age * MathF.PI)), 1, 255));
            }
        }
        void Sprite(Vector2 at, float w, float h, Shape shape, int colour, int alpha, float angle = 0)
        {
            int width = Math.Max(1, (int)MathF.Round(w));
            pool.TryAdd(new SpellParticle(at, Vector2.Zero, now, 100, width,
                Color.FromArgb(alpha, Color.FromArgb(colour)), shape, 0, Aspect: h / width, Rotation: angle,
                Layer: SpellParticleLayer.EmissiveCore, FrameOnly: true));
        }
    }
    // Orbit materials use the same RGB coverage atlas as the wing membranes.
    private static void EmitElemental(SpellParticlePool pool, byte effect, Vector2 shoulder,
        MirDirection direction, long now, uint seed, int onlySide, float torsoYaw, float torsoRoll)
    {
        float yaw = (int)direction * MathF.PI / 4 + torsoYaw;
        Vector2 across = new(MathF.Cos(yaw), MathF.Sin(yaw) * .49f);
        Vector2 back = new(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f);
        float profile = MathF.Abs(MathF.Sin(yaw));
        float clock = (now % 4800) * MathF.Tau / 4800 + (seed % 97) * .065f;
        Vector2 attachment = BackAttachment(shoulder, direction, torsoYaw, torsoRoll);
        int nearSide = NearSide(direction);
        int light = LightColour(effect).ToArgb() & 0xFFFFFF;

        // Particle colour is deliberately not full white: the cool/warm
        // surface, luminous ridge and shadowed underside stay distinguishable.
        void Dot(Vector2 position, float width, float height, Shape shape, int rgb, int alpha,
            SpellParticleLayer layer = SpellParticleLayer.Emissive, float rotation = 0)
        {
            int w = Math.Max(1, (int)MathF.Round(width));
            pool.TryAdd(new SpellParticle(position, Vector2.Zero, now, 100, w,
                Color.FromArgb(Math.Clamp(alpha, 1, 255), Color.FromArgb(unchecked((int)0xFF000000) | rgb)),
                shape, 0, Aspect: Math.Max(1f, height) / w, Rotation: rotation, Layer: layer,
                CastsLight: false, FrameOnly: true));
        }

        void Strip(Vector2 a, Vector2 b, float wa, float wb, Shape shape, int rgb, int alpha,
            SpellParticleLayer layer = SpellParticleLayer.Emissive)
        {
            Vector2 axis = b - a;
            if (axis.LengthSquared() < .15f) return;
            Vector2 n = Vector2.Normalize(new Vector2(-axis.Y, axis.X));
            Vector2 mid = (a + b) * .5f;
            var quad = new SpriteQuad(a - mid - n * wa, b - mid - n * wb,
                a - mid + n * wa, b - mid + n * wb);
            pool.TryAdd(new SpellParticle(mid, Vector2.Zero, now, 100, 1,
                Color.FromArgb(Math.Clamp(alpha, 1, 255), Color.FromArgb(unchecked((int)0xFF000000) | rgb)),
                shape, 0, Layer: layer, CastsLight: false, FrameOnly: true, Ribbon: quad));
        }

        void AuraMaterial(Vector2 a, Vector2 b, float width, int variant)
        {
            Vector2 axis = b - a;
            if (axis.LengthSquared() < .15f) return;
            Vector2 n = Vector2.Normalize(new Vector2(-axis.Y, axis.X));
            Vector2 mid = (a + b) * .5f;
            var q = new SpriteQuad(a - mid - n * width, b - mid - n * width,
                a - mid + n * width, b - mid + n * width);
            int materialBank = WingPoseAtlas.DesignBank + effect - ArmourWings.Phoenix;
            int frame = (int)((now / 220 + variant) % WingPoseAtlas.FrameCount(materialBank));
            for (int channel = 0; channel < 3; channel++)
                pool.TryAdd(new SpellParticle(mid, Vector2.Zero, now, 100, 1,
                    Color.FromArgb((165 << 24) | (255 << (channel * 8))),
                    Shape.Wisp, 0, Layer: SpellParticleLayer.Emissive, Ribbon: q,
                    CastsLight: false, FrameOnly: true)
                    { CoveragePlane = WingPoseAtlas.Handle(materialBank, frame, channel) });
        }

        if (ArmourWings.IsAura(effect))
        {
            // Orbit half-selection is based on actual projected ground depth,
            // not the facing's left/right order. Far half draws before body,
            // near half after body. Both halves translate with the same pose.
            const int steps = 16;
            float pulse = .5f + .5f * MathF.Sin(clock * 2);
            Vector2 centre = shoulder + new Vector2(0, 17);
            for (int i = 0; i < steps; i++)
            {
                float a = i * MathF.Tau / steps + clock * .38f;
                float b = a + MathF.Tau / steps;
                float mid = (a + b) * .5f;
                int side = MathF.Sin(mid) >= 0 ? nearSide : -nearSide;
                if (onlySide != 0 && onlySide != side) continue;

                if (effect == ArmourWings.SolarAscendant)
                {
                    Vector2 Ring(float q) => centre + new Vector2(MathF.Cos(q) * (28 + pulse * 2),
                        MathF.Sin(q) * 12 - 12);
                    Vector2 x = Ring(a), y = Ring(b);
                    AuraMaterial(x, y, 5.2f, 0);
                    Strip(x, y, 1.2f, 1.2f, Shape.WindRibbon, 0xFFDD7D, 195, SpellParticleLayer.EmissiveCore);
                    if (i % 2 == 0)
                    {
                        Vector2 ray = new(MathF.Cos(mid) * 7, MathF.Sin(mid) * 4 - 5);
                        Strip(x, x + ray, 2.4f, .3f, Shape.WindRibbon, 0xE99035, 190);
                        Dot(x + ray, 4, 7, Shape.Spark, 0xFFF1BB, 170, SpellParticleLayer.EmissiveCore);
                    }
                }
                else if (effect == ArmourWings.Eclipse)
                {
                    Vector2 Ring(float q) => centre + new Vector2(MathF.Cos(q) * 29,
                        MathF.Sin(q) * 13 - 8);
                    Vector2 x = Ring(a), y = Ring(b);
                    AuraMaterial(x, y, 6.5f, 0);
                    Strip(x, y, 1, 1, Shape.WindRibbon, 0xA15CEA, 160, SpellParticleLayer.EmissiveCore);
                    if (i % 3 == 0)
                    {
                        Vector2 end = x + new Vector2(5 * MathF.Sin(mid * 3 + clock),
                            -18 - 12 * (.5f + .5f * MathF.Cos(mid + clock)));
                        Strip(x, end, 5, .5f, Shape.WindRibbon, 0x582875, 145);
                        Dot(end, 5, 8, Shape.Wisp, 0xB890F8, 115, SpellParticleLayer.EmissiveCore);
                    }
                }
                else
                {
                    // Two inclined orbital planes cross above the waist.
                    for (int ring = 0; ring < 2; ring++)
                    {
                        float slope = ring == 0 ? .54f : -.54f;
                        Vector2 Orbit(float q) => centre + new Vector2(32 * MathF.Cos(q),
                            11 * MathF.Sin(q) + slope * 32 * MathF.Cos(q) - 15);
                        Vector2 x = Orbit(a), y = Orbit(b);
                        AuraMaterial(x, y, 3.4f, ring);
                        Strip(x, y, .7f, .7f, Shape.WindRibbon, 0xBEC9FF, 175, SpellParticleLayer.EmissiveCore);
                        if ((i + ring) % 6 == 0)
                            Dot(x, 6, 6, Shape.Flash, ring == 0 ? 0xDCEFFF : 0xE5BEFF,
                                190, SpellParticleLayer.EmissiveCore);
                    }
                }
            }
            return;
        }

    }

    private static readonly int[] Sides = [-1, 1];
}

