using System.Numerics;

namespace CrystalM2.Rendering.Core;

public static class ArmourPatternTransfer
{
    public static int[] Recipe(int source)
    {
        if(source is <1 or >11)throw new ArgumentOutOfRangeException(nameof(source));
        return ArmourDesign.Resolve(source+1).Donors;
    }

    // BGRA inputs are aligned in the actor's original cell coordinates.
    // Output is original sampled colour/detail, with exactly target alpha.
    // Regions are soft anatomical neighbourhoods, not painted panel lines.
    public static byte[] Compose(int width,int height,Vector2[] anchors,byte[] naked,
        byte[] fitted,byte[] coverage,byte[][] donors)
    {
        if(width<=0||height<=0||width>320||height>320||anchors.Length!=11||donors.Length!=4)
            throw new ArgumentException("Invalid pattern geometry.");
        int bytes=checked(width*height*4);
        if(naked.Length!=bytes||fitted.Length!=bytes||coverage.Length!=bytes||
            donors.Any(d=>d.Length!=bytes))throw new ArgumentException("Unaligned pattern buffers.");
        byte[] output=new byte[bytes];
        var waist=anchors[5];var hip=anchors[6];
        // An alpha-aware two-scale decomposition separates broad tone from
        // native-pixel engravings. Invalid/skin pixels do not bleed into blur.
        var layers=donors.Select(d=>Decompose(width,height,d,naked)).ToArray();
        float half=Math.Clamp(Vector2.Distance(anchors[2],anchors[3])*.5f,4,13);
        var chest=Vector2.Lerp(anchors[4],waist,.18f);
        var waistCentre=Vector2.Lerp(waist,hip,.35f);
        Span<float> score=stackalloc float[4];
        Span<float> weight=stackalloc float[4];
        Span<Vector3> colours=stackalloc Vector3[4];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int p=(y*width+x)*4;
            if(coverage[p+3]==0)continue;
            var point=new Vector2(x+.5f,y+.5f);
            // Segment distances make knee/leg regions follow bent limbs.
            // Shoulders and upper sleeve regions are assessed independently
            // of the vertical screen coordinate, so lunges use the same recipe.
            score[0]=Vector2.Distance(point,chest)/(half*.85f+2);
            score[1]=Math.Min(Vector2.Distance(point,anchors[2]),
                Vector2.Distance(point,anchors[3]))/(half*.85f+2);
            score[2]=Vector2.Distance(point,waistCentre)/(half*.78f+2);
            score[3]=Math.Min(Math.Min(SegmentDistance(point,hip,anchors[7]),SegmentDistance(point,hip,anchors[8])),
                Math.Min(SegmentDistance(point,anchors[7],anchors[9]),SegmentDistance(point,anchors[8],anchors[10])))/
                (half*.78f+2)+.12f;
            // Region ownership is stable. At a seam, blend only broad
            // colour/shape information; choose exactly one native detail
            // field so offset engravings cannot produce ghosted double lines.
            int i=p/4,winner=-1;
            float best=float.PositiveInfinity;
            bool hasBase=Usable(fitted,naked,p);
            for(int j=0;j<4;j++)
            {
                weight[j]=0;
                if(!layers[j].Valid[i])continue;
                // Discourage cutting through a donor's own contour. A
                // small bias is enough to keep nearby complete ornaments.
                float edgePenalty=layers[j].Edge[i]?.10f:0;
                score[j]+=edgePenalty;
                weight[j]=1;
                colours[j]=layers[j].Broad[i];
                if(score[j]<best){best=score[j];winner=j;}
            }
            Vector3 sum=Vector3.Zero;float total=0;
            if(winner>=0)
            {
                for(int j=0;j<4;j++)
                {
                    if(weight[j]==0)continue;
                    float t=Math.Clamp(1-(score[j]-best)/.48f,0,1);
                    float w=t*t*(3-2*t);
                    sum+=colours[j]*w;total+=w;
                }
            }
            Vector3 rgb;
            if(winner>=0 && total>0)
            {
                var layer=layers[winner];
                // Coarse tone blends; middle-scale relief and fine pixels
                // remain owned by the same donor. Slight fine-detail gain
                // is bounded to avoid sharpening transparent edges.
                Vector3 relief=layer.Local[i]-layer.Broad[i];
                Vector3 detail=Read(donors[winner],p)-layer.Local[i];
                rgb=sum/total+relief+detail*(layer.Edge[i]?1f:1.06f);
                if(hasBase)
                {
                    bool edge=x==0||x==width-1||y==0||y==height-1||
                        coverage[p-4+3]==0||coverage[p+4+3]==0||
                        coverage[p-width*4+3]==0||coverage[p+width*4+3]==0;
                    // Preserve the target's authored contour; never blend
                    // a second outline through the middle of a plate.
                    if(edge)rgb=Vector3.Lerp(rgb,Read(fitted,p),.62f);
                }
            }
            else if(hasBase)rgb=Read(fitted,p);
            else
            {
                // Covered flesh or missing art: neutral undersuit with source
                // value variation, not a fabricated line or exposed skin.
                float v=naked[p+3]>0?Luma(Read(naked,p))*.42f+12:32;
                rgb=new Vector3(v*.92f,v*.98f,v*1.08f);
            }
            output[p]=(byte)Math.Clamp((int)MathF.Round(rgb.Z),0,255);
            output[p+1]=(byte)Math.Clamp((int)MathF.Round(rgb.Y),0,255);
            output[p+2]=(byte)Math.Clamp((int)MathF.Round(rgb.X),0,255);
            output[p+3]=coverage[p+3];
        }
        return output;
    }

    private sealed record Bands(bool[] Valid,bool[] Edge,Vector3[] Local,Vector3[] Broad);

    private static Bands Decompose(int width,int height,byte[] pixels,byte[] naked)
    {
        int n=width*height;
        var valid=new bool[n];var edge=new bool[n];
        var local=new Vector3[n];var broad=new Vector3[n];
        var original=new Vector3[n];
        for(int i=0;i<n;i++)
        {
            valid[i]=Usable(pixels,naked,i*4);
            if(valid[i])original[i]=Read(pixels,i*4);
        }
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;
            if(!valid[i])continue;
            edge[i]=x==0||x==width-1||y==0||y==height-1||
                !valid[i-1]||!valid[i+1]||!valid[i-width]||!valid[i+width];
            local[i]=Blur(x,y,1,original);
        }
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            if(valid[y*width+x])broad[y*width+x]=Blur(x,y,2,local);
        return new(valid,edge,local,broad);

        Vector3 Blur(int x,int y,int radius,Vector3[] input)
        {
            Vector3 sum=Vector3.Zero;float total=0;
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++)
            {
                int sx=x+dx,sy=y+dy;
                if((uint)sx>=width||(uint)sy>=height)continue;
                int j=sy*width+sx;
                if(!valid[j])continue;
                // Binomial-like spatial weight, additionally penalise
                // large luminance discontinuities across relief boundaries.
                float spatial=(radius+1-Math.Abs(dx))*(radius+1-Math.Abs(dy));
                float delta=Math.Abs(Luma(original[j])-Luma(original[y*width+x]));
                float w=spatial/(1+delta/64f);
                sum+=input[j]*w;total+=w;
            }
            return total>0?sum/total:input[y*width+x];
        }
    }

    private static Vector3 Read(byte[] source,int p)=>new(source[p+2],source[p+1],source[p]);
    private static float Luma(Vector3 c)=>Vector3.Dot(c,new(.299f,.587f,.114f));
    private static bool Usable(byte[] source,byte[] naked,int p)
    {
        if(source[p+3]<96)return false;
        // Suppress exposed skin from donor sleeves. Require close colour
        // agreement with the naked frame, not just a gold/brown hue.
        if(naked[p+3]>=96&&naked[p+2]>=45&&naked[p+2]>=naked[p+1]&&naked[p+2]-naked[p]>=8&&
            source[p+2]>=source[p+1]&&source[p+2]-source[p]>=8&&
            Math.Max(Math.Abs(source[p]-naked[p]),Math.Max(Math.Abs(source[p+1]-naked[p+1]),
                Math.Abs(source[p+2]-naked[p+2])))<=28)return false;
        return true;
    }

    private static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
    {
        Vector2 d=b-a;
        float t=Math.Clamp(Vector2.Dot(p-a,d)/Math.Max(.001f,d.LengthSquared()),0,1);
        return Vector2.Distance(p,a+d*t);
    }
}
