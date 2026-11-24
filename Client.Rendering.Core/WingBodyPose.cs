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
    private static readonly Lazy<Vector2[]> Anchors = new(Load);

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
        delta = anchors[bodyFrame] - anchors[reference];
        return float.IsFinite(delta.X) && float.IsFinite(delta.Y);
    }

    private static Vector2[] Load()
    {
        using var stream = typeof(WingBodyPose).Assembly.GetManifestResourceStream("CrystalM2.ArmourBodyPoses")
            ?? throw new InvalidDataException("Missing embedded CArmour pose atlas.");
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var frames = root.GetProperty("frames");
        if (root.GetProperty("femaleFrameOffset").GetInt32() != GenderStride ||
            frames.GetArrayLength() != GenderStride * 2)
            throw new InvalidDataException("Unexpected classic CArmour pose bank.");
        var result = new Vector2[frames.GetArrayLength()];
        int index = 0;
        foreach (var frame in frames.EnumerateArray())
        {
            var points = frame.GetProperty("anchors");
            // Neck plus shoulder midpoint estimates the upper torso rather
            // than the moving sprite bounding box or its ground shadow.
            Vector2 neck = Read(points[1]);
            Vector2 shoulderA = Read(points[2]);
            Vector2 shoulderB = Read(points[3]);
            result[index++] = neck * .35f + (shoulderA + shoulderB) * .325f;
        }
        return result;

        static Vector2 Read(JsonElement point) => new(point[0].GetSingle(), point[1].GetSingle());
    }
}
