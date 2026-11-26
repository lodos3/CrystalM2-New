using System.Numerics;

namespace CrystalM2.Rendering.Core;

// Source-wide soft palette model plus local structure analysis. These
// are 2D appearance estimates, not recovered physical material labels.
public sealed record ArmourMaterialMap(int Width,int Height,Vector4[] Weights,byte[] Features)
{
    // BGRA: repeated/local texture, recess, ridge, material coverage.
    public static ArmourMaterialMap Analyse(int width,int height,ArmourSourceParts source,ArmourColourModel model)
    {
        if(width<=0||height<=0||width>320||height>320||
            source.Surface.Length!=checked(width*height*4)||source.Skin.Length!=source.Surface.Length)
            throw new ArgumentException("Invalid material analysis dimensions.");
        int n=width*height;
        byte[] art=source.Surface,features=new byte[n*4];
        var colour=new Vector3[n];var value=new float[n];
        var baseWeights=new Vector4[n];var weights=new Vector4[n];
        bool Valid(int i)=>art[i*4+3]>0&&source.Skin[i*4+3]==0;
        for(int i=0;i<n;i++)
        {
            if(!Valid(i))continue;
            int p=i*4;
            colour[i]=new(art[p+2]/255f,art[p+1]/255f,art[p]/255f);
            value[i]=ArmourRecolour.Luma(colour[i]);
            baseWeights[i]=model.Membership(colour[i]);
            weights[i]=baseWeights[i];
        }
        // Edge-aware local mask regularization. Reject neighbours across
        // strong colour/lighting discontinuities or transparent anatomy.
        for(int pass=0;pass<2;pass++)
        {
            var next=new Vector4[n];
            for(int i=0;i<n;i++)
            {
                if(!Valid(i))continue;
                Vector4 sum=baseWeights[i]*2;float total=2;
                int x=i%width,y=i/width;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dy==0)continue;
                    int sx=x+dx,sy=y+dy;
                    if((uint)sx>=width||(uint)sy>=height)continue;
                    int j=sy*width+sx;if(!Valid(j))continue;
                    float delta=Vector3.DistanceSquared(colour[i],colour[j]);
                    float k=MathF.Exp(-delta/.028f)*(dx!=0&&dy!=0?.55f:1);
                    sum+=weights[j]*k;total+=k;
                }
                next[i]=sum/total;
            }
            weights=next;
        }
        for(int i=0;i<n;i++)
        {
            if(!Valid(i))continue;
            int x=i%width,y=i/width,p=i*4;
            float total=0,mean=0,variance=0;
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
            {
                if(dx==0&&dy==0)continue;
                int sx=x+dx,sy=y+dy;
                if((uint)sx>=width||(uint)sy>=height)continue;
                int j=sy*width+sx;if(!Valid(j))continue;
                float k=1f/(1+dx*dx+dy*dy);
                mean+=value[j]*k;variance+=MathF.Abs(value[j]-value[i])*k;total+=k;
            }
            if(total<=0){features[p+3]=art[p+3];continue;}
            mean/=total;variance/=total;
            float detail=value[i]-mean;
            // Signed local contrast measures existing engraved/raised
            // structures rather than introducing screen-space noise.
            float ridge=Smooth(.018f,.20f,detail);
            float recess=Smooth(.025f,.22f,-detail);
            float repeat=0;
            // Small symmetric patch comparison detects repeated native
            // texture. Flat areas cannot produce a pattern response.
            for(int offset=2;offset<=4;offset++)
            {
                repeat=Math.Max(repeat,Repeat(offset,0));
                repeat=Math.Max(repeat,Repeat(0,offset));
            }
            features[p]=Byte(Smooth(.018f,.14f,variance)*(.45f+.55f*repeat));
            features[p+1]=Byte(recess);features[p+2]=Byte(ridge);features[p+3]=art[p+3];

            float Repeat(int dx,int dy)
            {
                int ax=x-dx,ay=y-dy,bx=x+dx,by=y+dy;
                if((uint)ax>=width||(uint)ay>=height||(uint)bx>=width||(uint)by>=height)return 0;
                int a=ay*width+ax,b=by*width+bx;
                if(!Valid(a)||!Valid(b))return 0;
                float error=MathF.Abs(value[a]-value[i])+MathF.Abs(value[b]-value[i]);
                return 1-Smooth(.035f,.18f,error);
            }
        }
        return new(width,height,weights,features);
    }
    internal static float Smooth(float a,float b,float x)
    {float t=Math.Clamp((x-a)/(b-a),0,1);return t*t*(3-2*t);}
    internal static byte Byte(float x)=>(byte)Math.Clamp((int)MathF.Round(x*255),0,255);
}

public static class ArmourMaterialReconstruction
{
    private readonly record struct Finish(Vector3 Ridge,Vector3 Recess,float Polish,float Patina,float Detail);
    private static readonly Finish[] Finishes=
    [
        new(new(.76f,.85f,.79f),new(.16f,.34f,.27f),.26f,.36f,.12f), // antique patina
        new(new(.92f,.68f,.35f),new(.42f,.10f,.07f),.40f,.23f,.16f), // ceremonial bronze
        new(new(.70f,.76f,.88f),new(.11f,.09f,.20f),.55f,.20f,.20f), // obsidian
        new(new(.98f,.90f,.69f),new(.33f,.15f,.09f),.39f,.20f,.13f), // gilded steel
        new(new(.66f,.80f,.88f),new(.19f,.28f,.34f),.32f,.30f,.16f), // weathered steel
        new(new(.73f,.90f,1),new(.16f,.28f,.52f),.61f,.19f,.18f), // crystal frost
        new(new(.90f,.71f,.96f),new(.31f,.15f,.41f),.32f,.24f,.23f), // arcane silk
        new(new(.97f,.71f,.40f),new(.25f,.38f,.20f),.48f,.42f,.19f), // oxidised copper
        new(new(.58f,.90f,.88f),new(.16f,.22f,.32f),.50f,.25f,.15f) // spectral metal
    ];
    public static ArmourColourLayers Render(ArmourSourceParts source,ArmourMaterialMap map,int profile)
    {
        if((uint)profile>=Finishes.Length||map.Weights.Length*4!=source.Surface.Length||
            map.Features.Length!=source.Surface.Length||source.Skin.Length!=source.Surface.Length)
            throw new ArgumentException("Invalid material reconstruction input.");
        var finish=Finishes[profile];
        var result=new byte[source.Surface.Length];
        var layers=Enumerable.Range(0,4).Select(_=>new byte[result.Length]).ToArray();
        for(int i=0;i<map.Weights.Length;i++)
        {
            int p=i*4;byte alpha=source.Surface[p+3];
            if(alpha==0||source.Skin[p+3]!=0)continue;
            Vector3 original=new(source.Surface[p+2]/255f,source.Surface[p+1]/255f,source.Surface[p]/255f);
            Vector4 w=map.Weights[i];
            float y=ArmourRecolour.Luma(original);
            Vector3 output=original;
            if(y>.035f&&y<.975f)
            {
                // Weighted reconstruction uses all four dyes, preserving
                // continuous material transitions across native pixels.
                output=ArmourLayeredRecolour.BlendReference(original,ArmourLayeredRecolour.ReferenceRamp(profile,0))*w.X+
                    ArmourLayeredRecolour.BlendReference(original,ArmourLayeredRecolour.ReferenceRamp(profile,1))*w.Y+
                    ArmourLayeredRecolour.BlendReference(original,ArmourLayeredRecolour.ReferenceRamp(profile,2))*w.Z+
                    ArmourLayeredRecolour.BlendReference(original,ArmourLayeredRecolour.ReferenceRamp(profile,3))*w.W;
                float ridge=map.Features[p+2]/255f,recess=map.Features[p+1]/255f,texture=map.Features[p]/255f;
                float gate=ArmourMaterialMap.Smooth(.035f,.17f,y)*(1-ArmourMaterialMap.Smooth(.80f,.975f,y));
                // Selectively polish raised detail, tint recesses using
                // multiply, retain fine texture with a small contrast pass.
                // Never add synthetic engravings or brighten the silhouette.
                Vector3 polish=Vector3.One-(Vector3.One-output)*(Vector3.One-finish.Ridge*(ridge*finish.Polish*.38f*gate));
                Vector3 patina=Vector3.Lerp(polish,polish*finish.Recess,recess*finish.Patina*.44f*gate);
                float detail=(y-.5f)*texture*finish.Detail*.20f*gate;
                output=Vector3.Clamp(patina+new Vector3(detail),Vector3.Zero,Vector3.One);
            }
            result[p]=ArmourMaterialMap.Byte(output.Z);result[p+1]=ArmourMaterialMap.Byte(output.Y);
            result[p+2]=ArmourMaterialMap.Byte(output.X);result[p+3]=alpha;
            // Legacy export layers are disjoint composite ownership, not
            // the soft weights themselves. Runtime uses combined artwork.
            int owner=0;float best=w.X;
            if(w.Y>best){best=w.Y;owner=1;}
            if(w.Z>best){best=w.Z;owner=2;}
            if(w.W>best)owner=3;
            result.AsSpan(p,4).CopyTo(layers[owner].AsSpan(p,4));
        }
        return new(layers,source.Skin,result);
    }

    public static Vector3 ReflectionColour(int profile)=>Finishes[profile].Ridge;
}
