using System;
using System.Drawing;

namespace Client.MirGraphics
{
    // Compact, managed alpha coverage. Native D3D9 lock pointers must never
    // survive UnlockRectangle or texture eviction/reset.
    internal sealed class AlphaCoverage
    {
        private readonly byte[] _visibleBits;
        private readonly int _width;
        private readonly int _height;

        internal Size VisibleSize { get; }
        internal int StorageBytes => _visibleBits.Length;

        private AlphaCoverage(int width, int height, byte[] bits, Size visibleSize)
        {
            _width = width;
            _height = height;
            _visibleBits = bits;
            VisibleSize = visibleSize;
        }

        internal static AlphaCoverage Capture(ReadOnlySpan<byte> bgra, int width, int height, int pitch)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Image dimensions must be positive.");
            if (pitch < (long)width * 4)
                throw new ArgumentException("BGRA row pitch is smaller than the image width.");
            long required = ((long)height - 1) * pitch + (long)width * 4;
            if (required > bgra.Length)
                throw new ArgumentException("BGRA buffer does not contain every image row.");

            int pixels = checked(width * height);
            byte[] bits = new byte[(pixels >> 3) + ((pixels & 7) == 0 ? 0 : 1)];
            int minX = width, minY = height, maxX = -1, maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = checked(y * pitch);
                int pixelOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    // Format.A8R8G8B8 is BGRA in little-endian system memory.
                    if (bgra[rowOffset + x * 4 + 3] == 0)
                        continue;

                    int bitIndex = pixelOffset + x;
                    bits[bitIndex >> 3] |= (byte)(1 << (bitIndex & 7));

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            // Retain the legacy dimensions for fully transparent images.
            Size size = maxX < 0 ? new Size(width, height) :
                new Size(maxX - minX + 1, maxY - minY + 1);
            return new AlphaCoverage(width, height, bits, size);
        }

        internal bool VisiblePixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
                return false;
            int index = y * _width + x;
            return (_visibleBits[index >> 3] & (1 << (index & 7))) != 0;
        }
    }
}
