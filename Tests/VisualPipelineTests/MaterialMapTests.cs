using System;
using System.Drawing;
using System.Text;
using Client.MirGraphics;

internal static class MaterialMapTests
{
    internal static void Run()
    {
        static void Check(bool ok, string why)
        {
            if (!ok) throw new Exception(why);
        }

        const string metadata = """
        {
          "version": 1,
          "regions": [
            {"x": 1, "y": 2, "width": 6, "height": 3,
             "material": "Stone", "wetResponse": 0.75, "blocksLight": false},
            {"x": 5, "y": 3, "width": 1, "height": 1,
             "material": "Metal", "wetResponse": 1.0, "blocksLight": true},
            {"x": 12, "y": 4, "width": 2, "height": 2, "material": "Foliage"}
          ]
        }
        """;
        Check(MapMaterialMap.TryParse(metadata, 20, 20, out MapMaterialMap atlas,
                                       out string error), error);
        Check(atlas.Regions.Count == 3, "Region indexing must preserve metadata.");
        Check(atlas.TrySample(1, 2, out MaterialRegion stone) &&
              stone.Material == MapMaterial.Stone && stone.WetResponse == 0.75f &&
              stone.BlocksLight == false, "Authored stone response or light override changed.");
        Check(atlas.TrySample(5, 3, out MaterialRegion metal) &&
              metal.Material == MapMaterial.Metal && metal.WetResponse == 1.0f &&
              metal.BlocksLight == true, "Later overlapping regions must take precedence.");
        Check(atlas.TrySample(12, 5, out MaterialRegion foliage) &&
              foliage.Material == MapMaterial.Foliage && foliage.WetResponse == 0 &&
              foliage.BlocksLight == null, "Absent optional fields must not infer characteristics.");
        Check(!atlas.TrySample(19, 19, out _), "Unannotated tiles must remain unclassified.");
        Check(!atlas.TrySample(-1, 2, out _), "Negative coordinates cannot be classified.");

        Check(!MapMaterialMap.TryParse("""
             {"version":2,"regions":[]}
             """, 20, 20, out _, out _), "Unknown future schema versions must fail safely.");
        Check(!MapMaterialMap.TryParse("""
             {"version":1,"regions":[{"x":19,"y":0,"width":2,"height":1,"material":"Stone"}]}
             """, 20, 20, out _, out _), "Material regions cannot cross map bounds.");
        Check(!MapMaterialMap.TryParse("""
             {"version":1,"regions":[{"x":0,"y":0,"width":1,"height":1,"material":"Lava"}]}
             """, 20, 20, out _, out _), "Do not guess unsupported materials.");
        Check(!MapMaterialMap.TryParse("""
             {"version":1,"regions":[{"x":0,"y":0,"width":1,"height":1,"material":"Water","wetResponse":2.0}]}
             """, 20, 20, out _, out _), "Wet response must remain bounded.");
        Check(!MapMaterialMap.TryParse("{invalid", 20, 20, out _, out _),
              "Malformed sidecars must never enter the rendering pipeline.");

        var tooMany = new StringBuilder("{\"version\":1,\"regions\":[");
        for (int i = 0; i <= MapMaterialMap.MaxRegions; i++)
        {
            if (i != 0) tooMany.Append(',');
            tooMany.Append("{\"x\":0,\"y\":0,\"width\":1,\"height\":1,\"material\":\"Stone\"}");
        }
        tooMany.Append("]}");
        Check(!MapMaterialMap.TryParse(tooMany.ToString(), 20, 20, out _, out _),
              "Sidecar regions must be capped for predictable lookup cost.");

        byte[] mask = new byte[48 * 32 * 4];
        WetSurfaceMask.WriteBgra(mask, 48, 32, 48 * 4);
        Check(mask[3] == 0, "Wet surface mask edges must remain transparent.");
        int middle = (16 * 48 + 24) * 4;
        Check(mask[middle + 3] > 0 && mask[middle] == 255,
              "Material sheen should be a translucent white mask.");
        try
        {
            WetSurfaceMask.WriteBgra(new byte[7], 48, 32, 192);
            throw new Exception("A short destination must be rejected.");
        }
        catch (ArgumentException) { }
        Check(WetSurfaceMask.TileBudget(40, 30) <= 96 && WetSurfaceMask.TileBudget(40, 30) > 0,
              "Wet surface draw budget must stay bounded.");
        Console.WriteLine("Material sidecar safety, override precedence and wet-surface mask checks passed.");
    }
}
