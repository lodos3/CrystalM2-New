using System.Numerics;

namespace CrystalM2.Rendering.Core;

// Four original material frames in one immutable page per design.
// The same three RGB coverage planes and projection as wings 2-9.
internal static class ElementalWingArt
{
    private const int W = 136, H = 160, Pad = 12, Count = 4;
    private static readonly Lazy<byte[]>[] Pages = Enumerable.Range(0, 10)
        .Select(i => new Lazy<byte[]>(() => Build(i))).ToArray();

    internal static int FrameCount(int style) => (uint)style < 10 ? Count : 0;
    internal static WingPoseAtlas.Pose Pose(int style, int frame, int firstPage) =>
        (uint)style < 10 && (uint)frame < Count
            ? new(-Pad,-Pad,W,H,firstPage+style,1+frame*(W+2),1) : default;
    internal static byte[] ReadPage(int style) => Pages[style].Value;

    private static byte[] Build(int style)
    {
        byte[] page = new byte[WingPoseAtlas.PageSize * WingPoseAtlas.PageSize];
        for(int f=0;f<Count;f++)
        {
            var art=Paint(style,f*MathF.Tau/Count);
            int ox=1+f*(W+2);
            for(int y=0;y<H;y++)
            for(int x=0;x<W;x++)
            {
                Vector3 c=Vector3.Clamp(art[y*W+x],Vector3.Zero,new Vector3(255));
                page[(1+y)*WingPoseAtlas.PageSize+ox+x]=(byte)c.Z;
                page[(1+y+H+2)*WingPoseAtlas.PageSize+ox+x]=(byte)c.Y;
                page[(1+y+2*(H+2))*WingPoseAtlas.PageSize+ox+x]=(byte)c.X;
            }
        }
        return page;
    }

    private static Vector3[] Paint(int style,float phase)
    {
        var art=new Vector3[W*H];
        (int dark,int body,int bright)=style switch
        {
            0=>(0x48151C,0xB53C19,0xFFD07A), 1=>(0x133D64,0x278FB4,0xB5F9FF),
            2=>(0x344052,0x7698A7,0xDBF1FF), 3=>(0x365D73,0x83C7D4,0xE1FFFA),
            4=>(0x1F4126,0x659C39,0xD9EE8E), 5=>(0x21265E,0x536CB8,0xDCEAFF),
            6=>(0x263D1B,0x79A738,0xE3FF75), 7=>(0x864321,0xDF9A3A,0xFFF2BC),
            8=>(0x211232,0x7141A6,0xD5A6FF), _=>(0x293763,0x6477C6,0xD8E2FF)
        };
        Vector3 shadow=RGB(dark), mid=RGB(body), hot=RGB(bright);
        void Ink(int x,int y,Vector3 c,float a)
        {
            x+=Pad;y+=Pad;
            if((uint)x>=W||(uint)y>=H)return;
            int i=y*W+x;
            art[i]=Vector3.Lerp(art[i],c,Math.Clamp(a,0,1));
        }
        // Same overlapping vane technique as the original art: sampled
        // curvature, shaded cross section, bright shaft, ribs and tapered rim.
        void Vane(Vector2 a,Vector2 b,Vector2 c,float width,int ordinal,bool angular)
        {
            for(int k=0;k<=144;k++)
            {
                float t=k/144f,v=1-t;
                Vector2 pos=v*v*a+2*v*t*b+t*t*c;
                Vector2 tangent=(b-a)*v+(c-b)*t;
                if(tangent.LengthSquared()<.01f)continue;
                tangent=Vector2.Normalize(tangent);
                Vector2 n=new(-tangent.Y,tangent.X);
                float radius=width*(angular?Math.Min(t*5+.08f,v*1.45f):
                    MathF.Pow(Math.Max(0,MathF.Sin(MathF.PI*t)),.75f))+.25f;
                for(float q=-radius;q<=radius;q+=.45f)
                {
                    float u=q/radius,e=MathF.Abs(u);
                    float ribs=MathF.Pow(.5f+.5f*MathF.Cos(t*74-e*6+ordinal*1.8f),4);
                    float face=(u<0?.54f:.20f)+ribs*.19f;
                    if(style==2)face=u<0?.61f:.12f+.18f*MathF.Floor(t*5)/5;
                    if(style==4)face+=.12f*MathF.Sin(t*39+e*6);
                    Vector3 colour=Vector3.Lerp(shadow,mid,Math.Clamp(face+.12f*t,0,1));
                    float shaft=MathF.Exp(-MathF.Pow(q/(.65f+.8f*v),2));
                    float shine=.72f*shaft+(u<0?.32f:.08f)*MathF.Pow(e,7)+.25f*t*t;
                    if(style is 0 or 5 or 6)shine+=.27f*ribs*(.55f+.45f*MathF.Sin(phase+ordinal+t*5));
                    colour=Vector3.Lerp(colour,hot,Math.Clamp(shine,0,1));
                    Vector2 pixel=pos+n*q;
                    Ink((int)MathF.Round(pixel.X),(int)MathF.Round(pixel.Y),colour,Math.Clamp((1-e)*4.8f,0,1)*.92f);
                }
            }
        }
        void Line(Vector2 a,Vector2 b,Vector3 colour,float radius)
        {
            int count=Math.Max(1,(int)MathF.Ceiling(Vector2.Distance(a,b)*2));
            for(int i=0;i<=count;i++)
            {
                Vector2 p=Vector2.Lerp(a,b,i/(float)count);
                for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)
                    Ink((int)MathF.Round(p.X)+x,(int)MathF.Round(p.Y)+y,colour,
                        Math.Clamp(radius+.5f-MathF.Sqrt(x*x+y*y),0,1));
            }
        }
        if(style>=7)
        {
            // Tiled ornamental material: raised rails, braided facets and
            // animated corona glints, applied to independently curved orbits.
            for(int y=0;y<136;y++)for(int x=0;x<112;x++)
            {
                float t=x/111f,q=(y-67.5f)/67.5f;
                float e=MathF.Abs(q-.08f*MathF.Sin(t*MathF.Tau*3+phase));
                float fill=Math.Clamp((.78f-e)*5,0,1);
                float rail=MathF.Exp(-MathF.Pow((e-.46f)/.085f,2));
                float knot=MathF.Pow(.5f+.5f*MathF.Cos(t*MathF.Tau*4+q*9+phase),6);
                float core=MathF.Exp(-MathF.Pow(q/.17f,2));
                Vector3 colour=Vector3.Lerp(shadow,mid,.24f+.42f*rail+.17f*knot);
                Ink(x,y,Vector3.Lerp(colour,hot,Math.Clamp(.46f*rail+.60f*core*knot,0,1)),fill);
            }
        }
        else
        {
            // Magma and venom receive scalloped membranes beneath the quills.
            if(style is 0 or 6)
            {
                for(int y=0;y<136;y++)for(int x=0;x<112;x++)
                {
                    float px=(x-5)/100f,py=(y-43)/81f;
                    if(px<0)continue;
                    float angle=MathF.Atan2(py,px),r=MathF.Sqrt(px*px+py*py);
                    float sector=(angle+.78f)/2.03f;
                    if(sector<0||sector>1)continue;
                    float l=.85f+.11f*MathF.Cos(sector*MathF.Tau*5);
                    if(r>l)continue;
                    float vein=MathF.Pow(.5f+.5f*MathF.Cos(sector*MathF.Tau*5+r*.7f),14);
                    float edge=MathF.Exp(-MathF.Pow((r-l)/.04f,2));
                    float crack=MathF.Pow(.5f+.5f*MathF.Sin(r*65+sector*24+MathF.Sin(sector*19)),12);
                    Vector3 colour=Vector3.Lerp(shadow,mid,.38f+.27f*vein);
                    colour=Vector3.Lerp(colour,hot,Math.Clamp(.35f*edge+.44f*crack*(.65f+.35f*MathF.Sin(phase+r*7)),0,1));
                    Ink(x,y,colour,Math.Clamp((l-r)*35,0,1)*.82f);
                }
            }
            int count=style==2?8:style==4?12:style==3?9:10;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)(count-1),bend=MathF.Sin(t*MathF.PI);
                float tipX=27+78*t,tipY=126-113*t;
                float width=style switch {0=>7,1=>13,2=>12,3=>8,4=>10,5=>8,_=>7};
                if(style is 0 or 6){tipY-=(i%2)*7;tipX+=(i%2)*3;}
                if(style==2){tipX-=(i%2)*11;tipY+=(i%2)*7;}
                if(style==1){tipX+=5*bend;tipY-=13*bend;}
                if(style==3){tipY-=17*bend;tipX-=10*(1-t);}
                if(style==4){tipY+=8*MathF.Sin(t*7);tipX-=6*bend;}
                Vector2 a=new(3+32*t,42-20*t),b=new(10+54*t,78-69*t-12*bend),end=new(tipX,tipY);
                Vane(a,b,end,width,i,style is 0 or 2 or 5);
                if(style==5)
                {
                    Vector2 previous=a;
                    for(int k=1;k<=8;k++)
                    {
                        float u=k/8f,v=1-u;
                        Vector2 next=v*v*a+2*v*u*b+u*u*end+
                            new Vector2(3*MathF.Sin(k*5.7f+i+phase)*MathF.Sin(u*MathF.PI),0);
                        Line(previous,next,hot,1);
                        if(k==4||k==6)Line(next,next+new Vector2(5,3-i*.3f),mid,.8f);
                        previous=next;
                    }
                }
            }
            for(int i=0;i<7;i++)
            {
                float t=i/6f;
                Vane(new(2+21*t,43-17*t),new(10+37*t,20-18*t),new(24+68*t,73-62*t),
                    style==2?9:8,i+17,style is 0 or 2);
            }
            Vane(new(2,44),new(24,2),new(95,12),5,31,style is 0 or 2);
        }
        // Selective coloured bloom, once per immutable material frame.
        var emission=new Vector3[art.Length];
        for(int i=0;i<art.Length;i++)
        {
            Vector3 c=art[i];
            emission[i]=c*Math.Clamp((Math.Max(c.X,Math.Max(c.Y,c.Z))-75)/150,0,1);
        }
        var near=Blur(emission,3);var far=Blur(emission,7);
        for(int i=0;i<art.Length;i++)
            art[i]+=(1-.65f*Math.Clamp(art[i].Length()/290,0,1))*(near[i]*.46f+far[i]*.58f);
        return art;
    }
    private static Vector3[] Blur(Vector3[] data,int r)
    {
        var h=new Vector3[data.Length];var o=new Vector3[data.Length];
        for(int y=0;y<H;y++)
        {
            Vector3 sum=Vector3.Zero;
            for(int x=0;x<=r;x++)sum+=data[y*W+x];
            for(int x=0;x<W;x++)
            {
                h[y*W+x]=sum/(r*2+1);
                if(x-r>=0)sum-=data[y*W+x-r];
                if(x+r+1<W)sum+=data[y*W+x+r+1];
            }
        }
        for(int x=0;x<W;x++)
        {
            Vector3 sum=Vector3.Zero;
            for(int y=0;y<=r;y++)sum+=h[y*W+x];
            for(int y=0;y<H;y++)
            {
                o[y*W+x]=sum/(r*2+1);
                if(y-r>=0)sum-=h[(y-r)*W+x];
                if(y+r+1<H)sum+=h[(y+r+1)*W+x];
            }
        }
        return o;
    }
    private static Vector3 RGB(int v)=>new(v>>16&255,v>>8&255,v&255);
}
