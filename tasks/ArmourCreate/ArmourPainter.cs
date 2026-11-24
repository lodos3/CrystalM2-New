using System.Drawing;
using System.Numerics;

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
        ReadOnlySpan<byte> geometry, bool liveSurface = false)
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
        float hx = neck.X - head.X, hy = neck.Y - head.Y;
        float headLength = MathF.Sqrt(hx * hx + hy * hy);
        float guardX = headLength >= 3 ? hx / headLength : dx;
        float guardY = headLength >= 3 ? hy / headLength : dy;
        float collar = Math.Max(3f, headLength) + 1.25f;
        float headRadius = Math.Max(6f, headLength + 2.5f);
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
            if (liveSurface)
            {
                alpha[i] = geometry[p + 3];
                if (alpha[i] >= 96) longest = Math.Max(longest, along);
                continue;
            }
            // Head exclusion takes precedence over every source channel. The
            // fitted-difference mask can contain head pixels from shading changes.
            // A conservative collar cut and radial guard protect the face even
            // when the actor bends or rotates. Never paint a helmet implicitly.
            float px = x + .5f - head.X, py = y + .5f - head.Y;
            if (px * guardX + py * guardY <= collar ||
                px * px + py * py <= headRadius * headRadius) continue;
            int skin = geometry[p + 2], fitted = geometry[p + 1];
            int cover = fitted;
            if (along >= 1.5f) cover = Math.Max(cover, skin);
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
        // Author a relief surface before lighting it. Separate pieces have raised
        // centres, bevelled rims and recessed seams; each projects onto the same
        // native sprite pixel grid as the body.
        int[] material = new int[count];
        float[] relief = new float[count];
        byte[] metal = new byte[count]; // 0 cloth, 1 steel, 2 enamel, 3 gold

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

            // The primary design is dark mail under raised silver plates.
            // Plate boundaries curve with the cross-section rather than forming
            // flat horizontal stripes across the entire character.
            float arch = 1 - Math.Clamp(local * local, 0, 1);
            float curve = MathF.Abs(local);
            float segment = t;
            float radius = 1.8f;
            int rgb = 0x273244;
            byte finish = 0;

            if (pauldron)
            {
                segment = (t + .10f * curve) / .30f;
                radius = 3.3f;
                rgb = 0xA8BCD0; finish = 1;
                if (segment > .74f) { rgb = 0xB88C48; finish = 3; }
            }
            else if (sleeve)
            {
                float cuff = Math.Clamp((t - .18f) / .88f, 0, 1);
                if (cuff < .38f)
                {
                    segment = cuff / .38f; radius = 2.2f; rgb = 0x899DB2; finish = 1;
                }
                else if (cuff > .54f)
                {
                    segment = (cuff - .54f) / .46f; radius = 2.5f;
                    rgb = curve < .43f ? 0x315E9E : 0xA3B8D0; finish = (byte)(curve < .43f ? 2 : 1);
                    if (segment > .86f) { rgb = 0xB88C48; finish = 3; }
                }
            }
            else if (t < .20f)
            {
                segment = t / .20f;
                rgb = 0xA3B6C9; finish = 1; radius = 1.6f;
            }
            else if (t < .79f)
            {
                // Cuirass has a raised breast/back shell, central blue inlay,
                // a lower articulated fauld, and a darker receding flank.
                segment = (t - .20f) / .59f;
                radius = back ? 3.1f : 4.1f;
                float chest = MathF.Abs(seam);
                float motif = back ? .16f : .16f + MathF.Max(0, .55f - t) * .85f;
                bool inset = chest < motif;
                rgb = inset ? 0x305A96 : 0xB0C2D5; finish = (byte)(inset ? 2 : 1);
                if (MathF.Abs(chest - motif) < .075f && t < .55f)
                { rgb = 0xC6A05A; finish = 3; }
                if (profile && chest > .76f) rgb = 0x697D93;
                if (t > .65f)
                {
                    segment = (t - .65f) / .14f;
                    radius = 1.9f;
                    rgb = inset ? 0x345B93 : 0x879EB6;
                }
            }
            else if (t < .98f)
            {
                segment = (t - .79f) / .19f; radius = 1.6f;
                if (segment > .22f && segment < .69f) { rgb = 0xB99153; finish = 3; }
            }
            else if (hipPlate)
            {
                segment = leg / .29f + .10f * curve; radius = 2.6f;
                bool tabard = !back && !profile && MathF.Abs(seam) < .30f;
                rgb = tabard ? 0x305A96 : 0xA6B9CD; finish = (byte)(tabard ? 2 : 1);
                if (segment > .53f && segment < .65f) { rgb = 0x34445A; finish = 0; }
            }
            else if (knee)
            {
                segment = (leg - .34f) / .15f; radius = 2.7f; rgb = 0xB8C8D8; finish = 1;
            }
            else if (boot)
            {
                segment = (leg - .79f) / .21f; radius = 2.0f; rgb = 0x899EB5; finish = 1;
                if (segment < .23f) { rgb = 0xB99153; finish = 3; }
            }
            else
            {
                segment = leg < .34f ? (leg - .29f) / .05f : (leg - .49f) / .30f;
                radius = 2.3f;
                if (leg > .51f && leg < .77f)
                {
                    rgb = curve < .40f ? 0x345E9F : 0x9EB4CC;
                    finish = (byte)(curve < .40f ? 2 : 1);
                }
            }

            float cap = Math.Clamp(segment, 0, 1);
            float bevel = Math.Clamp(Math.Min(cap, 1 - cap) * 6f, 0, 1);
            float rim = Math.Clamp((edge[i] - .5f) / 2.2f, 0, 1);
            // Physical-looking depth proxy: a convex plate rises over the
            // cloth layer, while gold trim is a narrow raised edge.
            relief[i] = finish == 0 ? .22f + .40f * arch * rim :
                .30f + radius * (.18f + .82f * MathF.Sqrt(arch)) *
                (.22f + .78f * bevel) * (.30f + .70f * rim);
            if (finish == 3) relief[i] += .35f * rim;
            material[i] = rgb;
            metal[i] = finish;
        }

        // Light the reconstructed surface from upper-left in screen space.
        // Strong contact shadows, cool reflected light and tight specular
        // edges are deliberately quantised for readability at native scale.
        Vector3 light = Vector3.Normalize(new Vector3(-.66f, -.76f, .80f));
        Vector3 halfLight = Vector3.Normalize(light + Vector3.UnitZ);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x, p = i * 4;
            if (alpha[i] == 0) continue;
            float leftH = HeightAt(x - 1, y), rightH = HeightAt(x + 1, y);
            float topH = HeightAt(x, y - 1), bottomH = HeightAt(x, y + 1);
            Vector3 normal = Vector3.Normalize(new Vector3((leftH - rightH) * .68f,
                (topH - bottomH) * .68f, 1f));
            if (liveSurface)
            {
                // BGRA: material and occlusion, normal Y, normal X, coverage.
                // Sample at native resolution with the point sampler.
                int occlusionCode = edge[i] <= 1 ? 38 : 63;
                output[p] = (byte)(metal[i] * 64 + occlusionCode);
                output[p+1] = (byte)Math.Clamp((int)MathF.Round((normal.Y*.5f+.5f)*255),0,255);
                output[p+2] = (byte)Math.Clamp((int)MathF.Round((normal.X*.5f+.5f)*255),0,255);
                output[p+3] = alpha[i];
                continue;
            }
            float diffuse = Math.Max(0, Vector3.Dot(normal, light));
            float specular = MathF.Pow(Math.Max(0, Vector3.Dot(normal, halfLight)),
                metal[i] == 1 ? 13f : metal[i] == 3 ? 18f : 23f);
            float occlusion = metal[i] == 0 ? .78f : 1f;
            float shade = (.37f + .81f * diffuse) * occlusion;
            if (edge[i] <= 1) shade *= .72f;
            float highlight = metal[i] == 0 ? 0 : specular * (metal[i] == 1 ? 88 : metal[i] == 3 ? 65 : 36);
            int rgb = material[i];
            output[p] = Lit(rgb & 255, shade, highlight, 8);
            output[p + 1] = Lit((rgb >> 8) & 255, shade, highlight, 4);
            output[p + 2] = Lit((rgb >> 16) & 255, shade, highlight, 0);
            output[p + 3] = alpha[i];
        }

        float HeightAt(int x, int y) =>
            (uint)x < (uint)width && (uint)y < (uint)height ? relief[y * width + x] : 0;

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

    private static byte Lit(int value, float shade, float highlight, int reflected) =>
        (byte)Math.Clamp((int)MathF.Round(value * shade + highlight + reflected * (1 - Math.Min(1, shade))), 0, 255);
}
