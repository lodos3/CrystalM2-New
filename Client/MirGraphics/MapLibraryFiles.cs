namespace Client.MirGraphics;

// Shared map library numbers used by map cells and the standalone preview.
internal static class MapLibraryFiles
{
    internal static void Register(Action<int, string> register)
    {
        //wemade mir2 (allowed from 0-99)
        register(0, "Map\\WemadeMir2\\Tiles");
        register(1, "Map\\WemadeMir2\\Smtiles");
        register(2, "Map\\WemadeMir2\\Objects");
        for (int i = 2; i < 28; i++)
        {
            register(i + 1, "Map\\WemadeMir2\\Objects" + i.ToString());
        }
        register(90, "Map\\WemadeMir2\\Objects_32bit");

        //shanda mir2 (allowed from 100-199)
        register(100, "Map\\ShandaMir2\\Tiles");
        for (int i = 1; i < 10; i++)
        {
            register(100 + i, "Map\\ShandaMir2\\Tiles" + (i + 1));
        }
        register(110, "Map\\ShandaMir2\\SmTiles");
        for (int i = 1; i < 10; i++)
        {
            register(110 + i, "Map\\ShandaMir2\\SmTiles" + (i + 1));
        }
        register(120, "Map\\ShandaMir2\\Objects");
        for (int i = 1; i < 31; i++)
        {
            register(120 + i, "Map\\ShandaMir2\\Objects" + (i + 1));
        }
        register(190, "Map\\ShandaMir2\\AniTiles1");
        //wemade mir3 (allowed from 200-299)
        string[] Mapstate = { "", "wood\\", "sand\\", "snow\\", "forest\\"};
        for (int i = 0; i < Mapstate.Length; i++)
        {
            register(200 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Tilesc");
            register(201 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles30c");
            register(202 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles5c");
            register(203 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Smtilesc");
            register(204 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Housesc");
            register(205 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Cliffsc");
            register(206 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Dungeonsc");
            register(207 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Innersc");
            register(208 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Furnituresc");
            register(209 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Wallsc");
            register(210 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "smObjectsc");
            register(211 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Animationsc");
            register(212 +(i*15), "Map\\WemadeMir3\\" + Mapstate[i] + "Object1c");
            register(213 + (i * 15), "Map\\WemadeMir3\\" + Mapstate[i] + "Object2c");
        }
        Mapstate = new string[] { "", "wood", "sand", "snow", "forest"};
        //shanda mir3 (allowed from 300-399)
        for (int i = 0; i < Mapstate.Length; i++)
        {
            register(300 + (i * 15), "Map\\ShandaMir3\\" + "Tilesc" + Mapstate[i]);
            register(301 + (i * 15), "Map\\ShandaMir3\\" + "Tiles30c" + Mapstate[i]);
            register(302 + (i * 15), "Map\\ShandaMir3\\" + "Tiles5c" + Mapstate[i]);
            register(303 + (i * 15), "Map\\ShandaMir3\\" + "Smtilesc" + Mapstate[i]);
            register(304 + (i * 15), "Map\\ShandaMir3\\" + "Housesc" + Mapstate[i]);
            register(305 + (i * 15), "Map\\ShandaMir3\\" + "Cliffsc" + Mapstate[i]);
            register(306 + (i * 15), "Map\\ShandaMir3\\" + "Dungeonsc" + Mapstate[i]);
            register(307 + (i * 15), "Map\\ShandaMir3\\" + "Innersc" + Mapstate[i]);
            register(308 + (i * 15), "Map\\ShandaMir3\\" + "Furnituresc" + Mapstate[i]);
            register(309 + (i * 15), "Map\\ShandaMir3\\" + "Wallsc" + Mapstate[i]);
            register(310 + (i * 15), "Map\\ShandaMir3\\" + "smObjectsc" + Mapstate[i]);
            register(311 + (i * 15), "Map\\ShandaMir3\\" + "Animationsc" + Mapstate[i]);
            register(312 + (i * 15), "Map\\ShandaMir3\\" + "Object1c" + Mapstate[i]);
            register(313 + (i * 15), "Map\\ShandaMir3\\" + "Object2c" + Mapstate[i]);
        }
    }
}
