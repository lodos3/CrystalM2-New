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
    private readonly string _data;
    private readonly JsonDocument _poses;
    private readonly JsonElement[] _frames;
    private readonly Dictionary<int,LegacyMirLibReader> _sources=new();
    public LiveArmourAtlas(string data,string art)
    {
        _data=data;
        _poses=JsonDocument.Parse(File.ReadAllText(Path.Combine(art,"poses.json")));
        _frames=_poses.RootElement.GetProperty("frames").EnumerateArray().ToArray();
        if(_frames.Length!=FrameCount) { _poses.Dispose(); throw new InvalidDataException("Invalid classic pose count."); }
    }

    public static bool Supports(int id)=>ArmourDesign.Supports(id);
    public static int Source(int id)=>ArmourDesign.Resolve(id).Source;

    public LiveArmourFrame Create(int id,int index)
    {
        if((uint)index>=FrameCount) throw new ArgumentOutOfRangeException(nameof(index));
        var naked=Open(0);
        var fitted=Open(Source(id));
        var hands=Open(2);
        if(naked.Count!=FrameCount || fitted.Count!=FrameCount) throw new InvalidDataException("Incompatible classic library.");
        var a=naked.ReadFrame(index);
        var b=fitted.ReadFrame(index);
        BakedShadowRepair.StripFromMask(a.Bgra,a.Width,a.Height,a.Width*4);
        BakedShadowRepair.StripFromMask(b.Bgra,b.Width,b.Height,b.Width*4);
        var reference=Source(id)==2?b:hands.ReadFrame(index);
        if(!ReferenceEquals(reference,b))
            BakedShadowRepair.StripFromMask(reference.Bgra,reference.Width,reference.Height,reference.Width*4);
        int x=Math.Min(a.OffsetX,b.OffsetX),y=Math.Min(a.OffsetY,b.OffsetY);
        int w=Math.Max(a.OffsetX+a.Width,b.OffsetX+b.Width)-x;
        int h=Math.Max(a.OffsetY+a.Height,b.OffsetY+b.Height)-y;
        if(w<=0||h<=0||w>320||h>320) throw new InvalidDataException("Oversized live armour pose.");
        byte[] first=Align(a),second=Align(b);
        var anchors=_frames[index].GetProperty("anchors").EnumerateArray()
            .Select(p=>new Vector2(p[0].GetInt32()-x,p[1].GetInt32()-y)).ToArray();
        var split=LiveArmourCoverage.Split(w,h,first,second,anchors,Align(reference));
        var sources=new Dictionary<int,byte[]>();
        sources[Source(id)]=second; sources[2]=Align(reference);
        var donors=ArmourDesign.Resolve(id).Donors.Select(source=>
        {
            if(sources.TryGetValue(source,out var aligned))return aligned;
            // An unavailable optional donor falls back to the fitted base;
            // never disable the whole appearance because one donor is absent.
            if(!File.Exists(Path.Combine(_data,"CArmour",$"{source:00}.Lib")))return second;
            var reader=Open(source);
            if(!reader.TryReadFrame(index,out var donor)||donor==null)return second;
            BakedShadowRepair.StripFromMask(donor.Bgra,donor.Width,donor.Height,donor.Width*4);
            aligned=Align(donor);sources.Add(source,aligned);return aligned;
        }).ToArray();
        var surface=ArmourPatternTransfer.Compose(w,h,anchors,first,second,split.Armour,donors);
        return new(w,h,x,y,split.Skin,surface);

        byte[] Align(LegacySpriteFrame frame)
        {
            byte[] dst=new byte[w*h*4];
            // Reference clothes can have a different trim extent. Align
            // only their intersection with the chosen pose, never shift it.
            int left=Math.Max(x,frame.OffsetX),right=Math.Min(x+w,frame.OffsetX+frame.Width);
            int top=Math.Max(y,frame.OffsetY),bottom=Math.Min(y+h,frame.OffsetY+frame.Height);
            if(right<=left||bottom<=top)return dst;
            for(int row=top;row<bottom;row++)
                Buffer.BlockCopy(frame.Bgra,((row-frame.OffsetY)*frame.Width+left-frame.OffsetX)*4,dst,
                    ((row-y)*w+left-x)*4,(right-left)*4);
            return dst;
        }
    }

    private LegacyMirLibReader Open(int id)
    {
        if(_sources.TryGetValue(id,out var existing))return existing;
        var reader=new LegacyMirLibReader(File.OpenRead(Path.Combine(_data,"CArmour",$"{id:00}.Lib")));
        if(reader.Count!=FrameCount){reader.Dispose();throw new InvalidDataException("Incompatible classic library.");}
        _sources.Add(id,reader);return reader;
    }

    // Native v2 output with zero shadow metadata. Both skin and donor
    // surface can be inspected directly in a standard LIB viewer.
    public void Export(int id,string destination)
    {
        Directory.CreateDirectory(destination);
        string skin=Path.Combine(destination,$"g{id:00}_HeadHands.Lib");
        string surface=Path.Combine(destination,$"g{id:00}_Surface.Lib");
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
        foreach(var reader in _sources.Values)reader.Dispose();
        _sources.Clear();_poses.Dispose();
    }
}
