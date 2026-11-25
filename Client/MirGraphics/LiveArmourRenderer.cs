using CrystalM2.ArmourCreate;
using CrystalM2.Rendering.Core;
using Client.MirScenes;

namespace Client.MirGraphics;

internal static class LiveArmourRenderer
{
    private const int MaxFrames=192;
    private const long MaxBytes=32*1024*1024;
    private sealed record Entry(Texture Skin,Texture Surface,int X,int Y,long Bytes);
    private static readonly Dictionary<(byte,int),LinkedListNode<((byte,int) Key,Entry Frame)>> Cache=new();
    private static readonly LinkedList<((byte,int) Key,Entry Frame)> Lru=new();
    private static readonly HashSet<byte> Failed=new();
    private static RendererDevice _owner;
    private static LiveArmourAtlas _atlas;
    private static long _bytes;

    // Restrained colour accents for the native donor patterns. The design
    // comes from the source artwork, not these four palette values.
    private static readonly uint[,] Palettes={
        {0x202B3D,0xB8C8D6,0x28589B,0xBF9551},
        {0x292D31,0x667D96,0x254B79,0xD1BC82},
        {0x25202B,0x958AA8,0x5A284E,0xC5A273},
        {0x252F28,0x91A99D,0x335E47,0xB7A163},
        {0x32292C,0xA9A2A8,0x803C40,0xCEA479},
        {0x1E2027,0x525C71,0x421B29,0xC7A253},
        {0x382523,0xB49376,0x8B3429,0xDCC181},
        {0x26243B,0x8FA8CF,0x4D4787,0xD9B96F},
        {0x172C37,0x7CACC3,0x225B71,0xBBC9D2},
        {0x2F2A1E,0xAD985D,0x3D6043,0xE1C487},
        {0x241E30,0x9A86AE,0x6D3C87,0xCABCA7}
    };

    internal static bool TryDraw(byte id,int frame,Point location,Color tint)
    {
        if(!LiveArmourAtlas.Supports(id)||DXManager.Device==null)return false;
        if(!ReferenceEquals(_owner,DXManager.Device))
        {
            Clear(); _owner=DXManager.Device;
        }
        if(Failed.Contains(id))return false;
        Entry entry;
        try { entry=Get(id,frame); }
        catch(Exception ex) when(ex is IOException || ex is ArgumentException ||
            ex is InvalidDataException || ex is UnauthorizedAccessException ||
            ex is System.Text.Json.JsonException || ex is InvalidOperationException)
        {
            Failed.Add(id);
            GameScene.Scene?.ChatDialog?.ReceiveChat($"g{id:00} live armour unavailable: {ex.Message}",ChatType.System);
            return false;
        }
        Point origin=new(location.X+entry.X,location.Y+entry.Y);
        if(!SpriteVisibility.IsVisible(origin.X,origin.Y,entry.Surface.Width,entry.Surface.Height,Settings.ScreenWidth,Settings.ScreenHeight))
            return true;
        var device=DXManager.Device;
        ShaderEffect previous=device.PixelShader;
        Span<Vector4> constants=stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        try
        {
            device.PixelShader=ShaderEffect.LiveArmour;
            device.SetPixelShaderConstant(0,[Colour(Palettes[id-2,0]),Colour(Palettes[id-2,1]),
                Colour(Palettes[id-2,2]),Colour(Palettes[id-2,3]),
                new Vector4(-.66f,-.76f,.80f,DXManager.GrayScale?1:0)]);
            DXManager.Draw(entry.Surface,null,new Vector3(origin.X,origin.Y,0),tint);
        }
        finally
        {
            device.Sprite.Flush();
            device.PixelShader=previous;
            device.SetPixelShaderConstant(0,constants);
        }
        DXManager.Draw(entry.Skin,null,new Vector3(origin.X,origin.Y,0),tint);
        return true;
    }

    private static Vector4 Colour(uint rgb)=>new((rgb>>16&255)/255f,(rgb>>8&255)/255f,(rgb&255)/255f,1);
    private static Entry Get(byte id,int index)
    {
        var key=(id,index);
        if(Cache.TryGetValue(key,out var node))
        {
            if(!node.Value.Frame.Skin.Disposed&&!node.Value.Frame.Surface.Disposed)
            { Lru.Remove(node);Lru.AddFirst(node);return node.Value.Frame; }
            Remove(node);
        }
        _atlas??=new LiveArmourAtlas(Settings.DataPath,Path.Combine(Settings.DataPath,"ArmourAuthoring"));
        var frame=_atlas.Create(id,index);
        Texture skin=null,surface=null;
        try
        {
            skin=Upload(frame.Width,frame.Height,frame.Skin);
            surface=Upload(frame.Width,frame.Height,frame.Surface);
            var entry=new Entry(skin,surface,frame.X,frame.Y,(long)frame.Width*frame.Height*8);
            while(Lru.Last!=null&&(Cache.Count>=MaxFrames||_bytes+entry.Bytes>MaxBytes))Remove(Lru.Last);
            Cache.Add(key,Lru.AddFirst((key,entry)));_bytes+=entry.Bytes;
            return entry;
        }
        catch { skin?.Dispose();surface?.Dispose();throw; }
    }

    private static Texture Upload(int w,int h,byte[] data)
    {
        var texture=new Texture(_owner,w,h) { IsWorldArtwork=true };
        try
        {
            var upload=texture.BeginUpload();
            upload.Data.Write(data,0,data.Length);
            texture.EndUpload();
            return texture;
        }
        catch { texture.Dispose();throw; }
    }

    private static void Remove(LinkedListNode<((byte,int) Key,Entry Frame)> node)
    {
        Cache.Remove(node.Value.Key); Lru.Remove(node);
        var entry=node.Value.Frame;_bytes-=entry.Bytes;
        entry.Skin.Dispose();entry.Surface.Dispose();
    }

    internal static void Clear()
    {
        while(Lru.Last!=null)Remove(Lru.Last);
        _atlas?.Dispose();_atlas=null;Failed.Clear();_bytes=0;_owner=null;
    }
}
