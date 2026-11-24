using System.Numerics;

namespace CrystalM2.Rendering.Core;

public sealed record LiveArmourSplit(byte[] Skin, byte[] Armour);

public static class LiveArmourCoverage
{
    public static LiveArmourSplit Split(int width,int height,ReadOnlySpan<byte> naked,
        ReadOnlySpan<byte> fitted,Vector2[] anchors)
    {
        int size=checked(width*height*4);
        if(width<=0 || height<=0 || naked.Length!=size || fitted.Length!=size || anchors.Length!=11)
            throw new ArgumentException("Invalid aligned armour frame.");
        byte[] skin=new byte[size], armour=new byte[size];
        var head=anchors[0]; var neck=anchors[1]; var pelvis=anchors[6];
        Vector2 axis=pelvis-head;
        float length=Math.Max(1,axis.Length());
        axis=length>1?axis/length:Vector2.UnitY;
        Vector2 cross=new(axis.Y,-axis.X);
        Vector2 headAxis=neck-head;
        float neckLength=headAxis.Length();
        headAxis=neckLength>=2?headAxis/neckLength:axis;
        float headRadius=Math.Clamp(neckLength+2,5,11);
        float shoulderHalf=Math.Clamp(Math.Abs(Vector2.Dot(anchors[3]-anchors[2],cross))*.5f,4,12);
        for(int y=0;y<height;y++) for(int x=0;x<width;x++)
        {
            int p=(y*width+x)*4;
            if(naked[p+3]==0 && fitted[p+3]==0) continue;
            var point=new Vector2(x+.5f,y+.5f);
            var fromHead=point-head;
            float down=Vector2.Dot(fromHead,axis);
            float lateral=Math.Abs(Vector2.Dot(point-neck,cross));
            bool warm=naked[p+3]>0 && naked[p+2]>=45 &&
                naked[p+2]>=naked[p+1] && naked[p+2]-naked[p]>=8;
            // A face follows the neck even when the body is horizontal.
            bool face=warm && fromHead.LengthSquared()<=headRadius*headRadius &&
                Vector2.Dot(fromHead,headAxis)<=Math.Max(2,neckLength-.75f);
            // Exposed hands: require agreement with the reference skin, a
            // lateral extremity rather than the chest, and exclusion of feet.
            // Raised fists are allowed above the head; covered gloves are armour.
            int error=Math.Max(Math.Abs(naked[p]-fitted[p]),Math.Max(
                Math.Abs(naked[p+1]-fitted[p+1]),Math.Abs(naked[p+2]-fitted[p+2])));
            bool fittedSkin=fitted[p+3]>0 && fitted[p+2]>=55 &&
                fitted[p+2]>=fitted[p+1] && fitted[p+2]-fitted[p]>=12 && error<=48;
            bool extremity=lateral>shoulderHalf*.9f || down<0;
            bool leg=Vector2.DistanceSquared(point,anchors[9])<36 ||
                Vector2.DistanceSquared(point,anchors[10])<36 ||
                Vector2.DistanceSquared(point,anchors[7])<16 ||
                Vector2.DistanceSquared(point,anchors[8])<16;
            bool hand=warm && fittedSkin && extremity && !leg && down<=length+5;
            if(face || hand)
                naked.Slice(p,4).CopyTo(skin.AsSpan(p,4));
            else
                armour[p+3]=Math.Max(naked[p+3],fitted[p+3]);
        }
        return new(skin,armour);
    }
}
