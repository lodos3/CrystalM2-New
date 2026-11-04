using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CrystalM2.Rendering.Core;

static class Assertions
{
    internal static void True(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }

    internal static void Equal<T>(T expected, T actual, string reason) where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
            throw new InvalidOperationException(reason + ": expected " + expected + ", got " + actual);
    }
}

static class Program
{
    private static void Main()
    {
        // The Mir sprite already contains its original artist-painted lighting
        // and shadows. Raw BGRA pixels, offsets and alpha must be preserved.
        byte[] source = { 13, 24, 230, 255, 2, 60, 87, 128, 0, 0, 0, 0, 90, 45, 22, 90 };
        byte[] upload = BakedArtPolicy.CopyBgraExact(source, 2, 2);
        Assertions.True(upload.SequenceEqual(source),
            "Baked sprite colours and alpha must remain byte-for-byte identical.");
        upload[0] = 0;
        Assertions.True(source[0] == 13, "Upload must never mutate decoded sprite source.");

        BakedSpriteCommand command = BakedArtPolicy.ForArtwork(7, 10, 20, 2, 2);
        Assertions.True(command.TintR == 255 && command.TintG == 255 &&
                        command.TintB == 255 && command.TintA == 255,
            "Default sprite path must never darken or colour baked artwork.");
        Assertions.True(command.Blend == SpriteBlend.NonPremultiplied,
            "Legacy straight-alpha BGRA must use non-premultiplied blend.");
        Assertions.True(!command.RecalculateNormals && !command.CastNewShadow,
            "Baked light and shadow must never be replaced by default.");
        var glow = BakedArtPolicy.ForExplicitGlow(7, 10, 20, 2, 2, 80, 120, 240, 42);
        Assertions.True(glow.Blend == SpriteBlend.Additive && glow.TintB == 240,
            "Additional light must be a separately requested overlay.");

        var commandList = new[]
        {
            BakedArtPolicy.ForArtwork(2, 0, 0, 48, 32),
            BakedArtPolicy.ForArtwork(2, 48, 0, 48, 32),
            BakedArtPolicy.ForArtwork(3, 0, 0, 48, 32),
            BakedArtPolicy.ForArtwork(2, 0, 32, 48, 32)
        };
        var runs = SpriteRunPlanner.GroupAdjacent(commandList);
        Assertions.Equal(3, runs.Count, "Batching may merge only adjacent sprites.");
        Assertions.Equal(2, runs[0].Count, "Adjacent matching frames should batch.");
        Assertions.Equal(1, runs[1].Count, "Do not reorder overlapping layers.");
        Assertions.Equal(1, runs[2].Count, "Do not reorder later frames back into first run.");

        // Test-only encoded fixture using the exact v3, 17-byte header and
        // gzip layout observed in the current MLibrary implementation.
        byte[] fixture = MakeTestLib(source);
        using var file = new MemoryStream(fixture, writable: false);
        using var lib = new LegacyMirLibReader(file, leaveOpen: true);
        Assertions.Equal(1, lib.Count, "Legacy version-three frame count.");
        LegacySpriteFrame frame = lib.ReadFrame(0);
        Assertions.Equal(2, frame.Width, "Width must match original LIB metadata.");
        Assertions.Equal(2, frame.Height, "Height must match original LIB metadata.");
        Assertions.Equal(7, frame.OffsetX, "Original sprite draw offset must be preserved.");
        Assertions.Equal(-3, frame.OffsetY, "Original sprite draw offset must be preserved.");
        Assertions.True(frame.Bgra.SequenceEqual(source), "Gzip BGRA pixels must be preserved.");
        Assertions.True(frame.Mask == null, "Unmasked frames stay unmasked.");
        Assertions.True(file.CanRead, "LeaveOpen must respect stream ownership.");
        Assertions.True(!lib.TryReadFrame(-1, out _) &&
                        !lib.TryReadFrame(4, out _), "Invalid image indices must fail safely.");

        byte[] malformed = (byte[])fixture.Clone();
        // Corrupt compressed payload; decoder must not silently return garbage.
        malformed[37] = 0;
        using var badStream = new MemoryStream(malformed);
        using var bad = new LegacyMirLibReader(badStream);
        bool rejected = false;
        try { bad.ReadFrame(0); }
        catch (InvalidDataException) { rejected = true; }
        Assertions.True(rejected, "Malformed compressed frame must be rejected.");

        Console.WriteLine("Baked-art fidelity, order-preserving batching and real LIB layout checks passed.");
    }

    private static byte[] MakeTestLib(byte[] bgra)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.NoCompression, leaveOpen: true))
            gzip.Write(bgra);

        using var raw = new MemoryStream();
        using (var writer = new BinaryWriter(raw, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(3); // library version
            writer.Write(1); // frame count
            writer.Write(0); // frame index metadata offset, ignored for pixel decoding
            writer.Write(16); // offset of frame header
            writer.Write((short)2);
            writer.Write((short)2);
            writer.Write((short)7);
            writer.Write((short)-3);
            writer.Write((short)0); // baked shadow offset x
            writer.Write((short)0); // baked shadow offset y
            writer.Write((byte)0);
            writer.Write(checked((int)compressed.Length));
            writer.Write(compressed.ToArray());
        }
        return raw.ToArray();
    }
}
