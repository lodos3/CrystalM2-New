using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using CrystalM2.ArmourCreate;

string art = Path.GetFullPath(args.Length == 0 ? "Art/CArmour" : args[0]);
using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(art, "poses.json")));
var root = document.RootElement;
string[] regions = root.GetProperty("regions").EnumerateArray().Select(x => x.GetString()!).ToArray();
if (!regions.SequenceEqual(["head","neck","shoulderA","shoulderB","chest","waist",
    "pelvis","kneeA","kneeB","bootA","bootB"]))
    throw new Exception("The geometry anchors have changed.");
if (root.GetProperty("femaleFrameOffset").GetInt32() != 808)
    throw new Exception("The classic female frame bank offset has changed.");

var frames = root.GetProperty("frames").EnumerateArray().ToArray();
var dimensions = new Dictionary<int, Size>();
int count = 0;
for (int index=0; index<frames.Length; index++)
{
    var frame = frames[index];
    if (frame.ValueKind == JsonValueKind.Null) continue;
    int page = frame.GetProperty("page").GetInt32();
    int[] rect = frame.GetProperty("rect").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
    int[][] anchors = frame.GetProperty("anchors").EnumerateArray()
        .Select(a=>a.EnumerateArray().Select(v=>v.GetInt32()).ToArray()).ToArray();
    if (rect.Length!=4 || anchors.Length!=11 || anchors.Any(a=>a.Length!=2))
        throw new Exception($"Invalid pose frame {index}.");
    if (!dimensions.TryGetValue(page, out Size size))
    {
        using var image = new Bitmap(Path.Combine(art,$"mask-{page:00}.png"));
        dimensions.Add(page, size = image.Size);
    }
    if (rect[0]<0 || rect[1]<0 || rect[2]<=0 || rect[3]<=0 ||
        rect[0]+rect[2]>size.Width || rect[1]+rect[3]>size.Height)
        throw new Exception($"Mask rectangle out of bounds at frame {index}.");
    count++;
}
if (count != 1616) throw new Exception($"Expected 1616 mapped frames, found {count}.");

// The old painter left large parts of the actual fitted silhouette transparent.
// Test real atlas geometry across all eight standing directions, walking and
// attack poses, on both banks. Every fitted garment pixel must remain covered.
foreach (int bank in new[] {0, 808})
foreach (int index in Enumerable.Range(0, 8).Select(d => bank + d * 4)
    .Concat(Enumerable.Range(0, 8).Select(d => bank + 32 + d * 6 + 2))
    .Concat(Enumerable.Range(0, 8).Select(d => bank + 136 + d * 6 + 2)))
{
    var frame = frames[index];
    int[] origin = frame.GetProperty("origin").EnumerateArray().Select(x => x.GetInt32()).ToArray();
    int[] rect = frame.GetProperty("rect").EnumerateArray().Select(x => x.GetInt32()).ToArray();
    PointF[] anchors = frame.GetProperty("anchors").EnumerateArray().Select(a =>
        new PointF(a[0].GetInt32() - origin[0], a[1].GetInt32() - origin[1])).ToArray();
    using var mask = new Bitmap(Path.Combine(art, $"mask-{frame.GetProperty("page").GetInt32():00}.png"));
    int w = rect[2], h = rect[3];
    byte[] geometry = new byte[w * h * 4];
    int fitted = 0;
    for (int y = 0; y < h; y++)
    for (int x = 0; x < w; x++)
    {
        Color c = mask.GetPixel(rect[0] + x, rect[1] + y);
        int p = (y * w + x) * 4;
        geometry[p] = c.B; geometry[p+1] = c.G; geometry[p+2] = c.R; geometry[p+3] = 255;
        if (c.G >= 128) fitted++;
    }
    int direction = ArmourPainter.Facing(index);
    byte[] paint = ArmourPainter.Paint(w, h, anchors, direction, geometry);
    if (fitted == 0) throw new Exception($"Empty fitted reference at {index}.");
    int blue = 0, silver = 0, painted = 0;
    for (int p = 0; p < paint.Length; p += 4)
    {
        if (paint[p+3] < geometry[p+1])
            throw new Exception($"Armour loses fitted coverage at frame {index}, pixel {p/4}.");
        if (geometry[p+1] == 0 && geometry[p+2] == 0 && paint[p+3] != 0)
            throw new Exception($"Floating armour outside the reference silhouette at frame {index}.");
        if (paint[p+3] < 128) continue;
        painted++;
        if (paint[p] > paint[p+2] + 20 && paint[p] > paint[p+1] + 10) blue++;
        if (paint[p+2] > 100 && paint[p+1] > 110 && paint[p] > 120) silver++;
    }
    if (painted < fitted || blue == 0 || silver == 0)
        throw new Exception($"Missing full plate materials at frame {index}.");
}
for (int d = 0; d < 8; d++)
{
    if (ArmourPainter.Facing(d*4) != d || ArmourPainter.Facing(808+32+d*6+2) != d ||
        ArmourPainter.Facing(136+d*6+2) != d)
        throw new Exception("CArmour direction mapping differs from the classic frame bank.");
}
Console.WriteLine("Verified silhouette coverage and material generation across 48 poses.");

using (var atlas = new ArmourAtlas(art, 1616))
{
    // These are the exact compressed records the live MLibrary consumes.
    int[] sample = [0, 16, 60, 824];
    foreach (int index in sample)
    {
        byte[] record = atlas.GetRecord(index);
        if (!ReferenceEquals(record, atlas.GetRecord(index)))
            throw new Exception($"Runtime frame {index} was not cached.");
        using var reader = new BinaryReader(new MemoryStream(record));
        int w = reader.ReadInt16(), h = reader.ReadInt16();
        int x = reader.ReadInt16(), y = reader.ReadInt16();
        var pose = frames[index];
        if (w != pose.GetProperty("rect")[2].GetInt32() || h != pose.GetProperty("rect")[3].GetInt32() ||
            x != pose.GetProperty("origin")[0].GetInt32() || y != pose.GetProperty("origin")[1].GetInt32())
            throw new Exception($"Generated frame {index} lost its pose alignment.");
        if (reader.ReadInt16() != 0 || reader.ReadInt16() != 0 || reader.ReadByte() != 0)
            throw new Exception("An overlay must not inherit body shadows.");
        int length = reader.ReadInt32();
        using var zip = new System.IO.Compression.GZipStream(new MemoryStream(reader.ReadBytes(length)),
            System.IO.Compression.CompressionMode.Decompress);
        using var pixels = new MemoryStream();
        zip.CopyTo(pixels);
        byte[] bgra = pixels.ToArray();
        if (bgra.Length != w*h*4 || !bgra.Where((_, n) => n % 4 == 3).Any(a => a > 0) ||
            !bgra.Where((_, n) => n % 4 == 3).Any(a => a == 0))
            throw new Exception($"Generated frame {index} is not an independent transparent overlay.");
    }
    string folder = Path.Combine(Path.GetTempPath(), "CrystalM2Armour-" + Guid.NewGuid().ToString("N"));
    try
    {
        string path = Path.Combine(folder, "01.Lib");
        atlas.Export(path);
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (reader.ReadInt32() != 3 || reader.ReadInt32() != atlas.Count)
            throw new Exception("Exported library header is invalid.");
        reader.ReadInt32();
        int[] offsets = Enumerable.Range(0, atlas.Count).Select(_ => reader.ReadInt32()).ToArray();
        foreach (int index in sample)
        {
            stream.Position = offsets[index];
            byte[] expected = atlas.GetRecord(index);
            if (!reader.ReadBytes(expected.Length).SequenceEqual(expected))
                throw new Exception($"Export does not preserve the preview frame {index}.");
        }
    }
    finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
Console.WriteLine("Verified cached runtime armour records and exported LIB byte parity.");
