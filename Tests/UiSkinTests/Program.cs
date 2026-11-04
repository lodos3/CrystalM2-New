using System.Drawing;
using CrystalM2.Rendering.Core;

static void Check(bool ok, string message)
{
    if (!ok) throw new InvalidOperationException(message);
}

var source = new Rectangle(16, 24, 48, 40);
var border = new SliceInsets(12, 8, 12, 8);
Span<NineSlicePatch> patches = stackalloc NineSlicePatch[9];
int count = NineSliceLayout.Write(patches, source, new Rectangle(10, 20, 300, 180), border);
Check(count == 9, "Normal windows need nine nonoverlapping patches.");
Check(patches[0].Destination == new Rectangle(10, 20, 12, 8), "Top-left ornament must keep its pixel size.");
Check(patches[4].Destination == new Rectangle(22, 28, 276, 164), "Only the container centre should expand.");
Check(patches[8].Source == new Rectangle(52, 56, 12, 8), "Atlas offsets must survive slicing.");

foreach (Size size in new[] { new Size(1, 1), new Size(7, 11), new Size(24, 16), new Size(25, 17), new Size(311, 219) })
{
    var target = new Rectangle(-5, 7, size.Width, size.Height);
    count = NineSliceLayout.Write(patches, source, target, border);
    int area = 0;
    for (int i = 0; i < count; i++)
    {
        Check(target.Contains(patches[i].Destination), "Small controls must not draw overlapping or protruding corners.");
        area += patches[i].Destination.Width * patches[i].Destination.Height;
        for (int j = 0; j < i; j++)
            Check(!patches[i].Destination.IntersectsWith(patches[j].Destination), "Nine-slice patches must not overlap.");
    }
    Check(area == size.Width * size.Height, "Every destination pixel must be covered exactly once.");
}
Check(NineSliceLayout.Write(patches, source, Rectangle.Empty, border) == 0, "Zero-sized hidden controls must submit nothing.");
bool rejected = false;
try { NineSliceLayout.Write(patches, source, new Rectangle(0, 0, 10, 10), new SliceInsets(40, 0, 40, 0)); }
catch (ArgumentException) { rejected = true; }
Check(rejected, "Invalid source borders must fail before uploading vertices.");

byte[] pixels = DarkFantasyAtlas.Generate();
Check(pixels.Length == DarkFantasyAtlas.Width * DarkFantasyAtlas.Height * 4, "The whole UI must share one bounded BGRA atlas.");
Check(pixels.SequenceEqual(DarkFantasyAtlas.Generate()), "Runtime generation must be deterministic.");
foreach (UiSurface surface in Enum.GetValues<UiSurface>().Where(x => x != UiSurface.None))
    foreach (UiVisualState state in Enum.GetValues<UiVisualState>())
    {
        UiAtlasPart part = DarkFantasyAtlas.Part(surface, state);
        Check(new Rectangle(0, 0, DarkFantasyAtlas.Width, DarkFantasyAtlas.Height).Contains(part.Source), "Every state must refer to real atlas pixels.");
    }
Check(DarkFantasyAtlas.Part(UiSurface.Button, UiVisualState.Hover).Source !=
      DarkFantasyAtlas.Part(UiSurface.Button, UiVisualState.Pressed).Source, "Hover and press must be visibly distinct states.");
foreach (UiGlyph glyph in Enum.GetValues<UiGlyph>().Where(x => x != UiGlyph.None))
{
    Rectangle ink = DarkFantasyAtlas.Glyph(glyph);
    bool visible = false;
    for (int y = ink.Top; y < ink.Bottom; y++)
        for (int x = ink.Left; x < ink.Right; x++)
            visible |= pixels[(y * DarkFantasyAtlas.Width + x) * 4 + 3] != 0;
    Check(visible, $"{glyph} must render a visible generated icon.");
}
if (args.Length == 2 && args[0] == "--export-atlas") File.WriteAllBytes(args[1], pixels);
Console.WriteLine("Nine-slice coverage, fixed corners, tiny controls, atlas bounds and state checks passed.");
