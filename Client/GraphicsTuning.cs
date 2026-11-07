using CrystalM2.Rendering.Core;

namespace Client;

internal enum GraphicsLayer
{
    Player, Monster, Npc, Item, Decoration, Ground, Middle, Front, MapAnimation,
    Spell, Particles, Weather, Background
}

internal enum LightSource { Player, Monster, Npc, Torch, Map, MapAnimation, Spell }
internal enum WeatherEdgeMode { Natural, Soft, Silhouette }

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

    internal sealed class Position(string key, bool canLink = true, int range = 128)
    {
        public readonly Value X = new(key + "X", 0, -range, range);
        public readonly Value Y = new(key + "Y", 0, -range, range);
        public readonly Value Link = canLink ? new(key + "Linked", 0, 0, 1) : null;
        public bool Linked => Link is { Current: 1 };
        public Point Pixels => Apply(Point.Empty);

        public Point Apply(Point point, bool originLinked = false)
        {
            point.Offset(X.Current, Y.Current);
            if (Linked && !originLinked) point.Offset(GroupOffset.X.Current, GroupOffset.Y.Current);
            return point;
        }
    }

    internal sealed class Emitter(LightSource source)
    {
        public readonly Position Offset = new(source + "LightOffset", range: IsEffectLight(source) ? 2048 : 128);
        public readonly Value Strength = new(source + "LightStrength", 100, 0, IsEffectLight(source) ? 1200 : 300);
        public readonly Value Radius = new(source + "LightRadius", 100, 25, IsEffectLight(source) ? 1600 : 250);
        public readonly Value Warmth = new(source + "LightWarmth", 0, -100, 100);
        public readonly Value Shape = new(source + "LightShape", 0, 0, 2);
        public readonly Value Direction = new(source + "LightDirection", 0, 0, 360);
        public readonly Value Beam = new(source + "LightBeam", 65, 12, 175);
        public readonly Value Blur = new(source + "LightBlur", 4, 0, 192);
        public readonly Value GlowRadius = IsEffectLight(source)
            ? new(source + "LightGlowRadius", source == LightSource.MapAnimation ? 160 : 128, 0, 1024) : null;
        public readonly Value GlowStrength = IsEffectLight(source)
            ? new(source + "LightGlowStrength", 45, 0, 200) : null;
        public readonly Value GlowBlur = IsEffectLight(source)
            ? new(source + "LightGlowBlur", 0, 0, 192) : null;
        public readonly Value CustomColour = new(source + "LightCustomColour", 0, 0, 1);
        public readonly Value Red = new(source + "LightRed", 255, 0, 255);
        public readonly Value Green = new(source + "LightGreen", 255, 0, 255);
        public readonly Value Blue = new(source + "LightBlue", 255, 0, 255);
        public float Intensity => Strength.Current / 100f;
        public Color Colour => Color.FromArgb(Red.Current, Green.Current, Blue.Current);
    }

    internal static readonly GraphicsLayer[] Layers = Enum.GetValues<GraphicsLayer>();
    internal static readonly LightSource[] LightSources = Enum.GetValues<LightSource>();
    private static readonly Position[] Offsets = Layers.Select(layer => new Position(layer + "Offset")).ToArray();
    private static readonly Value[] Blends = Layers.Select(layer => new Value(layer + "BlendMode", 0, 0, 7)).ToArray();
    private static readonly Emitter[] Emitters = LightSources.Select(source => new Emitter(source)).ToArray();

    internal static Position Offset(GraphicsLayer layer) => Offsets[(int)layer];
    internal static Emitter Light(LightSource source) => Emitters[(int)source];
    internal static bool IsEffectLight(LightSource source) => source is LightSource.MapAnimation or LightSource.Spell;
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
    internal static readonly Value MapColour = new("AmbientMapColour", 100, 0, 100);
    internal static readonly Value NightFloor = new("AmbientNightFloor", 14, 0, 35);
    internal static readonly Position AllLights = new("AllLightOffset");
    internal static readonly Position GroupOffset = new("LinkedOffset", canLink: false);
    internal static readonly Value OffsetGuides = new("OffsetGuides", 0, 0, 1);

    internal static Point LightPosition(LightSource source, Point point, bool originLinked = false) =>
        Light(source).Offset.Apply(AllLights.Apply(point, originLinked), originLinked || AllLights.Linked);

    internal static readonly Value Exposure = new("PostExposure", 100, 25, 200);
    internal static readonly Value Contrast = new("PostContrast", 100, 25, 200);
    internal static readonly Value Saturation = new("PostSaturation", 100, 0, 200);
    internal static readonly Value Gamma = new("PostGamma", 100, 50, 200);
    internal static readonly Value Grade = new("PostGrade", 0, 0, 4);
    internal static readonly Value Tone = new("PostTone", 0, 0, 2);
    internal static readonly Value PostShader = new("PostShader", 0, 0, 8);
    internal static readonly Value PostShaderStrength = new("PostShaderStrength", 100, 0, 200);
    internal static readonly Value BlackLift = new("PostBlackLift", 0, -25, 25);
    internal static readonly Value Highlights = new("PostHighlights", 100, 25, 200);
    internal static readonly Value Vibrance = new("PostVibrance", 0, -100, 100);
    internal static readonly Value SpellComposite = new("SpellComposite", 0, 0, 3);
    internal static readonly Value SpellBlackCutoff = new("SpellBlackCutoff", 4, 0, 50);
    internal static readonly Value SpellBlackFeather = new("SpellBlackFeather", 15, 1, 50);
    internal static readonly Value SpellExposure = new("SpellExposure", 115, 25, 300);
    internal static readonly Value SpellSaturation = new("SpellSaturation", 120, 0, 220);
    internal static readonly Value SpellVisualSize = new("SpellVisualSize", 128, 75, 225);
    internal static readonly Value SpellGlowStrength = new("SpellGlowStrength", 70, 0, 200);
    internal static readonly Value SpellGlowCutoff = new("SpellGlowCutoff", 44, 0, 100);
    internal static readonly Value SpellGlowBlend = new("SpellGlowBlend", 0, 0, 1);
    internal static readonly Value DeathLook = new("DeathLook", 0, 0, 2);
    internal static readonly Value FogDensity = new("PostFogDensity", 100, 0, 200);
    internal static readonly Value WeatherSource = new("WeatherSource", 0, 0, 1);
    internal static readonly Value WeatherRain = new("WeatherRain", 0, 0, 1);
    internal static readonly Value WeatherSnow = new("WeatherSnow", 0, 0, 1);
    internal static readonly Value WeatherFog = new("WeatherFog", 0, 0, 1);
    internal static readonly Value WeatherStorm = new("WeatherStorm", 0, 0, 1);
    internal static readonly Value Precipitation = new("WeatherPrecipitation", 100, 0, 200);
    internal static readonly Value SurfaceWater = new("WeatherSurfaceWater", 100, 0, 200);
    internal static readonly Value SurfaceSnow = new("WeatherSurfaceSnow", 100, 0, 200);
    internal static readonly Value WeatherWind = new("WeatherWind", 100, 0, 200);
    internal static readonly Value WeatherEdgeStyle = new("WeatherEdgeStyle", 0, 0, 2);
    internal static readonly Value WeatherSceneryFade = new("WeatherSceneryFade", 14, 0, 48);
    internal static readonly Value WeatherTileFade = new("WeatherTileFade", 36, 0, 72);
    internal static readonly Value WeatherEdgeBreakup = new("WeatherEdgeBreakup", 55, 0, 100);
    internal static readonly Value WeatherDropFade = new("WeatherDropFade", 100, 0, 100);
    internal static int WeatherMaskView { get; set; }

    internal static WeatherSetting LocalWeather =>
        (WeatherRain.Current != 0 || WeatherStorm.Current != 0 ? WeatherSetting.Rain : WeatherSetting.None) |
        (WeatherSnow.Current != 0 ? WeatherSetting.Snow : WeatherSetting.None) |
        (WeatherFog.Current != 0 ? WeatherSetting.Fog : WeatherSetting.None);
    internal static readonly Value BloomThreshold = new("BloomThreshold", 58, 0, 100);
    internal static readonly Value TextureFilter = new("TextureFilter", 0, 0, 4);
    internal static readonly Value TextureEdgeMatch = new("TextureEdgeMatch", 8, 1, 45);
    internal static readonly Value TextureSmoothing = new("TextureSmoothing", 100, 0, 100);
    internal static readonly Value TextureDetail = new("TextureDetail", 0, 0, 100);
    internal static readonly Value CorpseBlend = new("CorpseBlendMode", 0, 0, 7);
    internal static readonly Value HighlightBlend = new("HighlightBlendMode", 0, 0, 7);
    internal static readonly Value GroundParticleBlend = new("GroundParticleBlendMode", 0, 0, 7);

    internal static readonly Value BakedEnabled = new("BakedShadowRepair", 1, 0, 1);
    internal static readonly Value BakedJoin = new("BakedShadowJoinEdges", 1, 0, 1);
    internal static readonly Value BakedDarkness = new("BakedShadowDetection", 24, 8, 64);
    internal static readonly Value BakedMinAlpha = new("BakedShadowMinDotAlpha", 96, 32, 255);
    internal static readonly Value BakedTolerance = new("BakedShadowColourTolerance", 4, 0, 16);
    internal static readonly Value BakedRadius = new("BakedShadowBlur", 2, 1, 5);
    internal static readonly Value BakedOpacity = new("BakedShadowOpacity", 100, 0, 200);
    internal static readonly Value ProjectedWidth = new("ProjectedShadowWidth", 100, 50, 160);
    internal static readonly Value ProjectedSlope = new("ProjectedShadowDirection", 0, -100, 100);
    internal static readonly Value ProjectedPolicy = new("ProjectedShadowSource", 0, 0, 1);
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
