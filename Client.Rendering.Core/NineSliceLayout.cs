using System.Drawing;

namespace CrystalM2.Rendering.Core;

public readonly record struct SliceInsets(int Left, int Top, int Right, int Bottom);
public readonly record struct NineSlicePatch(Rectangle Source, Rectangle Destination);

public static class NineSliceLayout
{
    public static int Write(Span<NineSlicePatch> output, Rectangle source, Rectangle destination, SliceInsets border)
    {
        if (output.Length < 9) throw new ArgumentException("Nine patch slots required.", nameof(output));
        if (source.Width <= 0 || source.Height <= 0 || border.Left < 0 || border.Right < 0 ||
            border.Top < 0 || border.Bottom < 0 || (long)border.Left + border.Right >= source.Width ||
            (long)border.Top + border.Bottom >= source.Height)
            throw new ArgumentException("Borders must leave a nonempty atlas centre.");
        if (destination.Width <= 0 || destination.Height <= 0) return 0;

        int left = border.Left, right = border.Right, top = border.Top, bottom = border.Bottom;
        if (left + right > destination.Width)
        {
            left = (int)((long)destination.Width * left / (left + right));
            right = destination.Width - left;
        }
        if (top + bottom > destination.Height)
        {
            top = (int)((long)destination.Height * top / (top + bottom));
            bottom = destination.Height - top;
        }
        Span<int> sx = stackalloc int[] { source.Left, source.Left + border.Left, source.Right - border.Right, source.Right };
        Span<int> sy = stackalloc int[] { source.Top, source.Top + border.Top, source.Bottom - border.Bottom, source.Bottom };
        Span<int> dx = stackalloc int[] { destination.Left, destination.Left + left, destination.Right - right, destination.Right };
        Span<int> dy = stackalloc int[] { destination.Top, destination.Top + top, destination.Bottom - bottom, destination.Bottom };
        int count = 0;
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                Rectangle src = Rectangle.FromLTRB(sx[x], sy[y], sx[x + 1], sy[y + 1]);
                Rectangle dst = Rectangle.FromLTRB(dx[x], dy[y], dx[x + 1], dy[y + 1]);
                if (src.Width > 0 && src.Height > 0 && dst.Width > 0 && dst.Height > 0)
                    output[count++] = new NineSlicePatch(src, dst);
            }
        return count;
    }
}
