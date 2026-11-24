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

    internal static Vector2 BackAttachment(Vector2 shoulder, MirDirection direction)
    {
        float yaw = (int)direction * MathF.PI / 4;
        float profile = MathF.Abs(MathF.Sin(yaw));
        return shoulder + new Vector2(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f) *
            (8.5f + 4.5f * profile * profile);
    }

    internal static Vector2 Shoulder(byte effect, bool mounted) =>
        new(24, (mounted ? -42 : -24) - (effect is ArmourWings.FelFire or ArmourWings.Archangel or ArmourWings.DawnSeraph ? 8 : 0));

    internal static Color LightColour(byte effect) => Color.FromArgb(effect switch
    {
        1 => 0xA9CCFF, 2 => 0xFFAF48, ArmourWings.Phoenix => 0xFF7635,
        ArmourWings.Frost => 0x68DEFF, ArmourWings.CelestialElectric => 0xA5ACFF,
        ArmourWings.FelFire => 0x9CF438, ArmourWings.DemonicBone => 0xB34241,
        ArmourWings.Archangel => 0xCBDFFF, ArmourWings.DawnSeraph => 0xFFD28B, _ => 0xB78AFF
    } | unchecked((int)0xFF000000));

    internal static void Emit(SpellParticlePool pool, byte effect, int bank, int pose, Vector2 shoulder,
        MirDirection direction, long now, uint seed, int onlySide = 0)
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

        float yaw = (int)direction * (MathF.PI / 4);
        Vector2 across = new(MathF.Cos(yaw), MathF.Sin(yaw) * .49f);
        Vector2 back = new(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f);
        float profile = MathF.Abs(MathF.Sin(yaw));
        Vector2 attachment = BackAttachment(shoulder, direction);
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
        int segments = felFire || seraph ? 8 : 6;
        float thickness = effect switch
        {
            ArmourWings.Phoenix => 9f, ArmourWings.Frost => 8f,
            ArmourWings.Moth => 6f, ArmourWings.CelestialElectric => 7f,
            ArmourWings.FelFire => 11f, ArmourWings.DemonicBone => 8.5f,
            ArmourWings.Archangel => 9.5f, ArmourWings.DawnSeraph => 10f, _ => 8f
        };
        int edge = LightColour(effect).ToArgb();
        foreach (int side in Sides)
        {
            if (onlySide != 0 && side != onlySide) continue;
            bool near = profile < .001f || side == NearSide(direction);
            Vector2 root = attachment + across * (side * 5.5f);
            int opacity = (int)(218 + 12 * MathF.Sin(yaw) * side + 8 * flap);

            // Sweep the actual wing behind the actor as it turns edge-on.
            // The near wing extends farther back than the far wing, so they
            // do not collapse into the same line at a 90-degree facing.
            float spanLength = seraph ? 66 + 2 * flap : (felFire ? 60 : 48) + 6 * flap;
            float sweepLength = 19 + 6 * flap + profile * profile * (near ? 23 : 12);
            float sweepCurve = 5 + 7 * profile;
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
                return across * (side * (span + shell * depth * .23f)) +
                    back * (sweep + camber + twist + shell * depth) +
                    Vector2.UnitY * (height * .55f - 4 * u * flap + 4 * t * t - arch +
                    shell * depth * .30f * rearSign);
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
            int motes = effect == ArmourWings.DemonicBone ? 2 : seraph ? 3 : 7;
            for (int i = 0; i < motes; i++)
            {
                float age = (phase / MathF.Tau + i * .381966f + (side + 1) * .18f) % 1;
                float span = .22f + i * .096f;
                float y = effect == ArmourWings.FelFire ? 44 - 17 * span - age * 34 :
                    120 - 106 * span + age * (design == 0 ? -24 : 18);
                var mote = root + Project(span, y);
                Sprite(mote, 4, design == 1 ? 6 : 4, Shape.Spark, edge, (int)(190 * MathF.Sin(age * MathF.PI)));
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
    private static readonly int[] Sides = [-1, 1];
}

