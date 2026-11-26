using System.Numerics;

namespace CrystalM2.Rendering.Core;

// Screen-projected anatomical shoulder basis from the actual body frame.
// A proper rotation maps the reference pose to the current pose, preserving
// the length and shape of the wing. No arbitrary per-direction screen cant.
public readonly record struct WingTorsoFrame(Vector2 Across, Vector2 ReferenceAcross,
    Vector2 Centre, Vector2 ReferenceCentre,
    Vector2 NegativeShoulder, Vector2 PositiveShoulder,
    Vector2 ReferenceNegativeShoulder, Vector2 ReferencePositiveShoulder)
{
    public Vector2 Transform(Vector2 point)
    {
        float c = Vector2.Dot(ReferenceAcross, Across);
        float s = ReferenceAcross.X * Across.Y - ReferenceAcross.Y * Across.X;
        return new Vector2(point.X * c - point.Y * s, point.X * s + point.Y * c);
    }

    public Vector2 SocketOffset(Vector2 nominalAcross, int side, float halfWidth, Vector2 rear)
    {
        // Preserve the established rest offset from each individual shoulder.
        float handedness = Vector2.Dot(nominalAcross, ReferenceAcross) < 0 ? -1 : 1;
        float signedSide = side * handedness;
        bool positive = signedSide > 0;
        Vector2 referenceShoulder = positive ? ReferencePositiveShoulder : ReferenceNegativeShoulder;
        Vector2 currentShoulder = positive ? PositiveShoulder : NegativeShoulder;
        float restHalfWidth = halfWidth * nominalAcross.Length();
        Vector2 restRoot = ReferenceCentre + rear + ReferenceAcross * (signedSide * restHalfWidth);
        Vector2 local = restRoot - referenceShoulder;

        // On extreme compressed projections only, reduce the longitudinal
        // inset to prevent crossing; preserve the perpendicular offset.
        float referenceWidth = Vector2.Distance(ReferenceNegativeShoulder, ReferencePositiveShoulder);
        float currentWidth = Vector2.Distance(NegativeShoulder, PositiveShoulder);
        float combinedInset = referenceWidth - 2 * restHalfWidth;
        if (combinedInset > 0 && currentWidth - combinedInset < 1)
        {
            float scale = Math.Clamp((currentWidth - 1) / combinedInset, 0, 1);
            local += ReferenceAcross * (Vector2.Dot(local, ReferenceAcross) * (scale - 1));
        }
        return currentShoulder - Centre + Transform(local);
    }

    // The pose atlas shoulder line is a 2D estimate made from the sprite
    // silhouette, not a pair of true 3D shoulder joints. In the wing rig
    // its dynamic twist has the opposite sign (confirmed by the marked
    // attack screenshot). Reflect the *change* about the resting axis,
    // never the absolute facing or the entire body position.
    public WingTorsoFrame ForWingTwist()
    {
        Vector2 correctedAcross = Vector2.Normalize(2 * Vector2.Dot(Across, ReferenceAcross) * ReferenceAcross - Across);
        Vector2 midpoint = (NegativeShoulder + PositiveShoulder) * .5f;
        float halfWidth = Vector2.Distance(NegativeShoulder, PositiveShoulder) * .5f;
        return this with
        {
            Across = correctedAcross,
            NegativeShoulder = midpoint - correctedAcross * halfWidth,
            PositiveShoulder = midpoint + correctedAcross * halfWidth
        };
    }

    // Standing may translate with the body; spacing and twist stay neutral.
    public WingTorsoFrame AtRest() => this with
    {
        Across = ReferenceAcross,
        NegativeShoulder = ReferenceNegativeShoulder + Centre - ReferenceCentre,
        PositiveShoulder = ReferencePositiveShoulder + Centre - ReferenceCentre
    };
}

public static class WingProfilePlacement
{
    public static bool IsSide(int direction)=>direction is 2 or 6;
    public static Vector2 Socket(Vector2 relative,int direction)
    {
        if(!IsSide(direction))return relative;
        // Raise the previous profile roots by 1–2 pixels while keeping
        // them below the tracked upper back, including during attacks.
        return new(Math.Clamp(relative.X,-4.5f,4.5f),Math.Clamp(relative.Y,-1,2)+1.5f);
    }
    public static float SpanY(int direction,float projectedAcrossY,float lateral)
        => (IsSide(direction)?Math.Clamp(projectedAcrossY,-.14f,.14f):projectedAcrossY)*lateral;
    // Relative to the artwork attachment, shorten only the lower feathers.
    // Keep the projection continuous at the pinned root.
    public static float FeatherHeight(int direction,float relativeHeight)
        => IsSide(direction)&&relativeHeight>0?relativeHeight*.85f:relativeHeight;
    public static float Rise(int direction,float lift)=>IsSide(direction)?Math.Clamp(lift,0,2):lift;
}

public readonly record struct WingPoseMotion(Vector2 Offset, float Roll, float Yaw, float Pitch = 0)
{
    public WingTorsoFrame? TorsoFrame { get; init; }
    public float LegacyLift { get; init; }
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
        if (idle) return motion with
        {
            Roll = 0, Yaw = 0, Pitch = 0, LegacyLift = 0, TorsoFrame = motion.TorsoFrame?.AtRest()
        };
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
    private float _liftVelocity;
    public WingPoseMotion Current { get; private set; }

    public WingPoseMotion Advance(WingPoseMotion target, long now, bool reset,
        float tau = .045f, float maxAngularSpeed = .9f)
    {
        if (!_initialized || reset || now < _lastTime || now - _lastTime > 250)
        {
            _initialized = true;
            _lastTime = now;
            _velocity = Vector3.Zero;
            _liftVelocity = 0;
            return Current = target with { Roll = 0, Yaw = 0, Pitch = 0, LegacyLift = 0 };
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
        float lift = Math.Max(0, Step(Current.LegacyLift, target.LegacyLift, ref _liftVelocity, 12, 40));
        return Current = target with { Roll = roll, Yaw = yaw, Pitch = pitch, LegacyLift = lift };

        float Step(float value, float goal, ref float velocity, float bound, float rate = 0)
        {
            float limit = rate > 0 ? rate : speed;
            goal = Math.Clamp(goal, -bound, bound);
            float error = value - goal;
            float temp = (velocity + omega * error) * dt;
            float unconstrained = goal + (error + temp) * decay;
            float nextVelocity = (velocity - omega * temp) * decay;
            float next = Math.Clamp(unconstrained, value - limit * dt, value + limit * dt);
            next = Math.Clamp(next, -bound, bound);
            // A reversing target may initially move away due to inertia, but
            // the spring cannot overshoot through the target or its limits.
            if ((goal - value) * (next - goal) > 0)
            {
                velocity = 0;
                return goal;
            }
            velocity = MathF.Abs(next - unconstrained) > .00001f
                ? (next - value) / dt : Math.Clamp(nextVelocity, -limit, limit);
            return next;
        }
    }
}
