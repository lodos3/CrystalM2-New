using Client.Rendering;
using CrystalM2.Rendering.Core;
using System.Numerics;
using Vortice.Direct3D;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Real Microsoft D3D11 software device, using production source and
        // shaders. These small pixel inputs are test data, not game artwork.
        using var window = new Form { ClientSize = new Size(64, 64) };
        using var device = new RendererDevice(window.Handle, 64, 64, DriverType.Warp);
        using Surface backBuffer = device.GetBackBuffer();
        using var red = Upload(device, Color.Red);
        using var blue = Upload(device, Color.Blue);
        using var translucent = Upload(device, Color.FromArgb(128, 180, 80, 20));
        using var dim = Upload(device, Color.FromArgb(255, 20, 40, 60));
        using var intermediate = new Texture(device, 8, 8, true);
        using var surface = intermediate.GetSurface();

        device.SetRenderTarget(surface);
        device.Clear(Color.Transparent);
        device.Sprite.Begin();
        device.Sprite.Draw(translucent, null, new Vector3(1, 1, 0), Vector4.One);
        device.Sprite.End();
        device.SetRenderTarget(backBuffer);
        device.Clear(Color.Black);
        device.Sprite.Draw(intermediate, null, Vector3.Zero, Vector4.One);
        device.Sprite.Draw(translucent, null, new Vector3(12, 1, 0), Vector4.One);
        device.Sprite.End();
        using (var image = device.Capture(backBuffer))
        {
            Near(image.GetPixel(1, 1), image.GetPixel(12, 1), "Cached alpha must match direct alpha.");
            Near(Color.FromArgb(255, 90, 40, 10), image.GetPixel(1, 1), "Straight BGRA must blend once.");
            Near(Color.Black, image.GetPixel(0, 0), "Source offsets must leave surrounding pixels unchanged.");
        }

        device.BeginFrame();
        device.Clear(Color.Black);
        device.Sprite.Begin();
        device.Sprite.Draw(red, null, Vector3.Zero, Vector4.One);
        device.Sprite.Draw(red, null, new Vector3(4, 0, 0), Vector4.One);
        device.Sprite.Draw(blue, null, Vector3.Zero, Vector4.One);
        device.Sprite.Draw(red, null, new Vector3(1, 0, 0), Vector4.One);
        device.Sprite.End();
        Check(device.DrawCalls == 3, "Only adjacent texture runs may combine.");
        using (var image = device.Capture(backBuffer))
        {
            Near(Color.Blue, image.GetPixel(0, 0), "Later blue layer must cover the first red layer.");
            Near(Color.Red, image.GetPixel(1, 0), "Final red layer must retain painter's order.");
        }

        device.Present(false);
        device.BeginFrame();
        device.Clear(Color.Black);
        device.Sprite.Draw(blue, null, new Vector3(8, 8, 0), Vector4.One);
        using (var image = device.Capture(backBuffer))
            Near(Color.Blue, image.GetPixel(8, 8), "Backbuffer must rebind after flip-model Present.");

        device.SetRenderTarget(surface);
        device.Clear(Color.Black); // No emitted light, independent of ambient.
        device.SetRenderTarget(backBuffer);
        device.Clear(Color.White);
        device.Blend = SpriteBlendMode.Multiply;
        device.PixelShader = ShaderEffect.LightComposite;
        device.SetPixelShaderConstant(0, new[] { new Vector4(0.2f, 0.2f, 0.2f, 1) });
        device.Sprite.Draw(intermediate, null, Vector3.Zero, Vector4.One);
        device.Sprite.End();
        using (var image = device.Capture(backBuffer))
            Near(Color.FromArgb(255, 51, 51, 51), image.GetPixel(0, 0),
                "Ambient must be combined during light composition, not baked into the emission buffer.");
        device.PixelShader = ShaderEffect.BloomExtract;
        device.Blend = SpriteBlendMode.Alpha;
        device.SetPixelShaderConstant(0, new[] { new Vector4(0.58f, 3, 0, 1) });
        device.Clear(Color.Black);
        device.Sprite.Draw(intermediate, null, Vector3.Zero, Vector4.One);
        device.Sprite.End();
        using (var image = device.Capture(backBuffer))
            Near(Color.Black, image.GetPixel(0, 0), "Ambient darkness must never create bloom.");

        device.PixelShader = ShaderEffect.BloomBlur;
        device.SetPixelShaderConstant(0, new[]
        {
            new Vector4(0.5f, 0, 0, 0), new Vector4(1, 0, 0, 0)
        });
        device.Clear(Color.Black);
        device.Sprite.Draw(dim, null, Vector3.Zero, Vector4.One);
        device.Sprite.End();
        using (var image = device.Capture(backBuffer))
            Near(Color.FromArgb(255, 20, 40, 60), image.GetPixel(0, 0),
                "Normalised Gaussian weights must preserve constant light intensity.");

        using (var mask = new Texture(device, 2, 2))
        {
            var upload = mask.BeginUpload();
            for (int row = 0; row < 2; row++)
                upload.Data.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 255 });
            mask.EndUpload();
            device.PixelShader = ShaderEffect.ShadowMask;
            device.Clear(Color.White);
            device.Sprite.Transform = Matrix4x4.CreateScale(4);
            device.Sprite.Draw(mask, null, Vector3.Zero, Vector4.One);
            device.Sprite.End();
            using var image = device.Capture(backBuffer);
            Color edge = image.GetPixel(3, 0);
            Check(edge.R > 100 && edge.R < 240 && edge.R == edge.G && edge.G == edge.B,
                "Repaired masks must use smooth alpha edges, without tinting source artwork.");
            device.Sprite.Transform = Matrix4x4.Identity;
        }
        device.PixelShader = ShaderEffect.ProjectedShadow;
        device.SetPixelShaderConstant(0, [new Vector4(0.5f, 0.5f, 0, 0)]);
        var paddedShadow = new Rectangle(-1, -1, 4, 4);
        device.Clear(Color.White);
        device.Sprite.Draw(red, paddedShadow, new Vector3(7, 7, 0), new Vector4(0, 0, 0, 0.5f));
        byte coreRed;
        using (var image = device.Capture(backBuffer))
        {
            Color core = image.GetPixel(8, 8), edge = image.GetPixel(7, 8);
            coreRed = core.R;
            Check(core.R < edge.R && edge.R < 255 && edge.R == edge.G && edge.R == edge.B,
                "A softened shadow must feather beyond the original sprite silhouette.");
            Near(Color.White, image.GetPixel(6, 8), "The blur must stay within the padded shadow quad.");
        }
        device.Clear(Color.White);
        device.Sprite.Draw(red, paddedShadow, new Vector3(7, 7, 0), new Vector4(0, 0, 0, 0.25f));
        using (var image = device.Capture(backBuffer))
            Check(image.GetPixel(8, 8).R > coreRed && image.GetPixel(8, 8).R < 255,
                "Lower shadow opacity must darken the scene less.");

        device.Clear(Color.Black);
        device.PixelShader = ShaderEffect.ShapedLight;
        device.Blend = SpriteBlendMode.Additive;
        device.SetPixelShaderConstant(0, [new Vector4(0.5f, 0.5f, 0.75f, 0)]);
        device.Sprite.Draw(red, paddedShadow, new Vector3(7, 7, 0), Vector4.One);
        using (var image = device.Capture(backBuffer))
        {
            Color centre = image.GetPixel(8, 8), edge = image.GetPixel(7, 8);
            Check(centre.R > edge.R && edge.R > 0 && edge.G == 0 && edge.B == 0,
                "Shaped emission must follow the source frame and spread its colour beyond the edge.");
            Near(Color.Black, image.GetPixel(6, 8), "A shaped light must leave unrelated pixels dark.");
        }
        device.Blend = SpriteBlendMode.Alpha;
        device.PixelShader = ShaderEffect.Artwork;
        TestArtworkStyles(device, backBuffer, translucent);
        TestGroundWeather(device, backBuffer);
        TestSpellArtwork(device, backBuffer);
        TestConeProjection(device, backBuffer, red);

        long bytes = device.TextureBytes;
        int textures = device.TextureCount;
        for (int i = 0; i < 32; i++)
        {
            using var temporary = Upload(device, Color.White);
            device.Sprite.Draw(temporary, null, Vector3.Zero, Vector4.One);
        }
        Check(device.TextureBytes == bytes && device.TextureCount == textures,
            "Disposed uploads must leave no registered texture memory.");
        device.BeginFrame();
        device.Clear(Color.Black);
        device.Ui.Draw(UiSurface.Window, new Rectangle(0, 0, 64, 56));
        using (var image = device.Capture(backBuffer))
            Near(CrystalM2.Rendering.Core.DarkFantasyPalette.Accent, image.GetPixel(9, 6),
                "Nine-slice window ornaments must retain their original pixels.");
        Check(device.DrawCalls == 1, "All nine compatible atlas patches should share one draw.");
        bytes = device.TextureBytes;
        textures = device.TextureCount;
        for (int i = 0; i < 32; i++)
            device.Ui.Draw(UiSurface.Button, new Rectangle(2, 2, 45, 24), UiVisualState.Hover);
        device.Sprite.End();
        Check(device.TextureCount == textures && device.TextureBytes == bytes,
            "Resizing or hovering generated controls must not allocate per-control textures.");
        device.BeginFrame();
        device.Clear(Color.Black);
        var particles = new SpellParticlePool(512);
        particles.Burst(new Vector2(160, 96), Vector2.Zero, Color.Orange, 100, 20);
        textures = device.TextureCount;
        bytes = device.TextureBytes;
        var savedBlend = device.Blend;
        var savedShader = device.PixelShader;
        var camera = new Vector2(-128, -64);
        device.SpellParticles.Draw(particles.Active, camera, 100, ground: false);
        using (var image = device.Capture(backBuffer))
            Check(image.GetPixel(32, 32).R > 0 && image.GetPixel(0, 0).R == 0,
                "Pooled particles must use the world camera projection.");
        Check(device.Blend == savedBlend && device.PixelShader == savedShader && device.DrawCalls == 1 &&
            device.TextureCount == textures + 1 &&
            device.TextureBytes == bytes + SpellParticleAtlas.Width * SpellParticleAtlas.Height * 4,
            "Particles must restore render state and batch into a single shared atlas texture.");
        device.SpellParticles.Draw(particles.Active, camera, 100, ground: false);
        device.Sprite.End();
        Check(device.TextureCount == textures + 1, "Subsequent particle draws must reuse the atlas.");
        device.Resize(80, 60);
        using Surface resized = device.GetBackBuffer();
        device.Clear(Color.Black);
        device.Sprite.Draw(red, null, Vector3.Zero, Vector4.One);
        using (var image = device.Capture(resized))
        {
            Check(image.Size == new Size(80, 60), "Readback must use resized dimensions and row pitch.");
            Near(Color.Red, image.GetPixel(0, 0), "Existing textures must survive swapchain resize.");
        }
        Console.WriteLine("D3D11 shader, alpha, layer order, batching, flip, upload disposal and resize checks passed (WARP).");
    }

    private static void TestGroundWeather(RendererDevice device, Surface target)
    {
        device.PixelShader = ShaderEffect.WeatherGround;
        device.Clear(Color.Black);
        device.SetPixelShaderConstant(0, [new(0, 0, 0, 1), new(0, 0, 1, 0), Vector4.Zero]);
        device.Ui.Fill(new Rectangle(0, 0, 48, 32), Color.FromArgb(3, 255, 255, 255));
        using var first = device.Capture(target);
        device.Clear(Color.Black);
        device.SetPixelShaderConstant(0, [new(10, 6, 0, 1), new(10, 6, 1, 0), Vector4.Zero]);
        device.Ui.Fill(new Rectangle(10, 6, 48, 32), Color.FromArgb(3, 255, 255, 255));
        using var shifted = device.Capture(target);
        Near(first.GetPixel(20, 16), shifted.GetPixel(30, 22), "Accumulation and fog must remain anchored through camera motion.");
        Check(shifted.GetPixel(30, 22).B > 16, "Snow and fog should visibly affect the floor.");
        Near(Color.Black, shifted.GetPixel(58, 22), "A skipped obstacle tile must receive no weather coverage.");
        device.PixelShader = ShaderEffect.Artwork;
    }

    private static void TestSpellArtwork(RendererDevice device, Surface target)
    {
        using var spell = new Texture(device, 2, 2);
        var upload = spell.BeginUpload();
        for (int y = 0; y < 2; y++)
            upload.Data.Write(new byte[] { 0, 0, 0, 255, 16, 48, 152, 255 });
        spell.EndUpload();
        Color terrain = Color.FromArgb(25, 65, 50);
        device.Clear(terrain);
        device.PixelShader = ShaderEffect.SpellArtwork;
        device.SetPixelShaderConstant(0, [new(0.05f, 0.18f, 1, 1), new(1, 0, 0.25f, 0.45f)]);
        device.Sprite.Draw(spell, null, Vector3.Zero, Vector4.One);
        using var beforeGlow = device.Capture(target);
        Near(terrain, beforeGlow.GetPixel(0, 0), "A black spell backing must leave the terrain untouched.");
        Near(Color.FromArgb(152, 48, 16), beforeGlow.GetPixel(1, 0), "The keyed base pass must retain the bright spell.");
        device.ArtworkBlend = SpriteBlendMode.Screen;
        device.SetPixelShaderConstant(1, [new(1, 1, 0.25f, 0.45f)]);
        device.Sprite.Draw(spell, null, Vector3.Zero, Vector4.One);
        using var withGlow = device.Capture(target);
        Near(terrain, withGlow.GetPixel(0, 0), "The second pass must not restore the black backing.");
        Check(withGlow.GetPixel(1, 0).R > beforeGlow.GetPixel(1, 0).R + 15, "The glow pass must boost the spell independently.");
        device.ArtworkBlend = null;
        device.PixelShader = ShaderEffect.Artwork;
    }

    private static void TestConeProjection(RendererDevice device, Surface target, Texture red)
    {
        device.Clear(Color.Black);
        device.PixelShader = ShaderEffect.ConeLight;
        device.Blend = SpriteBlendMode.Additive;
        device.SetPixelShaderConstant(0, [new(1, 0, 0.8f, 0.92f)]);
        device.Sprite.Draw(red, new Rectangle(0, 0, 2, 2), new Rectangle(0, 0, 32, 32), Vector4.One);
        using var image = device.Capture(target);
        Near(Color.Red, image.GetPixel(25, 16), "An east-facing cone must illuminate its forward ray.");
        Near(Color.Black, image.GetPixel(7, 16), "A cone must mask its rear hemisphere.");
        device.Blend = SpriteBlendMode.Alpha;
        device.PixelShader = ShaderEffect.Artwork;
    }

    private static void TestArtworkStyles(RendererDevice device, Surface target, Texture translucent)
    {
        var background = Color.FromArgb(100, 100, 100);
        (SpriteBlendMode Mode, Color Expected)[] modes =
        [
            (SpriteBlendMode.Alpha, Color.FromArgb(140, 90, 60)),
            (SpriteBlendMode.Additive, Color.FromArgb(190, 140, 110)),
            (SpriteBlendMode.Screen, Color.FromArgb(155, 124, 106)),
            (SpriteBlendMode.ArtworkMultiply, Color.FromArgb(85, 66, 54)),
            (SpriteBlendMode.Subtractive, Color.FromArgb(10, 60, 90))
        ];
        foreach (var (mode, expected) in modes)
        {
            device.Clear(background);
            device.ArtworkBlend = mode;
            device.Sprite.Draw(translucent, null, Vector3.Zero, Vector4.One);
            device.ArtworkBlend = null;
            device.Sprite.Draw(translucent, null, new Vector3(4, 0, 0), Vector4.One);
            using var image = device.Capture(target);
            Near(expected, image.GetPixel(0, 0), $"Incorrect artwork blend for {mode}.");
            Near(modes[0].Expected, image.GetPixel(4, 0), "Resetting the artwork override must restore the authored blend.");
        }

        using var atlas = new Texture(device, 4, 3);
        var upload = atlas.BeginUpload();
        byte[] opaqueWhite = [255, 255, 255, 255];
        byte[] clear = [0, 0, 0, 0];
        byte[] opaqueRed = [0, 0, 255, 255];
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 4; x++)
            upload.Data.Write(x == 3 ? opaqueRed : x + y < 2 ? opaqueWhite : clear);
        atlas.EndUpload();
        Rectangle source = new(0, 0, 3, 3);
        var destination = new Rectangle(0, 0, 6, 6);

        device.TextureFilter = TextureFilterMode.Smooth;
        device.Clear(Color.Black);
        device.Sprite.Draw(atlas, source, destination, Vector4.One);
        using (var image = device.Capture(target))
        {
            Color edge = image.GetPixel(2, 2);
            Check(edge.R > 20 && edge.R < 255 && edge.R == edge.G, "Smooth interpolation must handle transparent edges.");
            Near(Color.Black, image.GetPixel(5, 1), "Smooth filtering must clamp to the selected atlas rectangle.");
        }

        device.TextureFilter = TextureFilterMode.EdgeAware;
        device.Clear(Color.Black);
        device.Sprite.Draw(atlas, source, destination, Vector4.One);
        using (var image = device.Capture(target))
        {
            Near(Color.White, image.GetPixel(2, 2), "Edge-aware magnification must reconstruct the diagonal corner.");
            Near(Color.Black, image.GetPixel(5, 1), "Edge-aware filtering must clamp to the selected atlas rectangle.");
        }

        device.TextureFilter = TextureFilterMode.Hd;
        device.Clear(Color.Black);
        device.Sprite.Draw(atlas, source, new Rectangle(0, 0, 3, 3), Vector4.One);
        using (var image = device.Capture(target))
            Near(Color.FromArgb(64, 64, 64), image.GetPixel(1, 1), "HD must resolve diagonal coverage at native size.");

        device.TextureFilter = TextureFilterMode.Pixel;
        device.PixelShader = ShaderEffect.Atmosphere;
        device.SetPixelShaderConstant(0, [new(1, 0, 1, 1), new(0.5f, 0.5f, 0, -1), Vector4.Zero,
            Vector4.Zero, Vector4.Zero, Vector4.Zero, new(1, 1, 1, 0), new(0, 1, 0, 1)]);
        device.Clear(Color.Black);
        device.Sprite.Draw(translucent, null, Vector3.Zero, Vector4.One);
        using (var image = device.Capture(target))
            Near(Color.FromArgb(52, 52, 52), image.GetPixel(0, 0), "Zero saturation must produce a monochrome image.");
        device.PixelShader = ShaderEffect.Artwork;
    }

    private static Texture Upload(RendererDevice device, Color colour)
    {
        var texture = new Texture(device, 2, 2);
        try
        {
            var upload = texture.BeginUpload();
            for (int i = 0; i < 4; i++)
                upload.Data.Write(new[] { colour.B, colour.G, colour.R, colour.A });
            texture.EndUpload();
            return texture;
        }
        catch { texture.Dispose(); throw; }
    }
    private static void Near(Color expected, Color actual, string message) => Check(
        Math.Abs(expected.R - actual.R) <= 2 && Math.Abs(expected.G - actual.G) <= 2 &&
        Math.Abs(expected.B - actual.B) <= 2 && Math.Abs(expected.A - actual.A) <= 2,
        $"{message} Expected {expected}, got {actual}.");
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
