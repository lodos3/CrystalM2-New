using System.Drawing;

namespace CrystalM2.Rendering.Core;

public static partial class DarkFantasyAtlas
{
    public const int OrbSize = 112;
    private const float GlassRadius = 44;
    public static readonly Rectangle OrbBasin = new(0, 160, OrbSize, OrbSize);
    public static readonly Rectangle OrbRim = new(112, 160, OrbSize, OrbSize);
    public static readonly Rectangle LifeLiquid = new(0, 272, OrbSize, OrbSize);
    public static readonly Rectangle ManaLiquid = new(112, 272, OrbSize, OrbSize);

    public static int OrbFillTop(double fraction)
    {
        fraction = double.IsFinite(fraction) ? Math.Clamp(fraction, 0, 1) : 0;
        return OrbSize / 2 + (int)GlassRadius - (int)Math.Round(2 * GlassRadius * fraction);
    }

    private static void PaintOrbs(byte[] pixels)
    {
        for (int y = 0; y < OrbSize; y++)
        for (int x = 0; x < OrbSize; x++)
        {
            float dx = x + .5f - OrbSize / 2, dy = y + .5f - OrbSize / 2;
            float radius = MathF.Sqrt(dx * dx + dy * dy);
            float edge = Math.Clamp(GlassRadius + .5f - radius, 0, 1);
            if (edge > 0)
            {
                float shadow = Math.Clamp(radius / GlassRadius, 0, 1);
                PutOrb(pixels, OrbBasin, x, y, edge,
                    20 - 9 * shadow, 20 - 8 * shadow, 26 - 8 * shadow);
                float light = Math.Clamp(1 - (dx * dx + (dy - 18) * (dy - 18)) / 3300, 0, 1);
                float current = MathF.Sin(dy * .18f + MathF.Cos(dx * .09f) * 2.7f) *
                    MathF.Sin(dx * .11f - dy * .08f);
                float sheen = Math.Max(0, current) * 28 * (1 - shadow * .8f);
                PutOrb(pixels, LifeLiquid, x, y, edge,
                    58 + 140 * light + sheen - 36 * shadow, 6 + 36 * light + sheen * .42f,
                    14 + 25 * light + sheen * .25f);
                PutOrb(pixels, ManaLiquid, x, y, edge,
                    8 + 33 * light + sheen * .3f, 26 + 83 * light + sheen * .65f,
                    66 + 145 * light + sheen - 32 * shadow);
            }

            float angle = MathF.Atan2(dy, dx);
            float outer = 51 + MathF.Cos(8 * angle) * 1.4f;
            float metal = Math.Clamp(outer + .5f - radius, 0, 1) *
                Math.Clamp(radius - GlassRadius + .5f, 0, 1);
            if (metal > 0)
            {
                float bevel = radius switch
                {
                    < 45 => .28f,
                    < 46.8f => .89f,
                    < 48.2f => .46f,
                    _ => radius > outer - 1.5f ? .28f : .72f
                };
                bevel += (-dy / 52 - dx / 92) * .19f;
                PutOrb(pixels, OrbRim, x, y, metal, 202 * bevel, 158 * bevel, 99 * bevel);
            }

            // Small facets on the compass points and a broad reflection on the upper glass.
            float facet = Math.Min(Math.Abs(dx) + Math.Abs(Math.Abs(dy) - 50),
                Math.Abs(dy) + Math.Abs(Math.Abs(dx) - 50));
            if (facet < 4.5f)
            {
                float bronze = facet < 1.6f ? 1 : facet < 3.2f ? .7f : .33f;
                PutOrb(pixels, OrbRim, x, y, 1, 217 * bronze, 173 * bronze, 107 * bronze);
            }
            else if (edge > 0 && dy < -9 && radius < GlassRadius - 1)
            {
                float reflection = Math.Clamp(1 - (dx + 10) * (dx + 10) / 980 -
                    (dy + 29) * (dy + 29) / 150, 0, 1);
                PutOrb(pixels, OrbRim, x, y, reflection * .30f, 224, 221, 216);
            }
        }
    }

    private static void PutOrb(byte[] pixels, Rectangle part, int x, int y, float alpha, float red, float green, float blue) =>
        Put(pixels, part.X + x, part.Y + y, Color.FromArgb((int)(255 * alpha),
            (int)Math.Clamp(red, 0, 255), (int)Math.Clamp(green, 0, 255), (int)Math.Clamp(blue, 0, 255)));
}
