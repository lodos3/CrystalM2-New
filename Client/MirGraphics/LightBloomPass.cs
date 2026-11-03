using System;
using System.Drawing;
using System.IO;
using SlimDX;
using SlimDX.Direct3D9;

namespace Client.MirGraphics
{
    // Optional Direct3D9 bloom. One reduced-size texture plus two shader draws.
    // No CPU/GPU readback and no extra full-resolution render target.
    internal static class LightBloomPass
    {
        private static Texture _texture;
        private static Surface _surface;
        private static PixelShader _extractShader, _blurShader;
        private static Size _size;
        private static bool _attemptedShaders, _failed;
        private static readonly Vector4[] ExtractConstants =
        {
            new Vector4(0.58f, 3.0f, 0f, 1f)
        };
        private static readonly Vector4[] BlurConstants =
        {
            new Vector4(0f, 0f, 0f, 0f),
            new Vector4(0.115f, 1f, 0f, 0f)
        };

        private static bool LoadShaders()
        {
            if (_attemptedShaders)
                return !_failed && _extractShader != null && _blurShader != null;
            _attemptedShaders = true;
            try
            {
                string extractPath = Path.Combine(Settings.ShadersPath, "bloom-extract.ps");
                string blurPath = Path.Combine(Settings.ShadersPath, "bloom-blur.ps");
                if (!File.Exists(extractPath) || !File.Exists(blurPath))
                {
                    _failed = true;
                    return false;
                }

                using (ShaderBytecode shader = ShaderBytecode.AssembleFromFile(extractPath, ShaderFlags.None))
                    _extractShader = new PixelShader(DXManager.Device, shader);
                using (ShaderBytecode shader = ShaderBytecode.AssembleFromFile(blurPath, ShaderFlags.None))
                    _blurShader = new PixelShader(DXManager.Device, shader);
                return true;
            }
            catch (Exception ex)
            {
                _failed = true;
                _extractShader?.Dispose();
                _blurShader?.Dispose();
                _extractShader = _blurShader = null;
                CMain.SaveError("Optional bloom shaders disabled: " + ex);
                return false;
            }
        }

        private static bool EnsureTarget()
        {
            Size size = BloomBufferLayout.Resolve(
                Settings.ScreenWidth, Settings.ScreenHeight, Settings.BloomDivisor);
            if (size.IsEmpty) return false;
            if (_texture != null && !_texture.Disposed && _surface != null &&
                !_surface.Disposed && size == _size)
                return true;

            ReleaseTarget();
            _texture = new Texture(DXManager.Device, size.Width, size.Height, 1,
                                   Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
            _surface = _texture.GetSurfaceLevel(0);
            _size = size;
            return true;
        }

        internal static void Composite()
        {
            if (!Settings.LightingBloom || DXManager.DeviceLost ||
                DXManager.LightTexture == null || DXManager.LightTexture.Disposed ||
                !LoadShaders())
                return;

            Surface originalSurface = DXManager.CurrentSurface;
            Matrix originalTransform = DXManager.Sprite.Transform;
            PixelShader originalShader = DXManager.Device.PixelShader;
            bool originalBlend = DXManager.Blending;
            float originalRate = DXManager.BlendingRate;
            BlendMode originalMode = DXManager.BlendingMode;
            bool failed = false;

            try
            {
                if (!EnsureTarget()) return;

                // Downsample the light mask, extracting only bright pixels.
                DXManager.SetSurface(_surface);
                DXManager.Device.Clear(ClearFlags.Target, Color.Black, 0f, 0);
                DXManager.Sprite.Flush();
                DXManager.Device.PixelShader = _extractShader;
                DXManager.Device.SetPixelShaderConstant(0, ExtractConstants);
                DXManager.Sprite.Transform = Matrix.Scaling(
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
                DXManager.Device.PixelShader = _blurShader;
                DXManager.Device.SetPixelShaderConstant(0, BlurConstants);
                DXManager.Sprite.Transform = Matrix.Scaling(
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
            if (_extractShader != null && !_extractShader.Disposed) _extractShader.Dispose();
            if (_blurShader != null && !_blurShader.Disposed) _blurShader.Dispose();
            _extractShader = _blurShader = null;
            _attemptedShaders = false;
            _failed = false;
        }
    }
}
