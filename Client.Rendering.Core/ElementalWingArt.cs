using System.IO.Compression;

namespace CrystalM2.Rendering.Core;

// One prepainted, independently compressed RGB coverage page per appearance.
internal static class ElementalWingArt
{
    private const int Width = 164, Height = 188, Padding = 26, Frames = 6;
    private static readonly long[] Offsets = ReadOffsets();
    private static Stream Open() => typeof(ElementalWingArt).Assembly.GetManifestResourceStream("CrystalM2.ElementalWings")!;

    private static long[] ReadOffsets()
    {
        using var stream = Open();
        using var reader = new BinaryReader(stream);
        var offsets = new long[WingPoseAtlas.ElementalPageCount];
        long offset = offsets.Length * sizeof(int);
        for (int i = 0; i < offsets.Length; i++)
        {
            offsets[i] = offset;
            offset += reader.ReadUInt32();
        }
        if (offset != stream.Length) throw new InvalidDataException("Invalid elemental material page index.");
        return offsets;
    }

    internal static int FrameCount(int style) => (uint)style < Offsets.Length ? Frames : 0;
    internal static WingPoseAtlas.Pose Pose(int style, int frame, int firstPage) =>
        (uint)style < Offsets.Length && (uint)frame < Frames
            ? new(-Padding, -Padding, Width, Height, firstPage + style, 1 + frame * (Width + 2), 1) : default;

    internal static byte[] ReadPage(int style)
    {
        using var stream = Open();
        stream.Position = Offsets[style];
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        var data = new byte[WingPoseAtlas.PageSize * WingPoseAtlas.PageSize];
        gzip.ReadExactly(data);
        return data;
    }
}
