using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using CrystalM2.ArmourCreate;

if (args.Length is < 3 or > 4)
    throw new ArgumentException("Usage: ArmourCreate <client Data directory> <Art/CArmour directory> <new GArmour/NN.Lib> [preview.png]");
string destination = Path.GetFullPath(args[2]);
if (!Path.GetFileName(Path.GetDirectoryName(destination)!).Equals("GArmour", StringComparison.OrdinalIgnoreCase) ||
    !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(destination), @"^(0[1-9]|[1-9][0-9])\.Lib$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    throw new ArgumentException("Generated armour must be saved to GArmour/01.Lib through GArmour/99.Lib.");
if (args.Length == 4 && File.Exists(args[3])) throw new IOException("Preview already exists.");
using var atlas = new ArmourAtlas(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
atlas.Export(destination);
if (args.Length == 4)
{
    using var preview = new Bitmap(720, 360);
    using var graphics = Graphics.FromImage(preview);
    graphics.Clear(Color.FromArgb(24, 27, 32));
    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
    int[] featured = [0, 16, 60, 147, 317, 414, 808, 824];
    for (int i = 0; i < featured.Length; i++)
    {
        using Bitmap frame = atlas.GetPreview(featured[i]);
        float scale = Math.Min(145f / frame.Width, 140f / frame.Height);
        int w = (int)(frame.Width * scale), h = (int)(frame.Height * scale);
        graphics.DrawImage(frame, i % 4 * 180 + (180 - w)/2, i / 4 * 180 + 8, w, h);
        graphics.DrawString($"Frame {featured[i]}", SystemFonts.MessageBoxFont!, Brushes.White, i % 4 * 180 + 10, i / 4 * 180 + 155);
    }
    preview.Save(args[3], ImageFormat.Png);
}
Console.WriteLine($"Created {atlas.Count} original armour poses in {destination}");
