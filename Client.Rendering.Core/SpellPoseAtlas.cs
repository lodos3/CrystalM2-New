using System.Drawing;
using System.IO.Compression;
using System.Numerics;

namespace CrystalM2.Rendering.Core;

public static class SpellPoseAtlas
{
    public const int PageSize = 1024;
    public readonly record struct Pose(Point Offset, Size WorldSize, int FirstPlane);
    public readonly record struct Plane(int Page, Rectangle Source, Vector3? HaloColour = null);
    private sealed record Sequence(int Frames, Pose[] Poses);

    private static readonly Dictionary<string, Sequence> Sequences = new();
    private static readonly List<byte[]> Pages = new();
    private static readonly List<Plane> Planes = [default]; // Zero selects the general particle atlas.

    static SpellPoseAtlas()
    {
        using var resource = typeof(SpellPoseAtlas).Assembly.GetManifestResourceStream("CrystalM2.SpellPoseMasks");
        using var stream = new GZipStream(resource!, CompressionMode.Decompress);
        using var reader = new BinaryReader(stream);
        int x = 1, y = 1, rowHeight = 0, length;
        Pages.Add(new byte[PageSize * PageSize]);
        while ((length = stream.ReadByte()) != -1)
        {
            string name = new(reader.ReadChars(length));
            int directions = reader.ReadByte(), frames = reader.ReadByte();
            var poses = new Pose[directions * frames];
            for (int i = 0; i < poses.Length; i++)
            {
                var offset = new Point(reader.ReadInt16(), reader.ReadInt16());
                int w = reader.ReadUInt16(), h = reader.ReadUInt16();
                int step = reader.ReadByte();
                if (w == 0 || h == 0) continue;
                if (w + 2 > PageSize || h + 2 > PageSize) throw new InvalidDataException("Oversized particle mask.");
                poses[i] = new Pose(offset, new Size(w * step, h * step), Planes.Count);
                int first = Planes.Count;
                Vector3 energy = default;
                for (int c = 0; c < 3; c++)
                {
                    if (x + w >= PageSize) { x = 1; y += rowHeight + 2; rowHeight = 0; }
                    if (y + h >= PageSize)
                    {
                        Pages.Add(new byte[PageSize * PageSize]);
                        x = y = 1;
                        rowHeight = 0;
                    }
                    Planes.Add(new Plane(Pages.Count - 1, new Rectangle(x, y, w, h)));
                    long sum = 0;
                    for (int line = y; line < y + h; line++)
                    {
                        var row = Pages[^1].AsSpan(line * PageSize + x, w);
                        stream.ReadExactly(row);
                        foreach (byte coverage in row) sum += coverage;
                    }
                    energy[2 - c] = sum; // The archive stores blue, green, red.
                    x += w + 2;
                    rowHeight = Math.Max(rowHeight, h);
                }
                float peak = Math.Max(energy.X, Math.Max(energy.Y, energy.Z));
                if (peak > 0) Planes[first] = Planes[first] with { HaloColour = energy / peak };
            }
            Sequences.Add(name, new Sequence(frames, poses));
        }
    }

    public static Pose GetPose(string name, int direction, int frame)
    {
        var sequence = Sequences[name];
        if ((uint)direction >= sequence.Poses.Length / sequence.Frames || (uint)frame >= sequence.Frames) return default;
        return sequence.Poses[direction * sequence.Frames + frame];
    }

    public static Plane GetPlane(int index) => Planes[index];
    public static ReadOnlySpan<byte> GetPage(int page) => Pages[page];
}
