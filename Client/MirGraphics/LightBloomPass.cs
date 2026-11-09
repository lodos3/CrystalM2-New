using System;
using System.Drawing;

namespace Client.MirGraphics
{
    // Two reduced-resolution targets: extract emitted light, horizontal blur,
    // then vertical blur while compositing. Baked map RGB never feeds bloom.
    internal static class LightBloomPass
    {
        private static Texture _texture, _blurTexture;
        private static Surface _surface, _blurSurface;
        private static Size _size;
        private static bool _failed;
        private static readonly Vector4[] ExtractConstants =
        {
            new Vector4(0.58f, 3.0f, 0f, 1f)
        };
        private static readonly Vector4[] BlurConstants = new Vector4[2];

        private static bool EnsureTarget()
        {
            Size size = BloomBufferLayout.Resolve(
                Settings.ScreenWidth, Settings.ScreenHeight, Settings.BloomDivisor);
            if (size.IsEmpty) return false;
            if (_texture != null && !_texture.Disposed &&
                _blurTexture != null && !_blurTexture.Disposed && size == _size)
                return true;

            ReleaseTarget();
            _texture = new Texture(DXManager.Device, size.Width, size.Height, true);
            _surface = _texture.GetSurface();
            _blurTexture = new Texture(DXManager.Device, size.Width, size.Height, true);
            _blurSurface = _blurTexture.GetSurface();
            _size = size;
            return true;
        }

        internal static void Composite()
        {
            if (!Settings.LightingBloom || Settings.BloomIntensityPercent == 0)
            {
                ReleaseTarget();
                return;
            }
            if (DXManager.DeviceLost || _failed || DXManager.LightTexture == null ||
                DXManager.LightTexture.Disposed) return;

            Surface originalSurface = DXManager.CurrentSurface;
            Matrix originalTransform = DXManager.Sprite.Transform;
            ShaderEffect originalShader = DXManager.Device.PixelShader;
            SpriteBlendMode originalBlend = DXManager.Device.Blend;
            float originalRate = DXManager.Device.BlendRate;
            float originalOpacity = DXManager.Device.Opacity;
            Span<Vector4> originalConstants = stackalloc Vector4[RendererDevice.EffectConstantCount];
            DXManager.Device.CopyPixelShaderConstants(originalConstants);
            bool failed = false;

            try
            {
                if (!EnsureTarget()) return;
                DXManager.Device.Opacity = 1;
                DXManager.Device.Blend = SpriteBlendMode.Alpha;

                DXManager.SetSurface(_surface);
                DXManager.Device.Clear(Color.Black);
                DXManager.Device.PixelShader = ShaderEffect.BloomExtract;
                ExtractConstants[0].X = GraphicsTuning.BloomThreshold.Current / 100f;
                DXManager.Device.SetPixelShaderConstant(0, ExtractConstants);
                DXManager.Sprite.Transform = Matrix.CreateScale(
                    (float)_size.Width / Settings.ScreenWidth,
                    (float)_size.Height / Settings.ScreenHeight, 1f);
                DXManager.Draw(DXManager.LightTexture,
                    new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight),
                    Vector3.Zero, Color.White);

                DXManager.SetSurface(_blurSurface);
                DXManager.Device.Clear(Color.Black);
                DXManager.Device.PixelShader = ShaderEffect.BloomBlur;
                BlurConstants[0] = new Vector4(1f / _size.Width, 0, 0, 0);
                BlurConstants[1] = new Vector4(1, 0, 0, 0);
                DXManager.Device.SetPixelShaderConstant(0, BlurConstants);
                DXManager.Sprite.Transform = Matrix.Identity;
                DXManager.Draw(_texture, new Rectangle(Point.Empty, _size), Vector3.Zero, Color.White);

                DXManager.SetSurface(originalSurface);
                DXManager.Device.Blend = SpriteBlendMode.Additive;
                BlurConstants[0] = new Vector4(0, 1f / _size.Height, 0, 0);
                BlurConstants[1] = new Vector4(0.575f * Settings.BloomIntensityPercent / 100f, 0, 0, 0);
                DXManager.Device.SetPixelShaderConstant(0, BlurConstants);
                DXManager.Sprite.Transform = Matrix.CreateScale(
                    (float)Settings.ScreenWidth / _size.Width,
                    (float)Settings.ScreenHeight / _size.Height, 1f);
                DXManager.Draw(_blurTexture, new Rectangle(Point.Empty, _size), Vector3.Zero, Color.White);
                DXManager.Sprite.Flush();
            }
            catch (Exception ex)
            {
                failed = _failed = true;
                DXManager.Device.DiscardPending();
                CMain.SaveError("Optional D3D11 light bloom disabled: " + ex);
            }
            finally
            {
                DXManager.Sprite.Transform = originalTransform;
                DXManager.SetSurface(originalSurface);
                DXManager.Device.PixelShader = originalShader;
                DXManager.Device.SetPixelShaderConstant(0, originalConstants);
                DXManager.Device.Blend = originalBlend;
                DXManager.Device.BlendRate = originalRate;
                DXManager.Device.Opacity = originalOpacity;
                if (failed) ReleaseTarget();
            }
        }

        private static void ReleaseTarget()
        {
            _surface?.Dispose();
            _blurSurface?.Dispose();
            _texture?.Dispose();
            _blurTexture?.Dispose();
            _surface = _blurSurface = null;
            _texture = _blurTexture = null;
            _size = Size.Empty;
        }

        internal static void DisposeAll()
        {
            ReleaseTarget();
            _failed = false;
        }
    }
}
