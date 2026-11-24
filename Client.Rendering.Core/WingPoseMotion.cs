using System.Numerics;

namespace CrystalM2.Rendering.Core;

public readonly record struct WingPoseMotion(Vector2 Offset, float Roll, float Yaw)
{
    public static WingPoseMotion Zero => new(Vector2.Zero, 0, 0);
}

// Frame-rate-independent 30 ms exponential interpolation. Both draw passes
// and the light use the same sample, advanced by the player's Process loop.
public struct WingPoseSmoother
{
    private bool _initialized;
    private long _lastTime;
    public WingPoseMotion Current { get; private set; }

    public WingPoseMotion Advance(WingPoseMotion target, long now, bool reset)
    {
        if (!_initialized || reset || now < _lastTime || now - _lastTime > 250)
        {
            _initialized = true;
            _lastTime = now;
            return Current = target;
        }
        if (now == _lastTime) return Current;
        float dt = (now - _lastTime) * .001f;
        _lastTime = now;
        float weight = 1 - MathF.Exp(-dt / .030f);
        return Current = new WingPoseMotion(Vector2.Lerp(Current.Offset, target.Offset, weight),
            Current.Roll + (target.Roll - Current.Roll) * weight,
            Current.Yaw + (target.Yaw - Current.Yaw) * weight);
    }
}
