using System.Numerics;

namespace CrystalM2.Rendering.Core;

public sealed record LiveArmourSplit(byte[] Skin, byte[] Armour);

public static class LiveArmourCoverage
{
    public static LiveArmourSplit Split(int width,int height,ReadOnlySpan<byte> naked,
        ReadOnlySpan<byte> fitted,Vector2[] anchors,ReadOnlySpan<byte> handReference=default)
    {
        if(width<=0 || height<=0 || width>320 || height>320 || anchors.Length!=11)
            throw new ArgumentException("Invalid aligned armour frame.");
        int count=checked(width*height),size=count*4;
        if(naked.Length!=size || fitted.Length!=size ||
            (!handReference.IsEmpty && handReference.Length!=size))
            throw new ArgumentException("Invalid aligned armour buffers.");
        if(handReference.IsEmpty) handReference=fitted;
        byte[] skin=new byte[size],armour=new byte[size];
        bool[] warm=new bool[count],face=new bool[count],candidate=new bool[count];
        float[] score=new float[count];
        var head=anchors[0];var neck=anchors[1];var pelvis=anchors[6];
        Vector2 axis=pelvis-head;
        float length=Math.Max(1,axis.Length());
        axis=length>1?axis/length:Vector2.UnitY;
        Vector2 across=new(axis.Y,-axis.X);
        Vector2 headAxis=neck-head;
        float neckLength=headAxis.Length();
        headAxis=neckLength>=2?headAxis/neckLength:axis;
        float headRadius=Math.Clamp(neckLength+2,5,11);
        float shoulderHalf=Math.Clamp(Math.Abs(Vector2.Dot(anchors[3]-anchors[2],across))*.5f,4,12);

        for(int i=0;i<count;i++)
        {
            int p=i*4;
            Vector2 pos=Position(i),fromHead=pos-head;
            warm[i]=naked[p+3]>0 && naked[p+2]>=45 &&
                naked[p+2]>=naked[p+1] && naked[p+2]-naked[p]>=8;
            face[i]=warm[i] && fromHead.LengthSquared()<=headRadius*headRadius &&
                Vector2.Dot(fromHead,headAxis)<=Math.Max(2,neckLength-.75f);
            if(face[i]) continue;
            if(!warm[i]) continue;

            float down=Vector2.Dot(fromHead,axis);
            float lateral=Math.Abs(Vector2.Dot(pos-neck,across));
            float shoulderDistance=Math.Min(Vector2.Distance(pos,anchors[2]),Vector2.Distance(pos,anchors[3]));
            bool limb=lateral>shoulderHalf*.8f || down<0;
            // Never let a bent leg, whose projected position may be beside the
            // torso, become an exposed hand. Very uncertain pixels remain
            // covered by a generated gauntlet, rather than leaving skin holes.
            bool nearLeg=Vector2.DistanceSquared(pos,anchors[9])<36 ||
                Vector2.DistanceSquared(pos,anchors[10])<36 ||
                Vector2.DistanceSquared(pos,anchors[7])<16 ||
                Vector2.DistanceSquared(pos,anchors[8])<16;
            int evidence=0;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                int x=i%width+dx,y=i/width+dy;
                if((uint)x>=width||(uint)y>=height)continue;
                int q=(y*width+x)*4;
                if(SimilarSkin(naked,handReference,p,q))evidence++;
                if(SimilarSkin(naked,fitted,p,q))evidence++;
            }
            candidate[i]=limb&&!nearLeg&&evidence>0&&shoulderDistance>=5&&
                pos.Y<=pelvis.Y+7&&down<=length+5;
            // Prefer the farthest exposed extremity, not the shoulder or elbow.
            score[i]=shoulderDistance+Math.Max(0,-down)*.25f+Math.Min(evidence,6)*.16f;
        }

        // A maximum of two bounded distal patches. Region growth follows the
        // original naked sprite pixels and never copies whole sleeves.
        bool[] hand=new bool[count];
        for(int limb=0;limb<2;limb++)
        {
            int tip=-1;
            for(int i=0;i<count;i++)
                if(candidate[i]&&(tip<0||score[i]>score[tip]))tip=i;
            if(tip<0)break;
            Vector2 centre=Position(tip);
            var queue=new Queue<int>();
            queue.Enqueue(tip);hand[tip]=true;
            while(queue.TryDequeue(out int i))
            {
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int x=i%width+dx,y=i/width+dy;
                    if((uint)x>=width||(uint)y>=height)continue;
                    int q=y*width+x;
                    if(hand[q]||face[q]||!warm[q]||
                        Vector2.DistanceSquared(Position(q),centre)>12.25f)continue;
                    hand[q]=true;queue.Enqueue(q);
                }
            }
            // Two tips on the same small hand must not expand it again.
            for(int i=0;i<count;i++)
                if(Vector2.DistanceSquared(Position(i),centre)<144)candidate[i]=false;
        }

        for(int i=0;i<count;i++)
        {
            int p=i*4;
            if(face[i]||hand[i])naked.Slice(p,4).CopyTo(skin.AsSpan(p,4));
            else armour[p+3]=Math.Max(naked[p+3],fitted[p+3]);
        }
        return new(skin,armour);

        Vector2 Position(int i)=>new(i%width+.5f,i/width+.5f);
    }

    private static bool SimilarSkin(ReadOnlySpan<byte> naked,ReadOnlySpan<byte> fitted,int p,int q)
    {
        if(fitted[q+3]==0||fitted[q+2]<55||fitted[q+2]<fitted[q+1]||fitted[q+2]-fitted[q]<12)
            return false;
        int error=Math.Max(Math.Abs(naked[p]-fitted[q]),Math.Max(
            Math.Abs(naked[p+1]-fitted[q+1]),Math.Abs(naked[p+2]-fitted[q+2])));
        return error<=48;
    }
}
