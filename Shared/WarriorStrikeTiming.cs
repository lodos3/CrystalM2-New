/// <summary>Shared timing contract for travelling warrior attacks, measured from the attack/cast.</summary>
public static class WarriorStrikeTiming
{
    public const int ThrustDuration = 600;
    public const int ThrustTravel = 240;
    public const int AvalancheDuration = 1000;
    public const int AvalancheFirstRise = 35;
    public const int AvalancheStepInterval = 45;
    public const int DrakeImpact = 300;

    public static int ThrustImpact(int distance) => 300 + Math.Max(0, distance - 1) * 75;
    public static int AvalancheImpact(int row) => 180 + Math.Max(0, row) * 90;

    public static bool SuppressTargetArt(Spell spell) =>
        spell is Spell.CrossHalfMoon or Spell.Thrusting or Spell.BladeAvalanche;

    // Preserve the two blades' nominal combined power. Third Fang contributes
    // its original percentage of one blade to the same hit, not another hit.
    public static int DrakeDamage(int oneBlade, int echoRank)
    {
        long blade = Math.Max(0, oneBlade);
        long echo = echoRank > 0 ? blade * (25 + 10 * Math.Clamp(echoRank, 0, 2)) / 100 : 0;
        return (int)Math.Min(int.MaxValue, blade * 2 + echo);
    }
}
