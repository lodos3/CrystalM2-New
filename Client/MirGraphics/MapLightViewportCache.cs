using System;
using System.Collections.Generic;
using System.Drawing;

namespace Client.MirGraphics
{
    // Cached map coordinates and decoded light data. No per-frame texture allocation.
    internal readonly struct CachedMapLight
    {
        internal int X { get; }
        internal int Y { get; }
        internal int Index { get; }
        internal Color Colour { get; }

        internal CachedMapLight(int x, int y, int index, Color colour)
        {
            X = x;
            Y = y;
            Index = index;
            Colour = colour;
        }
    }

    // Rebuild only after crossing an eight-tile guard band, changing map,
    // changing viewport dimensions or recreating the light-texture table.
    // The cache retains only nearby emitters, not a second full map array.
    internal sealed class MapLightViewportCache
    {
        private const int Padding = 24;  // Original map-light drawing reach.
        private const int GuardBand = 8;
        private readonly List<CachedMapLight> _items = new List<CachedMapLight>();
        private object _mapIdentity;
        private Rectangle _coverage;
        private int _mapWidth = -1, _mapHeight = -1, _maxIndex = -1;

        internal IReadOnlyList<CachedMapLight> Items => _items;

        internal void Reset()
        {
            _mapIdentity = null;
            _mapWidth = _mapHeight = _maxIndex = -1;
            _coverage = Rectangle.Empty;
            _items.Clear();
        }

        internal void Update(object mapIdentity, int width, int height, int centerX, int centerY,
                             int viewRangeX, int viewRangeY, int maximumLightIndex,
                             Func<int, int, byte> readEncodedLight)
        {
            if (mapIdentity == null) throw new ArgumentNullException(nameof(mapIdentity));
            if (readEncodedLight == null) throw new ArgumentNullException(nameof(readEncodedLight));
            if (width < 0 || height < 0 || viewRangeX < 0 || viewRangeY < 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            int left = Math.Max(0, centerX - viewRangeX - Padding);
            int top = Math.Max(0, centerY - viewRangeY - Padding);
            // The legacy x loop was exclusive on the far edge, and y inclusive.
            int right = Math.Min(width, centerX + viewRangeX + Padding);
            int bottom = Math.Min(height, centerY + viewRangeY + Padding + 1);
            bool withinCoverage =
                left >= _coverage.Left && top >= _coverage.Top &&
                right <= _coverage.Right && bottom <= _coverage.Bottom;

            if (ReferenceEquals(mapIdentity, _mapIdentity) && _mapWidth == width &&
                _mapHeight == height && _maxIndex == maximumLightIndex && withinCoverage)
                return;

            _items.Clear();
            _mapIdentity = mapIdentity;
            _mapWidth = width;
            _mapHeight = height;
            _maxIndex = maximumLightIndex;

            left = Math.Max(0, left - GuardBand);
            top = Math.Max(0, top - GuardBand);
            right = Math.Min(width, right + GuardBand);
            bottom = Math.Min(height, bottom + GuardBand);
            _coverage = Rectangle.FromLTRB(left, top, right, bottom);

            if (maximumLightIndex <= 0 || right <= left || bottom <= top) return;

            for (int y = top; y < bottom; y++)
                for (int x = left; x < right; x++)
                {
                    if (MapLightEncoding.TryDecode(readEncodedLight(x, y), maximumLightIndex,
                                                   out int index, out Color colour))
                        _items.Add(new CachedMapLight(x, y, index, colour));
                }
        }

        internal bool InViewport(CachedMapLight light, int centerX, int centerY,
                                 int viewRangeX, int viewRangeY)
        {
            return light.X >= centerX - viewRangeX - Padding &&
                   light.X < centerX + viewRangeX + Padding &&
                   light.Y >= centerY - viewRangeY - Padding &&
                   light.Y <= centerY + viewRangeY + Padding;
        }

        // Only the nearest static map light within a short tile radius influences
        // each shadow; never scan the full map or allocate during gameplay.
        internal bool TryNearest(int x, int y, int radius, out CachedMapLight nearest)
        {
            nearest = default;
            if (radius < 0) return false;

            long best = (long)radius * radius;
            bool found = false;
            for (int i = 0; i < _items.Count; i++)
            {
                CachedMapLight light = _items[i];
                long dx = x - (long)light.X, dy = y - (long)light.Y;
                long distance = dx * dx + dy * dy;
                if (distance > best) continue;
                best = distance;
                nearest = light;
                found = true;
            }
            return found;
        }
    }

    // Direction from the light towards the sprite (screen-space approximation).
    // A map without a close light retains its stable daylight shadow direction.
    internal readonly struct ShadowProjection
    {
        internal readonly float Horizontal;
        internal readonly float Vertical;

        internal ShadowProjection(float horizontal, float vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }

        internal static ShadowProjection Sunlight => new ShadowProjection(0.34f, -0.27f);

        internal static ShadowProjection FromLight(Point actorCell, Point lightCell)
        {
            double dx = actorCell.X - (double)lightCell.X;
            double dy = actorCell.Y - (double)lightCell.Y;
            double length = Math.Sqrt(dx * dx * 4.0 + dy * dy);
            if (length < 0.001) return Sunlight;

            return new ShadowProjection((float)(dx * 2.0 / length * 0.48),
                                        (float)(dy / length * 0.35));
        }
    }
}
