namespace Client.MirGraphics;

// Batch shapes that share a blur setting. Gaussian filtering happens in
// screen space after the original frame has been scaled around its centre.
internal static class ShapedLightPass
{
    private const int Padding = 32;
    private static readonly List<Shape> MapShapes = new(64);
    private static readonly List<Shape> SpellShapes = new(64);
    private static readonly Scratch[] Buffers = [new(), new(), new()];
    private readonly record struct Shape(Texture Texture, Rectangle Bounds, Vector4 Tint);

    internal static Rectangle ScaleFrame(Point point, int width, int height, int sizePercent)
    {
        int scaledWidth = Math.Max(1, (int)MathF.Round(width * sizePercent / 100f));
        int scaledHeight = Math.Max(1, (int)MathF.Round(height * sizePercent / 100f));
        return new Rectangle(point.X + (width - scaledWidth) / 2, point.Y + (height - scaledHeight) / 2,
            scaledWidth, scaledHeight);
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
    }

    internal static void Composite()
    {
        if (MapShapes.Count == 0 && SpellShapes.Count == 0) return;
        var device = DXManager.Device;
        Surface target = DXManager.CurrentSurface;
        Matrix transform = device.Sprite.Transform;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        try
        {
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

        void RenderGroup(List<Shape> shapes, GraphicsTuning.Emitter emitter)
        {
            if (shapes.Count == 0) return;
            int blur = emitter.Blur.Current;
            device.Blend = SpriteBlendMode.Additive;
            device.PixelShader = ShaderEffect.ShapedLight;
            device.SetPixelShaderConstant(0, [new(emitter.CustomColour.Current, blur > 0 ? 1 : 0, 0, 0)]);
            device.Sprite.Transform = Matrix.Identity;
            if (blur == 0)
            {
                DrawShapes(shapes);
                return;
            }

            int tier = blur >= 16 ? 2 : blur >= 8 ? 1 : 0;
            int divisor = 1 << tier;
            Scratch scratch = Buffers[tier];
            scratch.Ensure(device, (target.Texture.Width + 2 * Padding + divisor - 1) / divisor,
                (target.Texture.Height + 2 * Padding + divisor - 1) / divisor);
            Rectangle region = shapes[0].Bounds;
            for (int i = 1; i < shapes.Count; i++) region = Rectangle.Union(region, shapes[i].Bounds);
            region.Inflate(blur + divisor, blur + divisor);
            region.Offset(Padding, Padding);
            region = Rectangle.Intersect(new Rectangle(0, 0, scratch.Colour.Width, scratch.Colour.Height),
                Rectangle.FromLTRB((int)MathF.Floor(region.Left / (float)divisor), (int)MathF.Floor(region.Top / (float)divisor),
                    (int)MathF.Ceiling(region.Right / (float)divisor), (int)MathF.Ceiling(region.Bottom / (float)divisor)));
            if (region.IsEmpty) return;

            DXManager.SetSurface(scratch.ColourSurface);
            device.Clear(Color.Black);
            device.Sprite.Transform = Matrix.CreateTranslation(Padding, Padding, 0) *
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
            var destination = new Rectangle(region.X * divisor - Padding, region.Y * divisor - Padding,
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
            Colour = new Texture(device, width, height, true);
            Horizontal = new Texture(device, width, height, true);
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
