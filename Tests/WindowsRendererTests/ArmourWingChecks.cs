using System.Drawing.Imaging;
using System.Numerics;
using Client.MirScenes;
using Client.Rendering;
using CrystalM2.Rendering.Core;
using Vortice.Direct3D;

internal static class ArmourWingChecks
{
    // Check production geometry, channel masks and selectors without installed LIBs.
    internal static void CheckElementalDesigns()
    {
        CheckFacingCant();
        CheckAttackLean();
        CheckArtworkRootContact();
        for (int id = 10; id < 20; id++)
        {
            if (!ArmourWings.TrySelect(id.ToString(), out byte? selection) ||
                selection != 118 + id - 10 || !ArmourWings.IsParticle(selection.Value))
                throw new InvalidDataException($"Incorrect elemental wing selector {id}.");
            byte effect = selection.Value;
            var pool = new SpellParticlePool(ArmourWingParticles.ParticleCapacity);
            foreach (MirDirection facing in Enum.GetValues<MirDirection>())
            foreach (int time in new[] { 0, 233, 719, 1530, 3100 })
            {
                Vector2 shoulder = ArmourWingParticles.Shoulder(effect, false);
                ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, time, 101);
                var full = pool.Active.ToArray();
                if (full.Length < 12 || full.Length >= pool.Capacity ||
                    full.Any(p => p.CoveragePlane > 0 || p.Colour.A == 0 || p.Ground ||
                        !float.IsFinite(p.Origin.X) || !float.IsFinite(p.Origin.Y)))
                    throw new InvalidDataException($"Invalid procedural effect {id}, {facing}.");
                if (!full.Any(p => p.CoveragePlane < 0) || full.Select(p => p.Shape).Distinct().Count() < 2 ||
                    !full.Any(p => p.Layer == SpellParticleLayer.Emissive) ||
                    !full.Any(p => p.Layer == SpellParticleLayer.EmissiveCore))
                    throw new InvalidDataException($"Missing material layers for {id}.");
                foreach (var particle in full)
                {
                    if (particle.Ribbon is { } quad &&
                        (!float.IsFinite(quad.TopLeft.X) || !float.IsFinite(quad.BottomRight.Y) ||
                        quad.Bounds(particle.Origin).Width <= 0 || quad.Bounds(particle.Origin).Height <= 0))
                        throw new InvalidDataException($"Degenerate elemental surface {id}.");
                }
                int near = ArmourWingParticles.NearSide(facing);
                ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, time, 101, -near);
                int farCount = pool.Active.Length;
                ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, time, 101, near);
                if (farCount == 0 || pool.Active.Length == 0 || farCount + pool.Active.Length != full.Length)
                    throw new InvalidDataException($"Incorrect elemental occlusion split {id}.");
                Vector2 movement = new(18, 12);
                ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder + movement, facing, time, 101);
                var moved = pool.Active.ToArray();
                if (moved.Length != full.Length) throw new InvalidDataException("Pose changed the particle budget.");
                for (int i = 0; i < full.Length; i++)
                    if (Vector2.Distance(moved[i].Origin - full[i].Origin, movement) > .01f)
                        throw new InvalidDataException($"Elemental effect {id} detached from body motion.");
            }
        }
        if (ArmourWings.TrySelect("20", out _) || ArmourWings.TrySelect("-1", out _) ||
            ArmourWings.IsParticle(128))
            throw new InvalidDataException("Invalid elemental wing ID accepted.");
        var fingerprints = new HashSet<string>();
        for (int id = 0; id < 10; id++)
        {
            int bank = WingPoseAtlas.DesignBank + 8 + id;
            var plane = WingPoseAtlas.Plane(WingPoseAtlas.Handle(bank, 0, 2));
            byte[] page = WingPoseAtlas.ReadPage(plane.Page);
            var pixels = new List<byte>();
            for (int y = plane.Source.Top; y < plane.Source.Bottom; y++)
                pixels.AddRange(page.AsSpan(y * WingPoseAtlas.PageSize + plane.Source.Left, plane.Source.Width).ToArray());
            if (pixels.Count(v => v > 30) < 100 || pixels.Distinct().Count() < 20 ||
                !fingerprints.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pixels.ToArray()))))
                throw new InvalidDataException($"Blank or duplicate elemental atlas {id}.");
        }
        Console.WriteLine("Verified ten elemental designs across eight facings, five times, split layers and pose motion.");
    }

    private static void CheckFacingCant()
    {
        // Screen Y increases downwards: positive rotation turns the upper
        // crown clockwise (towards the right). Mirrored facings reverse it.
        Vector2 crown = new(0, -40), hem = new(0, 40);
        foreach (MirDirection facing in Enum.GetValues<MirDirection>())
        {
            var mirror = (MirDirection)((8 - (int)facing) % 8);
            float angle = ArmourWingParticles.FacingCant(facing);
            float expected = .11f * MathF.Sin((int)facing * MathF.PI / 4);
            if (MathF.Abs(angle - expected) > .00001f ||
                MathF.Abs(angle + ArmourWingParticles.FacingCant(mirror)) > .00001f)
                throw new InvalidDataException($"Incorrect mirrored facing cant in {facing}.");

            Vector2 upper = ArmourWingParticles.ApplyFacingCant(crown, facing);
            Vector2 lower = ArmourWingParticles.ApplyFacingCant(hem, facing);
            Vector2 mirrored = ArmourWingParticles.ApplyFacingCant(crown, mirror);
            if (ArmourWingParticles.ApplyFacingCant(Vector2.Zero, facing) != Vector2.Zero ||
                Vector2.Distance(upper, -lower) > .001f ||
                Vector2.Distance(upper, new(-mirrored.X, mirrored.Y)) > .001f ||
                MathF.Abs(upper.Length() - crown.Length()) > .001f ||
                MathF.Abs(upper.X - 40 * MathF.Sin(expected)) > .001f)
                throw new InvalidDataException($"Facing cant must rotate around the fixed root in {facing}.");
        }
        if (ArmourWingParticles.ApplyFacingCant(crown, MirDirection.Right).X <= 0 ||
            ArmourWingParticles.ApplyFacingCant(crown, MirDirection.Left).X >= 0)
            throw new InvalidDataException("Right must cant clockwise and left anticlockwise.");
    }

    private static void CheckArtworkRootContact()
    {
        // Verify the actual textured geometry, not just the particle origins.
        // Each art root lies on a mesh seam: its projected position must
        // stay attached for every facing, texture frame and flap phase.
        var pool = new SpellParticlePool(ArmourWingParticles.ParticleCapacity);
        for (byte effect = ArmourWings.Phoenix; effect < ArmourWings.SolarAscendant; effect++)
        {
            int bank = WingPoseAtlas.DesignBank + effect - ArmourWings.Phoenix;
            var art = WingPoseAtlas.GetPose(bank, 0);
            Vector2 sourceRoot = ArmourWingParticles.ArtworkRoot(effect);
            float u = (sourceRoot.X - art.X) / art.Width;
            float v = (sourceRoot.Y - art.Y) / art.Height;
            foreach (MirDirection facing in Enum.GetValues<MirDirection>())
            foreach (int time in new[] { 0, 73, 233, 459, 719, 1057, 1530, 2399 })
            foreach (int side in new[] { -1, 1 })
            {
                Vector2 shoulder = new(24, -31);
                Vector2 socket = ArmourWingParticles.BackAttachment(shoulder, facing);
                if (Vector2.Distance(socket, shoulder) > 1.501f)
                    throw new InvalidDataException("A physical socket must be seated within the torso.");
                float yaw = (int)facing * MathF.PI / 4;
                Vector2 expected = socket + new Vector2(MathF.Cos(yaw), MathF.Sin(yaw) * .49f) * (side * 5.5f);
                ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, time, 101, side,
                    torsoYaw: .14f, torsoRoll: -.18f, torsoPitch: .28f);
                int left = 0, right = 0;
                foreach (var particle in pool.Active)
                {
                    if (particle.CoveragePlane >= 0 || particle.Layer != SpellParticleLayer.EmissiveCore ||
                        particle.Ribbon is not { } quad || particle.RibbonTextureU is not { } uv) continue;
                    if (MathF.Abs(uv.X - u) < .00001f)
                    {
                        left++;
                        if (Vector2.Distance(particle.Origin + Vector2.Lerp(quad.TopLeft, quad.BottomLeft, v), expected) > .01f)
                            throw new InvalidDataException($"Wing {effect} left artwork root detached in {facing} at {time}ms.");
                    }
                    if (MathF.Abs(uv.Y - u) < .00001f)
                    {
                        right++;
                        if (Vector2.Distance(particle.Origin + Vector2.Lerp(quad.TopRight, quad.BottomRight, v), expected) > .01f)
                            throw new InvalidDataException($"Wing {effect} right artwork root detached in {facing} at {time}ms.");
                    }
                }
                if (left == 0 || right == 0)
                    throw new InvalidDataException($"Missing artwork root seam for {effect}.");
            }
        }
    }

    private static void CheckAttackLean()
    {
        var pool = new SpellParticlePool(ArmourWingParticles.ParticleCapacity);
        for (byte effect = ArmourWings.Phoenix; effect < ArmourWings.SolarAscendant; effect++)
        foreach (bool mounted in new[] { false, true })
        foreach (MirDirection facing in Enum.GetValues<MirDirection>())
        {
            Vector2 shoulder = ArmourWingParticles.Shoulder(effect, mounted);
            var atlas = WingPoseAtlas.GetPose(WingPoseAtlas.DesignBank + effect - ArmourWings.Phoenix, 0);
            float artWidth = atlas.Width + 2 * atlas.X;
            ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, 400, 1);
            var rest = pool.Active.ToArray();
            ArmourWingParticles.Emit(pool, effect, 0, 0, shoulder, facing, 400, 1,
                torsoYaw: .14f, torsoRoll: .18f, torsoPitch: .28f);
            var tilted = pool.Active.ToArray();
            if (tilted.Length != rest.Length) throw new InvalidDataException("A strike changed the wing particle budget.");
            bool pinned = false, flexed = false;
            for (int i = 0; i < rest.Length; i++)
            {
                if (rest[i].Ribbon is not { } before || rest[i].RibbonTextureU is not { } uv) continue;
                var after = tilted[i].Ribbon!.Value;
                if (tilted[i].Origin != rest[i].Origin)
                    throw new InvalidDataException($"Wing {effect} moved its physical socket in {facing}.");
                float left = (atlas.X + atlas.Width * uv.X) / artWidth;
                float right = (atlas.X + atlas.Width * uv.Y) / artWidth;
                VerifyEdge(left, before.TopLeft, before.BottomLeft, after.TopLeft, after.BottomLeft);
                VerifyEdge(right, before.TopRight, before.BottomRight, after.TopRight, after.BottomRight);
            }
            if (!pinned || !flexed) throw new InvalidDataException($"Wing {effect} lacks a pinned root or flexible span in {facing}.");

            void VerifyEdge(float u, Vector2 top, Vector2 bottom, Vector2 movedTop, Vector2 movedBottom)
            {
                float movement = Math.Max(Vector2.Distance(top, movedTop), Vector2.Distance(bottom, movedBottom));
                if (u <= .12f)
                {
                    pinned = true;
                    if (movement > .001f)
                        throw new InvalidDataException($"Wing {effect} proximal geometry detached by {movement}px in {facing}.");
                }
                if (u >= .65f && movement > .5f) flexed = true;
            }
        }
    }

    internal static void Render(string data, string output, bool elementalOnly = false)
    {
        const int previewWidth = 224, previewHeight = 180;
        using var window = new Form();
        using var device = new RendererDevice(window.Handle, previewWidth, previewHeight, DriverType.Warp);
        using var target = new Texture(device, previewWidth, previewHeight, true);
        using var surface = target.GetSurface();
        device.SetRenderTarget(surface);
        var pool = new SpellParticlePool(ArmourWingParticles.ParticleCapacity);
        using var armour = new LegacyMirLibReader(File.OpenRead(Path.Combine(data, "CArmour", "00.Lib")));
        string[] labels = ["Silver", "Gold", "Phoenix", "Frost", "Moon Moth", "Celestial Electric", "Fel Fire", "Demonic Bone", "Archangel", "Dawn Seraph",
            "Magma Wyrm", "Tidal Leviathan", "Geode Titan", "Tempest", "Verdant Monarch",
            "Thunderstorm", "Venom", "Solar Ascendant", "Eclipse", "Astral Convergence"];
        // First eight rows are the original direction comparison. Additional
        // rows exercise body motion at a fixed wing animation time.
        (MirDirection Facing, int BodyFrame)[] extra =
        [
            (MirDirection.Right, 44), (MirDirection.Right, 92),
            (MirDirection.Right, 149), (MirDirection.Right, 152),
            (MirDirection.Right, 366), (MirDirection.Left, 174),
            (MirDirection.Right, 310),
            .. new[] { MirDirection.Up, MirDirection.Right, MirDirection.DownLeft }.SelectMany(
                facing => Enumerable.Range(0, 6).Select(frame => (facing, 136 + (int)facing * 6 + frame)))
        ];
        int first = elementalOnly ? 10 : 0;
        MirDirection[] backViews = [MirDirection.UpLeft, MirDirection.Up, MirDirection.UpRight];
        using var sheet = elementalOnly ? new Bitmap(5*previewWidth, 6*(previewHeight+30)) :
            new Bitmap(labels.Length * previewWidth, 30 + (8 + extra.Length) * previewHeight);
        using var canvas = Graphics.FromImage(sheet);
        canvas.Clear(Color.FromArgb(22, 25, 30));
        var origin = new Vector2(previewWidth / 2f - 24, 112);
        if (!elementalOnly)
        {
            CheckBlendKeepsColour();
            CheckNorthFelFireTips();
            CheckGeneratedVolume();
        }
        CheckElementalDesigns();
        if (!ArmourWings.TrySelect("auto", out var auto) || auto.HasValue ||
            !ArmourWings.TrySelect("off", out var off) || off != 0 || ArmourWings.TrySelect("20", out _) ||
            !ArmourWings.TrySelect("8", out var archangel) || archangel != ArmourWings.Archangel ||
            !ArmourWings.TrySelect("9", out var dawn) || dawn != ArmourWings.DawnSeraph ||
            !ArmourWings.TrySelect("6", out var fel) || fel != ArmourWings.FelFire ||
            !ArmourWings.TrySelect("7", out var bone) || bone != ArmourWings.DemonicBone ||
            !ArmourWings.TrySelect("5", out var electric) || electric != ArmourWings.CelestialElectric)
            throw new InvalidOperationException("Invalid wing preview selector mapping.");
        for (int id = first; id < labels.Length; id++)
        for (int row = 0; row < (elementalOnly ? backViews.Length : 8 + extra.Length); row++)
        {
            if (!ArmourWings.TrySelect(id.ToString(), out var effect) || !effect.HasValue) throw new InvalidDataException();
            if (row >= 8 && id < 2) continue; // Legacy baked wings use a separate pose index.
            MirDirection facing = elementalOnly ? backViews[row] : row < 8 ? (MirDirection)row : extra[row - 8].Facing;
            int pose = row < 8 ? (int)facing * 4 : extra[row - 8].BodyFrame;
            if (!armour.TryReadFrame(pose, out var actor)) throw new InvalidDataException("Missing actor pose.");
            using var body = new Texture(device, actor.Width, actor.Height);
            body.BeginUpload().Data.Write(actor.Bgra);
            body.EndUpload();
            device.Clear(Color.FromArgb(22, 25, 30));
            Vector2 trackedShoulder = ArmourWingParticles.Shoulder(effect.Value, false);
            WingPoseMotion motion = WingPoseMotion.Zero;
            if (id >= 2 && ArmourWingParticles.TryGetBodyMotion(effect.Value, pose, 0, facing, false, out motion))
                trackedShoulder += motion.Offset;
            ArmourWingParticles.Emit(pool, effect.Value, id, pose, trackedShoulder, facing, 480, 101,
                torsoYaw: motion.Yaw, torsoRoll: motion.Roll, torsoPitch: motion.Pitch);
            if (pool.Active.Length < 3 || pool.Active.Length >= pool.Capacity)
                throw new InvalidOperationException($"Invalid particle count for {labels[id]}.");
            bool behind = id >= 2 && facing is (MirDirection.Down or MirDirection.DownLeft or MirDirection.DownRight);
            bool split = ArmourWings.IsAura(effect.Value) ||
                (id >= 2 && facing is (MirDirection.Left or MirDirection.Right));
            int near = ArmourWingParticles.NearSide(facing);
            if (split)
            {
                ArmourWingParticles.Emit(pool, effect.Value, id, pose, trackedShoulder, facing, 480, 101, -near, motion.Yaw, motion.Roll, motion.Pitch);
                Wings();
            }
            else if (behind) Wings();
            device.Blend = SpriteBlendMode.Alpha;
            device.Sprite.Draw(body, null, new Vector3(origin.X + actor.OffsetX, origin.Y + actor.OffsetY, 0), Vector4.One);
            if (split)
                ArmourWingParticles.Emit(pool, effect.Value, id, pose, trackedShoulder, facing, 480, 101, near, motion.Yaw, motion.Roll, motion.Pitch);
            if (split || !behind) Wings();
            using var image = device.Capture(surface);
            int x = (elementalOnly ? (id-first)%5 : id)*previewWidth;
            int y = elementalOnly ? (row*2+(id-first)/5)*(previewHeight+30) : row*previewHeight;
            if (elementalOnly)
            {
                int top = 0;
                while (top < actor.Width * actor.Height && actor.Bgra[top * 4 + 3] == 0) top++;
                int head = (int)origin.Y + actor.OffsetY + top / actor.Width;
                int raisedLeft = 0, raisedRight = 0;
                for (int py = 0; py < head - 4; py++)
                for (int px = 0; px < image.Width; px++)
                {
                    var pixel = image.GetPixel(px, py);
                    if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) <= 64) continue;
                    if (px < previewWidth / 2) raisedLeft++;
                    else raisedRight++;
                }
                bool high = ArmourWings.IsAura(effect.Value) ? raisedLeft + raisedRight >= 12 :
                    raisedLeft >= 6 && raisedRight >= 6;
                if (!high) throw new InvalidDataException($"Wing {id} sits below the head in {facing}.");
            }
            canvas.DrawImageUnscaled(image, x, 30 + y);
            if (row == 0 || elementalOnly)
                canvas.DrawString(elementalOnly ? $"{id}   {labels[id]}   ·   {facing}" : $"{id}   {labels[id]}",
                    SystemFonts.MessageBoxFont!, Brushes.White, x + 12, y + 8);
        }
        sheet.Save(output, ImageFormat.Png);
        Console.WriteLine(elementalOnly ? "Verified ten designs rise above the head in all three rear views." :
            $"Rendered {labels.Length} wing IDs in all eight facings and sampled action frames through the game particle renderer.");
        void Wings() => device.SpellParticles.Draw(pool.Active, origin, 480, false, emissiveOverlay: true, exposure: 1.22f);

        void CheckGeneratedVolume()
        {
            foreach (byte effect in Enumerable.Range(ArmourWings.Phoenix, 8).Select(i => (byte)i))
            foreach (bool mounted in new[] { false, true })
            foreach (MirDirection facing in Enum.GetValues<MirDirection>())
            {
                Vector2 shoulder = ArmourWingParticles.Shoulder(effect, mounted);
                Vector2 attachment = ArmourWingParticles.BackAttachment(shoulder, facing);
                if (ArmourWingParticles.BackAttachment(shoulder, facing, .14f, .18f) != attachment)
                    throw new InvalidDataException("Torso rotation moved the physical wing socket.");
                float yaw = (int)facing * MathF.PI / 4;
                Vector2 rear = new(-MathF.Sin(yaw), MathF.Cos(yaw) * .49f);
                float rearDistance = Vector2.Dot(attachment - shoulder, rear) / rear.LengthSquared();
                if (MathF.Abs(rearDistance - 1.5f) > .01f)
                    throw new InvalidDataException($"Wing {effect} socket is not seated in the back in {facing}.");
                if (facing is MirDirection.Right or MirDirection.Left)
                {
                    float offset = (attachment.X - shoulder.X) *
                        (facing == MirDirection.Right ? -1f : 1f);
                    if (MathF.Abs(offset - 1.5f) > .01f)
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
                    var volumeBySocket = new Dictionary<Vector2, float>();
                    foreach (var shell in shells)
                    {
                        var face = faces.First(p => p.Origin == shell.Origin &&
                            p.RibbonTextureU == shell.RibbonTextureU &&
                            p.CoveragePlane == shell.CoveragePlane);
                        var s = shell.Ribbon.Value;
                        var f = face.Ribbon.Value;
                        float separation = Math.Max(Vector2.Distance(s.TopLeft, f.TopLeft), Vector2.Distance(s.TopRight, f.TopRight));
                        if (!float.IsFinite(separation)) throw new InvalidDataException($"Invalid shell depth in wing {effect}.");
                        volumeBySocket[shell.Origin] = Math.Max(separation, volumeBySocket.GetValueOrDefault(shell.Origin));
                        if (s.Bounds(shell.Origin).Width < 1 || f.Bounds(face.Origin).Width < 1)
                            throw new InvalidDataException($"Wing {effect} contains a degenerate projection in {facing}.");
                    }
                    // The membrane and shell meet at the shoulder. Each wing
                    // must develop visible thickness away from its socket.
                    if (volumeBySocket.Values.Any(separation => separation < 1))
                        throw new InvalidDataException($"Wing {effect} collapsed to a single plane in {facing}.");
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
