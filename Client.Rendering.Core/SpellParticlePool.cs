using System.Drawing;
using System.Numerics;

namespace CrystalM2.Rendering.Core;

public enum SpellParticleShape { Spark, Wisp, Halo, Slash, Shard }

public readonly record struct SpellParticle(Vector2 Origin, Vector2 Velocity, long Start, int Duration,
    int Size, Color Colour, SpellParticleShape Shape, byte Direction)
{
    public float Age(long now) => Math.Clamp((now - Start) / (float)Duration, 0, 1);
    public Vector2 Position(long now)
    {
        float age = Age(now);
        return Origin + Velocity * (age * (2 - age));
    }
}

// A scene owns one pool. Positions and lifetimes use world pixels and the game clock.
public sealed class SpellParticlePool
{
    private readonly SpellParticle[] _items;
    private uint _random = 0x9e3779b9;
    private int _count;

    public SpellParticlePool(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _items = new SpellParticle[capacity];
    }

    public ReadOnlySpan<SpellParticle> Active => _items.AsSpan(0, _count);

    public bool TryAdd(SpellParticle particle)
    {
        if (particle.Duration <= 0 || particle.Size <= 0 || _count == _items.Length) return false;
        _items[_count++] = particle;
        return true;
    }

    public bool Burst(Vector2 center, Vector2 heading, Color colour, long now, int count,
        SpellParticleShape shape = SpellParticleShape.Spark)
    {
        int startCount = _count;
        for (int i = 0; i < count && _count < _items.Length; i++)
        {
            float angle = NextUnit() * MathF.Tau;
            Vector2 drift = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .65f) * (10 + NextUnit() * 38);
            TryAdd(new SpellParticle(center, drift + heading * 16, now, 260 + (int)(NextUnit() * 370),
                8 + (int)(NextUnit() * 14), colour, i % 4 == 0 ? SpellParticleShape.Wisp : shape, 0));
        }
        return _count != startCount;
    }

    // True for the last expired frame too, so cached world draws erase it.
    public bool Advance(long now)
    {
        if (_count == 0) return false;
        int alive = 0;
        for (int i = 0; i < _count; i++)
            if (now - _items[i].Start < _items[i].Duration)
                _items[alive++] = _items[i];
        _count = alive;
        return true;
    }

    public void Clear() => _count = 0;

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
    public const int Width = 256, Height = 64;
    private const int Cell = 32;

    public static Rectangle Part(SpellParticleShape shape, byte direction = 0) => shape switch
    {
        SpellParticleShape.Slash => new Rectangle((direction & 7) * Cell, Cell, Cell, Cell),
        _ => new Rectangle((int)shape * Cell, 0, Cell, Cell)
    };

    public static byte[] Generate()
    {
        byte[] pixels = new byte[Width * Height * 4];
        Paint(SpellParticleShape.Spark);
        Paint(SpellParticleShape.Wisp);
        Paint(SpellParticleShape.Halo);
        Paint(SpellParticleShape.Shard);
        for (byte direction = 0; direction < 8; direction++) Paint(SpellParticleShape.Slash, direction);
        return pixels;

        void Paint(SpellParticleShape shape, byte direction = 0)
        {
            Rectangle rect = Part(shape, direction);
            float angle = (direction - 2) * MathF.PI / 4;
            float cos = MathF.Cos(angle), sin = MathF.Sin(angle);
            for (int y = 1; y < Cell - 1; y++)
            for (int x = 1; x < Cell - 1; x++)
            {
                float dx = (x - 15.5f) / 15, dy = (y - 15.5f) / 15;
                float radius = MathF.Sqrt(dx * dx + dy * dy);
                float front = dx * cos + dy * sin;
                float alpha = shape switch
                {
                    SpellParticleShape.Spark => 1 - MathF.Pow(MathF.Abs(dx) + MathF.Abs(dy) * 1.3f, .65f),
                    SpellParticleShape.Wisp => (1 - radius) * (1 - radius) * (radius <= 1 ? .6f : 0),
                    SpellParticleShape.Halo => 1 - MathF.Abs(radius - .75f) * 13,
                    SpellParticleShape.Slash => (1 - MathF.Abs(radius - .74f) * 9) * Math.Clamp(front * 2, 0, 1),
                    SpellParticleShape.Shard => 1 - MathF.Abs(dx + dy * .45f) * 3.8f - MathF.Abs(dy),
                    _ => 0
                };
                int index = ((rect.Y + y) * Width + rect.X + x) * 4;
                pixels[index] = pixels[index + 1] = pixels[index + 2] = 255;
                pixels[index + 3] = (byte)(255 * Math.Clamp(alpha, 0, 1));
            }
        }
    }
}
