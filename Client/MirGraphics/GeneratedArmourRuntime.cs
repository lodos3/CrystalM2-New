using CrystalM2.ArmourCreate;
using Client.MirScenes;

namespace Client.MirGraphics
{
    internal static class GeneratedArmourRuntime
    {
        private static ArmourAtlas _atlas;
        private static MLibrary _preview;
        private static bool _failed;

        private static string Destination(byte id) => Path.Combine(Settings.DataPath, "GArmour", $"{id:00}.Lib");

        public static MLibrary GetLibrary(byte id)
        {
            if (id == 1 && !_failed)
            {
                try
                {
                    if (_preview == null)
                    {
                        _atlas = new ArmourAtlas(Settings.DataPath, Path.Combine(Settings.DataPath, "ArmourAuthoring"));
                        _preview = new MLibrary(Destination(id), _atlas);
                    }
                    return _preview;
                }
                catch (Exception ex)
                {
                    _atlas?.Dispose();
                    _atlas = null;
                    _failed = true;
                    Report("g01 runtime preview unavailable: " + ex.Message);
                }
            }
            return id < Libraries.GArmours.Length && File.Exists(Destination(id)) ? Libraries.GArmours[id] : null;
        }

        public static void Draw(byte id, int frame, Point location, Color colour)
        {
            if (id == 0) return;
            try { GetLibrary(id)?.Draw(frame, location, colour, true); }
            catch (Exception ex)
            {
                if (!_failed)
                {
                    _failed = true;
                    Report("Generated armour drawing failed: " + ex.Message);
                }
            }
        }

        public static void HandleAction(byte id, bool build)
        {
            if (LiveArmourAtlas.Supports(id))
            {
                try
                {
                    if (build)
                    {
                        using var live = new LiveArmourAtlas(Settings.DataPath, Path.Combine(Settings.DataPath,"ArmourAuthoring"));
                        live.Export(id,Path.Combine(Settings.DataPath,"LiveArmour"));
                        Report($"Exported g{id:00} skin and material surface.");
                    }
                    else
                    {
                        LiveArmourRenderer.Clear();
                        if (GameScene.Scene?.MapControl != null) GameScene.Scene.MapControl.TextureValid = false;
                        Report($"Reloaded g{id:00} live armour.");
                    }
                }
                catch (Exception ex) { Report("Live armour action failed: " + ex.Message); }
                return;
            }
            if (id != 1) { Report("Runtime designs: g01 to g12."); return; }
            try
            {
                if (build)
                {
                    GetLibrary(id);
                    if (_atlas == null) throw new InvalidOperationException("The runtime design could not be loaded.");
                    _atlas.Export(Destination(id));
                    Report("Exported " + Path.GetFullPath(Destination(id)));
                }
                else
                {
                    _preview?.ReleaseGeneratedPreview();
                    _preview = null;
                    _atlas = null;
                    _failed = false;
                    GetLibrary(id);
                    if (!_failed) Report("Reloaded g01 runtime design.");
                    if (GameScene.Scene?.MapControl != null) GameScene.Scene.MapControl.TextureValid = false;
                }
            }
            catch (Exception ex) { Report("Armour action failed: " + ex.Message); }
        }

        private static void Report(string message) => GameScene.Scene?.ChatDialog?.ReceiveChat(message, ChatType.System);
    }
}
