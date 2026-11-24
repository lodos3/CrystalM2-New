using System.Numerics;
using CrystalM2.Rendering.Core;

namespace Client.Rendering;

internal enum SpellMaskStyle { Sweep, Fire, Frost, Ward, Storm }

internal readonly record struct SpellParticleGrade(float Hue, float HighlightColour, float SmokeOpacity,
    float EdgeSoftness, float Detail)
{
    internal static SpellParticleGrade Neutral => new(0, 0, 1, 0, 0);
}

internal readonly struct ParticlePalette(SpellParticleGrade grade, float saturation)
{
    private readonly float _sin = MathF.Sin(grade.Hue), _cos = MathF.Cos(grade.Hue);

    internal Vector3 Apply(Vector3 rgb, SpellParticleLayer layer)
    {
        if (grade.Hue != 0)
        {
            Vector3 axis = new(.57735027f);
            rgb = rgb * _cos + Vector3.Cross(axis, rgb) * _sin + axis * (Vector3.Dot(axis, rgb) * (1 - _cos));
        }
        float chroma = saturation * (layer == SpellParticleLayer.EmissiveCore ? 1 + grade.HighlightColour : 1);
        if (chroma == 1) return Vector3.Max(rgb, Vector3.Zero);
        Vector3 grey = new(Vector3.Dot(rgb, new Vector3(.2126f, .7152f, .0722f)));
        return Vector3.Max(grey + (rgb - grey) * chroma, Vector3.Zero);
    }
}

internal sealed class SpellParticleRenderer
{
    private readonly RendererDevice _device;
    private readonly Texture _atlas;
    private readonly Dictionary<int, (Texture Texture, long LastUse)> _wingPages = new();
    private long _wingUse;

    private Texture WingPage(int index)
    {
        if (_wingPages.TryGetValue(index, out var cached))
        {
            _wingPages[index] = (cached.Texture, ++_wingUse);
            return cached.Texture;
        }
        if (_wingPages.Count >= 24)
        {
            var oldest = _wingPages.MinBy(entry => entry.Value.LastUse);
            _device.Sprite.Flush();
            _wingPages.Remove(oldest.Key);
            oldest.Value.Texture.Dispose();
        }
        var texture = Texture.CreateCoverageAtlas(_device, WingPoseAtlas.PageSize, WingPoseAtlas.PageSize, 1);
        try
        {
            texture.BeginUpload().Data.Write(WingPoseAtlas.ReadPage(index));
            texture.EndUpload();
            _wingPages.Add(index, (texture, ++_wingUse));
            return texture;
        }
        catch { texture.Dispose(); throw; }
    }

    public SpellParticleRenderer(RendererDevice device)
    {
        _device = device;
        _atlas = Texture.CreateCoverageAtlas(device, SpellParticleAtlas.Width, SpellParticleAtlas.Height, SpellParticleAtlas.MipLevels);
        try
        {
            DataRectangle upload = _atlas.BeginUpload();
            upload.Data.Write(SpellParticleAtlas.Generate());
            _atlas.EndUpload();
            // WingPage uploads each prepainted appearance on first display.
            // Casting a spell only needs the shared particle atlas.
        }
        catch
        {
            foreach (var page in _wingPages.Values) page.Texture.Dispose();
            _wingPages.Clear();
            _atlas.Dispose();
            throw;
        }
    }

    public void Draw(ReadOnlySpan<SpellParticle> particles, Vector2 camera, long now, bool ground,
                     float intensity = 1f, bool emission = false, float sizeScale = 1f, float warmth = 0f,
                     Rectangle? clipping = null, Vector3? colour = null, bool emissiveOverlay = false,
                     float exposure = 1f, float saturation = 1f, SpellParticleGlowPolicy glowPolicy = default,
                     Texture occlusion = null, int depthOrigin = 0, int cellHeight = 32, float lightRadius = 1f,
                     SpellParticleGrade? grade = null)
    {
        if (intensity <= 0) return;
        SpriteBlendMode previous = _device.Blend;
        SpriteBlendMode? previousArtworkBlend = _device.ArtworkBlend;
        ShaderEffect shader = _device.PixelShader;
        Texture previousAuxiliary = _device.AuxiliaryTexture;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        _device.CopyPixelShaderConstants(constants);
        var look = grade ?? SpellParticleGrade.Neutral;
        var palette = new ParticlePalette(look, saturation);
        Rectangle clip = clipping ?? new Rectangle(0, 0, _device.TargetWidth, _device.TargetHeight);
        // DrawLayer is a local function after try/finally, so its shared
        // animation and shader-mode state belongs in the enclosing Draw scope.
        float phase = now % 8000 * (MathF.Tau / 8000);
        int ribbonMode = 0; // 0 coverage, 1 blade cut, 2 crescent
        try
        {
            _device.PixelShader = ShaderEffect.ParticleCoverage;
            Matrix4x4.Invert(_device.Sprite.Transform, out Matrix4x4 inverse);
            _device.SetPixelShaderConstant(5,
            [
                new(inverse.M11, inverse.M21, inverse.M41 - camera.X, 4 * MathF.Sin(phase)),
                new(inverse.M12, inverse.M22, inverse.M42 - camera.Y, 3 * MathF.Cos(phase)),
                new(look.EdgeSoftness, look.Detail, 0, phase)
            ]);
            if (occlusion != null) _device.AuxiliaryTexture = occlusion;
            if (emission)
            {
                // Spell lighting uses the authored light mask, not the visual
                // compositing/glow controls. Avoid dimming the world's light.
                _device.Blend = SpriteBlendMode.Additive;
                DrawLayer(particles);
            }
            else if (ground)
            {
                if (glowPolicy.ShowAuthored)
                {
                    _device.Blend = SpriteBlendMode.Alpha;
                    DrawLayer(particles);
                }
                if (glowPolicy.ShowGlow) DrawGlowLayers(particles, groundParticles: true);
            }
            else if (emissiveOverlay)
            {
                if (glowPolicy.ShowAuthored)
                {
                    _device.Blend = SpriteBlendMode.Screen;
                    DrawLayer(particles, SpellParticleLayer.Glow);
                    _device.Blend = SpriteBlendMode.Alpha;
                    DrawLayer(particles, SpellParticleLayer.Emissive);
                    _device.Blend = SpriteBlendMode.Additive;
                    DrawLayer(particles, SpellParticleLayer.EmissiveCore);
                }
                if (glowPolicy.ShowGlow) DrawGlowLayers(particles, groundParticles: false);
            }
            else if (glowPolicy.ShowAuthored)
            {
                _device.Blend = SpriteBlendMode.Alpha;
                DrawLayer(particles, SpellParticleLayer.Smoke);
                DrawLayer(particles, SpellParticleLayer.World);
                _device.Blend = SpriteBlendMode.Additive;
                DrawLayer(particles, SpellParticleLayer.Legacy);
            }
        }
        finally
        {
            _device.ArtworkBlend = previousArtworkBlend;
            _device.Blend = previous;
            _device.PixelShader = shader;
            _device.AuxiliaryTexture = previousAuxiliary;
            _device.SetPixelShaderConstant(5, constants[5..]);
        }

        void DrawGlowLayers(ReadOnlySpan<SpellParticle> source, bool groundParticles)
        {
            // F12 Glow blend takes precedence over the general Particles blend
            // for this specific pass, then the caller's artwork state is restored.
            _device.ArtworkBlend = null;
            _device.Blend = glowPolicy.ScreenBlend ? SpriteBlendMode.Screen : SpriteBlendMode.Additive;
            if (groundParticles)
                DrawLayer(source, glowPass: true);
            else
            {
                DrawLayer(source, SpellParticleLayer.Glow, glowPass: true);
                DrawLayer(source, SpellParticleLayer.Emissive, glowPass: true);
                DrawLayer(source, SpellParticleLayer.EmissiveCore, glowPass: true);
            }
            _device.ArtworkBlend = previousArtworkBlend;
        }

        void DrawLayer(ReadOnlySpan<SpellParticle> layer, SpellParticleLayer? filter = null, bool glowPass = false)
        {
            var layerBlend = _device.ArtworkBlend;
            foreach (ref readonly SpellParticle particle in layer)
            {
                if (particle.CoveragePlane > 0 || particle.Ground != ground || (emission && !particle.CastsLight) ||
                    (filter.HasValue && particle.Layer != filter.Value)) continue;
                float age = particle.Age(now);
                Vector2 point = particle.Position(now) + camera;
                Vector2 size = particle.Dimensions(age, sizeScale);
                float ribbonScale = (emission ? lightRadius : 1f) * (particle.Ribbon.HasValue ? particle.RibbonScale(age) : 1f);
                float angle = particle.Angle(now);
                Rectangle bounds = particle.Bounds(point, size, ribbonScale, angle);
                if (!bounds.IntersectsWith(clip)) continue;

                Vector4 tint = RendererDevice.ToVector(particle.ColourAt(age));
                var rgb = colour ?? palette.Apply(new Vector3(tint.X, tint.Y, tint.Z), particle.Layer);
                if (warmth != 0) rgb *= new Vector3(1 + warmth, 1, 1 - warmth);
                tint = new Vector4(Vector3.Max(rgb, Vector3.Zero), tint.W);
                float glowAmount = glowPass ? glowPolicy.GlowAmount(new Vector3(tint.X, tint.Y, tint.Z)) : 1f;
                if (glowAmount <= 0f) continue;
                float alpha = particle.Fade(age);
                if (!emission && particle.Layer == SpellParticleLayer.Smoke) alpha *= look.SmokeOpacity;
                tint.W *= Math.Clamp(alpha * (emission ? 1 : intensity), 0, 1);
                if (emission)
                {
                    float strength = intensity * particle.LightEnergy * (particle.Emissive ? .65f : 1f);
                    tint.X *= strength;
                    tint.Y *= strength;
                    tint.Z *= strength;
                }
                else
                {
                    tint = new Vector4(new Vector3(tint.X, tint.Y, tint.Z) * (exposure * glowAmount), tint.W);
                }
                // The normal particle shader remains in place. Only actual
                // curved slash ribbon geometry opts into moving edge/sheen
                // shading, keeping sparks, smoke, runes and particle lights untouched.
                int desiredRibbonMode = !emission && particle.Ribbon.HasValue ? particle.Shape switch
                {
                    SpellParticleShape.BladeRibbon => 1,
                    SpellParticleShape.MoonRibbon => 2,
                    _ => 0
                } : 0;
                if (desiredRibbonMode != ribbonMode)
                {
                    _device.SetPixelShaderConstant(7,
                        [new Vector4(look.EdgeSoftness, look.Detail, desiredRibbonMode, phase)]);
                    ribbonMode = desiredRibbonMode;
                }
                var source = SpellParticleAtlas.Part(particle.Shape, particle.Direction, particle.AnimationFrame(age));
                Texture atlas = _atlas;
                if (particle.CoveragePlane < 0)
                {
                    var wing = WingPoseAtlas.Plane(particle.CoveragePlane);
                    source = wing.Source;
                    atlas = WingPage(wing.Page);
                }
                // These are independent channel masks. Blending one over another as alpha
                // would discard light from the earlier channels and change the frame hue.
                _device.ArtworkBlend = particle.CoveragePlane != 0 && !glowPass ? SpriteBlendMode.Additive : layerBlend;
                float depth = occlusion != null && !ground && !emission && !particle.AlwaysOnTop &&
                    (particle.DepthY.HasValue || particle.Motion.HasValue)
                    ? Math.Max(1, MathF.Floor(particle.DepthAt(now) / cellHeight) - depthOrigin)
                    : 0;
                // Negative depth carries the seed for overlay runes, which have no occlusion row.
                if (particle.AlwaysOnTop && particle.OpacityNoiseSeed != 0) depth = -particle.OpacityNoiseSeed;
                if (particle.Ribbon is { } ribbon)
                {
                    if (particle.RibbonTextureU is { } u)
                    {
                        int start = (int)MathF.Round(source.Width * u.X);
                        int end = (int)MathF.Round(source.Width * u.Y);
                        source.X += start;
                        source.Width = end - start;
                    }
                    _device.Sprite.DrawQuad(atlas, source, ribbon.Scaled(ribbonScale), point, tint, depth);
                }
                else if (particle.Emitter?.Settings.RenderMode is SpellEmitterRenderMode.QuadMesh or SpellEmitterRenderMode.TriangleMesh)
                {
                    Vector2 right = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (size.X * .5f);
                    Vector2 down = new Vector2(-MathF.Sin(angle), MathF.Cos(angle)) * (size.Y * .5f);
                    bool triangle = particle.Emitter.Settings.RenderMode == SpellEmitterRenderMode.TriangleMesh;
                    var mesh = new SpriteQuad(triangle ? -down : -right - down, triangle ? -down : right - down,
                        -right + down, right + down);
                    _device.Sprite.DrawQuad(atlas, source, mesh, point, tint, depth);
                }
                else
                    _device.Sprite.DrawRotated(atlas, source, point, size, angle, tint, depth);
            }
            _device.ArtworkBlend = layerBlend;
        }
    }

    internal void DrawSpriteMask(Texture mask, Rectangle destination, Vector4 tint, Vector4 core, long now,
        SpellMaskStyle style = SpellMaskStyle.Sweep, bool coverage = false)
    {
        if (tint.W <= 0) return;
        ShaderEffect shader = _device.PixelShader;
        SpriteBlendMode blend = _device.Blend;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        _device.CopyPixelShaderConstants(constants);
        try
        {
            _device.Blend = SpriteBlendMode.Additive;
            _device.PixelShader = ShaderEffect.WeaponSpellMask;
            _device.SetPixelShaderConstant(0, [new Vector4(now % 2000 * .012566371f, (int)style, coverage ? 1 : 0, 0), core]);
            _device.Sprite.Draw(mask, new Rectangle(0, 0, mask.Width, mask.Height), destination, tint);
        }
        finally
        {
            _device.PixelShader = shader;
            _device.Blend = blend;
            _device.SetPixelShaderConstant(0, constants);
        }
    }
}
