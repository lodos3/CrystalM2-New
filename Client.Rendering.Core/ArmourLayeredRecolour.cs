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
                float score=d*MathF.Sqrt(p.Count);
                if(score>best){best=score;separation=d;next=p.Position;}
            }
            // A source with fewer genuinely distinct colours can use fewer layers.
            if(separation<.0049f)break;
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
    // Deliberately contrasting four-colour palettes, not four nearly
    // identical tints. Slots correspond to learned source colours.
    private static readonly uint[,] Palettes=
    {
        {0x376BC8,0xD1A355,0xB9D9F5,0x65324D}, // sapphire, gold, ice, wine
        {0xB52C41,0xE3B74E,0x344B78,0xD5C6B2}, // crimson, gold, navy, ivory
        {0x353942,0xA7C8D9,0xB076D2,0x9E512F}, // obsidian, silver, violet, copper
        {0xE7DFC5,0xD29D32,0x355A8B,0x79494B}, // ivory, gold, royal blue, burgundy
        {0x268D61,0xB9813F,0xC2DBA0,0x564080}, // emerald, bronze, jade, violet
        {0x5DA9D6,0xCCDFF4,0x4352A5,0xC89658}, // frost, silver, cobalt, amber
        {0x8241BC,0x36A9B0,0xE4B668,0xD6C0EA}, // amethyst, teal, gold, lilac
        {0xB76A31,0x356F83,0xE9C77C,0x733C64}, // copper, patina, gold, plum
        {0x299CAB,0xE4A54A,0x845AD6,0xE1ECEA}  // teal, amber, violet, white
    };
    public static ArmourLayerEffect Effect(int profile)=>profile switch
    {
        5=>new(2,1,.18f), // frost shimmer
        6=>new(1,1,.22f), // arcane shimmer
        8=>new(0,2,.22f), // holographic scan
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

    public static ArmourColourLayers Apply(ArmourSourceParts source,ArmourColourModel model,int profile)
    {
        if((uint)profile>=Palettes.GetLength(0))throw new ArgumentOutOfRangeException(nameof(profile));
        return Apply(source,model,[Palettes[profile,0],Palettes[profile,1],Palettes[profile,2],Palettes[profile,3]]);
    }

    // A caller can independently assign any four RGB colours without
    // rebuilding the learned masks or altering the underlying source.
    public static ArmourColourLayers Apply(ArmourSourceParts source,ArmourColourModel model,ReadOnlySpan<uint> colours)
    {
        if(colours.Length!=4)throw new ArgumentException("Provide four RGB palette slots.",nameof(colours));
        if(source.Surface.Length%4!=0||source.Skin.Length!=source.Surface.Length)
            throw new ArgumentException("Unaligned source layers.");
        var layers=Enumerable.Range(0,4).Select(_=>new byte[source.Surface.Length]).ToArray();
        var combined=new byte[source.Surface.Length];
        for(int p=0;p<combined.Length;p+=4)
        {
            byte a=source.Surface[p+3];if(a==0)continue;
            Vector3 c=new(source.Surface[p+2]/255f,source.Surface[p+1]/255f,source.Surface[p]/255f);
            int slot=model.Classify(c);
            uint rgb=colours[slot];
            Vector3 target=new((rgb>>16&255)/255f,(rgb>>8&255)/255f,(rgb&255)/255f);
            Vector3 recoloured=Map(c,target);
            combined[p]=Byte(recoloured.Z);combined[p+1]=Byte(recoloured.Y);
            combined[p+2]=Byte(recoloured.X);combined[p+3]=a;
            combined.AsSpan(p,4).CopyTo(layers[slot].AsSpan(p,4));
        }
        return new(layers,source.Skin,combined);
    }

    private static Vector3 Map(Vector3 original,Vector3 target)
    {
        float y=ArmourRecolour.Luma(original);
        if(y<=.035f||y>=.975f)return original;
        float ty=ArmourRecolour.Luma(target);
        // Map midtone towards target brightness but maintain the local
        // source's order, gradients, darkest contours and highlight peaks.
        float tone=Math.Clamp(y+(ty-.5f)*y*(1-y)*1.15f,0,1);
        float strength=Smooth(.035f,.15f,y)*(1-Smooth(.79f,.975f,y));
        tone=y+(tone-y)*strength;
        Vector3 chroma=Vector3.Lerp(original-new Vector3(y),(target-new Vector3(ty))*1.30f,strength*.98f);
        float s=1;
        Limit(chroma.X);Limit(chroma.Y);Limit(chroma.Z);
        void Limit(float v)
        {
            if(v>0)s=Math.Min(s,(1-tone)/v);
            if(v<0)s=Math.Min(s,tone/-v);
        }
        return Vector3.Clamp(new Vector3(tone)+chroma*s,Vector3.Zero,Vector3.One);
    }
    private static float Smooth(float a,float b,float x)
    { float t=Math.Clamp((x-a)/(b-a),0,1);return t*t*(3-2*t); }
    private static byte Byte(float x)=>(byte)Math.Clamp((int)MathF.Round(x*255),0,255);
}
