using System;
using System.Numerics;

namespace CrystalM2.Rendering.Core;

public readonly record struct WeaponMaskTrace(Vector2 Hilt, Vector2 Tip)
{
    public WeaponMaskTrace Orient(Vector2 hand, WeaponMaskTrace? previous)
    {
        float forward = Vector2.DistanceSquared(Hilt, hand) * 2;
        float reverse = Vector2.DistanceSquared(Tip, hand) * 2;
        if (previous is { } blade)
        {
            forward += Vector2.DistanceSquared(Hilt, blade.Hilt) + Vector2.DistanceSquared(Tip, blade.Tip);
            reverse += Vector2.DistanceSquared(Tip, blade.Hilt) + Vector2.DistanceSquared(Hilt, blade.Tip);
        }
        return reverse < forward ? new WeaponMaskTrace(Tip, Hilt) : this;
    }

    public static bool TryMeasure(ReadOnlySpan<byte> bits, int width, int height, out WeaponMaskTrace trace)
    {
        trace = default;
        if (width <= 0 || height <= 0 || (long)width * height > (long)bits.Length * 8) return false;
        long xSum = 0, ySum = 0, xxSum = 0, yySum = 0, xySum = 0;
        int count = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            if ((bits[i >> 3] & (1 << (i & 7))) == 0) continue;
            count++;
            xSum += x;
            ySum += y;
            xxSum += (long)x * x;
            yySum += (long)y * y;
            xySum += (long)x * y;
        }
        if (count < 3) return false;

        Vector2 centre = new(xSum / (float)count, ySum / (float)count);
        double covarianceX = xxSum / (double)count - centre.X * centre.X;
        double covarianceY = yySum / (double)count - centre.Y * centre.Y;
        double covarianceXY = xySum / (double)count - centre.X * centre.Y;
        float angle = .5f * (float)Math.Atan2(2 * covarianceXY, covarianceX - covarianceY);
        Vector2 axis = new(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 first = centre, last = centre;
        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            if ((bits[i >> 3] & (1 << (i & 7))) == 0) continue;
            Vector2 point = new(x + .5f, y + .5f);
            float along = Vector2.Dot(point, axis);
            if (along < min) { min = along; first = point; }
            if (along > max) { max = along; last = point; }
        }
        trace = new WeaponMaskTrace(first, last);
        return max - min >= 3;
    }
}
