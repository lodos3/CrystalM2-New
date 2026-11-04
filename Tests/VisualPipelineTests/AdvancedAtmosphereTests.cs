using System;
using System.Drawing;
using Client.MirGraphics;

internal static class AdvancedAtmosphereTests
{
    internal static void Run()
    {
        static void Check(bool value, string reason)
        {
            if (!value) throw new Exception(reason);
        }

        var climate = new AtmosphericWeatherController();
        var clear = climate.Sample(WeatherSetting.None, 0, false, 0);
        Check(clear.Wetness == 0 && clear.CloudCover == 0 && clear.LightningFlash == 0,
              "Clear weather must not introduce cloud shadows or rain reflections.");

        var rainStart = climate.Sample(WeatherSetting.Rain, 1000, false, 0);
        Check(rainStart.Wetness == 0 && rainStart.CloudCover == 0,
              "A sudden shower must not snap to full intensity.");
        var halfway = climate.Sample(WeatherSetting.Rain, 16000, false, 0);
        Check(halfway.Wetness > 0.2f && halfway.Wetness < 0.8f &&
              halfway.CloudCover > 0, "Clouds and wetness should accumulate smoothly.");
        Check(halfway.DriftX > rainStart.DriftX && halfway.DriftY >= rainStart.DriftY,
              "Cloud drift must accumulate continuously during rain.");
        var soaked = climate.Sample(WeatherSetting.Rain, 41000, false, 0);
        Check(soaked.Wetness > 0.8f && soaked.CloudCover > 0.3f,
              "Sustained rain must increase wetness and overcast.");
        var dryStart = climate.Sample(WeatherSetting.None, 42000, false, 0);
        Check(dryStart.Wetness > 0.8f, "Wet ground must not instantly dry.");
        var drying = climate.Sample(WeatherSetting.None, 70000, false, 0);
        Check(drying.Wetness < dryStart.Wetness, "Ground wetness must fade after rain.");

        var ashClimate = new AtmosphericWeatherController();
        ashClimate.Sample(WeatherSetting.None, 0, false, 0);
        var ash = ashClimate.Sample(WeatherSetting.RedEmber | WeatherSetting.FireParticle, 20000, false, 0);
        ash = ashClimate.Sample(WeatherSetting.RedEmber | WeatherSetting.FireParticle, 40000, false, 0);
        Check(ash.FogCover > 0 && ash.WindPixelsPerSecondX > 0,
              "Ash storms must produce warm drifting particulate haze.");
        Check(AtmosphereColourGrade.FogStrength(WeatherSetting.RedEmber) > 0,
              "Active ember weather must influence distant atmospheric haze.");
        Color warmNightFog = AtmosphereColourGrade.FogTint(WeatherSetting.Fog, 4);
        Check(warmNightFog.R > warmNightFog.B,
              "Golden ambient maps must retain warm fog even at night.");
        Color blueNight = AtmosphereColourGrade.Target(LightSetting.Night, WeatherSetting.None, 3);
        Color slateNight = AtmosphereColourGrade.Target(LightSetting.Night, WeatherSetting.None, 2);
        Check(blueNight.B - blueNight.R > slateNight.B - slateNight.R,
              "Map-specific sky-blue night must differ from slate night.");

        Color ashHaze = AtmosphereColourGrade.FogTint(WeatherSetting.RedEmber);
        Color snowHaze = AtmosphereColourGrade.FogTint(WeatherSetting.Snow);
        Check(ashHaze.R > ashHaze.B && snowHaze.B > snowHaze.R,
              "Haze tint must follow smoky ash and cold snow rather than stay uniformly blue.");

        Color emberTint = AtmosphereColourGrade.Target(LightSetting.Day, WeatherSetting.RedEmber, 0);
        Check(emberTint.R > emberTint.B, "Ember skies must grade warm without changing map data.");
        Color violet = AtmosphereColourGrade.Target(LightSetting.Day, WeatherSetting.PurpleLeaves, 0);
        Check(violet.B > violet.G, "Purple foliage weather needs a subtle violet atmosphere.");

        var snow = new AtmosphericWeatherController().Sample(WeatherSetting.Snow, 0, false, 0);
        Check(snow.Wetness == 0, "Snow must not automatically be treated as liquid rain.");
        Check(AtmosphericWeatherController.LightningFlashAt(35000, false, true, 0) == 0,
              "Lightning must require explicit user opt-in.");
        Check(AtmosphericWeatherController.LightningFlashAt(35000, true, false, 0) == 0,
              "Lightning must require an actual lightning-enabled weather map.");
        float flash = AtmosphericWeatherController.LightningFlashAt(35000, true, true, 0);
        Check(flash > 0 && flash <= 0.15f, "Atmospheric lightning must be subtle and rate-limited.");
        Check(AtmosphericWeatherController.LightningFlashAt(36000, true, true, 0) == 0,
              "Lightning pulse cannot persist for a full second.");

        float sample = AtmosphereTileNoise.Sample(11, 24, 128, 128, 17);
        Check(sample == AtmosphereTileNoise.Sample(139, 24, 128, 128, 17) &&
              sample == AtmosphereTileNoise.Sample(11, 152, 128, 128, 17),
              "Procedural noise must tile without a seam.");
        Check(sample >= 0 && sample <= 1, "Noise must remain normalised.");
        byte[] rgba = new byte[32 * 32 * 4];
        AtmosphereTileNoise.WriteBgra(rgba, 32, 32, 128, 3);
        Check(rgba[3] > 0 && rgba[3] < 255 && rgba[2] == 255,
              "Generated atmosphere should have bounded alpha and white colour channels.");
        try
        {
            AtmosphereTileNoise.WriteBgra(new byte[15], 32, 32, 128, 3);
            throw new Exception("Undersized texture destination should throw.");
        }
        catch (ArgumentException) { }

        Check(Math.Abs(AtmosphereExposure.Brightness(0f, 1f) - 1.035f) < 0.0001f,
              "Clear skies must preserve the original brightness.");
        Check(AtmosphereExposure.Brightness(1f, 1f) < 0.95f,
              "Fully overcast weather should subtly darken the world.");
        Check(Math.Abs(AtmosphereExposure.Brightness(1f, 0f) - 1.035f) < 0.0001f,
              "Zero atmosphere intensity must not darken the map.");

        Check(AtmosphereIntensity.ClampPercent(-100) == 0 &&
              AtmosphereIntensity.ClampPercent(300) == 100 &&
              AtmosphereIntensity.Scale(0) == 0f &&
              AtmosphereIntensity.Scale(50) == 0.5f &&
              AtmosphereIntensity.Scale(100) == 1f,
              "Environmental quality intensity must be bounded and reversible.");

        Check(WeatherParticleBudget.Clamp(10) == 64 &&
              WeatherParticleBudget.Clamp(5000) == 1024,
              "Environmental particle settings must have safe hardware bounds.");
        Check(WeatherParticleBudget.CanSpawn(255, 256) &&
              !WeatherParticleBudget.CanSpawn(256, 256) &&
              !WeatherParticleBudget.CanSpawn(5000, 256),
              "Weather emitters must not allocate unbounded particles.");

        Check(AtmosphereOverlayLayout.TryCountTiles(1920, 1080, 512, out int count) &&
              count > 0 && count <= AtmosphereOverlayLayout.MaxTilesPerLayer,
              "Common resolutions must fit inside fog draw budget.");
        Check(!AtmosphereOverlayLayout.TryCountTiles(8192, 8192, 384, out _),
              "Huge windows must disable unbounded texture tiling.");
        Console.WriteLine("Advanced atmosphere, wetness, cloud/fog, lightning and tiling checks passed.");
    }
}
