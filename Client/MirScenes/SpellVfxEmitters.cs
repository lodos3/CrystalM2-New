using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using CrystalM2.Rendering.Core;

namespace Client.MirScenes;

public static partial class SpellVfxAssets
{
    internal const string EmitterGroup = "Emitters";

    [ThreadStatic] private static string _emitter;
    [ThreadStatic] private static float? _stageProgress;
    [ThreadStatic] private static uint _particleIndex;
    [ThreadStatic] private static string _soloEmitter;
    [ThreadStatic] private static Dictionary<string, EmitterSample> _observedEmitters;

    internal sealed record ConfiguredEmitter(SpellEmitterSettings Settings, SpellParticleColourCurve Gradient, SpellParticleColourCurve SpeedGradient, int? Rgb);

    internal sealed class EmitterSample(SpellParticle particle)
    {
        public SpellParticle Particle { get; } = particle;
        public bool Persistent { get; private set; } = !particle.FrameOnly;
        public bool HasMotion { get; private set; } = particle.Motion.HasValue;
        public bool HasCollision { get; private set; } = particle.Impact.HasValue;
        public bool HasAnimatedSprite { get; private set; } = IsAnimated(particle.Shape);
        public bool HasRibbon { get; private set; } = particle.Ribbon.HasValue;
        public int MinimumSize { get; private set; } = particle.Size;
        public int MaximumSize { get; private set; } = particle.Size;
        private static bool IsAnimated(SpellParticleShape shape) => shape is
            SpellParticleShape.FlameSequence or SpellParticleShape.FireSequence or SpellParticleShape.SmokeSequence;
        public void Add(SpellParticle next)
        {
            Persistent |= !next.FrameOnly;
            HasMotion |= next.Motion.HasValue;
            HasCollision |= next.Impact.HasValue;
            HasRibbon |= next.Ribbon.HasValue;
            HasAnimatedSprite |= IsAnimated(next.Shape);
            MinimumSize = Math.Min(MinimumSize, next.Size);
            MaximumSize = Math.Max(MaximumSize, next.Size);
        }
    }

    internal static Dictionary<string, EmitterSample> CollectEmitters(Action sample)
    {
        var previous = _observedEmitters;
        var result = new Dictionary<string, EmitterSample>(StringComparer.Ordinal);
        _observedEmitters = result;
        try { sample(); }
        finally { _observedEmitters = previous; }
        return result;
    }

    internal static SoloScope SoloEmitter(string id) => new(id);

    internal readonly struct SoloScope : IDisposable
    {
        private readonly string _previous;
        internal SoloScope(string id)
        {
            _previous = _soloEmitter;
            _soloEmitter = id;
        }
        public void Dispose() => _soloEmitter = _previous;
    }

    internal static string EmitterPart(string part) =>
        _emitter != null && (_observedEmitters != null || _scoped?.Emitters != null || _soloEmitter != null)
            ? _emitter + "." + part : null;

    internal static EmitterScope UseEmitter(string id) => new(id);

    internal readonly struct EmitterScope : IDisposable
    {
        private readonly string _previous;
        internal EmitterScope(string id)
        {
            _previous = _emitter;
            if (id != null) _emitter = id;
        }
        public void Dispose() => _emitter = _previous;
    }

    internal static StageScope AtProgress(float progress) => new(progress);

    internal readonly struct StageScope : IDisposable
    {
        private readonly float? _previous;
        internal StageScope(float progress)
        {
            _previous = _stageProgress;
            _stageProgress = Math.Clamp(progress, 0, 1);
        }
        public void Dispose() => _stageProgress = _previous;
    }

    internal static void Emit(SpellParticlePool pool, SpellParticle particle, string id = null,
        bool schedule = false, long? sampleTime = null)
    {
        id ??= _emitter;
        var emitters = _scoped?.Emitters;
        uint index = emitters != null ? ++_particleIndex : 0;
        if (_soloEmitter != null && id != _soloEmitter) return;
        if (id != null)
        {
            if (_observedEmitters != null)
            {
                if (_observedEmitters.TryGetValue(id, out var observed)) observed.Add(particle);
                else _observedEmitters.Add(id, new EmitterSample(particle));
            }
            if (emitters?.TryGetValue(id, out var emitter) == true)
            {
                if (!TryApplyEmitter(pool, id, emitter, index, ref particle)) return;
                schedule |= !particle.FrameOnly && emitter.Settings.StartDelay > 0;
            }
        }

        if (sampleTime is { } now)
        {
            if (particle.Start > now) pool.Schedule(particle);
            else if (now - particle.Start < particle.Duration) pool.TryAdd(particle);
        }
        else if (schedule) pool.Schedule(particle);
        else pool.TryAdd(particle);
    }

    private static bool TryApplyEmitter(SpellParticlePool pool, string id, ConfiguredEmitter configured, uint index, ref SpellParticle particle)
    {
        var s = configured.Settings;
        if (!s.Enabled || (s.EmissionProbability is { } probability && Sample(index) >= probability) ||
            (s.MaxParticles is { } max && pool.CountEmitter(id, (int)_spell) >= max)) return false;

        float? stage = particle.FrameOnly ? _stageProgress : null;
        int duration = !particle.FrameOnly && s.StartLifetime is { } life
            ? Math.Max(1, (int)MathF.Round(life.Sample(Sample(index + 37)) * 1000)) : particle.Duration;
        int size = s.StartSize is { } sizeRange ? Math.Max(1, (int)MathF.Round(sizeRange.Sample(Sample(index + 71)))) : particle.Size;
        float rotation = s.StartRotation is { } angles ? angles.Sample(Sample(index + 113)) * (MathF.PI / 180) : particle.Rotation;
        int rgb = configured.Rgb ?? (particle.Colour.ToArgb() & 0xffffff);
        byte alpha = s.StartOpacity is { } opacity ? (byte)MathF.Round(opacity * 255) : particle.Colour.A;
        Vector2 offset = new(s.PositionX ?? 0, s.PositionY ?? 0);
        if (s.ShapeSpread.HasValue || s.SpawnShape != SpellEmitterSpawnShape.Authored)
            offset += SpawnOffset(s, index);
        Vector2 velocity = particle.Velocity;
        SpellParticleMotion? motion = particle.Motion;
        bool editVelocity = s.StartSpeed != null || s.VelocityMultiplier.HasValue || s.VelocityLimit.HasValue;
        bool editForce = s.ForceX.HasValue || s.ForceY.HasValue;
        if (editVelocity || editForce || s.GravityModifier.HasValue)
        {
            float seconds = duration * .001f;
            if (motion.HasValue || editForce || s.GravityModifier.HasValue)
            {
                var physical = motion ?? new SpellParticleMotion(velocity / seconds, Vector2.Zero, 0, 0, 0);
                motion = physical with
                {
                    Drift = editVelocity ? AdjustVelocity(physical.Drift, rotation, s, index) : physical.Drift,
                    Gravity = s.GravityModifier is { } gravity ? (physical.Gravity > 0 ? physical.Gravity : 520) * gravity : physical.Gravity,
                    Wind = physical.Wind + new Vector2(s.ForceX ?? 0, s.ForceY ?? 0)
                };
            }
            else velocity = AdjustVelocity(velocity / seconds, rotation, s, index) * seconds;
        }

        SpellParticleImpact? impact = s.Collision == false ? null : particle.Impact;
        if (impact is { } landing)
            impact = landing with { Count = s.SubEmitterCount ?? landing.Count, Shape = s.SubEmitterSprite ?? landing.Shape };
        bool billboard = s.RenderMode != SpellEmitterRenderMode.Authored;
        float ribbonScale = particle.Ribbon.HasValue && s.StartSize != null ? size / (float)particle.Size : 1;
        var ribbon = billboard ? null : particle.Ribbon?.Scaled(ribbonScale);
        particle = particle with
        {
            Origin = particle.Origin + offset,
            DepthY = particle.DepthY + offset.Y,
            Start = particle.Start + (particle.FrameOnly ? 0 : s.StartDelay ?? 0),
            Duration = duration, Size = size, Rotation = rotation, Velocity = velocity, Motion = motion, Impact = impact,
            Colour = Color.FromArgb(alpha << 24 | rgb),
            Shape = s.Sprite ?? particle.Shape, Layer = s.SortingLayer ?? particle.Layer,
            CastsLight = s.CastsLight ?? particle.CastsLight, LightEnergy = s.LightEnergy ?? particle.LightEnergy,
            AlwaysOnTop = s.AlwaysOnTop ?? particle.AlwaysOnTop, Frame = s.TextureFrame ?? particle.Frame,
            Ribbon = ribbon, Emitter = new SpellEmitterPlayback(id, (int)_spell, s, stage, configured.Gradient, configured.SpeedGradient)
        };
        return true;
    }

    private static Vector2 SpawnOffset(SpellEmitterSettings s, uint index)
    {
        float width = s.ShapeSpread ?? 0;
        float height = s.ShapeHeight ?? width;
        float x = Sample(index + 191), y = Sample(index + 229);
        float angle = x * MathF.Tau;
        Vector2 offset = s.SpawnShape switch
        {
            SpellEmitterSpawnShape.Rectangle => new Vector2((2 * x - 1) * width, (2 * y - 1) * height),
            SpellEmitterSpawnShape.Line => new Vector2((2 * x - 1) * width, 0),
            SpellEmitterSpawnShape.Ring => new Vector2(MathF.Cos(angle) * width, MathF.Sin(angle) * height),
            _ => new Vector2(MathF.Cos(angle) * width, MathF.Sin(angle) * height) * MathF.Sqrt(y)
        };
        return Vector2.Transform(offset, Matrix3x2.CreateRotation((s.ShapeRotation ?? 0) * (MathF.PI / 180)));
    }

    private static Vector2 AdjustVelocity(Vector2 velocity, float rotation, SpellEmitterSettings s, uint index)
    {
        float speed = velocity.Length();
        Vector2 direction = speed > .0001f ? velocity / speed : new(MathF.Cos(rotation), MathF.Sin(rotation));
        speed = (s.StartSpeed?.Sample(Sample(index + 307)) ?? speed) * (s.VelocityMultiplier ?? 1);
        return direction * Math.Min(speed, s.VelocityLimit ?? float.MaxValue);
    }

    private static float Sample(uint index)
    {
        index ^= index >> 16;
        index *= 0x7feb352d;
        index ^= index >> 15;
        index *= 0x846ca68b;
        return ((index ^ (index >> 16)) >> 8) / 16777216f;
    }

    private static Dictionary<string, ConfiguredEmitter> DecodeEmitters(JsonObject nodes)
    {
        if (nodes.Count > 2048) throw new JsonException("Too many particle emitters.");
        var result = new Dictionary<string, ConfiguredEmitter>(StringComparer.Ordinal);
        foreach (var (id, value) in nodes)
        {
            if (id.Length is < 1 or > 160 || !id.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-') ||
                value is not JsonObject)
                throw new JsonException($"Invalid emitter ID '{id}'.");
            var s = value.Deserialize<SpellEmitterSettings>(Options) ?? throw new JsonException($"Missing emitter settings for {id}.");
            CheckRange(s.StartLifetime, .001f, 120); CheckRange(s.StartSpeed, 0, 10000);
            CheckRange(s.StartSize, 1, 8192); CheckRange(s.StartRotation, -36000, 36000);
            Check(s.StartOpacity, 0, 1); Check(s.StartDelay, 0, 120000);
            Check(s.GravityModifier, 0, 20); Check(s.MaxParticles, 0, 4096); Check(s.EmissionProbability, 0, 1);
            Check(s.PositionX, -10000, 10000); Check(s.PositionY, -10000, 10000); Check(s.ShapeSpread, 0, 10000);
            Check(s.ShapeHeight, 0, 10000); Check(s.ShapeRotation, -36000, 36000);
            Check(s.VelocityMultiplier, 0, 20); Check(s.VelocityLimit, 0, 10000);
            Check(s.ForceX, -10000, 10000); Check(s.ForceY, -10000, 10000);
            Check(s.SubEmitterCount, 0, 8); Check(s.LightEnergy, 0, 20); Check(s.TextureFrame, 0, SpellParticleAtlas.ClipFrames - 1);
            Check(s.TextureCycles, 0, 120);
            CheckKeys(s.VelocityOverLifetime, 0, 20);
            CheckKeys(s.SizeOverLifetime, 0, 20); CheckKeys(s.OpacityOverLifetime, 0, 1); CheckKeys(s.RotationOverLifetime, -36000, 36000);
            CheckRange(s.SpeedRange, 0, 10000);
            CheckKeys(s.SizeBySpeed, 0, 20); CheckKeys(s.RotationBySpeed, -36000, 36000);
            result.Add(id, new ConfiguredEmitter(s, Gradient(s.ColourOverLifetime), Gradient(s.ColourBySpeed),
                s.StartColour == null ? null : VfxRgbConverter.Parse(s.StartColour)));
        }
        return result;

        static SpellParticleColourCurve Gradient(IReadOnlyList<ParticleGradientKey> stops)
        {
            if (stops == null) return null;
            if (stops.Count is < 1 or > 32 || stops.Any(stop => stop == null))
                throw new JsonException("A gradient needs 1 to 32 colour stops.");
            return new SpellParticleColourCurve(stops.Select(stop => (stop.Time, VfxRgbConverter.Parse(stop.Colour))).ToArray());
        }

        static void Check(float? value, float min, float max)
        {
            if (value is { } number && (!float.IsFinite(number) || number < min || number > max))
                throw new JsonException($"Emitter value must be between {min} and {max}.");
        }

        static void CheckRange(ParticleRange range, float min, float max)
        {
            if (range == null) return;
            Check(range.Min, min, max); Check(range.Max, range.Min, max);
        }

        static void CheckKeys(IReadOnlyList<ParticleKey> keys, float min, float max)
        {
            if (keys == null) return;
            if (keys.Count is < 1 or > 32) throw new JsonException("A curve needs 1 to 32 keys.");
            float previous = -1;
            foreach (var key in keys)
            {
                if (key == null || key.Time <= previous) throw new JsonException("Curve times must be strictly increasing.");
                Check(key.Time, 0, 1); Check(key.Value, min, max);
                previous = key.Time;
            }
        }
    }
}
