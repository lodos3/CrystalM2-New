using System;

namespace CrystalM2.Rendering.Core;

public readonly record struct LegacyProjectileTimeline(long Launch, int CellDistance, int FrameInterval)
{
    public const int MillisecondsPerCell = 50;
    public int Duration => Math.Max(1, CellDistance * MillisecondsPerCell);
    public long Impact => Launch + Duration;
    public int Frames => Math.Max(1, Duration / Math.Max(1, FrameInterval));

    public bool InFlight(long now) => now >= Launch && now < Impact;
    public float Progress(long now) => Math.Clamp((now - Launch) / (float)Duration, 0, 1);
    public int Frame(long now) => Math.Min(Frames - 1, (int)(Progress(now) * Frames));
}
