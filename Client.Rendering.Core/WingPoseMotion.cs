using System.Numerics;

namespace CrystalM2.Rendering.Core;

// Screen-projected anatomical shoulder basis from the actual body frame.
// A proper rotation maps the reference pose to the current pose, preserving
// the length and shape of the wing. No arbitrary per-direction screen cant.
public readonly record struct WingTorsoFrame(Vector2 Across, Vector2 ReferenceAcross)
{
    public Vector2 Transform(Vector2 point)
    {
        float c = Vector2.Dot(ReferenceAcross, Across);
        float s = ReferenceAcross.X * Across.Y - ReferenceAcross.Y * Across.X;
        return new Vector2(point.X * c - point.Y * s, point.X * s + point.Y * c);
    }

    public Vector2 SocketOffset(Vector2 nominalAcross, int side, float halfWidth)
    {
        // Stable side correspondence is chosen from the reference, not the
        // current shoulder slope, so a swing cannot exchange the two roots.
        float handedness = Vector2.Dot(nominalAcross, ReferenceAcross) < 0 ? -1 : 1;
        return Across * (side * halfWidth * nominalAcross.Length() * handedness);
    }
}

public readonly record struct WingPoseMotion(Vector2 Offset, float Roll, float Yaw, float Pitch = 0)
{
    public WingTorsoFrame? TorsoFrame { get; init; }
    public static WingPoseMotion Zero => new(Vector2.Zero, 0, 0);

    // Recover forward travel from its projection onto the isometric ground.
    // Positive pitch carries a point above the shoulder towards the strike.
    public static float AttackPitch(Vector2 travel, int direction)
    {
        float facing = direction * (MathF.PI / 4);
        Vector2 forward = new(MathF.Sin(facing), -.49f * MathF.Cos(facing));
        float distance = Vector2.Dot(travel, forward) / forward.LengthSquared();
        return Math.Clamp(MathF.Atan(distance / 43f), -.16f, .33f);
    }

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
        if (idle) return motion with { Roll = 0, Yaw = 0, Pitch = 0 };
        return motion with
        {
            Roll = Math.Clamp(motion.Roll, locomotion ? -.055f : -.18f, locomotion ? .055f : .18f),
            Yaw = Math.Clamp(motion.Yaw, locomotion ? -.04f : -.14f, locomotion ? .04f : .14f),
            Pitch = locomotion ? 0 : motion.Pitch
        };
    }
}

// Hard positional constraint plus a critically damped rotational spring.
// One update per actor, shared by both draw passes and the light source.
public struct WingPoseSmoother
{
    private bool _initialized;
    private long _lastTime;
    private Vector3 _velocity;
    public WingPoseMotion Current { get; private set; }

    public WingPoseMotion Advance(WingPoseMotion target, long now, bool reset,
        float tau = .045f, float maxAngularSpeed = .9f)
    {
        if (!_initialized || reset || now < _lastTime || now - _lastTime > 250)
        {
            _initialized = true;
            _lastTime = now;
            _velocity = Vector3.Zero;
            return Current = target with { Roll = 0, Yaw = 0, Pitch = 0 };
        }

        float dt = (now - _lastTime) * .001f;
        _lastTime = now;
        // A sprite can change even at the same timer tick. No positional
        // spring, velocity or dead zone is allowed on the physical socket.
        if (dt == 0) return Current = Current with { Offset = target.Offset, TorsoFrame = target.TorsoFrame };

        float omega = 2f / Math.Clamp(tau, .025f, .100f);
        float decay = MathF.Exp(-omega * dt);
        float speed = Math.Clamp(maxAngularSpeed, .10f, 1.5f);
        float roll = Step(Current.Roll, target.Roll, ref _velocity.X, .18f);
        float yaw = Step(Current.Yaw, target.Yaw, ref _velocity.Y, .14f);
        float pitch = Step(Current.Pitch, target.Pitch, ref _velocity.Z, .33f);
        return Current = target with { Roll = roll, Yaw = yaw, Pitch = pitch };

        float Step(float value, float goal, ref float velocity, float bound)
        {
            goal = Math.Clamp(goal, -bound, bound);
            float error = value - goal;
            float temp = (velocity + omega * error) * dt;
            float unconstrained = goal + (error + temp) * decay;
            float nextVelocity = (velocity - omega * temp) * decay;
            float next = Math.Clamp(unconstrained, value - speed * dt, value + speed * dt);
            next = Math.Clamp(next, -bound, bound);
            // A reversing target may initially move away due to inertia, but
            // the spring cannot overshoot through the target or its limits.
            if ((goal - value) * (next - goal) > 0)
            {
                velocity = 0;
                return goal;
            }
            velocity = MathF.Abs(next - unconstrained) > .00001f
                ? (next - value) / dt : Math.Clamp(nextVelocity, -speed, speed);
            return next;
        }
    }
}
