using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using CrystalM2.Rendering.Core;
using Client.MirScenes;
using Client.Rendering;
using Vortice.Direct3D;

internal static class WingIntersectionChecks
{
    // Exactly partitions the 808 classic frames. Same effect indices and
    // intervals as FrameSet.Player. Attack4 aliases part of the mounted bank
    // and is exported separately below without changing the partition.
    private readonly record struct Sequence(string Name,int Start,int Count,int Delay,int WingStart,int WingCount,bool Mounted=false);
    private static readonly Sequence[] Sequences =
    [
        new("standing",0,4,500,0,8), new("walking",32,6,100,64,6),
        new("running",80,6,100,112,6), new("stance",128,1,1000,160,1),
        new("attack1",136,6,100,168,6), new("attack2",184,6,100,216,6),
        new("attack3",232,8,100,264,8), new("spell",296,6,100,328,6),
        new("harvest",344,2,300,376,2), new("struck",360,3,100,392,3),
        new("die",384,4,100,416,4), new("mount-standing",416,4,500,448,4,true),
        new("mount-walking",448,8,100,480,8,true), new("mount-running",512,6,100,544,6,true),
        new("mount-struck",560,3,100,592,3,true), new("mount-attack",584,6,100,616,6,true),
        new("fishing-cast",632,8,100,0,0), new("fishing-wait",696,6,120,0,0),
        new("fishing-reel",744,8,100,0,0)
    ];
    private const int CellW=192,CellH=168,SheetW=CellW*4,SheetH=CellH*2;
    private static readonly Vector2 Origin=new(CellW/2f-24,112);

    internal static void Render(string input,string destination,int subframes)
    {
        if(subframes is <1 or >4)throw new ArgumentOutOfRangeException(nameof(subframes),"Use 1-4 samples per body frame.");
        string[] paths=File.Exists(input)?[input]:
            new[]{"00","01","03","06","12"}.Select(s=>Path.Combine(input,"CArmour",s+".Lib")).Where(File.Exists).ToArray();
        if(paths.Length==0)throw new FileNotFoundException("Supply a classic CArmour LIB or client Data directory.");
        if(Sequences.Sum(s=>s.Count*8)!=808)throw new InvalidDataException("Incomplete classic body bank.");
        Directory.CreateDirectory(destination);
        using var window=new Form();
        using var device=new RendererDevice(window.Handle,CellW,CellH,DriverType.Warp);
        using var target=new Texture(device,CellW,CellH,true);
        using var surface=target.GetSurface();
        device.SetRenderTarget(surface);
        var pool=new SpellParticlePool(ArmourWingParticles.ParticleCapacity);
        using var report=new StreamWriter(Path.Combine(destination,"intersections.csv"));
        report.WriteLine("armour,wing,gender,action,direction,body_frame,subframe,body_pixels,far_overlap,near_overlap,near_overlap_outside_socket,root_lag_px,missing_wing_art");
        long samplesRendered=0,missingFrames=0;
        foreach(string path in paths)
        {
            string armourName=Path.GetFileNameWithoutExtension(path);
            using var lib=new LegacyMirLibReader(File.OpenRead(path));
            if(lib.Count!=1616)throw new InvalidDataException($"{path}: expected classic male/female 1616-frame bank.");
            // Decode every body/armour silhouette once, not for every wing.
            byte[][] masks=new byte[lib.Count][];
            for(int frame=0;frame<lib.Count;frame++)
            {
                byte[] mask=new byte[CellW*CellH];
                if(lib.TryReadFrame(frame,out var sprite)&&sprite!=null)
                {
                    BakedShadowRepair.StripFromMask(sprite.Bgra,sprite.Width,sprite.Height,sprite.Width*4);
                    for(int y=0;y<sprite.Height;y++)for(int x=0;x<sprite.Width;x++)
                    {
                        int dx=(int)Origin.X+sprite.OffsetX+x,dy=(int)Origin.Y+sprite.OffsetY+y;
                        if((uint)dx<CellW&&(uint)dy<CellH)
                            mask[dy*CellW+dx]=sprite.Bgra[(y*sprite.Width+x)*4+3];
                    }
                }
                masks[frame]=mask;
            }

            for(int id=0;id<20;id++)
            {
                if(!ArmourWings.TrySelect(id.ToString(),out byte? selection)||!selection.HasValue)
                    throw new InvalidDataException($"Missing wing selector {id}.");
                byte effect=selection.Value;
                for(int gender=0;gender<2;gender++)
                foreach(var seq in Sequences.Append(new Sequence("attack4",416,6,100,448,6)))
                {
                    int bank=gender*808;
                    string directory=Path.Combine(destination,armourName,$"wing-{id:00}",gender==0?"male":"female");
                    Directory.CreateDirectory(directory);
                    using var gif=new DebugGif(Path.Combine(directory,seq.Name+".gif"),SheetW,SheetH);
                    var smoothers=new WingPoseSmoother[8];
                    byte[] sheet=new byte[SheetW*SheetH];
                    long now=0;
                    for(int frame=0;frame<seq.Count;frame++)
                    for(int sub=0;sub<subframes;sub++)
                    {
                        Array.Clear(sheet);
                        for(int d=0;d<8;d++)
                        {
                            var facing=(MirDirection)d;
                            int index=bank+seq.Start+d*seq.Count+frame;
                            var mask=masks[index];
                            bool idle=seq.Name is "standing" or "stance" or "mount-standing";
                            bool move=seq.Name is "walking" or "running" or "mount-walking" or "mount-running";
                            ArmourWingParticles.TryGetBodyMotion(effect,index,bank,facing,seq.Mounted,out var targetMotion);
                            int wingPose=(gender==1?840:0)+seq.WingStart+
                                (seq.WingCount==0?0:d*seq.WingCount+Math.Min(seq.WingCount-1,(frame*subframes+sub)*seq.WingCount/(seq.Count*subframes)));
                            if(!idle && seq.WingCount>0 && targetMotion.TorsoFrame is { } torso)
                            {
                                int restCount=seq.Mounted?4:8;
                                int restStart=(gender==1?840:0)+(seq.Mounted?448:0)+d*restCount;
                                targetMotion=targetMotion with { LegacyLift=WingPoseAtlas.ReferenceLift(wingPose,
                                    restStart,restCount,torso.Centre.Y,torso.ReferenceCentre.Y) };
                            }
                            targetMotion=WingPoseMotion.Stabilize(targetMotion,idle,move);
                            WingPoseMotion motion=smoothers[d].Advance(targetMotion,now,frame==0&&sub==0,
                                (idle || move) ? .065f : .045f, idle || move ? .30f : .90f);
                            Vector2 shoulder=ArmourWingParticles.Shoulder(effect,seq.Mounted)+(id>=2?motion.Offset:Vector2.Zero);
                            Vector2 socket=Origin+ArmourWingParticles.BackAttachment(shoulder,facing);

                            bool split=id>=2&&(ArmourWings.IsAura(effect)||(facing is MirDirection.Right or MirDirection.Left));
                            bool behind=id>=2&&facing is (MirDirection.Down or MirDirection.DownLeft or MirDirection.DownRight);
                            int near=ArmourWingParticles.NearSide(facing);
                            bool missing=false;
                            byte[] far=split?Layer(-near):behind?Layer(0):new byte[CellW*CellH];
                            byte[] front=split?Layer(near):!behind?Layer(0):new byte[CellW*CellH];
                            int bodyCount=0,farOverlap=0,nearOverlap=0,outside=0;
                            int cellX=d%4*CellW,cellY=d/4*CellH;
                            for(int y=0;y<CellH;y++)for(int x=0;x<CellW;x++)
                            {
                                int i=y*CellW+x;
                                bool body=mask[i]>=64;
                                if(body)bodyCount++;
                                if(body&&far[i]>=32)farOverlap++;
                                if(body&&front[i]>=32)
                                {
                                    nearOverlap++;
                                    if(Vector2.DistanceSquared(new Vector2(x,y),socket)>144)outside++;
                                }
                                int intensity=Math.Max(body?0:far[i],front[i]);
                                // Index 0 white, 1 black, 2-129 pink; near
                                // overlap remains visible on the black silhouette.
                                byte colour=intensity>=12?(byte)(2+intensity*127/255):body?(byte)1:(byte)0;
                                sheet[(cellY+y)*SheetW+cellX+x]=colour;
                            }
                            report.WriteLine(FormattableString.Invariant($"{armourName},{id},{gender},{seq.Name},{d},{index},{sub},{bodyCount},{farOverlap},{nearOverlap},{outside},{Vector2.Distance(targetMotion.Offset,motion.Offset):F3},{missing}"));
                            samplesRendered++;if(missing)missingFrames++;

                            byte[] Layer(int side)
                            {
                                device.Clear(Color.Black);
                                ArmourWingParticles.Emit(pool,effect,id<2?id:0,id<2?wingPose:index,shoulder,facing,now,101,side,motion.Yaw,motion.Roll,motion.Pitch,motion.TorsoFrame,motion.LegacyLift);
                                if(pool.Active.Length==0){missing=true;return new byte[CellW*CellH];}
                                device.SpellParticles.Draw(pool.Active,Origin,now,false,colour:new Vector3(1,.04f,.5f),
                                    emissiveOverlay:true,exposure:1);
                                using var image=device.Capture(surface);
                                return Coverage(image);
                            }
                        }
                        gif.Frame(sheet,Math.Max(2,(int)Math.Round(seq.Delay/(float)subframes/10)));
                        now+=seq.Delay/subframes;
                    }
                    report.Flush();
                }
                Console.WriteLine($"{armourName}: completed wing {id}; {samplesRendered} render samples.");
            }
        }
        Console.WriteLine($"Rendered {samplesRendered} body/wing samples. Missing authored wing art: {missingFrames}. Review intersections.csv; overlap is diagnostic, not automatically a failure.");
    }

    private static byte[] Coverage(Bitmap image)
    {
        var result=new byte[CellW*CellH];
        var data=image.LockBits(new Rectangle(0,0,CellW,CellH),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try
        {
            byte[] row=new byte[CellW*4];
            for(int y=0;y<CellH;y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);
                for(int x=0;x<CellW;x++)result[y*CellW+x]=Math.Max(row[x*4+2],row[x*4]);
            }
        }
        finally{image.UnlockBits(data);}
        return result;
    }

    // Minimal GIF89a indexed writer. Clear the LZW dictionary before codes
    // exceed 9 bits; this retains valid streaming compression without an
    // external encoder or retaining thousands of bitmaps in RAM.
    private sealed class DebugGif:IDisposable
    {
        private readonly BinaryWriter writer;
        private readonly int width,height;
        internal DebugGif(string path,int w,int h)
        {
            width=w;height=h;writer=new BinaryWriter(File.Create(path));
            writer.Write("GIF89a"u8.ToArray());writer.Write((ushort)w);writer.Write((ushort)h);
            writer.Write((byte)0xF7);writer.Write((byte)0);writer.Write((byte)0);
            for(int i=0;i<256;i++)
            {
                int r,g,b;
                if(i==0)r=g=b=245;
                else if(i==1)r=g=b=0;
                else if(i<=129){float t=(i-2)/127f;r=255;g=(int)(220-205*t);b=(int)(230-80*t);}
                else r=g=b=i;
                writer.Write((byte)r);writer.Write((byte)g);writer.Write((byte)b);
            }
            writer.Write(new byte[]{0x21,0xff,11});writer.Write("NETSCAPE2.0"u8.ToArray());
            writer.Write(new byte[]{3,1,0,0,0});
        }
        internal void Frame(byte[] pixels,int delay)
        {
            if(pixels.Length!=width*height)throw new ArgumentException("GIF size mismatch.");
            writer.Write(new byte[]{0x21,0xf9,4,0x04});
            writer.Write((ushort)delay);writer.Write((byte)0);writer.Write((byte)0);
            writer.Write((byte)0x2c);writer.Write((ushort)0);writer.Write((ushort)0);
            writer.Write((ushort)width);writer.Write((ushort)height);writer.Write((byte)0);
            writer.Write((byte)8);
            using var compressed=new MemoryStream();
            int bits=0,buffer=0;
            void Code(int code)
            {
                buffer|=code<<bits;bits+=9;
                while(bits>=8){compressed.WriteByte((byte)buffer);buffer>>=8;bits-=8;}
            }
            var dictionary=new Dictionary<int,int>(256);int next=258;
            Code(256);int prefix=pixels[0];
            for(int i=1;i<pixels.Length;i++)
            {
                int key=(prefix<<8)|pixels[i];
                if(dictionary.TryGetValue(key,out int existing)){prefix=existing;continue;}
                Code(prefix);
                if(next<510)dictionary.Add(key,next++);
                else{Code(256);dictionary.Clear();next=258;}
                prefix=pixels[i];
            }
            Code(prefix);Code(257);
            if(bits>0)compressed.WriteByte((byte)buffer);
            byte[] bytes=compressed.ToArray();
            for(int i=0;i<bytes.Length;i+=255)
            {
                int count=Math.Min(255,bytes.Length-i);
                writer.Write((byte)count);writer.Write(bytes,i,count);
            }
            writer.Write((byte)0);
        }
        public void Dispose(){writer.Write((byte)0x3b);writer.Dispose();}
    }
}
