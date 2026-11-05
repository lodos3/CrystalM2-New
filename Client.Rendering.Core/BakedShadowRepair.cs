using System;
using System.Buffers;

namespace CrystalM2.Rendering.Core;

public static class BakedShadowRepair
{
    private const int Radius = 2;

    // Repair transparent checker shadows at their authored position. Dense
    // colour-matched seeds distinguish shadows from dark outlines and isolated pixels.
    public static bool Smooth(Span<byte> bgra, int width, int height, int pitch, bool joinTileEdges = false)
    {
        if (width < 2 || height < 2) return false;
        int seedWidth = Math.Min(width, height == 2 ? 4 : 3);
        int seedHeight = Math.Min(height, width == 2 ? 4 : 3);
        if (seedWidth * seedHeight < 8) return false;
        if (pitch < (long)width * 4 || (long)(height - 1) * pitch + width * 4L > bgra.Length)
            throw new ArgumentException("BGRA buffer does not contain every image row.");

        int count = checked(width * height);
        byte[]? selected = null;
        int[]? queue = null;
        int left = width, top = height, right = -1, bottom = -1;
        try
        {
            for (int y = 0; y <= height - seedHeight; y++)
            for (int x = 0; x <= width - seedWidth; x++)
            {
                int phase = bgra[y * pitch + x * 4 + 3] == 0 ? 1 : 0;
                int index = y * width + x + phase;
                if (selected != null && selected[index] != 0) continue;
                int pixel = y * pitch + x * 4;
                int key = pixel + phase * 4;
                if (!IsDark(bgra, key) || !IsSeed(bgra, pixel, key, phase, pitch, seedWidth, seedHeight)) continue;

                if (selected == null)
                {
                    selected = ArrayPool<byte>.Shared.Rent(count);
                    selected.AsSpan(0, count).Clear();
                    queue = ArrayPool<int>.Shared.Rent(count);
                }

                int end = 1;
                queue![0] = index;
                selected[index] = bgra[key + 3];
                for (int start = 0; start < end; start++)
                {
                    int cx = queue[start] % width, cy = queue[start] / width;
                    left = Math.Min(left, cx); right = Math.Max(right, cx);
                    top = Math.Min(top, cy); bottom = Math.Max(bottom, cy);
                    for (int dy = -1; dy <= 1; dy += 2)
                    for (int dx = -1; dx <= 1; dx += 2)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;
                        int neighbour = ny * width + nx;
                        int offset = ny * pitch + nx * 4;
                        if (selected[neighbour] != 0 || !SameColour(bgra, key, offset) ||
                            !HasClearNeighbours(bgra, nx, ny, width, height, pitch)) continue;
                        selected[neighbour] = bgra[offset + 3];
                        queue[end++] = neighbour;
                    }
                }
            }

            if (selected == null) return false;
            PaintShadow(bgra, selected, width, height, pitch, left, top, right, bottom, joinTileEdges);
            return true;
        }
        finally
        {
            if (selected != null) ArrayPool<byte>.Shared.Return(selected);
            if (queue != null) ArrayPool<int>.Shared.Return(queue);
        }
    }

    // The offline editor stored its shadow pixels as translucent black.
    public static bool HasOfflineRepair(ReadOnlySpan<byte> pixels, int width, int height, int pitch)
    {
        int matches = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int p = y * pitch + x * 4;
            if (pixels[p + 3] is >= 6 and <= 112 && (pixels[p] | pixels[p + 1] | pixels[p + 2]) == 0 && ++matches == 4)
                return true;
        }
        return false;
    }

    // The backup is usable only when every changed pixel can be explained by
    // the old shadow conversion. In particular, keep newer edits to the artwork.
    public static bool TryUseOriginal(Span<byte> pixels, int width, int height, int pitch, Span<byte> original)
    {
        int originalPitch = checked(width * 4);
        if (original.Length != (long)originalPitch * height) return false;
        int removedDots = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int p = y * pitch + x * 4, source = y * originalPitch + x * 4;
            if (pixels.Slice(p, 4).SequenceEqual(original.Slice(source, 4))) continue;
            if (pixels[p + 3] > 128 || (pixels[p] | pixels[p + 1] | pixels[p + 2]) != 0) return false;
            if (original[source + 3] == 0) continue;
            if (!IsDark(original, source)) return false;
            removedDots++;
        }

        if (removedDots < 4 || !Smooth(original, width, height, originalPitch, joinTileEdges: true)) return false;
        for (int y = 0; y < height; y++)
            original.Slice(y * originalPitch, originalPitch).CopyTo(pixels.Slice(y * pitch, originalPitch));
        return true;
    }

    private static bool IsDark(ReadOnlySpan<byte> pixels, int p) => pixels[p + 3] >= 96 &&
        pixels[p] <= 24 && pixels[p + 1] <= 24 && pixels[p + 2] <= 24 &&
        pixels[p] + pixels[p + 1] + pixels[p + 2] <= 36;

    private static bool SameColour(ReadOnlySpan<byte> pixels, int a, int b) => IsDark(pixels, b) &&
        Math.Abs(pixels[a] - pixels[b]) <= 4 && Math.Abs(pixels[a + 1] - pixels[b + 1]) <= 4 &&
        Math.Abs(pixels[a + 2] - pixels[b + 2]) <= 4 && Math.Abs(pixels[a + 3] - pixels[b + 3]) <= 32;

    private static bool IsSeed(ReadOnlySpan<byte> pixels, int pixel, int key, int phase, int pitch,
                               int seedWidth, int seedHeight)
    {
        int matches = 0;
        for (int y = 0; y < seedHeight; y++)
        for (int x = 0; x < seedWidth; x++)
        {
            int p = pixel + y * pitch + x * 4;
            if (pixels[p + 3] == 0) continue;
            if (((x + y) & 1) != phase || !SameColour(pixels, key, p)) return false;
            matches++;
        }
        return matches >= 4;
    }

    private static bool HasClearNeighbours(ReadOnlySpan<byte> pixels, int x, int y, int width, int height, int pitch)
    {
        int p = y * pitch + x * 4 + 3;
        int clear = 0;
        if (x == 0 || pixels[p - 4] == 0) clear++;
        if (x == width - 1 || pixels[p + 4] == 0) clear++;
        if (y == 0 || pixels[p - pitch] == 0) clear++;
        if (y == height - 1 || pixels[p + pitch] == 0) clear++;
        return clear >= 2;
    }

    private static void PaintShadow(Span<byte> pixels, ReadOnlySpan<byte> selected, int width, int height, int pitch,
                                    int left, int top, int right, int bottom, bool joinTileEdges)
    {
        for (int y = Math.Max(0, top - Radius); y <= Math.Min(height - 1, bottom + Radius); y++)
        for (int x = Math.Max(0, left - Radius); x <= Math.Min(width - 1, right + Radius); x++)
        {
            int p = y * pitch + x * 4;
            if (pixels[p + 3] != 0 && selected[y * width + x] == 0) continue;

            int coverage = 0;
            for (int dy = -Radius; dy <= Radius; dy++)
            for (int dx = -Radius; dx <= Radius; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (joinTileEdges)
                {
                    // Reflection keeps the alternating phase at the cut edge.
                    nx = Reflect(nx, width);
                    ny = Reflect(ny, height);
                }
                if ((uint)nx < (uint)width && (uint)ny < (uint)height)
                    coverage += (3 - Math.Abs(dx)) * (3 - Math.Abs(dy)) * selected[ny * width + nx];
            }
            if (coverage == 0) continue;
            pixels[p] = pixels[p + 1] = pixels[p + 2] = 0;
            pixels[p + 3] = (byte)Math.Min(128, (coverage + 40) / 81);
        }
    }

    private static int Reflect(int position, int size) => size == 2 ? position & 1 :
        position < 0 ? -position : position >= size ? 2 * (size - 1) - position : position;
}
