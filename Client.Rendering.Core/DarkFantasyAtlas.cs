using System.Drawing;

namespace CrystalM2.Rendering.Core;

public enum UiSurface { None, Window, Inset, Button, Slot, Header, ScrollTrack, ScrollThumb }
public enum UiVisualState { Normal, Hover, Pressed, Disabled, Selected, Focused }
public enum UiGlyph { None, Close, Up, Down, Left, Right, Plus, Minus, Check, Grip,
    Bag, Person, Sword, Book, Gear, Menu, Map, Mail, Group, Shield, Coin, Trash, Help, Search, Lock, Eye }
public readonly record struct UiAtlasPart(Rectangle Source, SliceInsets Border);

public static class DarkFantasyPalette
{
    public static readonly Color Text = Color.FromArgb(232, 226, 211);
    public static readonly Color MutedText = Color.FromArgb(171, 166, 155);
    public static readonly Color DisabledText = Color.FromArgb(144, 141, 135);
    public static readonly Color Accent = Color.FromArgb(184, 153, 99);
    public static readonly Color Input = Color.FromArgb(17, 19, 23);
    public static readonly Color Border = Color.FromArgb(89, 79, 61);
    public static readonly Color Health = Color.FromArgb(146, 48, 48);
    public static readonly Color Mana = Color.FromArgb(62, 101, 145);
    public static readonly Color Experience = Color.FromArgb(166, 140, 79);
}

// One 256 KiB generated texture replaces window-sized chrome bitmaps.
// No game artwork, font rasteriser, file IO or Windows API is involved.
public static class DarkFantasyAtlas
{
    public const int Width = 256, Height = 256;
    private static readonly UiAtlasPart Window = new(new(0, 0, 64, 64), new(14, 14, 14, 14));
    private static readonly UiAtlasPart Inset = new(new(64, 0, 32, 32), new(5, 5, 5, 5));
    private static readonly UiAtlasPart Button = new(new(96, 0, 48, 32), new(7, 7, 7, 7));
    private static readonly UiAtlasPart Hover = new(new(144, 0, 48, 32), new(7, 7, 7, 7));
    private static readonly UiAtlasPart Pressed = new(new(192, 0, 48, 32), new(7, 7, 7, 7));
    private static readonly UiAtlasPart Disabled = new(new(96, 32, 48, 32), new(7, 7, 7, 7));
    private static readonly UiAtlasPart Selected = new(new(144, 32, 48, 32), new(7, 7, 7, 7));
    private static readonly UiAtlasPart Slot = new(new(192, 32, 40, 40), new(5, 5, 5, 5));
    private static readonly UiAtlasPart Header = new(new(0, 64, 64, 32), new(12, 6, 12, 6));
    private static readonly UiAtlasPart Track = new(new(64, 40, 24, 48), new(4, 4, 4, 4));
    private static readonly UiAtlasPart Thumb = new(new(88, 72, 24, 32), new(4, 4, 4, 4));
    public static readonly Rectangle WhitePixel = new(248, 0, 1, 1);

    public static UiAtlasPart Part(UiSurface surface, UiVisualState state = UiVisualState.Normal) => surface switch
    {
        UiSurface.Window => Window,
        UiSurface.Inset => Inset,
        UiSurface.Header => Header,
        UiSurface.ScrollTrack => Track,
        UiSurface.ScrollThumb => state == UiVisualState.Hover ? Hover : Thumb,
        UiSurface.Slot => state is UiVisualState.Hover or UiVisualState.Selected or UiVisualState.Focused ? Selected : Slot,
        UiSurface.Button => state switch
        {
            UiVisualState.Disabled => Disabled,
            UiVisualState.Pressed => Pressed,
            UiVisualState.Hover or UiVisualState.Focused => Hover,
            UiVisualState.Selected => Selected,
            _ => Button
        },
        _ => throw new ArgumentOutOfRangeException(nameof(surface))
    };

    public static Rectangle Glyph(UiGlyph glyph)
    {
        if (glyph == UiGlyph.None) return Rectangle.Empty;
        int index = (int)glyph - 1;
        return new(index % 16 * 16, 128 + index / 16 * 16, 16, 16);
    }

    public static byte[] Generate()
    {
        byte[] pixels = new byte[Width * Height * 4];
        Paint(pixels, Window, Color.FromArgb(25, 27, 31), DarkFantasyPalette.Border, true);
        Paint(pixels, Inset, DarkFantasyPalette.Input, Color.FromArgb(58, 57, 54));
        Paint(pixels, Button, Color.FromArgb(37, 37, 39), Color.FromArgb(106, 92, 68));
        Paint(pixels, Hover, Color.FromArgb(54, 48, 37), DarkFantasyPalette.Accent);
        Paint(pixels, Pressed, Color.FromArgb(22, 22, 25), Color.FromArgb(139, 112, 70));
        Paint(pixels, Disabled, Color.FromArgb(27, 28, 31), Color.FromArgb(65, 64, 61));
        Paint(pixels, Selected, Color.FromArgb(47, 41, 31), DarkFantasyPalette.Accent);
        Paint(pixels, Slot, Color.FromArgb(15, 17, 21), Color.FromArgb(68, 66, 59));
        Paint(pixels, Header, Color.FromArgb(31, 31, 33), Color.FromArgb(106, 90, 64));
        Paint(pixels, Track, Color.FromArgb(12, 14, 17), Color.FromArgb(48, 48, 47));
        Paint(pixels, Thumb, Color.FromArgb(54, 51, 44), Color.FromArgb(116, 99, 72));
        foreach (UiGlyph glyph in Enum.GetValues<UiGlyph>())
            if (glyph != UiGlyph.None) PaintGlyph(pixels, glyph);
        Put(pixels, WhitePixel.X, WhitePixel.Y, Color.White);
        return pixels;
    }

    private static void Paint(byte[] pixels, UiAtlasPart part, Color fill, Color edge, bool ornaments = false)
    {
        Rectangle r = part.Source;
        for (int y = 0; y < r.Height; y++)
            for (int x = 0; x < r.Width; x++)
            {
                int cx = Math.Min(x, r.Width - 1 - x), cy = Math.Min(y, r.Height - 1 - y);
                if (cx + cy < 3) continue; // Restrained clipped metal corners.
                int distance = Math.Min(cx, cy);
                Color colour = distance switch
                {
                    0 => Color.FromArgb(235, 5, 6, 8),
                    1 => edge,
                    2 => Color.FromArgb(11, 12, 15),
                    3 => Color.FromArgb(52, 51, 49),
                    _ => Color.FromArgb(Math.Clamp(fill.R + 3 - y * 6 / r.Height, 0, 255),
                        Math.Clamp(fill.G + 3 - y * 6 / r.Height, 0, 255),
                        Math.Clamp(fill.B + 3 - y * 6 / r.Height, 0, 255))
                };
                if (ornaments && cx < 13 && cy < 13 && Math.Abs(cx - 8) + Math.Abs(cy - 8) == 3)
                    colour = DarkFantasyPalette.Accent;
                if (part == Selected && y == r.Height - 5 && cx >= part.Border.Left)
                    colour = DarkFantasyPalette.Accent;
                Put(pixels, r.X + x, r.Y + y, colour);
            }
    }

    private static void PaintGlyph(byte[] pixels, UiGlyph glyph)
    {
        Rectangle r = Glyph(glyph);
        for (int y = 3; y <= 12; y++)
            for (int x = 3; x <= 12; x++)
            {
                bool ink = glyph switch
                {
                    UiGlyph.Close => x == y || x + y == 15,
                    UiGlyph.Up => y >= 5 && y <= 9 && Math.Abs(x - 7) == 9 - y,
                    UiGlyph.Down => y >= 6 && y <= 10 && Math.Abs(x - 7) == y - 6,
                    UiGlyph.Left => x >= 5 && x <= 9 && Math.Abs(y - 7) == 9 - x,
                    UiGlyph.Right => x >= 6 && x <= 10 && Math.Abs(y - 7) == x - 6,
                    UiGlyph.Plus => x == 7 || y == 7,
                    UiGlyph.Minus => y == 7,
                    UiGlyph.Check => (x <= 6 && y == x + 3) || (x >= 6 && y == 15 - x),
                    UiGlyph.Grip => y is 6 or 9 && x is >= 5 and <= 10,
                    UiGlyph.Bag => (x is 4 or 11 && y is >= 6 and <= 12) || (y is 6 or 12 && x is >= 4 and <= 11) ||
                        (y is >= 3 and <= 5 && (x is 6 or 9 || y == 3 && x is >= 6 and <= 9)),
                    UiGlyph.Person => (x is >= 6 and <= 9 && y is >= 3 and <= 6) ||
                        (y is >= 8 and <= 12 && Math.Abs(x - 7) <= (y - 6) / 2 + 1),
                    UiGlyph.Sword => (x + y == 14 && x is >= 5 and <= 11) ||
                        (x + y == 15 && x is >= 6 and <= 11) || (x - y == -4 && x is >= 4 and <= 7) || (x == 4 && y == 11),
                    UiGlyph.Book => (x is 3 or 12 && y is >= 4 and <= 11) ||
                        (y is 4 or 11 && x is >= 3 and <= 12) || (x == 7 && y is >= 4 and <= 11),
                    UiGlyph.Gear => Math.Abs(x - 7) + Math.Abs(y - 7) is 4 or 5 ||
                        (x == 7 && y is 3 or 12) || (y == 7 && x is 3 or 12),
                    UiGlyph.Menu => y is 4 or 7 or 10 && x is >= 3 and <= 12,
                    UiGlyph.Map => (x is 3 or 7 or 12 && y is >= 4 and <= 11) ||
                        ((y == 4 + (x / 4) % 2 || y == 10 + (x / 4) % 2) && x is >= 3 and <= 12),
                    UiGlyph.Mail => (x is 3 or 12 && y is >= 4 and <= 11) ||
                        (y is 4 or 11 && x is >= 3 and <= 12) || (y == 8 - Math.Abs(x - 7) && x is >= 4 and <= 11),
                    UiGlyph.Group => (y is >= 3 and <= 5 && (x is >= 4 and <= 6 || x is >= 9 and <= 11)) ||
                        (y is >= 7 and <= 11 && (Math.Abs(x - 5) <= 1 || Math.Abs(x - 10) <= 1)),
                    UiGlyph.Shield => (y == 4 && x is >= 4 and <= 11) ||
                        (y is >= 5 and <= 8 && x is 4 or 11) || (y is >= 9 and <= 12 && Math.Abs(x - 7) <= 12 - y),
                    UiGlyph.Coin => Math.Abs(x - 7) + Math.Abs(y - 7) is 4 or 5 || (x == 7 && y is >= 5 and <= 9),
                    UiGlyph.Trash => (y is 5 or 12 && x is >= 4 and <= 11) || (y is >= 6 and <= 11 && x is 5 or 10) || (y == 3 && x is 7 or 8),
                    UiGlyph.Help => (y == 4 && x is >= 5 and <= 9) || (x == 10 && y is 5 or 6) ||
                        (x == 8 && y is 7 or 8) || (x == 8 && y == 11),
                    UiGlyph.Search => Math.Abs(x - 6) + Math.Abs(y - 6) == 3 || (x == y && x is >= 8 and <= 12),
                    UiGlyph.Lock => (y is >= 7 and <= 12 && x is >= 4 and <= 11) ||
                        (y is >= 3 and <= 6 && (x is 6 or 9 || y == 3 && x is >= 6 and <= 9)),
                    UiGlyph.Eye => (Math.Abs(x - 7) + 2 * Math.Abs(y - 7) is 4 or 5) || (x == 7 && y is >= 6 and <= 8),
                    _ => false
                };
                if (ink) Put(pixels, r.X + x, r.Y + y, Color.White);
            }
    }

    private static void Put(byte[] pixels, int x, int y, Color colour)
    {
        int offset = (y * Width + x) * 4;
        pixels[offset] = colour.B;
        pixels[offset + 1] = colour.G;
        pixels[offset + 2] = colour.R;
        pixels[offset + 3] = colour.A;
    }
}
