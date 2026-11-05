namespace Client.MirGraphics;

// Batch shapes that share a blur setting. Gaussian filtering happens in
// screen space after the original frame has been scaled around its centre.
internal static class ShapedLightPass
{
    private static readonly List<Shape> MapShapes = new(64);
    private static readonly List<Shape> SpellShapes = new(64);
    private static readonly List<Halo> Halos = new(128);
    private static readonly Scratch[] Buffers = [new(), new(), new(), new()];
    private static Texture _haloTexture;
    private static Surface _haloSurface;
    private readonly record struct Shape(Texture Texture, Rectangle Bounds, Vector4 Tint);
    private readonly record struct Halo(Rectangle Bounds, Vector4 Tint);

    internal static Rectangle ScaleFrame(Point point, int width, int height, int sizePercent, PointF pivot)
    {
        int scaledWidth = Math.Max(1, (int)MathF.Round(width * sizePercent / 100f));
        int scaledHeight = Math.Max(1, (int)MathF.Round(height * sizePercent / 100f));
        return new Rectangle(point.X + (int)MathF.Round(pivot.X * (1 - scaledWidth / (float)width)),
            point.Y + (int)MathF.Round(pivot.Y * (1 - scaledHeight / (float)height)),
            scaledWidth, scaledHeight);
    }

    internal static bool PotentiallyVisible(Point point, Size size, GraphicsTuning.Emitter emitter)
    {
        var bounds = new Rectangle(point, size);
        float grow = Math.Max(0, emitter.Radius.Current / 100f - 1);
        bounds.Inflate(Math.Max(emitter.GlowRadius.Current, (int)MathF.Ceiling(size.Width * grow) + emitter.Blur.Current),
            Math.Max(emitter.GlowRadius.Current, (int)MathF.Ceiling(size.Height * grow) + emitter.Blur.Current));
        return bounds.IntersectsWith(new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight));
    }

    internal static int BlurDivisor(int blur)
    {
        int divisor = 1;
        while (blur >= 8 * divisor && divisor < 8) divisor *= 2;
        return divisor;
    }

    internal static bool AddHalo(Point centre, Color tint, float intensity, GraphicsTuning.Emitter emitter)
    {
        int radius = emitter.GlowRadius.Current;
        float strength = intensity * emitter.GlowStrength.Current / 100f;
        if (radius == 0 || strength <= 0) return false;
        var bounds = new Rectangle(centre.X - radius, centre.Y - radius, radius * 2, radius * 2);
        if (!bounds.IntersectsWith(new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight))) return false;
        Vector4 colour = RendererDevice.ToVector(tint);
        colour.X *= strength;
        colour.Y *= strength;
        colour.Z *= strength;
        Halos.Add(new Halo(bounds, colour));
        return true;
    }

    internal static void Add(LightSource source, Texture texture, Rectangle bounds, Color tint, float intensity)
    {
        Vector4 colour = RendererDevice.ToVector(tint);
        colour.X *= intensity;
        colour.Y *= intensity;
        colour.Z *= intensity;
        (source == LightSource.MapAnimation ? MapShapes : SpellShapes).Add(new Shape(texture, bounds, colour));
    }

    internal static void Reset()
    {
        MapShapes.Clear();
        SpellShapes.Clear();
        Halos.Clear();
    }

    internal static void Composite()
    {
        if (MapShapes.Count == 0 && SpellShapes.Count == 0 && Halos.Count == 0) return;
        var device = DXManager.Device;
        Surface target = DXManager.CurrentSurface;
        Matrix transform = device.Sprite.Transform;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        try
        {
            RenderHalos();
            RenderGroup(MapShapes, GraphicsTuning.Light(LightSource.MapAnimation));
            RenderGroup(SpellShapes, GraphicsTuning.Light(LightSource.Spell));
        }
        finally
        {
            DXManager.SetSurface(target);
            device.Sprite.Transform = transform;
            device.PixelShader = shader;
            device.Blend = blend;
            device.SetPixelShaderConstant(0, constants);
            Reset();
        }

        void RenderHalos()
        {
            if (Halos.Count == 0) return;
            int divisor = (long)target.Texture.Width * target.Texture.Height > 4_000_000 ? 8 : 4;
            int width = (target.Texture.Width + divisor - 1) / divisor;
            int height = (target.Texture.Height + divisor - 1) / divisor;
            if (_haloTexture == null || _haloTexture.Disposed || _haloTexture.Width != width || _haloTexture.Height != height)
            {
                _haloSurface?.Dispose();
                _haloTexture?.Dispose();
                _haloTexture = new Texture(device, width, height, true, hdr: true);
                _haloSurface = _haloTexture.GetSurface();
            }
            DXManager.SetSurface(_haloSurface);
            device.Clear(Color.Black);
            device.Blend = SpriteBlendMode.Additive;
            device.PixelShader = ShaderEffect.LightHalo;
            device.Sprite.Transform = Matrix.CreateScale(width / (float)target.Texture.Width, height / (float)target.Texture.Height, 1);
            foreach (var halo in Halos) device.Ui.Fill(halo.Bounds, halo.Tint);

            DXManager.SetSurface(target);
            device.Sprite.Transform = Matrix.Identity;
            device.PixelShader = ShaderEffect.GaussianLight;
            device.SetPixelShaderConstant(0, [Vector4.Zero]);
            device.Sprite.Draw(_haloTexture, new Rectangle(0, 0, width, height),
                new Rectangle(0, 0, target.Texture.Width, target.Texture.Height), Vector4.One);
        }

        void RenderGroup(List<Shape> shapes, GraphicsTuning.Emitter emitter)
        {
            if (shapes.Count == 0) return;
            int blur = emitter.Blur.Current;
            int divisor = BlurDivisor(blur);
            device.Blend = SpriteBlendMode.Additive;
            device.PixelShader = ShaderEffect.ShapedLight;
            device.SetPixelShaderConstant(0, [new(emitter.CustomColour.Current, blur > 0 ? divisor : 0, 0, 0)]);
            device.Sprite.Transform = Matrix.Identity;
            if (blur == 0)
            {
                DrawShapes(shapes);
                return;
            }

            int tier = BitOperations.TrailingZeroCount((uint)divisor);
            Scratch scratch = Buffers[tier];
            int padding = divisor == 8 ? 192 : divisor * 8;
            scratch.Ensure(device, (target.Texture.Width + 2 * padding + divisor - 1) / divisor,
                (target.Texture.Height + 2 * padding + divisor - 1) / divisor);
            Rectangle region = shapes[0].Bounds;
            for (int i = 1; i < shapes.Count; i++) region = Rectangle.Union(region, shapes[i].Bounds);
            region.Inflate(blur + divisor, blur + divisor);
            region.Offset(padding, padding);
            region = Rectangle.Intersect(new Rectangle(0, 0, scratch.Colour.Width, scratch.Colour.Height),
                Rectangle.FromLTRB((int)MathF.Floor(region.Left / (float)divisor), (int)MathF.Floor(region.Top / (float)divisor),
                    (int)MathF.Ceiling(region.Right / (float)divisor), (int)MathF.Ceiling(region.Bottom / (float)divisor)));
            if (region.IsEmpty) return;

            DXManager.SetSurface(scratch.ColourSurface);
            device.Clear(Color.Black);
            device.Sprite.Transform = Matrix.CreateTranslation(padding, padding, 0) *
                Matrix.CreateScale(1f / divisor, 1f / divisor, 1f);
            DrawShapes(shapes);

            float radius = blur / (float)divisor;
            float sigma = Math.Max(0.55f, radius * 0.45f);
            float gaussian = 0.5f / (sigma * sigma);
            DXManager.SetSurface(scratch.HorizontalSurface);
            device.Clear(Color.Black);
            device.Sprite.Transform = Matrix.Identity;
            device.PixelShader = ShaderEffect.GaussianLight;
            device.Blend = SpriteBlendMode.Alpha;
            device.SetPixelShaderConstant(0, [new(1f / scratch.Colour.Width, 0, MathF.Ceiling(radius), gaussian)]);
            device.Sprite.Draw(scratch.Colour, region, region, Vector4.One);

            DXManager.SetSurface(target);
            device.Blend = SpriteBlendMode.Additive;
            device.SetPixelShaderConstant(0, [new(0, 1f / scratch.Colour.Height, MathF.Ceiling(radius), gaussian)]);
            var destination = new Rectangle(region.X * divisor - padding, region.Y * divisor - padding,
                region.Width * divisor, region.Height * divisor);
            device.Sprite.Draw(scratch.Horizontal, region, destination, Vector4.One);
        }

        void DrawShapes(List<Shape> shapes)
        {
            foreach (var shape in shapes)
                device.Sprite.Draw(shape.Texture, new Rectangle(0, 0, shape.Texture.Width, shape.Texture.Height),
                    shape.Bounds, shape.Tint);
        }
    }

    internal static void DisposeAll()
    {
        Reset();
        _haloSurface?.Dispose();
        _haloTexture?.Dispose();
        _haloSurface = null;
        _haloTexture = null;
        foreach (var scratch in Buffers) scratch.Dispose();
    }

    private sealed class Scratch : IDisposable
    {
        public Texture Colour, Horizontal;
        public Surface ColourSurface, HorizontalSurface;

        public void Ensure(RendererDevice device, int width, int height)
        {
            if (Colour is { Disposed: false } && Horizontal is { Disposed: false } && Colour.Width == width && Colour.Height == height)
                return;
            Dispose();
            Colour = new Texture(device, width, height, true, hdr: true);
            Horizontal = new Texture(device, width, height, true, hdr: true);
            ColourSurface = Colour.GetSurface();
            HorizontalSurface = Horizontal.GetSurface();
        }

        public void Dispose()
        {
            ColourSurface?.Dispose();
            HorizontalSurface?.Dispose();
            Colour?.Dispose();
            Horizontal?.Dispose();
            ColourSurface = HorizontalSurface = null;
            Colour = Horizontal = null;
        }
    }
}
