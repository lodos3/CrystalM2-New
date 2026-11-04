using System;
using System.Drawing;
using System.IO;

namespace Client.MirGraphics
{
    // Optional D3D11 light-mask bloom. One reduced-size texture plus two shader draws.
    // No CPU/GPU readback and no extra full-resolution render target.
    internal static class LightBloomPass
    {
        private static Texture _texture;
        private static Surface _surface;
        private static Size _size;
        private static bool _failed;
        private static readonly Vector4[] ExtractConstants =
        {
            new Vector4(0.58f, 3.0f, 0f, 1f)
        };
        private static readonly Vector4[] BlurConstants =
        {
            new Vector4(0f, 0f, 0f, 0f),
            new Vector4(0.115f, 1f, 0f, 0f)
        };

        private static bool EnsureTarget()
        {
            Size size = BloomBufferLayout.Resolve(
                Settings.ScreenWidth, Settings.ScreenHeight, Settings.BloomDivisor);
            if (size.IsEmpty) return false;
            if (_texture != null && !_texture.Disposed && _surface != null &&
                !_surface.Disposed && size == _size)
                return true;

            ReleaseTarget();
            _texture = new Texture(DXManager.Device, size.Width, size.Height, true);
            _surface = _texture.GetSurface();
            _size = size;
            return true;
        }

        internal static void Composite()
        {
            if (!Settings.LightingBloom || DXManager.DeviceLost ||
                DXManager.LightTexture == null || DXManager.LightTexture.Disposed ||
                _failed)
                return;

            Surface originalSurface = DXManager.CurrentSurface;
            Matrix originalTransform = DXManager.Sprite.Transform;
            ShaderEffect originalShader = DXManager.Device.PixelShader;
            bool originalBlend = DXManager.Blending;
            float originalRate = DXManager.BlendingRate;
            BlendMode originalMode = DXManager.BlendingMode;
            bool failed = false;

            try
            {
                if (!EnsureTarget()) return;

                // Downsample the light mask, extracting only bright pixels.
                DXManager.SetSurface(_surface);
                DXManager.Device.Clear(Color.Black);
                DXManager.Sprite.Flush();
                DXManager.Device.PixelShader = ShaderEffect.BloomExtract;
                DXManager.Device.SetPixelShaderConstant(0, ExtractConstants);
                DXManager.Sprite.Transform = Matrix.CreateScale(
                    (float)_size.Width / Settings.ScreenWidth,
                    (float)_size.Height / Settings.ScreenHeight, 1f);
                DXManager.Draw(DXManager.LightTexture,
                    new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight),
                    Vector3.Zero, Color.White);
                DXManager.Sprite.Flush();

                // Blur while upsampling. Add only light colours to the world
                // already composed with ambient darkness, not the HUD.
                DXManager.Sprite.Transform = originalTransform;
                DXManager.Device.PixelShader = originalShader;
                DXManager.SetSurface(originalSurface);
                DXManager.SetBlend(true);
                DXManager.Sprite.Flush();

                BlurConstants[0] = new Vector4(1f / _size.Width, 0f, 0f, 1f / _size.Height);
                DXManager.Device.PixelShader = ShaderEffect.BloomBlur;
                DXManager.Device.SetPixelShaderConstant(0, BlurConstants);
                DXManager.Sprite.Transform = Matrix.CreateScale(
                    (float)Settings.ScreenWidth / _size.Width,
                    (float)Settings.ScreenHeight / _size.Height, 1f);
                DXManager.Draw(_texture, new Rectangle(0, 0, _size.Width, _size.Height),
                               Vector3.Zero, Color.White);
                DXManager.Sprite.Flush();
            }
            catch (Exception ex)
            {
                failed = true;
                _failed = true;
                CMain.SaveError("Optional light bloom pass disabled: " + ex);
            }
            finally
            {
                // Restore every renderer state this optional pass touched.
                DXManager.Sprite.Transform = originalTransform;
                DXManager.Device.PixelShader = originalShader;
                DXManager.SetSurface(originalSurface);
                DXManager.SetBlend(originalBlend, originalRate, originalMode);
                if (failed) ReleaseTarget();
            }
        }

        private static void ReleaseTarget()
        {
            if (_surface != null && !_surface.Disposed) _surface.Dispose();
            if (_texture != null && !_texture.Disposed) _texture.Dispose();
            _surface = null;
            _texture = null;
            _size = Size.Empty;
        }

        internal static void DisposeAll()
        {
            ReleaseTarget();
            _failed = false;
        }
    }
}
