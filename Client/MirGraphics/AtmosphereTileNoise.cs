using System;
using System.Drawing;

namespace Client.MirGraphics
{
    // Small, repeatable tileable value noise. Generated once per device, never
    // read back from the GPU. The 128x128 BGRA tile uses exactly 64 KiB.
    internal static class AtmosphereTileNoise
    {
        private const int GridCells = 8;

        internal static float Sample(int x, int y, int width, int height, int seed)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            int u = ((x % width) + width) % width;
            int v = ((y % height) + height) % height;

            double fx = (double)u * GridCells / width;
            double fy = (double)v * GridCells / height;
            int ix = (int)fx;
            int iy = (int)fy;
            double dx = fx - ix;
            double dy = fy - iy;
            dx = dx * dx * (3 - 2 * dx);
            dy = dy * dy * (3 - 2 * dy);

            double n00 = Corner(ix, iy, seed);
            double n10 = Corner(ix + 1, iy, seed);
            double n01 = Corner(ix, iy + 1, seed);
            double n11 = Corner(ix + 1, iy + 1, seed);
            double lower = n00 + (n10 - n00) * dx;
            double upper = n01 + (n11 - n01) * dx;
            return (float)(lower + (upper - lower) * dy);
        }

        private static float Corner(int x, int y, int seed)
        {
            unchecked
            {
                uint n = (uint)((x & 7) * 374761393) +
                         (uint)((y & 7) * 668265263) +
                         (uint)seed * 2246822519u;
                n = (n ^ (n >> 13)) * 1274126177u;
                n ^= n >> 16;
                return (n & 65535) / 65535f;
            }
        }

        internal static void WriteBgra(Span<byte> pixels, int width, int height,
                                       int pitch, int seed)
        {
            if (width <= 0 || height <= 0 || pitch < (long)width * 4 ||
                ((long)height - 1) * pitch + (long)width * 4 > pixels.Length)
                throw new ArgumentException("Invalid atmospheric noise buffer dimensions.");

            for (int y = 0; y < height; y++)
            {
                int row = checked(y * pitch);
                for (int x = 0; x < width; x++)
                {
                    float noise = Sample(x, y, width, height, seed);
                    int offset = row + x * 4;
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 255;
                    pixels[offset + 3] = (byte)(32 + (int)(noise * 166f));
                }
            }
        }
    }

    internal static class AtmosphereOverlayLayout
    {
        internal const int MaxTilesPerLayer = 48;

        internal static bool TryCountTiles(int width, int height, int tileSize,
                                           out int count)
        {
            count = 0;
            if (width <= 0 || height <= 0 || tileSize <= 0) return false;
            long columns = ((long)width - 1) / tileSize + 2;
            long rows = ((long)height - 1) / tileSize + 2;
            long total = columns * rows;
            if (total > MaxTilesPerLayer) return false;
            count = (int)total;
            return true;
        }
    }
}