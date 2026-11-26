using System.Numerics;

namespace CrystalM2.Rendering.Core;

public sealed record ArmourSourceParts(byte[] Surface,byte[] Skin);
public sealed record ArmourColourLayers(byte[][] Layers,byte[] Skin,byte[] Surface);
public readonly record struct ArmourLayerEffect(int Layer,int Mode,float Strength);

public sealed class ArmourColourModel
{
    private readonly Vector3[] _centres;
    internal ArmourColourModel(Vector3[] centres)=>_centres=centres;
    public int Count=>_centres.Length;
    // Exposure-normalised chroma keeps the same dye together across
    // highlight and shadow. A small value term separates neutral materials.
    internal static Vector3 Feature(Vector3 c)
    {
        float y=ArmourRecolour.Luma(c);
        return new((c.X-c.Y)/(y+.16f),(c.Z-c.Y)/(y+.16f),y*.24f);
    }
    public Vector4 Membership(Vector3 rgb)
    {
        Vector3 point=Feature(rgb);
        Span<float> distances=stackalloc float[4];
        float min=float.PositiveInfinity;
        for(int i=0;i<_centres.Length;i++)
        {
            distances[i]=Vector3.DistanceSquared(point,_centres[i]);
            min=Math.Min(min,distances[i]);
        }
        Span<float> weights=stackalloc float[4];weights.Clear();
        float total=0;
        for(int i=0;i<_centres.Length;i++)
        {
            // Stabilized soft assignment. In ambiguous transitions retain
            // several contributors instead of forcing a nearest-colour edge.
            float v=MathF.Exp(-(distances[i]-min)/.085f);
            weights[i]=v;total+=v;
        }
        return new Vector4(weights[0],weights[1],weights[2],weights[3])/Math.Max(total,.00001f);
    }

    public int Classify(Vector3 rgb)
    {
        Vector3 point=Feature(rgb);
        int best=0;float distance=float.PositiveInfinity;
        for(int i=0;i<_centres.Length;i++)
        {
            float d=Vector3.DistanceSquared(point,_centres[i]);
            if(d<distance){best=i;distance=d;}
        }
        return best;
    }
}

// One model is learned from several native poses of each source library,
// never independently from each animation frame. No random seeds.
public sealed class ArmourColourHistogram
{
    private readonly int[] _counts=new int[32768];
    private readonly Vector3[] _sums=new Vector3[32768];
    public void Add(ReadOnlySpan<byte> pixels)
    {
        if(pixels.Length%4!=0)throw new ArgumentException("Invalid BGRA samples.");
        for(int p=0;p<pixels.Length;p+=4)
        {
            if(pixels[p+3]<96)continue;
            Vector3 c=new(pixels[p+2]/255f,pixels[p+1]/255f,pixels[p]/255f);
            float y=ArmourRecolour.Luma(c);
            // Outlines and near-white highlights remain unchanged and
            // must not monopolise a material cluster.
            if(y<.065f||y>.94f)continue;
            int key=(pixels[p+2]>>3)*1024+(pixels[p+1]>>3)*32+(pixels[p]>>3);
            _counts[key]++;_sums[key]+=c;
        }
    }
    public ArmourColourModel Build()
    {
        var points=Enumerable.Range(0,_counts.Length).Where(i=>_counts[i]>0)
            .Select(i=>(Position:ArmourColourModel.Feature(_sums[i]/_counts[i]),Count:_counts[i],Key:i))
            .OrderByDescending(p=>p.Count).ThenBy(p=>p.Key).ToArray();
        if(points.Length==0)return new([Vector3.Zero]);
        var centres=new List<Vector3>{points[0].Position};
        while(centres.Count<4)
        {
            float best=0, separation=0;Vector3 next=default;
            foreach(var p in points)
            {
                float d=centres.Min(c=>Vector3.DistanceSquared(c,p.Position));
                // Rare but chromatically distinct trim must compete with large plates.
                float score=d*MathF.Pow(p.Count,.22f);
                if(score>best){best=score;separation=d;next=p.Position;}
            }
            // A source with fewer genuinely distinct colours can use fewer layers.
            if(separation<.0036f)break;
            centres.Add(next);
        }
        int k=centres.Count;
        int[] population=new int[k];
        for(int iteration=0;iteration<12;iteration++)
        {
            var sums=new Vector3[k];Array.Clear(population,0,k);
            foreach(var p in points)
            {
                int best=0;float d=float.PositiveInfinity;
                for(int i=0;i<k;i++)
                {
                    float v=Vector3.DistanceSquared(p.Position,centres[i]);
                    if(v<d){d=v;best=i;}
                }
                sums[best]+=p.Position*p.Count;population[best]+=p.Count;
            }
            for(int i=0;i<k;i++)if(population[i]>0)centres[i]=sums[i]/population[i];
        }
        // Most prevalent source dye gets the main colour. Remaining slots
        // are ranked once by prevalence, with deterministic tie ordering.
        return new(Enumerable.Range(0,k).Where(i=>population[i]>0)
            .OrderByDescending(i=>population[i]).ThenBy(i=>i).Select(i=>centres[i]).ToArray());
    }
}

public static class ArmourLayeredRecolour
{
    public readonly record struct DyeRamp(uint Shadow,uint Midtone,uint Highlight);
    // Measured palette ramps from the provided original LIBs. The 18th,
    // 50th and 82nd percentile neighbourhood medians are sampled from
    // four chroma/value clusters, across eight idle and eight attack poses.
    // These are colour references only; no donor geometry is transferred.
    private static readonly DyeRamp[,] ReferencePalettes=
    {
        {new(0x303028,0x686858,0xB0B090),new(0x504020,0x786840,0xB89460),new(0x184048,0x305068,0x588CA0),new(0x184028,0x008080,0x508448)}, // 14.Lib
        {new(0x403018,0x705828,0xB89448),new(0x501810,0x902818,0xB86030),new(0x701000,0xD80000,0xC83000),new(0x182828,0x184028,0x307028)}, // 17.Lib
        {new(0x181818,0x282820,0x303028),new(0x403838,0x484050,0x686058),new(0x282018,0x504028,0x786858),new(0x808480,0xA0A0A0,0xC0C0C0)}, // 24.Lib
        {new(0x584828,0x887048,0xB8A470),new(0x282818,0x484848,0xA0A490),new(0x502010,0x703828,0xA04830),new(0x801018,0xC83428,0xE06458)}, // 13.Lib
        {new(0x282818,0x584828,0xA08C70),new(0x282828,0x484050,0x9894A8),new(0x503018,0x704828,0x987838),new(0x102848,0x305078,0x5880A0)}, // 27.Lib
        {new(0x484050,0x887898,0xC8B4F0),new(0x283860,0x485890,0x6878D0),new(0x184048,0x485878,0x88B8C8),new(0x281880,0x281880,0x1048D8)}, // 18.Lib
        {new(0x383838,0x686468,0xB8B0B8),new(0x683828,0xC06450,0xF89868),new(0x502840,0x884878,0xB86CB0),new(0x181458,0x282478,0x404090)}, // 12.Lib
        {new(0x281810,0x282818,0x484038),new(0x401810,0x703018,0xB84830),new(0x503018,0x704818,0x986838),new(0x182810,0x303810,0x404010)}, // 25.Lib
        {new(0x281818,0x303028,0x605850),new(0x382818,0x503828,0x705438),new(0x583018,0x884828,0xC06040),new(0x182828,0x184048,0x305068)} // 28.Lib
    };
    public static DyeRamp ReferenceRamp(int profile,int slot)
    {
        if((uint)profile>=ReferencePalettes.GetLength(0)||(uint)slot>=4)
            throw new ArgumentOutOfRangeException();
        return ReferencePalettes[profile,slot];
    }

    public static ArmourLayerEffect Effect(int profile)=>profile switch
    {
        0=>new(2,1,.12f), // patinated bronze
        1=>new(2,1,.16f), // ceremonial metal
        2=>new(2,2,.20f), // obsidian iridescence
        3=>new(2,1,.14f), // gilded steel
        4=>new(2,1,.10f), // weathered steel
        5=>new(2,1,.24f), // frost
        6=>new(1,2,.22f), // arcane
        7=>new(2,1,.13f), // oxidised copper
        8=>new(0,2,.27f), // spectral
        _=>new(-1,0,0)
    };

    public static ArmourSourceParts Separate(ReadOnlySpan<byte> fitted,ReadOnlySpan<byte> naked,ReadOnlySpan<byte> isolated)
    {
        if(fitted.Length%4!=0||naked.Length!=fitted.Length||isolated.Length!=fitted.Length)
            throw new ArgumentException("Unaligned original frame.");
        byte[] surface=new byte[fitted.Length],skin=new byte[fitted.Length];
        for(int p=0;p<fitted.Length;p+=4)
        {
            if(fitted[p+3]==0)continue;
            int difference=Math.Max(Math.Abs(fitted[p]-naked[p]),
                Math.Max(Math.Abs(fitted[p+1]-naked[p+1]),Math.Abs(fitted[p+2]-naked[p+2])));
            bool warm=naked[p+2]>=45&&naked[p+2]>=naked[p+1]&&naked[p+2]-naked[p]>=8;
            bool restore=naked[p+3]>0&&warm&&
                ((isolated[p+3]>0&&difference<=32)||difference<=8);
            fitted.Slice(p,4).CopyTo((restore?skin:surface).AsSpan(p,4));
        }
        return new(surface,skin);
    }

    // Use anatomical protection in the runtime/authoring pipeline. The
    // older overload remains available for non-geometric pixel operations.
    public static ArmourSourceParts Separate(int width,int height,ReadOnlySpan<byte> fitted,
        ReadOnlySpan<byte> naked,ReadOnlySpan<byte> isolated,Vector2[] anchors)
    {
        if(width<=0||height<=0||width>320||height>320||anchors.Length!=11||
            fitted.Length!=checked(width*height*4)||naked.Length!=fitted.Length||isolated.Length!=fitted.Length)
            throw new ArgumentException("Invalid anatomical protection frame.");
        int count=width*height;
        var parts=Separate(fitted,naked,isolated);
        var seed=new bool[count];
        var protect=new bool[count];
        Vector2 head=anchors[0],neck=anchors[1],pelvis=anchors[6];
        Vector2 headAxis=neck-head;
        float neckLength=headAxis.Length();
        headAxis=neckLength>=2?headAxis/neckLength:Vector2.UnitY;
        Vector2 sideAxis=new(headAxis.Y,-headAxis.X);
        float radius=Math.Clamp(neckLength+3,6,12);
        Vector2 axis=pelvis-head;
        float bodyLength=axis.Length();axis=bodyLength>1?axis/bodyLength:Vector2.UnitY;
        Vector2 across=new(axis.Y,-axis.X);
        float half=Math.Clamp(Math.Abs(Vector2.Dot(anchors[3]-anchors[2],across))*.5f,4,12);
        for(int i=0;i<count;i++)
        {
            int q=i*4;if(fitted[q+3]==0)continue;
            Vector2 at=new(i%width+.5f,i/width+.5f);
            Vector2 fromHead=at-head;
            float down=Vector2.Dot(fromHead,headAxis);
            float side=Math.Abs(Vector2.Dot(fromHead,sideAxis));
            // Preserve the entire authored head silhouette (including hair,
            // eyes, outline, shaded and neutral pixels), not just warm RGB.
            bool headZone=down>=-radius&&down<=neckLength+.5f&&side<=radius*.75f&&
                fromHead.LengthSquared()<=radius*radius*1.2f;
            if(headZone){protect[i]=true;continue;}
            if(naked[q+3]==0)continue;
            float lateral=Math.Abs(Vector2.Dot(at-neck,across));
            float shoulderDistance=Math.Min(Vector2.Distance(at,anchors[2]),Vector2.Distance(at,anchors[3]));
            bool leg=Vector2.DistanceSquared(at,anchors[7])<16||Vector2.DistanceSquared(at,anchors[8])<16||
                Vector2.DistanceSquared(at,anchors[9])<36||Vector2.DistanceSquared(at,anchors[10])<36;
            bool limb=!leg&&shoulderDistance>=4&&
                (lateral>=half*.72f||Vector2.Dot(fromHead,axis)<0)&&Vector2.Dot(fromHead,axis)<=bodyLength+7;
            int difference=Math.Max(Math.Abs(fitted[q]-naked[q]),
                Math.Max(Math.Abs(fitted[q+1]-naked[q+1]),Math.Abs(fitted[q+2]-naked[q+2])));
            // Trust the anatomically isolated hand mask even for dark
            // fingers and glove edges. A close same-pose reference match
            // at a distal limb can also seed a missed hand.
            if(isolated[q+3]>0||limb&&difference<=16){seed[i]=true;protect[i]=true;}
        }
        // A bounded one-pixel guard band captures shaded fingers and
        // antialiased edges while avoiding a general flesh-hue deletion.
        for(int i=0;i<count;i++)
        {
            int q=i*4;if(protect[i]||fitted[q+3]==0||naked[q+3]==0)continue;
            int x=i%width,y=i/width;
            int difference=Math.Max(Math.Abs(fitted[q]-naked[q]),
                Math.Max(Math.Abs(fitted[q+1]-naked[q+1]),Math.Abs(fitted[q+2]-naked[q+2])));
            if(difference>56)continue;
            for(int dy=-1;dy<=1&&!protect[i];dy++)for(int dx=-1;dx<=1;dx++)
            {
                int sx=x+dx,sy=y+dy;
                if((uint)sx>=width||(uint)sy>=height)continue;
                if(seed[sy*width+sx]){protect[i]=true;break;}
            }
        }
        for(int i=0;i<count;i++)
        {
            int q=i*4;if(!protect[i])continue;
            fitted.Slice(q,4).CopyTo(parts.Skin.AsSpan(q,4));
            parts.Surface.AsSpan(q,4).Clear();
        }
        return parts;
    }

    public static ArmourColourLayers Apply(ArmourSourceParts source,ArmourColourModel model,int profile)
    {
        if((uint)profile>=ReferencePalettes.GetLength(0))throw new ArgumentOutOfRangeException(nameof(profile));
        return ApplyCore(source,model,profile,default);
    }

    // A caller can independently assign any four RGB colours without
    // rebuilding the learned masks or altering the underlying source.
    public static ArmourColourLayers Apply(ArmourSourceParts source,ArmourColourModel model,ReadOnlySpan<uint> colours)
    {
        if(colours.Length!=4)throw new ArgumentException("Provide four RGB palette slots.",nameof(colours));
        return ApplyCore(source,model,-1,colours);
    }

    private static ArmourColourLayers ApplyCore(ArmourSourceParts source,ArmourColourModel model,int profile,ReadOnlySpan<uint> colours)
    {
        if(source.Surface.Length%4!=0||source.Skin.Length!=source.Surface.Length)
            throw new ArgumentException("Unaligned source layers.");
        var layers=Enumerable.Range(0,4).Select(_=>new byte[source.Surface.Length]).ToArray();
        var combined=new byte[source.Surface.Length];
        for(int p=0;p<combined.Length;p+=4)
        {
            byte a=source.Surface[p+3];if(a==0)continue;
            Vector3 c=new(source.Surface[p+2]/255f,source.Surface[p+1]/255f,source.Surface[p]/255f);
            int slot=model.Classify(c);
            Vector3 recoloured=profile>=0?BlendReference(c,ReferenceRamp(profile,slot)):
                MaterialColour(c,colours[slot],slot);
            combined[p]=Byte(recoloured.Z);combined[p+1]=Byte(recoloured.Y);
            combined[p+2]=Byte(recoloured.X);combined[p+3]=a;
            combined.AsSpan(p,4).CopyTo(layers[slot].AsSpan(p,4));
        }
        return new(layers,source.Skin,combined);
    }

    // Four independent dye slots use three-point chromatic ramps. The
    // original value determines the point on the ramp, not a new painted
    // pattern. Preserve source variations in saturation and brightness.
    public static Vector3 MaterialColour(Vector3 original,uint dye,int slot)
    {
        if((uint)slot>=4)throw new ArgumentOutOfRangeException(nameof(slot));
        float y=ArmourRecolour.Luma(original);
        if(y<=.035f||y>=.975f)return original;
        Vector3 target=new((dye>>16&255)/255f,(dye>>8&255)/255f,(dye&255)/255f);
        float ty=ArmourRecolour.Luma(target);
        Vector3 mid=target-new Vector3(ty);
        // Complementary cool shadow and warm/cool reflected highlights.
        // Distinct response per layer: dominant plate, secondary cloth,
        // ornament, and accent. These are stylistic ramps, not inferred BRDFs.
        Vector3 cool=new(-.035f,-.012f,.105f),warm=new(.10f,.025f,-.065f);
        // A perpendicular chroma axis gives coloured materials a genuine
        // split-tone response even when their base hue is already blue.
        // Unlike a fixed blue tint, this adapts to every chosen dye.
        Vector3 cross=Vector3.Cross(new Vector3(.299f,.587f,.114f),mid);
        Vector3 reflected=cross.LengthSquared()>.000001f?Vector3.Normalize(cross)*.065f:ZeroLuma(cool);
        Vector3 shadow=ZeroLuma(mid*.76f+cool*(slot==1?.55f:1.2f)+reflected);
        Vector3 highlight=ZeroLuma(mid*.42f+(slot==2?warm:cool*.45f+warm*.70f)-reflected);
        Vector3 ramp=y<.48f?Vector3.Lerp(shadow,mid,Smooth(.10f,.48f,y)):
            Vector3.Lerp(mid,highlight,Smooth(.48f,.90f,y));
        float preserve=Smooth(.035f,.14f,y)*(1-Smooth(.78f,.975f,y));
        // Small monotonic value shift, so all sculpted relief survives.
        float tone=y+(ty-.5f)*y*(1-y)*.60f*preserve;
        Vector3 native=original-new Vector3(y);
        float sourceChroma=Math.Max(original.X,Math.Max(original.Y,original.Z))-
            Math.Min(original.X,Math.Min(original.Y,original.Z));
        // Subtle native microcolour variation remains visible even in the
        // middle of a fully recoloured region.
        float strength=(.90f-.10f*Smooth(.1f,.7f,sourceChroma))*preserve;
        Vector3 chroma=Vector3.Lerp(native,ramp*(slot==2?1.4f:1.18f),strength);
        float s=1;
        Limit(chroma.X);Limit(chroma.Y);Limit(chroma.Z);
        void Limit(float v)
        {
            if(v>0)s=Math.Min(s,(1-tone)/v);
            if(v<0)s=Math.Min(s,tone/-v);
        }
        return Vector3.Clamp(new Vector3(tone)+chroma*s,Vector3.Zero,Vector3.One);
    }
    // Soft-light changes value instead of replacing the source surface;
    // the reference gradient supplies a colour blend at that retained value.
    // Bounded chroma compression protects highlights from RGB clipping.
    public static Vector3 BlendReference(Vector3 original,DyeRamp dye)
    {
        float y=ArmourRecolour.Luma(original);
        if(y<=.035f||y>=.975f)return original;
        Vector3 shadow=Decode(dye.Shadow),mid=Decode(dye.Midtone),light=Decode(dye.Highlight);
        float preserve=Smooth(.035f,.15f,y)*(1-Smooth(.80f,.975f,y));
        float midValue=ArmourRecolour.Luma(mid);
        // Soft light in the authored artwork's display colour space.
        // A fixed material midtone, not a changing curve coefficient,
        // retains monotonicity; reference highlights provide hue only.
        float soft=midValue<=.5f?y-(1-2*midValue)*y*(1-y):
            y+(2*midValue-1)*(SoftD(y)-y);
        float value=y+(soft-y)*(.42f*preserve);
        Vector3 hue=y<.5f?Vector3.Lerp(ZeroLuma(shadow),ZeroLuma(mid),Smooth(.08f,.5f,y)):
            Vector3.Lerp(ZeroLuma(mid),ZeroLuma(light),Smooth(.5f,.92f,y));
        // Retain the native chromatic detail rather than a solid dye
        // overlay. Reflections follow the reference artwork's own hues.
        Vector3 chroma=Vector3.Lerp(ZeroLuma(original),hue,.72f*preserve);
        float scale=1;
        Limit(chroma.X);Limit(chroma.Y);Limit(chroma.Z);
        void Limit(float c)
        {
            if(c>0)scale=Math.Min(scale,(1-value)/c);
            else if(c<0)scale=Math.Min(scale,value/-c);
        }
        return Vector3.Clamp(new Vector3(value)+chroma*scale,Vector3.Zero,Vector3.One);
    }
    private static float SoftD(float y)=>y<=.25f?((16*y-12)*y+4)*y:MathF.Sqrt(y);
    private static Vector3 Decode(uint rgb)=>new((rgb>>16&255)/255f,(rgb>>8&255)/255f,(rgb&255)/255f);
    private static Vector3 ZeroLuma(Vector3 v)=>v-new Vector3(ArmourRecolour.Luma(v));

    private static float Smooth(float a,float b,float x)
    { float t=Math.Clamp((x-a)/(b-a),0,1);return t*t*(3-2*t); }
    private static byte Byte(float x)=>(byte)Math.Clamp((int)MathF.Round(x*255),0,255);
}
