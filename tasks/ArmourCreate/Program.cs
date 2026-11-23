using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using CrystalM2.ArmourCreate;
using CrystalM2.Rendering.Core;

if (args.Length is < 3 or > 4)
    throw new ArgumentException("Usage: ArmourCreate <client Data directory> <Art/CArmour directory> <new GArmour/NN.Lib> [preview.png]");

string dataRoot = Path.GetFullPath(args[0]);
string artRoot = Path.GetFullPath(args[1]);
string destination = Path.GetFullPath(args[2]);
string? previewFile = args.Length == 4 ? Path.GetFullPath(args[3]) : null;
if (!Directory.Exists(Path.GetDirectoryName(destination)))
    throw new DirectoryNotFoundException("Create the destination CArmour directory first.");
if (File.Exists(destination))
    throw new IOException($"Refusing to overwrite the existing armour: {destination}");
if (previewFile is not null && File.Exists(previewFile))
    throw new IOException($"Refusing to overwrite the preview: {previewFile}");
if (Path.GetFileName(destination).Equals("00.Lib", StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("GArmour/00.Lib is reserved; the naked body is always CArmour/00.Lib.");
if (!Path.GetFileName(Path.GetDirectoryName(destination)!).Equals("GArmour", StringComparison.OrdinalIgnoreCase) ||
    !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(destination), @"^(0[1-9]|[1-9][0-9])\.Lib$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    throw new ArgumentException("Generated armour must be saved to GArmour/01.Lib through GArmour/99.Lib.");

using var source = new LegacyMirLibReader(File.OpenRead(Path.Combine(dataRoot, "CArmour", "00.Lib")));
using var poses = JsonDocument.Parse(File.ReadAllText(Path.Combine(artRoot, "poses.json")));
JsonElement[] mapped = poses.RootElement.GetProperty("frames").EnumerateArray().ToArray();
if (mapped.Length != source.Count)
    throw new InvalidDataException($"Pose count {mapped.Length} does not match naked body count {source.Count}.");
if (poses.RootElement.GetProperty("regions").GetArrayLength() != 11)
    throw new InvalidDataException("The pose atlas must contain eleven body anchors.");

var masks = new Dictionary<int, Sheet>();
var offsets = new int[source.Count];
int created = 0;
string staging = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
int[] featured = [0, 16, 60, 147, 317, 414, 808, 824];
using var preview = previewFile is null ? null : new Bitmap(4 * 180, 2 * 180);
using var previewGraphics = preview is null ? null : Graphics.FromImage(preview);
previewGraphics?.Clear(Color.FromArgb(24, 27, 32));

try
{
    using (var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
    using (var writer = new BinaryWriter(stream))
    {
        // Native v3 layout: frame-offset index followed by compressed BGRA frames
        // and a trailing zero-count FrameSet record.
        writer.Write(3);
        writer.Write(source.Count);
        writer.Write(0); // patched with the FrameSet offset.
        for (int i = 0; i < offsets.Length; i++) writer.Write(0);

        for (int index = 0; index < source.Count; index++)
        {
            if (!source.TryReadFrame(index, out LegacySpriteFrame? naked)
                || naked is null) continue;
            offsets[index] = checked((int)stream.Position);
            byte[] pixels;
            int width = naked.Width, height = naked.Height;
            int originX = naked.OffsetX, originY = naked.OffsetY;
            using Bitmap? composite = mapped[index].ValueKind == JsonValueKind.Object
                ? Compose(naked, mapped[index], artRoot, masks) : null;
            if (composite is not null)
            {
                width = composite.Width;
                height = composite.Height;
                originX = mapped[index].GetProperty("origin")[0].GetInt32();
                originY = mapped[index].GetProperty("origin")[1].GetInt32();
                pixels = Bgra(composite);
                created++;
            }
            else throw new InvalidDataException($"Missing pose mapping for frame {index}.");

            WriteFrame(writer, width, height, originX, originY, pixels);
            int slot = Array.IndexOf(featured, index);
            if (slot >= 0 && composite is not null && previewGraphics is not null)
            {
                int px = slot % 4 * 180 + 15, py = slot / 4 * 180 + 15;
                previewGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                float size = Math.Min(145f / composite.Width, 140f / composite.Height);
                int dw = (int)MathF.Round(composite.Width * size);
                int dh = (int)MathF.Round(composite.Height * size);
                previewGraphics.DrawImage(composite, px + (145 - dw) / 2, py + (140 - dh) / 2, dw, dh);
                using var label = new SolidBrush(Color.White);
                previewGraphics.DrawString($"Frame {index}", SystemFonts.MessageBoxFont!, label,
                    slot % 4 * 180 + 12, slot / 4 * 180 + 153);
            }
        }
        int frameSet = checked((int)stream.Position);
        writer.Write(0); // No custom FrameSet overrides: default player actions remain authoritative.
        stream.Position = sizeof(int) * 2;
        writer.Write(frameSet);
        stream.Position = sizeof(int) * 3;
        foreach (int offset in offsets) writer.Write(offset);
        writer.Flush();
    }

    // Reopen the actual output through the production decoder, not a duplicate decoder.
    using (var check = new LegacyMirLibReader(File.OpenRead(staging)))
    {
        if (check.Count != source.Count) throw new InvalidDataException("Written frame count differs.");
        foreach (int index in featured)
        {
            if (index >= check.Count || mapped[index].ValueKind != JsonValueKind.Object) continue;
            if (!check.TryReadFrame(index, out var image, includeMask: true) || image is null)
                throw new InvalidDataException($"Generated frame {index} is unreadable.");
            var rect = mapped[index].GetProperty("rect");
            if (image.Width != rect[2].GetInt32() || image.Height != rect[3].GetInt32())
                throw new InvalidDataException($"Generated frame {index} has incorrect dimensions.");
            int transparent = 0, painted = 0;
            for (int p = 3; p < image.Bgra.Length; p += 4)
            {
                if (image.Bgra[p] == 0) transparent++; else painted++;
            }
            if (painted == 0 || transparent == 0 || image.Mask != null || image.ShadowFlags != 0)
                throw new InvalidDataException($"Frame {index} is not a transparent armour-only overlay.");
        }
    }

    File.Move(staging, destination); // Atomic on the same volume; never overwrite an existing LIB.
    if (previewFile is not null) preview!.Save(previewFile, ImageFormat.Png);
    Console.WriteLine($"Created {created} original armour poses in {destination}");
    if (previewFile is not null) Console.WriteLine($"Preview: {previewFile}");
}
finally
{
    if (File.Exists(staging)) File.Delete(staging);
    foreach (var sheet in masks.Values) sheet.Dispose();
}

static Bitmap Compose(LegacySpriteFrame naked, JsonElement pose, string artRoot,
    Dictionary<int, Sheet> sheets)
{
    int page = pose.GetProperty("page").GetInt32();
    int[] rect = pose.GetProperty("rect").EnumerateArray().Select(v => v.GetInt32()).ToArray();
    int[] origin = pose.GetProperty("origin").EnumerateArray().Select(v => v.GetInt32()).ToArray();
    PointF[] anchors = pose.GetProperty("anchors").EnumerateArray()
        .Select(point => new PointF(point[0].GetInt32()-origin[0], point[1].GetInt32()-origin[1])).ToArray();
    if (rect.Length != 4 || origin.Length != 2 || anchors.Length != 11 ||
        rect[2] <= 0 || rect[3] <= 0 || rect[2] > 320 || rect[3] > 320)
        throw new InvalidDataException("Invalid CArmour pose geometry.");

    if (!sheets.TryGetValue(page, out Sheet? sheet))
        sheets.Add(page, sheet = new Sheet(Path.Combine(artRoot, $"mask-{page:00}.png")));
    if (rect[0] < 0 || rect[1] < 0 || rect[0]+rect[2] > sheet.Width ||
        rect[1]+rect[3] > sheet.Height)
        throw new InvalidDataException("CArmour pose points beyond its mask sheet.");

    // The source body is drawn by the client. LIB output contains ONLY authored armour
    // pixels, otherwise the source character would be blended twice on every frame.
    var result = new Bitmap(rect[2], rect[3], PixelFormat.Format32bppArgb);
    using var design = new Bitmap(result.Width, result.Height, PixelFormat.Format32bppArgb);
    using (var paint = Graphics.FromImage(design)) ArmourPainter.Paint(paint, anchors);
    byte[] overlay = Bgra(design);
    for (int y = 0; y < design.Height; y++)
    for (int x = 0; x < design.Width; x++)
    {
        int i = (y * design.Width + x) * 4;
        if (overlay[i+3] == 0) continue;
        int allow = sheet.Coverage(rect[0]+x, rect[1]+y);
        // RGB data channels encode skin, fitted seams and loose silhouettes.
        // Low, one-example blue coverage is allowed without reusing its original colour.
        overlay[i+3] = (byte)(overlay[i+3] * Math.Min(255, allow*3) / 255);
    }
    SetBgra(design, overlay);
    using (var canvas = Graphics.FromImage(result)) canvas.DrawImageUnscaled(design, 0, 0);
    return result;
}

static void WriteFrame(BinaryWriter writer,
    int width, int height, int offsetX, int offsetY, byte[] bgra)
{
    writer.Write(checked((short)width));
    writer.Write(checked((short)height));
    writer.Write(checked((short)offsetX));
    writer.Write(checked((short)offsetY));
    // No inherited baked shadow offsets or original body's secondary effect mask.
    writer.Write((short)0);
    writer.Write((short)0);
    writer.Write((byte)0);
    WriteCompressed(writer, bgra);
}

static void WriteCompressed(BinaryWriter writer, byte[] bytes)
{
    using var buffer = new MemoryStream();
    using (var zip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        zip.Write(bytes);
    byte[] compressed = buffer.ToArray();
    writer.Write(compressed.Length);
    writer.Write(compressed);
}

static byte[] Bgra(Bitmap bitmap)
{
    var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
    BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    try
    {
        byte[] bytes = new byte[checked(bitmap.Width*bitmap.Height*4)];
        if (data.Stride != bitmap.Width*4)
            throw new InvalidDataException("Unsupported bitmap stride.");
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        return bytes;
    }
    finally { bitmap.UnlockBits(data); }
}

static void SetBgra(Bitmap bitmap, byte[] bytes)
{
    if (bytes.Length != bitmap.Width*bitmap.Height*4)
        throw new InvalidDataException("BGRA image does not match the declared dimensions.");
    var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
    BitmapData data = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
    try
    {
        if (data.Stride != bitmap.Width*4)
            throw new InvalidDataException("Unsupported bitmap stride.");
        Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
    }
    finally { bitmap.UnlockBits(data); }
}

sealed class Sheet : IDisposable
{
    private readonly byte[] _pixels;
    public int Width { get; }
    public int Height { get; }

    public Sheet(string path)
    {
        using var input = new Bitmap(path);
        Width = input.Width;
        Height = input.Height;
        using var converted = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        using (var canvas = Graphics.FromImage(converted)) canvas.DrawImageUnscaled(input, 0, 0);
        _pixels = new byte[Width*Height*4];
        var data = converted.LockBits(new Rectangle(0,0,Width,Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            if (data.Stride != Width*4) throw new InvalidDataException("Unsupported sheet stride.");
            Marshal.Copy(data.Scan0, _pixels, 0, _pixels.Length);
        }
        finally { converted.UnlockBits(data); }
    }

    public int Coverage(int x, int y)
    {
        int p = (y*Width+x)*4;
        return Math.Max(_pixels[p+2], Math.Max(_pixels[p+1], _pixels[p]));
    }

    public void Dispose() { }
}
