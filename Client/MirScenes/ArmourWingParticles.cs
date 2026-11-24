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
        // The tracked shoulder is already the upper back, not the body centre.
        // Seat the socket just inside the armour instead of adding a second
        // six-pixel stand-off before the membrane projection.
        return shoulder + new Vector2(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f) * 1.5f;
    }

    // Authored quill/membrane roots, in unpadded art coordinates. These
    // correspond to the starts in paint-armour-wings, paint-infernal-wings,
    // paint-seraph-wings and elemental-wings/bake, not the texture's corner.
    internal static Vector2 ArtworkRoot(byte effect) => effect switch
    {
        ArmourWings.Phoenix or ArmourWings.Frost => new(5, 43),
        ArmourWings.Moth => new(10, 52),
        ArmourWings.CelestialElectric => new(5, 46),
        ArmourWings.FelFire => new(4, 44),
        ArmourWings.DemonicBone => new(5, 46),
        ArmourWings.Archangel or ArmourWings.DawnSeraph => new(4, 45),
        ArmourWings.MagmaWyrm or ArmourWings.TidalLeviathan => new(5, 45),
        ArmourWings.GeodeTitan => new(7, 45),
        ArmourWings.Tempest or ArmourWings.Thunderstorm => new(5, 47),
        ArmourWings.VerdantMonarch => new(8, 47),
        ArmourWings.Venom => new(6, 47),
        _ => new(4, 44)
    };

    private static Vector2 Rotate(Vector2 v, float angle)
    {
        if (angle == 0) return v;
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    internal static Vector2 Shoulder(byte effect, bool mounted) =>
        new(24, (mounted ? -42 : -24) - (effect is ArmourWings.FelFire or ArmourWings.Archangel or ArmourWings.DawnSeraph ? 8 : 0));

    // Shared by live rendering and the visual regression exporters. Keep
    // orbiting auras on their existing centre; only physical wings use a
    // direct skeletal upper-back socket, independent of the design's rest height.
    internal static bool TryGetBodyMotion(byte effect, int bodyFrame, int bank,
        MirDirection direction, bool mounted, out WingPoseMotion motion)
    {
        if (!WingBodyPose.TryGetMotion(bodyFrame, bank, (int)direction, mounted, out motion)) return false;
        if (ArmourWings.IsAura(effect)) return true;
        if (!WingBodyPose.TryGetUpperBack(bodyFrame, bank, (int)direction, out Vector2 upperBack)) return false;
        if (!WingBodyPose.TryGetTorsoFrame(bodyFrame, bank, (int)direction, mounted, out var torso)) return false;
        // The rigid shoulder basis already contains the torso bank. Do not
        // apply the same body lean a second time via the secondary roll spring.
        motion = motion with { Offset = upperBack - Shoulder(effect, mounted), TorsoFrame = torso.ForWingTwist(), Roll = 0 };
        return true;
    }

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
        MirDirection direction, long now, uint seed, int onlySide = 0, float torsoYaw = 0, float torsoRoll = 0,
        float torsoPitch = 0, WingTorsoFrame? torsoFrame = null, float legacyLift = 0)
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
        float baseYaw = (int)direction * (MathF.PI / 4);
        Vector2 bodyBack = new(-MathF.Sin(baseYaw), .49f * MathF.Cos(baseYaw));
        // Rotate the vertical axis about the two shoulder sockets, then project
        // onto the map. Signed height puts feathers either side of that hinge.
        var (sinPitch, cosPitch) = MathF.SinCos(torsoPitch);
        Vector2 lean = bodyBack * sinPitch + Vector2.UnitY * (cosPitch - 1);
        if (ArmourWings.IsAura(effect))
        {
            EmitElemental(pool, effect, shoulder, direction, now, seed, onlySide, lean);
            return;
        }

        // Nominal geometry is calculated in the body's fixed facing basis.
        // Dynamic angles may only deform the free span, never the socket.
        float yaw = baseYaw;
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
        bool elemental = ArmourWings.IsElemental(effect);
        float tipLift = felFire ? 36 * north * north : seraph ? 9 + 14 * north * north : 0;
        int segments = felFire || seraph || elemental ? 8 : 6;
        Vector2 artRoot = ArtworkRoot(effect);
        int rootColumn = (int)artRoot.X - membrane.X;
        float rootTextureU = rootColumn / (float)membrane.Width;
        float rootTextureV = (artRoot.Y - membrane.Y) / membrane.Height;
        float rootU = artRoot.X / artWidth;
        int rootSegment = Math.Clamp((int)MathF.Round(rootTextureU * segments), 1, segments - 1);
        // Keep the existing strip count and texture sampling. Insert the
        // physical root as an exact shared edge of two neighbouring strips.
        float Boundary(int segment) => segment == rootSegment ? rootTextureU :
            MathF.Round(segment * membrane.Width / (float)segments) / membrane.Width;
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
            Vector2 nominalAcross = new(MathF.Cos(baseYaw), MathF.Sin(baseYaw) * .49f);
            Vector2 root = torsoFrame is { } torso
                ? shoulder + torso.SocketOffset(nominalAcross, side, 5.5f, attachment - shoulder)
                : attachment + nominalAcross * (side * 5.5f);
            int opacity = (int)(218 + 12 * MathF.Sin(yaw) * side + 8 * flap);

            // Sweep the actual wing behind the actor as it turns edge-on.
            // The near wing extends farther back than the far wing, so they
            // do not collapse into the same line at a 90-degree facing.
            float spanLength = seraph ? 66 + 2 * flap : (felFire ? 60 : elemental ? 54 : 48) + 6 * flap;
            float sweepLength = (19 + 6 * flap + profile * profile * (near ? 23 : 12)) * RearDepthScale;
            float sweepCurve = (5 + 7 * profile) * RearDepthScale;
            bool rearDiagonal = elemental && direction is (MirDirection.UpLeft or MirDirection.UpRight);
            if (rearDiagonal)
                sweepLength = Math.Min(sweepLength, spanLength * .40f - sweepCurve);
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
                // Lift the leading edge into a high arch without moving the shoulder
                // socket or dragging the hanging lobes up with it.
                if (elemental)
                {
                    float crown = Math.Clamp((88 - y) / 65, .15f, 1);
                    float lift = 20 + 32 * north;
                    if (rearDiagonal) lift += Math.Max(0, side * across.Y * spanLength) + 6;
                    arch += lift * (t * (2 - t)) * crown;
                }
                // The raised outer rim widens slightly as well as receding.
                // Match the vertical thickness offset to the projected rear
                // axis so a south-facing wing cannot cancel its own depth.
                float rearSign = MathF.Cos(yaw) < 0 ? -1f : 1f;
                // Distal deformation with a fixed root and a minimum rear
                // half-plane in profile. Rotation cannot swing the socket.
                float free = Math.Clamp((t - .12f) / .88f, 0, 1);
                float flex = free * free * (3 - 2 * free);
                float localYaw = baseYaw + torsoYaw * flex;
                Vector2 localAcross = new(MathF.Cos(localYaw), MathF.Sin(localYaw) * .49f);
                Vector2 localBack = new(-MathF.Sin(localYaw), MathF.Cos(localYaw) * .49f);
                float down = height * .55f - 4 * u * flap + 4 * t * t - arch + shell * depth * .30f * rearSign * flex;
                // Twist is a change in horizontal depth and socket placement,
                // not a screen-space rotation of the feather column. Retain
                // the neutral vertical projection and lift the rearward span
                // when the character pitches forwards. The legacy frame
                // envelope provides an additional, bounded upward reference.
                float lateral = side * (span + shell * depth * .23f * flex);
                float rearward = sweep + (camber + twist + shell * depth * flex) * RearDepthScale;
                float pitchLift = Math.Max(0, sinPitch) * Math.Max(0, sweep) * flex;
                float rise = Math.Max(pitchLift, Math.Clamp(legacyLift, 0, 12) * flex);
                Vector2 projected = new(localAcross.X * lateral + localBack.X * rearward,
                    across.Y * lateral + back.Y * rearward + down - rise);
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

            // Pin the actual rendered material, not the analytical curve at
            // its centre. The rasterizer linearly interpolates the two ends
            // of a shared mesh edge. Subtract that exact projected root to
            // prevent flap, camber, pitch or padding from opening a gap.
            int top = membrane.Y, bottom = top + membrane.Height;
            Vector2 correction = Vector2.Lerp(Project(rootU, top), Project(rootU, bottom), rootTextureV);
            // Shoulder twist moves the two sockets independently, never
            // rotates the entire wing in screen X/Y. The upright membrane
            // stays pinned while its rearward span lifts and flexes.
            Vector2 Pinned(float u, float y, float shell = 0) => Project(u, y, shell) - correction;
            for (int segment = 0; segment < segments; segment++)
            {
                float a = Boundary(segment);
                float b = Boundary(segment + 1);
                float left = segment == rootSegment ? rootU : (membrane.X + membrane.Width * a) / artWidth;
                float right = segment + 1 == rootSegment ? rootU : (membrane.X + membrane.Width * b) / artWidth;
                var quad = new SpriteQuad(Pinned(left, top), Pinned(right, top), Pinned(left, bottom), Pinned(right, bottom));
                var underside = new SpriteQuad(Pinned(left, top, 1), Pinned(right, top, 1),
                    Pinned(left, bottom, 1), Pinned(right, bottom, 1));
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
                var mote = root + Pinned(span, y);
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
    // Distinct orbiting sun crests, amethyst crescents and inclined star instruments.
    // Depth splitting follows each ornament's actual orbital position.
    private static void EmitElemental(SpellParticlePool pool, byte effect, Vector2 shoulder,
        MirDirection direction, long now, uint seed, int onlySide, Vector2 lean)
    {
        bool solar = effect == ArmourWings.SolarAscendant, eclipse = effect == ArmourWings.Eclipse;
        int count = solar ? 8 : eclipse ? 5 : 6;
        int bank = WingPoseAtlas.DesignBank + effect - ArmourWings.Phoenix;
        int nearSide = NearSide(direction);
        float phase = (now % 8200) * MathF.Tau / 8200 + (seed % 97) * .06f;
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count + phase;
            float x = MathF.Cos(a), depth = MathF.Sin(a);
            if (onlySide != 0 && onlySide != (depth >= 0 ? nearSide : -nearSide)) continue;
            float radius = solar ? 32 : eclipse ? 35 : 39;
            float altitude = solar ? -21 : eclipse ? -17 : (i % 2 == 0 ? -27 : -9);
            float downFromShoulder = altitude + depth * (solar ? 27 : 17) - x * (eclipse ? 16 : 6);
            var position = shoulder + new Vector2(radius * x, downFromShoulder) + lean * downFromShoulder;
            float angle = solar ? MathF.Atan2(depth * 24, x * radius) + MathF.PI / 2 :
                eclipse ? a + .45f : -.48f * a + i;
            float size = (solar ? 23 : eclipse ? 30 : 27) * (.85f + .12f * depth);
            Vector2 right = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * size;
            Vector2 down = new Vector2(-MathF.Sin(angle), MathF.Cos(angle)) * size;
            right += lean * right.Y;
            down += lean * down.Y;
            var surface = new SpriteQuad(-right-down,right-down,-right+down,right+down);
            int frame = (int)((now / 240 + i) % WingPoseAtlas.FrameCount(bank));
            int alpha = (int)(194 + 39 * depth);
            for (int channel = 0; channel < 3; channel++)
                pool.TryAdd(new SpellParticle(position, Vector2.Zero, now, 100, 1,
                    Color.FromArgb(alpha << 24 | 255 << (8 * channel)), Shape.Wisp, 0,
                    Layer: SpellParticleLayer.Emissive, Ribbon: surface, CastsLight: false, FrameOnly: true)
                    { CoveragePlane = WingPoseAtlas.Handle(bank,frame,channel) });
            pool.TryAdd(new SpellParticle(position + right * .21f, Vector2.Zero, now, 100, 5,
                Color.FromArgb((int)(110 + 60 * MathF.Sin(a * 3)), LightColour(effect)), Shape.Spark, 0,
                Layer: SpellParticleLayer.EmissiveCore, CastsLight: false, FrameOnly: true));
        }
    }
    private static readonly int[] Sides = [-1, 1];
}
