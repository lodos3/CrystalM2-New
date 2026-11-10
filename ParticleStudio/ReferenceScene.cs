using System.Drawing;
using System.IO;
using System.Numerics;
using Client.MirGraphics;
using Client.MirObjects;
using Client.MirScenes;
using Client.Rendering;
using CrystalM2.Rendering.Core;

namespace CrystalM2.ParticleStudio;

internal sealed class ReferenceScene : IDisposable
{
    private const int CellWidth = MapControl.CellWidth;
    private const int CellHeight = MapControl.CellHeight;
    private const string Armour = @"CArmour\00";
    private const string Weapon = @"CWeapon\02";
    private const string Hair = @"CHair\00";
    private readonly Dictionary<int, string> _mapLibraries = new();
    private readonly Dictionary<string, LegacyMirLibReader?> _readers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(string Library, int Frame), Artwork?> _frames = new();
    private readonly List<DrawItem> _floor = [];
    private readonly List<DrawItem> _objects = [];
    private readonly List<MapLight> _lights = [];
    private MapReader? _map;
    private Point _cell;

    public ReferenceScene(string clientDirectory)
    {
        ClientDirectory = Path.GetFullPath(clientDirectory);
        if (!Directory.Exists(Path.Combine(ClientDirectory, "Data", "CArmour")))
            throw new InvalidDataException("Choose a client folder containing Data\\CArmour.");
        MapLibraryFiles.Register((index, path) => _mapLibraries[index] = path);
    }

    public string ClientDirectory { get; }
    public string? MapFile { get; private set; }
    public string MapDirectory => Path.Combine(ClientDirectory, "Map");
    public bool HasFloor => _floor.Count != 0;
    public bool HasActor { get; private set; }
    public IReadOnlyList<MapLight> Lights => _lights;
    internal readonly record struct MapLight(Vector2 Position, int Index, Color Colour);
    private sealed record Artwork(Texture Texture, int OffsetX, int OffsetY, WeaponMaskTrace? Weapon);
    private readonly record struct DrawItem(Texture Texture, Vector2 Position, float FootY, bool Additive);

    internal static string? FindClientDirectory()
    {
        for (var parent = new DirectoryInfo(AppContext.BaseDirectory); parent != null; parent = parent.Parent)
        {
            string sibling = Path.Combine(parent.FullName, "Client", "Release");
            if (Directory.Exists(Path.Combine(sibling, "Data", "CArmour"))) return sibling;
            if (Directory.Exists(Path.Combine(parent.FullName, "Data", "CArmour"))) return parent.FullName;
        }
        return null;
    }

    public void OpenDefaultMap()
    {
        string candidate = Path.Combine(MapDirectory, "0101.map");
        if (File.Exists(candidate)) OpenMap(candidate);
    }

    public void OpenMap(string filename)
    {
        var file = new FileInfo(filename);
        if (!file.Exists || file.Length < 52 || file.Length > 32 * 1024 * 1024)
            throw new InvalidDataException("Choose an existing CrystalM2 map file up to 32 MB.");
        MapReader map;
        try { map = new MapReader(file.FullName); }
        catch (IndexOutOfRangeException error)
        {
            throw new InvalidDataException("The map file is truncated or its layout is unsupported.", error);
        }
        if (map.Width <= 0 || map.Height <= 0)
            throw new InvalidDataException("The reference map has no tiles.");
        Point centre = new(map.Width / 2, map.Height / 2);
        // Use a walkable cell near the middle of the map.
        int best = int.MaxValue;
        Point anchor = centre;
        for (int y = Math.Max(0, centre.Y - 16); y <= Math.Min(map.Height - 1, centre.Y + 16); y++)
        for (int x = Math.Max(0, centre.X - 16); x <= Math.Min(map.Width - 1, centre.X + 16); x++)
        {
            var cell = map.MapCells[x, y];
            int distance = Math.Abs(x - centre.X) + Math.Abs(y - centre.Y);
            if (distance < best && cell.AllowsWeather)
            {
                best = distance;
                anchor = new Point(x, y);
            }
        }
        _map = map;
        _cell = anchor;
        MapFile = file.FullName;
        ClearFrames();
    }

    public PreviewRig WithWeapon(RendererDevice device, PreviewRig rig, MirAction action)
    {
        return rig with
        {
            ReferenceWeapon = progress =>
            {
                int frame = PlayerFrame(action, rig.Direction, progress);
                var weapon = Sprite(device, Weapon, frame, weaponTrace: true);
                if (weapon?.Weapon is not { } trace) return null;
                Vector2 origin = ActorOrigin(rig.Centre) + new Vector2(weapon.OffsetX, weapon.OffsetY);
                Vector2 hand = rig.Centre - Vector2.UnitY * 40;
                return new WeaponMaskTrace(trace.Hilt + origin, trace.Tip + origin).Orient(hand, null);
            }
        };
    }

    public void Gather(RendererDevice device, PreviewRig rig, PreviewStage? stage, int time)
    {
        _floor.Clear();
        _objects.Clear();
        _lights.Clear();
        HasActor = false;
        if (_map != null) GatherMap(device, rig.Centre, time);

        MirAction action = stage?.ActorAction ?? MirAction.Standing;
        float progress = action == MirAction.Standing ? 0 : time / (float)Math.Max(1, stage!.Duration);
        int frame = PlayerFrame(action, rig.Direction, progress);
        var origin = ActorOrigin(rig.Centre);
        var body = Sprite(device, Armour, frame) ?? Sprite(device, Armour, PlayerFrame(MirAction.Standing, rig.Direction, 0));
        if (body != null)
        {
            AddActor(body);
            HasActor = true;
        }
        AddActor(Sprite(device, Hair, frame));
        AddActor(Sprite(device, Weapon, frame, weaponTrace: true));

        void AddActor(Artwork? sprite)
        {
            if (sprite != null)
                _objects.Add(new DrawItem(sprite.Texture, origin + new Vector2(sprite.OffsetX, sprite.OffsetY), rig.Centre.Y, false));
        }
    }

    private void GatherMap(RendererDevice device, Vector2 centre, int time)
    {
        var map = _map!;
        int columns = device.TargetWidth / CellWidth / 2 + 4;
        int rows = device.TargetHeight / CellHeight / 2 + 8;
        int firstX = Math.Max(0, _cell.X - columns);
        int firstY = Math.Max(0, _cell.Y - rows);
        int lastX = Math.Min(map.Width - 1, _cell.X + columns);
        int lastY = Math.Min(map.Height - 1, _cell.Y + rows);
        int animation = time / 100;
        for (int y = firstY; y <= lastY; y++)
        for (int x = firstX; x <= lastX; x++)
        {
            var cell = map.MapCells[x, y];
            Vector2 tile = ActorOrigin(centre) + new Vector2((x - _cell.X) * CellWidth, (y - _cell.Y) * CellHeight);
            if ((x & 1) == 0 && (y & 1) == 0)
                Tile(cell.BackIndex, (cell.BackImage & 0x1fffffff) - 1, tile, true, false, false);

            int middleAnimation = cell.MiddleAnimationFrame is 0 or 255 ? 0 : cell.MiddleAnimationFrame & 15;
            int frontAnimation = cell.FrontAnimationFrame & 127;
            Tile(cell.MiddleIndex, Frame(cell.MiddleImage - 1, middleAnimation, cell.MiddleAnimationTick),
                tile, false, middleAnimation > 0, middleAnimation is 8 or 10);
            Tile(cell.FrontIndex, Frame((cell.FrontImage & 0x7fff) - 1, frontAnimation, cell.FrontAnimationTick),
                tile, false, frontAnimation > 0, (cell.FrontAnimationFrame & 128) != 0);
            if (MapLightEncoding.TryDecode(cell.Light, DXManager.LightSizes.Length - 1, out int light, out Color colour))
                _lights.Add(new MapLight(tile + new Vector2(10, 32), light, colour));

            int Frame(int index, int count, int tick) => index < 0 || count == 0
                ? index : index + animation / (tick + 1) % count;
        }

        void Tile(int library, int index, Vector2 position, bool back, bool animated, bool additive)
        {
            if (index < 0 || !_mapLibraries.TryGetValue(library, out string? path) || Sprite(device, path, index) is not { } sprite)
                return;
            var texture = sprite.Texture;
            bool flat = texture.Width == CellWidth && texture.Height == CellHeight ||
                texture.Width == 2 * CellWidth && texture.Height == 2 * CellHeight;
            if (back || flat && !animated)
                _floor.Add(new DrawItem(texture, position, 0, false));
            else
            {
                var artworkPosition = position - Vector2.UnitY * texture.Height;
                _objects.Add(new DrawItem(texture, artworkPosition, position.Y + CellHeight / 2, additive));
            }
        }
    }

    public void DrawFloor(RendererDevice device)
    {
        foreach (var item in _floor) Draw(device, item, 0);
    }

    public void DrawObjects(RendererDevice device, bool depthMask)
    {
        foreach (var item in _objects.OrderBy(item => item.FootY))
        {
            if (depthMask && item.Additive) continue;
            if (!depthMask) device.Blend = item.Additive ? SpriteBlendMode.Additive : SpriteBlendMode.Alpha;
            Draw(device, item, depthMask ? Math.Max(1, MathF.Floor(item.FootY / CellHeight) + 1) : 0);
        }
        if (!depthMask) device.Blend = SpriteBlendMode.Alpha;
    }

    private static void Draw(RendererDevice device, DrawItem item, float depth) =>
        device.Sprite.Draw(item.Texture, null, new Vector3(item.Position, depth), Vector4.One);

    private Artwork? Sprite(RendererDevice device, string library, int index, bool weaponTrace = false)
    {
        if (_frames.TryGetValue((library, index), out var artwork)) return artwork;
        if (!_readers.TryGetValue(library, out var reader))
        {
            string path = Path.Combine(ClientDirectory, "Data", library + ".Lib");
            reader = File.Exists(path) ? new LegacyMirLibReader(
                new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) : null;
            _readers.Add(library, reader);
        }
        if (reader?.TryReadFrame(index, out var frame) == true && frame != null)
        {
            var texture = new Texture(device, frame.Width, frame.Height) { IsWorldArtwork = true };
            try
            {
                var upload = texture.BeginUpload();
                upload.Data.Write(frame.Bgra);
                texture.EndUpload();
                var trace = weaponTrace ? AlphaCoverage.Capture(frame.Bgra, frame.Width, frame.Height, frame.Width * 4).GetWeaponTrace() : null;
                artwork = new Artwork(texture, frame.OffsetX, frame.OffsetY, trace);
            }
            catch
            {
                texture.Dispose();
                throw;
            }
        }
        _frames[(library, index)] = artwork;
        return artwork;
    }

    private static Vector2 ActorOrigin(Vector2 feet) => feet - new Vector2(CellWidth / 2, CellHeight / 2);

    private static int PlayerFrame(MirAction action, MirDirection direction, float progress)
    {
        Frame frame = FrameSet.Player[action];
        int index = Math.Clamp((int)(progress * frame.Count), 0, frame.Count - 1);
        return frame.Start + frame.OffSet * (int)direction + index;
    }

    private void ClearFrames()
    {
        foreach (var artwork in _frames.Values) artwork?.Texture.Dispose();
        _frames.Clear();
        foreach (var reader in _readers.Values) reader?.Dispose();
        _readers.Clear();
        _floor.Clear();
        _objects.Clear();
        _lights.Clear();
    }

    public void Dispose() => ClearFrames();
}
