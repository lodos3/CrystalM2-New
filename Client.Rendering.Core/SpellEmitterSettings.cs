namespace CrystalM2.Rendering.Core;

public enum SpellEmitterSpawnShape { Authored, Disk, Ring, Rectangle, Line }

public enum SpellEmitterRenderMode { Authored, Billboard, QuadMesh, TriangleMesh }

public sealed record ParticleRange(float Min, float Max)
{
    public float Sample(float random) => Min + (Max - Min) * random;
}

public sealed record ParticleKey(float Time, float Value);

public sealed record ParticleGradientKey(float Time, string Colour);

public sealed record SpellEmitterSettings
{
    public bool Enabled { get; init; } = true;
    public ParticleRange? StartLifetime { get; init; }
    public ParticleRange? StartSpeed { get; init; }
    public ParticleRange? StartSize { get; init; }
    public ParticleRange? StartRotation { get; init; }
    public string? StartColour { get; init; }
    public float? StartOpacity { get; init; }
    public int? StartDelay { get; init; }
    public float? GravityModifier { get; init; }
    public int? MaxParticles { get; init; }
    public float? EmissionProbability { get; init; }
    public float? PositionX { get; init; }
    public float? PositionY { get; init; }
    public SpellEmitterSpawnShape SpawnShape { get; init; }
    public float? ShapeSpread { get; init; }
    public float? ShapeHeight { get; init; }
    public float? ShapeRotation { get; init; }
    public float? VelocityMultiplier { get; init; }
    public IReadOnlyList<ParticleKey>? VelocityOverLifetime { get; init; }
    public float? VelocityLimit { get; init; }
    public float? ForceX { get; init; }
    public float? ForceY { get; init; }
    public IReadOnlyList<ParticleGradientKey>? ColourOverLifetime { get; init; }
    public IReadOnlyList<ParticleKey>? SizeOverLifetime { get; init; }
    public IReadOnlyList<ParticleKey>? OpacityOverLifetime { get; init; }
    public IReadOnlyList<ParticleKey>? RotationOverLifetime { get; init; }
    public ParticleRange? SpeedRange { get; init; }
    public IReadOnlyList<ParticleGradientKey>? ColourBySpeed { get; init; }
    public IReadOnlyList<ParticleKey>? SizeBySpeed { get; init; }
    public IReadOnlyList<ParticleKey>? RotationBySpeed { get; init; }
    public bool? Collision { get; init; }
    public int? SubEmitterCount { get; init; }
    public SpellParticleShape? SubEmitterSprite { get; init; }
    public SpellParticleShape? Sprite { get; init; }
    public SpellParticleLayer? SortingLayer { get; init; }
    public bool? CastsLight { get; init; }
    public float? LightEnergy { get; init; }
    public bool? AlwaysOnTop { get; init; }
    public SpellEmitterRenderMode RenderMode { get; init; }
    public byte? TextureFrame { get; init; }
    public float? TextureCycles { get; init; }

    internal static float Evaluate(IReadOnlyList<ParticleKey>? keys, float t, float fallback)
    {
        if (keys == null || keys.Count == 0) return fallback;
        for (int i = 1; i < keys.Count; i++)
        {
            var a = keys[i - 1];
            var b = keys[i];
            if (t > b.Time) continue;
            return a.Value + (b.Value - a.Value) * Math.Clamp((t - a.Time) / (b.Time - a.Time), 0, 1);
        }
        return keys[^1].Value;
    }
    // Integrate a linear multiplier so velocity keys affect the path continuously.
    internal static float Integrate(IReadOnlyList<ParticleKey>? keys, float time)
    {
        if (keys == null || keys.Count == 0) return time;
        float t = Math.Clamp(time, 0, 1);
        float sum = Math.Min(t, keys[0].Time) * keys[0].Value;
        for (int i = 1; i < keys.Count && t > keys[i - 1].Time; i++)
        {
            var a = keys[i - 1];
            var b = keys[i];
            float width = Math.Min(t, b.Time) - a.Time;
            float end = a.Value + (b.Value - a.Value) * width / (b.Time - a.Time);
            sum += width * (a.Value + end) * .5f;
        }
        return sum + Math.Max(0, t - keys[^1].Time) * keys[^1].Value;
    }

}

public sealed record SpellEmitterPlayback(string Id, int Spell, SpellEmitterSettings Settings, float? StageProgress, SpellParticleColourCurve? Colour, SpellParticleColourCurve? SpeedColour);
