using System.Numerics;

namespace CrystalM2.Rendering.Core;

// Keep the source identity of every existing g02–g99 appearance. Each
// group of eleven advances to a new palette; no donor libraries are mixed.
public readonly record struct ArmourRecolourDesign(int Source,int Profile)
{
    public static bool Supports(int id)=>id is >=2 and <=99;
    public static ArmourRecolourDesign Resolve(int id)
    {
        if(!Supports(id))throw new ArgumentOutOfRangeException(nameof(id));
        return new((id-2)%11+1,(id-2)/11);
    }
}

public sealed record ArmourRecolourResult(byte[] Surface,byte[] Skin);

public static class ArmourRecolour
{
    private readonly record struct Palette(uint Main,uint Panel,uint Trim,uint Cloth,float Tone);
    // Curated design choices, not extracted material labels.
    private static readonly Palette[] Profiles=
    [
        new(0x527FAF,0x253D69,0xE2C38A,0x303B50,1.02f), // sapphire steel
        new(0x9A3946,0x542432,0xE9BB77,0x37242A,.96f), // crimson gold
        new(0x363C50,0x252B36,0xA9BACD,0x252831,.67f), // obsidian silver
        new(0xD5E1EB,0x8097B2,0xE5C875,0x67788F,1.27f), // ivory paladin
        new(0x3D9176,0x234E42,0xC7B477,0x293E36,1.00f), // emerald bronze
        new(0xA0CFE5,0x497DA7,0xD8EAF5,0x364C67,1.15f), // frost silver
        new(0x8067AA,0x49316E,0xC2A4E4,0x372C4C,.98f), // amethyst
        new(0xBD8551,0x74452E,0xECD49A,0x4C342C,1.03f), // burnished copper
        new(0x489AA7,0x24536E,0xE5AA62,0x2C414B,.96f) // teal amber
    ];
    private static readonly Vector3 Weights=new(.299f,.587f,.114f);
    public static float Luma(Vector3 rgb)=>Vector3.Dot(rgb,Weights);

    // All region weights are derived from the original colour once. Later
    // passes never classify an already recoloured pixel. Smooth transitions
    // avoid harsh bands on the original artist's gradient and antialiasing.
    public static Vector3 Colour(Vector3 source,int profile)
    {
        if((uint)profile>=Profiles.Length)throw new ArgumentOutOfRangeException(nameof(profile));
        var p=Profiles[profile];
        float y=Luma(source);
        if(y<=.035f||y>=.975f)return source;
        float max=Math.Max(source.X,Math.Max(source.Y,source.Z));
        float min=Math.Min(source.X,Math.Min(source.Y,source.Z));
        float chroma=max-min;
        float warm=Smooth(.04f,.30f,source.X-source.Z)*
            Smooth(.015f,.18f,source.Y-source.Z)*
            Smooth(.14f,.40f,y);
        float cloth=(1-warm)*(1-Smooth(.13f,.40f,y));
        float panel=(1-warm)*(1-cloth)*Smooth(.045f,.25f,chroma);
        float main=Math.Max(0,1-warm-cloth-panel);
        float total=main+panel+warm+cloth;
        // Stage 1: main and secondary material; stage 2: warm trim;
        // stage 3: dark cloth. Each contributes only its own source mask.
        Vector3 tint=(Rgb(p.Main)*main+Rgb(p.Panel)*panel+
            Rgb(p.Trim)*warm+Rgb(p.Cloth)*cloth)/Math.Max(.0001f,total);
        // Stage 4: monotone, bounded tone curve preserves the painted
        // highlights and shadows. No neighbourhood blur or geometry changes.
        float preserve=Smooth(.035f,.16f,y)*(1-Smooth(.76f,.975f,y));
        // Feather the tone curve to identity at the protected endpoints;
        // abrupt cutoffs would invert some near-black/near-white shades.
        float value=Math.Clamp(y+(p.Tone-1)*y*(1-y)*1.35f*preserve,0,1);
        // Retain some native chroma; move toward the palette without clipping
        // channels. Work in a constant-luminance plane and compress chroma
        // only when necessary to stay inside the RGB gamut.
        Vector3 target=tint-new Vector3(Luma(tint));
        Vector3 original=source-new Vector3(y);
        float strength=(.78f+.14f*warm)*preserve;
        Vector3 chromaOffset=Vector3.Lerp(original,target*1.18f,strength);
        float scale=1;
        Limit(chromaOffset.X);Limit(chromaOffset.Y);Limit(chromaOffset.Z);
        return Vector3.Clamp(new Vector3(value)+chromaOffset*scale,Vector3.Zero,Vector3.One);

        void Limit(float c)
        {
            if(c>0)scale=Math.Min(scale,(1-value)/c);
            else if(c<0)scale=Math.Min(scale,value/-c);
        }
    }

    // Keep original dimensions, alpha and pixel placement. The isolated
    // skin layer is a protection mask, never a source of new silhouettes.
    // Copy the original dressed pixel (including its authored shading).
    public static ArmourRecolourResult Apply(ReadOnlySpan<byte> fitted,
        ReadOnlySpan<byte> naked,ReadOnlySpan<byte> isolatedSkin,int profile)
    {
        if(fitted.Length%4!=0||naked.Length!=fitted.Length||isolatedSkin.Length!=fitted.Length)
            throw new ArgumentException("Unaligned armour frame.");
        if((uint)profile>=Profiles.Length)throw new ArgumentOutOfRangeException(nameof(profile));
        var surface=new byte[fitted.Length];
        var skin=new byte[fitted.Length];
        for(int p=0;p<fitted.Length;p+=4)
        {
            byte alpha=fitted[p+3];
            if(alpha==0)continue;
            int difference=Math.Max(Math.Abs(fitted[p]-naked[p]),
                Math.Max(Math.Abs(fitted[p+1]-naked[p+1]),Math.Abs(fitted[p+2]-naked[p+2])));
            bool warm=naked[p+2]>=45&&naked[p+2]>=naked[p+1]&&naked[p+2]-naked[p]>=8;
            // Exact matching exposed skin is also protected when the
            // heuristic isolation missed it. Never uncover a glove/helmet.
            bool restore=naked[p+3]>0&&warm&&
                ((isolatedSkin[p+3]>0&&difference<=32)||difference<=8);
            if(restore)
            {
                fitted.Slice(p,4).CopyTo(skin.AsSpan(p,4));
                continue;
            }
            Vector3 rgb=new(fitted[p+2]/255f,fitted[p+1]/255f,fitted[p]/255f);
            Vector3 c=Colour(rgb,profile);
            surface[p]=Byte(c.Z);surface[p+1]=Byte(c.Y);surface[p+2]=Byte(c.X);
            surface[p+3]=alpha;
        }
        return new(surface,skin);
    }

    private static Vector3 Rgb(uint c)=>new((c>>16&255)/255f,(c>>8&255)/255f,(c&255)/255f);
    private static byte Byte(float value)=>(byte)Math.Clamp((int)MathF.Round(value*255),0,255);
    private static float Smooth(float a,float b,float v)
    {
        float t=Math.Clamp((v-a)/(b-a),0,1);return t*t*(3-2*t);
    }
}
