using CrystalM2.Rendering.Core;

namespace Client.MirGraphics;

// Batch shapes that share a blur setting. Gaussian filtering happens in
// screen space after the original frame has been scaled around its centre.
internal static class ShapedLightPass
{
    private static readonly List<Shape> MapShapes = new(64);
    private static readonly List<Shape> SpellShapes = new(64);
    private static readonly List<Halo> MapHalos = new(64);
    private static readonly List<Halo> SpellHalos = new(64);
    private static readonly Scratch[] Buffers = [new(), new(), new(), new()];
    private static Texture _haloTexture;
    private static Surface _haloSurface;
    private static ParticleLight _particles;
    private readonly record struct Shape(Texture Texture, Rectangle Bounds, Vector4 Tint, ShaderEffect Shader, Vector4 Cone);
    private readonly record struct Halo(Rectangle Bounds, Vector4 Tint);
    private readonly record struct ParticleHalo(Vector2 Position, int Tint, float Energy);
    private readonly record struct ParticleLight(SpellParticlePool Pool, Vector2 Camera, long Time,
        float Intensity, float Radius, float VisualSize, float Warmth, Vector3? Colour, Rectangle Bounds);
    private readonly record struct Filter(Scratch Scratch, Rectangle Region, int Divisor, int Padding, int Blur);

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
        int halo = emitter.GlowRadius.Current + emitter.GlowBlur.Current;
        bounds.Inflate(Math.Max(halo, (int)MathF.Ceiling(size.Width * grow) + emitter.Blur.Current),
            Math.Max(halo, (int)MathF.Ceiling(size.Height * grow) + emitter.Blur.Current));
        return bounds.IntersectsWith(new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight));
    }

    internal static int BlurDivisor(int blur)
    {
        int divisor = 1;
        while (blur >= 8 * divisor && divisor < 8) divisor *= 2;
        return divisor;
    }

    internal static bool AddHalo(Point centre, Color tint, float intensity, GraphicsTuning.Emitter emitter, LightSource source)
    {
        int radius = emitter.GlowRadius.Current;
        float strength = intensity * emitter.GlowStrength.Current / 100f;
        if (radius == 0 || strength <= 0) return false;
        var bounds = new Rectangle(centre.X - radius, centre.Y - radius, radius * 2, radius * 2);
        if (!Rectangle.Inflate(bounds, emitter.GlowBlur.Current, emitter.GlowBlur.Current)
            .IntersectsWith(new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight))) return false;
        Vector4 colour = RendererDevice.ToVector(tint);
        colour.X *= strength;
        colour.Y *= strength;
        colour.Z *= strength;
        (source == LightSource.MapAnimation ? MapHalos : SpellHalos).Add(new Halo(bounds, colour));
        return true;
    }

    internal static void Add(LightSource source, Texture texture, Rectangle bounds, Color tint, float intensity)
    {
        Vector4 colour = RendererDevice.ToVector(tint);
        colour.X *= intensity;
        colour.Y *= intensity;
        colour.Z *= intensity;
        (source == LightSource.MapAnimation ? MapShapes : SpellShapes).Add(new Shape(texture, bounds, colour, ShaderEffect.ShapedLight, default));
    }

    internal static void AddProjection(LightSource source, Texture texture, Rectangle bounds, Vector4 colour, Vector4? cone) =>
        (source == LightSource.MapAnimation ? MapShapes : SpellShapes).Add(new Shape(texture, bounds, colour,
            cone.HasValue ? ShaderEffect.ConeLight : ShaderEffect.Artwork, cone.GetValueOrDefault()));

    internal static void AddParticles(SpellParticlePool pool, Vector2 camera, long now, float intensity,
        float radius, float visualSize, float warmth, Vector3? colour)
    {
        if (intensity <= 0) return;
        Rectangle bounds = Rectangle.Empty;
        var emitter = GraphicsTuning.Light(LightSource.Spell);
        int margin = Math.Max(emitter.Blur.Current, emitter.GlowRadius.Current + emitter.GlowBlur.Current);
        var screen = new Rectangle(-margin, -margin, Settings.ScreenWidth + margin * 2, Settings.ScreenHeight + margin * 2);
        Span<ParticleHalo> glowCentres = stackalloc ParticleHalo[12];
        int glowCount = 0;
        foreach (ref readonly SpellParticle particle in pool.Active)
        {
            if (!particle.CastsLight || particle.LightEnergy <= 0) continue;
            float age = particle.Age(now);
            Vector2 point = particle.Position(now) + camera;
            var light = particle.Bounds(point, particle.Dimensions(age, radius * (particle.Ground ? 1f : visualSize)), radius,
                particle.Angle(now));
            if (!light.IntersectsWith(screen)) continue;
            bounds = bounds.IsEmpty ? light : Rectangle.Union(bounds, light);

            // Sample one soft halo per nearby group of larger emitters. Their exact silhouettes
            // still contribute to the shaped pass, including all the smaller sparks.
            float energy = intensity * particle.LightEnergy * particle.Fade(age) * particle.Colour.A / 255f;
            if (energy <= .025f || particle.Size < 18) continue;
            int index = -1, weakest = 0;
            for (int i = 0; i < glowCount; i++)
            {
                if (Vector2.DistanceSquared(point, glowCentres[i].Position) < 64 * 64) { index = i; break; }
                if (glowCentres[i].Energy < glowCentres[weakest].Energy) weakest = i;
            }
            if (index < 0 && glowCount < glowCentres.Length)
                index = glowCount++;
            else
            {
                if (index < 0) index = weakest;
                if (energy <= glowCentres[index].Energy) continue;
            }
            Color tintAtAge = particle.ColourAt(age);
            Vector3 rgb = colour ?? new Vector3(tintAtAge.R, tintAtAge.G, tintAtAge.B) / 255f;
            rgb = Vector3.Clamp(rgb * new Vector3(1 + warmth, 1, 1 - warmth), Vector3.Zero, Vector3.One);
            Color tint = Color.FromArgb((int)(rgb.X * 255), (int)(rgb.Y * 255), (int)(rgb.Z * 255));
            glowCentres[index] = new ParticleHalo(point, tint.ToArgb(), energy);
        }
        foreach (ParticleHalo halo in glowCentres[..glowCount])
            AddHalo(new Point((int)halo.Position.X, (int)halo.Position.Y), Color.FromArgb(halo.Tint), halo.Energy, emitter, LightSource.Spell);
        if (emitter.Shape.Current != 1 && !bounds.IsEmpty) _particles = new(pool, camera, now, intensity, radius, visualSize, warmth, colour, bounds);
    }

    internal static void Reset()
    {
        MapShapes.Clear();
        SpellShapes.Clear();
        MapHalos.Clear();
        SpellHalos.Clear();
        _particles = default;
    }

    internal static void Composite()
    {
        if (MapShapes.Count == 0 && SpellShapes.Count == 0 && MapHalos.Count == 0 && SpellHalos.Count == 0 && _particles.Pool == null) return;
        var device = DXManager.Device;
        Surface target = DXManager.CurrentSurface;
        Matrix transform = device.Sprite.Transform;
        ShaderEffect shader = device.PixelShader;
        SpriteBlendMode blend = device.Blend;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        try
        {
            var map = GraphicsTuning.Light(LightSource.MapAnimation);
            var spell = GraphicsTuning.Light(LightSource.Spell);
            RenderHalos(MapHalos, map.GlowBlur.Current);
            RenderHalos(SpellHalos, spell.GlowBlur.Current);
            RenderGroup(MapShapes, map, false);
            RenderGroup(SpellShapes, spell, false);
            if (_particles.Pool != null)
                RenderGroup(Array.Empty<Shape>(), spell, true, GraphicsTuning.ParticleLightCeiling.Current / 100f);
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

        void RenderHalos(List<Halo> halos, int blur)
        {
            if (halos.Count == 0) return;
            if (blur != 0)
            {
                Rectangle region = halos[0].Bounds;
                for (int i = 1; i < halos.Count; i++) region = Rectangle.Union(region, halos[i].Bounds);
                Filter filter = PrepareFilter(region, blur);
                if (filter.Region.IsEmpty) return;
                device.Blend = SpriteBlendMode.Additive;
                device.PixelShader = ShaderEffect.LightHalo;
                foreach (var halo in halos) device.Ui.Fill(halo.Bounds, halo.Tint);
                FinishFilter(filter);
                return;
            }
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
            foreach (var halo in halos) device.Ui.Fill(halo.Bounds, halo.Tint);

            DXManager.SetSurface(target);
            device.Sprite.Transform = Matrix.Identity;
            device.PixelShader = ShaderEffect.GaussianLight;
            device.SetPixelShaderConstant(0, [Vector4.Zero, Vector4.Zero]);
            device.Sprite.Draw(_haloTexture, new Rectangle(0, 0, width, height),
                new Rectangle(0, 0, target.Texture.Width, target.Texture.Height), Vector4.One);
        }

        void RenderGroup(IReadOnlyList<Shape> shapes, GraphicsTuning.Emitter emitter, bool includeParticles, float ceiling = 0)
        {
            if (shapes.Count == 0 && !includeParticles) return;
            int blur = emitter.Blur.Current;
            device.Blend = SpriteBlendMode.Additive;
            device.Sprite.Transform = Matrix.Identity;
            if (blur == 0 && ceiling <= 0)
            {
                DrawShapes(shapes, emitter, 0, includeParticles);
                return;
            }
            Rectangle region = includeParticles ? _particles.Bounds : shapes[0].Bounds;
            foreach (var shape in shapes) region = Rectangle.Union(region, shape.Bounds);
            Filter filter = PrepareFilter(region, blur);
            if (filter.Region.IsEmpty) return;
            DrawShapes(shapes, emitter, filter.Divisor, includeParticles);
            FinishFilter(filter, ceiling);
        }

        Filter PrepareFilter(Rectangle region, int blur)
        {
            int divisor = BlurDivisor(blur);
            int tier = BitOperations.TrailingZeroCount((uint)divisor);
            Scratch scratch = Buffers[tier];
            int padding = divisor == 8 ? 192 : divisor * 8;
            scratch.Ensure(device, (target.Texture.Width + 2 * padding + divisor - 1) / divisor,
                (target.Texture.Height + 2 * padding + divisor - 1) / divisor);
            region.Inflate(blur + divisor, blur + divisor);
            region.Offset(padding, padding);
            region = Rectangle.Intersect(new Rectangle(0, 0, scratch.Colour.Width, scratch.Colour.Height),
                Rectangle.FromLTRB((int)MathF.Floor(region.Left / (float)divisor), (int)MathF.Floor(region.Top / (float)divisor),
                    (int)MathF.Ceiling(region.Right / (float)divisor), (int)MathF.Ceiling(region.Bottom / (float)divisor)));
            if (region.IsEmpty) return default;

            DXManager.SetSurface(scratch.ColourSurface);
            device.Clear(Color.Black);
            device.Sprite.Transform = Matrix.CreateTranslation(padding, padding, 0) *
                Matrix.CreateScale(1f / divisor, 1f / divisor, 1f);
            return new Filter(scratch, region, divisor, padding, blur);
        }

        void FinishFilter(Filter filter, float ceiling = 0)
        {
            var (scratch, region, divisor, padding, blur) = filter;
            float radius = blur / (float)divisor;
            float sigma = Math.Max(0.55f, radius * 0.45f);
            float gaussian = 0.5f / (sigma * sigma);
            device.Sprite.Transform = Matrix.Identity;
            device.PixelShader = ShaderEffect.GaussianLight;
            if (blur > 0)
            {
                DXManager.SetSurface(scratch.HorizontalSurface);
                device.Clear(Color.Black);
                device.Blend = SpriteBlendMode.Alpha;
                device.SetPixelShaderConstant(0, [new(1f / scratch.Colour.Width, 0, MathF.Ceiling(radius), gaussian), Vector4.Zero]);
                device.Sprite.Draw(scratch.Colour, region, region, Vector4.One);
            }

            DXManager.SetSurface(target);
            device.Blend = SpriteBlendMode.Additive;
            device.SetPixelShaderConstant(0, [new(0, 1f / scratch.Colour.Height, MathF.Ceiling(radius), gaussian), new(ceiling, 0, 0, 0)]);
            var destination = new Rectangle(region.X * divisor - padding, region.Y * divisor - padding,
                region.Width * divisor, region.Height * divisor);
            device.Sprite.Draw(blur > 0 ? scratch.Horizontal : scratch.Colour, region, destination, Vector4.One);
        }

        void DrawShapes(IReadOnlyList<Shape> shapes, GraphicsTuning.Emitter emitter, int divisor, bool includeParticles)
        {
            foreach (var shape in shapes)
            {
                device.PixelShader = shape.Shader;
                if (shape.Shader == ShaderEffect.ShapedLight)
                    device.SetPixelShaderConstant(0, [new(emitter.CustomColour.Current, divisor, 0, 0)]);
                else if (shape.Shader == ShaderEffect.ConeLight)
                    device.SetPixelShaderConstant(0, [shape.Cone]);
                device.Sprite.Draw(shape.Texture, new Rectangle(0, 0, shape.Texture.Width, shape.Texture.Height),
                    shape.Bounds, shape.Tint);
            }
            if (!includeParticles) return;
            var (pool, camera, now, intensity, radius, visualSize, warmth, colour, _) = _particles;
            var clip = new Rectangle(-emitter.Blur.Current, -emitter.Blur.Current,
                target.Texture.Width + emitter.Blur.Current * 2, target.Texture.Height + emitter.Blur.Current * 2);
            device.SpellParticles.Draw(pool.Active, camera, now, true, intensity, true, radius, warmth, clip, colour, lightRadius: radius);
            device.SpellParticles.Draw(pool.Active, camera, now, false, intensity, true, radius * visualSize, warmth, clip, colour, lightRadius: radius);
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
