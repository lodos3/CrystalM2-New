using System;

namespace Client.MirGraphics
{
    // Bounds CPU work to 24 tiles per ray. Only static map blocking flags may
    // be passed as occluders; dynamic players/items are not stable scenery.
    internal static class LightVisibility
    {
        internal const int MaxRayCells = 24;

        internal static bool Clear(int sx, int sy, int tx, int ty,
                                   Func<int, int, bool> blocksLight)
        {
            if (blocksLight == null) throw new ArgumentNullException(nameof(blocksLight));

            long distX = (long)tx - sx, distY = (long)ty - sy;
            if (Math.Max(Math.Abs(distX), Math.Abs(distY)) > MaxRayCells) return false;

            int dx = (int)Math.Abs(distX);
            int dy = (int)Math.Abs(distY);
            if (dx == 0 && dy == 0) return true;

            int stepX = sx < tx ? 1 : -1;
            int stepY = sy < ty ? 1 : -1;
            int error = dx - dy;
            int x = sx, y = sy;
            while (x != tx || y != ty)
            {
                int twiceError = 2 * error;
                if (twiceError > -dy) { error -= dy; x += stepX; }
                if (twiceError < dx) { error += dx; y += stepY; }
                if (x == tx && y == ty) break;
                if (blocksLight(x, y)) return false;
            }
            return true;
        }
    }
}
