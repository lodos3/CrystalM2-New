using CrystalM2.Rendering.Core;

namespace Client.Rendering;

public sealed class UiSkinRenderer
{
    private readonly RendererDevice _device;
    private readonly Texture _atlas;

    internal UiSkinRenderer(RendererDevice device)
    {
        _device = device;
        _atlas = new Texture(device, DarkFantasyAtlas.Width, DarkFantasyAtlas.Height);
        try
        {
            DataRectangle upload = _atlas.BeginUpload();
            upload.Data.Write(DarkFantasyAtlas.Generate());
            _atlas.EndUpload();
        }
        catch { _atlas.Dispose(); throw; }
    }

    public void Draw(UiSurface surface, Rectangle bounds, UiVisualState state = UiVisualState.Normal, float opacity = 1)
    {
        if (surface == UiSurface.None || bounds.Width <= 0 || bounds.Height <= 0) return;
        UiAtlasPart part = DarkFantasyAtlas.Part(surface, state);
        Span<NineSlicePatch> patches = stackalloc NineSlicePatch[9];
        int count = NineSliceLayout.Write(patches, part.Source, bounds, part.Border);
        var tint = new Vector4(1, 1, 1, Math.Clamp(opacity, 0, 1));
        for (int i = 0; i < count; i++)
            _device.Sprite.Draw(_atlas, patches[i].Source, patches[i].Destination, tint);
    }

    public void Glyph(UiGlyph glyph, Rectangle bounds, Color colour)
    {
        if (glyph == UiGlyph.None) return;
        _device.Sprite.Draw(_atlas, DarkFantasyAtlas.Glyph(glyph), bounds, RendererDevice.ToVector(colour));
    }

    public void Fill(Rectangle bounds, Color colour) =>
        _device.Sprite.Draw(_atlas, DarkFantasyAtlas.WhitePixel, bounds, RendererDevice.ToVector(colour));

    public void Orb(Point location, double fraction, bool mana)
    {
        var bounds = new Rectangle(location, new Size(DarkFantasyAtlas.OrbSize, DarkFantasyAtlas.OrbSize));
        _device.Sprite.Draw(_atlas, DarkFantasyAtlas.OrbBasin, bounds, Vector4.One);
        int top = DarkFantasyAtlas.OrbFillTop(fraction);
        if (top < DarkFantasyAtlas.OrbFillTop(0))
        {
            Rectangle source = mana ? DarkFantasyAtlas.ManaLiquid : DarkFantasyAtlas.LifeLiquid;
            source.Y += top;
            source.Height -= top;
            _device.Sprite.Draw(_atlas, source, new Rectangle(bounds.X, bounds.Y + top, source.Width, source.Height), Vector4.One);
            if (top > DarkFantasyAtlas.OrbFillTop(1))
                _device.Sprite.Draw(_atlas, new Rectangle(source.X, source.Y, source.Width, 1),
                    new Rectangle(bounds.X, bounds.Y + top, source.Width, 1), new Vector4(1.6f, 1.6f, 1.6f, .8f));
        }
        _device.Sprite.Draw(_atlas, DarkFantasyAtlas.OrbRim, bounds, Vector4.One);
    }

    public void Progress(Rectangle bounds, double fraction, Color colour, bool vertical = false)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        Draw(UiSurface.ScrollTrack, bounds);
        int inset = Math.Clamp((Math.Min(bounds.Width, bounds.Height) - 1) / 4, 0, 3);
        bounds.Inflate(-inset, -inset);
        fraction = double.IsFinite(fraction) ? Math.Clamp(fraction, 0, 1) : 0;
        if (vertical)
        {
            int height = (int)Math.Round(bounds.Height * fraction);
            bounds = new Rectangle(bounds.X, bounds.Bottom - height, bounds.Width, height);
        }
        else bounds.Width = (int)Math.Round(bounds.Width * fraction);
        Fill(bounds, colour);
    }
}
