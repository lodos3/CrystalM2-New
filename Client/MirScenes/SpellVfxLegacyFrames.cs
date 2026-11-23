using System.Numerics;

namespace Client.MirScenes;

// Measurements from the legacy additive sprites. Each row is one facing in library order.
// Core positions use the intensity-weighted centre (fourth power of the brightest channel)
// relative to the default projectile anchor. The game needs no spell library to play them.
internal static class SpellVfxLegacyFrames
{
    // Centre of the faint blade glint in the first Magic #3480 pose.
    internal static readonly Vector2[] FlameIgnition =
    [
        new(36, -77), new(31, -57), new(8, -45), new(-20, -48),
        new(-37, -66), new(-32, -83), new(-10, -95), new(20, -94)
    ];
    internal static Vector2 ProjectileOffset(Spell spell, int direction, int pose)
    {
        if ((uint)direction >= 16 || pose < 0) return Vector2.Zero;
        var offsets = spell switch
        {
            Spell.FireBall => Fireball,
            Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower => GreatFireball,
            Spell.FrostCrunch => FrostCrunch,
            Spell.SoulFireBall or Spell.SoulShield or Spell.BlessedArmour or Spell.MassHiding or
                Spell.Curse or Spell.Hallucination or Spell.Plague or Spell.PoisonCloud => Talisman,
            Spell.CatTongue => CatTongue,
            _ => null
        };
        if (offsets == null) return Vector2.Zero;
        int count = offsets.Length / 16;
        var point = offsets[direction * count + pose % count];
        return new Vector2(point.X, point.Y);
    }

    internal static float BladeStrength(Spell spell, SpellVisualPhase phase, int index)
    {
        if (phase is not (SpellVisualPhase.Swing or SpellVisualPhase.FollowThrough)) return 1;
        var strengths = spell == Spell.FlamingSword ? Flame : phase == SpellVisualPhase.FollowThrough ? DrakeFollow : Drake;
        return strengths[index] / 100f;
    }

    // Magic #10, direction stride 10.
    private static readonly (sbyte X, sbyte Y)[] Fireball =
    [
        (-1, 1), (0, 3), (-1, 3), (-1, 2), (-1, 2), (-1, 3),
        (-1, 3), (-1, 3), (-1, 3), (-1, 3), (-1, 3), (0, 3),
        (1, 3), (2, 2), (0, 4), (1, 3), (0, 3), (2, 3),
        (-1, 0), (-1, 0), (-2, 1), (-1, 0), (-1, 0), (-1, 0),
        (-1, 0), (-1, 0), (-1, 0), (-1, 0), (-2, 0), (-1, 0),
        (0, 1), (0, 1), (0, 1), (0, 1), (1, 1), (0, 1),
        (1, 0), (1, -1), (0, -1), (0, -1), (1, 0), (0, -1),
        (2, -2), (2, -1), (2, -2), (2, -1), (2, -1), (2, 0),
        (1, -1), (0, -1), (0, -1), (1, -1), (0, -1), (1, -1),
        (0, -5), (0, -4), (0, -5), (0, -4), (0, -4), (0, -3),
        (-3, -6), (-3, -7), (-2, -7), (-2, -7), (-3, -6), (-2, -7),
        (-1, -4), (-1, -4), (-1, -4), (-1, -4), (-2, -4), (-1, -4),
        (0, -1), (0, -1), (0, -1), (0, -1), (1, -1), (0, -1),
        (1, -4), (1, -4), (2, -3), (1, -4), (1, -4), (1, -4),
        (-2, -2), (-3, -3), (-1, -1), (-2, -2), (-1, -2), (-3, -2),
        (-3, 0), (-3, 0), (-4, -1), (-3, -1), (-3, -1), (-4, 0),
    ];

    // Magic #410, direction stride 10.
    private static readonly (sbyte X, sbyte Y)[] GreatFireball =
    [
        (0, -4), (0, -4), (0, -4), (0, -4), (0, -4), (0, -4),
        (4, -4), (3, -3), (5, -3), (4, -4), (3, -3), (5, -3),
        (3, -2), (0, -3), (2, -3), (3, -2), (0, -3), (2, -3),
        (1, -1), (0, -2), (1, -1), (1, -1), (0, -2), (1, -1),
        (3, 0), (4, -1), (4, 0), (3, 0), (4, -1), (4, 0),
        (2, 0), (2, 0), (2, 2), (2, 0), (2, 0), (2, 2),
        (4, 2), (4, 1), (3, 1), (4, 2), (4, 1), (3, 1),
        (0, 0), (1, 1), (-1, 1), (0, 0), (1, 1), (-1, 1),
        (-1, 1), (0, 0), (-1, 0), (-1, 1), (0, 0), (-1, 0),
        (-2, 0), (-2, 1), (-1, 1), (-2, 0), (-2, 1), (-1, 1),
        (-1, 2), (0, 1), (0, 1), (-1, 2), (0, 1), (0, 1),
        (-2, -1), (-2, -1), (-2, 0), (-2, -1), (-2, -1), (-2, 0),
        (0, -1), (-1, -1), (-1, 1), (0, -1), (-1, -1), (-1, 1),
        (-2, -4), (-1, -5), (-1, -3), (-2, -4), (-1, -5), (-1, -3),
        (-4, -3), (-2, -4), (-3, -4), (-4, -3), (-2, -4), (-3, -4),
        (-2, -4), (-2, -4), (-3, -4), (-2, -4), (-2, -4), (-3, -4),
    ];

    // Magic2 #410, direction stride 10.
    private static readonly (sbyte X, sbyte Y)[] FrostCrunch =
    [
        (1, -25), (1, -24), (0, -23), (0, -18),
        (12, -18), (11, -18), (11, -17), (8, -12),
        (14, -15), (13, -14), (11, -13), (7, -10),
        (15, -10), (13, -10), (11, -10), (6, -8),
        (16, -5), (14, -5), (12, -5), (5, -6),
        (13, 3), (12, 2), (10, 1), (5, -2),
        (9, 9), (9, 9), (7, 8), (4, 5),
        (3, 13), (3, 13), (2, 10), (0, 6),
        (-3, 15), (-3, 14), (-3, 12), (-3, 7),
        (-12, 9), (-12, 9), (-10, 8), (-7, 4),
        (-17, 5), (-17, 5), (-15, 4), (-11, 2),
        (-20, -1), (-20, -1), (-18, -1), (-13, -2),
        (-21, -5), (-21, -5), (-19, -5), (-15, -5),
        (-21, -14), (-21, -13), (-19, -12), (-14, -11),
        (-14, -18), (-14, -18), (-13, -17), (-10, -14),
        (-7, -23), (-7, -23), (-7, -21), (-4, -17),
    ];

    // Magic #1160, direction stride 10.
    private static readonly (sbyte X, sbyte Y)[] Talisman =
    [
        (-1, -1), (-1, 1), (-1, -2),
        (-1, -1), (-1, 1), (0, -2),
        (-1, -1), (-1, 0), (0, -2),
        (-1, 0), (-1, 0), (0, -1),
        (-2, 0), (-1, 0), (-1, 0),
        (-3, 0), (-1, 0), (-1, 0),
        (-3, 0), (-2, 0), (-1, 0),
        (-3, -1), (-3, -6), (-2, -1),
        (-3, -2), (-2, -1), (-2, -1),
        (-2, -2), (-2, 0), (-2, -2),
        (-2, -2), (-2, 0), (-2, -2),
        (-1, -2), (-3, 0), (-1, -2),
        (-1, -1), (-4, 0), (-1, -2),
        (-2, 0), (-4, -1), (-2, -1),
        (-2, 0), (-3, -1), (-3, -1),
        (-2, -1), (-2, 0), (-2, -2),
    ];

    // Magic3 #260, direction stride 10.
    private static readonly (sbyte X, sbyte Y)[] CatTongue =
    [
        (-2, -28), (-1, -28), (-1, -27), (-2, -28), (-1, -28), (0, -28),
        (-3, -27), (-3, -27), (-6, -25), (-3, -27), (-3, -27), (-5, -25),
        (-3, -29), (-5, -29), (-7, -28), (-3, -29), (-5, -29), (-7, -28),
        (-5, -29), (-5, -29), (-5, -29), (-5, -29), (-5, -29), (-5, -29),
        (-5, -30), (-5, -32), (-6, -31), (-5, -30), (-5, -32), (-6, -31),
        (-8, -32), (-7, -33), (-6, -32), (-8, -33), (-6, -34), (-6, -32),
        (-4, -31), (-3, -31), (-3, -33), (-3, -32), (-3, -31), (-3, -33),
        (-4, -36), (-5, -36), (-4, -38), (-2, -33), (-4, -37), (-4, -38),
        (-2, -31), (-1, -33), (0, -33), (-2, -31), (-1, -34), (-1, -34),
        (0, -34), (2, -38), (1, -34), (1, -35), (2, -38), (1, -35),
        (0, -31), (3, -35), (2, -33), (1, -31), (3, -35), (2, -34),
        (6, -32), (5, -33), (6, -33), (6, -32), (5, -33), (6, -34),
        (1, -30), (4, -31), (1, -32), (1, -30), (4, -31), (1, -33),
        (8, -28), (5, -29), (7, -30), (8, -28), (5, -30), (7, -31),
        (2, -28), (3, -29), (3, -30), (2, -28), (2, -29), (3, -30),
        (3, -25), (2, -27), (4, -26), (2, -25), (2, -27), (4, -26),
    ];

    // Relative emitted light within each six-pose swing, compressed for additive particles.
    // Magic #3480, direction stride 10.
    private static readonly byte[] Flame =
    [
        30, 78, 80, 85, 100, 78,
        30, 95, 100, 100, 100, 86,
        30, 88, 86, 92, 100, 77,
        30, 99, 83, 97, 100, 84,
        30, 100, 80, 99, 73, 89,
        30, 96, 78, 98, 100, 88,
        30, 82, 71, 91, 100, 80,
        30, 71, 72, 87, 100, 77,
    ];

    // Magic2 #220, direction stride 20.
    private static readonly byte[] Drake =
    [
        40, 90, 81, 90, 100, 85,
        46, 79, 76, 71, 100, 76,
        48, 78, 58, 62, 100, 64,
        41, 84, 55, 66, 100, 73,
        53, 78, 55, 78, 100, 91,
        53, 71, 57, 72, 94, 100,
        53, 74, 63, 78, 94, 100,
        59, 80, 78, 86, 97, 100,
    ];

    // Magic2 #226, direction stride 20.
    private static readonly byte[] DrakeFollow =
    [
        95, 100, 82, 88, 89, 51,
        92, 100, 80, 80, 77, 51,
        94, 100, 77, 78, 87, 50,
        96, 100, 76, 83, 87, 53,
        96, 100, 76, 83, 87, 57,
        98, 100, 79, 86, 93, 61,
        99, 100, 80, 89, 89, 60,
        96, 100, 81, 87, 87, 58,
    ];
}
