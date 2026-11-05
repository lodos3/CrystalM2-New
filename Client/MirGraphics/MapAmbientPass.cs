namespace Client.MirGraphics;

// Sample the unlit map at low resolution, then reduce it to a smoothed
// screen average on the GPU. Actors and weather never enter the sample.
internal static class MapAmbientPass
{
    private static readonly Size SampleSize = new(48, 27);
    private static Texture _terrain;
    private static Surface _terrainSurface;
    private static readonly Texture[] Averages = new Texture[2];
    private static readonly Surface[] AverageSurfaces = new Surface[2];
    private static long _nextSample;
    private static int _current;
    private static bool _hasAverage;

    internal static Texture Average => _hasAverage && Averages[_current] is { Disposed: false } sample ? sample : null;

    internal static bool CaptureFloor(Texture world, long now)
    {
        if (Average != null && now < _nextSample) return false;
        var device = DXManager.Device;
        if (_terrain == null || _terrain.Disposed)
        {
            DisposeAll();
            _terrain = new Texture(device, SampleSize.Width, SampleSize.Height, true);
            _terrainSurface = _terrain.GetSurface();
            for (int i = 0; i < Averages.Length; i++)
            {
                Averages[i] = new Texture(device, 1, 1, true);
                AverageSurfaces[i] = Averages[i].GetSurface();
            }
        }

        Surface target = DXManager.CurrentSurface;
        var shader = device.PixelShader;
        var blend = device.Blend;
        Matrix transform = device.Sprite.Transform;
        try
        {
            DXManager.SetSurface(_terrainSurface);
            device.PixelShader = ShaderEffect.MapColourDownsample;
            device.Blend = SpriteBlendMode.Alpha;
            device.Sprite.Transform = Matrix.Identity;
            device.Sprite.Draw(world, new Rectangle(0, 0, world.Width, world.Height),
                new Rectangle(Point.Empty, SampleSize), Vector4.One);
        }
        finally
        {
            DXManager.SetSurface(target);
            device.PixelShader = shader;
            device.Blend = blend;
            device.Sprite.Transform = transform;
        }
        return true;
    }

    internal static void Finish(List<WorldSpriteCapture.Draw> scenery, long now)
    {
        var device = DXManager.Device;
        Surface target = DXManager.CurrentSurface;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        SpriteBlendMode? artworkBlend = device.ArtworkBlend;
        Texture previousAuxiliary = device.AuxiliaryTexture;
        Matrix transform = device.Sprite.Transform;
        float opacity = device.Opacity;
        TextureFilterMode filter = device.TextureFilter;
        Vector3 adjustment = device.TextureAdjustment;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        try
        {
            var scale = Matrix.CreateScale((float)SampleSize.Width / device.TargetWidth,
                (float)SampleSize.Height / device.TargetHeight, 1);
            DXManager.SetSurface(_terrainSurface);
            device.PixelShader = ShaderEffect.Artwork;
            device.ArtworkBlend = null;
            device.Opacity = 1;
            device.TextureFilter = TextureFilterMode.Smooth;
            device.TextureAdjustment = new Vector3(0.08f, 1, 0);
            foreach (var sprite in scenery)
            {
                device.Blend = sprite.Blend;
                device.Sprite.Transform = sprite.Transform * scale;
                device.Sprite.Draw(sprite.Texture, sprite.Source, sprite.Position, sprite.Tint);
            }

            int next = _current ^ 1;
            DXManager.SetSurface(AverageSurfaces[next]);
            device.PixelShader = ShaderEffect.MapColourAverage;
            device.AuxiliaryTexture = Average;
            device.Blend = SpriteBlendMode.Alpha;
            device.Sprite.Transform = Matrix.Identity;
            device.SetPixelShaderConstant(0, [new(Average == null ? 1 : 0.35f, 0, 0, 0)]);
            device.Sprite.Draw(_terrain, new Rectangle(Point.Empty, SampleSize), new Rectangle(0, 0, 1, 1), Vector4.One);
            device.Sprite.Flush();
            _current = next;
            _hasAverage = true;
            _nextSample = now + 180;
        }
        finally
        {
            DXManager.SetSurface(target);
            device.PixelShader = shader;
            device.Blend = blend;
            device.ArtworkBlend = artworkBlend;
            device.AuxiliaryTexture = previousAuxiliary;
            device.Sprite.Transform = transform;
            device.Opacity = opacity;
            device.TextureFilter = filter;
            device.TextureAdjustment = adjustment;
            device.SetPixelShaderConstant(0, constants);
        }
    }

    internal static void Reset()
    {
        _hasAverage = false;
        _nextSample = 0;
    }

    internal static void DisposeAll()
    {
        Reset();
        _terrainSurface?.Dispose();
        _terrain?.Dispose();
        _terrainSurface = null;
        _terrain = null;
        for (int i = 0; i < Averages.Length; i++)
        {
            AverageSurfaces[i]?.Dispose();
            Averages[i]?.Dispose();
            AverageSurfaces[i] = null;
            Averages[i] = null;
        }
    }
}
