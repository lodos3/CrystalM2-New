using System.Drawing;
using System.IO.Compression;
using System.Numerics;

namespace CrystalM2.Rendering.Core;

public enum SpellParticleShape
{
    Spark, Wisp, Halo, Slash, Shard, Flash, Bolt, Glyph, CellMark,
    Hammer, Shield, Claw, Banner, Prism, Lantern, Pillar, Feather,
    Orb, Flame, FireCurl, FrostCore, Talisman, BladeEdge, Smoke,
    WardBack, WardFront, FlameTongue, IceSpire, SigilEye, Filament, SigilDisc,
    FlameSequence, FireSequence, SmokeSequence, BladeRibbon,
    WarriorCrescent, WarriorTripleCut, WarriorSpark, WarriorDust, WarriorRing, WarriorImpact, MoonRibbon, WindRibbon,
    SoulGlyph, ArmourGlyph, CurseGlyph, Trigrams, YinYang, SpiritFigure
}

public enum SpellParticleLayer : byte { Legacy, Ground, World, Emissive, EmissiveCore, Smoke, Glow, Floor }

public readonly record struct SpellParticle(Vector2 Origin, Vector2 Velocity, long Start, int Duration,
    int Size, Color Colour, SpellParticleShape Shape, byte Direction, float Aspect = 0, float Rotation = 0,
    float Growth = 0, SpellParticleLayer Layer = SpellParticleLayer.Legacy, bool Linear = false, bool CastsLight = true,
    bool FrameOnly = false, uint FieldObjectID = 0, byte Frame = 0, SpriteQuad? Ribbon = null, float? DepthY = null,
    bool AlwaysOnTop = false)
{
    public bool Ground => Layer is SpellParticleLayer.Ground or SpellParticleLayer.Floor ||
        (Layer == SpellParticleLayer.Legacy && Shape == SpellParticleShape.Halo);
    public bool Emissive => Layer is SpellParticleLayer.Emissive or SpellParticleLayer.EmissiveCore or SpellParticleLayer.Glow;
    public float GroundDepth => DepthY ?? Origin.Y;

    public float Age(long now) => Math.Clamp((now - Start) / (float)Duration, 0, 1);
    public Vector2 Position(long now)
    {
        float age = Age(now);
        return Origin + Velocity * (Linear ? age : age * (2 - age));
    }

    public Vector2 Dimensions(float age, float scale)
    {
        float size = Size * scale;
        size *= Layer == SpellParticleLayer.Legacy ? Ground ? .7f + age * .3f : 1 - age * .22f : 1 + age * Growth;
        return new Vector2(size, size * (Aspect > 0 ? Aspect : Ground ? .5f : 1));
    }

    public Rectangle Bounds(Vector2 centre, Vector2 dimensions, float ribbonScale = 1f)
    {
        if (Ribbon is { } ribbon) return ribbon.Scaled(ribbonScale).Bounds(centre);
        if (Rotation != 0)
        {
            float sin = MathF.Abs(MathF.Sin(Rotation)), cos = MathF.Abs(MathF.Cos(Rotation));
            dimensions = new Vector2(cos * dimensions.X + sin * dimensions.Y, sin * dimensions.X + cos * dimensions.Y);
        }
        Vector2 half = dimensions * .5f;
        return Rectangle.FromLTRB((int)MathF.Floor(centre.X - half.X), (int)MathF.Floor(centre.Y - half.Y),
            (int)MathF.Ceiling(centre.X + half.X), (int)MathF.Ceiling(centre.Y + half.Y));
    }
}

// A scene owns one pool. Positions and lifetimes use world pixels and the game clock.
public sealed class SpellParticlePool
{
    private readonly SpellParticle[] _items;
    private readonly SpellParticle[] _pending;
    private uint _random = 0x9e3779b9;
    private int _count;
    private int _pendingCount;

    public SpellParticlePool(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _items = new SpellParticle[capacity];
        _pending = new SpellParticle[Math.Max(1, capacity / 2)];
    }

    public ReadOnlySpan<SpellParticle> Active => _items.AsSpan(0, _count);

    public bool TryAdd(SpellParticle particle)
    {
        if (particle.Duration <= 0 || particle.Size <= 0) return false;
        if (_count == _items.Length)
        {
            if (!particle.Emissive) return false;
            for (int i = 0; i < _count; i++)
            {
                if (_items[i].Emissive) continue;
                _items[i] = particle;
                return true;
            }
            return false;
        }
        _items[_count++] = particle;
        return true;
    }

    public bool Schedule(SpellParticle particle)
    {
        if (particle.Duration <= 0 || particle.Size <= 0 || _pendingCount == _pending.Length) return false;
        _pending[_pendingCount++] = particle;
        return true;
    }

    public bool Burst(Vector2 center, Vector2 heading, Color colour, long now, int count,
        SpellParticleShape shape = SpellParticleShape.Spark, int delay = 0)
    {
        bool emitted = false;
        for (int i = 0; i < count; i++)
        {
            float angle = NextUnit() * MathF.Tau;
            Vector2 drift = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .65f) * (10 + NextUnit() * 38);
            var particle = new SpellParticle(center, drift + heading * 16, now + delay, 260 + (int)(NextUnit() * 370),
                8 + (int)(NextUnit() * 14), colour, i % 4 == 0 ? SpellParticleShape.Wisp : shape, 0);
            if (!(delay > 0 ? Schedule(particle) : TryAdd(particle))) break;
            emitted = true;
        }
        return emitted;
    }

    // True for the last expired frame too, so cached world draws erase it.
    public bool Advance(long now)
    {
        bool redraw = _count > 0;
        int alive = 0;
        for (int i = 0; i < _count; i++)
            if (!_items[i].FrameOnly && now - _items[i].Start < _items[i].Duration)
                _items[alive++] = _items[i];
        _count = alive;
        int pending = 0;
        for (int i = 0; i < _pendingCount; i++)
        {
            SpellParticle particle = _pending[i];
            if (now < particle.Start)
                _pending[pending++] = particle;
            else if (now - particle.Start < particle.Duration)
                redraw |= TryAdd(particle);
        }
        _pendingCount = pending;
        return redraw;
    }

    public void Clear() => _count = _pendingCount = 0;

    public bool RemoveField(uint objectID)
    {
        if (objectID == 0) return false;
        int remaining = Compact(_items, _count);
        int pending = Compact(_pending, _pendingCount);
        bool removed = remaining != _count || pending != _pendingCount;
        _count = remaining;
        _pendingCount = pending;
        return removed;

        int Compact(SpellParticle[] particles, int length)
        {
            int write = 0;
            for (int i = 0; i < length; i++)
                if (particles[i].FieldObjectID != objectID)
                    particles[write++] = particles[i];
            return write;
        }
    }

    private float NextUnit()
    {
        _random ^= _random << 13;
        _random ^= _random >> 17;
        _random ^= _random << 5;
        return (_random >> 8) / 16777216f;
    }
}

public static class SpellParticleAtlas
{
    public const int Resolution = 256;
    public const int MipLevels = 6;
    // Regular masks retain a transparent texel on each side at the smallest mip.
    private const int Padding = 1 << (MipLevels - 1);
    private const int Cell = Resolution + 2 * Padding;
    public const int Width = 8 * Cell;
    public const int ClipFrames = 24;
    private const int ClipWidth = 128, FlameHeight = 256;
    private const int ClipColumns = Width / (ClipWidth + 2 * Padding);
    private const int ClipRows = (ClipFrames + ClipColumns - 1) / ClipColumns;
    private const int FlameTop = 5 * Cell;
    private const int FireTop = FlameTop + ClipRows * (FlameHeight + 2 * Padding);
    private const int SmokeTop = FireTop + ClipRows * (ClipWidth + 2 * Padding);
    private const int WarriorTop = SmokeTop + ClipRows * (ClipWidth + 2 * Padding);
    private const int TaoistTop = WarriorTop + Cell;
    public const int Height = TaoistTop + Cell;

    public static Rectangle Part(SpellParticleShape shape, byte direction = 0, byte frame = 0)
    {
        if (shape == SpellParticleShape.BladeRibbon)
            return new Rectangle((int)SpellParticleShape.Slash * Cell + Padding, Padding, Resolution, Resolution);
        if (shape >= SpellParticleShape.SoulGlyph)
            return new Rectangle((shape - SpellParticleShape.SoulGlyph) * Cell + Padding, TaoistTop + Padding, Resolution, Resolution);
        if (shape >= SpellParticleShape.WarriorCrescent)
            return new Rectangle((shape - SpellParticleShape.WarriorCrescent) * Cell + Padding,
                WarriorTop + Padding, Resolution, Resolution);
        if (shape is SpellParticleShape.FlameSequence or SpellParticleShape.FireSequence or SpellParticleShape.SmokeSequence)
        {
            int top = shape switch { SpellParticleShape.FlameSequence => FlameTop, SpellParticleShape.FireSequence => FireTop, _ => SmokeTop };
            int height = shape == SpellParticleShape.FlameSequence ? FlameHeight : ClipWidth;
            int index = frame % ClipFrames;
            return new Rectangle(index % ClipColumns * (ClipWidth + 2 * Padding) + Padding,
                top + index / ClipColumns * (height + 2 * Padding) + Padding, ClipWidth, height);
        }
        int column = shape == SpellParticleShape.Slash ? direction & 7 : (int)shape % 8;
        int row = shape == SpellParticleShape.Slash ? 1 : shape < SpellParticleShape.CellMark ? 0 : (int)shape / 8 + 1;
        return new Rectangle(column * Cell + Padding, row * Cell + Padding, Resolution, Resolution);
    }

    public static byte[] Generate()
    {
        byte[] coverage = new byte[Width * Height];
        foreach (SpellParticleShape shape in Enum.GetValues<SpellParticleShape>())
            if ((shape < SpellParticleShape.FlameSequence && shape != SpellParticleShape.Slash) ||
                shape is SpellParticleShape.BladeRibbon or SpellParticleShape.MoonRibbon or SpellParticleShape.WindRibbon ||
                shape >= SpellParticleShape.SoulGlyph) Paint(shape);
        for (byte direction = 0; direction < 8; direction++) Paint(SpellParticleShape.Slash, direction);
        ReadClips(coverage);
        using var warrior = ReadPack("CrystalM2.WarriorMasks");
        for (SpellParticleShape shape = SpellParticleShape.WarriorCrescent; shape <= SpellParticleShape.WarriorImpact; shape++)
            ReadMask(warrior, coverage, Part(shape));
        if (warrior.ReadByte() != -1) throw new InvalidDataException("Unexpected data after the Warrior particle masks.");
        return coverage;

        void Paint(SpellParticleShape shape, byte direction = 0)
        {
            Rectangle rect = Part(shape, direction);
            if (shape is SpellParticleShape.BladeRibbon or SpellParticleShape.MoonRibbon or SpellParticleShape.WindRibbon)
            {
                // The swept quads meet at their U edges. Extend the radial profile
                // into both U gutters so filtering cannot cut a line between strips.
                for (int y = 1; y < Resolution - 1; y++)
                {
                    float dy = (y + .5f) * (2f / Resolution) - 1;
                    float alpha = shape switch
                    {
                        SpellParticleShape.BladeRibbon => Sweep(dy),
                        SpellParticleShape.WindRibbon => MathF.Pow(1 - dy * dy, 3),
                        _ => Math.Clamp((dy + 1) * 2.4f, 0, 1) * Math.Clamp((1 - dy) * 3.5f, 0, 1) * (.85f + dy * .15f)
                    };
                    coverage.AsSpan((rect.Y + y) * Width + rect.X - Padding, Cell)
                        .Fill((byte)MathF.Round(255 * alpha));
                }
                return;
            }
            float angle = (direction - 2) * MathF.PI / 4;
            float cos = MathF.Cos(angle), sin = MathF.Sin(angle);
            for (int y = 1; y < Resolution - 1; y++)
            for (int x = 1; x < Resolution - 1; x++)
            {
                float dx = (x + .5f) * (2f / Resolution) - 1, dy = (y + .5f) * (2f / Resolution) - 1;
                float ax = MathF.Abs(dx), ay = MathF.Abs(dy);
                float radius = MathF.Sqrt(dx * dx + dy * dy);
                float front = dx * cos + dy * sin;
                float alpha = shape switch
                {
                    SpellParticleShape.Spark => 1 - MathF.Pow(ax + ay * 1.3f, .65f),
                    SpellParticleShape.Wisp => (1 - radius) * (1 - radius) * (radius <= 1 ? .6f : 0),
                    SpellParticleShape.Halo => Stroke(radius - .75f, .095f),
                    SpellParticleShape.Slash => Stroke(radius - .74f, .145f) * Math.Clamp(front * 2, 0, 1),
                    SpellParticleShape.Shard => 1 - MathF.Abs(dx + dy * .45f) * 3.8f - MathF.Abs(dy),
                    SpellParticleShape.Flash => MathF.Max(1 - radius * 2.6f, MathF.Max(1 - ax * 12 - ay, 1 - ay * 12 - ax)),
                    SpellParticleShape.Bolt => MathF.Max(0, 1 - ay * (2.1f + dx * dx * 5)) * (1 - MathF.Pow(ax, 2.3f)),
                    SpellParticleShape.Glyph => MathF.Max(Stroke(radius - .77f, .06f), MathF.Max(Stroke(radius - .60f, .04f) * .7f,
                        Stroke(ax + ay - .72f, .065f) * .86f)),
                    SpellParticleShape.CellMark => MathF.Max(Stroke(MathF.Max(ax, ay) + MathF.Max(0, ax + ay - 1.38f) - .86f, .10f),
                        ax < .82f && ay < .82f && ax + ay < 1.43f ? .13f : 0),
                    SpellParticleShape.Hammer => MathF.Max(Box(dx, dy + .40f, .78f, .23f), MathF.Max(Box(dx, dy - .15f, .09f, .53f), Box(dx, dy - .68f, .17f, .065f))),
                    SpellParticleShape.Shield => Shield(ax, dy),
                    SpellParticleShape.Claw => Claw(dx, dy),
                    SpellParticleShape.Banner => MathF.Max(Box(dx + .40f, dy, .052f, .87f), Box(dx - .02f, dy + .42f, .46f, .29f - dx * .20f) * .8f),
                    SpellParticleShape.Prism => MathF.Max(Stroke(ax * 1.45f + ay * .84f - .78f, .075f), Stroke(dx - dy * .23f, .08f) * Math.Clamp(1 - radius, 0, 1)),
                    SpellParticleShape.Lantern => MathF.Max(Stroke(MathF.Max(ax * 1.8f + ay * .32f, ay * 1.4f) - .77f, .10f),
                        MathF.Max(1 - radius * 1.48f, Box(dx, dy + .77f, .18f, .07f))),
                    SpellParticleShape.Pillar => MathF.Max(0, 1 - ax * (2.3f + (dy + 1) * 2.6f)) * (1 - MathF.Pow(ay, 3)),
                    SpellParticleShape.Feather => MathF.Max(0, 1 - MathF.Abs(dx - dy * .28f) * (2 + ay * 4) - ay * .92f),
                    SpellParticleShape.Orb => Math.Clamp((.96f - radius) * 1.95f, 0, 1),
                    SpellParticleShape.Flame => Flame(dx, dy, radius),
                    SpellParticleShape.FireCurl => FireCurl(dx, dy, radius),
                    SpellParticleShape.FrostCore => Frost(dx, dy),
                    SpellParticleShape.Talisman => Talisman(dx, dy),
                    SpellParticleShape.BladeEdge => MathF.Max(0, 1 - ay * (2.6f + 8 * ax * ax)) *
                        Math.Clamp((1 - ax) * 4, 0, 1),
                    SpellParticleShape.Smoke => Math.Clamp((.91f - radius) * (1.18f + .38f * MathF.Sin(dx * 13 + dy * 5) *
                        MathF.Cos(dy * 11 - dx * 4)), 0, 1),
                    SpellParticleShape.WardBack => Ward(dx, dy, front: false),
                    SpellParticleShape.WardFront => Ward(dx, dy, front: true),
                    SpellParticleShape.FlameTongue => Tongue(dx, dy),
                    SpellParticleShape.IceSpire => Spire(dx, dy),
                    SpellParticleShape.SigilEye => Eye(dx, dy),
                    SpellParticleShape.Filament => Math.Clamp((1 - ax) * 12, 0, 1) * MathF.Pow(1 - ay, 1.5f),
                    SpellParticleShape.SigilDisc => Sigil(dx, dy, radius),
                    SpellParticleShape.SoulGlyph or SpellParticleShape.ArmourGlyph or SpellParticleShape.CurseGlyph =>
                        Ink(dx, dy, GlyphStrokes(shape)),
                    SpellParticleShape.Trigrams => Trigrams(dx, dy),
                    SpellParticleShape.YinYang => YinYang(dx, dy, radius),
                    SpellParticleShape.SpiritFigure => SpiritFigure(dx, dy),
                    _ => 0
                };
                coverage[(rect.Y + y) * Width + rect.X + x] = (byte)MathF.Round(255 * Math.Clamp(alpha, 0, 1));
            }
        }
    }

    private static readonly Vector4[] SoulStrokes =
    [
        new(-.2f,-.86f,.14f,-.74f), new(-.68f,-.64f,.77f,-.67f), new(-.66f,-.61f,-.72f,.25f),
        new(-.72f,.25f,-.89f,.68f), new(-.53f,-.41f,-.08f,-.43f), new(-.31f,-.60f,-.33f,-.15f),
        new(-.32f,-.39f,-.54f,-.18f), new(-.30f,-.36f,-.09f,-.19f), new(.05f,-.42f,.59f,-.44f),
        new(.31f,-.61f,.31f,-.14f), new(.29f,-.39f,.08f,-.17f), new(.31f,-.37f,.56f,-.19f),
        new(-.07f,-.23f,-.22f,-.04f), new(-.37f,-.04f,.43f,-.07f), new(-.36f,-.02f,-.36f,.36f),
        new(.42f,-.05f,.43f,.34f), new(-.34f,.15f,.42f,.14f), new(-.34f,.35f,.43f,.34f),
        new(.01f,-.05f,.02f,.37f), new(-.14f,.34f,-.28f,.62f), new(-.28f,.62f,-.60f,.79f),
        new(.13f,.35f,.18f,.70f), new(.18f,.70f,.56f,.76f), new(.56f,.76f,.78f,.62f),
        new(.37f,.44f,.24f,.60f), new(.24f,.60f,.56f,.58f), new(.49f,.47f,.62f,.61f)
    ];

    private static readonly Vector4[] ArmourStrokes =
    [
        new(-.70f,-.74f,-.73f,.80f), new(-.69f,-.73f,-.27f,-.76f), new(-.27f,-.76f,-.46f,-.37f),
        new(-.46f,-.37f,-.25f,-.12f), new(-.25f,-.12f,-.31f,.16f), new(-.31f,.16f,-.58f,.20f),
        new(.14f,-.81f,.34f,-.62f), new(-.19f,-.49f,.82f,-.53f), new(.21f,-.45f,.10f,.25f),
        new(.10f,.25f,-.23f,.67f), new(-.23f,.67f,-.48f,.81f), new(.15f,-.12f,.65f,-.18f),
        new(.65f,-.18f,.54f,.58f), new(.54f,.58f,.36f,.77f), new(.36f,.77f,.11f,.64f)
    ];

    private static readonly Vector4[] CurseStrokes =
    [
        new(-.71f,-.70f,-.22f,-.72f), new(-.70f,-.70f,-.68f,-.26f), new(-.22f,-.71f,-.22f,-.29f),
        new(-.69f,-.28f,-.23f,-.29f), new(.12f,-.73f,.65f,-.72f), new(.12f,-.73f,.13f,-.27f),
        new(.65f,-.72f,.63f,-.28f), new(.13f,-.28f,.63f,-.29f), new(-.40f,-.06f,.45f,-.09f),
        new(-.37f,-.06f,-.41f,.35f), new(-.41f,.35f,-.65f,.66f), new(-.65f,.66f,-.87f,.76f),
        new(.41f,-.08f,.33f,.56f), new(.33f,.56f,.47f,.72f), new(.47f,.72f,.77f,.72f),
        new(.77f,.72f,.86f,.47f)
    ];

    public static ReadOnlySpan<Vector4> GlyphStrokes(SpellParticleShape shape) => shape switch
    {
        SpellParticleShape.SoulGlyph => SoulStrokes,
        SpellParticleShape.ArmourGlyph => ArmourStrokes,
        SpellParticleShape.CurseGlyph => CurseStrokes,
        _ => []
    };

    private static float Ink(float x, float y, ReadOnlySpan<Vector4> strokes)
    {
        var point = new Vector2(x + .012f * MathF.Sin(y * 37 + x * 23), y + .009f * MathF.Sin(x * 56 - y * 12));
        float ink = 0;
        foreach (var stroke in strokes)
        {
            Vector2 delta = new(stroke.Z - stroke.X, stroke.W - stroke.Y);
            Vector2 local = point - new Vector2(stroke.X, stroke.Y);
            float progress = Math.Clamp(Vector2.Dot(local, delta) / delta.LengthSquared(), 0, 1);
            float width = .023f + .039f * MathF.Sin(progress * MathF.PI * .87f + .24f);
            ink = MathF.Max(ink, Stroke((local - delta * progress).Length(), width));
        }
        return ink;
    }

    private static float Trigrams(float x, float y)
    {
        float octagon = MathF.Max(MathF.Max(MathF.Abs(x), MathF.Abs(y)), (MathF.Abs(x) + MathF.Abs(y)) * .707107f);
        float ink = MathF.Max(Stroke(octagon - .9f, .022f), Stroke(octagon - .82f, .012f));
        const float eighth = MathF.PI / 4;
        int sector = (int)MathF.Round(MathF.Atan2(y, x) / eighth);
        float cos = MathF.Cos(sector * eighth), sin = MathF.Sin(sector * eighth);
        float across = MathF.Abs(y * cos - x * sin), forward = x * cos + y * sin;
        if (across > .245f) return ink;
        for (int i = 0; i < 3; i++)
            if (((sector & 7) & (1 << i)) != 0 || across > .04f)
                ink = MathF.Max(ink, Stroke(forward - (.46f + i * .108f), .020f) * Math.Clamp((.245f - across) * 36, 0, 1));
        return ink;
    }

    private static float YinYang(float x, float y, float radius)
    {
        if (radius > .86f) return Stroke(radius - .89f, .023f) * .65f;
        float upper = Vector2.Distance(new Vector2(x, y), new Vector2(0, -.43f));
        float lower = Vector2.Distance(new Vector2(x, y), new Vector2(0, .43f));
        bool light = upper < .43f || (x >= 0 && lower > .43f);
        if (upper < .088f || lower < .088f) light = !light;
        return light ? Math.Clamp((.86f - radius) * 55, 0, 1) : Stroke(radius - .847f, .017f) * .68f;
    }

    private static float SpiritFigure(float x, float y)
    {
        float sway = .12f * MathF.Sin(y * 10 + x * 5);
        float head = 1 - Vector2.Distance(new Vector2(x * 1.7f, (y + .58f) * 1.35f), Vector2.Zero) * 3.4f;
        float torso = 1 - MathF.Abs(x - sway) * (2.8f + MathF.Max(0, y) * 4.8f) - MathF.Abs(y + .07f) * 1.4f;
        float rags = MathF.Sin(x * 38 + y * 13) * MathF.Sin(y * 22 - x * 4);
        return MathF.Max(head * .82f, torso * (.53f + .21f * rags)) * Math.Clamp((.93f - y) * 5, 0, 1);
    }

    private static void ReadClips(byte[] coverage)
    {
        // CC0 frames from Brackeys' VFX Bundle, stored in order as tightly packed R8 rows.
        using var frames = ReadPack("CrystalM2.SpellMasks");
        foreach (SpellParticleShape shape in new[] { SpellParticleShape.FlameSequence, SpellParticleShape.FireSequence, SpellParticleShape.SmokeSequence })
        for (byte i = 0; i < ClipFrames; i++)
            ReadMask(frames, coverage, Part(shape, frame: i));
        if (frames.ReadByte() != -1) throw new InvalidDataException("Unexpected data after the spell particle animations.");
    }

    private static GZipStream ReadPack(string name) => new(typeof(SpellParticleAtlas).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidDataException($"Missing particle coverage resource {name}."), CompressionMode.Decompress);

    private static void ReadMask(Stream stream, byte[] coverage, Rectangle part)
    {
        for (int y = part.Top; y < part.Bottom; y++)
            stream.ReadExactly(coverage.AsSpan(y * Width + part.Left, part.Width));
    }

    private static float Sweep(float y)
    {
        // V follows the blade from hilt to tip. Keep U uniform so adjacent strips share the same edge.
        float length = (y + 1) * .5f;
        float body = length * length * Math.Clamp((1 - length) * 13, 0, 1);
        float edge = Stroke(length - .87f, .10f);
        return MathF.Max(body * .60f, edge);
    }

    private static float Stroke(float distance, float width) => MathF.Max(0, 1 - MathF.Abs(distance) / width);

    private static float Box(float x, float y, float halfWidth, float halfHeight) =>
        Math.Clamp(1 - MathF.Max(MathF.Abs(x) - halfWidth, MathF.Abs(y) - halfHeight) * 22, 0, 1);

    private static float Shield(float x, float y)
    {
        float edge = MathF.Max(x + MathF.Max(0, y) * .67f - .68f, MathF.Max(-y - .69f, y - .84f));
        return MathF.Max(Stroke(edge, .074f), edge < 0 ? .25f : 0);
    }

    private static float Ward(float x, float y, bool front)
    {
        // A narrow crown and rounded base, with more reflection at the sides than over the actor.
        float u = x / (.87f + .11f * y), v = (y + .015f) / .92f;
        float edge = MathF.Sqrt(u * u + v * v) - 1;
        float rim = Stroke(edge, .057f) * (.59f + .41f * MathF.Abs(u));
        float haze = Stroke(edge + .025f, .22f) * .33f;
        if (!front) return MathF.Max(rim * .72f, haze);
        float grain = .5f + .5f * MathF.Sin(x * 49 + y * 19) * MathF.Cos(y * 52 - x * 17);
        float veil = Math.Clamp(-edge * 5, 0, 1) * (.095f + .09f * grain);
        float reflection = Stroke(u + .69f + .13f * y, .12f) * Stroke(y + .31f, .44f) * .63f;
        return MathF.Max(rim * .86f, MathF.Max(haze * .62f, reflection)) + veil;
    }

    private static float Tongue(float x, float y)
    {
        float height = (1 - y) * .5f;
        float flow = MathF.Sin(height * 9.5f + .2f) * height * .24f;
        float taper = (.78f - height * .61f) * Math.Clamp((1 - height) * 8, 0, 1);
        float tongue = Math.Clamp((taper - MathF.Abs(x - flow)) * 4, 0, 1);
        float fold = Stroke(x - flow * .55f + .18f, .19f) * Math.Clamp((.89f - height) * 3, 0, 1);
        float vortex = MathF.Sin((x - flow) * 17 - height * 33) * MathF.Cos(x * 21 + height * 19);
        float filaments = .71f + .19f * vortex + .12f * MathF.Sin((x - flow * .4f) * 39 - height * 14);
        return MathF.Max(tongue * (.97f - height * .44f) * filaments, fold * (.72f + .28f * vortex)) * Math.Clamp(height * 15, 0, 1);
    }

    private static float Spire(float x, float y)
    {
        float height = (1 - y) * .5f;
        float lean = x + height * .38f;
        float width = .73f * (1 - height);
        float crystal = Math.Clamp((width - MathF.Abs(lean)) * 10, 0, 1) * Math.Clamp(height * 16, 0, 1);
        float facet = Stroke(lean + width * .28f, .07f) + (lean < -.12f ? .35f : 0);
        return crystal * Math.Clamp(.48f + facet, 0, 1);
    }

    private static float Eye(float x, float y)
    {
        float ax = MathF.Abs(x);
        float lid = .56f * (1 - x * x / .83f);
        float edge = Stroke(MathF.Abs(y) - lid, .10f) * Math.Clamp((.94f - ax) * 9, 0, 1);
        float iris = Stroke(MathF.Sqrt(x * x * 3.5f + y * y) - .36f, .085f);
        float pupil = Stroke(x, .09f) * Stroke(y, .33f);
        return MathF.Max(edge, MathF.Max(iris, pupil));
    }

    private static float Sigil(float x, float y, float radius)
    {
        float angle = MathF.Atan2(y, x);
        float glyph = Stroke(MathF.Sin(angle * 12 + radius * 11), .32f) * Stroke(radius - .74f, .085f);
        float rim = Stroke(radius - .89f, .031f) + Stroke(radius - .82f, .022f) * .70f;
        float inner = Stroke(radius - .63f, .023f) * .89f;
        float point = .40f + .15f * MathF.Cos(angle * 6);
        float star = Stroke(radius - point, .022f) * .78f;
        float hub = Stroke(radius - .21f, .033f) * .87f;
        return MathF.Max(rim, MathF.Max(glyph, MathF.Max(inner, MathF.Max(star, hub))));
    }

    private static float Claw(float x, float y)
    {
        float curve = x + y * .52f - .25f * y * y;
        float cuts = MathF.Max(Stroke(curve, .12f), MathF.Max(Stroke(curve - .42f, .10f), Stroke(curve + .42f, .10f)) * .8f);
        return cuts * Math.Clamp((1 - MathF.Abs(y)) * 2, 0, 1);
    }

    private static float Flame(float x, float y, float radius)
    {
        float angle = MathF.Atan2(y, x);
        float tongues = .62f + .12f * MathF.Sin(angle * 5 + radius * 7) +
            .08f * MathF.Sin(angle * 9 - radius * 5);
        float curl = Stroke(radius - tongues, .21f) * (.68f + .32f * MathF.Sin(angle * 3 + 2 * radius));
        float core = Math.Clamp((.66f - radius) * 2.4f, 0, 1);
        return MathF.Max(curl, core) * Math.Clamp((1 - radius) * 3.4f, 0, 1);
    }

    private static float FireCurl(float x, float y, float radius)
    {
        float angle = MathF.Atan2(y, x);
        float arms = MathF.Pow(.5f + .5f * MathF.Cos(3 * angle - radius * 10), 3);
        float falloff = Math.Clamp(radius * 5, 0, 1) * Math.Clamp((.96f - radius) * 4, 0, 1);
        return arms * falloff;
    }

    private static float Frost(float x, float y)
    {
        float edge = MathF.Max(MathF.Abs(x) * .93f + MathF.Abs(y) * .68f,
            MathF.Abs(x * .37f - y * .73f));
        float facets = MathF.Max(Stroke(x - y * .31f, .1f), Stroke(y + x * .41f, .08f));
        return Math.Clamp((.83f - edge) * 5.5f, 0, 1) * (.55f + .45f * facets);
    }

    private static float Talisman(float x, float y)
    {
        float paper = Box(x + y * .25f, y + x * .12f, .75f, .43f);
        float seal = Stroke(MathF.Abs(x + .13f) + MathF.Abs(y) * 1.5f - .31f, .11f);
        return paper * (.58f + .42f * seal);
    }
}
