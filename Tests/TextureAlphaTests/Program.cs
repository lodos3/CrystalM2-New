using System;
using System.Drawing;
using System.Numerics;
using Client.MirGraphics;
using CrystalM2.Rendering.Core;

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

byte[] blade = new byte[24 * 24 * 4];
for (int i = 4; i < 20; i++)
    blade[((23 - i) * 24 + i) * 4 + 3] = 255;
var weapon = AlphaCoverage.Capture(blade, 24, 24, 24 * 4).GetWeaponTrace();
Verify(weapon is { } && Vector2.DistanceSquared(weapon.Value.Hilt, weapon.Value.Tip) > 350,
    "A weapon trace must span the actual alpha, including diagonal blade tips.");
var oriented = weapon!.Value.Orient(new Vector2(4, 20), null);
Verify(oriented.Hilt.X < oriented.Tip.X && oriented.Hilt.Y > oriented.Tip.Y,
    "The end nearest the actor's hand must be the ribbon base.");
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

byte[] emitter = new byte[12 * 8 * 4];
for (int i = 3; i < emitter.Length; i += 4) emitter[i] = 255; // Opaque black animation backing.
int flame = (2 * 12 + 8) * 4;
emitter[flame] = 16;
emitter[flame + 1] = 112;
emitter[flame + 2] = 240;
var light = AlphaCoverage.Capture(emitter, 12, 8, 48);
Verify(light.EmissionPivot == new PointF(8.5f, 2.5f), "Scaling must anchor the bright pixel through black padding.");
Verify(light.EmissionColour.R == 255 && light.EmissionColour.G > 100 && light.EmissionColour.B < 25,
       "The halo must retain the frame's warm emission colour.");

byte[] wide = new byte[17 * 4];
wide[16 * 4 + 3] = 255;
var packed = AlphaCoverage.Capture(wide, 17, 1, 68);
Verify(packed.StorageBytes == 3, "Seventeen pixels should pack into three bytes.");
Verify(packed.VisiblePixel(16, 0) && !packed.VisiblePixel(15, 0), "Bit boundary 15/16 is incorrect.");

Reject(() => AlphaCoverage.Capture(new byte[4], 3, 2, 16), "Truncated source must throw.");
Reject(() => AlphaCoverage.Capture(new byte[32], 3, 2, 8), "Pitch less than pixel width must throw.");
Reject(() => AlphaCoverage.Capture(new byte[32], 0, 2, 16), "Zero width must throw.");


// Checkerboard detection is deliberately limited to alpha coverage; source artwork
// must not be recoloured or overwritten by the shadow repair.
byte[] checker = new byte[8 * 8 * 4];
for (int y = 0; y < 8; y++)
    for (int x = 0; x < 8; x++)
        checker[(y * 8 + x) * 4 + 3] = (byte)(((x + y) & 1) == 0 ? 255 : 0);
var checkerCoverage = AlphaCoverage.Capture(checker, 8, 8, 32);
Verify(checkerCoverage.HasCheckerboard, "Alternating 8x8 alpha must be detected.");
Verify(checkerCoverage.VisiblePixel(0, 0) && !checkerCoverage.VisiblePixel(1, 0),
       "Checker detection must never modify original hit-testing.");
byte[] repaired = new byte[checkerCoverage.ShadowWidth * checkerCoverage.ShadowHeight * 4];
checkerCoverage.WriteShadowMask(repaired, checkerCoverage.ShadowWidth * 4);
Verify(repaired[3] == 128 && repaired[7] == 128,
       "Downsampled checker shadow should retain half coverage, not sharp holes.");
var solid = new byte[8 * 8 * 4];
for (int i = 3; i < solid.Length; i += 4) solid[i] = 255;
Verify(!AlphaCoverage.Capture(solid, 8, 8, 32).HasCheckerboard,
       "Opaque sprites should not allocate a repaired shadow mask.");
Reject(() => checkerCoverage.WriteShadowMask(new byte[3], 2),
       "Truncated shadow destination must be rejected.");

const int maskWidth = 32, maskHeight = 16;
byte[] actor = new byte[maskWidth * maskHeight * 4];
for (int y = 2; y < 12; y++)
for (int x = 2; x < 9; x++)
    actor[(y * maskWidth + x) * 4 + 3] = 255;
for (int y = 6; y < 14; y++)
for (int x = 16; x < 27; x++)
    actor[(y * maskWidth + x) * 4 + 3] = (byte)((x + y) % 2 == 0 ? 255 : 0);
for (int x = 12; x < 16; x++)
    actor[(13 * maskWidth + x) * 4 + 3] = 74;
int translucentCloth = (2 * maskWidth + 1) * 4;
actor[translucentCloth + 1] = 39;
actor[translucentCloth + 3] = 95;
Verify(BakedShadowRepair.StripFromMask(actor, maskWidth, maskHeight, maskWidth * 4) &&
       actor[(8 * maskWidth + 20) * 4 + 3] == 0 && actor[(13 * maskWidth + 13) * 4 + 3] == 0 &&
       actor[(5 * maskWidth + 5) * 4 + 3] == 255 && actor[translucentCloth + 3] == 95,
       "Spell coverage must exclude dithered and repaired shadows and retain opaque black and translucent body art.");

Console.WriteLine("Packed BGRA alpha coverage and checkerboard shadow regression checks passed.");
