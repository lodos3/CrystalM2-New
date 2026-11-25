using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CrystalM2.ArmourCreate;

internal static class OriginalArmour
{
    private enum Piece { Chest, Back, Flank, Shoulder, Bracer, Belt, Skirt, Greave, Boot }
    private const int Tile = 72;
    private static readonly Lazy<Vector4[][]> Art = new(ReadArt);

    // Every piece is an original painted cutout. Mips are premultiplied so
    // shrinking a silver edge into a two-pixel cuff retains its colour.
    private static Vector4[][] ReadArt()
    {
        using var stream=typeof(OriginalArmour).Assembly.GetManifestResourceStream("CrystalM2.OriginalPlate")
            ?? throw new InvalidDataException("Missing original armour art.");
        using var image=new Bitmap(stream);
        if(image.Width!=Tile*3 || image.Height!=Tile*3) throw new InvalidDataException("Unexpected armour sheet dimensions.");
        var data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        var pixels=new byte[data.Stride*image.Height];
        Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
        image.UnlockBits(data);
        var art=new Vector4[9*4][];
        for(int part=0;part<9;part++)
        {
            var plane=art[part*4]=new Vector4[Tile*Tile];
            for(int y=0;y<Tile;y++)
            for(int x=0;x<Tile;x++)
            {
                int p=(part/3*Tile+y)*data.Stride+(part%3*Tile+x)*4;
                float alpha=pixels[p+3]/255f;
                plane[y*Tile+x]=new Vector4(pixels[p+2]*alpha,pixels[p+1]*alpha,pixels[p]*alpha,255*alpha);
            }
            for(int level=1;level<4;level++)
            {
                int size=Tile>>level;
                var smaller=new Vector4[size*size];
                for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                {
                    int i=2*y*(2*size)+2*x;
                    smaller[y*size+x]=(plane[i]+plane[i+1]+plane[i+2*size]+plane[i+2*size+1])*.25f;
                }
                art[part*4+level]=plane=smaller;
            }
        }
        return art;
    }

    internal static byte[] Paint(int id,int w,int h,byte[] naked,byte[] skin,Vector2[] a,int facing)
    {
        int count=w*h;
        var painted=new Vector4[count];
        var distance=new byte[count];
        Vector2 neckAxis=a[1]-a[0];
        float neckLength=Math.Clamp(neckAxis.Length(),4,12);
        neckAxis=neckAxis.LengthSquared()>1?Vector2.Normalize(neckAxis):Vector2.UnitY;
        for(int i=0;i<count;i++)
        {
            int p=i*4;
            distance[i]=naked[p+3]>48?(byte)0:(byte)32;
            if(naked[p+3]==0)continue;
            Vector2 headDelta=new Vector2(i%w+.5f,i/w+.5f)-a[0];
            if (headDelta.LengthSquared() < (neckLength+3)*(neckLength+3) &&
                Vector2.Dot(headDelta,neckAxis) < neckLength-.7f &&
                Math.Abs(headDelta.X*neckAxis.Y-headDelta.Y*neckAxis.X)<6.2f)
            {
                Buffer.BlockCopy(naked,p,skin,p,4);
                continue;
            }
            float shade=.19f+.79f*(naked[p]+naked[p+1]+naked[p+2])/(3*255f);
            float alpha=naked[p+3]/255f;
            painted[i]=new Vector4(63*shade,78*shade,91*shade,255)*alpha;
        }
        for(int i=0;i<count;i++)
        {
            int d=distance[i];
            if(i%w>0)d=Math.Min(d,distance[i-1]+1);
            if(i>=w)d=Math.Min(d,distance[i-w]+1);
            distance[i]=(byte)d;
        }
        for(int i=count-1;i>=0;i--)
        {
            int d=distance[i];
            if(i%w+1<w)d=Math.Min(d,distance[i+1]+1);
            if(i+w<count)d=Math.Min(d,distance[i+w]+1);
            distance[i]=(byte)d;
        }

        Vector2 spine=a[6]-a[1];
        float length=Math.Clamp(spine.Length(),13,30);
        Vector2 down=spine.LengthSquared()>1?Vector2.Normalize(spine):Vector2.UnitY;
        Vector2 across=new(down.Y,-down.X);
        float half=Math.Clamp(Math.Abs(Vector2.Dot(a[3]-a[2],across))*.5f,4,12);
        bool profile=facing is 2 or 6, rear=facing is 0 or 1 or 7;
        float weight=1+.045f*((id-2)%4);
        float hem=.56f+.09f*((id-2)%5);
        int near=facing is 1 or 2 or 3?1:0;
        for(int side=0;side<2;side++)
        {
            int s=side==0?1-near:near;
            Vector2 knee=a[7+s],foot=a[9+s];
            Vector2 leg=foot-knee;
            if(leg.LengthSquared()<9)continue;
            Vector2 line=Vector2.Normalize(leg);
            Draw(Piece.Boot,foot-line*4,foot+line*3.2f,8.2f*weight,s==1,2);
            Draw(Piece.Greave,knee-line*1.7f,foot-line*1.7f,8.5f*weight,s==1,2);
        }
        for(int side=0;side<2;side++)
        {
            int s=side==0?1-near:near;
            float sign=s==0?-1:1;
            Vector2 hip=a[6]+across*sign*(half*.57f);
            Vector2 bottom=Vector2.Lerp(a[7+s],a[9+s],hem*.54f)+across*(sign*(profile?2:5));
            Draw(Piece.Skirt,hip-down*5,bottom,half*(profile?1.13f:1.48f),s==1,10);
        }
        Arm(1-near);
        Draw(profile?Piece.Flank:rear?Piece.Back:Piece.Chest,a[1]-down*1.4f,a[6]+down*1.4f,
            half*(profile?1.97f:2.27f),facing==6,4);
        Draw(Piece.Belt,a[5]-down*1.6f,a[5]+down*3.1f,half*(profile?1.55f:1.96f),false,3);
        Arm(near);
        byte[] surface=new byte[count*4];
        for(int i=0;i<count;i++)
        {
            Vector4 v=painted[i];
            if(v.W<8 || skin[i*4+3]!=0)continue;
            float r=v.X/v.W,g=v.Y/v.W,b=v.Z/v.W;
            float l=.299f*r+.587f*g+.114f*b;
            int metal=r>b*1.44f && g>b*1.14f && r-b>.12f?3:
                b>r*1.28f && b-g>.035f?2:l<.15f?0:1;
            float reference=metal switch { 0=>.19f,1=>.78f,2=>.38f,_=>.64f };
            float value=Math.Clamp(1.12f*l/reference,0,1.25f);
            float nx=i%w==0||i%w==w-1?0:Relief(i-1)-Relief(i+1);
            float ny=i<w||i+w>=count?0:Relief(i-w)-Relief(i+w);
            int p=i*4;
            surface[p]=(byte)(metal*64+Math.Clamp((int)MathF.Round(value/1.25f*63),0,63));
            surface[p+1]=(byte)Math.Clamp((int)(128+ny*44),0,255);
            surface[p+2]=(byte)Math.Clamp((int)(128+nx*44),0,255);
            surface[p+3]=(byte)Math.Clamp((int)MathF.Round(v.W),0,255);
        }
        return surface;

        float Relief(int i)
        {
            var v=painted[i];
            return v.W>16?(v.X*.299f+v.Y*.587f+v.Z*.114f)/v.W:0;
        }

        void Arm(int s)
        {
            float sign=s==0?-1:1;
            Vector2 shoulder=a[2+s];
            Vector2 wrist=shoulder+down*(length*.98f)+across*sign*1.8f;
            float best=0,reach=length*1.55f;
            int x0=Math.Max(0,(int)(shoulder.X-reach)),x1=Math.Min(w-1,(int)(shoulder.X+reach));
            int y0=Math.Max(0,(int)(shoulder.Y-reach)),y1=Math.Min(h-1,(int)(shoulder.Y+reach));
            for(int y=y0;y<=y1;y++)
            for(int x=x0;x<=x1;x++)
            {
                int j=(y*w+x)*4;
                if(naked[j+3]<120 || skin[j+3]!=0 || naked[j+2]-naked[j]<10)continue;
                Vector2 p=new(x+.5f,y+.5f);
                float d=Vector2.DistanceSquared(shoulder,p);
                Vector2 relative=p-a[1];
                float lateral=Vector2.Dot(relative,across)*sign;
                float along=Vector2.Dot(relative,down);
                if(d<=best || d>reach*reach || Vector2.DistanceSquared(p,a[0])<78 ||
                    along>length+3 || (along>length*.48f && lateral<half*.93f) ||
                    Vector2.DistanceSquared(p,a[7])<36 || Vector2.DistanceSquared(p,a[8])<36 ||
                    Vector2.DistanceSquared(p,a[3-s])+4<d)continue;
                best=d; wrist=p;
            }
            Vector2 arm=wrist-shoulder;
            Vector2 axis=arm.LengthSquared()>1?Vector2.Normalize(arm):down;
            if(best>36)
            {
                // Preserve all painted skin tones, including dark outlines,
                // on the terminal hand rather than selecting skin by hue.
                Vector2 palm=wrist-axis*2;
                for(int y=Math.Max(0,(int)palm.Y-4);y<Math.Min(h,palm.Y+4);y++)
                for(int x=Math.Max(0,(int)palm.X-4);x<Math.Min(w,palm.X+4);x++)
                {
                    if(Vector2.DistanceSquared(new(x+.5f,y+.5f),palm)>10)continue;
                    int j=(y*w+x)*4;
                    Buffer.BlockCopy(naked,j,skin,j,4);
                }
            }
            Draw(Piece.Bracer,Vector2.Lerp(shoulder,wrist,.42f),wrist-axis*1.8f,7.5f*weight,s==1,2);
            Vector2 cap=shoulder+across*sign*1.4f;
            Draw(Piece.Shoulder,cap-axis*4.6f,cap+axis*(6.6f*weight),
                (profile?10.5f:13.5f)*weight,s==1,6);
        }

        void Draw(Piece piece,Vector2 top,Vector2 bottom,float width,bool mirror,float maxDistance)
        {
            Vector2 axis=bottom-top;
            float height=axis.Length();
            if(height<2)return;
            axis/=height;
            Vector2 horizontal=new(axis.Y,-axis.X);
            Vector2 centre=(top+bottom)*.5f;
            Vector2 extent=(Vector2.Abs(horizontal)*width+Vector2.Abs(axis)*height)*.5f;
            int level=Math.Clamp((int)MathF.Floor(MathF.Log2(Tile/Math.Max(width,height))),0,3);
            int n=Tile>>level;
            var tex=Art.Value[(int)piece*4+level];
            for(int y=Math.Max(0,(int)(centre.Y-extent.Y));y<Math.Min(h,centre.Y+extent.Y+1);y++)
            for(int x=Math.Max(0,(int)(centre.X-extent.X));x<Math.Min(w,centre.X+extent.X+1);x++)
            {
                int i=y*w+x;
                if(distance[i]>maxDistance || skin[i*4+3]!=0)continue;
                Vector2 offset=new Vector2(x+.5f,y+.5f)-top;
                float u=.5f+Vector2.Dot(offset,horizontal)*(mirror?-1:1)/width;
                float v=Vector2.Dot(offset,axis)/height;
                if(u<0||u>1||v<0||v>1)continue;
                float tx=u*(n-1),ty=v*(n-1);
                int ix=(int)tx,iy=(int)ty,j=iy*n+ix;
                int right=ix+1<n?1:0,below=iy+1<n?n:0;
                Vector4 sample=Vector4.Lerp(Vector4.Lerp(tex[j],tex[j+right],tx-ix),
                    Vector4.Lerp(tex[j+below],tex[j+below+right],tx-ix),ty-iy);
                if(piece==Piece.Skirt && sample.Z>sample.X*1.06f)
                {
                    sample.Y*=1.16f;
                    sample.Z=Math.Min(sample.W,sample.Z*1.52f);
                }
                painted[i]=sample+painted[i]*(1-sample.W/255);
            }
        }
    }
}
