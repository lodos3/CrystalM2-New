using System.Numerics;
using CrystalM2.Rendering.Core;

namespace Client.Rendering;

internal enum SpellMaskStyle { Sweep, Fire, Frost, Ward, Storm }

internal readonly record struct SpellParticleGrade(float Hue, float HighlightColour, float SmokeOpacity,
    float EdgeSoftness, float Detail)
{
    internal static SpellParticleGrade Neutral => new(0, 0, 1, 0, 0);
}

internal sealed class SpellParticleRenderer
{
    private readonly RendererDevice _device;
    private readonly Texture _atlas;

    public SpellParticleRenderer(RendererDevice device)
    {
        _device = device;
        _atlas = Texture.CreateCoverageAtlas(device, SpellParticleAtlas.Width, SpellParticleAtlas.Height, SpellParticleAtlas.MipLevels);
        try
        {
            DataRectangle upload = _atlas.BeginUpload();
            upload.Data.Write(SpellParticleAtlas.Generate());
            _atlas.EndUpload();
        }
        catch { _atlas.Dispose(); throw; }
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
        var look = emission ? SpellParticleGrade.Neutral : grade ?? SpellParticleGrade.Neutral;
        float sin = MathF.Sin(look.Hue), cos = MathF.Cos(look.Hue);
        Vector3 greyAxis = new(.57735027f);
        Rectangle clip = clipping ?? new Rectangle(0, 0, _device.TargetWidth, _device.TargetHeight);
        try
        {
            _device.PixelShader = ShaderEffect.ParticleCoverage;
            Matrix4x4.Invert(_device.Sprite.Transform, out Matrix4x4 inverse);
            float phase = now % 8000 * (MathF.Tau / 8000);
            int ribbonMode = 0; // 0 ordinary coverage, 1 blade cut, 2 crescent cut
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
            foreach (ref readonly SpellParticle particle in layer)
            {
                if (particle.Ground != ground || (emission && !particle.CastsLight) ||
                    (filter.HasValue && particle.Layer != filter.Value)) continue;
                float age = particle.Age(now);
                Vector2 point = particle.Position(now) + camera;
                Vector2 size = particle.Dimensions(age, sizeScale);
                float ribbonScale = emission ? lightRadius : 1f;
                float angle = particle.Angle(now);
                Rectangle bounds = particle.Bounds(point, size, ribbonScale, angle);
                if (!bounds.IntersectsWith(clip)) continue;

                Vector4 tint = RendererDevice.ToVector(particle.ColourAt(age));
                if (colour is { } rgb) tint = new Vector4(rgb, tint.W);
                tint.X = Math.Clamp(tint.X * (1 + warmth), 0, 1);
                tint.Z = Math.Clamp(tint.Z * (1 - warmth), 0, 1);
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
                    Vector3 visible = new(tint.X, tint.Y, tint.Z);
                    if (look.Hue != 0)
                        visible = visible * cos + Vector3.Cross(greyAxis, visible) * sin +
                            greyAxis * (Vector3.Dot(greyAxis, visible) * (1 - cos));
                    float luma = Vector3.Dot(visible, new Vector3(.2126f, .7152f, .0722f));
                    float chroma = saturation * (particle.Layer == SpellParticleLayer.EmissiveCore ? 1 + look.HighlightColour : 1);
                    visible = Vector3.Max(new Vector3(luma) + (visible - new Vector3(luma)) * chroma, Vector3.Zero);
                    visible *= exposure * glowAmount;
                    tint = new Vector4(visible, tint.W);
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
                var source = SpellParticleAtlas.Part(particle.Shape, particle.Direction, particle.Frame);
                float depth = occlusion != null && !ground && !emission && !particle.AlwaysOnTop &&
                    (particle.DepthY.HasValue || particle.Motion.HasValue)
                    ? Math.Max(1, MathF.Floor(particle.DepthAt(now) / cellHeight) - depthOrigin)
                    : 0;
                // Negative depth carries the seed for overlay runes, which have no occlusion row.
                if (particle.AlwaysOnTop && particle.OpacityNoiseSeed != 0) depth = -particle.OpacityNoiseSeed;
                if (particle.Ribbon is { } ribbon)
                    _device.Sprite.DrawQuad(_atlas, source, ribbon.Scaled(ribbonScale), point, tint, depth);
                else
                    _device.Sprite.DrawRotated(_atlas, source, point, size, angle, tint, depth);
            }
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
