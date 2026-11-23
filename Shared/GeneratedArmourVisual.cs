using System;

public static class GeneratedArmourVisual
{
    public const byte MaxId = 99;

    // The legacy packet field is a signed short. Ordinary equipped armour shapes
    // remain nonnegative; -1 through -99 are reserved for visual-only GArmour overlays.
    // This changes no packet lengths and never changes ItemInfo.Shape or combat stats.
    public static short Encode(short equippedArmour, byte generatedId)
    {
        if (generatedId == 0) return equippedArmour;
        if (generatedId > MaxId) throw new ArgumentOutOfRangeException(nameof(generatedId));
        return checked((short)-generatedId);
    }

    public static void Decode(short transmitted, out short baseArmour, out byte generatedId)
    {
        if (transmitted < 0 && transmitted >= -MaxId)
        {
            baseArmour = 0;
            generatedId = checked((byte)-transmitted);
            return;
        }

        // Invalid negative values are not legitimate generated IDs.
        baseArmour = (short)Math.Max(0, transmitted);
        generatedId = 0;
    }

    public static bool TryParseId(string? name, out byte id)
    {
        id = 0;
        return name is { Length: 3 } &&
               (name[0] == 'g' || name[0] == 'G') &&
               name[1] is >= '0' and <= '9' &&
               name[2] is >= '0' and <= '9' &&
               byte.TryParse(name.AsSpan(1, 2), out id) && id > 0 && id <= MaxId;
    }
}
