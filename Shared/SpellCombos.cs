public sealed record SpellCombo(Spell Spender, string Name, byte Capacity, ushort Duration, ushort BuildInterval,
    bool TargetBound, string Rotation);

public static class SpellCombos
{
    public static SpellCombo For(Spell spender) => spender switch
    {
        Spell.Notchbreaker => Opening,
        Spell.RedWake => Momentum,
        Spell.Interpose => Guard,
        Spell.AshSeal => Embers,
        Spell.GlacialGate => Rime,
        Spell.StormRelay => Charge,
        Spell.LanternLink => Reserve,
        Spell.Distil => Ink,
        Spell.SpiritCommand => Bond,
        _ => null
    };

    private static readonly SpellCombo Opening = new(Spell.Notchbreaker, "Opening", 1, 6500, 0, true,
        "Land Thrusting at two tiles or land Flaming Sword. Notchbreaker exploits the opening on that enemy.");
    private static readonly SpellCombo Momentum = new(Spell.RedWake, "Momentum", 2, 8000, 450, false,
        "Land Half Moon or Cross Half Moon. Red Wake spends Momentum to strengthen its secondary hits.");
    private static readonly SpellCombo Guard = new(Spell.Interpose, "Guard", 2, 8000, 1200, false,
        "Mitigate a hit while Protection Field or Immortal Skin is active. Interpose spends Guard on a stronger ward.");
    private static readonly SpellCombo Embers = new(Spell.AshSeal, "Embers", 2, 8000, 1000, false,
        "Land Great Fire Ball or burn an enemy with Fire Wall. Ash Seal spends Embers on its first successful pulse.");
    private static readonly SpellCombo Rime = new(Spell.GlacialGate, "Rime", 1, 6500, 0, true,
        "Land Frost Crunch to chill an enemy. Ice Thrust or Glacial Gate consumes Rime for extra damage.");
    private static readonly SpellCombo Charge = new(Spell.StormRelay, "Charge", 2, 8000, 850, false,
        "Land Thunder Bolt or Lightning. Storm Relay spends Charge when it finds a second target, strengthening its bounces.");
    private static readonly SpellCombo Reserve = new(Spell.LanternLink, "Healing Reserve", 2, 10000, 900, false,
        "Restore missing health with Healing, Mass Healing or Healing Circle. Lantern Link converts the reserve into absorption.");
    private static readonly SpellCombo Ink = new(Spell.Distil, "Fresh Ink", 1, 6500, 850, true,
        "Apply your poison with Poisoning or Poison Cloud. Distil consumes your poison and deals extra damage while Fresh Ink is active.");
    private static readonly SpellCombo Bond = new(Spell.SpiritCommand, "Spirit Bond", 2, 8000, 850, false,
        "Land Soul Fire Ball or Spirit Spark with a spirit present, or cast Pet Enhancer on it. Spirit Command spends Bond on a stronger mend.");
}

// Transient actor state. Marks replace each other when the builder changes target.
public struct SpellComboProgress
{
    private byte _stacks;
    private uint _targetId;
    private long _expires, _nextBuild;

    public readonly uint TargetId => _targetId;
    public readonly long Expires => _expires;

    public readonly byte Count(long now, uint targetId = 0) =>
        now < _expires && (_targetId == 0 || targetId == _targetId) ? _stacks : (byte)0;

    public bool Build(SpellCombo combo, long now, uint targetId)
    {
        if (now < _nextBuild || (combo.TargetBound && targetId == 0)) return false;
        uint key = combo.TargetBound ? targetId : 0;
        _stacks = (byte)Math.Min(combo.Capacity, Count(now, key) + 1);
        _targetId = key;
        _expires = now + combo.Duration;
        _nextBuild = now + combo.BuildInterval;
        return true;
    }

    public bool Spend(long now, uint targetId, int amount = int.MaxValue)
    {
        if (Count(now, targetId) == 0 || amount <= 0) return false;
        if (amount < _stacks)
        {
            _stacks -= (byte)amount;
            return true;
        }
        _stacks = 0;
        _targetId = 0;
        _expires = 0;
        return true;
    }
}
