using System;
using System.Drawing;
using Client.MirGraphics;

static void Verify(bool condition, string description)
{
    if (!condition) throw new Exception(description);
}

static void Reject(Action action, string description)
{
    try { action(); }
    catch (ArgumentException) { return; }
    throw new Exception(description);
}

// Two 3-pixel BGRA rows with a 16-byte hardware pitch. Alpha in row padding
// must not be considered an image pixel.
byte[] padded = new byte[32];
padded[3] = 255;            // (0,0)
padded[16 + 2 * 4 + 3] = 1; // (2,1), any nonzero alpha is visible
padded[15] = 255;           // padding; must be ignored
var coverage = AlphaCoverage.Capture(padded, 3, 2, 16);
Verify(coverage.StorageBytes == 1, "Six pixels should require exactly one packed byte.");
Verify(coverage.VisiblePixel(0, 0), "First visible pixel missing.");
Verify(coverage.VisiblePixel(2, 1), "Last visible pixel missing.");
Verify(!coverage.VisiblePixel(1, 0), "Transparent pixel detected as visible.");
Verify(!coverage.VisiblePixel(0, 1), "Row pitch incorrectly interpreted.");
Verify(!coverage.VisiblePixel(-1, 0) && !coverage.VisiblePixel(3, 0) &&
       !coverage.VisiblePixel(0, -1) && !coverage.VisiblePixel(0, 2), "Out-of-range hit-test must be false.");
Verify(coverage.VisibleSize == new Size(3, 2), "Visible bounding dimensions incorrect.");

var single = new byte[32];
single[16 + 2 * 4 + 3] = 255;
Verify(AlphaCoverage.Capture(single, 3, 2, 16).VisibleSize == new Size(1, 1),
       "Single visible pixel should occupy 1x1.");
var empty = AlphaCoverage.Capture(new byte[32], 3, 2, 16);
Verify(empty.VisibleSize == new Size(3, 2), "Empty sprites preserve legacy size fallback.");
Verify(!empty.VisiblePixel(0, 0), "Empty sprites must be transparent.");

byte[] wide = new byte[17 * 4];
wide[16 * 4 + 3] = 255;
var packed = AlphaCoverage.Capture(wide, 17, 1, 68);
Verify(packed.StorageBytes == 3, "Seventeen pixels should pack into three bytes.");
Verify(packed.VisiblePixel(16, 0) && !packed.VisiblePixel(15, 0), "Bit boundary 15/16 is incorrect.");

Reject(() => AlphaCoverage.Capture(new byte[4], 3, 2, 16), "Truncated source must throw.");
Reject(() => AlphaCoverage.Capture(new byte[32], 3, 2, 8), "Pitch less than pixel width must throw.");
Reject(() => AlphaCoverage.Capture(new byte[32], 0, 2, 16), "Zero width must throw.");
Console.WriteLine("Packed BGRA alpha coverage regression checks passed.");
