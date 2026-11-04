using System;
using System.Drawing;
using Client.MirScenes;
using SlimDX;
using SlimDX.Direct3D9;

namespace Client.MirGraphics
{
    // Two slowly drifting translucent world layers. One 128x128 managed
    // noise texture is shared by both; no extra render targets are allocated.
    internal static class AtmosphereFogOverlay
    {
        private const int TilePixels = 128;
        private static readonly Rectangle TileRect = new Rectangle(0, 0, TilePixels, TilePixels);
        private static Texture _tile;
        private static bool _failed;

        private static bool EnsureTexture()
        {
            if (_failed) return false;
            if (_tile != null && !_tile.Disposed) return true;

            Texture candidate = null;
            try
            {
                candidate = new Texture(DXManager.Device, TilePixels, TilePixels, 1,
                                        Usage.None, Format.A8R8G8B8, Pool.Managed);
                DataRectangle data = candidate.LockRectangle(0, LockFlags.None);
                try
                {
                    unsafe
                    {
                        Span<byte> pixels = new Span<byte>((void*)data.Data.DataPointer,
                                                           checked(data.Pitch * TilePixels));
                        AtmosphereTileNoise.WriteBgra(pixels, TilePixels, TilePixels,
                                                     data.Pitch, 1947);
                    }
                }
                finally
                {
                    data.Data.Dispose();
                    candidate.UnlockRectangle(0);
                }
                _tile = candidate;
                return true;
            }
            catch (Exception ex)
            {
                candidate?.Dispose();
                _failed = true;
                CMain.SaveError("Optional atmospheric fog texture disabled: " + ex);
                return false;
            }
        }

        internal static void Draw(AtmosphericWeatherFrame frame, WeatherSetting weather)
        {
            if (!Settings.EnvironmentFogLayers || DXManager.DeviceLost ||
                DXManager.Sprite == null || DXManager.Sprite.Disposed ||
                DXManager.CurrentSurface == null ||
                (frame.FogCover < 0.01f && frame.CloudCover < 0.01f) ||
                !EnsureTexture())
                return;

            Matrix original = DXManager.Sprite.Transform;
            try
            {
                // World-space haze: draw before nameplates and HUD; existing
                // precipitation particles remain an independent effect.
                if (frame.FogCover > 0.01f)
                {
                    bool snow = (weather & WeatherSetting.Snow) != 0;
                    bool rain = (weather & WeatherSetting.Rain) != 0;
                    Color mist = snow ? Color.FromArgb(220, 233, 246) :
                                 rain ? Color.FromArgb(176, 196, 220) :
                                        Color.FromArgb(201, 213, 226);
                    int alpha = Math.Min(66, Math.Max(0, (int)(frame.FogCover * 74f)));
                    RenderLayer(512, frame.DriftX, frame.DriftY, Color.FromArgb(alpha, mist));
                }

                if (frame.CloudCover > 0.02f)
                {
                    // A broad, lower-frequency dark overcast layer introduces
                    // soft moving canopy shadows without scene depth geometry.
                    int alpha = Math.Min(32, Math.Max(0, (int)(frame.CloudCover * 34f)));
                    RenderLayer(768, frame.DriftX * 0.42f + 100f,
                                frame.DriftY * 0.35f + 210f,
                                Color.FromArgb(alpha, 32, 42, 57));
                }
            }
            catch (Exception ex)
            {
                _failed = true;
                CMain.SaveError("Optional atmospheric fog overlay disabled: " + ex);
            }
            finally
            {
                DXManager.Sprite.Flush();
                DXManager.Sprite.Transform = original;
            }
        }

        private static void RenderLayer(int tileSize, float driftX, float driftY, Color tint)
        {
            if (tint.A == 0 || !AtmosphereOverlayLayout.TryCountTiles(
                    Settings.ScreenWidth, Settings.ScreenHeight, tileSize, out _))
                return;

            float scale = tileSize / (float)TilePixels;
            float offsetX = driftX % tileSize;
            float offsetY = driftY % tileSize;
            DXManager.Sprite.Flush();
            DXManager.Sprite.Transform = Matrix.Scaling(scale, scale, 1f);

            for (float y = offsetY - tileSize; y < Settings.ScreenHeight; y += tileSize)
                for (float x = offsetX - tileSize; x < Settings.ScreenWidth; x += tileSize)
                    DXManager.Draw(_tile, TileRect,
                        new Vector3(x / scale, y / scale, 0f), tint);
            DXManager.Sprite.Flush();
        }

        internal static void DisposeAll()
        {
            if (_tile != null && !_tile.Disposed) _tile.Dispose();
            _tile = null;
            _failed = false;
        }
    }
}
