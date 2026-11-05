using Client.MirGraphics;

namespace Client.MirScenes;

public sealed partial class MapControl
{
    private float _surfaceWetness, _surfaceSnow, _groundFog;
    private long _weatherTime = -1;
    private readonly List<WorldSpriteCapture.Draw> _floorWeatherCutouts = new();
    private readonly List<WorldSpriteCapture.Draw> _scenerySprites = new();
    private readonly List<WorldSpriteCapture.Draw> _weatherActors = new();
    private bool _captureWeather;

    private bool LocalWeatherActive => GraphicsTuning.WeatherSource.Current == 0 &&
        (Weather != WeatherSetting.None || _surfaceWetness > 0.001f || _surfaceSnow > 0.001f || _groundFog > 0.001f);

    private void ResetLocalWeather()
    {
        _surfaceWetness = _surfaceSnow = _groundFog = 0;
        _weatherTime = -1;
    }

    internal void RefreshWeatherSource()
    {
        ResetLocalWeather();
        UpdateWeather();
        TextureValid = false;
    }

    private void DrawLocalWeather()
    {
        WeatherSetting weather = Weather;
        bool rain = (weather & WeatherSetting.Rain) != 0;
        bool snow = (weather & WeatherSetting.Snow) != 0;
        bool fog = (weather & WeatherSetting.Fog) != 0;
        long now = CMain.Time;
        float elapsed = _weatherTime < 0 ? 0 : Math.Clamp(now - _weatherTime, 0, 250) / 1000f;
        _weatherTime = now;
        _surfaceWetness = Approach(_surfaceWetness, rain, elapsed, 9f, 26f);
        _surfaceSnow = Approach(_surfaceSnow, snow, elapsed, 15f, rain ? 10f : 36f);
        _groundFog = Approach(_groundFog, fog, elapsed, 1.8f, 3f);

        Point floor = GraphicsTuning.Offset(GraphicsLayer.Ground).Apply(ToMouseLocation(Point.Empty));
        floor.X -= OffSetX; // Match DrawFloorTiles, including its legacy horizontal adjustment.
        Point weatherOrigin = GraphicsTuning.Offset(GraphicsLayer.Weather).Apply(floor, GraphicsTuning.Offset(GraphicsLayer.Ground).Linked);
        var viewport = new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight);
        Point start = new(Math.Max(0, User.Movement.X - ViewRangeX), Math.Max(1, User.Movement.Y - ViewRangeY));
        Point end = new(Math.Min(Width - 1, User.Movement.X + ViewRangeX), Math.Min(Height - 1, User.Movement.Y + ViewRangeY));
        var device = DXManager.Device;
        Texture previous = device.AuxiliaryTexture;
        device.AuxiliaryTexture = WeatherMaskPass.Render(_floorWeatherCutouts, _scenerySprites, _weatherActors);
        try
        {
            DrawGroundWeather(floor, weatherOrigin, viewport, start, end, now, rain);
            if (rain || snow) DrawPrecipitation(weatherOrigin, viewport, start, end, now, rain, snow);
        }
        finally { device.AuxiliaryTexture = previous; }
    }

    private static float Approach(float value, bool accumulating, float seconds, float fill, float drain) =>
        accumulating ? Math.Min(1, value + seconds / fill) : Math.Max(0, value - seconds / drain);

    private void DrawGroundWeather(Point floor, Point weatherOrigin, Rectangle viewport, Point start, Point end, long now, bool rain)
    {
        float wet = _surfaceWetness * GraphicsTuning.SurfaceWater.Current / 100f;
        float snow = _surfaceSnow * GraphicsTuning.SurfaceSnow.Current / 100f;
        float fog = _groundFog * GraphicsTuning.FogDensity.Current / 100f;
        if (wet <= 0.001f && snow <= 0.001f && fog <= 0.001f) return;

        RendererDevice device = DXManager.Device;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        float opacity = device.Opacity;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        device.PixelShader = ShaderEffect.WeatherGround;
        device.Blend = SpriteBlendMode.Alpha;
        device.Opacity = 1;
        float seconds = now / 1000f;
        float wind = GraphicsTuning.WeatherWind.Current / 100f;
        float seed = (Index & 1023) * 3.17f;
        device.SetPixelShaderConstant(0, [new(floor.X, floor.Y, wet, snow),
            new(weatherOrigin.X + seconds * wind * 8, weatherOrigin.Y + seconds * wind * 2, fog, seconds),
            new(seed, rain ? 1 : 0, 0, 0)]);
        try
        {
            if (_mapMaterials.Regions.Count == 0)
            {
                var terrain = new Rectangle(floor.X + start.X * CellWidth, floor.Y + start.Y * CellHeight,
                    (end.X - start.X + 1) * CellWidth, (end.Y - start.Y + 1) * CellHeight);
                device.Ui.Fill(Rectangle.Intersect(viewport, terrain), Color.FromArgb(255, 204, 255, 255));
                return;
            }

            for (int y = start.Y; y <= end.Y; y++)
            for (int x = start.X; x <= end.X; x++)
            {
                var bounds = new Rectangle(floor.X + x * CellWidth, floor.Y + y * CellHeight, CellWidth, CellHeight);
                if (!bounds.IntersectsWith(viewport)) continue;

                float wetResponse = 0.80f, snowResponse = 1;
                if (_mapMaterials.TrySample(x, y, out MaterialRegion material))
                {
                    wetResponse = material.WetResponse;
                    if (material.Material == MapMaterial.Water) wetResponse = snowResponse = 0;
                    else if (material.Material == MapMaterial.Foliage) snowResponse = 0.35f;
                }
                device.Ui.Fill(bounds, Color.FromArgb(255, (int)(wetResponse * 255), (int)(snowResponse * 255), 255));
            }
        }
        finally
        {
            device.Sprite.Flush();
            device.Opacity = opacity;
            device.Blend = blend;
            device.PixelShader = shader;
            device.SetPixelShaderConstant(0, constants);
        }
    }

    private void DrawPrecipitation(Point origin, Rectangle viewport, Point start, Point end, long now, bool rain, bool snow)
    {
        float density = GraphicsTuning.Precipitation.Current / 100f;
        if (density <= 0) return;
        float wind = GraphicsTuning.WeatherWind.Current / 100f;
        var device = DXManager.Device;
        Matrix original = device.Sprite.Transform;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        using var artwork = DXManager.BlendArtwork(GraphicsLayer.Weather);
        try
        {
            device.PixelShader = ShaderEffect.WeatherParticle;
            device.Blend = device.ArtworkBlend ?? SpriteBlendMode.Alpha;
            for (int y = start.Y; y <= end.Y; y++)
            for (int x = start.X; x <= end.X; x++)
            {
                int left = origin.X + x * CellWidth, top = origin.Y + y * CellHeight;
                if (left < -CellWidth || left >= viewport.Right + CellWidth || top < -CellHeight ||
                    top > viewport.Bottom + 190) continue;
                uint seed = WeatherHash(unchecked((uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)Index * 83492791u));
                float gust = MathF.Sin(x * 0.17f + y * 0.26f - now * 0.0009f);
                float amount = density * (0.85f + 0.4f * gust);
                for (int i = 0; i < 3; i++)
                {
                    uint key = WeatherHash(seed + (uint)i * 251u);
                    if (rain) Drop(key, true);
                    if (snow) Drop(key ^ 0x5d124cd7u, false);
                }

                void Drop(uint key, bool streak)
                {
                    int lifetime = streak ? 360 + (int)(key >> 5 & 511) : 1500 + (int)(key >> 5 & 2047);
                    long clock = now + (key >> 16);
                    uint cycle = WeatherHash(key ^ (uint)(clock / lifetime));
                    if ((cycle & 255) >= 61 * amount) return;
                    float age = clock % lifetime / (float)lifetime;
                    uint shape = WeatherHash(cycle + 16411);
                    float groundX = left + (cycle >> 8 & 255) * (CellWidth / 256f);
                    float groundY = top + (cycle >> 16 & 255) * (CellHeight / 256f);
                    float height = 72 + (shape >> 9 & 95);
                    float drift = (wind * (15 + gust * 14) + (int)(shape & 31) - 15) * (streak ? 1 : 1.65f);
                    int brightness = 100 + (int)(shape >> 24 & 127);
                    float fade = Math.Min(1, age * 9);
                    if (streak && age > 0.89f)
                    {
                        if (_surfaceWetness > 0.15f)
                        {
                            int span = (int)(4 + (age - 0.89f) * (28 + (shape >> 16 & 31)));
                            device.Ui.Fill(new Rectangle((int)groundX - span / 2, (int)groundY - 2, span, 4),
                                Color.FromArgb((int)((1 - age) * 740), 2, brightness, 0));
                        }
                        return;
                    }
                    float progress = streak ? age / 0.89f : age;
                    float sway = streak ? 0 : MathF.Sin((progress * 2 + (shape >> 16 & 63) / 20f) * MathF.PI) * (3 + wind * 3);
                    float angle = streak ? -MathF.Atan2(drift, height) : 0;
                    int width = streak ? 2 + (int)(shape >> 8 & 1) : 2 + (int)(shape >> 7 & 3);
                    int length = streak ? 5 + (int)(shape >> 16 & 15) : width;
                    device.Sprite.Transform = Matrix.CreateRotationZ(angle) *
                        Matrix.CreateTranslation(groundX - (1 - progress) * drift + sway, groundY - (1 - progress) * height, 0) * original;
                    device.Ui.Fill(new Rectangle(0, 0, width, length),
                        Color.FromArgb((int)(fade * (streak ? 80 + (shape >> 2 & 95) : 120 + (shape >> 3 & 111))),
                            streak ? 0 : 1, brightness, 0));
                    device.Sprite.Transform = original;
                }
            }
        }
        finally
        {
            device.PixelShader = shader;
            device.Blend = blend;
            device.Sprite.Transform = original;
        }
    }

    private static uint WeatherHash(uint n)
    {
        n ^= n >> 16;
        n *= 0x7feb352du;
        n ^= n >> 15;
        n *= 0x846ca68bu;
        return n ^ (n >> 16);
    }
}
