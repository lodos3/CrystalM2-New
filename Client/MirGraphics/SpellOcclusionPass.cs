namespace Client.MirGraphics;

internal static class SpellOcclusionPass
{
    private static Texture _mask;
    private static Surface _surface;

    internal static Texture Render(List<WorldSpriteCapture.Draw> scenery, List<WorldSpriteCapture.Draw> actors, int originRow)
    {
        if (scenery.Count == 0 && actors.Count == 0) return null;
        var device = DXManager.Device;
        if (_mask == null || _mask.Disposed || _mask.Width != device.TargetWidth || _mask.Height != device.TargetHeight)
        {
            DisposeAll();
            _mask = Texture.CreateSpellOcclusion(device, device.TargetWidth, device.TargetHeight);
            _surface = _mask.GetSurface();
        }

        Surface target = DXManager.CurrentSurface;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        Matrix transform = device.Sprite.Transform;
        float opacity = device.Opacity;
        try
        {
            DXManager.SetSurface(_surface);
            device.Clear(Color.Black);
            device.PixelShader = ShaderEffect.SpellOcclusion;
            device.Blend = SpriteBlendMode.Maximum;
            device.Opacity = 1;
            Draw(scenery);
            Draw(actors);
        }
        finally
        {
            DXManager.SetSurface(target);
            device.Sprite.Transform = transform;
            device.Blend = blend;
            device.PixelShader = shader;
            device.Opacity = opacity;
        }
        return _mask;

        void Draw(List<WorldSpriteCapture.Draw> sprites)
        {
            foreach (var sprite in sprites)
            {
                if (!sprite.Occludes || sprite.Row < 0) continue;
                device.Sprite.Transform = sprite.Transform;
                var position = new Vector3(sprite.Position.X, sprite.Position.Y, Math.Max(1, sprite.Row - originRow));
                device.Sprite.Draw(sprite.Texture, sprite.Source, position, sprite.Tint);
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
