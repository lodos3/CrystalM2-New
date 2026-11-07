using System;
using System.Drawing;
using Client.MirScenes;

namespace Client.MirGraphics
{
    // A slowly drifting overcast layer. Ground fog is rendered by the map's weather pass.
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
                candidate = new Texture(DXManager.Device, TilePixels, TilePixels);
                DataRectangle data = candidate.BeginUpload();
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
                    candidate.EndUpload();
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

        internal static void DrawClouds(AtmosphericWeatherFrame frame, WeatherSetting weather)
        {
            float intensity = AtmosphereIntensity.Scale(Settings.AtmosphereIntensityPercent) * GraphicsTuning.FogDensity.Current / 100f;
            if (intensity <= 0f || !Settings.EnvironmentFogLayers || DXManager.DeviceLost ||
                DXManager.Sprite == null || DXManager.Sprite.Disposed ||
                DXManager.CurrentSurface == null ||
                frame.CloudCover <= 0.02f ||
                !EnsureTexture())
                return;

            Matrix original = DXManager.Sprite.Transform;
            try
            {
                Point offset = GraphicsTuning.Offset(GraphicsLayer.Weather).Pixels;
                int alpha = Math.Min(32, Math.Max(0, (int)(frame.CloudCover * 34f * intensity)));
                RenderLayer(768, frame.DriftX * 0.42f + 100f + offset.X,
                            frame.DriftY * 0.35f + 210f + offset.Y,
                            (weather & (WeatherSetting.RedEmber | WeatherSetting.FireParticle)) != 0
                                ? Color.FromArgb(alpha, 70, 47, 43)
                                : Color.FromArgb(alpha, 32, 42, 57));
            }
            catch (Exception ex)
            {
                _failed = true;
                CMain.SaveError("Optional atmospheric fog overlay disabled: " + ex);
            }
            finally
            {
                // A device-reset can invalidate the sprite between entering
                // this optional pass and restoring state. Do not crash gameplay.
                try
                {
                    if (DXManager.Sprite != null && !DXManager.Sprite.Disposed)
                    {
                        DXManager.Sprite.Flush();
                        DXManager.Sprite.Transform = original;
                    }
                }
                catch (Exception ex)
                {
                    _failed = true;
                    CMain.SaveError("Atmosphere fog state restore failed: " + ex);
                }
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
            DXManager.Sprite.Transform = Matrix.CreateScale(scale, scale, 1f);

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
