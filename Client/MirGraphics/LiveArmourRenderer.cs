using CrystalM2.ArmourCreate;
using CrystalM2.Rendering.Core;
using Client.MirScenes;

namespace Client.MirGraphics;

internal static class LiveArmourRenderer
{
    private const int MaxFrames=192;
    private const long MaxBytes=32*1024*1024;
    private sealed record Entry(Texture Skin,Texture Surface,Texture[] Layers,int Width,int Height,int X,int Y,long Bytes);
    private static readonly Dictionary<(byte,int),LinkedListNode<((byte,int) Key,Entry Frame)>> Cache=new();
    private static readonly LinkedList<((byte,int) Key,Entry Frame)> Lru=new();
    private static readonly HashSet<byte> Failed=new();
    private static RendererDevice _owner;
    private static LiveArmourAtlas _atlas;
    private static long _bytes;

    internal static bool TryDraw(byte id,int frame,Point location,Color tint)
    {
        if(!LiveArmourAtlas.Supports(id)||DXManager.Device==null)return false;
        // Generated IDs take precedence over stale legacy exports.
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
        if(!SpriteVisibility.IsVisible(origin.X,origin.Y,entry.Width,entry.Height,Settings.ScreenWidth,Settings.ScreenHeight))
            return true;
        var position=new Vector3(origin.X,origin.Y,0);
        var effect=ArmourLayeredRecolour.Effect(ArmourRecolourDesign.Resolve(id).Profile);
        var device=DXManager.Device;
        if(entry.Surface!=null)
        {
            DXManager.Draw(entry.Surface,null,position,tint);
            DXManager.Draw(entry.Skin,null,position,tint);
            return true;
        }
        for(int i=0;i<entry.Layers.Length;i++)
        {
            var texture=entry.Layers[i];if(texture==null)continue;
            if(i!=effect.Layer||!Settings.Effect||DXManager.GrayScale||effect.Strength<=0)
            {
                DXManager.Draw(texture,null,position,tint);
                continue;
            }
            ShaderEffect previous=device.PixelShader;
            Span<Vector4> constants=stackalloc Vector4[RendererDevice.EffectConstantCount];
            device.CopyPixelShaderConstants(constants);
            try
            {
                device.PixelShader=ShaderEffect.LiveArmour;
                float phase=(CMain.Time%1800)/1800f;
                device.SetPixelShaderConstant(6,[new Vector4(phase,effect.Strength,effect.Mode,1)]);
                DXManager.Draw(texture,null,position,tint);
            }
            finally
            {
                device.Sprite.Flush();device.PixelShader=previous;
                device.SetPixelShaderConstant(0,constants);
            }
        }
        DXManager.Draw(entry.Skin,null,position,tint);
        return true;
    }

    private static Entry Get(byte id,int index)
    {
        var key=(id,index);
        if(Cache.TryGetValue(key,out var node))
        {
            if(!node.Value.Frame.Skin.Disposed&&(node.Value.Frame.Surface==null||!node.Value.Frame.Surface.Disposed)&&node.Value.Frame.Layers.All(x=>x==null||!x.Disposed))
            { Lru.Remove(node);Lru.AddFirst(node);return node.Value.Frame; }
            Remove(node);
        }
        _atlas??=new LiveArmourAtlas(Settings.DataPath,Path.Combine(Settings.DataPath,"ArmourAuthoring"));
        var frame=_atlas.Create(id,index);
        Texture skin=null,surface=null;
        var layers=new Texture[4];
        try
        {
            skin=Upload(frame.Width,frame.Height,frame.Skin);
            long bytes=(long)frame.Width*frame.Height*4;
            bool animated=ArmourLayeredRecolour.Effect(ArmourRecolourDesign.Resolve(id).Profile).Strength>0;
            if(!animated)
            {
                surface=Upload(frame.Width,frame.Height,frame.Surface);
                bytes+=(long)frame.Width*frame.Height*4;
            }
            for(int i=0;animated&&i<layers.Length;i++)
            {
                bool populated=false;
                for(int p=3;p<frame.Layers[i].Length;p+=4)
                    if(frame.Layers[i][p]!=0){populated=true;break;}
                if(!populated)continue;
                layers[i]=Upload(frame.Width,frame.Height,frame.Layers[i]);
                bytes+=(long)frame.Width*frame.Height*4;
            }
            var entry=new Entry(skin,surface,layers,frame.Width,frame.Height,frame.X,frame.Y,bytes);
            while(Lru.Last!=null&&(Cache.Count>=MaxFrames||_bytes+entry.Bytes>MaxBytes))Remove(Lru.Last);
            Cache.Add(key,Lru.AddFirst((key,entry)));_bytes+=entry.Bytes;
            return entry;
        }
        catch { skin?.Dispose();surface?.Dispose();foreach(var layer in layers)layer?.Dispose();throw; }
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
        entry.Skin.Dispose();entry.Surface?.Dispose();foreach(var layer in entry.Layers)layer?.Dispose();
    }

    internal static void Clear()
    {
        while(Lru.Last!=null)Remove(Lru.Last);
        _atlas?.Dispose();_atlas=null;Failed.Clear();_bytes=0;_owner=null;
    }
}
