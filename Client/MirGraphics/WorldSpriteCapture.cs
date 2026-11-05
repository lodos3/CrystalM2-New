namespace Client.MirGraphics;

internal static class WorldSpriteCapture
{
    internal enum Kind { CutoutTile, Scenery, Actor }

    internal readonly record struct Draw(Texture Texture, Rectangle? Source, Vector3 Position, Vector4 Tint,
        Matrix Transform, SpriteBlendMode Blend);

    private static List<Draw> _target;
    private static Kind _kind;

    internal static void Record(Texture texture, Rectangle? source, Vector3? position, Color tint)
    {
        if (_target == null || texture == null || texture.Disposed || tint.A == 0) return;
        if (_kind == Kind.CutoutTile && !texture.HasCutout || _kind == Kind.Actor && !texture.IsWorldArtwork) return;
        var device = DXManager.Device;
        var colour = RendererDevice.ToVector(tint);
        colour.W *= device.Opacity;
        _target.Add(new Draw(texture, source, position ?? Vector3.Zero, colour,
            DXManager.Sprite.Transform, device.ArtworkBlend ?? device.Blend));
    }

    internal readonly struct Scope : IDisposable
    {
        private readonly List<Draw> _previous;
        private readonly Kind _previousKind;

        public Scope(List<Draw> target, Kind kind)
        {
            _previous = _target;
            _previousKind = _kind;
            _target = target;
            _kind = kind;
        }

        public void Dispose()
        {
            _target = _previous;
            _kind = _previousKind;
        }
    }
}
