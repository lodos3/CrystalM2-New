using System;
using System.Drawing;
using Client.MirScenes;

namespace Client.MirGraphics
{
    // Optional authored-tile response to accumulated rainfall. Draws one tiny
    // precomputed sheen texture on selected ground cells; no CPU readbacks.
    internal static class WetSurfacePass
    {
        private const int TileWidth = 48;
        private const int TileHeight = 32;
        private static readonly Rectangle TileRect =
            new Rectangle(0, 0, TileWidth, TileHeight);
        private static Texture _sheen;
        private static bool _failed;

        private static bool EnsureSheen()
        {
            if (_failed) return false;
            if (_sheen != null && !_sheen.Disposed) return true;

            Texture candidate = null;
            try
            {
                candidate = new Texture(DXManager.Device, TileWidth, TileHeight);
                DataRectangle locked = candidate.BeginUpload();
                try
                {
                    unsafe
                    {
                        Span<byte> pixels = new Span<byte>((void*)locked.Data.DataPointer,
                                                           checked(locked.Pitch * TileHeight));
                        WetSurfaceMask.WriteBgra(pixels, TileWidth, TileHeight,
                                                 locked.Pitch);
                    }
                }
                finally
                {
                    locked.Data.Dispose();
                    candidate.EndUpload();
                }
                _sheen = candidate;
                return true;
            }
            catch (Exception ex)
            {
                candidate?.Dispose();
                _failed = true;
                CMain.SaveError("Optional wet surface sheen disabled: " + ex.Message);
                return false;
            }
        }

        internal static void Draw(MapMaterialMap atlas, AtmosphericWeatherFrame frame,
                                  Point cameraCell, Point movementOffset,
                                  int offsetX, int offsetY,
                                  int viewRangeX, int viewRangeY,
                                  int mapWidth, int mapHeight)
        {
            if (!Settings.AtmosphereWetSurfaces || !Settings.EnvironmentAtmosphere ||
                DXManager.DeviceLost || atlas == null || atlas.Regions.Count == 0 ||
                frame.Wetness < 0.05f ||
                DXManager.Sprite == null || DXManager.Sprite.Disposed)
                return;

            float intensity = AtmosphereIntensity.Scale(Settings.AtmosphereIntensityPercent);
            if (intensity <= 0f || !EnsureSheen()) return;

            int startX = Math.Max(0, cameraCell.X - viewRangeX);
            int endX = Math.Min(mapWidth, cameraCell.X + viewRangeX + 1);
            int startY = Math.Max(0, cameraCell.Y - viewRangeY);
            int endY = Math.Min(mapHeight, cameraCell.Y + viewRangeY + 1);
            if (endX <= startX || endY <= startY) return;

            int totalVisible = (endX - startX) * (endY - startY);
            int stride = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(
                totalVisible / (double)WetSurfaceMask.MaxTilesPerFrame)));
            int drawn = 0;

            // Subsample densely tagged regions on a regular camera-relative
            // grid. One cell never consumes more than a single draw call.
            try
            {
                for (int y = startY; y < endY && drawn < WetSurfaceMask.MaxTilesPerFrame; y += stride)
                {
                    for (int x = startX; x < endX && drawn < WetSurfaceMask.MaxTilesPerFrame; x += stride)
                    {
                        if (!atlas.TrySample(x, y, out MaterialRegion material) ||
                            material.WetResponse <= 0f)
                            continue;

                        int alpha = Math.Min(150, Math.Max(0,
                            (int)(frame.Wetness * material.WetResponse * intensity * 145f)));
                        if (alpha == 0) continue;

                        int screenX = (x - cameraCell.X + offsetX) * TileWidth -
                                      offsetX + movementOffset.X;
                        int screenY = (y - cameraCell.Y + offsetY) * TileHeight +
                                      movementOffset.Y;
                        if (!SpriteVisibility.IsVisible(screenX, screenY,
                                                        TileWidth, TileHeight,
                                                        Settings.ScreenWidth, Settings.ScreenHeight))
                            continue;

                        DXManager.Draw(_sheen, TileRect,
                                       new Vector3(screenX, screenY, 0f),
                                       Color.FromArgb(alpha, 218, 233, 255));
                        drawn++;
                    }
                }
            }
            catch (Exception ex)
            {
                _failed = true;
                CMain.SaveError("Optional wet surface effect disabled: " + ex.Message);
            }
        }

        internal static void DisposeAll()
        {
            if (_sheen != null && !_sheen.Disposed) _sheen.Dispose();
            _sheen = null;
            _failed = false;
        }
    }
}
