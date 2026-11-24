using System.Drawing;

namespace CrystalM2.ArmourCreate;

// Material synthesis at native sprite resolution. RGB mask channels are geometry only:
// red = bare body, green = fitted garment. No original armour colours are sampled.
// Coverage follows the visible source pose, including the actual overlap of limbs.
public static class ArmourPainter
{
    // Standard CArmour frame bank, as defined in Client/MirObjects/Frames.cs.
    // Mount and fishing blocks use their own strides; the female bank repeats the layout.
    public static int Facing(int index)
    {
        int n = index % 808;
        (int start, int stride) = n switch
        {
            < 32 => (0, 4), < 80 => (32, 6), < 128 => (80, 6),
            < 136 => (128, 1), < 184 => (136, 6), < 232 => (184, 6),
            < 296 => (232, 8), < 344 => (296, 6), < 360 => (344, 2),
            < 384 => (360, 3), < 416 => (384, 4), < 448 => (416, 4),
            < 512 => (448, 8), < 560 => (512, 6), < 584 => (560, 3),
            < 632 => (584, 6), < 696 => (632, 8), < 744 => (696, 6),
            _ => (744, 8)
        };
        return Math.Clamp((n - start) / stride, 0, 7);
    }

    public static byte[] Paint(int width, int height, PointF[] anchors, int direction,
        ReadOnlySpan<byte> geometry)
    {
        if (width <= 0 || height <= 0 || anchors.Length != 11 ||
            geometry.Length != checked(width * height * 4) || (uint)direction >= 8)
            throw new ArgumentException("Invalid armour pose geometry.");
        int count = width * height;
        byte[] output = new byte[count * 4];
        byte[] alpha = new byte[count];
        var neck = anchors[1];
        var head = anchors[0];
        var hip = anchors[6];
        float ax = hip.X - neck.X, ay = hip.Y - neck.Y;
        float torso = MathF.Sqrt(ax * ax + ay * ay);
        if (torso < 5)
        {
            ax = hip.X - head.X; ay = hip.Y - head.Y;
            torso = MathF.Sqrt(ax * ax + ay * ay);
            if (torso < 5) { ax = 0; ay = 1; torso = 5; }
        }
        float dx = ax / torso, dy = ay / torso;
        float sx = dy, sy = -dx;
        float longest = torso + 5;
        float shoulder = Math.Clamp(MathF.Abs((anchors[3].X - anchors[2].X) * sx +
            (anchors[3].Y - anchors[2].Y) * sy) * .5f, 4f, 13f);

        // Begin with the actual fitted garment silhouette, not a handful of
        // polygons. Restore solid bare-body coverage below the collar to produce
        // a continuous undersuit, including sleeves, thighs, knees and boots.
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x, p = i * 4;
            float along = (x + .5f - neck.X) * dx + (y + .5f - neck.Y) * dy;
            int skin = geometry[p + 2], fitted = geometry[p + 1];
            int cover = fitted;
            if (along >= 1.5f) cover = Math.Max(cover, skin);
            // Preserve face/hair. The green mask is allowed to cover raised arms
            // even when an attack brings them above the neck.
            alpha[i] = (byte)cover;
            if (cover >= 96) longest = Math.Max(longest, along);
        }

        // Distance-to-edge gives every separate visible limb its own bevel.
        // Two small linear passes avoid flood fills, allocations per pixel or
        // GDI+ vector paths. Each cached frame remains deterministic.
        int[] edge = new int[count];
        for (int i = 0; i < count; i++) edge[i] = alpha[i] >= 96 ? 255 : 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            if (edge[i] == 0) continue;
            if (x == 0 || y == 0) edge[i] = 1;
            else edge[i] = Math.Min(edge[i], 1 + Math.Min(edge[i - 1], edge[i - width]));
        }
        for (int y = height - 1; y >= 0; y--)
        for (int x = width - 1; x >= 0; x--)
        {
            int i = y * width + x;
            if (edge[i] == 0) continue;
            if (x == width - 1 || y == height - 1) edge[i] = 1;
            else edge[i] = Math.Min(edge[i], 1 + Math.Min(edge[i + 1], edge[i + width]));
        }

        bool back = direction is 0 or 1 or 7;
        bool profile = direction is 2 or 6;
        float yaw = MathF.Sin(direction * MathF.PI / 4);
        // Facing right exposes the front on the right-hand side of the
        // projected torso; a back view gets its own spine and panel pattern.
        float seamShift = yaw * (profile ? .55f : .28f);
        float legLength = Math.Max(5, longest - torso);

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x, p = i * 4;
            if (alpha[i] == 0) continue;
            float along = (x + .5f - neck.X) * dx + (y + .5f - neck.Y) * dy;
            float across = (x + .5f - neck.X) * sx + (y + .5f - neck.Y) * sy;
            float t = along / torso;
            // Measure the contiguous silhouette run on the local transverse
            // axis, so a side-on or bent limb is narrower than a frontal one.
            int left = Run(x, y, -sx, -sy), right = Run(x, y, sx, sy);
            float local = (left - right) / (float)Math.Max(1, left + right);
            float u = across / shoulder;
            float seam = u - seamShift;
            float leg = (along - torso) / legLength;
            bool sleeve = t < 1.10f && (MathF.Abs(u) > 1.04f ||
                (MathF.Abs(u) > .64f && MathF.Abs(local) > .62f && t > .32f));
            bool pauldron = t < .27f && MathF.Abs(u) > .52f;
            bool hipPlate = t >= .98f && leg < .29f;
            bool boot = leg > .79f;
            bool knee = leg > .34f && leg < .49f;

            // Dark flexible joints connect the plate sections; blue enamel,
            // steel facets and gold edges are original material choices.
            int rgb;
            if (pauldron)
                rgb = t < .08f ? 0xC4CEDA : t < .20f ? 0x8DA4B9 : 0xC9A05D;
            else if (sleeve)
            {
                float cuff = Math.Clamp((t - .18f) / .88f, 0, 1);
                rgb = cuff < .16f || cuff is > .43f and < .54f ? 0x354458 :
                    cuff is > .82f and < .91f ? 0xC9A05D :
                    MathF.Abs(local) < .38f ? 0x547FB3 : 0xA9B8CA;
            }
            else if (t < .13f)
                rgb = 0xA7B5C6; // Collar, not a floating shoulder polygon.
            else if (t < .84f)
            {
                float chest = MathF.Abs(seam);
                float chevron = back ? .15f : .18f + MathF.Max(0, .52f - t) * .70f;
                rgb = chest < chevron ? (back ? 0x48678F : 0x477AB6) : 0xB9C8D9;
                if (MathF.Abs(chest - chevron) < .075f && t < .58f) rgb = 0xC6A064;
                if (t > .65f && t < .72f) rgb = 0x8499B1;
                if (profile && chest > .75f) rgb = 0x647F9D;
            }
            else if (t < .98f)
                rgb = t < .90f ? 0xC8A062 : 0x303D50; // Belt and articulation.
            else if (hipPlate)
                rgb = !back && !profile && MathF.Abs(seam) < .32f ? 0x416DAB :
                    leg < .18f ? 0x9EADC0 : 0xC2CDD8;
            else if (knee)
                rgb = leg < .40f ? 0xC3D1DE : 0x75899E;
            else if (boot)
                rgb = leg < .86f ? 0xBF985A : 0x748DA9;
            else
                rgb = MathF.Abs(local) < .45f ? 0x486FA5 : 0xA5B7CA;

            // A fixed screen-space light keeps highlights consistent as the
            // character turns. Width-aware bevels are never painted on air.
            float shade = 0.91f - local * .20f;
            if (edge[i] <= 1) shade *= local < -.15f ? .79f : .53f;
            else if (edge[i] == 2) shade *= local < 0 ? 1.16f : .91f;
            else shade *= 1.08f;
            if (sleeve && yaw * u > .4f) shade *= .88f;
            output[p] = Channel(rgb & 255, shade);
            output[p + 1] = Channel((rgb >> 8) & 255, shade);
            output[p + 2] = Channel((rgb >> 16) & 255, shade);
            output[p + 3] = alpha[i];
        }
        return output;

        int Run(int x, int y, float vx, float vy)
        {
            for (int step = 1; step <= 24; step++)
            {
                int px = (int)MathF.Round(x + vx * step);
                int py = (int)MathF.Round(y + vy * step);
                if ((uint)px >= (uint)width || (uint)py >= (uint)height ||
                    alpha[py * width + px] < 96) return step - 1;
            }
            return 24;
        }
    }

    private static byte Channel(int value, float shade) =>
        (byte)Math.Clamp((int)MathF.Round(value * shade), 0, 255);
}
