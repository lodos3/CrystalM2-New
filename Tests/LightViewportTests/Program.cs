using System;
using System.Drawing;
using Client.MirGraphics;

static void Verify(bool ok, string why)
{
    if (!ok) throw new Exception(why);
}

var cells = new byte[150, 150];
cells[40, 40] = 21;
cells[47, 41] = 49;
cells[100, 100] = 30;  // Invalid zero-radius colour; never draw it.
int reads = 0;
byte ReadCell(int x, int y) { reads++; return cells[x, y]; }

var cache = new MapLightViewportCache();
var mapA = new object();
cache.Update(mapA, 150, 150, 40, 40, 5, 5, 9, ReadCell);
Verify(cache.Items.Count == 2, "Must decode exactly the two valid lights in the window.");
Verify(cache.Items[0].Index == 3 && cache.Items[0].Colour == Color.FromArgb(255, 120, 180, 255),
       "Cached light tint and index must preserve original map decoding.");
int firstReads = reads;
Verify(firstReads > 0, "Initial viewport must be indexed.");
cache.Update(mapA, 150, 150, 42, 41, 5, 5, 9, ReadCell);
Verify(reads == firstReads, "Camera movement within the guard band must not scan map cells.");
Verify(cache.InViewport(cache.Items[0], 42, 41, 5, 5), "Near light should be visible.");
Verify(!cache.InViewport(new CachedMapLight(100, 100, 9, Color.White), 42, 41, 5, 5),
       "Out-of-view static lights must be culled.");
cache.Update(mapA, 150, 150, 70, 70, 5, 5, 9, ReadCell);
Verify(reads > firstReads, "Leaving guard band must rebuild cache.");
int secondReads = reads;
cache.Update(new object(), 150, 150, 70, 70, 5, 5, 9, ReadCell);
Verify(reads > secondReads, "Map identity changes must invalidate cached cells.");
int thirdReads = reads;
cache.Update(mapA, 150, 150, 40, 40, 5, 5, 4, ReadCell);
Verify(reads > thirdReads, "Light texture count changes must invalidate cached indices.");
Verify(cache.Items[0].Index == 3, "Clamp should preserve the small valid index.");
Verify(cache.Items[1].Index == 4, "Large radii must clamp to texture count.");

Verify(cache.TryNearest(41, 40, 12, out CachedMapLight nearest) &&
       nearest.X == 40 && nearest.Y == 40, "Nearest static light should be selected.");
Verify(!cache.TryNearest(10, 10, 12, out _), "Distant light must not influence shadows.");

var right = ShadowProjection.FromLight(new Point(45, 40), new Point(40, 40));
var left = ShadowProjection.FromLight(new Point(35, 40), new Point(40, 40));
Verify(right.Horizontal > 0 && left.Horizontal < 0, "Shadow must project away from light.");
var down = ShadowProjection.FromLight(new Point(40, 45), new Point(40, 40));
var up = ShadowProjection.FromLight(new Point(40, 35), new Point(40, 40));
Verify(down.Vertical > 0 && up.Vertical < 0, "Shadow vertical projection must follow light direction.");
Verify(ShadowProjection.FromLight(new Point(40, 40), new Point(40, 40)).Horizontal ==
       ShadowProjection.Sunlight.Horizontal, "Co-located source must use a stable fallback.");
var sunlight = ShadowProjection.Sunlight;
Verify(sunlight.IsProjectedVisible(new Point(-80, 0), 60, 100, 100, 100),
       "Projected shadow entering the viewport must not be culled by sprite origin.");
Verify(!sunlight.IsProjectedVisible(new Point(-150, 0), 60, 100, 100, 100),
       "Completely offscreen projected shadows must be culled.");
Verify(!sunlight.IsProjectedVisible(new Point(130, 0), 60, 100, 100, 100),
       "Right-side offscreen projected shadows must be culled.");
Verify(!sunlight.IsProjectedVisible(new Point(0, 150), 60, 100, 100, 100),
       "Below-screen projected shadows must be culled.");
// Pixel-accurate viewport checks must happen before sprite GPU texture upload.
// A sprite whose edge is exactly on the boundary has no visible pixels.
Verify(SpriteVisibility.IsVisible(-9, 10, 10, 10, 100, 100),
       "One pixel entering from the left must render.");
Verify(!SpriteVisibility.IsVisible(-10, 10, 10, 10, 100, 100),
       "A sprite entirely left of the viewport must not render.");
Verify(SpriteVisibility.IsVisible(99, 20, 2, 2, 100, 100),
       "One pixel entering from the right must render.");
Verify(!SpriteVisibility.IsVisible(100, 20, 2, 2, 100, 100),
       "Sprites at the right boundary must not upload.");
Verify(!SpriteVisibility.IsVisible(10, 100, 5, 5, 100, 100),
       "Sprites below the viewport must not upload.");
Verify(SpriteVisibility.IsVisible(10, 99, 5, 5, 100, 100),
       "Sprites straddling the bottom border must render.");
Verify(!SpriteVisibility.IsVisible(0, 0, 0, 5, 100, 100),
       "Empty frames are never visible.");
Verify(!SpriteVisibility.IsVisible(int.MaxValue - 1, 0, 100, 5, 100, 100),
       "Position arithmetic must not overflow into a visible area.");
Verify(!SpriteVisibility.IsVisible(int.MinValue, 0, 100, 5, 100, 100),
       "Negative extreme coordinates must be rejected.");
Console.WriteLine("Viewport cache, culling, source changes and directional shadow regression checks passed.");
