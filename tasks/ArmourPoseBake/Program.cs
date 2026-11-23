using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using CrystalM2.Rendering.Core;

if (args.Length != 2) throw new ArgumentException("Supply the client Data folder and an output folder.");
string output = Directory.CreateDirectory(args[1]).FullName;
LegacyMirLibReader Open(string name) => new(File.OpenRead(Path.Combine(args[0], name + ".Lib")));
using var body = Open("CArmour/00");
using var fitted = Open("CArmour/01");
using var cape = Open("CArmour/03");
using var plate = Open("CArmour/06");
using var robe = Open("CArmour/12");
using var hair = Open("CHair/00");
LegacyMirLibReader[] libraries = [body, fitted, cape, plate, robe, hair];
string[] regions = ["head","neck","shoulderA","shoulderB","chest","waist","pelvis","kneeA","kneeB","bootA","bootB"];
var frames = new object?[body.Count];
const int size = 1024;
byte[] page = new byte[size*size*4];
int pageIndex=0, left=1, top=1, shelf=0, mapped=0;
int[] examples = [16,60,147,317,414,824];
using var preview = new Bitmap(examples.Length*106,134);
using var canvas = Graphics.FromImage(preview);
canvas.Clear(Color.FromArgb(24,27,32));
for (int index=0; index<body.Count; index++)
{
    var samples = new LegacySpriteFrame?[libraries.Length];
    for (int n=0;n<libraries.Length;n++)
        if (libraries[n].TryReadFrame(index,out var f) && f is not null)
        {
            BakedShadowRepair.StripFromMask(f.Bgra,f.Width,f.Height,f.Width*4);
            samples[n]=f;
        }
    if (samples[0] is not {} naked) continue;
    var skin = Pixels(naked);
    if (skin.Count==0) continue;
    var anchors = Anchors(skin,naked,samples[5],index>=808);
    var bounds = samples.Take(5).OfType<LegacySpriteFrame>().Select(f =>
        new Rectangle(f.OffsetX,f.OffsetY,f.Width,f.Height)).Aggregate(Rectangle.Union);
    int w=bounds.Width,h=bounds.Height;
    if (w+2>size || h+2>size) throw new InvalidDataException($"Oversized pose {index}.");
    if (left+w>=size) {left=1; top+=shelf; shelf=0;}
    if (top+h>=size) {SavePage(); left=top=1; shelf=0;}
    int example=Array.IndexOf(examples,index);
    using var overlay=example>=0 ? new Bitmap(w,h) : null;
    for(int y=0;y<h;y++)
    for(int x=0;x<w;x++)
    {
        int cellX=bounds.X+x,cellY=bounds.Y+y;
        int original=Pixel(naked,cellX,cellY), alpha=(int)((uint)original>>24);
        int slim=Coverage(original,Pixel(samples[1],cellX,cellY));
        int outer=(Coverage(original,Pixel(samples[2],cellX,cellY))+
            Coverage(original,Pixel(samples[3],cellX,cellY))+Coverage(original,Pixel(samples[4],cellX,cellY))+1)/3;
        int p=((top+y)*size+left+x)*4;
        page[p]=(byte)outer; page[p+1]=(byte)slim; page[p+2]=(byte)alpha;
        if (overlay!=null)
        {
            // A test cuirass: crop the fitted garment in the pose's torso coordinates.
            Vector2 axis=anchors[6]-anchors[0], pos=new Vector2(cellX,cellY)-anchors[0];
            float depth=Vector2.Dot(pos,axis)/Math.Max(1,axis.LengthSquared());
            bool armour=slim>96 && depth>.23f && depth<.96f;
            var colour=Color.FromArgb(original);
            if (armour) {
                int shade=Pixel(samples[1],cellX,cellY)>>8 & 255;
                colour=Color.FromArgb(255,24+shade/5,54+shade/2,57+shade/2);
            }
            overlay.SetPixel(x,y,colour);
        }
    }
    frames[index]=new {page=pageIndex,rect=new[]{left,top,w,h},origin=new[]{bounds.X,bounds.Y},
        anchors=anchors.Select(a=>new[]{(int)a.X,(int)a.Y}).ToArray()};
    if (overlay!=null)
    {
        int dx=example*106+(106-w)/2, dy=21+(100-h)/2;
        canvas.DrawImageUnscaled(overlay,dx,dy);
        foreach(int n in new[]{0,2,3,6,9,10})
            canvas.FillRectangle(n==6?Brushes.Orange:Brushes.Aqua,dx+anchors[n].X-bounds.X-1,dy+anchors[n].Y-bounds.Y-1,3,3);
        canvas.DrawString($"Frame {index}",SystemFonts.MessageBoxFont!,Brushes.White,example*106+10,114);
    }
    mapped++; left+=w+2; shelf=Math.Max(shelf,h+2);
}
SavePage();
File.WriteAllText(Path.Combine(output,"poses.json"),JsonSerializer.Serialize(new {
    femaleFrameOffset=808, regions, frames
}));
preview.Save(Path.Combine(output,"overlay-preview.png"),ImageFormat.Png);
Console.WriteLine($"Mapped {mapped}/{frames.Length} CArmour frames into {pageIndex} mask sheets.");

void SavePage()
{
    using var bitmap=new Bitmap(size,size,PixelFormat.Format32bppRgb);
    var data=bitmap.LockBits(new Rectangle(0,0,size,size),ImageLockMode.WriteOnly,bitmap.PixelFormat);
    Marshal.Copy(page,0,data.Scan0,page.Length);
    bitmap.UnlockBits(data);
    bitmap.Save(Path.Combine(output,$"mask-{pageIndex++:00}.png"),ImageFormat.Png);
    Array.Clear(page);
}

static int Pixel(LegacySpriteFrame? f,int x,int y)
{
    if (f==null || (uint)(x-f.OffsetX)>=f.Width || (uint)(y-f.OffsetY)>=f.Height) return 0;
    int p=4*((y-f.OffsetY)*f.Width+x-f.OffsetX);
    return f.Bgra[p+3]==0 ? 0 : BitConverter.ToInt32(f.Bgra,p);
}

static int Coverage(int body,int armour)
{
    int alpha=(int)((uint)armour>>24);
    if(alpha==0 || body==armour) return 0;
    if((uint)body>>24==0) return alpha;
    int difference=0;
    for(int c=0;c<24;c+=8)
        difference=Math.Max(difference,Math.Abs(((body>>c)&255)-((armour>>c)&255)));
    return alpha*Math.Clamp(difference-5,0,16)/16;
}

static List<Vector2> Pixels(LegacySpriteFrame f)
{
    var points=new List<Vector2>();
    for(int y=0;y<f.Height;y++) for(int x=0;x<f.Width;x++)
        if(f.Bgra[4*(y*f.Width+x)+3]>=64) points.Add(new(x+f.OffsetX,y+f.OffsetY));
    return points;
}

static Vector2[] Anchors(List<Vector2> skin,LegacySpriteFrame body,LegacySpriteFrame? hair,bool female)
{
    static Vector2 Mean(IEnumerable<Vector2> list) => list.Aggregate(Vector2.Zero,(sum,p)=>sum+p)/list.Count();
    Vector2 Snap(Vector2 p) => skin.MinBy(q=>Vector2.DistanceSquared(q,p));
    var hairPixels=hair==null ? [] : Pixels(hair);
    var head=Snap(hairPixels.Count>0 ? Mean(hairPixels) : Mean(skin.OrderBy(p=>p.Y).Take(12)));
    var briefs=skin.Where(p=>{
        int rgb=Pixel(body,(int)p.X,(int)p.Y),r=rgb>>16&255,g=rgb>>8&255,b=rgb&255;
        return female ? r>g*1.7+20 && r>b*1.8+15 : b>r+3 && b>g;
    }).OrderByDescending(p=>Vector2.DistanceSquared(p,head)).ToArray();
    // Female base art also has a red top. Only the more distant red cluster identifies the hips.
    Vector2 pelvis=Snap(briefs.Length>=3 ? Mean(briefs.Take(Math.Max(3,briefs.Length/2))) : Mean(skin));
    Vector2 delta=pelvis-head, down=delta.LengthSquared()>1?Vector2.Normalize(delta):Vector2.UnitY;
    Vector2 across=new(down.Y,-down.X);
    Vector2 Torso(float t)=>Vector2.Lerp(head,pelvis,t);
    Vector2 Boot(int side)
    {
        var foot=skin.Where(p=>Vector2.Dot(p-pelvis,across)*side>=0)
            .OrderByDescending(p=>Vector2.Dot(p-pelvis,down)).Take(5).ToArray();
        return foot.Length==0 ? pelvis : Snap(Mean(foot));
    }
    Vector2 bootA=Boot(-1),bootB=Boot(1);
    float shoulderWidth=Math.Clamp(delta.Length()*.27f,4,9);
    return [head,Snap(Torso(.26f)),Snap(Torso(.38f)-across*shoulderWidth),Snap(Torso(.38f)+across*shoulderWidth),
        Snap(Torso(.51f)),Snap(Torso(.83f)),pelvis,Snap(Vector2.Lerp(pelvis,bootA,.52f)),
        Snap(Vector2.Lerp(pelvis,bootB,.52f)),bootA,bootB];
}
