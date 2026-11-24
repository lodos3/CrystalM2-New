using System.Drawing.Imaging;
using System.Numerics;
using Client.MirScenes;
using Client.Rendering;
using CrystalM2.Rendering.Core;
using Vortice.Direct3D;

internal static class ArmourWingChecks
{
    internal static void Render(string data, string output)
    {
        const int previewWidth = 224, previewHeight = 180;
        using var window = new Form();
        using var device = new RendererDevice(window.Handle, previewWidth, previewHeight, DriverType.Warp);
        using var target = new Texture(device, previewWidth, previewHeight, true);
        using var surface = target.GetSurface();
        device.SetRenderTarget(surface);
        var pool = new SpellParticlePool(ArmourWingParticles.ParticleCapacity);
        using var armour = new LegacyMirLibReader(File.OpenRead(Path.Combine(data, "CArmour", "00.Lib")));
        string[] labels = ["Silver", "Gold", "Phoenix", "Frost", "Moon Moth", "Celestial Electric", "Fel Fire", "Demonic Bone", "Archangel", "Dawn Seraph"];
        using var sheet = new Bitmap(labels.Length * previewWidth, 30 + 8 * previewHeight);
        using var canvas = Graphics.FromImage(sheet);
        canvas.Clear(Color.FromArgb(22, 25, 30));
        var origin = new Vector2(previewWidth / 2f - 24, 112);
        CheckBlendKeepsColour();
        CheckNorthFelFireTips();
        CheckGeneratedVolume();
        if (!ArmourWings.TrySelect("auto", out var auto) || auto.HasValue ||
            !ArmourWings.TrySelect("off", out var off) || off != 0 || ArmourWings.TrySelect("10", out _) ||
            !ArmourWings.TrySelect("8", out var archangel) || archangel != ArmourWings.Archangel ||
            !ArmourWings.TrySelect("9", out var dawn) || dawn != ArmourWings.DawnSeraph ||
            !ArmourWings.TrySelect("6", out var fel) || fel != ArmourWings.FelFire ||
            !ArmourWings.TrySelect("7", out var bone) || bone != ArmourWings.DemonicBone ||
            !ArmourWings.TrySelect("5", out var electric) || electric != ArmourWings.CelestialElectric)
            throw new InvalidOperationException("Invalid wing preview selector mapping.");
        for (int id = 0; id < labels.Length; id++)
        for (int row = 0; row < 8; row++)
        {
            if (!ArmourWings.TrySelect(id.ToString(), out var effect) || !effect.HasValue) throw new InvalidDataException();
            MirDirection facing = (MirDirection)row;
            int pose = (int)facing * 4;
            if (!armour.TryReadFrame(pose, out var actor)) throw new InvalidDataException("Missing actor pose.");
            using var body = new Texture(device, actor.Width, actor.Height);
            body.BeginUpload().Data.Write(actor.Bgra);
            body.EndUpload();
            device.Clear(Color.FromArgb(22, 25, 30));
            ArmourWingParticles.Emit(pool, effect.Value, id, pose, ArmourWingParticles.Shoulder(effect.Value, false), facing, 480, 101);
            if (pool.Active.Length < 3 || pool.Active.Length >= pool.Capacity)
                throw new InvalidOperationException($"Invalid particle count for {labels[id]}.");
            bool behind = id >= 2 && facing is (MirDirection.Down or MirDirection.DownLeft or MirDirection.DownRight);
            bool split = id >= 2 && facing is (MirDirection.Left or MirDirection.Right);
            int near = ArmourWingParticles.NearSide(facing);
            if (split)
            {
                ArmourWingParticles.Emit(pool, effect.Value, id, pose, ArmourWingParticles.Shoulder(effect.Value, false), facing, 480, 101, -near);
                Wings();
            }
            else if (behind) Wings();
            device.Blend = SpriteBlendMode.Alpha;
            device.Sprite.Draw(body, null, new Vector3(origin.X + actor.OffsetX, origin.Y + actor.OffsetY, 0), Vector4.One);
            if (split)
                ArmourWingParticles.Emit(pool, effect.Value, id, pose, ArmourWingParticles.Shoulder(effect.Value, false), facing, 480, 101, near);
            if (!behind) Wings();
            using var image = device.Capture(surface);
            canvas.DrawImageUnscaled(image, id * previewWidth, 30 + row * previewHeight);
            if (row == 0) canvas.DrawString($"{id}   {labels[id]}", SystemFonts.MessageBoxFont!, Brushes.White, id * previewWidth + 12, 8);
        }
        sheet.Save(output, ImageFormat.Png);
        Console.WriteLine($"Rendered {labels.Length} wing IDs in all eight facings through the game particle renderer.");
        void Wings() => device.SpellParticles.Draw(pool.Active, origin, 480, false, emissiveOverlay: true, exposure: 1.22f);

        void CheckGeneratedVolume()
        {
            foreach (byte effect in Enumerable.Range(ArmourWings.Phoenix, 8).Select(i => (byte)i))
            foreach (bool mounted in new[] { false, true })
            foreach (MirDirection facing in Enum.GetValues<MirDirection>())
            {
                Vector2 shoulder = ArmourWingParticles.Shoulder(effect, mounted);
                Vector2 attachment = ArmourWingParticles.BackAttachment(shoulder, facing);
                float yaw = (int)facing * MathF.PI / 4;
                Vector2 rear = new(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f);
                if (Vector2.Dot(attachment - shoulder, rear) < 2)
                    throw new InvalidDataException($"Wing {effect} is not attached behind the shoulder in {facing}.");
                if (facing is MirDirection.Right or MirDirection.Left)
                {
                    float offset = (attachment.X - shoulder.X) *
                        (facing == MirDirection.Right ? -1f : 1f);
                    if (offset < 7f || offset > 9f)
                        throw new InvalidDataException($"Side wing root must hug the back, not penetrate or float: {offset}px in {facing}.");
                }
                for (int time = 0; time < 1800; time += 225)
                {
                    ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, time, 101);
                    var all = pool.Active.ToArray();
                    if (all.Length == 0 || all.Length >= pool.Capacity)
                        throw new InvalidDataException($"Wing {effect} exceeded its particle budget.");
                    var shells = all.Where(p => p.CoveragePlane < 0 && p.Layer == SpellParticleLayer.Emissive && p.Ribbon.HasValue).ToArray();
                    var faces = all.Where(p => p.CoveragePlane < 0 && p.Layer == SpellParticleLayer.EmissiveCore && p.Ribbon.HasValue).ToArray();
                    if (shells.Length == 0 || faces.Length == 0)
                        throw new InvalidDataException($"Wing {effect} has no volume shell.");
                    foreach (var shell in shells)
                    {
                        var face = faces.First(p => p.Origin == shell.Origin &&
                            p.RibbonTextureU == shell.RibbonTextureU &&
                            p.CoveragePlane == shell.CoveragePlane);
                        var s = shell.Ribbon.Value;
                        var f = face.Ribbon.Value;
                        float separation = Vector2.Distance(s.TopLeft, f.TopLeft);
                        if (!float.IsFinite(separation) || separation < 1)
                            throw new InvalidDataException($"Wing {effect} collapsed to a single plane in {facing}.");
                        if (s.Bounds(shell.Origin).Width < 1 || f.Bounds(face.Origin).Width < 1)
                            throw new InvalidDataException($"Wing {effect} contains a degenerate projection in {facing}.");
                    }
                    if (facing is MirDirection.Right or MirDirection.Left)
                    {
                        int near = ArmourWingParticles.NearSide(facing);
                        ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, time, 101, -near);
                        var far = pool.Active.ToArray();
                        ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, time, 101, near);
                        var front = pool.Active.ToArray();
                        if (far.Length + front.Length != all.Length ||
                            far.Any(p => p.Ribbon.HasValue && p.Origin.Y >= attachment.Y) ||
                            front.Any(p => p.Ribbon.HasValue && p.Origin.Y <= attachment.Y))
                            throw new InvalidDataException($"Wing {effect} has incorrect near/far separation.");
                    }
                }
            }
            Console.WriteLine("Verified all eight generated wing styles, directions, mount offsets and animation phases.");
        }

        void CheckNorthFelFireTips()
        {
            var shoulder = ArmourWingParticles.Shoulder(ArmourWings.FelFire, false);
            var centre = origin + shoulder;
            for (int time = 0; time < 3600; time += 150)
            {
                device.Clear(Color.Black);
                ArmourWingParticles.Emit(pool, ArmourWings.FelFire, 0, 0, shoulder, MirDirection.Up, time, 101);
                device.SpellParticles.Draw(pool.Active, origin, time, false, emissiveOverlay: true, exposure: 1.22f);
                using var frame = device.Capture(surface);
                bool left = false, right = false;
                for (int y = 0; y < centre.Y - 15; y++)
                for (int x = 0; x < frame.Width; x++)
                {
                    if (Math.Abs(x - centre.X) < 39 || frame.GetPixel(x, y).G < 120) continue;
                    if (x < centre.X) left = true;
                    else right = true;
                }
                if (!left || !right) throw new InvalidDataException($"North-facing Fel Fire tips dropped at {time} ms.");
            }
        }

        void CheckBlendKeepsColour()
        {
            pool.Clear();
            int bank = WingPoseAtlas.DesignBank + 7;
            var pose = WingPoseAtlas.GetPose(bank, 0);
            for (int c = 0; c < 3; c++)
                pool.TryAdd(new SpellParticle(new Vector2(80, 72), Vector2.Zero, 480, 50, pose.Width,
                    Color.FromArgb(unchecked((int)0xff000000) | (255 << (c * 8))), SpellParticleShape.Wisp, 0,
                    Aspect: pose.Height / (float)pose.Width, Layer: SpellParticleLayer.EmissiveCore)
                    { CoveragePlane = WingPoseAtlas.Handle(bank, 0, c) });
            using var reference = RenderPose(null);
            using var alpha = RenderPose(SpriteBlendMode.Alpha);
            long red = 0, blue = 0;
            for (int y = 0; y < alpha.Height; y++)
            for (int x = 0; x < alpha.Width; x++)
            {
                var pixel = alpha.GetPixel(x, y);
                if (pixel != reference.GetPixel(x, y)) throw new InvalidDataException("Blend override changed the pose palette.");
                red += pixel.R;
                blue += pixel.B;
            }
            if (red <= blue) throw new InvalidDataException("Dawn Seraph lost its warm colours.");
            Bitmap RenderPose(SpriteBlendMode? blend)
            {
                device.Clear(Color.Black);
                device.ArtworkBlend = blend;
                device.SpellParticles.Draw(pool.Active, Vector2.Zero, 480, false, emissiveOverlay: true);
                var image = device.Capture(surface);
                device.ArtworkBlend = null;
                return image;
            }
        }
    }
}
