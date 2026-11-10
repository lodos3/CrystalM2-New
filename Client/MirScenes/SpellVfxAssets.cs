using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CrystalM2.Rendering.Core;

namespace Client.MirScenes;

/// <summary>The file format shared by the client and particle authoring tools.</summary>
public static class SpellVfxAssets
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { RequireNodeProperties }
        },
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static IReadOnlyList<string> Groups { get; } =
        Array.AsReadOnly(new[] { "Warrior", "Wizard", "Taoist", "Signature", "Legacy", "Frame", "Debris" });

    public static string InstalledDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Data", "SpellVfx");

    private static readonly Dictionary<string, JsonObject> BuiltIn = Groups.ToDictionary(
        group => group, group => ReadBuiltIn(group), StringComparer.Ordinal);

    private static Snapshot _active = Load(InstalledDirectory, tolerateBadFiles: true);
    [ThreadStatic] private static Snapshot _frame;
    [ThreadStatic] private static SpellVfxData _scoped;
    [ThreadStatic] private static Spell _spell;
    [ThreadStatic] private static HashSet<(string Group, string Node)> _observedNodes;

    internal static SpellVfxData Current => _scoped ?? Volatile.Read(ref _active).Defaults;

    /// <summary>Apply a complete, validated directory to subsequent effect calls.</summary>
    public static void Reload(string directory) => Volatile.Write(ref _active, Load(directory, tolerateBadFiles: false));

    internal static void PreviewSpell(string directory, Spell spell, JsonObject overrides)
    {
        var defaults = ReadGroups(directory, tolerateBadFiles: false);
        var settings = Decode(defaults);
        var draft = WithOverrides(settings, defaults, (JsonObject)JsonNode.Parse(overrides.ToJsonString()));
        Volatile.Write(ref _active, new Snapshot(settings, new Dictionary<Spell, SpellVfxData> { [spell] = draft }));
    }

    [ThreadStatic] private static Dictionary<(string Group, string Node), HashSet<string>> _observedFields;

    internal static HashSet<(string Group, string Node)> CollectNodes(Action sample) =>
        CollectNodes(sample, out _);

    internal static HashSet<(string Group, string Node)> CollectNodes(Action sample,
        out Dictionary<(string Group, string Node), HashSet<string>> fields)
    {
        var previous = _observedNodes;
        var previousFields = _observedFields;
        var result = new HashSet<(string Group, string Node)>();
        fields = new();
        _observedNodes = result;
        _observedFields = fields;
        try { sample(); }
        finally { (_observedNodes, _observedFields) = (previous, previousFields); }
        return result;
    }

    internal static FieldProbe Fields(string group, string node) => new(group, node);

    internal readonly struct FieldProbe(string group, string node)
    {
        internal T Read<T>(T value, [CallerArgumentExpression(nameof(value))] string expression = null)
        {
            if (_observedFields == null) return value;
            var key = (group, node);
            _observedNodes.Add(key);
            if (!_observedFields.TryGetValue(key, out var fields))
                _observedFields[key] = fields = new(StringComparer.Ordinal);
            fields.Add(expression["tuning.".Length..]);
            return value;
        }
    }

    internal static T Observe<T>(T node, [CallerArgumentExpression(nameof(node))] string path = null)
    {
        if (_observedNodes != null)
        {
            const string prefix = "SpellVfxAssets.Current.";
            int split = path.IndexOf('.', prefix.Length);
            _observedNodes.Add((path[prefix.Length..split], path[(split + 1)..]));
        }
        return node;
    }

    /// <summary>Return the effective node settings for a group, including any local defaults.</summary>
    public static JsonObject ReadGroup(string directory, string group)
    {
        string path = GroupPath(directory, group);
        return Merge(BuiltIn[group], ReadSection(path, "nodes"), group);
    }

    /// <summary>Return the explicit overrides for a spell, grouped by asset and node.</summary>
    public static JsonObject ReadSpell(string directory, Spell spell)
    {
        var groups = ReadSection(SpellPath(directory, spell), "groups");
        var defaults = ReadGroups(directory, tolerateBadFiles: false);
        WithOverrides(Decode(defaults), defaults, groups);
        return groups;
    }

    /// <summary>Return a group's values as used by a particular spell.</summary>
    public static JsonObject ReadSpellGroup(string directory, Spell spell, string group)
    {
        var defaults = ReadGroup(directory, group);
        var overrides = ReadSpell(directory, spell);
        return overrides[group] is JsonObject nodes ? Merge(defaults, nodes, group) : defaults;
    }

    public static void SaveGroup(string directory, string group, JsonObject nodes)
    {
        string path = GroupPath(directory, group);
        JsonObject document = CreateDocument("nodes", nodes);
        var groups = ReadGroups(directory, tolerateBadFiles: false);
        groups[group] = Merge(BuiltIn[group], (JsonObject)document["nodes"], group);
        var settings = Decode(groups);
        ReadSpells(directory, settings, groups, tolerateBadFiles: false);
        WriteAtomic(path, document);
    }

    public static void SaveSpell(string directory, Spell spell, JsonObject groups)
    {
        string path = SpellPath(directory, spell);
        JsonObject document = CreateDocument("groups", groups);
        var defaults = ReadGroups(directory, tolerateBadFiles: false);
        WithOverrides(Decode(defaults), defaults, (JsonObject)document["groups"]);
        WriteAtomic(path, document);
    }

    private static string GroupPath(string directory, string group) => BuiltIn.ContainsKey(group)
        ? Path.Combine(directory, group + ".json")
        : throw new ArgumentException($"Unknown VFX group '{group}'.", nameof(group));

    private static string SpellPath(string directory, Spell spell) => Enum.IsDefined(spell) && spell != Spell.None
        ? Path.Combine(directory, "Spells", spell + ".json")
        : throw new ArgumentOutOfRangeException(nameof(spell));

    private static JsonObject ReadBuiltIn(string group)
    {
        using var stream = typeof(SpellVfxAssets).Assembly.GetManifestResourceStream($"SpellVfx.{group}.json")
            ?? throw new InvalidOperationException($"Missing built-in VFX group '{group}'.");
        return Section(JsonNode.Parse(stream), "nodes", group);
    }

    private static JsonObject ReadSection(string path, string section)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            return Section(JsonNode.Parse(stream), section, path);
        }
        catch (FileNotFoundException) { return new JsonObject(); }
        catch (DirectoryNotFoundException) { return new JsonObject(); }
    }

    private static JsonObject Section(JsonNode document, string section, string source)
    {
        if (document is not JsonObject obj || obj.Count != 2 ||
            obj["schemaVersion"] is not JsonValue version || !version.TryGetValue<int>(out int number) ||
            number != SchemaVersion || obj[section] is not JsonObject result)
            throw new InvalidDataException($"Expected schema version {SchemaVersion} and a '{section}' object in {source}.");
        return result;
    }

    private static JsonObject CreateDocument(string section, JsonObject values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return (JsonObject)JsonNode.Parse(new JsonObject
        {
            ["schemaVersion"] = SchemaVersion,
            [section] = values.DeepClone()
        }.ToJsonString());
    }

    private static void WriteAtomic(string path, JsonObject document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, document.ToJsonString(Options) + Environment.NewLine);
            File.Move(temp, path, overwrite: true);
        }
        finally { File.Delete(temp); }
    }

    private static Snapshot Load(string directory, bool tolerateBadFiles)
    {
        var groups = ReadGroups(directory, tolerateBadFiles);
        var settings = Decode(groups);
        return new Snapshot(settings, ReadSpells(directory, settings, groups, tolerateBadFiles));
    }

    private static Dictionary<string, JsonObject> ReadGroups(string directory, bool tolerateBadFiles)
    {
        var result = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (string group in Groups)
        {
            try
            {
                result[group] = ReadGroup(directory, group);
                if (tolerateBadFiles) DecodeGroup(new SpellVfxData(), group, result[group]);
            }
            catch (Exception error) when (tolerateBadFiles && IsAssetError(error))
            {
                Trace.TraceError($"Spell VFX {group}: {error.Message}. Using built-in values.");
                result[group] = BuiltIn[group];
            }
        }
        return result;
    }

    private static Dictionary<Spell, SpellVfxData> ReadSpells(string directory, SpellVfxData settings,
        Dictionary<string, JsonObject> defaults, bool tolerateBadFiles)
    {
        var result = new Dictionary<Spell, SpellVfxData>();
        string path = Path.Combine(directory, "Spells");
        if (!Directory.Exists(path)) return result;
        foreach (string file in Directory.EnumerateFiles(path, "*.json"))
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!Enum.TryParse(name, out Spell spell) || spell == Spell.None || spell.ToString() != name)
                    throw new InvalidDataException($"Unknown spell in {file}.");
                var groups = ReadSection(file, "groups");
                if (groups.Count != 0)
                    result.Add(spell, WithOverrides(settings, defaults, groups));
            }
            catch (Exception error) when (tolerateBadFiles && IsAssetError(error))
            {
                Trace.TraceError($"Spell VFX {file}: {error.Message}. Using group defaults.");
            }
        }
        return result;
    }

    private static bool IsAssetError(Exception error) =>
        error is IOException or InvalidDataException or JsonException or ArgumentException or
            InvalidOperationException or FormatException or UnauthorizedAccessException;

    private static SpellVfxData WithOverrides(SpellVfxData settings, Dictionary<string, JsonObject> defaults,
        JsonObject overrides)
    {
        foreach (var (group, value) in overrides)
        {
            if (!defaults.TryGetValue(group, out var original) || value is not JsonObject nodes)
                throw new InvalidDataException($"Invalid VFX group '{group}'.");
            settings = DecodeGroup(settings, group, Merge(original, nodes, group));
        }
        return settings;
    }

    private static JsonObject Merge(JsonObject defaults, JsonObject changes, string path)
    {
        var merged = (JsonObject)defaults.DeepClone();
        foreach (var (key, value) in changes)
        {
            string field = path + "." + key;
            if (!defaults.TryGetPropertyValue(key, out var original) || original == null ||
                value == null || !SameValueType(original, value))
                throw new InvalidDataException($"Unknown VFX field or wrong value type at {field}.");

            if (original is JsonObject originalObject && value is JsonObject valueObject)
                merged[key] = Merge(originalObject, valueObject, field);
            else
            {
                if (value.GetValueKind() == JsonValueKind.Number)
                    ValidateNumber(field, key, original.GetValue<double>(), value.GetValue<double>());
                merged[key] = value.DeepClone();
            }
        }
        return merged;
    }

    private static bool SameValueType(JsonNode left, JsonNode right) =>
        left.GetValueKind() == right.GetValueKind() ||
        (left.GetValueKind() is JsonValueKind.True or JsonValueKind.False &&
         right.GetValueKind() is JsonValueKind.True or JsonValueKind.False);

    private static void ValidateNumber(string path, string key, double original, double value)
    {
        // Keys numbered within a node have the same constraints as the first parameter of their kind.
        string role = key.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        bool valid = double.IsFinite(value) && Math.Abs(value) <= 120_000;
        if (role.EndsWith("Count", StringComparison.Ordinal) && original > 0)
            valid &= value >= 1 && value <= Math.Max(16, original * 4);
        if (role.EndsWith("Stride", StringComparison.Ordinal) && original > 0)
            valid &= value >= 1 && value <= Math.Max(16, original * 4);
        if (role.EndsWith("Divisor", StringComparison.Ordinal))
            valid &= Math.Abs(value) >= .0001 && Math.Sign(value) == Math.Sign(original);
        if (role is "FadeStart") valid &= value >= 0 && value < 1;
        if (role is "DissipationDuration" or "SmokeReleaseDuration") valid &= value > 0;
        if ((role.EndsWith("DurationMs", StringComparison.Ordinal) || role.EndsWith("IntervalMs", StringComparison.Ordinal) || role is "TravelPerCellMs")
            && original > 0)
            valid &= value >= 1;
        if (!valid) throw new InvalidDataException($"VFX value out of range at {path}.");
    }

    private static void RequireNodeProperties(JsonTypeInfo type)
    {
        if (type.Kind != JsonTypeInfoKind.Object || !type.Type.FullName!.StartsWith(
                typeof(SpellVfxData).FullName + "+", StringComparison.Ordinal)) return;
        foreach (var property in type.Properties) property.IsRequired = true;
    }

    private static T Decode<T>(JsonObject nodes) => nodes.Deserialize<T>(Options);
    private static SpellVfxData Decode(Dictionary<string, JsonObject> nodes)
    {
        var data = new SpellVfxData();
        foreach (var (group, values) in nodes) data = DecodeGroup(data, group, values);
        return data;
    }

    private static SpellVfxData DecodeGroup(SpellVfxData data, string group, JsonObject nodes) => group switch
    {
        "Warrior" => data with { Warrior = Decode<SpellVfxData.WarriorNodes>(nodes) },
        "Wizard" => data with { Wizard = Decode<SpellVfxData.WizardNodes>(nodes) },
        "Taoist" => data with { Taoist = Decode<SpellVfxData.TaoistNodes>(nodes) },
        "Signature" => data with { Signature = Decode<SpellVfxData.SignatureNodes>(nodes) },
        "Legacy" => data with { Legacy = Decode<SpellVfxData.LegacyNodes>(nodes) },
        "Frame" => data with { Frame = Decode<SpellVfxData.FrameNodes>(nodes) },
        "Debris" => data with { Debris = Decode<SpellVfxData.DebrisNodes>(nodes) },
        _ => throw new ArgumentOutOfRangeException(nameof(group))
    };

    private sealed record Snapshot(SpellVfxData Defaults, Dictionary<Spell, SpellVfxData> Spells);

    // Effect generation runs synchronously. A scope pins one snapshot through nested effects,
    // so a studio reload cannot mix old and new parameters in one emission.
    internal static Scope For(Spell spell) => new(spell);

    internal static int CueDuration(Spell spell, Func<SpellVfxData.LegacyNodes.CueTimingNode, int> select)
    {
        using var scope = For(spell);
        return select(Current.Legacy.CueTiming);
    }

    internal readonly struct Scope : IDisposable
    {
        private readonly Snapshot _previousFrame;
        private readonly SpellVfxData _previous;
        private readonly Spell _previousSpell;

        internal Scope(Spell spell)
        {
            (_previousFrame, _previous, _previousSpell) = (_frame, _scoped, _spell);
            if (_frame != null && spell == _spell) return;
            _frame ??= Volatile.Read(ref _active);
            _scoped = _frame.Spells.TryGetValue(spell, out var data) ? data : _frame.Defaults;
            _spell = spell;
        }

        public void Dispose() => (_frame, _scoped, _spell) = (_previousFrame, _previous, _previousSpell);
    }
}

internal sealed class VfxRgbConverter : JsonConverter<int>
{
    internal static int Parse(string text) => text is { Length: 7 } && text[0] == '#' &&
        int.TryParse(text.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int rgb)
        ? rgb : throw new JsonException("Expected a colour in #RRGGBB format.");

    public override int Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
        writer.WriteStringValue($"#{value:X6}");
}

internal sealed class VfxColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        Color.FromArgb(unchecked((int)0xff000000) | VfxRgbConverter.Parse(reader.GetString()));

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
        writer.WriteStringValue($"#{value.ToArgb() & 0xffffff:X6}");
}

internal sealed class VfxCurveConverter : JsonConverter<SpellParticleColourCurve>
{
    public override SpellParticleColourCurve Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var keys = document.RootElement;
        if (keys.ValueKind != JsonValueKind.Array || keys.GetArrayLength() is < 1 or > 32)
            throw new JsonException("Expected 1 to 32 colour stops.");
        var stops = new (float, int)[keys.GetArrayLength()];
        int index = 0;
        foreach (var key in keys.EnumerateArray())
        {
            if (key.ValueKind != JsonValueKind.Object || !key.TryGetProperty("position", out var position) ||
                position.ValueKind != JsonValueKind.Number || !key.TryGetProperty("colour", out var colour) ||
                colour.ValueKind != JsonValueKind.String)
                throw new JsonException("Each colour stop needs a position and a #RRGGBB colour.");
            stops[index++] = (position.GetSingle(), VfxRgbConverter.Parse(colour.GetString()));
        }
        try { return new SpellParticleColourCurve(stops); }
        catch (ArgumentException error) { throw new JsonException(error.Message, error); }
    }

    public override void Write(Utf8JsonWriter writer, SpellParticleColourCurve value, JsonSerializerOptions options) =>
        throw new NotSupportedException("Save colour stops from their source document.");
}
