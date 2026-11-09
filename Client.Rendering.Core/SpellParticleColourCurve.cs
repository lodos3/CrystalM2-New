using System.Drawing;

namespace CrystalM2.Rendering.Core;

public sealed class SpellParticleColourCurve
{
    private readonly (float Position, int Rgb)[] _keys;

    public SpellParticleColourCurve(params (float Position, int Rgb)[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Length == 0) throw new ArgumentException("At least one colour key is required.", nameof(keys));
        _keys = ((float, int)[])keys.Clone();
        float previous = -1;
        foreach (var (position, _) in _keys)
        {
            if (!float.IsFinite(position) || position <= previous || position < 0 || position > 1)
                throw new ArgumentException("Colour keys must be strictly increasing and within [0, 1].", nameof(keys));
            previous = position;
        }
    }

    public int Sample(float position)
    {
        for (int i = 1; i < _keys.Length; i++)
        {
            var (start, from) = _keys[i - 1];
            var (end, to) = _keys[i];
            if (position >= end) continue;
            float mix = Math.Clamp((position - start) / (end - start), 0, 1);
            return Channel(16) << 16 | Channel(8) << 8 | Channel(0);

            int Channel(int shift)
            {
                int a = from >> shift & 255, b = to >> shift & 255;
                return (int)MathF.Round(a + (b - a) * mix);
            }
        }
        return _keys[^1].Rgb & 0xffffff;
    }

    internal Color Apply(Color colour, float position) => Color.FromArgb(colour.A << 24 | Sample(position));
}
