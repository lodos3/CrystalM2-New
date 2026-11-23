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

foreach (int index in new[] {0, 16, 60, 824})
{
    var frame = frames[index];
    int[] origin = frame.GetProperty("origin").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
    int[] rect = frame.GetProperty("rect").EnumerateArray().Select(x=>x.GetInt32()).ToArray();
    PointF[] anchors = frame.GetProperty("anchors").EnumerateArray().Select(a =>
        new PointF(a[0].GetInt32()-origin[0],a[1].GetInt32()-origin[1])).ToArray();
    using var design = new Bitmap(rect[2],rect[3],PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(design)) ArmourPainter.Paint(g,anchors);
    int painted=0, blue=0, white=0, gold=0;
    for (int y=0;y<design.Height;y++)
    for (int x=0;x<design.Width;x++)
    {
        Color c=design.GetPixel(x,y);
        if (c.A<128) continue;
        painted++;
        if (c.B > c.R+20 && c.B > c.G+20) blue++;
        if (c.R>180 && c.G>180 && c.B>180) white++;
        if (c.R>120 && c.R>c.B*1.4 && c.G>c.B) gold++;
    }
    if (painted<15 || blue<2 || white<2 || gold<1)
        throw new Exception($"The original colour layers did not render at frame {index}: " +
            $"pixels={painted}, blue={blue}, white={white}, gold={gold}.");
}
Console.WriteLine($"Verified {count} real CArmour masks and four original armour poses.");
