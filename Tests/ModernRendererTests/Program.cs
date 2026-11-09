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

        foreach (int phase in new[] { 0, 1 })
        {
            const int width = 12, height = 10, pitch = 52;
            byte[] world = new byte[pitch * height];
            for (int y = 2; y < 8; y++)
            for (int x = 3; x < 10; x++)
            {
                if (((x + y) & 1) != phase) continue;
                int p = y * pitch + x * 4;
                world[p + 2] = 8;
                world[p + 3] = 255;
            }
            world[4] = 77;
            world[7] = 255;
            world[9] = 4;
            world[11] = 255;
            Assertions.True(BakedShadowRepair.Smooth(world, width, height, pitch),
                "Repair checker shadows in either parity with a padded row pitch.");
            for (int x = 5; x < 8; x++)
            {
                int p = 4 * pitch + x * 4;
                Assertions.True(world[p + 2] == 0 && world[p + 3] is >= 120 and <= 128,
                    "Replace the opaque dots and transparent holes with stable translucent coverage.");
            }
            Assertions.True(world[4] == 77 && world[7] == 255 && world[9] == 4 && world[11] == 255,
                "Preserve nearby colour and solid dark details.");
            byte[] softened = world.ToArray();
            Assertions.True(!BakedShadowRepair.Smooth(world, width, height, pitch) && world.SequenceEqual(softened),
                "Do not reprocess already-softened artwork.");
        }
        byte[] darkTiles = Enumerable.Repeat(new byte[] { 0, 0, 8, 255, 30, 40, 50, 255 }, 32)
            .SelectMany(pair => pair).ToArray();
        Assertions.True(!BakedShadowRepair.Smooth(darkTiles, 8, 8, 32),
            "Opaque dark tile patterns are not transparent checker shadows.");
        CheckShadowTileEdges();

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
        // F12 spell compositing must behave consistently for authored coverage
        // particles and for legacy spell textures. Keying is unnecessary for an
        // alpha coverage atlas, but glow-only and cutoff still matter.
        var authoredParticles = new SpellParticleGlowPolicy(0, .44f, .7f, false);
        var keyedParticles = new SpellParticleGlowPolicy(1, .44f, .7f, false);
        var enhancedParticles = new SpellParticleGlowPolicy(2, .44f, .7f, false);
        var glowOnlyParticles = new SpellParticleGlowPolicy(3, .44f, .7f, true);
        Assertions.True(authoredParticles.ShowAuthored && !authoredParticles.ShowGlow &&
                        keyedParticles.ShowAuthored && !keyedParticles.ShowGlow,
            "Authored and black-key particle modes must not add a glow pass.");
        Assertions.True(enhancedParticles.ShowAuthored && enhancedParticles.ShowGlow &&
                        !glowOnlyParticles.ShowAuthored && glowOnlyParticles.ShowGlow &&
                        glowOnlyParticles.ScreenBlend,
            "Glow-plus-base and glow-only must select the correct passes and blend mode.");
        Assertions.True(enhancedParticles.GlowAmount(new Vector3(.2f, .1f, .08f)) == 0f &&
                        enhancedParticles.GlowAmount(Vector3.One) > .69f,
            "The F12 glow cutoff should reject dark particles and retain bright sparks.");
        Assertions.True(new SpellParticleGlowPolicy(2, 1f, 2f, false).GlowAmount(Vector3.One) == 0f,
            "A maximum cutoff must suppress glow, even for the brightest particle.");
        Assertions.True(new SpellParticleGlowPolicy(2, 0f, 0f, false).GlowAmount(Vector3.One) == 0f &&
                        !new SpellParticleGlowPolicy(3, 0f, 0f, true).ShowGlow,
            "Zero glow power must disable the glow layer, including glow-only mode.");
        Assertions.True(new SpellParticleGlowPolicy(2, 0f, 2f, true).GlowAmount(Vector3.One) > 1.9f,
            "Glow power up to 200 percent must scale particle highlights.");

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

        var motion = new SpellParticleMotion(new Vector2(48, 12), new Vector2(-12, 4), 10, 64, 290, 5);
        var airborne = particle with
        {
            Duration = 900, Motion = motion, FieldObjectID = 93,
            Impact = new SpellParticleImpact(SpellParticleShape.Spark, 3, 6, 300, 40)
        };
        long landing = airborne.Start + motion.LandingMilliseconds;
        Assertions.True(airborne.Position(200).Y < origin.Y && airborne.DepthAt(200) > origin.Y &&
            airborne.Angle(200) > airborne.Rotation, "Height, ground depth and spin advance on separate axes.");
        Assertions.True(pool.TryAdd(airborne) && pool.Advance(landing - 1) && pool.Active.Length == 1,
            "Keep the parent visible until it reaches the projected ground.");
        Assertions.True(pool.Advance(landing + 40) && pool.Active.Length == 3 &&
            pool.Active.ToArray().All(p => p.Start == landing && p.FieldObjectID == 93 &&
                Vector2.Distance(p.Origin, airborne.Position(landing)) < .01f),
            "Landing spawns one group of children at the physical contact point and backdates their age.");
        Assertions.True(pool.Advance(landing + 41) && pool.Active.Length == 3, "The contact must fire once.");
        Assertions.True(pool.RemoveField(93) && pool.Active.IsEmpty, "Removing a field clears its children.");
        pool.TryAdd(airborne);
        Assertions.True(pool.Advance(landing + 500) && pool.Active.IsEmpty, "Do not spawn expired contact debris after a stall.");
        pool.Schedule(airborne);
        Assertions.True(pool.Advance(landing + 40) && pool.Active.Length == 3 &&
            pool.Active.ToArray().All(p => p.Start == landing), "A late scheduled parent should enter at the child stage immediately.");
        pool.Clear();

        var gradient = new SpellParticleColourCurve((0, 0x801000), (.5f, 0xff8000), (1, 0xfffffe));
        var ember = particle with { Colour = Color.FromArgb(170, Color.White), ColourCurve = gradient };
        Assertions.True(ember.ColourAt(.75f).ToArgb() == Color.FromArgb(170, 255, 192, 127).ToArgb(),
            "An age curve interpolates its colour keys while preserving authored opacity.");
        var fastEmber = ember with { Duration = 1000, Velocity = new Vector2(100, 0), ColourCurveFullSpeed = 200 };
        Assertions.True(fastEmber.ColourAt(0).ToArgb() == Color.FromArgb(170, 255, 255, 254).ToArgb() &&
            fastEmber.ColourAt(1).ToArgb() == Color.FromArgb(170, 128, 16, 0).ToArgb(),
            "A speed curve tracks the instantaneous velocity of a decelerating particle.");
        fastEmber = fastEmber with { Motion = new SpellParticleMotion(new Vector2(100, 0), new Vector2(-100, 0), 0, 0, 0) };
        Assertions.True(fastEmber.ColourAt(0).ToArgb() == Color.FromArgb(170, 255, 128, 0).ToArgb(),
            "Simulated particles sample the colour curve from projected physics velocity.");

        byte[] atlas = SpellParticleAtlas.Generate();
        Assertions.Equal(SpellParticleAtlas.Width * SpellParticleAtlas.Height, atlas.Length,
            "All particle masks share one single-channel coverage texture.");
        Assertions.True(atlas.SequenceEqual(SpellParticleAtlas.Generate()), "Particle masks must be deterministic.");
        foreach (SpellParticleShape ribbon in new[] { SpellParticleShape.BladeRibbon, SpellParticleShape.MoonRibbon, SpellParticleShape.WindRibbon })
        {
            Rectangle part = SpellParticleAtlas.Part(ribbon);
            int row = (part.Top + 3 * part.Height / 4) * SpellParticleAtlas.Width;
            byte centre = atlas[row + part.Left + part.Width / 2];
            Assertions.True(centre > 0 && atlas[row + part.Left - 1] == centre && atlas[row + part.Right] == centre,
                "Ribbon coverage must continue across both joins when the GPU filters it.");
        }
        // Five source masks are packed into the Core assembly, not generated from
        // primitive geometry or loaded from missing runtime texture folders.
        foreach (var textured in new[]
            {
                SpellParticleShape.UserFlame, SpellParticleShape.UserLightning,
                SpellParticleShape.UserEmber, SpellParticleShape.UserSlash,
                SpellParticleShape.UserRock
            })
        {
            Rectangle part = SpellParticleAtlas.Part(textured);
            Assertions.True(part.Width == SpellParticleAtlas.Resolution &&
                part.Height == SpellParticleAtlas.Resolution,
                $"Packed texture {textured} must have a full atlas cell.");
            int covered = 0, transitions = 0;
            for (int y = part.Top; y < part.Bottom; y++)
            for (int x = part.Left; x < part.Right; x++)
            {
                byte a = atlas[y * SpellParticleAtlas.Width + x];
                if (a != 0) covered++;
                if (x > part.Left && a != atlas[y * SpellParticleAtlas.Width + x - 1])
                    transitions++;
            }
            Assertions.True(covered > 120 && transitions > 100,
                $"Packed user texture {textured} must contain detailed coverage rather than a blank primitive.");
        }
        var shapes = Enum.GetValues<SpellParticleShape>().Where(shape => shape != SpellParticleShape.Slash).ToArray();
        var distinct = new HashSet<Rectangle>();
        foreach (SpellParticleShape shape in shapes)
        {
            bool animated = shape is SpellParticleShape.FlameSequence or SpellParticleShape.FireSequence or SpellParticleShape.SmokeSequence;
            int count = animated ? SpellParticleAtlas.ClipFrames : 1;
            for (byte frame = 0; frame < count; frame++)
            {
                Rectangle region = SpellParticleAtlas.Part(shape, frame: frame);
                int minimum = animated ? 128 : 256;
                Assertions.True(distinct.Add(region) && region.Right <= SpellParticleAtlas.Width && region.Bottom <= SpellParticleAtlas.Height &&
                    region.Width >= minimum && region.Height >= minimum, $"{shape} frame {frame} needs its own full-resolution atlas cell.");
                bool hasAlpha = false;
                for (int y = region.Top; y < region.Bottom; y++)
                for (int x = region.Left; x < region.Right; x++)
                    hasAlpha |= atlas[y * SpellParticleAtlas.Width + x] != 0;
                Assertions.True(hasAlpha, $"{shape} frame {frame} has a visible mask.");
            }
        }
        var projectile = particle with { Linear = true, Layer = SpellParticleLayer.Emissive, Size = 80, Aspect = .25f, Rotation = MathF.PI / 2 };
        Assertions.Equal(new Vector2(136, 96), projectile.Position(200), "Projectiles arrive on the authored timeline.");
        Rectangle rotated = projectile.Bounds(projectile.Position(200), projectile.Dimensions(.5f, 1));
        Assertions.True(rotated.Height >= 80 && rotated.Width <= 22, "Light and viewport bounds cover the rotated projectile.");
        for (byte direction = 0; direction < 8; direction++)
        {
            Rectangle part = SpellParticleAtlas.Part(SpellParticleShape.Slash, direction);
            Assertions.True(part.Right <= SpellParticleAtlas.Width && part.Bottom <= SpellParticleAtlas.Height,
                "Every directional slash must fit inside the shared atlas.");
            bool hasAlpha = false;
            for (int y = part.Top; y < part.Bottom; y++)
            for (int x = part.Left; x < part.Right; x++)
                hasAlpha |= atlas[y * SpellParticleAtlas.Width + x] > 0;
            Assertions.True(hasAlpha, "Each facing must have a visible slash mask.");
        }
    }

    private static void CheckShadowTileEdges()
    {
        const int width = 7, height = 8, pitch = width * 4;
        byte[] left = CheckerTile(width, height, 0);
        byte[] right = CheckerTile(width, height, 1);
        byte[] previousRepair = left.ToArray();
        Assertions.True(BakedShadowRepair.Smooth(previousRepair, width, height, pitch), "Reproduce the old per-frame fade.");
        int edge = 3 * pitch + (width - 1) * 4 + 3;
        Assertions.True(previousRepair[edge] < 110, "Missing samples used to fade the edge of a cut tile.");
        Assertions.True(BakedShadowRepair.Smooth(left, width, height, pitch, joinTileEdges: true) &&
                        BakedShadowRepair.Smooth(right, width, height, pitch, joinTileEdges: true), "Repair both sides of the tile join.");
        for (int row = 0; row < height; row++)
        {
            byte leftAlpha = left[row * pitch + (width - 1) * 4 + 3];
            byte rightAlpha = right[row * pitch + 3];
            Assertions.True(leftAlpha >= 126 && Math.Abs(leftAlpha - rightAlpha) <= 2,
                "Connected map shadows must retain coverage across opposite phases at the cut.");
        }

        byte[] thinTile = CheckerTile(2, 6, 0, alpha: 144, red: 16);
        Assertions.True(BakedShadowRepair.Smooth(thinTile, 2, 6, 8, joinTileEdges: true) && thinTile[3] is >= 70 and <= 74,
            "Recognise narrow, darker-tinted shadow strips and preserve their lower opacity.");

        byte[] raw = CheckerTile(width, height, 0);
        byte[] converted = raw.ToArray();
        for (int i = 0; i < converted.Length; i += 4)
        {
            converted[i + 2] = 0;
            converted[i + 3] = 112;
        }
        byte[] edited = converted.ToArray();
        edited[2] = 127;
        byte[] editedBefore = edited.ToArray();
        Assertions.True(!BakedShadowRepair.TryUseOriginal(edited, width, height, pitch, raw.ToArray()) && edited.SequenceEqual(editedBefore),
            "Reject the older source if the current artwork differs.");
        Assertions.True(BakedShadowRepair.HasOfflineRepair(converted, width, height, pitch) &&
                        BakedShadowRepair.TryUseOriginal(converted, width, height, pitch, raw) && converted.SequenceEqual(left),
            "Rebuild an offline-converted tile from a matching backup using the same edge rules as its neighbour.");
    }

    private static byte[] CheckerTile(int width, int height, int phase, byte alpha = 255, byte red = 8)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (((x + y) & 1) != phase) continue;
            int p = (y * width + x) * 4;
            pixels[p + 2] = red;
            pixels[p + 3] = alpha;
        }
        return pixels;
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
