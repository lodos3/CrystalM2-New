using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;

namespace Client.MirGraphics
{
    // Materials are explicitly authored; sprite colours and map collision bits
    // are not sufficient evidence of stone, water, metal or object height.
    internal enum MapMaterial : byte
    {
        Unknown = 0,
        Stone,
        Wood,
        Soil,
        Metal,
        Water,
        Foliage
    }

    internal readonly struct MaterialRegion
    {
        internal readonly Rectangle Bounds;
        internal readonly MapMaterial Material;
        internal readonly float WetResponse;
        internal readonly bool? BlocksLight;

        internal MaterialRegion(Rectangle bounds, MapMaterial material,
                                float wetResponse, bool? blocksLight)
        {
            Bounds = bounds;
            Material = material;
            WetResponse = wetResponse;
            BlocksLight = blocksLight;
        }
    }

    // One compact list of authored regions per map. No second map-sized array
    // or inferred classifications. Later regions override earlier ones.
    internal sealed class MapMaterialMap
    {
        internal const int MaxRegions = 128;
        internal const int MaxJsonCharacters = 512 * 1024;
        private readonly List<MaterialRegion> _regions;
        internal static readonly MapMaterialMap Empty = new MapMaterialMap(new List<MaterialRegion>());
        internal IReadOnlyList<MaterialRegion> Regions => _regions;

        private MapMaterialMap(List<MaterialRegion> regions)
        {
            _regions = regions;
        }

        internal bool TrySample(int x, int y, out MaterialRegion material)
        {
            for (int i = _regions.Count - 1; i >= 0; i--)
            {
                MaterialRegion candidate = _regions[i];
                if (candidate.Bounds.Contains(x, y))
                {
                    material = candidate;
                    return true;
                }
            }

            material = default;
            return false;
        }

        internal static bool TryParse(string json, int mapWidth, int mapHeight,
                                      out MapMaterialMap map, out string error)
        {
            map = Empty;
            error = null;
            if (mapWidth <= 0 || mapHeight <= 0 || json == null ||
                json.Length > MaxJsonCharacters)
            {
                error = "Invalid map dimensions or oversized material metadata.";
                return false;
            }

            try
            {
                using (JsonDocument document = JsonDocument.Parse(
                    json, new JsonDocumentOptions { MaxDepth = 12 }))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object ||
                        !root.TryGetProperty("version", out JsonElement version) ||
                        version.GetInt32() != 1 ||
                        !root.TryGetProperty("regions", out JsonElement regions) ||
                        regions.ValueKind != JsonValueKind.Array ||
                        regions.GetArrayLength() > MaxRegions)
                    {
                        error = "Expected version 1 and at most 128 material regions.";
                        return false;
                    }

                    var parsed = new List<MaterialRegion>(regions.GetArrayLength());
                    foreach (JsonElement region in regions.EnumerateArray())
                    {
                        if (region.ValueKind != JsonValueKind.Object ||
                            !TryInt(region, "x", out int x) ||
                            !TryInt(region, "y", out int y) ||
                            !TryInt(region, "width", out int width) ||
                            !TryInt(region, "height", out int height) ||
                            x < 0 || y < 0 || width <= 0 || height <= 0 ||
                            (long)x + width > mapWidth || (long)y + height > mapHeight ||
                            !region.TryGetProperty("material", out JsonElement named) ||
                            named.ValueKind != JsonValueKind.String ||
                            !Enum.TryParse(named.GetString(), true, out MapMaterial material) ||
                            !Enum.IsDefined(material) || material == MapMaterial.Unknown)
                        {
                            error = "Invalid material name or out-of-bounds rectangle.";
                            return false;
                        }

                        float wetResponse = 0f;
                        if (region.TryGetProperty("wetResponse", out JsonElement wet))
                        {
                            if (wet.ValueKind != JsonValueKind.Number ||
                                !wet.TryGetSingle(out wetResponse) ||
                                float.IsNaN(wetResponse) ||
                                wetResponse < 0f || wetResponse > 1f)
                            {
                                error = "Wet response must be a number from 0 through 1.";
                                return false;
                            }
                        }

                        bool? blocksLight = null;
                        if (region.TryGetProperty("blocksLight", out JsonElement blocked))
                        {
                            if (blocked.ValueKind != JsonValueKind.True &&
                                blocked.ValueKind != JsonValueKind.False)
                            {
                                error = "blocksLight must be a boolean.";
                                return false;
                            }
                            blocksLight = blocked.GetBoolean();
                        }

                        parsed.Add(new MaterialRegion(new Rectangle(x, y, width, height),
                                                      material, wetResponse, blocksLight));
                    }

                    map = new MapMaterialMap(parsed);
                    return true;
                }
            }
            catch (Exception ex) when (ex is JsonException ||
                                       ex is InvalidOperationException ||
                                       ex is FormatException ||
                                       ex is OverflowException ||
                                       ex is ArgumentException)
            {
                error = "Invalid material JSON: " + ex.Message;
                return false;
            }
        }

        private static bool TryInt(JsonElement element, string key, out int value)
        {
            value = 0;
            return element.TryGetProperty(key, out JsonElement number) &&
                   number.ValueKind == JsonValueKind.Number &&
                   number.TryGetInt32(out value);
        }
    }

    // A small, precomputed alpha mask for optional rain sheen. Unannotated
    // tiles are never drawn, and the source artwork remains unchanged.
    internal static class WetSurfaceMask
    {
        internal const int MaxTilesPerFrame = 96;

        internal static int TileBudget(int columns, int rows)
        {
            if (columns <= 0 || rows <= 0) return 0;
            return (int)Math.Min(MaxTilesPerFrame, (long)columns * rows);
        }

        internal static void WriteBgra(Span<byte> destination, int width, int height, int pitch)
        {
            if (width <= 0 || height <= 0 || pitch < (long)width * 4 ||
                ((long)height - 1) * pitch + (long)width * 4 > destination.Length)
                throw new ArgumentException("Invalid wet sheen image dimensions.");

            for (int y = 0; y < height; y++)
            {
                int row = y * pitch;
                for (int x = 0; x < width; x++)
                {
                    int pixel = row + x * 4;
                    float dx = (x - width * 0.5f) / (width * 0.55f);
                    float dy = (y - height * 0.5f) / (height * 0.6f);
                    float edgeFade = Math.Max(0f, 1f - dx * dx - dy * dy);
                    int diagonal = Math.Abs((x + 2 * y) % 23 - 11);
                    float glint = Math.Max(0f, 1f - diagonal / 5f);
                    byte alpha = (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        ? (byte)0
                        : (byte)Math.Min(96, (int)(edgeFade * (18f + 50f * glint)));
                    destination[pixel] = destination[pixel + 1] =
                        destination[pixel + 2] = 255;
                    destination[pixel + 3] = alpha;
                }
            }
        }
    }
}
