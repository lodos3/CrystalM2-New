using CrystalM2.Rendering.Core;

namespace Client;

internal enum GraphicsLayer
{
    Player, Monster, Npc, Item, Decoration, Ground, Middle, Front, MapAnimation,
    Spell, Particles, Weather, Background
}

internal enum LightSource { Player, Monster, Npc, Torch, Map, MapAnimation, Spell }

internal static class GraphicsTuning
{
    private static readonly List<Value> Values = new();

    internal sealed class Value
    {
        public string Key { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public int Default { get; }
        public int Current { get; set; }

        public Value(string key, int initial, int minimum, int maximum)
        {
            Key = key;
            Current = initial;
            Default = initial;
            Minimum = minimum;
            Maximum = maximum;
            Values.Add(this);
        }
    }

    internal sealed class Position(string key)
    {
        public readonly Value X = new(key + "X", 0, -128, 128);
        public readonly Value Y = new(key + "Y", 0, -128, 128);
        public Point Pixels => new(X.Current, Y.Current);
        public Point Apply(Point point) => new(point.X + X.Current, point.Y + Y.Current);
    }

    internal sealed class Emitter(LightSource source)
    {
        public readonly Position Offset = new(source + "LightOffset");
        public readonly Value Strength = new(source + "LightStrength", 100, 0, 300);
        public readonly Value Radius = new(source + "LightRadius", 100, 25, 250);
        public readonly Value Warmth = new(source + "LightWarmth", 0, -100, 100);
        public float Intensity => Strength.Current / 100f;
    }

    internal static readonly GraphicsLayer[] Layers = Enum.GetValues<GraphicsLayer>();
    internal static readonly LightSource[] LightSources = Enum.GetValues<LightSource>();
    private static readonly Position[] Offsets = Layers.Select(layer => new Position(layer + "Offset")).ToArray();
    private static readonly Value[] Blends = Layers.Select(layer => new Value(layer + "BlendMode", 0, 0, 5)).ToArray();
    private static readonly Emitter[] Emitters = LightSources.Select(source => new Emitter(source)).ToArray();

    internal static Position Offset(GraphicsLayer layer) => Offsets[(int)layer];
    internal static Emitter Light(LightSource source) => Emitters[(int)source];
    internal static Value Blend(GraphicsLayer layer) => Blends[(int)layer];

    internal static GraphicsLayer Layer(ObjectType type) => type switch
    {
        ObjectType.Player or ObjectType.Hero => GraphicsLayer.Player,
        ObjectType.Monster or ObjectType.Creature => GraphicsLayer.Monster,
        ObjectType.Merchant => GraphicsLayer.Npc,
        ObjectType.Item => GraphicsLayer.Item,
        ObjectType.Spell => GraphicsLayer.Spell,
        _ => GraphicsLayer.Decoration
    };

    internal static readonly Value Dawn = new("DawnShade", 100, 0, 100);
    internal static readonly Value Evening = new("EveningShade", 100, 0, 100);
    internal static readonly Value Night = new("NightShade", 100, 0, 100);
    internal static readonly Value DayGlow = new("DayGlow", 14, 0, 100);
    internal static readonly Value DayBrightness = new("DayBrightness", 100, 0, 100);
    internal static readonly Value LightWarmth = new("AmbientWarmth", 0, -100, 100);
    internal static readonly Position AllLights = new("AllLightOffset");

    internal static Point LightPosition(LightSource source, Point point) =>
        AllLights.Apply(Light(source).Offset.Apply(point));

    internal static readonly Value Exposure = new("PostExposure", 100, 25, 200);
    internal static readonly Value Contrast = new("PostContrast", 100, 25, 200);
    internal static readonly Value Saturation = new("PostSaturation", 100, 0, 200);
    internal static readonly Value Gamma = new("PostGamma", 100, 50, 200);
    internal static readonly Value Grade = new("PostGrade", 0, 0, 4);
    internal static readonly Value Tone = new("PostTone", 0, 0, 2);
    internal static readonly Value PostShader = new("PostShader", 0, 0, 3);
    internal static readonly Value DeathLook = new("DeathLook", 0, 0, 2);
    internal static readonly Value FogDensity = new("PostFogDensity", 100, 0, 200);
    internal static readonly Value BloomThreshold = new("BloomThreshold", 58, 0, 100);
    internal static readonly Value TextureFilter = new("TextureFilter", 0, 0, 3);
    internal static readonly Value CorpseBlend = new("CorpseBlendMode", 0, 0, 5);
    internal static readonly Value HighlightBlend = new("HighlightBlendMode", 0, 0, 5);
    internal static readonly Value GroundParticleBlend = new("GroundParticleBlendMode", 0, 0, 5);

    internal static readonly Value BakedEnabled = new("BakedShadowRepair", 1, 0, 1);
    internal static readonly Value BakedJoin = new("BakedShadowJoinEdges", 1, 0, 1);
    internal static readonly Value BakedDarkness = new("BakedShadowDetection", 24, 8, 64);
    internal static readonly Value BakedMinAlpha = new("BakedShadowMinDotAlpha", 96, 32, 255);
    internal static readonly Value BakedTolerance = new("BakedShadowColourTolerance", 4, 0, 16);
    internal static readonly Value BakedRadius = new("BakedShadowBlur", 2, 1, 5);
    internal static readonly Value BakedOpacity = new("BakedShadowOpacity", 100, 0, 200);
    internal static readonly Value ProjectedWidth = new("ProjectedShadowWidth", 100, 50, 160);
    internal static readonly Value ProjectedSlope = new("ProjectedShadowDirection", 0, -100, 100);
    internal static readonly Position ProjectedOffset = new("ProjectedShadowOffset");

    internal static BakedShadowSettings BakedSettings { get; private set; } = BakedShadowSettings.Default;
    internal static bool RepairBakedShadows { get; private set; } = true;
    internal static bool JoinBakedEdges { get; private set; } = true;
    internal static int ShadowRevision { get; private set; }

    internal static void ApplyBakedSettings()
    {
        BakedSettings = new(BakedDarkness.Current, BakedMinAlpha.Current, BakedTolerance.Current,
            BakedRadius.Current, BakedOpacity.Current);
        RepairBakedShadows = BakedEnabled.Current != 0;
        JoinBakedEdges = BakedJoin.Current != 0;
        ShadowRevision++;
    }

    internal static void Load(Func<string, int, int, int, int> read)
    {
        foreach (Value value in Values)
            value.Current = read(value.Key, value.Current, value.Minimum, value.Maximum);
        ApplyBakedSettings();
    }

    internal static void Save(Action<string, object> write)
    {
        foreach (Value value in Values)
            write(value.Key, value.Current);
    }
}
