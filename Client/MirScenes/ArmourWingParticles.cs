using System.Numerics;
using CrystalM2.Rendering.Core;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

internal static class ArmourWingParticles
{
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
        MirDirection direction, long now, uint seed)
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
        int segments = felFire || seraph ? 8 : 4;
        int edge = LightColour(effect).ToArgb();
        foreach (int side in Sides)
        {
            Vector2 root = shoulder + across * (side * 4);
            int opacity = (int)(232 + 12 * MathF.Sin(yaw) * side + 8 * flap);
            Vector2 Project(float u, float y)
            {
                float sweep = u * (18 + 7 * flap) + 3 * u * u;
                float span = u * (seraph ? 66 + 2 * flap : (felFire ? 60 : 48) + 6 * flap);
                // Bend upwards progressively from the shoulder. The padded glow,
                // flame fronts and embers all follow this same projected surface.
                float arch = tipLift * MathF.Max(0, u) * MathF.Max(0, u);
                return across * (side * span) + back * sweep + Vector2.UnitY *
                    ((y - (design == 2 ? 52 : 42)) * .55f - 4 * u * flap + 4 * u * u - arch);
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
                AddFrame(firstFrame, 1 - mix);
                if (mix > 0) AddFrame(nextFrame, mix);

                void AddFrame(int index, float weight)
                {
                    int alpha = (int)MathF.Round(opacity * weight);
                    if (alpha == 0) return;
                    for (int channel = 0; channel < 3; channel++)
                        pool.TryAdd(new SpellParticle(root, Vector2.Zero, now, 100, 1,
                            Color.FromArgb((alpha << 24) | (255 << (channel * 8))),
                            Shape.Wisp, 0, Layer: SpellParticleLayer.EmissiveCore, Ribbon: quad,
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

