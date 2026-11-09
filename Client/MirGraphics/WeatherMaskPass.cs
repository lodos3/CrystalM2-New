namespace Client.MirGraphics;

internal static class WeatherMaskPass
{
    private static Texture _mask;
    private static Surface _surface;
    private static Texture _softScenery;
    private static Surface _softSurface;

    internal static (Texture Mask, Texture SoftScenery) Render(List<WorldSpriteCapture.Draw> scenery,
        List<WorldSpriteCapture.Draw> actors, List<Rectangle> excludedGround, int tileFade, int sceneryFade)
    {
        var device = DXManager.Device;
        if (_mask == null || _mask.Disposed || _mask.Width != device.TargetWidth || _mask.Height != device.TargetHeight)
        {
            DisposeAll();
            _mask = Texture.CreateWeatherMask(device, device.TargetWidth, device.TargetHeight);
            _surface = _mask.GetSurface();
        }

        Surface world = DXManager.CurrentSurface;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        Matrix transform = device.Sprite.Transform;
        float opacity = device.Opacity;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        bool softenScenery = sceneryFade > 0 && (scenery.Count > 0 || actors.Count > 0);
        try
        {
            DXManager.SetSurface(_surface);
            device.Clear(Color.FromArgb(0, 0, 0, 0));
            device.PixelShader = ShaderEffect.WeatherOcclusion;
            device.Blend = SpriteBlendMode.Alpha;
            device.Opacity = 1;
            Draw(scenery);
            Draw(actors);

            // Alpha holds the silhouette. The terrain pass writes a hard red
            // mask and a rounded green distance ramp without changing alpha.
            device.PixelShader = ShaderEffect.WeatherTerrainMask;
            device.Blend = SpriteBlendMode.Maximum;
            device.Sprite.Transform = Matrix.Identity;
            device.SetPixelShaderConstant(0, [new Vector4(tileFade, 0, 0, 0)]);
            foreach (Rectangle bounds in excludedGround)
            {
                var centreAndExtent = new Vector4(bounds.Left + bounds.Width * .5f, bounds.Top + bounds.Height * .5f,
                    bounds.Width * .5f, bounds.Height * .5f);
                device.Ui.Fill(Rectangle.Inflate(bounds, tileFade, tileFade), centreAndExtent);
            }

            if (softenScenery)
            {
                // Filter just the silhouette once at quarter resolution. Weather quads reuse it.
                if (_softScenery == null)
                {
                    _softScenery = new Texture(device, (_mask.Width + 3) / 4, (_mask.Height + 3) / 4, true);
                    _softSurface = _softScenery.GetSurface();
                }
                DXManager.SetSurface(_softSurface);
                _mask.GenerateMips();
                device.PixelShader = ShaderEffect.WeatherSceneryBlur;
                device.Blend = SpriteBlendMode.Alpha;
                float spacing = sceneryFade * 0.46f;
                float mip = Math.Clamp(MathF.Log2(sceneryFade * 0.53f), 0, 3);
                device.SetPixelShaderConstant(0, [new Vector4(spacing / _mask.Width, spacing / _mask.Height, mip, 0)]);
                device.Sprite.Draw(_mask, new Rectangle(0, 0, _mask.Width, _mask.Height),
                    new Rectangle(0, 0, _softScenery.Width, _softScenery.Height), Vector4.One);
            }
        }
        finally
        {
            DXManager.SetSurface(world);
            device.PixelShader = shader;
            device.Blend = blend;
            device.Sprite.Transform = transform;
            device.Opacity = opacity;
            device.SetPixelShaderConstant(0, constants);
        }
        return (_mask, softenScenery ? _softScenery : null);

        void Draw(List<WorldSpriteCapture.Draw> sprites)
        {
            foreach (var sprite in sprites)
            {
                if (!sprite.Occludes) continue;
                device.Sprite.Transform = sprite.Transform;
                device.Sprite.Draw(sprite.Texture, sprite.Source, sprite.Position, sprite.Tint);
            }
        }
    }

    internal static void DisposeAll()
    {
        _surface?.Dispose();
        _mask?.Dispose();
        _softSurface?.Dispose();
        _softScenery?.Dispose();
        _surface = _softSurface = null;
        _mask = _softScenery = null;
    }
}
