namespace CrystalM2.ParticleStudio;

internal static class ParticleLayers
{
    private static readonly Dictionary<string, string[]> Roar = new()
    {
        ["Ground ring"] = ["WarriorRing*"],
        ["Roar crest"] = ["WarriorImpact*"],
        ["Glow cloud"] = ["WispWidth", "WispHeight", "WispShape", "WispColour", "WispColour2", "WispAlpha", "WispLayer"],
        ["Core flash"] = ["Flash*"],
        ["Radial wisps"] = ["WispCount", "WispWidth2", "WispHeight2", "WispHeight3", "WispShape2", "WispAlpha2"],
        ["Shockwave threads"] = ["Thread*"],
        ["Edge sparks"] = ["Spark*"],
        ["Shared roar settings"] = ["TintColour*", "Spread*", "Head", "Angle*", "Ellipse*", "Ray*", "Bend*"]
    };

    internal static IEnumerable<string?> Names(string group, string node, bool advanced) =>
        !advanced && group == "Warrior" && node == "Roar" ? Roar.Keys : [null];

    internal static bool Includes(string? layer, string key, Spell spell)
    {
        if (layer == null) return true;
        if (key is "WispColour" or "WispColour2" or "TintColour" or "TintColour2")
            if (key.EndsWith('2') == (spell == Spell.BattleCry)) return false;
        return Roar[layer].Any(pattern => pattern.EndsWith('*')
            ? key.StartsWith(pattern[..^1], StringComparison.Ordinal) : key == pattern);
    }

    internal static string? Label(string? layer, string key)
    {
        if (layer == null) return null;
        if (key is "WispHeight2" or "SparkWidth") return key == "SparkWidth" ? "Width" : "Height";
        if (key is "WispColour" or "WispColour2" or "TintColour" or "TintColour2") return "Colour";
        string name = key.TrimEnd('2');
        return new[] { "Width", "Height", "Shape", "Alpha", "Layer", "Light", "Count" }
            .FirstOrDefault(suffix => name.EndsWith(suffix, StringComparison.Ordinal)) switch
        {
            "Shape" => "Sprite",
            "Alpha" => "Opacity",
            "Layer" => "Draw layer",
            "Light" => "Casts light",
            "Count" => "Burst count",
            "Width" when !key.StartsWith("Thread", StringComparison.Ordinal) => "Width",
            "Height" => "Height",
            _ => null
        };
    }

    // Existing authored pairs are stored as base plus variation, or as alpha and fade.
    internal static (string Key, bool Fade)? Pair(string? layer, string key) => (layer, key) switch
    {
        ("Radial wisps", "WispHeight2") => ("WispHeight3", false),
        ("Edge sparks", "SparkWidth") => ("SparkWidth2", false),
        ("Core flash", "FlashAlpha") => ("FlashAlpha2", true),
        _ => null
    };

    internal static bool IsPairComponent(string? layer, string key) => (layer, key) is
        ("Radial wisps", "WispHeight3") or ("Edge sparks", "SparkWidth2") or ("Core flash", "FlashAlpha2");
}
