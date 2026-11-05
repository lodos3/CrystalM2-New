using System.Numerics;
using CrystalM2.Rendering.Core;

namespace Client.Rendering;

internal sealed class SpellParticleRenderer
{
    private readonly RendererDevice _device;
    private readonly Texture _atlas;

    public SpellParticleRenderer(RendererDevice device)
    {
        _device = device;
        _atlas = new Texture(device, SpellParticleAtlas.Width, SpellParticleAtlas.Height);
        try
        {
            DataRectangle upload = _atlas.BeginUpload();
            upload.Data.Write(SpellParticleAtlas.Generate());
            _atlas.EndUpload();
        }
        catch { _atlas.Dispose(); throw; }
    }

    public void Draw(ReadOnlySpan<SpellParticle> particles, Vector2 camera, long now, bool ground,
                     float intensity = 1f, bool emission = false, float sizeScale = 1f, float warmth = 0f)
    {
        if (intensity <= 0) return;
        SpriteBlendMode previous = _device.Blend;
        ShaderEffect shader = _device.PixelShader;
        try
        {
            _device.PixelShader = ShaderEffect.Artwork;
            _device.Blend = ground && !emission ? SpriteBlendMode.Alpha : SpriteBlendMode.Additive;
            foreach (ref readonly SpellParticle particle in particles)
            {
                if ((particle.Shape == SpellParticleShape.Halo) != ground) continue;
                float age = particle.Age(now);
                Vector2 point = particle.Position(now) + camera;
                int size = (int)(particle.Size * (ground ? .7f + age * .3f : 1 - age * .22f) * sizeScale);
                int height = ground ? size / 2 : size;
                var bounds = new Rectangle((int)point.X - size / 2, (int)point.Y - height / 2, size, height);
                if (bounds.Right <= 0 || bounds.Bottom <= 0 || bounds.Left >= _device.TargetWidth ||
                    bounds.Top >= _device.TargetHeight) continue;

                Vector4 tint = RendererDevice.ToVector(particle.Colour);
                tint.X = Math.Clamp(tint.X * (1 + warmth), 0, 1);
                tint.Z = Math.Clamp(tint.Z * (1 - warmth), 0, 1);
                tint.W *= Math.Clamp((1 - age) * (1 - age) * intensity, 0, 1);
                _device.Sprite.Draw(_atlas, SpellParticleAtlas.Part(particle.Shape, particle.Direction), bounds, tint);
            }
        }
        finally
        {
            _device.Blend = previous;
            _device.PixelShader = shader;
        }
    }
}
