using System.Numerics;

namespace CrystalM2.Rendering.Core;

public readonly record struct WingPoseMotion(Vector2 Offset, float Roll, float Yaw)
{
    public static WingPoseMotion Zero => new(Vector2.Zero, 0, 0);

    // The sprite changes discretely. Never allow the interpolated root to
    // trail the current shoulder by more than a small screen-space distance.
    public static Vector2 LimitOffset(Vector2 value, Vector2 target, float maxLag)
    {
        Vector2 error = value - target;
        float distance = error.Length();
        return distance > maxLag && distance > 0 ? target + error * (maxLag / distance) : value;
    }

    public static WingPoseMotion Stabilize(WingPoseMotion motion, bool idle, bool locomotion)
    {
        if (idle) return Zero;
        Vector2 offset = locomotion && motion.Offset.LengthSquared() < .64f ? Vector2.Zero : motion.Offset;
        return new(offset, Math.Clamp(motion.Roll, locomotion ? -.055f : -.18f, locomotion ? .055f : .18f),
            Math.Clamp(motion.Yaw, locomotion ? -.04f : -.14f, locomotion ? .04f : .14f));
    }
}

// Frame-rate-independent 30 ms exponential interpolation. Both draw passes
// and the light use the same sample, advanced by the player's Process loop.
public struct WingPoseSmoother
{
    private bool _initialized;
    private long _lastTime;
    public WingPoseMotion Current { get; private set; }

    public WingPoseMotion Advance(WingPoseMotion target, long now, bool reset, float tau = .035f, float maxLag = 1.5f)
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
        float weight = 1 - MathF.Exp(-dt / Math.Clamp(tau, .015f, .080f));
        return Current = new WingPoseMotion(WingPoseMotion.LimitOffset(Vector2.Lerp(Current.Offset, target.Offset, weight), target.Offset, maxLag),
            Current.Roll + (target.Roll - Current.Roll) * weight,
            Current.Yaw + (target.Yaw - Current.Yaw) * weight);
    }
}
