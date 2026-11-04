using System;

namespace Client.MirGraphics
{
    // Frame values contain no textures, references, or runtime allocations.
    internal readonly struct AtmosphericWeatherFrame
    {
        internal readonly float Wetness;
        internal readonly float CloudCover;
        internal readonly float FogCover;
        internal readonly float WindPixelsPerSecondX;
        internal readonly float WindPixelsPerSecondY;
        internal readonly float LightningFlash;
        internal readonly float DriftX, DriftY;

        internal AtmosphericWeatherFrame(float wetness, float cloudCover, float fogCover,
                                         float windX, float windY, float lightning,
                                         float driftX, float driftY)
        {
            Wetness = wetness;
            CloudCover = cloudCover;
            FogCover = fogCover;
            WindPixelsPerSecondX = windX;
            WindPixelsPerSecondY = windY;
            LightningFlash = lightning;
            DriftX = driftX;
            DriftY = driftY;
        }
    }

    internal static class AtmosphereExposure
    {
        // Smooth overcast response on the existing world shader. Does not
        // require another ambient render target or modify light map metadata.
        internal static float Brightness(float cloudCover, float intensity)
        {
            float clouds = Math.Max(0f, Math.Min(1f, cloudCover));
            float strength = Math.Max(0f, Math.Min(1f, intensity));
            return 1.035f - 0.12f * clouds * strength;
        }
    }

    internal static class AtmosphereIntensity
    {
        internal static int ClampPercent(int requested)
        {
            return Math.Max(0, Math.Min(100, requested));
        }

        internal static float Scale(int requested)
        {
            return ClampPercent(requested) / 100f;
        }
    }

    internal static class WeatherParticleBudget
    {
        internal static int Clamp(int requested)
        {
            return Math.Max(64, Math.Min(1024, requested));
        }

        internal static bool CanSpawn(int current, int requestedLimit)
        {
            return current >= 0 && current < Clamp(requestedLimit);
        }
    }

    // Deterministic, interruption-safe environmental accumulation. Wetness
    // persists after rain stops; clouds and fog dissolve more quickly.
    internal sealed class AtmosphericWeatherController
    {
        private readonly SmoothScalarTransition _wetness = new SmoothScalarTransition(30000);
        private readonly SmoothScalarTransition _clouds = new SmoothScalarTransition(16000);
        private readonly SmoothScalarTransition _fog = new SmoothScalarTransition(14000);
        private long _lastTime;
        private bool _hasTime;
        private float _driftX, _driftY;

        internal void Reset()
        {
            _wetness.Reset();
            _clouds.Reset();
            _fog.Reset();
            _hasTime = false;
            _driftX = _driftY = 0f;
        }

        internal AtmosphericWeatherFrame Sample(WeatherSetting weather, long now,
                                                bool lightningEnabled, int mapSeed)
        {
            bool rain = (weather & WeatherSetting.Rain) != 0;
            bool snow = (weather & WeatherSetting.Snow) != 0;
            bool foggy = (weather & WeatherSetting.Fog) != 0;
            bool embers = (weather & (WeatherSetting.RedEmber |
                                      WeatherSetting.FireParticle)) != 0;

            float wetness = _wetness.Sample(rain ? 0.9f : 0, now);
            float cloudCover = _clouds.Sample(rain ? 0.72f :
                                              snow ? 0.55f :
                                              foggy ? 0.35f :
                                              embers ? 0.28f : 0, now);
            float fogCover = _fog.Sample(foggy ? 0.75f :
                                         rain ? 0.23f :
                                         snow ? 0.16f :
                                         embers ? 0.30f : 0, now);

            // Smooth wind field: variation occurs on 12s+ cycles. Rates are
            // deliberately modest; only the 64 KiB procedural cloud tile moves.
            float baseWind = rain ? 13f : snow ? 7f : foggy ? 4f : embers ? 10f : 0f;
            float windX = 0f, windY = 0f;
            if (baseWind > 0f)
            {
                double seconds = (now >= 0 ? now : 0) / 1000.0;
                double phase = mapSeed * 0.013;
                windX = baseWind * (0.85f + 0.15f * (float)Math.Sin(seconds / 19.0 + phase));
                windY = baseWind * (0.25f + 0.12f * (float)Math.Sin(seconds / 27.0 - phase));
            }
            float flash = rain ? LightningFlashAt(now, lightningEnabled, true, mapSeed) : 0;

            if (_hasTime)
            {
                // Cap one update to 250ms after a pause to prevent the fog
                // pattern from abruptly jumping across the whole screen.
                float dt = Math.Min(250L, Math.Max(0L, now - _lastTime)) / 1000f;
                _driftX = (_driftX + windX * dt) % 8192f;
                _driftY = (_driftY + windY * dt) % 8192f;
            }
            _lastTime = now;
            _hasTime = true;

            return new AtmosphericWeatherFrame(wetness, cloudCover, fogCover,
                                               windX, windY, flash, _driftX, _driftY);
        }

        // A rare, optional and deliberately restrained lightning exposure bump.
        // No strobing/white-screen flash, no extra lights, no RNG allocations.
        internal static float LightningFlashAt(long now, bool optIn,
                                               bool mapAllowsLightning, int mapSeed)
        {
            if (!optIn || !mapAllowsLightning || now < 0) return 0;
            const long interval = 35000;
            long phase = (now + (long)(uint)mapSeed * 7919L % interval) % interval;
            if (phase >= 150) return 0;
            float fraction = 1f - phase / 150f;
            return 0.12f * fraction * fraction;
        }
    }
}