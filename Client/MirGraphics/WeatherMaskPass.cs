namespace Client.MirGraphics;

internal static class WeatherMaskPass
{
    private static Texture _mask;
    private static Surface _surface;

    internal static Texture Render(List<WorldSpriteCapture.Draw> floor, List<WorldSpriteCapture.Draw> scenery,
                                   List<WorldSpriteCapture.Draw> actors)
    {
        var device = DXManager.Device;
        if (_mask == null || _mask.Disposed || _mask.Width != device.TargetWidth || _mask.Height != device.TargetHeight)
        {
            DisposeAll();
            _mask = new Texture(device, device.TargetWidth, device.TargetHeight, true);
            _surface = _mask.GetSurface();
        }

        Surface world = DXManager.CurrentSurface;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        Matrix transform = device.Sprite.Transform;
        float opacity = device.Opacity;
        try
        {
            DXManager.SetSurface(_surface);
            device.Clear(Color.Transparent);
            device.PixelShader = ShaderEffect.WeatherOcclusion;
            device.Blend = SpriteBlendMode.Alpha;
            device.Opacity = 1;
            Draw(floor);
            Draw(scenery);
            Draw(actors);
        }
        finally
        {
            DXManager.SetSurface(world);
            device.PixelShader = shader;
            device.Blend = blend;
            device.Sprite.Transform = transform;
            device.Opacity = opacity;
        }
        return _mask;

        void Draw(List<WorldSpriteCapture.Draw> sprites)
        {
            foreach (var sprite in sprites)
            {
                if (sprite.Blend is SpriteBlendMode.Additive or SpriteBlendMode.InverseLight or SpriteBlendMode.Screen)
                    continue; // Emissive artwork does not occlude ground weather.
                device.Sprite.Transform = sprite.Transform;
                device.Sprite.Draw(sprite.Texture, sprite.Source, sprite.Position, sprite.Tint);
            }
        }
    }

    internal static void DisposeAll()
    {
        _surface?.Dispose();
        _mask?.Dispose();
        _surface = null;
        _mask = null;
    }
}
