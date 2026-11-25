using System.Numerics;

namespace CrystalM2.Rendering.Core;

public static class ArmourPatternTransfer
{
    // Indexed by original CArmour ID. Mix plate donors with plate silhouettes
    // and robe donors with robes, rather than randomly switching every frame.
    private static readonly int[,] Recipes =
    {
        {2,9,6,5}, {9,5,6,4}, {10,7,11,3}, {6,9,2,5},
        {1,6,9,2}, {9,6,4,5}, {11,8,10,7}, {7,11,10,8},
        {6,2,4,9}, {7,11,8,10}, {10,7,8,11}
    };

    public static int[] Recipe(int source)
    {
        if(source is <1 or >11) throw new ArgumentOutOfRangeException(nameof(source));
        return [Recipes[source-1,0],Recipes[source-1,1],Recipes[source-1,2],Recipes[source-1,3]];
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
        var neck=anchors[1];var waist=anchors[5];var hip=anchors[6];
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
            // Choose the closest available part. Transition only over a
            // narrow organic zone, preserving the high-frequency donor art.
            float best=float.PositiveInfinity;
            bool hasBase=Usable(fitted,naked,p);
            for(int j=0;j<4;j++)
            {
                weight[j]=0;
                if(!Usable(donors[j],naked,p))continue;
                colours[j]=Read(donors[j],p);
                weight[j]=1;
                best=Math.Min(best,score[j]);
            }
            Vector3 sum=Vector3.Zero;float total=0;
            if(float.IsFinite(best))
            {
                for(int j=0;j<4;j++)
                {
                    if(weight[j]==0)continue;
                    float t=Math.Clamp(1-(score[j]-best)/.36f,0,1);
                    float w=t*t*(3-2*t);
                    sum+=colours[j]*w;total+=w;
                }
            }
            Vector3 rgb;
            if(total>0)
            {
                rgb=sum/total;
                // Keep a little target artwork at silhouette edges; a donor
                // cannot change coverage or erase the original dark contour.
                if(hasBase)
                {
                    bool edge=x==0||x==width-1||y==0||y==height-1||
                        coverage[p-4+3]==0||coverage[p+4+3]==0||
                        coverage[p-width*4+3]==0||coverage[p+width*4+3]==0;
                    rgb=Vector3.Lerp(rgb,Read(fitted,p),edge?.55f:.10f);
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
