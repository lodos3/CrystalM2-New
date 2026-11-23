using CrystalM2.Rendering.Core;

internal static class SpellPoseComparisons
{
    internal static void Run(string directory)
    {
        int checkedFrames = 0;
        (string Name, string Library, int First, int Directions, int Stride, int Frames, int Step)[] sequences =
        [
            ("Slaying", "Magic", 2090, 8, 10, 6, 1), ("Thrusting", "Magic", 2460, 8, 10, 6, 1),
            ("HalfMoon", "Magic", 2830, 8, 10, 6, 1), ("CrossHalfMoon", "Magic2", 40, 8, 10, 6, 1),
            ("BladeAvalanche", "Magic2", 740, 8, 20, 15, 2), ("CatTongue", "Magic3", 260, 16, 10, 6, 2),
            ("FireBall", "Magic", 10, 16, 10, 6, 1), ("GreatFireBall", "Magic", 410, 16, 10, 6, 1),
            ("FrostCrunch", "Magic2", 410, 16, 10, 4, 1), ("SoulFireBall", "Magic", 1160, 16, 10, 3, 1),
            ("FlamingSword", "Magic", 3480, 8, 10, 6, 2), ("TwinDrakeBlade", "Magic2", 220, 8, 20, 6, 1),
            ("TwinDrakeFollow", "Magic2", 226, 8, 20, 6, 1), ("DrakePrepare", "Magic2", 210, 1, 0, 6, 1),
            ("DrakeStun", "Magic2", 380, 1, 0, 6, 1), ("LionRoar", "Magic2", 710, 1, 0, 20, 1),
            ("Entrapment", "Magic2", 990, 1, 0, 10, 1), ("Rage", "Magic2", 1510, 1, 0, 10, 1),
            ("ProtectionField", "Magic2", 1520, 1, 0, 10, 1), ("Fury", "Magic3", 187, 1, 0, 10, 1),
            ("FuryFlash", "Magic3", 200, 1, 0, 8, 1), ("ImmortalSkin", "Magic3", 550, 1, 0, 17, 1),
            ("SkinFlash", "Magic3", 570, 1, 0, 5, 1), ("SlashingBurst", "Magic2", 1700, 8, 10, 9, 1),
            ("CounterAttack", "Magic", 3480, 8, 10, 10, 1), ("CounterFlash", "Magic3", 140, 1, 0, 2, 1),
            ("SoulShieldWard", "Magic", 1320, 1, 0, 15, 1), ("BlessedArmourWard", "Magic", 1340, 1, 0, 15, 1),
            ("CurseRune", "Magic2", 950, 1, 0, 24, 1)
        ];
        foreach (var s in sequences)
        {
            using var lib = new LegacyMirLibReader(File.OpenRead(Path.Combine(directory, s.Library + ".Lib")));
            for (int direction = 0; direction < s.Directions; direction++)
            for (int pose = 0; pose < s.Frames; pose++)
            {
                int index = s.First + direction * s.Stride + pose;
                var mask = SpellPoseAtlas.GetPose(s.Name, direction, pose);
                if (!lib.TryReadFrame(index, out var original))
                {
                    if (mask.FirstPlane != 0) throw new InvalidDataException($"Expected an empty slot at {s.Name} #{index}.");
                    continue;
                }
                if (mask.FirstPlane == 0 || mask.Offset != new Point(original.OffsetX - 24, original.OffsetY - 16))
                    throw new InvalidDataException($"Incorrect anchor for {s.Name} #{index}.");
                for (int c = 0; c < 3; c++)
                {
                    var plane = SpellPoseAtlas.GetPlane(mask.FirstPlane + c);
                    var pixels = SpellPoseAtlas.GetPage(plane.Page);
                    var rect = plane.Source;
                    if (rect.Width != (original.Width + s.Step - 1) / s.Step ||
                        rect.Height != (original.Height + s.Step - 1) / s.Step ||
                        mask.WorldSize != new Size(rect.Width * s.Step, rect.Height * s.Step))
                        throw new InvalidDataException($"Incorrect extent for {s.Name} #{index}.");
                    for (int y = 0; y < rect.Height; y++)
                    for (int x = 0; x < rect.Width; x++)
                    {
                        int sum = 0;
                        for (int sy = y * s.Step; sy < Math.Min(original.Height, (y + 1) * s.Step); sy++)
                        for (int sx = x * s.Step; sx < Math.Min(original.Width, (x + 1) * s.Step); sx++)
                        {
                            int src = 4 * (sy * original.Width + sx);
                            sum += original.Bgra[src + c] * original.Bgra[src + 3];
                        }
                        int divisor = 255 * s.Step * s.Step;
                        int expected = (sum + divisor / 2) / divisor;
                        byte actual = pixels[(rect.Y + y) * SpellPoseAtlas.PageSize + rect.X + x];
                        if (Math.Abs(actual - expected) > 1)
                            throw new InvalidDataException($"Coverage mismatch in {s.Name} #{index} at {x},{y}, channel {c}.");
                    }
                }
                checkedFrames++;
            }
        }
        Console.WriteLine($"Compared {checkedFrames} nonempty directional poses with the source spell libraries.");
    }
}
