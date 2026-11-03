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
        private bool? _checkerboard;

        // Run checker detection only for sprite frames that actually cast a shadow.
        internal bool HasCheckerboard
        {
            get
            {
                if (!_checkerboard.HasValue) _checkerboard = DetectCheckerboard();
                return _checkerboard.Value;
            }
        }
        internal int ShadowWidth => (_width + 1) / 2;
        internal int ShadowHeight => (_height + 1) / 2;

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

        // An 8x8 alternating alpha patch indicates binary checkerboard dithering.
        // Do not change the original alpha coverage or visible sprite art.
        private bool DetectCheckerboard()
        {
            if (_width < 8 || _height < 8) return false;
            // Bound initial detection work for large frames: at most ~13x13
            // sampled patches rather than a full sliding-window scan.
            int stepY = Math.Max(4, (_height - 8) / 12);
            int stepX = Math.Max(4, (_width - 8) / 12);
            for (int y = 0; y <= _height - 8; y += stepY)
            {
                for (int x = 0; x <= _width - 8; x += stepX)
                {
                    int phaseA = 0;
                    for (int dy = 0; dy < 8; dy++)
                        for (int dx = 0; dx < 8; dx++)
                            if (VisiblePixel(x + dx, y + dy) == (((dx + dy) & 1) == 0))
                                phaseA++;
                    if (phaseA >= 62 || phaseA <= 2)
                        return true;
                }
            }
            return false;
        }

        // A half-resolution coverage mask antialiases checkerboard holes on the
        // projected ground shadow. Zero RGB avoids bleeding coloured sprite edges.
        internal void WriteShadowMask(Span<byte> bgra, int pitch)
        {
            int width = ShadowWidth, height = ShadowHeight;
            int rowBytes = checked(width * 4);
            if (pitch < rowBytes || ((long)height - 1) * pitch + rowBytes > bgra.Length)
                throw new ArgumentException("Shadow mask destination is too small.");

            for (int y = 0; y < height; y++)
            {
                int row = y * pitch;
                for (int x = 0; x < width; x++)
                {
                    int visible = 0, count = 0;
                    for (int dy = 0; dy < 2 && y * 2 + dy < _height; dy++)
                        for (int dx = 0; dx < 2 && x * 2 + dx < _width; dx++)
                        {
                            count++;
                            if (VisiblePixel(x * 2 + dx, y * 2 + dy)) visible++;
                        }
                    int pixel = row + x * 4;
                    bgra[pixel] = bgra[pixel + 1] = bgra[pixel + 2] = 0;
                    bgra[pixel + 3] = (byte)((visible * 255 + count / 2) / count);
                }
            }
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
