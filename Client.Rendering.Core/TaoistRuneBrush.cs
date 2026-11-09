using System.Numerics;

namespace CrystalM2.Rendering.Core;

// Centre lines and brush pressure traced from the legacy Taoist rune frames.
public static class TaoistRuneBrush
{
    private static readonly Vector3[][] Soul =
    [
        Trace((100, 34, 1.0f), (104, 38, 2.8f), (110, 40, 3.4f), (112, 44, 1.2f)),
        Trace((64, 46, 2.4f), (77, 46, 1.6f), (94, 47, 2.0f), (111, 46, 1.5f), (131, 46, 1.5f), (146, 44, 2.9f), (154, 46, 2.0f)),
        Trace((60, 37, 2.2f), (65, 43, 5.6f), (65, 57, 4.8f), (63, 72, 5.0f), (59, 86, 4.9f), (48, 107, 4.0f),
            (33, 121, 2.3f), (25, 125, .5f)),
        Trace((72, 57, 1.0f), (83, 56, 2.3f), (96, 56, 2.1f), (106, 56, 1.1f)),
        Trace((89, 53, 2.1f), (90, 58, 3.3f), (90, 66, 3.2f), (88, 73, 1.8f)),
        Trace((81, 59, 2.5f), (78, 65, 2.7f), (73, 70, 2.2f), (66, 74, .6f)),
        Trace((108, 57, 1.1f), (119, 57, 2.1f), (130, 57, 2.0f), (143, 56, 2.8f), (151, 56, 1.7f)),
        Trace((126, 53, 1.7f), (125, 59, 3.2f), (126, 67, 4.1f), (128, 74, 3.5f)),
        Trace((117, 60, 2.7f), (114, 66, 3.3f), (109, 71, 2.7f), (105, 77, .8f)),
        Trace((135, 60, 2.8f), (136, 66, 3.0f), (139, 71, 2.2f), (147, 71, 2.0f), (151, 70, .7f)),
        Trace((108, 74, 1.5f), (113, 71, 2.6f), (120, 72, 1.1f)),
        Trace((81, 76, 1.9f), (81, 85, 3.6f), (81, 94, 3.3f), (86, 98, 1.2f)),
        Trace((82, 78, 2.4f), (98, 77, 1.8f), (115, 77, 2.2f), (132, 76, 3.4f), (137, 82, 3.8f), (137, 92, 3.1f), (134, 96, 1.2f)),
        Trace((83, 87, 1.8f), (99, 86, 2.0f), (120, 86, 2.7f), (136, 86, 2.2f)),
        Trace((82, 96, 2.1f), (102, 94, 3.0f), (125, 94, 3.0f), (138, 95, 1.5f)),
        Trace((109, 77, 2.0f), (108, 87, 2.8f), (107, 96, 2.1f)),
        Trace((100, 93, 3.5f), (93, 104, 4.1f), (84, 113, 3.3f), (69, 121, 2.4f), (54, 126, .5f)),
        Trace((114, 92, 3.6f), (111, 103, 4.7f), (112, 115, 4.7f), (121, 121, 4.4f), (141, 123, 4.0f),
            (159, 123, 5.0f), (166, 117, 4.8f), (166, 106, 3.0f), (169, 101, .6f)),
        Trace((135, 97, 2.2f), (128, 109, 2.8f), (124, 116, 1.9f), (140, 113, 1.5f), (149, 114, .5f)),
        Trace((143, 106, 1.5f), (149, 111, 2.4f), (153, 118, .6f))
    ];

    private static readonly Vector3[][] Armour =
    [
        Trace((54, 26, 3.2f), (57, 33, 4.0f), (54, 52, 3.6f), (49, 72, 4.3f), (43, 93, 5.0f),
            (36, 113, 6.4f), (31, 125, 7.0f), (30, 128, 4.2f)),
        Trace((57, 34, 1.6f), (69, 34, 1.4f), (81, 34, 1.7f), (90, 33, 4.0f), (94, 34, 2.0f)),
        Trace((90, 33, 3.5f), (80, 43, 2.8f), (73, 52, 1.8f), (70, 55, 1.1f)),
        Trace((70, 55, 1.4f), (78, 61, 2.2f), (84, 69, 2.7f), (86, 79, 3.3f), (80, 88, 4.3f),
            (69, 95, 5.1f), (60, 92, 2.1f), (52, 91, 1.0f)),
        Trace((86, 44, 1.5f), (101, 44, 1.8f), (118, 45, 2.1f), (136, 45, 1.7f), (152, 44, 3.1f), (168, 44, 2.5f)),
        Trace((119, 22, 2.5f), (122, 33, 4.1f), (122, 54, 4.5f), (117, 71, 4.8f), (108, 86, 4.5f),
            (96, 101, 3.8f), (81, 113, 3.0f), (65, 121, 1.7f), (53, 126, .55f)),
        Trace((122, 64, 2.4f), (135, 63, 2.0f), (149, 62, 2.4f), (158, 63, 4.1f), (163, 65, 2.0f)),
        Trace((157, 63, 3.4f), (160, 77, 3.9f), (160, 92, 4.0f), (157, 106, 4.8f), (151, 118, 6.5f),
            (142, 120, 6.2f), (132, 115, 3.5f), (120, 111, 1.1f))
    ];

    private static readonly Vector3[][] Curse =
    [
        Trace((57, 28, 2.0f), (56, 40, 4.5f), (52, 55, 5.7f), (45, 70, 4.7f), (38, 82, 2.5f), (33, 87, .8f)),
        Trace((57, 34, 1.7f), (64, 34, 1.4f), (72, 32, 1.6f), (77, 29, 2.4f)),
        Trace((77, 29, 3.0f), (78, 44, 4.0f), (76, 59, 4.9f), (71, 73, 3.8f), (65, 82, 1.0f)),
        Trace((88, 27, 3.3f), (89, 38, 4.9f), (88, 54, 4.3f), (87, 68, 2.8f), (84, 73, .7f)),
        Trace((88, 28, 2.0f), (100, 29, 2.4f), (114, 30, 1.2f), (128, 29, 1.0f), (134, 26, 2.4f)),
        Trace((134, 26, 2.7f), (139, 32, 4.4f), (138, 46, 4.0f), (140, 58, 5.1f), (143, 68, 4.7f), (145, 75, .9f)),
        Trace((88, 63, 1.0f), (100, 60, 1.6f), (111, 60, 2.3f), (124, 61, 1.8f), (137, 59, 1.6f)),
        Trace((99, 63, 2.1f), (95, 79, 2.8f), (91, 94, 3.0f), (82, 106, 4.0f), (68, 115, 5.3f),
            (52, 119, 3.7f), (34, 121, 1.3f)),
        Trace((115, 61, 2.4f), (117, 80, 2.9f), (119, 98, 3.4f), (124, 114, 4.4f), (137, 121, 5.7f),
            (153, 120, 5.0f), (162, 113, 6.4f), (162, 102, 4.7f), (158, 89, 1.1f))
    ];

    public static int StrokeCount(SpellParticleShape glyph) => Strokes(glyph).Length;
    public static ReadOnlySpan<Vector3> Stroke(SpellParticleShape glyph, int index) => Strokes(glyph)[index];

    private static Vector3[][] Strokes(SpellParticleShape glyph) => glyph switch
    {
        SpellParticleShape.SoulGlyph => Soul,
        SpellParticleShape.ArmourGlyph => Armour,
        SpellParticleShape.CurseGlyph => Curse,
        _ => []
    };

    internal static float Coverage(SpellParticleShape glyph, float x, float y)
    {
        Vector2 point = new(x * 83, y * 65);
        float fibres = .25f * MathF.Sin(x * 108 + y * 62) + .19f * MathF.Sin(x * 143 - y * 82);
        point.X += .60f * MathF.Sin(y * 17 + x * 13);
        point.Y += .41f * MathF.Sin(x * 19 - y * 18);
        float ink = 0;
        foreach (Vector3[] stroke in Strokes(glyph))
        for (int i = 1; i < stroke.Length; i++)
        {
            Vector3 a = stroke[i - 1], b = stroke[i];
            Vector2 delta = new((b.X - a.X) * 83, (b.Y - a.Y) * 65);
            Vector2 local = point - new Vector2(a.X * 83, a.Y * 65);
            float t = Math.Clamp(Vector2.Dot(local, delta) / delta.LengthSquared(), 0, 1);
            float pressure = a.Z + (b.Z - a.Z) * t;
            float distance = (local - delta * t).Length();
            ink = MathF.Max(ink, Math.Clamp((pressure + fibres - distance) * .67f + .42f, 0, 1));
        }
        return ink;
    }

    private static Vector3[] Trace(params (float X, float Y, float Pressure)[] knots)
    {
        var points = new List<Vector3>();
        for (int i = 1; i < knots.Length; i++)
        {
            Vector3 a = Knot(i - 2), b = Knot(i - 1), c = Knot(i), d = Knot(i + 1);
            int steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(new(b.X, b.Y), new(c.X, c.Y)) / 5));
            for (int step = 0; step < steps; step++)
            {
                float t = step / (float)steps;
                Vector3 curve = .5f * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * (t * t) +
                    (-a + 3 * b - 3 * c + d) * (t * t * t));
                points.Add(Normalize(curve));
            }
        }
        points.Add(Normalize(Knot(knots.Length - 1)));
        return points.ToArray();

        Vector3 Knot(int index)
        {
            var (x, y, pressure) = knots[Math.Clamp(index, 0, knots.Length - 1)];
            return new Vector3(x, y, pressure);
        }

        static Vector3 Normalize(Vector3 point) => new((point.X - 102) / 83, (point.Y - 76) / 65, MathF.Max(.45f, point.Z));
    }
}
