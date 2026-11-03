using System;
using System.Drawing;

namespace Client.MirGraphics
{
    // Nominal BGRA target cap is 4 MiB per bloom pass, independent of window size.
    internal static class BloomBufferLayout
    {
        internal const int MaxTargetPixels = 1024 * 1024;

        internal static int ClampDivisor(int requested)
        {
            return Math.Max(2, Math.Min(8, requested));
        }

        internal static Size Resolve(int width, int height, int divisor)
        {
            if (width <= 0 || height <= 0) return Size.Empty;
            divisor = ClampDivisor(divisor);
            int targetWidth = (width - 1) / divisor + 1;
            int targetHeight = (height - 1) / divisor + 1;
            if ((long)targetWidth * targetHeight > MaxTargetPixels)
                return Size.Empty;
            return new Size(targetWidth, targetHeight);
        }
    }
}
