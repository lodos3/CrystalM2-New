// Appearance packets already carry a byte. Keep 1 and 2 as the original armour wing effects.
public static class ArmourWings
{
    public const byte Phoenix = 110, Frost = 111, Moth = 112, CelestialElectric = 113, FelFire = 114, DemonicBone = 115;
    public const byte Archangel = 116, DawnSeraph = 117;
    public static bool IsParticle(byte effect) => effect is 1 or 2 or >= Phoenix and <= DawnSeraph;
    public const string Choices = "0 Silver, 1 Gold, 2 Phoenix, 3 Frost, 4 Moon Moth, 5 Celestial Electric, 6 Fel Fire, 7 Demonic Bone, 8 Archangel, 9 Dawn Seraph, off, auto";
    public static bool TrySelect(string value, out byte? effect)
    {
        effect = value.ToLowerInvariant() switch
        {
            "0" => 1, "1" => 2, "2" => Phoenix, "3" => Frost, "4" => Moth, "5" => CelestialElectric,
            "6" => FelFire, "7" => DemonicBone, "8" => Archangel, "9" => DawnSeraph,
            "off" => 0, _ => null
        };
        return effect.HasValue || string.Equals(value, "auto", System.StringComparison.OrdinalIgnoreCase);
    }
}
