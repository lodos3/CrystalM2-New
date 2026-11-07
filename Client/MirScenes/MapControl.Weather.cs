using Client.MirGraphics;

namespace Client.MirScenes;

public sealed partial class MapControl
{
    private float _surfaceWetness, _surfaceSnow, _groundFog;
    private long _weatherTime = -1;
    private readonly List<WorldSpriteCapture.Draw> _scenerySprites = new();
    private readonly List<WorldSpriteCapture.Draw> _weatherActors = new();
    private readonly List<Rectangle> _excludedWeatherGround = new();
    private readonly Dictionary<(int Start, int End), int> _excludedWeatherRuns = new();
    private bool _captureWeather;

    private bool MapWeatherActive => (Weather & (WeatherSetting.Rain | WeatherSetting.Snow | WeatherSetting.Fog)) != 0 ||
        _surfaceWetness > 0.001f || _surfaceSnow > 0.001f || _groundFog > 0.001f || GraphicsTuning.WeatherMaskView != 0;

    private void ResetMapWeather()
    {
        _surfaceWetness = _surfaceSnow = _groundFog = 0;
        _weatherTime = -1;
    }

    internal void RefreshWeatherSource()
    {
        ResetMapWeather();
        UpdateWeather();
        TextureValid = false;
    }

    private void DrawMapWeather(bool serverWeather)
    {
        WeatherSetting weather = Weather;
        bool rain = (weather & WeatherSetting.Rain) != 0;
        bool snow = (weather & WeatherSetting.Snow) != 0;
        bool fog = (weather & WeatherSetting.Fog) != 0 && (!serverWeather || Settings.EnvironmentFogLayers);
        long now = CMain.Time;
        float elapsed = _weatherTime < 0 ? 0 : Math.Clamp(now - _weatherTime, 0, 250) / 1000f;
        _weatherTime = now;
        bool wetSurfaces = !serverWeather || (Settings.EnvironmentAtmosphere && Settings.AtmosphereWetSurfaces);
        _surfaceWetness = Approach(_surfaceWetness, rain && wetSurfaces, elapsed, 9f, 26f);
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
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        var edgeMode = (WeatherEdgeMode)GraphicsTuning.WeatherEdgeStyle.Current;
        int sceneryFade = edgeMode == WeatherEdgeMode.Silhouette ? 0 : GraphicsTuning.WeatherSceneryFade.Current;
        int tileFade = GraphicsTuning.WeatherTileFade.Current;
        float breakup = edgeMode == WeatherEdgeMode.Natural ? GraphicsTuning.WeatherEdgeBreakup.Current / 100f : 0;
        CollectWeatherExclusions(floor, Rectangle.Inflate(viewport, tileFade, tileFade));
        var (mask, softScenery) = WeatherMaskPass.Render(_scenerySprites, _weatherActors,
            _excludedWeatherGround, tileFade, sceneryFade);
        try
        {
            device.AuxiliaryTexture = mask;
            float sceneryStrength = softScenery == null ? 0 : Math.Min(1, sceneryFade / 4f);
            device.SetPixelShaderConstant(3, [new(floor.X, floor.Y, sceneryStrength, (Index & 1023) * 3.17f),
                new(breakup, GraphicsTuning.WeatherDropFade.Current / 100f, GraphicsTuning.WeatherMaskView, 0)]);
            DrawGroundWeather(floor, weatherOrigin, viewport, start, end, now, softScenery);
            if (rain || snow) DrawPrecipitation(weatherOrigin, viewport, now, rain, snow, softScenery);
            if (GraphicsTuning.WeatherMaskView != 0) DrawWeatherMaskView(viewport, softScenery);
        }
        finally
        {
            device.AuxiliaryTexture = previous;
            device.SetPixelShaderConstant(0, constants);
        }
    }

    private static float Approach(float value, bool accumulating, float seconds, float fill, float drain) =>
        accumulating ? Math.Min(1, value + seconds / fill) : Math.Max(0, value - seconds / drain);

    private bool AllowsWeather(int x, int y) => (uint)x < (uint)Width && y > 0 && y < Height &&
        M2CellInfo[x, y] is { AllowsWeather: true };

    private void CollectWeatherExclusions(Point floor, Rectangle viewport)
    {
        _excludedWeatherGround.Clear();
        _excludedWeatherRuns.Clear();
        int left = (viewport.Left - floor.X) / CellWidth - 1;
        int right = (viewport.Right - floor.X) / CellWidth + 1;
        int top = (viewport.Top - floor.Y) / CellHeight - 1;
        int bottom = (viewport.Bottom - floor.Y) / CellHeight + 1;
        for (int y = top; y <= bottom; y++)
        {
            int x = left;
            while (x <= right)
            {
                if (AllowsWeather(x, y))
                {
                    x++;
                    continue;
                }

                int start = x++;
                while (x <= right && !AllowsWeather(x, y)) x++;
                int rowTop = floor.Y + y * CellHeight;
                var run = (start, x);
                if (_excludedWeatherRuns.TryGetValue(run, out int index) && _excludedWeatherGround[index].Bottom == rowTop)
                {
                    Rectangle bounds = _excludedWeatherGround[index];
                    bounds.Height += CellHeight;
                    _excludedWeatherGround[index] = bounds;
                    continue;
                }

                _excludedWeatherRuns[run] = _excludedWeatherGround.Count;
                _excludedWeatherGround.Add(new Rectangle(floor.X + start * CellWidth, rowTop, (x - start) * CellWidth, CellHeight));
            }
        }
    }

    private void DrawGroundWeather(Point floor, Point weatherOrigin, Rectangle viewport, Point start, Point end, long now,
        Texture softScenery)
    {
        float wet = _surfaceWetness * GraphicsTuning.SurfaceWater.Current / 100f;
        float snow = _surfaceSnow * GraphicsTuning.SurfaceSnow.Current / 100f;
        float fog = _groundFog * GraphicsTuning.FogDensity.Current / 100f;
        if (wet <= 0.001f && snow <= 0.001f && fog <= 0.001f) return;

        RendererDevice device = DXManager.Device;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        float opacity = device.Opacity;
        device.PixelShader = ShaderEffect.WeatherGround;
        device.Blend = SpriteBlendMode.Alpha;
        device.Opacity = 1;
        float seconds = now / 1000f;
        float wind = GraphicsTuning.WeatherWind.Current / 100f;
        float seed = (Index & 1023) * 3.17f;
        device.SetPixelShaderConstant(0, [new(floor.X, floor.Y, wet, snow),
            new(weatherOrigin.X + seconds * wind * 8, weatherOrigin.Y + seconds * wind * 2, fog, 0),
            new(seed, 0, 0, 0)]);
        try
        {
            if (_mapMaterials.Regions.Count == 0)
            {
                var terrain = new Rectangle(floor.X + start.X * CellWidth, floor.Y + start.Y * CellHeight,
                    (end.X - start.X + 1) * CellWidth, (end.Y - start.Y + 1) * CellHeight);
                DrawWeatherQuad(softScenery, Rectangle.Intersect(viewport, terrain), Color.FromArgb(255, 204, 255, 255));
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
                DrawWeatherQuad(softScenery, bounds, Color.FromArgb(255, (int)(wetResponse * 255), (int)(snowResponse * 255), 255));
            }
        }
        finally
        {
            device.Sprite.Flush();
            device.Opacity = opacity;
            device.Blend = blend;
            device.PixelShader = shader;
        }
    }

    private void DrawPrecipitation(Point origin, Rectangle viewport, long now, bool rain, bool snow, Texture softScenery)
    {
        float density = GraphicsTuning.Precipitation.Current / 100f;
        if (density <= 0) return;
        float wind = GraphicsTuning.WeatherWind.Current / 100f;
        var device = DXManager.Device;
        Matrix original = device.Sprite.Transform;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        using var artwork = DXManager.BlendArtwork(GraphicsLayer.Weather);
        // A patch has several independent emitters. Their world positions, lifetimes and gaps
        // are independent of the map grid and the camera.
        const int patchWidth = 128, patchHeight = 112, margin = 192;
        int leftPatch = Math.Max(0, (viewport.Left - origin.X - margin) / patchWidth);
        int rightPatch = Math.Min((Width * CellWidth - 1) / patchWidth, (viewport.Right - origin.X + margin) / patchWidth);
        int topPatch = Math.Max(0, (viewport.Top - origin.Y) / patchHeight);
        int bottomPatch = Math.Min((Height * CellHeight - 1) / patchHeight, (viewport.Bottom - origin.Y + margin) / patchHeight);
        try
        {
            device.PixelShader = ShaderEffect.WeatherParticle;
            device.Blend = device.ArtworkBlend ?? SpriteBlendMode.Alpha;
            for (int y = topPatch; y <= bottomPatch; y++)
            for (int x = leftPatch; x <= rightPatch; x++)
            {
                uint seed = WeatherHash(unchecked((uint)x * 73856093u ^ (uint)y * 19349663u ^ (uint)Index * 83492791u));
                float gust = (seed >> 24) / 127.5f - 1;
                float amount = density * (0.82f + 0.38f * gust);
                for (int i = 0; i < 6; i++)
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
                    if ((cycle & 255) >= (streak ? 61 : 84) * amount) return;
                    float worldX = x * patchWidth + (cycle >> 8 & 255) * (patchWidth / 256f);
                    float worldY = y * patchHeight + (cycle >> 16 & 255) * (patchHeight / 256f);
                    if (!AllowsWeather((int)worldX / CellWidth, (int)worldY / CellHeight)) return;

                    float age = clock % lifetime / (float)lifetime;
                    uint shape = WeatherHash(cycle + 16411);
                    float groundX = origin.X + worldX;
                    float groundY = origin.Y + worldY;
                    float height = 72 + (shape >> 9 & 95);
                    float drift = (wind * (15 + gust * 14) + (int)(shape & 31) - 15) * (streak ? 1 : 1.65f);
                    int brightness = 100 + (int)(shape >> 24 & 127);
                    float fade = Math.Min(1, age * 9);
                    const float impact = 0.86f;
                    if (streak && age > impact)
                    {
                        if (_surfaceWetness > 0.15f)
                        {
                            int span = (int)(4 + (age - impact) * (28 + (shape >> 16 & 31)));
                            DrawWeatherQuad(softScenery, new Rectangle((int)groundX - span / 2, (int)groundY - 2, span, 4),
                                Color.FromArgb((int)((1 - age) * 740), 2, brightness, 0));
                        }
                        return;
                    }
                    float progress = streak ? age / impact : age;
                    float sway = streak ? 0 : MathF.Sin((progress * 2 + (shape >> 16 & 63) / 20f) * MathF.PI) * (3 + wind * 3);
                    float angle = streak ? -MathF.Atan2(drift, height) : 0;
                    int width = streak ? 1 + (int)(shape >> 8 & 3) : 2 + (int)(shape >> 7 & 3);
                    int length = streak ? 4 + (int)(shape >> 16 & 23) : width;
                    device.Sprite.Transform = Matrix.CreateRotationZ(angle) *
                        Matrix.CreateTranslation(groundX - (1 - progress) * drift + sway, groundY - (1 - progress) * height, 0) * original;
                    DrawWeatherQuad(softScenery, new Rectangle(0, 0, width, length),
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

    private static void DrawWeatherMaskView(Rectangle viewport, Texture softScenery)
    {
        var device = DXManager.Device;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        try
        {
            device.PixelShader = ShaderEffect.WeatherMaskView;
            device.Blend = SpriteBlendMode.Alpha;
            DrawWeatherQuad(softScenery, viewport, Color.White);
        }
        finally
        {
            device.Blend = blend;
            device.PixelShader = shader;
        }
    }

    private static void DrawWeatherQuad(Texture softScenery, Rectangle bounds, Color tint)
    {
        var device = DXManager.Device;
        if (softScenery == null) device.Ui.Fill(bounds, tint);
        else device.Sprite.Draw(softScenery, new Rectangle(0, 0, softScenery.Width, softScenery.Height),
            bounds, RendererDevice.ToVector(tint));
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
