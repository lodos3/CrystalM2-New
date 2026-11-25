using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using CrystalM2.Rendering.Core;
using Client.Rendering;
using CrystalM2.ArmourCreate;
using Vortice.Direct3D;

internal static class OriginalArmourChecks
{
    internal static void Render(string data, string art, string output)
    {
        using var atlas = new LiveArmourAtlas(data,art);
        using var naked = new LegacyMirLibReader(File.OpenRead(Path.Combine(data,"CArmour","00.Lib")));
        using var window = new Form();
        using var device = new RendererDevice(window.Handle,112,100,DriverType.Warp);
        using var target = new Texture(device,112,100,true);
        using var surface = target.GetSurface();
        device.SetRenderTarget(surface);
        using var sheet = new Bitmap(1104,458);
        using var g = Graphics.FromImage(sheet);
        g.Clear(Color.FromArgb(25,28,33));
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        int[] featured = [16,0,152,808+18,808+42,808+440];
        string[] labels = ["Front","Back","Swing","Front","Walk","Mounted"];
        int frames = 0;
        for(int id=2;id<=12;id++)
        foreach(int index in featured.Concat(Enumerable.Range(0,8).SelectMany(d=>new[]{136+d*6+3,808+384+d*4+2})))
        {
            var f = atlas.Create(id,index);
            var body = naked.ReadFrame(index);
            BakedShadowRepair.StripFromMask(body.Bgra,body.Width,body.Height,body.Width*4);
            int extensions=0;
            for(int y=0;y<f.Height;y++)
            for(int x=0;x<f.Width;x++)
            {
                int p=(y*f.Width+x)*4;
                if(f.Surface[p+3]==0)continue;
                if(f.Skin[p+3]!=0) throw new InvalidDataException($"Armour covered skin on g{id:00} pose {index}.");
                int bx=x+f.X-body.OffsetX,by=y+f.Y-body.OffsetY;
                if ((uint)bx>=body.Width || (uint)by>=body.Height || body.Bgra[(by*body.Width+bx)*4+3]==0) extensions++;
            }
            if(extensions<3) throw new InvalidDataException($"Missing original silhouette on g{id:00} pose {index}.");
            frames++;
            int column=Array.IndexOf(featured,index);
            if(id!=2||column<0)continue;
            for(int row=0;row<2;row++)
            {
                device.Clear(Color.FromArgb(25,28,33));
                using var material = Upload(f.Surface);
                using var skin = Upload(f.Skin);
                device.PixelShader=ShaderEffect.LiveArmour;
                device.SetPixelShaderConstant(0,[new(.125f,.169f,.239f,1),new(.722f,.784f,.839f,1),
                    new(.157f,.345f,.608f,1),new(.749f,.584f,.318f,1),new(-.66f,-.76f,.80f,0)]);
                Vector3 origin=new(31+f.X,70+f.Y,0);
                device.Sprite.Draw(material,null,origin,Vector4.One);
                device.PixelShader=ShaderEffect.Artwork;
                device.Sprite.Draw(skin,null,origin,Vector4.One);
                using var shot=device.Capture(surface);
                int x=column*184;
                if(row==0)
                {
                    g.DrawString(labels[column],SystemFonts.MessageBoxFont!,Brushes.White,x+12,8);
                    g.DrawImageUnscaled(shot,x+36,32);
                }
                else
                {
                    g.DrawImage(shot,new Rectangle(x,152,184,291),new Rectangle(26,0,62,98),GraphicsUnit.Pixel);
                }
            }

            Texture Upload(byte[] pixels)
            {
                var texture=new Texture(device,f.Width,f.Height);
                texture.BeginUpload().Data.Write(pixels);
                texture.EndUpload();
                return texture;
            }
        }
        sheet.Save(output,ImageFormat.Png);
        Console.WriteLine($"Verified {frames} pose/design combinations and rendered g02 at native size and enlarged through the live armour shader.");
    }
}
