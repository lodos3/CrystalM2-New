using System.Drawing;
using System.Numerics;
using Client.MirGraphics;
using Client.Rendering;
using CrystalM2.Rendering.Core;

namespace CrystalM2.ParticleStudio;

internal sealed class ReferenceLighting : IDisposable
{
    private readonly Dictionary<int, Texture> _radial = new();
    private Texture? _emission, _occlusion;
    private Surface? _emissionSurface, _occlusionSurface;

    public Texture Mask(RendererDevice device, ReferenceScene scene)
    {
        EnsureTargets(device);
        Surface world = device.CurrentSurface;
        device.SetRenderTarget(_occlusionSurface);
        device.Clear(Color.Black);
        device.PixelShader = ShaderEffect.SpellOcclusion;
        device.Blend = SpriteBlendMode.Maximum;
        scene.DrawObjects(device, depthMask: true);
        device.SetRenderTarget(world);
        device.PixelShader = ShaderEffect.Artwork;
        device.Blend = SpriteBlendMode.Alpha;
        return _occlusion!;
    }

    public void Composite(RendererDevice device, ReferenceScene scene, ReadOnlySpan<SpellParticle> particles,
        int time, LightSetting lighting)
    {
        EnsureTargets(device);
        Surface world = device.CurrentSurface;
        device.SetRenderTarget(_emissionSurface);
        device.Clear(Color.Black);
        device.Blend = SpriteBlendMode.Additive;
        foreach (var light in scene.Lights)
        {
            int index = Math.Clamp(light.Index, 1, DXManager.LightSizes.Length - 1);
            if (!_radial.TryGetValue(index, out var texture))
            {
                Point size = DXManager.LightSizes[index];
                texture = DXManager.CreateRadialLight(device, size.X, size.Y);
                _radial.Add(index, texture);
            }
            var position = light.Position - new Vector2(texture.Width / 2f + 24, texture.Height / 2f + 21);
            device.Sprite.Draw(texture, null, new Vector3(position, 0), RendererDevice.ToVector(light.Colour));
        }
        device.SpellParticles.Draw(particles, Vector2.Zero, time, ground: true, intensity: .22f, emission: true);
        device.SpellParticles.Draw(particles, Vector2.Zero, time, ground: false, intensity: .22f, emission: true);
        device.SetRenderTarget(world);

        Color ambient = AmbientLightTransition.Target(lighting, mapDarkLight: 2);
        device.PixelShader = ShaderEffect.LightComposite;
        if (ambient.ToArgb() != Color.White.ToArgb())
        {
            device.Blend = SpriteBlendMode.Multiply;
            device.SetPixelShaderConstant(0, [new(ambient.R / 255f, ambient.G / 255f, ambient.B / 255f, 0), Vector4.Zero]);
            device.Sprite.Draw(_emission, null, Vector3.Zero, Vector4.One);
        }
        device.Blend = SpriteBlendMode.Screen;
        device.SetPixelShaderConstant(0, [Vector4.Zero, new(0, 0, .36f, 0)]);
        device.Sprite.Draw(_emission, null, Vector3.Zero, Vector4.One);
        device.Sprite.Flush();
        device.PixelShader = ShaderEffect.Artwork;
        device.Blend = SpriteBlendMode.Alpha;
    }

    private void EnsureTargets(RendererDevice device)
    {
        int width = device.TargetWidth, height = device.TargetHeight;
        if (_emission?.Width == width && _emission.Height == height) return;
        DisposeTargets();
        _emission = new Texture(device, width, height, renderTarget: true, hdr: true);
        _emissionSurface = _emission.GetSurface();
        _occlusion = Texture.CreateSpellOcclusion(device, width, height);
        _occlusionSurface = _occlusion.GetSurface();
    }

    private void DisposeTargets()
    {
        _emissionSurface?.Dispose();
        _emission?.Dispose();
        _occlusionSurface?.Dispose();
        _occlusion?.Dispose();
    }

    public void Dispose()
    {
        DisposeTargets();
        foreach (var texture in _radial.Values) texture.Dispose();
        _radial.Clear();
    }
}
