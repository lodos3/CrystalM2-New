using System.Numerics;
using System.Text.Json;

namespace CrystalM2.Rendering.Core;

/// <summary>
/// Cell-relative upper-torso positions extracted from the same CArmour pose
/// atlas used by generated armour. Coordinates already include the source LIB
/// frame's X/Y offsets; adding those offsets again would double the motion.
/// </summary>
public static class WingBodyPose
{
    private const int GenderStride = 808;
    private readonly record struct Sample(Vector2 Position, Vector2 Attachment, float Roll, Vector2 Torso);
    private static readonly Lazy<Sample[]> Anchors = new(Load);

    // Conservative authored twist estimates from the visible windup and
    // follow-through of the supplied CArmour/00.Lib. A 2D LIB cannot
    // provide exact 3D joint rotations.
    private static readonly float[] Attack1Yaw = [12, 24, 0, -18, -20, -8];
    private static readonly float[] Attack2Yaw = [10, 22, 12, -16, -12, 0];
    private static readonly float[] Attack3Yaw = [12, 20, 24, 12, -8, -18, -8, 0];
    private static readonly float[] MountedAttackYaw = [6, 10, 3, -10, -6, 0];

    public static bool TryGetDelta(int bodyFrame, int bankStart, int direction, bool mounted,
        out Vector2 delta)
    {
        delta = Vector2.Zero;
        if (bankStart is not (0 or GenderStride) || (uint)direction >= 8 ||
            (uint)(bodyFrame - bankStart) >= GenderStride) return false;
        int reference = bankStart + (mounted ? 416 : 0) + direction * 4;
        var anchors = Anchors.Value;
        if ((uint)bodyFrame >= (uint)anchors.Length || (uint)reference >= (uint)anchors.Length)
            return false;
        delta = anchors[bodyFrame].Position - anchors[reference].Position;
        return float.IsFinite(delta.X) && float.IsFinite(delta.Y);
    }

    public static bool TryGetAttachmentDelta(int bodyFrame, int bankStart, int direction, bool mounted,
        out Vector2 delta)
    {
        delta = Vector2.Zero;
        if (!TryGetDelta(bodyFrame, bankStart, direction, mounted, out _)) return false;
        int reference = bankStart + (mounted ? 416 : 0) + direction * 4;
        var anchors = Anchors.Value;
        delta = anchors[bodyFrame].Attachment - anchors[reference].Attachment;
        return float.IsFinite(delta.X) && float.IsFinite(delta.Y);
    }

    public static bool TryGetMotion(int bodyFrame, int bankStart, int direction, bool mounted,
        out WingPoseMotion motion)
    {
        motion = WingPoseMotion.Zero;
        if (!TryGetAttachmentDelta(bodyFrame, bankStart, direction, mounted, out var delta)) return false;
        var samples = Anchors.Value;
        int reference = bankStart + (mounted ? 416 : 0) + direction * 4;
        float angle = samples[bodyFrame].Roll - samples[reference].Roll;
        angle = MathF.Atan2(MathF.Sin(angle), MathF.Cos(angle));
        float roll = Math.Clamp(angle * .24f, -.18f, .18f);
        int local = bodyFrame - bankStart;
        float degrees = local switch
        {
            >= 136 and < 184 => Attack1Yaw[(local - 136) % 6],
            >= 184 and < 232 => Attack2Yaw[(local - 184) % 6],
            >= 232 and < 296 => Attack3Yaw[(local - 232) % 8],
            >= 584 and < 632 => MountedAttackYaw[(local - 584) % 6],
            _ => 0
        };
        if (local < 32 || local is >= 128 and < 136 || (local is >= 416 and < 448 && mounted))
        {
            motion = new WingPoseMotion(delta, 0, 0);
            return true;
        }
        bool locomotion = local is >= 32 and < 128 or >= 448 and < 560;
        if (locomotion)
        {
            roll = Math.Clamp(roll * .30f, -.055f, .055f);
        }
        bool attack = local is >= 136 and < 296 or >= 584 and < 632;
        float pitch = attack ? WingPoseMotion.AttackPitch(samples[bodyFrame].Torso - samples[reference].Torso, direction) : 0;
        motion = new WingPoseMotion(delta, roll, Math.Clamp(degrees * MathF.PI / 180f * .32f, -.14f, .14f), pitch);
        return true;
    }

    private static Sample[] Load()
    {
        using var stream = typeof(WingBodyPose).Assembly.GetManifestResourceStream("CrystalM2.ArmourBodyPoses")
            ?? throw new InvalidDataException("Missing embedded CArmour pose atlas.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var frames = root.GetProperty("frames");
        if (root.GetProperty("femaleFrameOffset").GetInt32() != GenderStride ||
            frames.GetArrayLength() != GenderStride * 2)
            throw new InvalidDataException("Unexpected classic CArmour pose bank.");
        var result = new Sample[frames.GetArrayLength()];
        int index = 0;
        foreach (var frame in frames.EnumerateArray())
        {
            var points = frame.GetProperty("anchors");
            // Neck plus shoulder midpoint estimates the upper torso rather
            // than the moving sprite bounding box or its ground shadow.
            Vector2 neck = Read(points[1]);
            Vector2 shoulderA = Read(points[2]);
            Vector2 shoulderB = Read(points[3]);
            Vector2 torso = neck - Read(points[6]);
            float roll = torso.LengthSquared() > 1f ? MathF.Atan2(torso.X, -torso.Y) : 0f;
            // Lower thoracic socket, not the head/shoulder centroid. The chest
            // and waist landmarks move with the part of the body the wing joins.
            Vector2 attachment = Vector2.Lerp(Read(points[4]), Read(points[5]), .65f);
            result[index++] = new Sample(neck * .35f + (shoulderA + shoulderB) * .325f, attachment, roll, torso);
        }
        return result;

        static Vector2 Read(JsonElement point) => new(point[0].GetSingle(), point[1].GetSingle());
    }
}
