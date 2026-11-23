using System.Drawing;
using System.Drawing.Drawing2D;

namespace CrystalM2.ArmourCreate;

// An original drawn design. CArmour/01 supplies coverage information only; no armour pixels
// or colours are sampled from that library when these plates are painted.
public static class ArmourPainter
{
    private static readonly Color Ink = Color.FromArgb(11, 16, 28);
    private static readonly Color Midnight = Color.FromArgb(24, 34, 52);
    private static readonly Color Porcelain = Color.FromArgb(224, 231, 235);
    private static readonly Color Blue = Color.FromArgb(42, 139, 220);
    private static readonly Color Gold = Color.FromArgb(211, 165, 75);
    private static readonly Color Edge = Color.FromArgb(92, 108, 133);

    public static void Paint(Graphics g, PointF[] points)
    {
        if (points.Length != 11) throw new ArgumentException("Expected the eleven CArmour anchors.", nameof(points));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        PointF neck = points[1], a = points[2], b = points[3],
            chest = points[4], waist = points[5], hips = points[6];
        PointF down = Unit(Sub(hips, neck));
        if (Length(Sub(hips, neck)) < 5) return; // Badly foreshortened or unmappable pose.
        PointF across = new(down.Y, -down.X);
        float sideA = Dot(Sub(a, neck), across) <= Dot(Sub(b, neck), across) ? -1f : 1f;
        float sideB = -sideA;
        float s = Math.Clamp(Length(Sub(hips, neck)) / 28f, .65f, 1.35f);
        float width = Math.Clamp(Length(Sub(a, b)) * .30f, 3f, 8f);

        PointF outerA = Move(a, across, sideA * 2.7f * s);
        PointF outerB = Move(b, across, sideB * 2.7f * s);
        PointF waistA = Move(waist, across, sideA * width);
        PointF waistB = Move(waist, across, sideB * width);
        PointF lowerA = Move(hips, across, sideA * width * .80f);
        PointF lowerB = Move(hips, across, sideB * width * .80f);
        PointF top = Move(neck, down, 2.2f * s);

        // Plate outline and dark enamel are authored per pose, following the anatomical axis.
        PointF[] cuirass = [outerA, top, outerB, waistB, lowerB, lowerA, waistA];
        using (var path = new GraphicsPath())
        {
            path.AddPolygon(cuirass);
            using var enamel = new LinearGradientBrush(outerA,
                Length(Sub(outerA, outerB)) >= 1 ? outerB : Move(outerA, across, 1),
                Midnight, Ink);
            g.FillPath(enamel, path);
            using var seam = new Pen(Edge, Math.Max(.8f, s * .9f));
            g.DrawPath(seam, path);
        }

        // Asymmetric inset white ceramic plates, separated by a blue articulated spine.
        PointF centre = Move(chest, down, 1.5f * s);
        PointF midWaist = Move(waist, down, -1f * s);
        using (var white = new SolidBrush(Porcelain))
        {
            g.FillPolygon(white, [Move(a, down, 3f*s), Move(centre, across, sideA*1.5f*s),
                Move(midWaist, across, sideA*1.7f*s), Move(waistA, down, -2f*s)]);
            g.FillPolygon(white, [Move(b, down, 3f*s), Move(waistB, down, -2f*s),
                Move(midWaist, across, sideB*1.7f*s), Move(centre, across, sideB*1.5f*s)]);
        }
        using (var blue = new Pen(Blue, Math.Max(1.1f, s*2.0f)))
        {
            g.DrawLine(blue, Move(top, down, 2f*s), centre);
            g.DrawLine(blue, centre, midWaist);
            g.DrawLine(blue, Move(waistA, down, -1.8f*s), Move(waist, down, 1f*s));
            g.DrawLine(blue, Move(waist, down, 1f*s), Move(waistB, down, -1.8f*s));
        }
        using (var gold = new Pen(Gold, Math.Max(.8f, s)))
        {
            g.DrawLine(gold, Move(a, down, 2f*s), Move(top, down, 1f*s));
            g.DrawLine(gold, Move(top, down, 1f*s), Move(b, down, 2f*s));
            g.DrawLine(gold, waistA, waistB);
        }

        Shoulder(g, a, down, across, sideA, s);
        Shoulder(g, b, down, across, sideB, s);
        Greave(g, points[7], points[9], s);
        Greave(g, points[8], points[10], s);

        using (var gold = new SolidBrush(Gold))
        using (var blue = new SolidBrush(Blue))
        {
            float dot = Math.Max(1.5f, s*2.3f);
            g.FillEllipse(gold, waist.X-dot/2, waist.Y-dot/2, dot, dot);
            g.FillEllipse(blue, chest.X-dot/3, chest.Y-dot/3, dot*.66f, dot*.66f);
        }
    }

    private static void Shoulder(Graphics g, PointF centre, PointF down,
        PointF across, float side, float s)
    {
        PointF tip = Move(Move(centre, across, side * 4.5f*s), down, -2.2f*s);
        PointF[] plate = [
            Move(centre, down, -2f*s), tip,
            Move(Move(centre, across, side*6f*s), down, 1f*s),
            Move(Move(centre, across, side*2f*s), down, 5f*s),
            Move(centre, down, 3f*s)
        ];
        using var dark = new SolidBrush(Ink);
        using var white = new SolidBrush(Porcelain);
        using var outline = new Pen(Edge, Math.Max(.8f, s));
        using var blue = new Pen(Blue, Math.Max(1f, s*1.3f));
        using var gold = new Pen(Gold, Math.Max(.6f, s*.75f));
        g.FillPolygon(dark, plate);
        g.DrawPolygon(outline, plate);
        g.FillPolygon(white, [Move(centre, down, -.5f*s), tip,
            Move(centre, across, side*3.2f*s), Move(centre, down, 2.6f*s)]);
        g.DrawLine(blue, tip, Move(centre, down, 2.5f*s));
        g.DrawLine(gold, Move(centre, down, -2f*s), tip);
    }

    private static void Greave(Graphics g, PointF knee, PointF boot, float s)
    {
        PointF direction = Sub(boot, knee);
        float length = Length(direction);
        if (length < 3) return;
        direction = Unit(direction);
        PointF sideways = new(direction.Y, -direction.X);
        float wide = Math.Clamp(length * .24f, 1.5f*s, 4.5f*s);
        PointF upper = Move(knee, direction, 1.5f*s);
        PointF lower = Move(boot, direction, -2f*s);
        PointF[] leg = [Move(upper, sideways, -wide), Move(upper, sideways, wide),
            Move(lower, sideways, wide*.70f), Move(lower, sideways, -wide*.70f)];
        using var black = new SolidBrush(Ink);
        using var outline = new Pen(Edge, Math.Max(.6f, .8f*s));
        using var white = new Pen(Porcelain, Math.Max(.9f, s*1.35f));
        using var blue = new Pen(Blue, Math.Max(.9f, s));
        using var gold = new Pen(Gold, Math.Max(.6f, .8f*s));
        g.FillPolygon(black, leg);
        g.DrawPolygon(outline, leg);
        g.DrawLine(white, Move(upper, sideways, -wide*.57f), Move(lower, sideways, -wide*.43f));
        g.DrawLine(blue, upper, lower);
        g.DrawLine(gold, Move(lower, sideways, -wide*.60f), Move(lower, sideways, wide*.60f));
    }

    private static PointF Sub(PointF a, PointF b) => new(a.X-b.X, a.Y-b.Y);
    private static PointF Move(PointF a, PointF delta, float amount) =>
        new(a.X+delta.X*amount, a.Y+delta.Y*amount);
    private static float Dot(PointF a, PointF b) => a.X*b.X+a.Y*b.Y;
    private static float Length(PointF a) => MathF.Sqrt(a.X*a.X+a.Y*a.Y);
    private static PointF Unit(PointF a)
    {
        float length = Length(a);
        return length < .01f ? new PointF(0, 1) : new PointF(a.X/length, a.Y/length);
    }
}
