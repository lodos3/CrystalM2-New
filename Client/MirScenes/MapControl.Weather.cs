using Client.MirGraphics;

namespace Client.MirScenes;

public sealed partial class MapControl
{
    private float _surfaceWetness, _surfaceSnow, _groundFog;
    private long _weatherTime = -1;

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
        DrawGroundWeather(floor, weatherOrigin, viewport, start, end, now, rain);
        if (rain || snow) DrawPrecipitation(weatherOrigin, viewport, start, end, now, rain, snow);
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
        float seconds = now % 600_000 / 1000f;
        float wind = GraphicsTuning.WeatherWind.Current / 100f;
        float seed = (Index & 1023) * 3.17f;
        device.SetPixelShaderConstant(0, [new(floor.X, floor.Y, wet, snow),
            new(weatherOrigin.X + seconds * wind * 8, weatherOrigin.Y + seconds * wind * 2, fog, seconds),
            new(seed, rain ? 1 : 0, 0, 0)]);
        try
        {
            for (int y = start.Y; y <= end.Y; y++)
            for (int x = start.X; x <= end.X; x++)
            {
                var bounds = new Rectangle(floor.X + x * CellWidth, floor.Y + y * CellHeight, CellWidth, CellHeight);
                if (!bounds.IntersectsWith(viewport) || IsStaticOccluder(x, y)) continue;
                var cell = M2CellInfo[x, y];
                if (cell.MiddleImage == 0 && cell.FrontImage == 0 && M2CellInfo[x & ~1, y & ~1].BackImage == 0)
                    continue;

                float wetResponse = 0.80f, snowResponse = 1;
                if (_mapMaterials.TrySample(x, y, out MaterialRegion material))
                {
                    wetResponse = material.WetResponse;
                    if (material.Material == MapMaterial.Water) wetResponse = snowResponse = 0;
                    else if (material.Material == MapMaterial.Foliage) snowResponse = 0.35f;
                }
                int edges = 0;
                if (fog > 0)
                {
                    if (IsStaticOccluder(x - 1, y)) edges |= 1;
                    if (IsStaticOccluder(x + 1, y)) edges |= 2;
                    if (IsStaticOccluder(x, y - 1)) edges |= 4;
                    if (IsStaticOccluder(x, y + 1)) edges |= 8;
                }
                device.Ui.Fill(bounds, Color.FromArgb(edges + 1, (int)(wetResponse * 255), (int)(snowResponse * 255), 255));
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
        Matrix original = DXManager.Sprite.Transform;
        using var artwork = DXManager.BlendArtwork(GraphicsLayer.Weather);
        try
        {
            for (int y = start.Y; y <= end.Y; y++)
            for (int x = start.X; x <= end.X; x++)
            {
                int left = origin.X + x * CellWidth, top = origin.Y + y * CellHeight;
                if (left < -CellWidth || left >= viewport.Right + CellWidth || top < -CellHeight ||
                    top > viewport.Bottom + 150 || IsStaticOccluder(x, y)) continue;
                uint seed = WeatherHash(unchecked((uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)Index * 83492791u));
                for (int i = 0; i < 2; i++)
                {
                    uint key = WeatherHash(seed + (uint)i * 251u);
                    if (rain) Drop(key, 650, 116, true);
                    if (snow) Drop(key ^ 0x5d124cd7u, 2300, 130, false);
                }

                void Drop(uint key, int lifetime, int height, bool streak)
                {
                    long clock = now + (key >> 16) % lifetime;
                    uint cycle = WeatherHash(key ^ (uint)(clock / lifetime));
                    if ((cycle & 255) >= 96 * density) return;
                    float age = clock % lifetime / (float)lifetime;
                    float xOffset = (int)(cycle >> 8 & 31) + (age - 0.5f) * wind * (streak ? 17 : 35);
                    float yOffset = (int)(cycle >> 13 & 15) - (1 - age) * height;
                    if (streak && age > 0.87f)
                    {
                        if (_surfaceWetness > 0.15f)
                            DXManager.Device.Ui.Fill(new Rectangle(left + (int)xOffset - 2, top + 8, 5, 1),
                                Color.FromArgb((int)((1 - age) * 450), 184, 215, 248));
                        return;
                    }
                    int size = streak ? 1 : 2 + (int)(cycle >> 19 & 1);
                    DXManager.Sprite.Transform = Matrix.CreateRotationZ(streak ? wind * 0.13f : 0) *
                        Matrix.CreateTranslation(left + xOffset, top + yOffset, 0) * original;
                    DXManager.Device.Ui.Fill(new Rectangle(0, 0, size, streak ? 10 : size),
                        streak ? Color.FromArgb(150, 172, 207, 247) : Color.FromArgb(219, 228, 240, 255));
                    DXManager.Sprite.Transform = original;
                }
            }
        }
        finally { DXManager.Sprite.Transform = original; }
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
