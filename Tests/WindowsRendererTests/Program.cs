using Client.Rendering;
using System.Numerics;
using Vortice.Direct3D;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Real Microsoft D3D11 software device, using production source and
        // shaders. These small pixel inputs are test data, not game artwork.
        using var window = new Form { ClientSize = new Size(64, 64) };
        using var device = new RendererDevice(window.Handle, 64, 64, DriverType.Warp);
        using Surface backBuffer = device.GetBackBuffer();
        using var red = Upload(device, Color.Red);
        using var blue = Upload(device, Color.Blue);
        using var translucent = Upload(device, Color.FromArgb(128, 180, 80, 20));
        using var intermediate = new Texture(device, 8, 8, true);
        using var surface = intermediate.GetSurface();

        device.SetRenderTarget(surface);
        device.Clear(Color.Transparent);
        device.Sprite.Begin();
        device.Sprite.Draw(translucent, null, new Vector3(1, 1, 0), Vector4.One);
        device.Sprite.End();
        device.SetRenderTarget(backBuffer);
        device.Clear(Color.Black);
        device.Sprite.Draw(intermediate, null, Vector3.Zero, Vector4.One);
        device.Sprite.Draw(translucent, null, new Vector3(12, 1, 0), Vector4.One);
        device.Sprite.End();
        using (var image = device.Capture(backBuffer))
        {
            Near(image.GetPixel(1, 1), image.GetPixel(12, 1), "Cached alpha must match direct alpha.");
            Near(Color.FromArgb(255, 90, 40, 10), image.GetPixel(1, 1), "Straight BGRA must blend once.");
            Near(Color.Black, image.GetPixel(0, 0), "Source offsets must leave surrounding pixels unchanged.");
        }

        device.BeginFrame();
        device.Clear(Color.Black);
        device.Sprite.Begin();
        device.Sprite.Draw(red, null, Vector3.Zero, Vector4.One);
        device.Sprite.Draw(red, null, new Vector3(4, 0, 0), Vector4.One);
        device.Sprite.Draw(blue, null, Vector3.Zero, Vector4.One);
        device.Sprite.Draw(red, null, new Vector3(1, 0, 0), Vector4.One);
        device.Sprite.End();
        Check(device.DrawCalls == 3, "Only adjacent texture runs may combine.");
        using (var image = device.Capture(backBuffer))
        {
            Near(Color.Blue, image.GetPixel(0, 0), "Later blue layer must cover the first red layer.");
            Near(Color.Red, image.GetPixel(1, 0), "Final red layer must retain painter's order.");
        }

        device.Present(false);
        device.BeginFrame();
        device.Clear(Color.Black);
        device.Sprite.Draw(blue, null, new Vector3(8, 8, 0), Vector4.One);
        using (var image = device.Capture(backBuffer))
            Near(Color.Blue, image.GetPixel(8, 8), "Backbuffer must rebind after flip-model Present.");

        long bytes = device.TextureBytes;
        int textures = device.TextureCount;
        for (int i = 0; i < 32; i++)
        {
            using var temporary = Upload(device, Color.White);
            device.Sprite.Draw(temporary, null, Vector3.Zero, Vector4.One);
        }
        Check(device.TextureBytes == bytes && device.TextureCount == textures,
            "Disposed uploads must leave no registered texture memory.");
        device.Resize(80, 60);
        using Surface resized = device.GetBackBuffer();
        device.Clear(Color.Black);
        device.Sprite.Draw(red, null, Vector3.Zero, Vector4.One);
        using (var image = device.Capture(resized))
        {
            Check(image.Size == new Size(80, 60), "Readback must use resized dimensions and row pitch.");
            Near(Color.Red, image.GetPixel(0, 0), "Existing textures must survive swapchain resize.");
        }
        Console.WriteLine("D3D11 shader, alpha, layer order, batching, flip, upload disposal and resize checks passed (WARP).");
    }

    private static Texture Upload(RendererDevice device, Color colour)
    {
        var texture = new Texture(device, 2, 2);
        try
        {
            var upload = texture.BeginUpload();
            for (int i = 0; i < 4; i++)
                upload.Data.Write(new[] { colour.B, colour.G, colour.R, colour.A });
            texture.EndUpload();
            return texture;
        }
        catch { texture.Dispose(); throw; }
    }
    private static void Near(Color expected, Color actual, string message) => Check(
        Math.Abs(expected.R - actual.R) <= 2 && Math.Abs(expected.G - actual.G) <= 2 &&
        Math.Abs(expected.B - actual.B) <= 2 && Math.Abs(expected.A - actual.A) <= 2,
        $"{message} Expected {expected}, got {actual}.");
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
