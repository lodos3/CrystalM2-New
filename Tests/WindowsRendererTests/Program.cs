using Client.Rendering;
using Client.MirScenes;
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

        TestShapedLight(device, backBuffer, red);
        TestHdrHalo(device, backBuffer);
        TestLightSpill(device, backBuffer);
        TestArtworkStyles(device, backBuffer, translucent);
        TestGroundWeather(device, backBuffer);
        TestWeatherSilhouette(device, backBuffer);
        TestMapAmbient(device, backBuffer, intermediate);
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
        int coverageBytes = Enumerable.Range(0, SpellParticleAtlas.MipLevels)
            .Sum(mip => (SpellParticleAtlas.Width >> mip) * (SpellParticleAtlas.Height >> mip));
        Check(device.Blend == savedBlend && device.PixelShader == savedShader && device.DrawCalls == 1 &&
            device.TextureCount == textures + 1 &&
            device.TextureBytes == bytes + coverageBytes,
            "Particles must restore render state and batch into a single shared atlas texture.");
        device.SpellParticles.Draw(particles.Active, camera, 100, ground: false);
        device.Sprite.End();
        Check(device.TextureCount == textures + 1, "Subsequent particle draws must reuse the atlas.");
        TestSpellDepth(device, backBuffer, red);
        device.Resize(80, 60);
        using Surface resized = device.GetBackBuffer();
        device.Clear(Color.Black);
        device.Sprite.Draw(red, null, Vector3.Zero, Vector4.One);
        using (var image = device.Capture(resized))
        {
            Check(image.Size == new Size(80, 60), "Readback must use resized dimensions and row pitch.");
            Near(Color.Red, image.GetPixel(0, 0), "Existing textures must survive swapchain resize.");
        }
        TestParticleCoverageSampling(device);
        TestSignatureSpellEffects(device);
        TestLegacySpellEffects(device);
        TestWizardProjection();
        TestWarriorProjection();
        TestTaoistProjection();
        TestLegacyProjectilePlayback(device);
        TestWeaponSpellMask(device);
        Console.WriteLine("D3D11 shader, alpha, layer order, batching, flip, upload disposal and resize checks passed (WARP).");
    }

    private static void TestParticleCoverageSampling(RendererDevice device)
    {
        using var texture = new Texture(device, 640, 384, true);
        using var surface = texture.GetSurface();
        using var original = device.GetBackBuffer();
        TextureFilterMode filter = device.TextureFilter;
        Vector3 adjustment = device.TextureAdjustment;
        Matrix4x4 transform = device.Sprite.Transform;
        var particles = new SpellParticle[]
        {
            new(new Vector2(233.3f, 196.7f), Vector2.Zero, 100, 100, 400, Color.Gold,
                SpellParticleShape.SigilDisc, 0, Aspect: .65f, Layer: SpellParticleLayer.Ground),
            new(new Vector2(475.4f, 218.2f), Vector2.Zero, 100, 100, 39, Color.OrangeRed,
                SpellParticleShape.FlameTongue, 0, Aspect: 4.8f, Rotation: .17f, Layer: SpellParticleLayer.EmissiveCore),
            new(new Vector2(546.2f, 132.6f), Vector2.Zero, 100, 100, 62, Color.Gold,
                SpellParticleShape.WardFront, 0, Aspect: 1.5f, Layer: SpellParticleLayer.Emissive)
        };
        device.SetRenderTarget(surface);
        try
        {
            foreach (bool emission in new[] { false, true })
            {
                using Bitmap smooth = Render(TextureFilterMode.Smooth, new Vector3(.08f, 1, 0), emission);
                using Bitmap hd = Render(TextureFilterMode.Hd, new Vector3(.45f, 1, 0), emission);
                using Bitmap pixel = Render(TextureFilterMode.Pixel, Vector3.Zero, emission);
                int lit = 0, mismatches = 0;
                for (int y = 0; y < smooth.Height; y += 2)
                for (int x = 0; x < smooth.Width; x += 2)
                {
                    Color expected = smooth.GetPixel(x, y);
                    if (expected.R > 48) lit++;
                    if (expected.ToArgb() != hd.GetPixel(x, y).ToArgb() || expected.ToArgb() != pixel.GetPixel(x, y).ToArgb())
                        mismatches++;
                }
                Check(lit > (emission ? 8 : 500) && mismatches == 0,
                    $"Particle {(emission ? "light" : "art")} must have smooth coverage at any world sprite filter. Changed samples: {mismatches}.");
            }
        }
        finally
        {
            device.TextureFilter = filter;
            device.TextureAdjustment = adjustment;
            device.Sprite.Transform = transform;
            device.SetRenderTarget(original);
        }

        Bitmap Render(TextureFilterMode mode, Vector3 tuning, bool emission)
        {
            device.TextureFilter = mode;
            device.TextureAdjustment = tuning;
            device.Clear(Color.Black);
            device.Sprite.Transform = emission ? Matrix4x4.CreateScale(.125f, .125f, 1f) : Matrix4x4.Identity;
            device.SpellParticles.Draw(particles, Vector2.Zero, 100, ground: true, emission: emission);
            device.SpellParticles.Draw(particles, Vector2.Zero, 100, ground: false, emission: emission);
            if (!emission)
                device.SpellParticles.Draw(particles, Vector2.Zero, 100, ground: false, emissiveOverlay: true);
            Check(device.TextureFilter == mode && device.TextureAdjustment == tuning,
                "Particle drawing must preserve the world texture settings.");
            return device.Capture(surface);
        }
    }

    private static void TestSignatureSpellEffects(RendererDevice device)
    {
        using var texture = new Texture(device, 192, 160, true);
        using var surface = texture.GetSurface();
        using var original = device.GetBackBuffer();
        device.SetRenderTarget(surface);
        var pool = new SpellParticlePool(512);
        Vector2 impact = new(110, 110);
        foreach (SpecialisationAbility ability in SpecialisationAbilities.All.Concat(TalentAbilities.All.Select(talent => talent.Spell)))
        {
            pool.Clear();
            SignatureSpellEffects.Cell(pool, ability, impact, SpellEffect.SignatureImpact, 1000);
            pool.Advance(1050);
            device.Clear(Color.FromArgb(5, 6, 8));
            device.SpellParticles.Draw(pool.Active, Vector2.Zero, 1050, ground: false, emissiveOverlay: true);
            using var image = device.Capture(surface);
            int lit = 0;
            for (int y = 25; y < 126; y += 2)
            for (int x = 40; x < 178; x += 2)
            {
                Color pixel = image.GetPixel(x, y);
                if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) >= 75) lit++;
            }
            Check(lit > 18, $"{ability.Name} must have a readable impact on a nearly black map. Lit samples {lit}.");
            Near(Color.FromArgb(5, 6, 8), image.GetPixel(1, 1), $"{ability.Name} must preserve the background outside its particles.");
        }

        Check(SpecialisationAbilities.TryGet(Spell.CinderLance, out var cinder), "Cinder Lance has an authored flight.");
        pool.Clear();
        SignatureSpellEffects.Cast(pool, cinder, new Vector2(30, 115), new Vector2(164, 115), MirDirection.Right, 1000);
        pool.Advance(1220);
        device.Clear(Color.Black);
        device.SpellParticles.Draw(pool.Active, Vector2.Zero, 1220, ground: false, emissiveOverlay: true);
        using (var image = device.Capture(surface))
            Check(image.GetPixel(100, 92).R > 80 && image.GetPixel(164, 115).R < 15,
                "The bright projectile head travels between actors before impact.");

        Check(SpecialisationAbilities.TryGet(Spell.ArcaneHammer, out var hammer), "Arcane Hammer has an authored silhouette.");
        pool.Clear();
        SignatureSpellEffects.Cast(pool, hammer, new Vector2(42, 115), impact, MirDirection.Right, 1000);
        pool.Advance(1240);
        device.Clear(Color.FromArgb(205, 195, 178));
        device.SpellParticles.Draw(pool.Active, Vector2.Zero, 1240, ground: false, emissiveOverlay: true);
        using (var image = device.Capture(surface))
        {
            int silhouette = 0;
            for (int y = 8; y < 98; y += 2)
            for (int x = 48; x < 170; x += 2)
            {
                Color pixel = image.GetPixel(x, y);
                if (pixel.B > pixel.R + 40 && pixel.G < 145) silhouette++;
            }
            Check(silhouette > 25, "The coloured hammer outline must remain visible on bright terrain.");
        }
        device.SetRenderTarget(original);
        Console.WriteLine("Verified all signature impact profiles and the Cinder Lance flight in D3D11 WARP.");
    }

    private static void TestLegacySpellEffects(RendererDevice device)
    {
        using var texture = new Texture(device, 240, 192, true);
        using var surface = texture.GetSurface();
        using var original = device.GetBackBuffer();
        device.SetRenderTarget(surface);
        var pool = new SpellParticlePool(512);
        Vector2 feet = new(120, 126);
        Spell[] spells = new[] { MirClass.Warrior, MirClass.Wizard, MirClass.Taoist }
            .SelectMany(SpellCurriculum.ForClass).ToArray();
        foreach (Spell spell in spells)
        {
            Check(LegacySpellEffects.HasProfile(spell), $"Author a particle profile for {spell}.");
            bool field = spell is Spell.FireWall or Spell.Blizzard or Spell.MeteorStrike;
            var phase = field ? SpellVisualPhase.Field : SpellVisualPhase.Impact;
            long sampleTime = spell == Spell.Blizzard ? 1810 : 1050;
            Render(spell, phase, sampleTime, emission: false);
            Render(spell, phase, sampleTime, emission: true);
        }

        foreach (Spell spell in new[] { Spell.FireBall, Spell.GreatFireBall, Spell.FrostCrunch, Spell.SoulFireBall, Spell.FireBounce, Spell.Plague })
            Render(spell, SpellVisualPhase.Flight, 1042, emission: false);
        foreach (Spell spell in new[] { Spell.FireWall, Spell.PoisonCloud, Spell.TrapHexagon, Spell.Blizzard, Spell.MeteorStrike, Spell.Reincarnation, Spell.HealingCircle })
            Render(spell, SpellVisualPhase.Field, spell == Spell.Blizzard ? 1810 : 1075, emission: false);
        foreach (Spell spell in new[] { Spell.MagicShield, Spell.ProtectionField, Spell.EnergyShield, Spell.Fury, Spell.ImmortalSkin })
            Render(spell, SpellVisualPhase.Aura, 1100, emission: true);
        foreach (Spell spell in new[] { Spell.Slaying, Spell.Thrusting, Spell.HalfMoon, Spell.CrossHalfMoon, Spell.FlamingSword, Spell.TwinDrakeBlade, Spell.BladeAvalanche })
            Render(spell, SpellVisualPhase.Swing, 1120, emission: false);
        Render(Spell.FlamingSword, SpellVisualPhase.Burn, 1100, emission: true);
        Render(Spell.SlashingBurst, SpellVisualPhase.Swing, 1190, emission: false);
        Render(Spell.CounterAttack, SpellVisualPhase.Swing, 1040, emission: true);
        Render(Spell.Entrapment, SpellVisualPhase.Prepare, 1120, emission: false);
        device.SetRenderTarget(original);
        Console.WriteLine($"Verified visible particle art and light for all {spells.Length} Warrior, Wizard and Taoist spells in D3D11 WARP.");

        void Render(Spell spell, SpellVisualPhase phase, long now, bool emission)
        {
            pool.Clear();
            var visual = new SpellVisual(spell, phase);
            if (WizardSpellEffects.HasArt(spell))
            {
                if (phase == SpellVisualPhase.Flight)
                    LegacySpellEffects.ProjectileFrame(pool, spell, feet - Vector2.UnitY * 47, Vector2.UnitX,
                        now, 1, 250, leaveWake: true);
                else
                    WizardSpellEffects.DrawFrame(pool, visual, feet, Vector2.UnitX, now, 1000,
                        phase == SpellVisualPhase.Field ? 3000 : 620);
            }
            else if (WarriorSpellEffects.HasArt(spell))
            {
                if (phase == SpellVisualPhase.Swing &&
                    spell is not (Spell.Thrusting or Spell.BladeAvalanche or Spell.SlashingBurst or Spell.CounterAttack))
                {
                    var previous = new WeaponMaskTrace(feet + new Vector2(0, -31), feet + new Vector2(38, -85));
                    var current = new WeaponMaskTrace(feet + new Vector2(4, -32), feet + new Vector2(61, -32));
                    LegacySpellEffects.WeaponSweep(pool, spell, current, previous, now, 180, 100);
                }
                else
                    WarriorSpellEffects.DrawFrame(pool, visual, feet, Vector2.UnitX, now, 1000, 620);
            }
            else if (TaoistSpellEffects.HasArt(spell))
            {
                if (phase == SpellVisualPhase.Flight)
                    LegacySpellEffects.ProjectileFrame(pool, spell, feet - Vector2.UnitY * 36, Vector2.UnitX,
                        now, 1, 250, leaveWake: true);
                else
                    TaoistSpellEffects.DrawFrame(pool, visual, feet, Vector2.UnitX, now, 1000,
                        phase == SpellVisualPhase.Field ? 3000 : LegacySpellEffects.PacketDuration(visual));
            }
            else
            {
                LegacySpellEffects.Emit(pool, visual, feet, Vector2.UnitX, 1000);
                pool.Advance(now);
            }
            device.Clear(Color.Black);
            device.SpellParticles.Draw(pool.Active, Vector2.Zero, now, ground: true, emission: emission);
            device.SpellParticles.Draw(pool.Active, Vector2.Zero, now, ground: false, emission: emission, sizeScale: 1.28f);
            if (!emission) device.SpellParticles.Draw(pool.Active, Vector2.Zero, now, ground: false, sizeScale: 1.28f, emissiveOverlay: true);
            using var image = device.Capture(surface);
            int lit = 0;
            for (int y = 8; y < image.Height - 8; y += 2)
            for (int x = 12; x < image.Width - 12; x += 2)
            {
                Color pixel = image.GetPixel(x, y);
                if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) > 24) lit++;
            }
            Check(lit > 5, $"{spell} {phase} must render {(emission ? "light" : "visible art")}. Samples {lit}.");
        }
    }

    private static void TestWizardProjection()
    {
        var pool = new SpellParticlePool(1024);
        Vector2 feet = new(180, 220);
        SpellParticle[] shell = Sample(Spell.MagicShield, SpellVisualPhase.Aura, 250, feet);
        SpellParticle back = shell.Single(p => p.Shape == SpellParticleShape.WardBack);
        SpellParticle front = shell.Single(p => p.Shape == SpellParticleShape.WardFront);
        Vector2 dims = front.Dimensions(0, WizardSpellEffects.DefaultSize);
        Check(back.Ground && back.GroundDepth == feet.Y && front.Emissive &&
            dims.Y > dims.X * 1.3f && front.Origin == feet - Vector2.UnitY * 29 &&
            front.Colour.R > front.Colour.G && front.Colour.G > front.Colour.B,
            "Magic Shield places a gold oval around the actor and draws a back layer behind the sprite.");
        Vector2 moved = new(37, -12);
        SpellParticle movingShell = Sample(Spell.MagicShield, SpellVisualPhase.Aura, 287, feet + moved)
            .Single(p => p.Shape == SpellParticleShape.WardFront);
        Check(movingShell.Origin == front.Origin + moved && movingShell.Size == front.Size,
            "The persistent ward follows its owner without a pulse in size.");
        SpellParticle settled = Sample(Spell.MagicShield, SpellVisualPhase.Hit, 999, feet)
            .Single(p => p.Shape == SpellParticleShape.WardFront && p.Layer == SpellParticleLayer.Emissive);
        Check(settled.Colour.A > 200 && settled.Size == front.Size,
            "Magic Shield retains its oval when the hit flash settles back into the aura.");

        SpellParticle[] shock = Sample(Spell.ElectricShock, SpellVisualPhase.Impact, 100, feet);
        Rectangle shockBounds = Bounds(shock);
        Rectangle later = Bounds(Sample(Spell.ElectricShock, SpellVisualPhase.Impact, 540, feet));
        Check(shockBounds.Top >= feet.Y - 129 && shockBounds.Width < 96 && shockBounds.Top < later.Top - 12 &&
            shock.Any(p => p.Ground && p.Shape == SpellParticleShape.Filament) &&
            shock.Any(p => p.Emissive && p.Shape == SpellParticleShape.Filament),
            "Electric Shock descends around the body and has depth on both sides of the actor.");
        Check(Bounds(Sample(Spell.ThunderBolt, SpellVisualPhase.Impact, 170, feet)).Top < feet.Y - 800,
            "Thunder Bolt keeps the distinct full-height sky strike.");

        SpellParticle[] wall = Sample(Spell.FireWall, SpellVisualPhase.Field, 280, feet);
        SpellParticle[] flame = wall.Where(p => p.Shape == SpellParticleShape.FlameSequence).ToArray();
        Rectangle wallBounds = Bounds(flame);
        Check(wall.Any(p => p.Ground && p.Origin == feet) && wallBounds.Top < feet.Y - 112 && wallBounds.Width < 95,
            "Fire Wall rises vertically above its floor tile.");
        Check(!flame.Select(p => p.Frame).SequenceEqual(Sample(Spell.FireWall, SpellVisualPhase.Field, 441, feet)
                .Where(p => p.Shape == SpellParticleShape.FlameSequence).Select(p => p.Frame)),
            "The Fire Wall particle clip advances independently of the persistent field clock.");
        SpellParticle[] field = Sample(Spell.FlameField, SpellVisualPhase.Impact, 740, feet);
        Check(field.Any(p => p.Ground && p.Shape == SpellParticleShape.SigilDisc && p.Size > 300 && p.Origin == feet) &&
            field.Where(p => p.Shape == SpellParticleShape.FlameSequence).Select(p => p.Origin).Distinct().Count() >= 20 &&
            field.Any(p => p.Emissive && p.Shape == SpellParticleShape.SigilDisc && p.Origin.Y < feet.Y - 110),
            "Flame Field has a projected ground seal, a raised seal, and flames across the affected area.");
        Check(!Sample(Spell.Blizzard, SpellVisualPhase.Field, 100, feet).Any(p => p.Ground) &&
            Sample(Spell.Blizzard, SpellVisualPhase.Field, 800, feet).Any(p => p.Ground && Vector2.Distance(p.Origin, feet) < 32),
            "Blizzard descends before it starts emitting snow at the floor.");
        Check(Sample(Spell.MeteorStrike, SpellVisualPhase.Impact, 300, feet)
                .Any(p => p.Ground && p.Shape == SpellParticleShape.SigilDisc && p.Origin == feet && p.Size > 225) &&
            !Sample(Spell.MeteorStrike, SpellVisualPhase.Field, 800, feet).Any(p => p.Shape == SpellParticleShape.SigilDisc),
            "Meteor Strike draws its seal at the cast centre and keeps the per-cell field clear of duplicate runes.");

        SpellParticle[] iceEast = Sample(Spell.IceThrust, SpellVisualPhase.Impact, 510, feet);
        SpellParticle[] iceNorth = Sample(Spell.IceThrust, SpellVisualPhase.Impact, 510, feet, -Vector2.UnitY);
        Check(Sample(Spell.IceThrust, SpellVisualPhase.Impact, 130, feet)
                .Any(p => p.Shape == SpellParticleShape.Orb && p.Origin == feet + new Vector2(8, -42)) &&
            iceEast.Any(p => p.Ground && p.Origin.X > feet.X + 165) &&
            iceNorth.Any(p => p.Ground && p.Origin.Y < feet.Y - 111) &&
            iceNorth.All(p => p.Shape != SpellParticleShape.IceSpire),
            "Ice Thrust begins at the chest and extends into a projected area along the chosen heading.");
        SpellParticle[] storm = Sample(Spell.IceStorm, SpellVisualPhase.Impact, 510, feet);
        Check(storm.Count(p => p.Shape == SpellParticleShape.Orb && p.Size < 14) > 23 &&
            storm.Any(p => p.Shape == SpellParticleShape.SmokeSequence) &&
            storm.All(p => p.Shape is not (SpellParticleShape.Shard or SpellParticleShape.IceSpire)),
            "Ice Storm is made of blowing snow and wind layers.");

        pool.Clear();
        Vector2 to = feet + new Vector2(288, 192);
        WizardSpellEffects.DrawArc(pool, Spell.Lightning, feet, to, 1200, 1000, WizardSpellEffects.LightningDuration,
            WizardSpellEffects.DefaultSize, 56);
        SpellParticle[] arc = pool.Active.ToArray();
        Rectangle arcBounds = Bounds(arc);
        Check(arc.Length > 100 && arc.Select(p => MathF.Round(p.Rotation, 2)).Distinct().Count() > 24 &&
            arcBounds.Left <= feet.X && arcBounds.Right >= to.X - 3 && arcBounds.Bottom >= to.Y - 44,
            "Lightning uses irregular connected channels along the projected server path.");
        pool.Advance(1600);
        WizardSpellEffects.DrawArc(pool, Spell.Lightning, feet, to, 1600, 1000, WizardSpellEffects.LightningDuration,
            WizardSpellEffects.DefaultSize, 56);
        Check(pool.Active.IsEmpty, "The channel stops at the legacy cue boundary.");

        SpellParticle[] Sample(Spell spell, SpellVisualPhase phase, int time, Vector2 point, Vector2? direction = null)
        {
            pool.Clear();
            WizardSpellEffects.DrawFrame(pool, new SpellVisual(spell, phase), point, direction ?? Vector2.UnitX,
                1000 + time, 1000, 1000, seed: 365);
            return pool.Active.ToArray();
        }

        static Rectangle Bounds(SpellParticle[] particles) => particles.Select(p =>
            p.Bounds(p.Origin, p.Dimensions(0, p.Ground ? 1f : WizardSpellEffects.DefaultSize))).Aggregate(Rectangle.Union);
    }

    private static void TestWarriorProjection()
    {
        var pool = new SpellParticlePool(256);
        Vector2 feet = new(200, 200);
        const long cast = 1000;
        var burst = LegacySpellEffects.Windup(Spell.SlashingBurst, 6, 100);
        long start = cast + burst.Delay;
        Check(burst.AtCell && burst.Duration == 900 &&
            LegacySpellEffects.Windup(Spell.BladeAvalanche, 6, 100).Duration == 1500 &&
            LegacySpellEffects.Windup(Spell.CounterAttack, 6, 100).Duration == 1000,
            "Warrior effects retain the original cast durations and Slashing Burst's delayed tile origin.");
        var visual = new SpellVisual(Spell.SlashingBurst, burst.Phase);
        WarriorSpellEffects.DrawFrame(pool, visual, feet, Vector2.UnitX, start - 1, start, burst.Duration);
        Check(pool.Active.IsEmpty, "Slashing Burst waits for the original 300 ms cue.");
        WarriorSpellEffects.DrawFrame(pool, visual, feet, -Vector2.UnitY, start + 310, start, burst.Duration);
        Check(pool.Active.ToArray().Any(p => p.Shape == SpellParticleShape.WarriorImpact &&
            p.Origin.Y < feet.Y - 65 && p.Origin.X == feet.X), "A northbound slash projects forward from its tile.");
        pool.Advance(start + burst.Duration);
        WarriorSpellEffects.DrawFrame(pool, visual, feet, Vector2.UnitX, start + burst.Duration, start, burst.Duration);
        Check(pool.Active.IsEmpty, "Slashing Burst stops at the end of the legacy cue.");

        var skin = new SpellVisual(Spell.ImmortalSkin, SpellVisualPhase.Aura);
        WarriorSpellEffects.DrawFrame(pool, skin, feet, Vector2.UnitX, cast, cast, 1400);
        SpellParticle ring = pool.Active.ToArray().Single(p => p.Shape == SpellParticleShape.WarriorRing);
        pool.Advance(cast + 25);
        Vector2 step = new(31, 20);
        WarriorSpellEffects.DrawFrame(pool, skin, feet + step, Vector2.UnitX, cast + 25, cast + 25, 1400);
        Check(pool.Active.ToArray().Single(p => p.Shape == SpellParticleShape.WarriorRing).Origin == ring.Origin + step,
            "Immortal Skin follows the interpolated actor each frame.");

        pool.Clear();
        WarriorSpellEffects.Dash(pool, feet, feet + new Vector2(48, -32), cast);
        Check(pool.Active.ToArray().Count(p => p.Ground && p.Shape == SpellParticleShape.WarriorDust) == 4 &&
            pool.Active.ToArray().All(p => !p.FrameOnly && !p.CastsLight),
            "Shoulder Dash leaves world-space dust along its tile path without glowing.");

        var blade = new WeaponMaskTrace(feet + new Vector2(4, -36), feet + new Vector2(46, -37));
        SpellParticle[] moon = Sweep(Spell.HalfMoon, Vector2.UnitX);
        SpellParticle[] edges = moon.Where(p => p.Ribbon.HasValue && p.Colour.A == 235).ToArray();
        Check(edges.Length == 14 && edges.All(p => p.Origin.X + p.Ribbon.Value.BottomRight.X > feet.X + 15) &&
            Thickness(edges[6]) > 8 * Thickness(edges[0]) &&
            Thickness(edges[6]) > 8 * Vector2.Distance(edges[^1].Ribbon.Value.TopRight, edges[^1].Ribbon.Value.BottomRight),
            "Half Moon opens toward the target direction with tapered tips and a thick middle.");
        SpellParticle[] cross = Sweep(Spell.CrossHalfMoon, Vector2.UnitX);
        Check(cross.Any(p => p.Ribbon.HasValue && p.Ground && p.Ribbon.Value.TopLeft.X < -62) &&
            cross.Any(p => p.Ribbon.HasValue && p.Emissive && p.Ribbon.Value.TopLeft.X > 62),
            "Cross Half Moon wraps behind and in front of the warrior.");
        pool.Clear();
        var backswing = new WeaponMaskTrace(blade.Hilt, blade.Tip - new Vector2(4, 26));
        LegacySpellEffects.WeaponSweep(pool, Spell.Thrusting, blade, backswing, cast, 190, 100, Vector2.UnitX, feet: feet);
        Check(!pool.Active.IsEmpty && pool.Active.ToArray().All(p => p.Shape == SpellParticleShape.BladeRibbon),
            "Thrusting keeps the weapon trail separate from the player's wind.");

        var thrust = LegacySpellEffects.Windup(Spell.Thrusting, 6, 100);
        Check(thrust is { Phase: SpellVisualPhase.Swing, Duration: 300, Delay: 300, AtCell: false },
            "The gust opens on the fourth legacy frame, after the three weapon frames.");
        Vector2 chest = feet - Vector2.UnitY * 36;
        SpellParticle[] earlyWind = Wind(24, Vector2.UnitX);
        SpellParticle[] fullWind = Wind(230, Vector2.UnitX);
        Check(earlyWind.Length > 0 && earlyWind.Max(p => p.Origin.X) < chest.X + 28 &&
            fullWind.Min(p => p.Origin.X) < chest.X + 10 && fullWind.Max(p => p.Origin.X) > chest.X + 86 &&
            fullWind.All(p => p.FrameOnly && p.Velocity == Vector2.Zero),
            "The curl opens forward from the player while the roots of the strands stay attached.");
        SpellParticle[] strands = fullWind.Where(p => p.Shape == SpellParticleShape.WindRibbon).ToArray();
        Check(strands.Length > 50 && Thickness(strands[4]) > Thickness(strands[0]) * 5 &&
            Vector2.DistanceSquared(strands[0].Origin + strands[0].Ribbon.Value.TopRight,
                strands[1].Origin + strands[1].Ribbon.Value.TopLeft) < .0001f &&
            fullWind.All(p => p.Shape is SpellParticleShape.WindRibbon or SpellParticleShape.Wisp),
            "The gust uses joined, tapered strands with soft light around the curl.");
        SpellParticle[] northWind = Wind(230, -Vector2.UnitY);
        float endOnWidth = northWind.Max(p => p.Origin.X) - northWind.Min(p => p.Origin.X);
        float sideOnWidth = fullWind.Max(p => p.Origin.Y) - fullWind.Min(p => p.Origin.Y);
        Check(northWind.Min(p => p.Origin.Y) < chest.Y - 62 &&
            endOnWidth < sideOnWidth * .53f && sideOnWidth > 59 &&
            Wind(230, Vector2.UnitX, extraReach: 1).Max(p => p.Origin.X) > fullWind.Max(p => p.Origin.X) + 35 &&
            Wind(-1, Vector2.UnitX).Length == 0 && Wind(thrust.Duration, Vector2.UnitX).Length == 0,
            $"The gust opens wider from the side, follows reach perks and stays inside the original release window. Widths {endOnWidth:F1} and {sideOnWidth:F1}.");

        pool.Clear();
        WarriorSpellEffects.DrawWeaponEnchant(pool, Spell.TwinDrakeBlade, blade, Vector2.UnitX, 1240, 1.28f, 71, striking: true);
        Check(pool.Active.ToArray().Where(p => p.Shape == SpellParticleShape.Filament)
            .Select(p => MathF.Round(p.Rotation, 2)).Distinct().Count() > 18,
            "Twin Drake Blade has varied angular channels and forks.");

        SpellParticle[] initial = Sample(Spell.BladeAvalanche, SpellVisualPhase.Swing, 280, 1500);
        SpellParticle[] advancing = Sample(Spell.BladeAvalanche, SpellVisualPhase.Swing, 1160, 1500);
        SpellParticle[] spikes = advancing.Where(p => p.Shape == SpellParticleShape.IceSpire).ToArray();
        Check(initial.All(p => p.Shape != SpellParticleShape.IceSpire) && spikes.Length >= 9 &&
            spikes.Max(p => p.Origin.X) > feet.X + 132 &&
            advancing.Where(p => p.Shape == SpellParticleShape.WarriorDust).Select(p => p.Origin.Y).Distinct().Count() >= 5,
            "Blade Avalanche gathers at the warrior, then sends spreading lanes and rising spikes forward.");
        SpellParticle[] fire = Sample(Spell.FlamingSword, SpellVisualPhase.Contact, 240, 640);
        Check(fire.Any(p => p.Shape == SpellParticleShape.FlameSequence && p.Ground && p.CastsLight) &&
            fire.Any(p => p.Shape == SpellParticleShape.FlameSequence && p.Emissive && p.Origin.Y < feet.Y - 40),
            "Flaming Sword contact puts rooted flames behind and in front of the target.");

        SpellParticle[] Sweep(Spell spell, Vector2 facing)
        {
            pool.Clear();
            LegacySpellEffects.WeaponSweep(pool, spell, blade, null, cast, 190, 100, facing, feet: feet);
            return pool.Active.ToArray();
        }

        SpellParticle[] Sample(Spell spell, SpellVisualPhase phase, int elapsed, int duration)
        {
            pool.Clear();
            WarriorSpellEffects.DrawFrame(pool, new SpellVisual(spell, phase), feet, Vector2.UnitX, cast + elapsed, cast, duration);
            return pool.Active.ToArray();
        }

        SpellParticle[] Wind(int elapsed, Vector2 direction, int extraReach = 0)
        {
            pool.Clear();
            long launch = cast + thrust.Delay;
            WarriorSpellEffects.DrawFrame(pool, new SpellVisual(Spell.Thrusting, thrust.Phase, ExtraReach: extraReach),
                feet, direction, launch + elapsed, launch, thrust.Duration);
            return pool.Active.ToArray();
        }

        static float Thickness(SpellParticle p) =>
            Vector2.Distance(p.Ribbon.Value.TopLeft, p.Ribbon.Value.BottomLeft);
    }

    private static void TestTaoistProjection()
    {
        var pool = new SpellParticlePool(512);
        Vector2 feet = new(200, 200);
        const long start = 1000;
        TaoistSpellEffects.DrawFrame(pool, new SpellVisual(Spell.HealingCircle, SpellVisualPhase.Field),
            feet, Vector2.UnitX, 1620, start, int.MaxValue, field: 47);
        SpellParticle[] circle = pool.Active.ToArray();
        SpellParticle centre = circle.Single(p => p.Shape == SpellParticleShape.YinYang);
        SpriteQuad seal = centre.Ribbon.GetValueOrDefault();
        Vector2 right = Unproject(seal.TopRight - seal.TopLeft);
        Vector2 down = Unproject(seal.BottomLeft - seal.TopLeft);
        Check(centre.Layer == SpellParticleLayer.Floor && centre.Origin == feet - Vector2.UnitY &&
            MathF.Abs(right.Length() - down.Length()) < .01f && MathF.Abs(Vector2.Dot(right, down)) < .01f &&
            circle.Count(p => p.Shape == SpellParticleShape.Trigrams && p.Layer == SpellParticleLayer.Floor) == 3 &&
            circle.All(p => p.FieldObjectID == 47),
            "Healing Circle rotates in the ground plane and keeps its runes and light on the field owner.");
        Check(pool.RemoveField(47) && pool.Active.IsEmpty, "Removing the field erases the floor seal and all of its particles.");

        foreach (var (spell, glyph) in new[] { (Spell.SoulShield, SpellParticleShape.SoulGlyph),
                     (Spell.BlessedArmour, SpellParticleShape.ArmourGlyph) })
        {
            var ward = new SpellVisual(spell);
            TaoistSpellEffects.DrawFrame(pool, ward, feet, Vector2.UnitX, 1180, start, 1200);
            var ignition = pool.Active.ToArray();
            Check(ignition.Any(p => p.Shape == SpellParticleShape.Filament && p.Origin.Y < feet.Y - 26) &&
                ignition.All(p => p.Shape != glyph), "The ward reveals only the ignited strokes at first.");
            pool.Clear();
            TaoistSpellEffects.DrawFrame(pool, ward, feet, Vector2.UnitX, 1720, start, 1200);
            Check(pool.Active.ToArray().Count(p => p.Shape == SpellParticleShape.FlameTongue && p.Emissive) >= 20 &&
                pool.Active.ToArray().Any(p => p.Shape == glyph),
                "The ward settles into its full-size glyph with flames following its strokes.");
            pool.Clear();
        }
        TaoistSpellEffects.DrawFrame(pool, new SpellVisual(Spell.EnergyShield, SpellVisualPhase.Aura),
            feet, Vector2.UnitX, 1320, start, 800);
        SpellParticle[] shield = pool.Active.ToArray();
        Check(shield.Any(p => p.Shape == SpellParticleShape.Filament && p.Origin.X < feet.X && p.Colour.B > p.Colour.R) &&
            shield.Any(p => p.Shape == SpellParticleShape.Filament && p.Origin.X > feet.X && p.Colour.R > p.Colour.B),
            "Energy Shield closes around the target with distinct blue and gold crescents.");

        static Vector2 Unproject(Vector2 point) => new(point.X, point.Y / SpellParticleFrame.GroundAspect);
    }

    private static void TestLegacyProjectilePlayback(RendererDevice device)
    {
        var cast = LegacySpellEffects.Windup(Spell.FireBall, 6, 100);
        Check(cast.Duration == 600 && cast.Delay == 0 && cast.Phase == SpellVisualPhase.Prepare,
            "Fire Ball preparation spans the original six-frame cast.");
        Check(LegacySpellEffects.Windup(Spell.ThunderBolt, 6, 100).Duration == 300 &&
            LegacySpellEffects.Windup(Spell.SlashingBurst, 6, 100).Delay == 300 &&
            LegacySpellEffects.FieldInterval(Spell.FireWall) == 120, "Preserve authored spell cue offsets and cadence.");

        var flight = new LegacyProjectileTimeline(1000 + cast.Duration, 5, 30);
        Check(flight.Duration == 250 && flight.Impact == 1850 && flight.Frames == 8 &&
            !flight.InFlight(1599) && flight.InFlight(1600) && flight.InFlight(1849) && !flight.InFlight(1850),
            "The moving head has no anticipation or overshoot around the legacy flight window.");
        Check(Math.Abs(flight.Progress(1725) - .5f) < .001f, "Retain fifty milliseconds of flight per map cell.");

        using var texture = new Texture(device, 248, 192, true);
        using var surface = texture.GetSurface();
        using var original = device.GetBackBuffer();
        device.SetRenderTarget(surface);
        var pool = new SpellParticlePool(256);
        long now = 1725;
        Vector2 head = new(126, 83);
        foreach (Vector2 heading in new[] { Vector2.UnitX, Vector2.UnitY, Vector2.Normalize(new Vector2(1, -1)) })
        {
            pool.Clear();
            LegacySpellEffects.ProjectileFrame(pool, Spell.FireBall, head, heading, now, flight.Frame(now),
                (int)(flight.Impact - now), leaveWake: true);
            device.Clear(Color.Black);
            device.SpellParticles.Draw(pool.Active, Vector2.Zero, now, ground: false, sizeScale: 1.28f, emissiveOverlay: true);
            using var image = device.Capture(surface);
            int top = image.Height, bottom = -1, left = image.Width, right = -1;
            for (int y = 51; y < 115; y++)
            for (int x = 94; x < 158; x++)
            {
                Color pixel = image.GetPixel(x, y);
                if (pixel.R < 100) continue;
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
            int width = right - left + 1, height = bottom - top + 1;
            Check(width > 29 && height > 29 && Math.Max(width, height) < 1.6f * Math.Min(width, height),
                $"Fire Ball must have a full head in any direction. Bright bounds were {width} × {height}.");
            Check(image.GetPixel((int)head.X, (int)head.Y).R > 210, "The moving head retains a hot core.");
        }
        pool.Advance(now + 16);
        Check(pool.Active.ToArray().All(particle => !particle.FrameOnly), "Head geometry cannot linger behind the current projectile.");
        pool.Advance(flight.Impact);
        Check(pool.Active.IsEmpty, "The wake cannot extend the legacy flight duration.");
        LegacySpellEffects.ProjectileFrame(pool, Spell.FrostCrunch, head, Vector2.UnitX, now, flight.Frame(now),
            (int)(flight.Impact - now), leaveWake: true);
        Check(pool.Active.ToArray().Any(p => p.Shape == SpellParticleShape.FireSequence && p.Colour.B > p.Colour.R) &&
            pool.Active.ToArray().Count(p => !p.FrameOnly && p.Linear && p.Velocity.Y > 0 && p.Origin.Y > head.Y) == 4,
            "Frost Crunch uses the rounded Fire Ball clip and drops snow beneath the moving head.");
        pool.Advance(flight.Impact);
        Check(pool.Active.IsEmpty, "Frost Crunch snow respects the flight window.");
        pool.Clear();
        WizardSpellEffects.DrawFrame(pool, new SpellVisual(Spell.FireBall, SpellVisualPhase.Prepare), head,
            Vector2.UnitX, 1599, 1000, cast.Duration);
        Check(!pool.Active.IsEmpty, "The cast charge remains visible through its final animation frame.");
        pool.Advance(1600);
        WizardSpellEffects.DrawFrame(pool, new SpellVisual(Spell.FireBall, SpellVisualPhase.Prepare), head,
            Vector2.UnitX, 1600, 1000, cast.Duration);
        Check(pool.Active.IsEmpty, "The charge disappears at the legacy launch time.");
        WizardSpellEffects.DrawFrame(pool, new SpellVisual(Spell.MeteorStrike, SpellVisualPhase.Field), head, Vector2.UnitX,
            1700, 1600, 4000, field: 101);
        WizardSpellEffects.DrawFrame(pool, new SpellVisual(Spell.FireWall, SpellVisualPhase.Field), head, Vector2.UnitX,
            1700, 1600, 4000, field: 102);
        Check(pool.RemoveField(101), "Remove a finished ground effect.");
        Check(!pool.Active.IsEmpty && pool.Active.ToArray().All(particle => particle.FieldObjectID == 102),
            "Field expiry clears the removed field's art while preserving other fields.");
        device.SetRenderTarget(original);
    }

    private static void TestWeaponSpellMask(RendererDevice device)
    {
        using var texture = new Texture(device, 192, 160, true);
        using var surface = texture.GetSurface();
        using var original = device.GetBackBuffer();
        using var mask = new Texture(device, 16, 16);
        DataRectangle upload = mask.BeginUpload();
        var row = new byte[upload.Pitch];
        for (int y = 0; y < 16; y++)
        {
            Array.Clear(row);
            if (y is >= 6 and <= 9)
                for (int x = 2; x < 14; x++) row[x * 4 + 3] = 255;
            upload.Data.Write(row);
        }
        mask.EndUpload();

        device.SetRenderTarget(surface);
        device.Clear(Color.Black);
        ShaderEffect previousShader = device.PixelShader;
        SpriteBlendMode previousBlend = device.Blend;
        device.SpellParticles.DrawSpriteMask(mask, new Rectangle(54, 41, 64, 64),
            new Vector4(1, .25f, .02f, 1), new Vector4(1, .83f, .42f, 1), 1175);
        using var image = device.Capture(surface);
        Check(image.GetPixel(81, 71).R > 100, "The equipped blade alpha carries the spell tint even on a dark weapon.");
        Near(Color.Black, image.GetPixel(81, 54), "Transparent texels above the blade must stay dark.");
        Near(Color.Black, image.GetPixel(57, 71), "The mask must follow the weapon's own offset and width.");
        Check(device.PixelShader == previousShader && device.Blend == previousBlend, "Weapon glow restores sprite state.");

        using var coat = Texture.CreateCoverageAtlas(device, 16, 16, mipLevels: 1);
        var coverage = new byte[16 * 16];
        for (int y = 2; y < 13; y++)
        for (int x = 3; x < 8; x++)
            coverage[y * 16 + x] = 255;
        DataRectangle coatUpload = coat.BeginUpload();
        coatUpload.Data.Write(coverage);
        coat.EndUpload();
        device.Clear(Color.Black);
        device.SpellParticles.DrawSpriteMask(coat, new Rectangle(54, 41, 64, 64),
            new Vector4(.19f, .65f, .98f, 1), new Vector4(.59f, .93f, 1, 1), 1175, SpellMaskStyle.Ward, coverage: true);
        using var coated = device.Capture(surface);
        Check(coated.GetPixel(77, 70).B > 120, "The shader reads an R8 actor mask as coverage.");
        Near(Color.Black, coated.GetPixel(103, 83), "Transparent shadow coverage stays clear beside the body.");

        var pool = new SpellParticlePool(64);
        var prior = new WeaponMaskTrace(new(30, 85), new(89, 42));
        var current = new WeaponMaskTrace(new(44, 85), new(115, 85));
        LegacySpellEffects.WeaponSweep(pool, Spell.FlamingSword, current, prior, 1100, remaining: 84, interval: 100);
        SpellParticle[] ribbons = pool.Active.ToArray().Where(p => p.Ribbon.HasValue).ToArray();
        Vector2 oldEdge = prior.Hilt + (prior.Tip - prior.Hilt) * 1.21f;
        Vector2 newEdge = current.Hilt + (current.Tip - current.Hilt) * 1.21f;
        Check(ribbons.Length >= 2 && ribbons.Any(p => Vector2.DistanceSquared(p.Origin + p.Ribbon.Value.BottomLeft, oldEdge) < .01f) &&
            ribbons.Any(p => Vector2.DistanceSquared(p.Origin + p.Ribbon.Value.BottomRight, newEdge) < .01f),
            "The swept ribbon spans both actual weapon poses.");
        device.Clear(Color.Black);
        device.SpellParticles.Draw(ribbons, Vector2.Zero, 1100, ground: false, emissiveOverlay: true);
        using var swept = device.Capture(surface);
        int lit = 0;
        for (int y = 42; y < 85; y++)
        for (int x = 64; x < 123; x++)
            if (swept.GetPixel(x, y).R > 48) lit++;
        Check(lit > 160, "The Vortice renderer draws coverage across the swept area.");
        pool.Advance(1184);
        Check(pool.Active.IsEmpty, "Weapon trails respect the remaining swing duration.");
        device.SetRenderTarget(original);
        Console.WriteLine("Verified round projectile heads, legacy cue windows, swept weapon tips and exact weapon alpha in D3D11 WARP.");
    }

    private static void TestGroundWeather(RendererDevice device, Surface target)
    {
        device.PixelShader = ShaderEffect.WeatherGround;
        device.Clear(Color.Black);
        device.SetPixelShaderConstant(0, [new(0, 0, 0, 1), new(0, 0, 1, 0), Vector4.Zero]);
        device.SetPixelShaderConstant(3, [Vector4.Zero, Vector4.Zero]);
        device.Ui.Fill(new Rectangle(0, 0, 48, 32), Color.FromArgb(3, 255, 255, 255));
        using var first = device.Capture(target);
        device.Clear(Color.Black);
        device.SetPixelShaderConstant(0, [new(10, 6, 0, 1), new(10, 6, 1, 0), Vector4.Zero]);
        device.Ui.Fill(new Rectangle(10, 6, 48, 32), Color.FromArgb(3, 255, 255, 255));
        using var shifted = device.Capture(target);
        Near(first.GetPixel(20, 16), shifted.GetPixel(30, 22), "Accumulation and fog must remain anchored through camera motion.");
        Check(shifted.GetPixel(30, 22).B > 16, "Snow and fog should visibly affect the floor.");
        Near(Color.Black, shifted.GetPixel(58, 22), "A skipped obstacle tile must receive no weather coverage.");

        device.Clear(Color.Black);
        device.SetPixelShaderConstant(0, [Vector4.Zero, new(32 - 46 * 271, -7031, 0.9f, 0), Vector4.Zero]);
        device.Ui.Fill(new Rectangle(0, 0, 64, 64), Color.White);
        using var distantFog = device.Capture(target);
        for (int y = 0; y < 64; y++)
            Check(Math.Abs(distantFog.GetPixel(31, y).B - distantFog.GetPixel(32, y).B) <= 5,
                "Adjacent noise cells far from the origin must share the same edge.");
        device.PixelShader = ShaderEffect.Artwork;
    }

    private static void TestWeatherSilhouette(RendererDevice device, Surface target)
    {
        using var sprite = new Texture(device, 4, 1);
        var data = sprite.BeginUpload();
        data.Data.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 128, 26, 129, 39, 255, 0, 0, 0, 255 });
        sprite.EndUpload();
        using var dither = new Texture(device, 4, 4);
        var ditherData = dither.BeginUpload();
        var dots = new byte[4 * 4 * 4];
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
            if ((x + y) % 2 == 0) dots[(y * 4 + x) * 4 + 3] = 255;
        ditherData.Data.Write(dots);
        dither.EndUpload();
        const int size = 128, tileFeather = 28;
        using var weather = new Texture(device, size, size, true);
        using var weatherSurface = weather.GetSurface();
        using var mask = Texture.CreateWeatherMask(device, size, size);
        using var maskSurface = mask.GetSurface();
        using var blurred = new Texture(device, size / 4, size / 4, true);
        using var blurredSurface = blurred.GetSurface();
        var scene = new Rectangle(0, 0, size, size);
        var blurBounds = new Rectangle(0, 0, blurred.Width, blurred.Height);
        var excluded = new Rectangle(24, 88, 48, 32);

        device.SetRenderTarget(maskSurface);
        device.Clear(Color.FromArgb(0, 0, 0, 0));
        device.PixelShader = ShaderEffect.WeatherOcclusion;
        device.Sprite.Draw(sprite, new Rectangle(0, 0, 4, 1), new Rectangle(12, 12, 40, 28), Vector4.One);
        device.Sprite.Draw(dither, new Rectangle(0, 0, 4, 4), new Rectangle(0, 12, 16, 16), Vector4.One);
        device.PixelShader = ShaderEffect.WeatherTerrainMask;
        device.Blend = SpriteBlendMode.Maximum;
        device.SetPixelShaderConstant(0, [new Vector4(tileFeather, 0, 0, 0)]);
        device.Ui.Fill(Rectangle.Inflate(excluded, tileFeather, tileFeather),
            new Vector4(excluded.X + excluded.Width / 2f, excluded.Y + excluded.Height / 2f, excluded.Width / 2f, excluded.Height / 2f));

        device.SetRenderTarget(blurredSurface);
        mask.GenerateMips();
        device.Blend = SpriteBlendMode.Alpha;
        device.PixelShader = ShaderEffect.WeatherSceneryBlur;
        device.SetPixelShaderConstant(0, [new(6f / size, 6f / size, 2.8f, 0)]);
        device.Sprite.Draw(mask, scene, blurBounds, Vector4.One);

        Color ground = Color.FromArgb(22, 25, 23);
        device.SetRenderTarget(weatherSurface);
        device.Clear(ground);
        device.PixelShader = ShaderEffect.WeatherGround;
        device.SetPixelShaderConstant(0, [Vector4.Zero, new(0, 0, 3, 0), Vector4.Zero]);
        device.Ui.Fill(scene, Color.White);
        using var unmasked = device.Capture(weatherSurface);

        device.Clear(ground);
        device.AuxiliaryTexture = mask;
        device.SetPixelShaderConstant(3, [new(0, 0, 1, 0), Vector4.Zero]);
        device.Sprite.Draw(blurred, blurBounds, scene, Vector4.One);
        using var image = device.Capture(weatherSurface);
        Check(Enumerable.Range(12, 28).Any(y => image.GetPixel(26, y).B > 45),
            "Fog must cover the translucent baked shadow pixels.");
        Near(unmasked.GetPixel(9, 21), image.GetPixel(9, 21), "Fog must cover legacy checkerboard shadows.");
        Near(ground, image.GetPixel(36, 24), "Fog must follow the solid foreground sprite.");
        Near(ground, image.GetPixel(46, 24), "Opaque black artwork must still block fog.");
        Near(ground, image.GetPixel(42, 104), "Forbidden ground must block fog regardless of scenery coverage.");
        int openFog = unmasked.GetPixel(80, 104).B - ground.B;
        int tileFade = image.GetPixel(80, 104).B - ground.B;
        Check(openFog > 30 && tileFade < openFog * 0.65f,
            $"The blocked-tile fade must extend beyond the old square rim. Open {openFog}, masked {tileFade}.");
        Near(unmasked.GetPixel(110, 104), image.GetPixel(110, 104), "Tile fade must end within the requested radius.");
        int feather = image.GetPixel(30, 24).B;
        Check(feather > ground.B && feather < unmasked.GetPixel(30, 24).B - 10,
            "Fog must gradually thin on the open side of the scenery edge.");

        device.Clear(ground);
        device.SetPixelShaderConstant(4, [new(1, 0, 0, 0)]);
        device.Sprite.Draw(blurred, blurBounds, scene, Vector4.One);
        using var natural = device.Capture(weatherSurface);
        Check(Enumerable.Range(90, 20).Any(y => Math.Abs(natural.GetPixel(84, y).B - image.GetPixel(84, y).B) > 10),
            "Natural edges must break up a uniform fade along the tile boundary.");
        Near(ground, natural.GetPixel(71, 104), "Natural breakup must never expose forbidden ground.");

        device.Clear(ground);
        device.PixelShader = ShaderEffect.WeatherParticle;
        Vector4 drop = RendererDevice.ToVector(Color.FromArgb(230, 0, 220, 0));
        device.SetPixelShaderConstant(4, [Vector4.Zero]);
        device.Sprite.Draw(blurred, blurBounds, new Rectangle(78, 98, 5, 8), drop);
        device.Sprite.Draw(blurred, blurBounds, new Rectangle(40, 98, 5, 8), drop);
        using var hardRain = device.Capture(weatherSurface);
        device.Clear(ground);
        device.SetPixelShaderConstant(4, [new(0, 1, 0, 0)]);
        device.Sprite.Draw(blurred, blurBounds, new Rectangle(78, 98, 5, 8), drop);
        using var softRain = device.Capture(weatherSurface);
        Check(hardRain.GetPixel(80, 103).B > softRain.GetPixel(80, 103).B + 30,
            "Rain fade must be adjustable separately from the ground effects.");
        Near(ground, hardRain.GetPixel(42, 103), "Rain must respect forbidden ground even with drop fade disabled.");

        device.Clear(ground);
        device.PixelShader = ShaderEffect.WeatherMaskView;
        device.SetPixelShaderConstant(4, [new(0, 0, 3, 0)]);
        device.Sprite.Draw(blurred, blurBounds, scene, Vector4.One);
        using var debug = device.Capture(weatherSurface);
        Check(debug.GetPixel(36, 24).B > 170 && debug.GetPixel(42, 104).R > 170,
            "The mask view must distinguish scenery silhouettes and blocked tiles.");
        int side = debug.GetPixel(80, 104).R, corner = debug.GetPixel(80, 79).R;
        Check(side > corner + 7 && corner > ground.R + 15,
            $"The fade must round the tile corner using distance. Side {side}, corner {corner}.");
        device.AuxiliaryTexture = null;
        device.PixelShader = ShaderEffect.Artwork;
        device.SetPixelShaderConstant(3, [Vector4.Zero, Vector4.Zero]);
        device.SetRenderTarget(target);
    }

    private static void TestSpellDepth(RendererDevice device, Surface target, Texture red)
    {
        using var mask = Texture.CreateSpellOcclusion(device, 64, 64);
        using var maskSurface = mask.GetSurface();
        var source = new Rectangle(0, 0, red.Width, red.Height);
        device.SetRenderTarget(maskSurface);
        device.Clear(Color.Black);
        device.PixelShader = ShaderEffect.SpellOcclusion;
        device.Blend = SpriteBlendMode.Maximum;
        device.Sprite.Draw(red, source, new Rectangle(0, 0, 20, 64), Vector4.One, depth: 3);
        device.Sprite.Draw(red, source, new Rectangle(20, 0, 20, 64), new Vector4(1, 1, 1, .5f), depth: 3);
        device.Sprite.Draw(red, source, new Rectangle(40, 0, 24, 64), Vector4.One, depth: 1);

        device.SetRenderTarget(target);
        device.Clear(Color.Black);
        SpellParticle[] particles = [Spark(10), Spark(30), Spark(52)];
        device.SpellParticles.Draw(particles, Vector2.Zero, 1000, ground: false, emissiveOverlay: true, occlusion: mask);
        using var image = device.Capture(target);
        Near(Color.Black, image.GetPixel(10, 30), "A later world row must occlude a spell highlight.");
        Check(image.GetPixel(30, 30).G is > 70 and < 170 && image.GetPixel(52, 30).G > 200,
            "A partial sprite must soften spell coverage while an earlier row leaves it visible.");

        device.Clear(Color.Black);
        device.SpellParticles.Draw(particles, Vector2.Zero, 1000, ground: false, emission: true, occlusion: mask);
        using var emission = device.Capture(target);
        Check(emission.GetPixel(10, 30).G > 130, "The spell still lights a foreground surface covering its particles.");
        device.Blend = SpriteBlendMode.Alpha;
        device.PixelShader = ShaderEffect.Artwork;

        static SpellParticle Spark(int x) => new(new Vector2(x, 30), Vector2.Zero, 1000, 250, 14, Color.White,
            SpellParticleShape.Orb, 0, Layer: SpellParticleLayer.EmissiveCore, DepthY: 64);
    }

    private static void TestShapedLight(RendererDevice device, Surface target, Texture red)
    {
        using var shape = new Texture(device, 32, 32, true);
        using var shapeSurface = shape.GetSurface();
        using var horizontal = new Texture(device, 32, 32, true);
        using var horizontalSurface = horizontal.GetSurface();
        var rect = new Rectangle(0, 0, 32, 32);

        device.SetRenderTarget(shapeSurface);
        device.Clear(Color.Black);
        device.PixelShader = ShaderEffect.ShapedLight;
        device.Blend = SpriteBlendMode.Additive;
        device.SetPixelShaderConstant(0, [new(1, 0, 0, 0)]);
        device.Sprite.Draw(red, new Rectangle(0, 0, 2, 2), new Rectangle(12, 12, 8, 8), new Vector4(0, 1, 0, 1));
        using (var image = device.Capture(shapeSurface))
        {
            Near(Color.Lime, image.GetPixel(12, 14), "Custom tint must recolour the source without keeping its original hue.");
            Near(Color.Black, image.GetPixel(11, 14), "A larger frame must keep a crisp edge with blur at zero.");
        }

        device.SetRenderTarget(horizontalSurface);
        device.PixelShader = ShaderEffect.GaussianLight;
        device.Blend = SpriteBlendMode.Alpha;
        device.SetPixelShaderConstant(0, [new(1f / 32, 0, 4, 0.154f)]);
        device.Sprite.Draw(shape, rect, rect, Vector4.One);

        device.SetRenderTarget(target);
        device.Clear(Color.Black);
        device.SetPixelShaderConstant(0, [new(0, 1f / 32, 4, 0.154f)]);
        device.Sprite.Draw(horizontal, rect, rect, Vector4.One);
        using (var image = device.Capture(target))
        {
            Color centre = image.GetPixel(16, 16), side = image.GetPixel(10, 16), diagonal = image.GetPixel(10, 10);
            Check(centre.G > side.G && side.G > diagonal.G && diagonal.G > 0,
                "The Gaussian must extend smoothly along both axes around the scaled shape.");
            Near(Color.Black, image.GetPixel(5, 5), "The Gaussian must leave unrelated pixels dark.");
        }
        device.PixelShader = ShaderEffect.Artwork;
    }

    private static void TestMapAmbient(RendererDevice device, Surface target, Texture darkEmission)
    {
        using var terrain = new Texture(device, 32, 32, true);
        using var terrainSurface = terrain.GetSurface();
        using var reduced = new Texture(device, 8, 8, true);
        using var reducedSurface = reduced.GetSurface();
        using var mean = new Texture(device, 1, 1, true);
        using var meanSurface = mean.GetSurface();

        device.SetRenderTarget(terrainSurface);
        device.Clear(Color.FromArgb(70, 135, 48));
        device.Ui.Fill(new Rectangle(0, 0, 32, 16), Color.FromArgb(30, 185, 72));
        device.SetRenderTarget(reducedSurface);
        device.PixelShader = ShaderEffect.MapColourDownsample;
        device.Sprite.Draw(terrain, new Rectangle(0, 0, 32, 32), new Rectangle(0, 0, 8, 8), Vector4.One);
        device.SetRenderTarget(meanSurface);
        device.PixelShader = ShaderEffect.MapColourAverage;
        device.SetPixelShaderConstant(0, [Vector4.One]);
        device.Sprite.Draw(reduced, new Rectangle(0, 0, 8, 8), new Rectangle(0, 0, 1, 1), Vector4.One);
        using (var image = device.Capture(meanSurface))
            Near(Color.FromArgb(50, 160, 60), image.GetPixel(0, 0), "Terrain reduction must average the visible scene colours.");

        device.SetRenderTarget(target);
        device.Clear(Color.White);
        device.PixelShader = ShaderEffect.LightComposite;
        device.Blend = SpriteBlendMode.Multiply;
        device.AuxiliaryTexture = mean;
        device.SetPixelShaderConstant(0, [new(0, 0, 0, 1), new(0.15f, 0, 0, 0)]);
        device.Sprite.Draw(darkEmission, null, Vector3.Zero, Vector4.One);
        using (var image = device.Capture(target))
        {
            Color night = image.GetPixel(2, 2);
            Check(night.G > 35 && night.G > night.R * 2 && night.G > night.B * 2,
                "A green scene must give even a black night preset a green ambient colour.");
        }
        device.AuxiliaryTexture = null;
        device.PixelShader = ShaderEffect.Artwork;
        device.Blend = SpriteBlendMode.Alpha;
    }

    private static void TestHdrHalo(RendererDevice device, Surface target)
    {
        using var light = new Texture(device, 64, 64, true, hdr: true);
        using var lightSurface = light.GetSurface();
        var bounds = new Rectangle(0, 0, 64, 64);
        device.SetRenderTarget(lightSurface);
        device.Clear(Color.Black);
        device.Blend = SpriteBlendMode.Additive;
        device.PixelShader = ShaderEffect.LightHalo;
        device.Ui.Fill(bounds, new Vector4(3, 0.6f, 0, 1));
        device.SetRenderTarget(target);
        device.Clear(Color.Black);
        device.Blend = SpriteBlendMode.Alpha;
        device.PixelShader = ShaderEffect.BloomBlur;
        device.SetPixelShaderConstant(0, [Vector4.Zero, new(0.2f, 0, 0, 0)]);
        device.Sprite.Draw(light, bounds, bounds, Vector4.One);
        using var image = device.Capture(target);
        Color centre = image.GetPixel(32, 32), falloff = image.GetPixel(54, 32);
        Check(centre.R > 140 && falloff.R is > 10 and < 60,
            $"Halo intensity above 100% must survive in the light buffer. Centre {centre}, falloff {falloff}.");
        Near(Color.Black, image.GetPixel(2, 2), "The halo must fall to zero outside its radius.");
        device.PixelShader = ShaderEffect.Artwork;
    }

    private static void TestLightSpill(RendererDevice device, Surface target)
    {
        using var light = new Texture(device, 64, 64, true, hdr: true);
        using var lightSurface = light.GetSurface();
        var bounds = new Rectangle(0, 0, 64, 64);
        device.SetRenderTarget(lightSurface);
        device.Clear(Color.Black);
        device.Ui.Fill(new Rectangle(0, 0, 64, 32), new Vector4(3, 2, 1, 1));

        device.SetRenderTarget(target);
        Color terrain = Color.FromArgb(215, 193, 153);
        Color scenery = Color.FromArgb(95, 73, 38);
        device.Clear(terrain);
        device.Ui.Fill(new Rectangle(32, 0, 32, 64), scenery);
        device.PixelShader = ShaderEffect.LightComposite;
        device.Blend = SpriteBlendMode.Screen;
        device.SetPixelShaderConstant(0, [Vector4.Zero, new(0, 0, 0.22f, 0)]);
        device.Sprite.Draw(light, bounds, bounds, Vector4.One);
        using var image = device.Capture(target);
        Color litTerrain = image.GetPixel(16, 16), litScenery = image.GetPixel(48, 16);
        Check(litTerrain.R > terrain.R && litTerrain.R < 245,
            "A 300% HDR emitter must brighten daylight without clipping bright ground.");
        Check(litScenery.R > scenery.R + 15 && litScenery.R < litTerrain.R - 70,
            "The same spill must light foreground art and keep its contrast with the ground.");
        Near(terrain, image.GetPixel(16, 48), "The spill must affect only pixels reached by the emitter.");
        device.Blend = SpriteBlendMode.Alpha;
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

        foreach (var (mode, expected) in new[]
        {
            (SpriteBlendMode.Maximum, Color.FromArgb(90, 140, 220)),
            (SpriteBlendMode.Minimum, Color.FromArgb(40, 140, 137))
        })
        {
            device.Clear(Color.FromArgb(40, 140, 220));
            device.ArtworkBlend = mode;
            device.Sprite.Draw(translucent, null, Vector3.Zero, Vector4.One);
            device.ArtworkBlend = null;
            using var image = device.Capture(target);
            Near(expected, image.GetPixel(0, 0), $"Incorrect channel blend for {mode}.");
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

        device.TextureFilter = TextureFilterMode.Cubic;
        device.Clear(Color.Black);
        device.Sprite.Draw(atlas, source, destination, Vector4.One);
        using (var image = device.Capture(target))
        {
            Color edge = image.GetPixel(2, 2);
            Check(edge.R is > 30 and < 250 && edge.R == edge.G, "Bicubic reconstruction must smooth diagonal coverage.");
            Near(Color.Black, image.GetPixel(5, 1), "Bicubic taps must not pull colour from another atlas region.");
        }
        device.TextureAdjustment = new Vector3(0.08f, 0, 0);
        device.Clear(Color.Black);
        device.Sprite.Draw(atlas, source, destination, Vector4.One);
        using (var image = device.Capture(target))
            Near(Color.Black, image.GetPixel(2, 2), "Zero filter mix must restore nearest-neighbour output.");
        device.TextureAdjustment = new Vector3(0.08f, 1, 0);

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
