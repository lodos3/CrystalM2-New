#nullable enable
using System.Drawing;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using CrystalM2.Rendering.Core;

namespace CrystalM2.ArmourCreate;

public sealed record LiveArmourFrame(int Width,int Height,int X,int Y,byte[] Skin,byte[] Surface,byte[][] Layers,byte[] Features);

public sealed class LiveArmourAtlas : IDisposable
{
    public const int FrameCount=1616;
    private readonly string _data;
    private readonly JsonDocument _poses;
    private readonly JsonElement[] _frames;
    private readonly Dictionary<int,LegacyMirLibReader> _sources=new();
    private readonly Dictionary<int,ArmourColourModel> _models=new();
    private readonly Dictionary<(int,int),LinkedListNode<((int,int) Key,Original Original,ArmourMaterialMap Map,long Bytes)>> _materialCache=new();
    private readonly LinkedList<((int,int) Key,Original Original,ArmourMaterialMap Map,long Bytes)> _materialLru=new();
    private long _materialBytes;
    private const long MaterialBudget=24*1024*1024;
    public LiveArmourAtlas(string data,string art)
    {
        _data=data;
        _poses=JsonDocument.Parse(File.ReadAllText(Path.Combine(art,"poses.json")));
        _frames=_poses.RootElement.GetProperty("frames").EnumerateArray().ToArray();
        if(_frames.Length!=FrameCount) { _poses.Dispose(); throw new InvalidDataException("Invalid classic pose count."); }
    }

    public static bool Supports(int id)=>ArmourRecolourDesign.Supports(id);
    public static int Source(int id)=>ArmourRecolourDesign.Resolve(id).Source;

    public LiveArmourFrame Create(int id,int index)
    {
        var design=ArmourRecolourDesign.Resolve(id);
        if((uint)index>=FrameCount)throw new ArgumentOutOfRangeException(nameof(index));
        if(!_models.TryGetValue(design.Source,out var model))
        {
            var histogram=new ArmourColourHistogram();
            // Both sexes, every facing, walking and two attacks. Fixed sample
            // set means every variant and animation uses identical slots.
            foreach(int bank in new[]{0,808})
            for(int direction=0;direction<8;direction++)
            {
                histogram.Add(ReadOriginal(design.Source,bank+direction*4).Parts.Surface);
                histogram.Add(ReadOriginal(design.Source,bank+32+direction*6+2).Parts.Surface);
                histogram.Add(ReadOriginal(design.Source,bank+136+direction*6+2).Parts.Surface);
                histogram.Add(ReadOriginal(design.Source,bank+184+direction*6+3).Parts.Surface);
            }
            model=histogram.Build();_models.Add(design.Source,model);
        }
        var key=(design.Source,index);
        if(!_materialCache.TryGetValue(key,out var node))
        {
            var input=ReadOriginal(design.Source,index);
            var map=ArmourMaterialMap.Analyse(input.Width,input.Height,input.Parts,model);
            long bytes=(long)input.Width*input.Height*28; // source/skin, weights, features
            while(_materialLru.Last!=null&&_materialBytes+bytes>MaterialBudget)
            {
                var last=_materialLru.Last;
                _materialCache.Remove(last.Value.Key);_materialBytes-=last.Value.Bytes;_materialLru.RemoveLast();
            }
            node=_materialLru.AddFirst((key,input,map,bytes));_materialCache.Add(key,node);_materialBytes+=bytes;
        }
        else {_materialLru.Remove(node);_materialLru.AddFirst(node);}
        var original=node.Value.Original;
        var layers=ArmourMaterialReconstruction.Render(original.Parts,node.Value.Map,design.Profile);
        return new(original.Width,original.Height,original.X,original.Y,layers.Skin,layers.Surface,layers.Layers,node.Value.Map.Features);
    }

    private sealed record Original(int Width,int Height,int X,int Y,ArmourSourceParts Parts);
    private Original ReadOriginal(int source,int index)
    {
        if((uint)index>=FrameCount) throw new ArgumentOutOfRangeException(nameof(index));
        var naked=Open(0);
        var fitted=Open(source);
        var hands=Open(2);
        if(naked.Count!=FrameCount || fitted.Count!=FrameCount) throw new InvalidDataException("Incompatible classic library.");
        var a=naked.ReadFrame(index);
        var b=fitted.ReadFrame(index);
        BakedShadowRepair.StripFromMask(a.Bgra,a.Width,a.Height,a.Width*4);
        BakedShadowRepair.StripFromMask(b.Bgra,b.Width,b.Height,b.Width*4);
        var reference=source==2?b:hands.ReadFrame(index);
        if(!ReferenceEquals(reference,b))
            BakedShadowRepair.StripFromMask(reference.Bgra,reference.Width,reference.Height,reference.Width*4);
        // The dressed sprite is the sole geometry source. Naked reference
        // pixels outside its authored rectangle can never enlarge the body.
        int x=b.OffsetX,y=b.OffsetY,w=b.Width,h=b.Height;
        if(w<=0||h<=0||w>320||h>320) throw new InvalidDataException("Oversized live armour pose.");
        byte[] first=Align(a),second=Align(b);
        var anchors=_frames[index].GetProperty("anchors").EnumerateArray()
            .Select(p=>new Vector2(p[0].GetInt32()-x,p[1].GetInt32()-y)).ToArray();
        var split=LiveArmourCoverage.Split(w,h,first,second,anchors,Align(reference));
        return new(w,h,x,y,ArmourLayeredRecolour.Separate(w,h,second,first,split.Skin,anchors));

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

    // Native v2 output with zero shadow metadata. Both skin and recoloured
    // surface can be inspected directly in a standard LIB viewer.
    public void Export(int id,string destination)
    {
        if(!Supports(id))throw new ArgumentOutOfRangeException(nameof(id));
        Directory.CreateDirectory(destination);
        string[] names=["HeadHands","Surface","Layer0","Layer1","Layer2","Layer3","Structure"];
        string[] output=names.Select(n=>Path.Combine(destination,$"g{id:00}_{n}.Lib")).ToArray();
        if(output.Any(File.Exists))throw new IOException("Output already exists; select a new export directory.");
        string[] temporary=output.Select(p=>p+"."+Guid.NewGuid().ToString("N")+".tmp").ToArray();
        var writers=new BinaryWriter?[output.Length];
        var offsets=output.Select(_=>new int[FrameCount]).ToArray();
        var moved=new List<string>();
        try
        {
            for(int j=0;j<writers.Length;j++)
            {
                writers[j]=new BinaryWriter(File.Open(temporary[j],FileMode.CreateNew));
                Header(writers[j]!);
            }
            for(int i=0;i<FrameCount;i++)
            {
                var frame=Create(id,i);
                byte[][] pixels=[frame.Skin,frame.Surface,..frame.Layers,frame.Features];
                for(int j=0;j<writers.Length;j++)
                {
                    var writer=writers[j]!;
                    offsets[j][i]=checked((int)writer.BaseStream.Position);
                    Write(writer,frame,pixels[j]);
                }
            }
            for(int j=0;j<writers.Length;j++)
            {
                Index(writers[j]!,offsets[j]);writers[j]!.Dispose();writers[j]=null;
                using var check=new LegacyMirLibReader(File.OpenRead(temporary[j]));
                for(int i=0;i<FrameCount;i++)_=check.ReadFrame(i);
            }
            for(int j=0;j<output.Length;j++)
            {
                File.Move(temporary[j],output[j]);moved.Add(output[j]);
            }
        }
        catch { foreach(string path in moved)File.Delete(path);throw; }
        finally
        {
            foreach(var writer in writers)writer?.Dispose();
            foreach(string path in temporary)File.Delete(path);
        }
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
        _sources.Clear();_models.Clear();_materialCache.Clear();_materialLru.Clear();_materialBytes=0;_poses.Dispose();
    }
}
