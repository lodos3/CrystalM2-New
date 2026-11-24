// Appearance packets already carry a byte. Keep 1 and 2 as the original armour wing effects.
public static class ArmourWings
{
    public const byte Phoenix = 110, Frost = 111, Moth = 112, CelestialElectric = 113, FelFire = 114, DemonicBone = 115;
    public const byte Archangel = 116, DawnSeraph = 117;
    public const byte MagmaWyrm = 118, TidalLeviathan = 119, GeodeTitan = 120, Tempest = 121,
        VerdantMonarch = 122, Thunderstorm = 123, Venom = 124, SolarAscendant = 125,
        Eclipse = 126, AstralConvergence = 127;
    public static bool IsElemental(byte effect) => effect is >= MagmaWyrm and <= AstralConvergence;
    public static bool IsAura(byte effect) => effect is >= SolarAscendant and <= AstralConvergence;
    public static bool IsParticle(byte effect) => effect is 1 or 2 or >= Phoenix and <= AstralConvergence;
    public const string Choices = "0 Silver, 1 Gold, 2 Phoenix, 3 Frost, 4 Moon Moth, 5 Celestial Electric, 6 Fel Fire, 7 Demonic Bone, 8 Archangel, 9 Dawn Seraph, 10 Magma Wyrm, 11 Tidal Leviathan, 12 Geode Titan, 13 Tempest, 14 Verdant Monarch, 15 Thunderstorm, 16 Venom, 17 Solar Ascendant, 18 Eclipse, 19 Astral Convergence, off, auto";
    public static bool TrySelect(string value, out byte? effect)
    {
        effect = value.ToLowerInvariant() switch
        {
            "0" => 1, "1" => 2, "2" => Phoenix, "3" => Frost, "4" => Moth, "5" => CelestialElectric,
            "6" => FelFire, "7" => DemonicBone, "8" => Archangel, "9" => DawnSeraph,
            "10" => MagmaWyrm, "11" => TidalLeviathan, "12" => GeodeTitan, "13" => Tempest,
            "14" => VerdantMonarch, "15" => Thunderstorm, "16" => Venom, "17" => SolarAscendant,
            "18" => Eclipse, "19" => AstralConvergence,
            "off" => 0, _ => null
        };
        return effect.HasValue || string.Equals(value, "auto", System.StringComparison.OrdinalIgnoreCase);
    }
}
