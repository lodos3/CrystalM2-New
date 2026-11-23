namespace Client.MirScenes;

internal enum BladeSpriteLibrary { Magic, Magic2 }

internal readonly record struct BladePoseCue(
    BladeSpriteLibrary Library, int FirstFrame, int DirectionStride, int FrameCount, int Duration)
{
    public int FrameInterval => Duration / FrameCount;

    public int FrameIndex(MirDirection direction, int frame) =>
        FirstFrame + DirectionStride * (int)direction + frame;

    public int FrameAt(int elapsed) => Math.Clamp(elapsed / FrameInterval, 0, FrameCount - 1);
}

internal static class SpellVfxBladeFrames
{
    public const int DrakeStrikeInterval = 50;
    public const int DrakeFollowThroughInterval = 45;

    public static bool Supports(Spell spell) => spell is Spell.FlamingSword or Spell.TwinDrakeBlade;

    public static readonly BladePoseCue DrakePrepare = new(BladeSpriteLibrary.Magic2, 210, 0, 6, 500);
    public static readonly BladePoseCue DrakeStun = new(BladeSpriteLibrary.Magic2, 380, 0, 6, 800);

    public static BladePoseCue? Attack(Spell spell, MirAction action) => (spell, action) switch
    {
        (Spell.FlamingSword, MirAction.Attack1 or MirAction.Attack4) => new(BladeSpriteLibrary.Magic, 3480, 10, 6, 600),
        (Spell.TwinDrakeBlade, MirAction.Attack1) => new(BladeSpriteLibrary.Magic2, 220, 20, 6, 6 * DrakeStrikeInterval),
        (Spell.TwinDrakeBlade, MirAction.Attack4) => new(BladeSpriteLibrary.Magic2, 226, 20, 6, 6 * DrakeFollowThroughInterval),
        _ => null
    };
}
