#nullable enable
using System.Drawing;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using CrystalM2.Rendering.Core;

namespace CrystalM2.ArmourCreate;

public sealed record LiveArmourFrame(int Width,int Height,int X,int Y,byte[] Skin,byte[] Surface);

public sealed class LiveArmourAtlas : IDisposable
{
    public const int FrameCount=1616;
    private readonly LegacyMirLibReader _body;
    private readonly JsonDocument _poses;
    private readonly JsonElement[] _frames;
    public LiveArmourAtlas(string data,string art)
    {
        _poses=JsonDocument.Parse(File.ReadAllText(Path.Combine(art,"poses.json")));
        try
        {
            _frames=_poses.RootElement.GetProperty("frames").EnumerateArray().ToArray();
            if (_frames.Length!=FrameCount) throw new InvalidDataException("Invalid classic pose count.");
            _body=new LegacyMirLibReader(File.OpenRead(Path.Combine(data,"CArmour","00.Lib")));
            if (_body.Count!=FrameCount) throw new InvalidDataException("Incompatible classic body library.");
        }
        catch { _body?.Dispose(); _poses.Dispose(); throw; }
    }

    public static bool Supports(int id)=>id is >=2 and <=12;
    public static int Source(int id)=>Supports(id)?id-1:throw new ArgumentOutOfRangeException(nameof(id));

    public LiveArmourFrame Create(int id,int index)
    {
        if (!Supports(id)) throw new ArgumentOutOfRangeException(nameof(id));
        if ((uint)index>=FrameCount) throw new ArgumentOutOfRangeException(nameof(index));
        var body=_body.ReadFrame(index);
        BakedShadowRepair.StripFromMask(body.Bgra,body.Width,body.Height,body.Width*4);
        // Leave room for pauldrons and the divided coat beyond the bare silhouette.
        const int pad=16;
        int w=body.Width+pad*2,h=body.Height+pad*2,x=body.OffsetX-pad,y=body.OffsetY-pad;
        if (w>320 || h>320) throw new InvalidDataException("Oversized live armour pose.");
        byte[] naked=new byte[w*h*4];
        for(int row=0;row<body.Height;row++)
            Buffer.BlockCopy(body.Bgra,row*body.Width*4,naked,((row+pad)*w+pad)*4,body.Width*4);
        var anchors=_frames[index].GetProperty("anchors").EnumerateArray()
            .Select(p=>new Vector2(p[0].GetInt32()-x,p[1].GetInt32()-y)).ToArray();
        var skin=new byte[naked.Length];
        var surface=OriginalArmour.Paint(id,w,h,naked,skin,anchors,ArmourPainter.Facing(index));
        return new(w,h,x,y,skin,surface);
    }

    // Native v2 output with zero shadow metadata; data surface is intentionally
    // not a display-ready sprite. A normal library viewer can inspect skin.
    public void Export(int id,string destination)
    {
        Directory.CreateDirectory(destination);
        string skin=Path.Combine(destination,$"{Source(id):00}_HeadHands.Lib");
        string surface=Path.Combine(destination,$"{Source(id):00}_Surface.Lib");
        if(File.Exists(skin)||File.Exists(surface)) throw new IOException("Output already exists.");
        string s1=skin+"."+Guid.NewGuid().ToString("N")+".tmp";
        string s2=surface+"."+Guid.NewGuid().ToString("N")+".tmp";
        bool moved=false;
        try
        {
            using(var a=new BinaryWriter(File.Open(s1,FileMode.CreateNew)))
            using(var b=new BinaryWriter(File.Open(s2,FileMode.CreateNew)))
            {
                Header(a); Header(b);
                var ia=new int[FrameCount]; var ib=new int[FrameCount];
                for(int i=0;i<FrameCount;i++)
                {
                    var frame=Create(id,i);
                    ia[i]=checked((int)a.BaseStream.Position); ib[i]=checked((int)b.BaseStream.Position);
                    Write(a,frame,frame.Skin); Write(b,frame,frame.Surface);
                }
                Index(a,ia); Index(b,ib);
            }
            foreach(string path in new[]{s1,s2})
            using(var check=new LegacyMirLibReader(File.OpenRead(path)))
                for(int i=0;i<FrameCount;i++) _=check.ReadFrame(i);
            File.Move(s1,skin); moved=true; File.Move(s2,surface);
        }
        catch { if(moved) File.Delete(skin); throw; }
        finally { File.Delete(s1); File.Delete(s2); }
    }
    private static void Header(BinaryWriter w)
    {
        w.Write(2);w.Write(FrameCount);for(int i=0;i<FrameCount;i++)w.Write(0);
    }
    private static void Index(BinaryWriter w,int[] entries)
    {
        w.BaseStream.Position=8;foreach(int i in entries)w.Write(i);
    }
    private static void Write(BinaryWriter w,LiveArmourFrame f,byte[] bytes)
    {
        w.Write((short)f.Width);w.Write((short)f.Height);w.Write((short)f.X);w.Write((short)f.Y);
        w.Write((short)0);w.Write((short)0);w.Write((byte)0);
        using var stream=new MemoryStream();
        using(var zip=new GZipStream(stream,CompressionLevel.Fastest,true))zip.Write(bytes);
        var encoded=stream.ToArray();w.Write(encoded.Length);w.Write(encoded);
    }
    public void Dispose()
    {
        _body.Dispose();
        _poses.Dispose();
    }
}
