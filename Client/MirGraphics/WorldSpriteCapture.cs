namespace Client.MirGraphics;

internal static class WorldSpriteCapture
{
    internal enum Kind { Terrain, Scenery, Actor }

    internal readonly record struct Draw(Texture Texture, Rectangle? Source, Vector3 Position, Vector4 Tint,
        Matrix Transform, SpriteBlendMode Blend, bool OccludesWeather, int Row)
    {
        public bool Occludes => OccludesWeather &&
            Blend is not (SpriteBlendMode.Additive or SpriteBlendMode.InverseLight or SpriteBlendMode.Screen);
    }

    private static List<Draw> _target;
    private static Kind _kind;
    private static int _row;

    internal static void Record(Texture texture, Rectangle? source, Vector3? position, Color tint)
    {
        if (_target == null || texture == null || texture.Disposed || tint.A == 0) return;
        if (_kind == Kind.Actor && !texture.IsWorldArtwork) return;
        var device = DXManager.Device;
        var colour = RendererDevice.ToVector(tint);
        colour.W *= device.Opacity;
        _target.Add(new Draw(texture, source, position ?? Vector3.Zero, colour,
            DXManager.Sprite.Transform, device.ArtworkBlend ?? device.Blend, _kind != Kind.Terrain, _row));
    }

    internal readonly struct Scope : IDisposable
    {
        private readonly List<Draw> _previous;
        private readonly Kind _previousKind;
        private readonly int _previousRow;

        public Scope(List<Draw> target, Kind kind, int row)
        {
            _previous = _target;
            _previousKind = _kind;
            _previousRow = _row;
            _target = target;
            _kind = kind;
            _row = row;
        }

        public void Dispose()
        {
            _target = _previous;
            _kind = _previousKind;
            _row = _previousRow;
        }
    }
}
