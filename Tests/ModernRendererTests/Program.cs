using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Drawing;
using System.Numerics;
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
        Span<SpriteVertex> vertices = stackalloc SpriteVertex[6];
        Assertions.True(SpriteGeometry.WriteQuad(vertices, new Rectangle(16, 8, 32, 24),
            64, 64, new Vector2(7, -3), Matrix4x4.Identity, Vector4.One, 100, 100),
            "A partly visible offset frame must survive viewport culling.");
        Assertions.True(vertices[0].Position == new Vector2(7, -3) &&
                        vertices[5].Position == new Vector2(7, 21),
            "Offset and crop size must survive vertex generation.");
        Assertions.True(vertices[0].Uv == new Vector2(0.25f, 0.125f) &&
                        vertices[4].Uv == new Vector2(0.75f, 0.5f),
            "Source rectangles must sample the original atlas region.");
        Assertions.True(!SpriteGeometry.WriteQuad(vertices, new Rectangle(0, 0, 20, 20),
            20, 20, new Vector2(101, 0), Matrix4x4.Identity, Vector4.One, 100, 100),
            "Fully offscreen sprites must not consume a batch slot.");
        var shear = Matrix4x4.Identity;
        shear.M21 = -2;
        Assertions.True(SpriteGeometry.WriteQuad(vertices, new Rectangle(0, 0, 20, 20),
            20, 20, new Vector2(110, 5), shear, Vector4.One, 100, 100),
            "Projected shadows intersecting the viewport must not be culled by their origin.");
        Assertions.True(!SpriteGeometry.WriteQuad(vertices, new Rectangle(0, 0, 20, 20),
            20, 20, new Vector2(float.NaN, 0), Matrix4x4.Identity, Vector4.One, 100, 100),
            "Non-finite vertices must never reach the GPU.");
        Assertions.True(!new ShadowSourcePolicy("").Allows("Data/Human/000000.Lib"),
            "Baked artwork must not acquire replacement shadows by default.");
        var unshadowed = new ShadowSourcePolicy("Data/Human/000000.Lib;Data/Monster/01.Lib");
        Assertions.True(unshadowed.Allows(@".\data\human\000000.lib") &&
                        !unshadowed.Allows("Data/NPC/000000.Lib") &&
                        !unshadowed.Allows("Data/Human/../NPC/000000.Lib"),
            "Shadow permission must be explicit for the exact library, without filename collisions.");

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

        // Mir3 two-layer sprites carry an independent authored auxiliary mask.
        // The new renderer must decode it without altering the baked base RGB.
        using var maskedArchive = new MemoryStream(MakeTestLib(source, includeMask: true));
        using var maskedLib = new LegacyMirLibReader(maskedArchive);
        LegacySpriteFrame layered = maskedLib.ReadFrame(0, includeMask: true);
        Assertions.True(layered.Bgra.SequenceEqual(source),
            "Reading a mask must not rewrite the base image.");
        Assertions.True(layered.Mask != null && layered.Mask.Bgra.SequenceEqual(source),
            "Second-layer mask BGRA must decompress independently.");
        Assertions.Equal(4, layered.Mask!.OffsetX,
            "Authored mask offsets must be preserved.");
        Assertions.Equal(-6, layered.Mask.OffsetY,
            "Authored mask offsets must be preserved.");
        Assertions.True(maskedLib.ReadFrame(0).Mask == null,
            "Mask decoding must remain opt-in.");
        Assertions.True(file.CanRead, "LeaveOpen must respect stream ownership.");
        Assertions.True(!lib.TryReadFrame(-1, out _) &&
                        !lib.TryReadFrame(4, out _), "Invalid image indices must fail safely.");

        byte[] malformed = (byte[])fixture.Clone();
        // Corrupt compressed payload; decoder must not silently return garbage.
        malformed[33] = 0; // GZip magic, after the 17-byte frame header.
        using var badStream = new MemoryStream(malformed);
        using var bad = new LegacyMirLibReader(badStream);
        bool rejected = false;
        try { bad.ReadFrame(0); }
        catch (InvalidDataException) { rejected = true; }
        Assertions.True(rejected, "Malformed compressed frame must be rejected.");

        CheckSpellParticles();
        Console.WriteLine("Baked-art fidelity, batching, LIB layout and spell particle checks passed.");
    }

    private static void CheckSpellParticles()
    {
        var pool = new SpellParticlePool(512);
        Vector2 origin = new(120, 96);
        var particle = new SpellParticle(origin, new Vector2(32, 0), 100, 200, 12,
            Color.OrangeRed, SpellParticleShape.Spark, 0);
        Assertions.True(!pool.TryAdd(particle with { Duration = 0 }), "Reject invalid particle lifetimes.");
        Assertions.True(pool.TryAdd(particle), "An active spell should occupy one pool slot.");
        Assertions.Equal(new Vector2(144, 96), particle.Position(200), "World motion must use the game clock.");
        Assertions.True(pool.Burst(origin, Vector2.Zero, Color.CornflowerBlue, 100, 700, SpellParticleShape.Shard),
            "Accept an oversized burst up to the fixed scene budget.");
        Assertions.Equal(512, pool.Active.Length, "Pool capacity must remain fixed.");
        Assertions.True(!pool.Burst(origin, Vector2.Zero, Color.White, 100, 1), "Reject overflow in a full scene.");
        Assertions.True(pool.Advance(300) && pool.Active[0] != particle, "Remove expired particles in place.");
        Assertions.True(pool.Advance(730) && pool.Active.IsEmpty && !pool.Advance(731),
            "Redraw once when the last particle expires.");
        pool.TryAdd(particle);
        pool.Clear();
        Assertions.True(pool.Active.IsEmpty, "A map reset must drop every active particle.");

        var later = particle with { Start = 400, Shape = SpellParticleShape.Halo };
        var sooner = particle with { Start = 200, Shape = SpellParticleShape.Shard };
        Assertions.True(pool.Schedule(later) && pool.Schedule(sooner), "Schedule layers out of order.");
        Assertions.True(!pool.Advance(199) && pool.Active.IsEmpty, "Future layers must stay hidden.");
        Assertions.True(pool.Advance(200) && pool.Active.Length == 1 && pool.Active[0] == sooner,
            "An earlier layer must start even when a later one was queued first.");
        Assertions.True(pool.Advance(450) && pool.Active.Length == 1 && pool.Active[0] == later &&
            pool.Active[0].Age(450) == .25f, "Preserve layer age when a frame arrives late.");
        pool.Schedule(later with { Start = 700 });
        pool.Clear();
        Assertions.True(!pool.Advance(750), "A map reset must cancel queued layers.");
        pool.Schedule(sooner);
        Assertions.True(!pool.Advance(900), "Do not replay expired layers after a frame stall.");

        byte[] atlas = SpellParticleAtlas.Generate();
        Assertions.Equal(SpellParticleAtlas.Width * SpellParticleAtlas.Height * 4, atlas.Length,
            "All particle masks share one BGRA texture.");
        Assertions.True(atlas.SequenceEqual(SpellParticleAtlas.Generate()), "Particle masks must be deterministic.");
        for (byte direction = 0; direction < 8; direction++)
        {
            Rectangle part = SpellParticleAtlas.Part(SpellParticleShape.Slash, direction);
            Assertions.True(part.Right <= SpellParticleAtlas.Width && part.Bottom <= SpellParticleAtlas.Height,
                "Every directional slash must fit inside the shared atlas.");
            bool hasAlpha = false;
            for (int y = part.Top; y < part.Bottom; y++)
            for (int x = part.Left; x < part.Right; x++)
                hasAlpha |= atlas[(y * SpellParticleAtlas.Width + x) * 4 + 3] > 0;
            Assertions.True(hasAlpha, "Each facing must have a visible slash mask.");
        }
    }

    private static byte[] MakeTestLib(byte[] bgra, bool includeMask = false)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.NoCompression, leaveOpen: true))
            gzip.Write(bgra);
        using var compressedMask = new MemoryStream();
        if (includeMask)
        {
            using var gzip = new GZipStream(compressedMask, CompressionLevel.NoCompression, leaveOpen: true);
            gzip.Write(bgra);
        }

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
            writer.Write(includeMask ? (byte)0x80 : (byte)0);
            writer.Write(checked((int)compressed.Length));
            writer.Write(compressed.ToArray());
            if (includeMask)
            {
                writer.Write((short)2); // mask width
                writer.Write((short)2); // mask height
                writer.Write((short)4); // authored mask x
                writer.Write((short)-6); // authored mask y
                writer.Write(checked((int)compressedMask.Length));
                writer.Write(compressedMask.ToArray());
            }
        }
        return raw.ToArray();
    }
}
