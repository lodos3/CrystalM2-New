#nullable enable
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using CrystalM2.Rendering.Core;

namespace CrystalM2.ArmourCreate;

// One immutable design snapshot. The runtime and offline exporter use identical
// per-pose pixel generation and native LIB encoding.
public sealed class ArmourAtlas : IDisposable
{
    private readonly JsonDocument _poses;
    private readonly JsonElement[] _frames;
    private readonly string _artRoot;
    private readonly Dictionary<int, Sheet> _sheets = new();
    private readonly Dictionary<int, byte[]> _records = new();
    public int Count => _frames.Length;

    public ArmourAtlas(string dataRoot, string artRoot)
    {
        _artRoot = artRoot;
        using var source = new LegacyMirLibReader(File.OpenRead(Path.Combine(dataRoot, "CArmour", "00.Lib")));
        _poses = JsonDocument.Parse(File.ReadAllText(Path.Combine(artRoot, "poses.json")));
        _frames = _poses.RootElement.GetProperty("frames").EnumerateArray().ToArray();
        if (source.Count != _frames.Length || _poses.RootElement.GetProperty("regions").GetArrayLength() != 11)
        {
            _poses.Dispose();
            throw new InvalidDataException("Armour pose atlas does not match the naked body library.");
        }
    }

    public byte[] GetRecord(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (_records.TryGetValue(index, out byte[]? record)) return record;
        var pose = _frames[index];
        using Bitmap image = Compose(pose, _artRoot, _sheets);
        var origin = pose.GetProperty("origin");
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            WriteFrame(writer, image.Width, image.Height, origin[0].GetInt32(), origin[1].GetInt32(), Bgra(image));
        record = stream.ToArray();
        _records.Add(index, record);
        return record;
    }

    public void Export(string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        if (File.Exists(destination)) throw new IOException($"Generated library already exists: {destination}");
        string staging = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(3);
                writer.Write(Count);
                writer.Write(0);
                int[] offsets = new int[Count];
                for (int i = 0; i < Count; i++) writer.Write(0);
                for (int i = 0; i < Count; i++)
                {
                    offsets[i] = checked((int)stream.Position);
                    writer.Write(GetRecord(i));
                }
                int frameSet = checked((int)stream.Position);
                writer.Write(0);
                stream.Position = 8;
                writer.Write(frameSet);
                foreach (int offset in offsets) writer.Write(offset);
            }
            using (var check = new LegacyMirLibReader(File.OpenRead(staging)))
            {
                if (check.Count != Count) throw new InvalidDataException("Exported frame count mismatch.");
                for (int i = 0; i < Count; i++)
                {
                    if (!check.TryReadFrame(i, out var frame) || frame is null)
                        throw new InvalidDataException($"Unreadable generated frame {i}.");
                }
            }
            File.Move(staging, destination);
        }
        finally { if (File.Exists(staging)) File.Delete(staging); }
    }

    public Bitmap GetPreview(int index) => Compose(_frames[index], _artRoot, _sheets);

    public void Dispose()
    {
        _poses.Dispose();
        foreach (var sheet in _sheets.Values) sheet.Dispose();
        _sheets.Clear();
        _records.Clear();
    }

private static Bitmap Compose(JsonElement pose, string artRoot,
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

private static void WriteFrame(BinaryWriter writer,
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

private static void WriteCompressed(BinaryWriter writer, byte[] bytes)
{
    using var buffer = new MemoryStream();
    using (var zip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        zip.Write(bytes);
    byte[] compressed = buffer.ToArray();
    writer.Write(compressed.Length);
    writer.Write(compressed);
}

private static byte[] Bgra(Bitmap bitmap)
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

private static void SetBgra(Bitmap bitmap, byte[] bytes)
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
