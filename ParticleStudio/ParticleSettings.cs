using System.Text.RegularExpressions;

namespace CrystalM2.ParticleStudio;

internal static partial class ParticleSettings
{
    private static readonly string[] Modules =
        ["Timing", "Emission", "Shape", "Velocity", "Size and rotation", "Colour and opacity", "Collision", "Renderer", "Procedural"];

    private static readonly string[] DebrisProfiles = ["FlamingSword", "ThunderBolt", "IceStorm", "Plague"];

    internal static int ModuleOrder(string module) => Array.IndexOf(Modules, module);

    internal static string NodeLabel(string group, string node) => (group, node) switch
    {
        ("Debris", "Emit") => "Impact debris",
        ("Debris", "Palette") => "Debris colour over lifetime",
        (_, "PacketDuration") => $"Stage duration ({group})",
        (_, "Windup") => "Cast timing",
        (_, "DrawFrame") => $"Stage settings ({group})",
        (_, "DrawProjectile") => $"Projectile ({group})",
        _ => $"{Words(group)} / {Words(node)}"
    };

    internal static (string Label, string Module) Describe(string group, string node, string key, Spell spell)
    {
        if (group == "Debris" && node == "Emit")
        {
            string profile = DebrisProfiles
                .FirstOrDefault(prefix => key.StartsWith(prefix, StringComparison.Ordinal)) ?? "";
            if (profile.Length != 0)
            {
                string style = profile switch
                {
                    "FlamingSword" => "Embers", "ThunderBolt" => "Sparks", "IceStorm" => "Frost", _ => "Plague"
                };
                var (label, module) = key[profile.Length..] switch
                {
                    "Shape" => ("Particle sprite", "Renderer"),
                    "Shape2" => ("Landing particle sprite", "Collision"),
                    "Hot" => ("Highlight colour", "Colour and opacity"),
                    "Cool" => ("Base colour", "Colour and opacity"),
                    "Speed" => ("Start speed", "Velocity"),
                    "Lift" => ("Start lift", "Velocity"),
                    _ => ("Light energy", "Renderer")
                };
                return ($"{label} ({style})", module);
            }
            return key switch
            {
                "Count" => ("Burst count", "Emission"),
                "Birth" => ("Spawn interval (ms)", "Emission"),
                "Root" => ("Spawn radius", "Shape"),
                "Root2" => ("Spawn radius variation", "Shape"),
                "Gravity" => ("Gravity", "Velocity"),
                "Height" => ("Start height", "Shape"),
                "Height2" => ("Start height variation", "Shape"),
                "SpellParticleDuration" => ("Lifetime after landing (ms)", "Collision"),
                "SpellParticleSize" => ("Start size", "Size and rotation"),
                "SpellParticleSize2" => ("Start size variation", "Size and rotation"),
                "Particle" => ("Start opacity", "Colour and opacity"),
                "SpellParticleImpactCount" => ("Landing burst count", "Collision"),
                "SpellParticleImpactSize" => ("Landing particle size", "Collision"),
                "SpellParticleImpactDuration" => ("Landing lifetime (ms)", "Collision"),
                "SpellParticleImpactDuration2" => ("Landing lifetime step (ms)", "Collision"),
                "SpellParticleImpactScatter" => ("Landing scatter", "Collision"),
                _ => (Words(key), Module(key))
            };
        }

        string prefixForSpell = spell.ToString();
        string name = key.StartsWith(prefixForSpell, StringComparison.Ordinal) && key.Length > prefixForSpell.Length
            ? key[prefixForSpell.Length..] : key;
        return (Words(name), Module(key));
    }

    private static string Module(string key)
    {
        string name = key.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        if (name.Contains("Impact", StringComparison.Ordinal) && name.StartsWith("SpellParticle")) return "Collision";
        if (name.EndsWith("Count")) return "Emission";
        if (name.EndsWith("DurationMs") || name.EndsWith("Delay") || name.EndsWith("Duration") || name.EndsWith("IntervalMs")) return "Timing";
        if (name.EndsWith("Radius") || name.EndsWith("Offset") || name.EndsWith("Spread")) return "Shape";
        if (name.EndsWith("Speed") || name.EndsWith("Velocity") || name.EndsWith("Gravity") || name.EndsWith("Lift")) return "Velocity";
        if (name.EndsWith("Size") || name.EndsWith("Width") || name.EndsWith("Height") || name.EndsWith("Rotation") || name.EndsWith("Growth")) return "Size and rotation";
        if (name.EndsWith("Colour") || name.EndsWith("Alpha") || name.EndsWith("Opacity") || name.EndsWith("Tint")) return "Colour and opacity";
        if (name.EndsWith("Layer") || name.EndsWith("Shape") || name.EndsWith("Light") || name.EndsWith("Energy")) return "Renderer";
        return "Procedural";
    }

    private static string Words(string value) => Boundary().Replace(value, " ");

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Za-z])(?=[0-9])")]
    private static partial Regex Boundary();
}
