using System.Drawing;
using System.IO.Compression;

namespace CrystalM2.Rendering.Core;

public static class WingPoseAtlas
{
    public const int PageSize = 1024;
    public const int DesignBank = 6;
    public readonly record struct Pose(int X, int Y, int Width, int Height, int Page, int U, int V);
    private static readonly Pose[][] Banks = new Pose[DesignBank + 8][]; // original archive format
    private const int ElementalStart = DesignBank + 8;
    private static readonly (long Offset, int Length)[] Pages;

    static WingPoseAtlas()
    {
        using var stream = Open();
        using var reader = new BinaryReader(stream);
        for (int bank = 0; bank < Banks.Length; bank++)
        {
            var poses = Banks[bank] = new Pose[reader.ReadUInt16()];
            for (int i = 0; i < poses.Length; i++)
                poses[i] = new(reader.ReadInt16(), reader.ReadInt16(), reader.ReadUInt16(), reader.ReadUInt16(),
                    reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
        }
        Pages = new (long, int)[reader.ReadUInt16()];
        long offset = stream.Position + Pages.Length * 4;
        for (int i = 0; i < Pages.Length; i++)
        {
            int length = reader.ReadInt32();
            Pages[i] = (offset, length);
            offset += length;
        }
    }

    private static Stream Open() => typeof(WingPoseAtlas).Assembly.GetManifestResourceStream("CrystalM2.WingPoses")!;
    public static int FirstElementalPage => Pages.Length;
    public const int ElementalPageCount = 10;

    public static int FrameCount(int bank) => bank >= ElementalStart ? ElementalWingArt.FrameCount(bank - ElementalStart) : Banks[bank].Length;
    public static Pose GetPose(int bank, int frame) => bank >= ElementalStart ? ElementalWingArt.Pose(bank - ElementalStart, frame, Pages.Length) :
        (uint)frame < Banks[bank].Length ? Banks[bank][frame] : default;

    // Negative handles distinguish wing masks from the spell atlas. Each library has fewer than 2048 frames.
    public static int Handle(int bank, int frame, int channel) => -1 - ((bank * 2048 + frame) * 3 + channel);
    public static (int Page, Rectangle Source) Plane(int handle)
    {
        int id = -1 - handle, frame = id / 3;
        var p = GetPose(frame / 2048, frame % 2048);
        return (p.Page, new Rectangle(p.U, p.V + (id % 3) * (p.Height + 2), p.Width, p.Height));
    }

    public static byte[] ReadPage(int page)
    {
        if (page >= Pages.Length) return ElementalWingArt.ReadPage(page - Pages.Length);
        using var stream = Open();
        stream.Position = Pages[page].Offset;
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        var bytes = new byte[PageSize * PageSize];
        gzip.ReadExactly(bytes);
        return bytes;
    }
}
