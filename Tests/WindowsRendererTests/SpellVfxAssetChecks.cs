using System.Numerics;
using System.Text.Json.Nodes;
using Client.MirScenes;
using Client.Rendering;
using Vortice.Direct3D;
using CrystalM2.Rendering.Core;

internal static class SpellVfxAssetChecks
{
    private static void CheckOriginalBladeFrames()
    {
        var flame = SpellVfxBladeFrames.Attack(Spell.FlamingSword, MirAction.Attack1)!.Value;
        var drake = SpellVfxBladeFrames.Attack(Spell.TwinDrakeBlade, MirAction.Attack1)!.Value;
        var drakeAlternate = SpellVfxBladeFrames.Attack(Spell.TwinDrakeBlade, MirAction.Attack4)!.Value;
        Check(flame == SpellVfxBladeFrames.Attack(Spell.FlamingSword, MirAction.Attack4) &&
            flame.Library == BladeSpriteLibrary.Magic && flame.FrameInterval == 100,
            "Flaming Sword must reuse the six original attack frames on either attack action.");
        Check(drake.Library == BladeSpriteLibrary.Magic2 && drake.FrameInterval == 50 &&
            drakeAlternate.Library == BladeSpriteLibrary.Magic2 && drakeAlternate.FrameInterval == 45,
            "Twin Drake Blade must fit both six-pose strikes into one combo.");

        for (int direction = 0; direction < 8; direction++)
        for (int frame = 0; frame < 6; frame++)
        {
            var facing = (MirDirection)direction;
            Check(flame.FrameIndex(facing, flame.FrameAt(frame * 100)) == 3480 + 10 * direction + frame &&
                drake.FrameIndex(facing, drake.FrameAt(frame * 50)) == 220 + 20 * direction + frame &&
                drakeAlternate.FrameIndex(facing, drakeAlternate.FrameAt(frame * 45)) == 226 + 20 * direction + frame,
                "The original attack archive indexes must match their direction and frame.");
        }

        var prepare = SpellVfxBladeFrames.DrakePrepare;
        var stun = SpellVfxBladeFrames.DrakeStun;
        Check(prepare.FrameIndex(MirDirection.Left, prepare.FrameAt(499)) == 215 && prepare.FrameInterval == 83 &&
            stun.FrameIndex(MirDirection.UpRight, stun.FrameAt(799)) == 385 && stun.FrameInterval == 133,
            "Twin Drake Blade activation and stun must retain six non-directional frames at legacy timing.");
    }

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

        CheckOriginalBladeFrames();
        SpellVfxAssets.Reload(SpellVfxAssets.InstalledDirectory);
        var directory = Path.Combine(Path.GetTempPath(), "CrystalM2-Vfx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            CheckBladeParticleLayers(directory);
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
            Check(roar.Count >= 8 && roar.Values.All(layer => layer.Particle.CoveragePlane == 0),
                $"Lion Roar should expose its individual particle emitters without legacy sprite planes. Found {roar.Count} emitters.");
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
                File.Delete(Path.Combine(spells, "FlamingSword.json"));
                Directory.Delete(spells);
            }
            Directory.Delete(directory);
        }

        Console.WriteLine("Spell VFX catalogue, packaged defaults and per-spell save/reload verified.");
    }

    private static void CheckBladeParticleLayers(string directory)
    {
        var pool = new SpellParticlePool(1024);
        var feet = new Vector2(150, 150);
        foreach (var (spell, phase) in new[]
        {
            (Spell.FlamingSword, SpellVisualPhase.Swing),
            (Spell.FlamingSword, SpellVisualPhase.FollowThrough),
            (Spell.TwinDrakeBlade, SpellVisualPhase.Swing),
            (Spell.TwinDrakeBlade, SpellVisualPhase.FollowThrough),
            (Spell.TwinDrakeBlade, SpellVisualPhase.Prepare),
            (Spell.TwinDrakeBlade, SpellVisualPhase.Hit)
        })
        for (int direction = 0; direction < 8; direction++)
        for (int frame = 0; frame < 6; frame++)
        {
            pool.Clear();
            SpellVfxBladeParticles.Draw(pool, spell, phase, feet, (MirDirection)direction, frame, frame / 6f,
                frame * 100, SpellParticleFrame.ReferenceSize, 79);
            Check(pool.Active.Length is > 0 and < 950, $"{spell} {phase} must produce a bounded particle layer for every reference pose.");
        }

        var before = SampleFlame();
        var emitters = SpellVfxAssets.CollectEmitters(() => SampleFlame());
        Check(emitters.Keys.ToHashSet().SetEquals(["Blade.Hot-core", "Blade.Heat-sheet", "Blade.Wake-glow"]),
            "Flaming Sword must expose independent core, heat sheet and glow layers.");
        CheckFlameMotion(pool, feet);

        pool.Clear();
        using (SpellVfxAssets.SoloEmitter("Blade.Lightning-sparks"))
            SpellVfxBladeParticles.Draw(pool, Spell.TwinDrakeBlade, SpellVisualPhase.FollowThrough, feet, MirDirection.Right,
                3, .5f, 135, SpellParticleFrame.ReferenceSize, 79);
        Check(pool.Active.Length == 600 && pool.Active.ToArray().All(p => p.Size is >= 1 and <= 3),
            "The charged swing must use six hundred pinpoints in place of large blue dots.");
        var sparks = pool.Active.ToArray();
        Check(sparks.Max(p => p.Origin.X) - sparks.Min(p => p.Origin.X) > 55 &&
            sparks.Max(p => p.Origin.Y) - sparks.Min(p => p.Origin.Y) > 40,
            "Lightning must spread sideways and vertically.");

        pool.Clear();
        using (SpellVfxAssets.SoloEmitter("Blade.Sweep-filaments"))
            SpellVfxBladeParticles.Draw(pool, Spell.TwinDrakeBlade, SpellVisualPhase.FollowThrough, feet, MirDirection.Right,
                4, .75f, 203, SpellParticleFrame.ReferenceSize, 79);
        var rim = pool.Active.ToArray();
        Check(rim.Length == 10 && rim.All(p => p.Shape == SpellParticleShape.DirectionalSweep && p.Ribbon.HasValue),
            "The swipe must render as one continuously textured surface.");
        Check(rim[0].RibbonTextureU!.Value.X == 0 && rim[^1].RibbonTextureU!.Value.Y == 1 &&
            rim.Zip(rim.Skip(1)).All(pair => pair.First.RibbonTextureU!.Value.Y == pair.Second.RibbonTextureU!.Value.X),
            "The material must span the swipe without repeating at every segment.");
        var fan = rim.SelectMany(p => new[] { p.Origin + p.Ribbon!.Value.TopLeft, p.Origin + p.Ribbon.Value.BottomLeft }).ToArray();
        Check(fan.Max(point => point.Y) - fan.Min(point => point.Y) > 17,
            "The release must open into a broad wind fan.");
        pool.Clear();
        using (SpellVfxAssets.SoloEmitter("Blade.Sweep-flick"))
            SpellVfxBladeParticles.Draw(pool, Spell.TwinDrakeBlade, SpellVisualPhase.FollowThrough, feet, MirDirection.Right,
                4, .75f, 203, SpellParticleFrame.ReferenceSize, 79);
        var returnStroke = pool.Active.ToArray();
        Check(returnStroke.Length > 5 && returnStroke.Max(p => p.Origin.X) - returnStroke[^1].Origin.X > 12,
            "The release must retain a returning edge flick.");

        int interval = SpellVfxBladeFrames.DrakeFollowThroughInterval;
        var ground = SampleCharged("Blade.Body-lightning", 0);
        var peak = SampleCharged("Blade.Body-lightning", interval);
        var fading = SampleCharged("Blade.Body-lightning", interval * 3 - 11);
        Check(ground.Length > 0 && ground.Min(p => p.Origin.Y) > feet.Y - 28,
            "The discharge must start at the ground.");
        Check(peak.All(p => p.Ribbon.HasValue) && peak.Min(p => p.Origin.Y) < feet.Y - 119 &&
            peak.Max(p => p.Origin.Y) > feet.Y - 7,
            "The discharge must rise through the character and past the head.");
        Check(fading.Length > 0 && fading.Max(p => p.Origin.Y) < feet.Y - 32,
            "The discharge must dissipate upward in its third pose.");
        foreach (string emitter in new[] { "Blade.Body-lightning", "Blade.Body-forks", "Blade.Body-wrap", "Blade.Overhead-forks" })
        for (int pose = 3; pose < 6; pose++)
            Check(SampleCharged(emitter, interval * pose).Length == 0,
                "The body discharge and its forks must end before the fourth pose.");
        Check(SampleCharged("Blade.Front-forks", interval * 4).Length > 0,
            "The weapon forks must continue after the body discharge ends.");

        SpellParticle[] SampleCharged(string emitter, int time)
        {
            pool.Clear();
            using var solo = SpellVfxAssets.SoloEmitter(emitter);
            SpellVfxBladeParticles.Draw(pool, Spell.TwinDrakeBlade, SpellVisualPhase.FollowThrough, feet, MirDirection.Right,
                time / interval, time / (float)(interval * 6), time, SpellParticleFrame.ReferenceSize, 79);
            return pool.Active.ToArray();
        }

        var overrides = JsonNode.Parse("""
            {"Emitters":{"Blade.Hot-core":{"StartColour":"#1DEB70","PositionX":12}}}
            """).AsObject();
        SpellVfxAssets.SaveSpell(directory, Spell.FlamingSword, overrides);
        SpellVfxAssets.Reload(directory);
        var edited = SampleFlame();
        Check(edited.Any(p => p.Emitter?.Id == "Blade.Hot-core" && (p.Colour.ToArgb() & 0xffffff) == 0x1DEB70) &&
            edited.Where(p => p.Emitter == null).SequenceEqual(before.Where((_, index) => edited[index].Emitter == null)),
            "Saving a layer edit must change the real blade effect and preserve every other layer.");

        var invalid = JsonNode.Parse("""{"Warrior":{"BladeMotion":{"Sweep":[[0,0,0,0,0,0]]}}}""").AsObject();
        ExpectInvalid(() => SpellVfxAssets.SaveSpell(directory, Spell.TwinDrakeBlade, invalid));

        SpellParticle[] SampleFlame()
        {
            pool.Clear();
            SpellVfxBladeParticles.Draw(pool, Spell.FlamingSword, SpellVisualPhase.Swing, feet, MirDirection.Right,
                4, .7f, 420, SpellParticleFrame.ReferenceSize, 79);
            return pool.Active.ToArray();
        }
    }

    private static void CheckFlameMotion(SpellParticlePool pool, Vector2 feet)
    {
        pool.Clear();
        int elapsed = -1;
        var motionLayers = SpellVfxAssets.CollectEmitters(() => Sample(400));
        Check(motionLayers.Keys.ToHashSet().SetEquals(["Blade.Fire-wake", "Blade.Smoke", "Blade.Blade-trail",
            "Blade.Embers", "Blade.Rear-flames", "Blade.Rear-embers", "Blade.Carry-flames"]) &&
            motionLayers.Values.All(layer => layer.Persistent && layer.HasMotion),
            "Fire wake, tracked trail, embers and rear burst must be editable lifetime particles.");
        var particles = pool.Active.ToArray();
        var release = particles.Where(p => p.Start == 400).ToArray();
        Check(release.Count(p => p.Motion!.Value.Drift.X < 0) == 56 &&
            release.Count(p => p.Motion!.Value.Drift.X > 0) == 24,
            "The release must throw a rear burst while the blade carries a separate streak forward.");
        var trail = particles.First(p => p.Emitter?.Id == "Blade.Blade-trail");
        Check(trail.Origin.X is > 145 and < 182 && trail.Motion!.Value.Spin != 0 &&
            trail.Position(trail.Start + 35) != trail.Origin &&
            trail.Dimensions(.15f, 1) != trail.Dimensions(.65f, 1) &&
            trail.ColourAt(.05f) != trail.ColourAt(.8f),
            "The tracked fire must travel from the weapon, spin, resize and cool.");
        Sample(400);
        Check(pool.Active.Length == particles.Length, "Sampling twice at the same time must not duplicate particles.");

        void Sample(int time) => SpellVfxBladeParticles.EmitFlameMotion(pool, feet, Vector2.UnitX, MirDirection.Right,
            pose => new WeaponMaskTrace(feet + new Vector2(-15 + pose * 8, -40), feet + new Vector2(12 + pose * 8, -50)),
            time, time, 100, ref elapsed, SpellParticleFrame.ReferenceSize, 79);
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
