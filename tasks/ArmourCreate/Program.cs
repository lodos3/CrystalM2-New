using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using CrystalM2.ArmourCreate;
using CrystalM2.Rendering.Core;

if (args is ["--live", var data, var art, var output])
{
    using var live = new LiveArmourAtlas(data,art);
    for(int id=2;id<=99;id++) live.Export(id,output);
    Console.WriteLine("Exported 98 classic armour sets (skin, combined surface, four ownership layers and structure map).");
    return;
}

if (args is ["--live-one", var singleData, var singleArt, var singleOutput, var idText])
{
    if(!int.TryParse(idText,out int id)||!LiveArmourAtlas.Supports(id))
        throw new ArgumentException("Live appearance ID must be 2–99.");
    using var live=new LiveArmourAtlas(singleData,singleArt);
    live.Export(id,singleOutput);
    return;
}

if (args is ["--material-source", var sourceData, var sourceArt, var sourceOutput, var sourceText, var profileText])
{
    if(!int.TryParse(sourceText,out int source)||source is <1 or >ArmourRecolourDesign.SourceCount||
        !int.TryParse(profileText,out int profile)||profile is <0 or >=ArmourRecolourDesign.ProfileCount)
        throw new ArgumentException("Source must be 1–30 and finish must be 0–8.");
    using var live=new LiveArmourAtlas(sourceData,sourceArt);
    live.ExportSource(source,profile,sourceOutput);
    return;
}

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
