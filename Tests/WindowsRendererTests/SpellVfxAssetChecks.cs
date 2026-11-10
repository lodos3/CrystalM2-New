using System.Numerics;
using System.Text.Json.Nodes;
using Client.MirScenes;
using Client.Rendering;
using Vortice.Direct3D;
using CrystalM2.Rendering.Core;

internal static class SpellVfxAssetChecks
{
    internal static void Run()
    {
        var expected = new Dictionary<MirClass, int>
        {
            [MirClass.Warrior] = 41,
            [MirClass.Wizard] = 49,
            [MirClass.Taoist] = 48
        };
        Check(SpellVfxCatalogue.All.Select(entry => entry.Spell).Distinct().Count() == 138,
            "The studio catalogue must contain all 138 unique abilities.");
        foreach (var (actorClass, count) in expected)
            Check(SpellVfxCatalogue.All.Count(entry => entry.Class == actorClass) == count,
                $"Wrong number of {actorClass} catalogue entries.");

        SpellVfxAssets.Reload(SpellVfxAssets.InstalledDirectory);
        var directory = Path.Combine(Path.GetTempPath(), "CrystalM2-Vfx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var drake = new SpellVisual(Spell.TwinDrakeBlade, SpellVisualPhase.Impact);
            var flaming = new SpellVisual(Spell.FlamingSword, SpellVisualPhase.Impact);
            int baseline = WarriorSpellEffects.PacketDuration(drake);
            int unaffected = WarriorSpellEffects.PacketDuration(flaming);
            using var window = new Form { ClientSize = new Size(320, 270) };
            using var device = new RendererDevice(window.Handle, 320, 270, DriverType.Warp);
            using Surface backBuffer = device.GetBackBuffer();
            device.SetRenderTarget(backBuffer);
            var before = SampleCharge();
            using var beforeImage = RenderCharge(device, backBuffer, before);
            var overrides = JsonNode.Parse("""
                {
                  "Warrior": {
                    "PacketDuration": { "TwinDrakeBladeDurationMs": 975 },
                    "DrakeCharge": { "SparkCount": 4, "WispShape": "Smoke" }
                  }
                }
                """).AsObject();
            SpellVfxAssets.SaveSpell(directory, Spell.TwinDrakeBlade, overrides);
            SpellVfxAssets.Reload(directory);
            Check(WarriorSpellEffects.PacketDuration(drake) == 975, "A saved override must change the client duration.");
            Check(WarriorSpellEffects.PacketDuration(flaming) == unaffected, "A spell override must stay isolated.");
            var after = SampleCharge();
            Check(after.Active.Length < before.Active.Length, "The actual Warrior emitter must use the edited particle count.");
            using var afterImage = RenderCharge(device, backBuffer, after);
            Check(PixelsChanged(beforeImage, afterImage), "A saved override must change the D3D11 preview.");
            var roar = SpellVfxAssets.CollectEmitters(() => SampleRoar(Spell.LionRoar));
            var ring = roar.Single(item => item.Value.Particle.Shape == SpellParticleShape.WarriorRing);
            Check(roar.Count == 8, "Lion Roar should expose each authored particle layer separately.");
            var emitter = new JsonObject
            {
                ["StartSize"] = new JsonObject { ["Min"] = 160, ["Max"] = 160 },
                ["RenderMode"] = "TriangleMesh",
                ["SizeOverLifetime"] = JsonNode.Parse("""[{"Time":0,"Value":1},{"Time":1,"Value":0.5}]""")
            };
            var roarProfile = new JsonObject
            {
                [SpellVfxAssets.EmitterGroup] = new JsonObject { [ring.Key] = emitter }
            };
            var originalRoar = SampleRoar(Spell.LionRoar).Active.ToArray();
            SpellVfxAssets.SaveSpell(directory, Spell.LionRoar, roarProfile);
            SpellVfxAssets.Reload(directory);
            var editedRoar = SampleRoar(Spell.LionRoar).Active.ToArray();
            var mesh = editedRoar.Single(particle => particle.Emitter != null);
            Check(mesh.Emitter!.Id == ring.Key && mesh.Emitter.Settings.RenderMode == SpellEmitterRenderMode.TriangleMesh &&
                mesh.Size == 160 && mesh.RibbonScale(0) is < 1 and > .5f,
                "The runtime must apply saved size, stage curve and mesh mode to the chosen layer.");
            Check(editedRoar.Where(particle => particle.Emitter == null).SequenceEqual(
                originalRoar.Where(particle => particle.Shape != SpellParticleShape.WarriorRing)),
                "Editing the ring must preserve every other layer.");
            Check(SampleRoar(Spell.BattleCry).Active.ToArray().All(particle => particle.Emitter == null),
                "A spell sharing the same emitter site must retain its own settings.");
            using var originalRoarImage = RenderCharge(device, backBuffer, SampleRoarPool(originalRoar));
            using var editedRoarImage = RenderCharge(device, backBuffer, SampleRoarPool(editedRoar));
            Check(PixelsChanged(originalRoarImage, editedRoarImage), "The emitter settings must change the Direct3D output.");

            emitter["StartSize"] = new JsonObject { ["Min"] = 40, ["Max"] = 20 };
            try
            {
                SpellVfxAssets.SaveSpell(directory, Spell.LionRoar, roarProfile);
                throw new InvalidOperationException("The loader accepted an inverted particle range.");
            }
            catch (System.Text.Json.JsonException) { }
            Check(SampleRoar(Spell.LionRoar).Active.ToArray().Any(particle => particle.Emitter != null && particle.Size == 160),
                "An invalid range must leave the last good profile active.");

            var shared = new JsonObject
            {
                ["WindRibbon"] = new JsonObject { ["WindRibbonCastsLight"] = true }
            };
            SpellVfxAssets.SaveGroup(directory, "Frame", shared);
            SpellVfxAssets.Reload(directory);
            Check((bool)SpellVfxAssets.ReadGroup(directory, "Frame")["WindRibbon"]["WindRibbonCastsLight"],
                "The studio must be able to save a shared lighting flag.");
            Check((int)SpellVfxAssets.ReadSpellGroup(directory, Spell.TwinDrakeBlade, "Warrior")["DrakeCharge"]["SparkCount"] == 4,
                "The studio must be able to reopen effective spell settings.");

            var path = Path.Combine(directory, "Spells", "TwinDrakeBlade.json");
            string saved = File.ReadAllText(path);
            overrides["Warrior"]["PacketDuration"]["TwinDrakeBladeDurationMs"] = -1;
            ExpectInvalid(() => SpellVfxAssets.SaveSpell(directory, Spell.TwinDrakeBlade, overrides));
            Check(File.ReadAllText(path) == saved, "A rejected edit must leave the last saved file intact.");

            File.WriteAllText(path, saved.Replace("975", "-1"));
            ExpectInvalid(() => SpellVfxAssets.Reload(directory));
            Check(WarriorSpellEffects.PacketDuration(drake) == 975, "An invalid reload must keep the active settings.");

            File.Delete(path);
            SpellVfxAssets.Reload(directory);
            Check(WarriorSpellEffects.PacketDuration(drake) == baseline, "Removing the override must restore the default.");
        }
        finally
        {
            SpellVfxAssets.Reload(SpellVfxAssets.InstalledDirectory);
            File.Delete(Path.Combine(directory, "Frame.json"));
            string spells = Path.Combine(directory, "Spells");
            if (Directory.Exists(spells))
            {
                File.Delete(Path.Combine(spells, "TwinDrakeBlade.json"));
                File.Delete(Path.Combine(spells, "LionRoar.json"));
                Directory.Delete(spells);
            }
            Directory.Delete(directory);
        }

        Console.WriteLine("Spell VFX catalogue, packaged defaults and per-spell save/reload verified.");
    }

    private static SpellParticlePool SampleCharge()
    {
        var pool = new SpellParticlePool(2048);
        WarriorSpellEffects.DrawFrame(pool, new SpellVisual(Spell.TwinDrakeBlade, SpellVisualPhase.Prepare),
            new Vector2(150, 150), Vector2.UnitX, 1200, 1000, 500, seed: 79);
        return pool;
    }

    private static SpellParticlePool SampleRoar(Spell spell)
    {
        var pool = new SpellParticlePool(2048);
        WarriorSpellEffects.DrawFrame(pool, new SpellVisual(spell), new Vector2(150, 150), Vector2.UnitX,
            1200, 1000, 500, seed: 79);
        return pool;
    }

    private static SpellParticlePool SampleRoarPool(SpellParticle[] particles)
    {
        var pool = new SpellParticlePool(2048);
        foreach (var particle in particles) pool.TryAdd(particle);
        return pool;
    }

    private static Bitmap RenderCharge(RendererDevice device, Surface target, SpellParticlePool pool)
    {
        device.Clear(Color.Black);
        device.SpellParticles.Draw(pool.Active, Vector2.Zero, 1200, ground: true);
        device.SpellParticles.Draw(pool.Active, Vector2.Zero, 1200, ground: false);
        device.SpellParticles.Draw(pool.Active, Vector2.Zero, 1200, ground: false, emissiveOverlay: true);
        return device.Capture(target);
    }

    private static bool PixelsChanged(Bitmap first, Bitmap second)
    {
        for (int y = 0; y < first.Height; y++)
        for (int x = 0; x < first.Width; x++)
            if (first.GetPixel(x, y) != second.GetPixel(x, y)) return true;
        return false;
    }

    private static void ExpectInvalid(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("An invalid asset was accepted.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
