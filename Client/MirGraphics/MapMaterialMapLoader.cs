using System;
using System.IO;

namespace Client.MirGraphics
{
    // New, optional sidecar format: <map-file>.materials.json.
    // Never reinterpret or modify original Mir2/Mir3 map archives.
    internal static class MapMaterialMapLoader
    {
        internal static MapMaterialMap Load(string mapFile, string mapDirectory,
                                            int mapWidth, int mapHeight)
        {
            if (string.IsNullOrWhiteSpace(mapFile) ||
                string.IsNullOrWhiteSpace(mapDirectory))
                return MapMaterialMap.Empty;

            try
            {
                string root = Path.GetFullPath(mapDirectory);
                string mapPath = Path.GetFullPath(mapFile);
                string rootPrefix = Path.TrimEndingDirectorySeparator(root) +
                                    Path.DirectorySeparatorChar;

                // Never read sidecars from outside the configured map directory.
                if (!mapPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                    return MapMaterialMap.Empty;

                string filename = mapPath + ".materials.json";
                if (!File.Exists(filename)) return MapMaterialMap.Empty;

                var file = new FileInfo(filename);
                if (file.Length > MapMaterialMap.MaxJsonCharacters)
                {
                    CMain.SaveError("Material metadata ignored: file exceeds 512 KiB.");
                    return MapMaterialMap.Empty;
                }

                string json = File.ReadAllText(filename);
                if (!MapMaterialMap.TryParse(json, mapWidth, mapHeight,
                                             out MapMaterialMap atlas, out string problem))
                {
                    CMain.SaveError("Material metadata ignored (" + Path.GetFileName(filename) +
                                    "): " + problem);
                    return MapMaterialMap.Empty;
                }
                return atlas;
            }
            catch (Exception ex) when (ex is IOException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is ArgumentException ||
                                       ex is NotSupportedException)
            {
                CMain.SaveError("Optional map material metadata unavailable: " + ex.Message);
                return MapMaterialMap.Empty;
            }
        }
    }
}
