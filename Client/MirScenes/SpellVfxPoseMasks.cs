using System.Numerics;
using CrystalM2.Rendering.Core;

namespace Client.MirScenes;

internal static class SpellVfxPoseMasks
{
    internal static bool IsWeaponSwing(Spell spell) =>
        spell is Spell.Slaying or Spell.Thrusting or Spell.HalfMoon or Spell.CrossHalfMoon;

    internal static int Facing(Vector2 heading, int directions)
    {
        float turn = MathF.Atan2(heading.X, -heading.Y / SpellParticleFrame.GroundAspect);
        return ((int)MathF.Round(turn * directions / MathF.Tau) + directions) % directions;
    }

    internal static void Draw(SpellParticleFrame frame, Spell spell, Vector2 feet, int direction, int pose,
        float opacity = .70f, bool followThrough = false)
    {
        string sequence = spell switch
        {
            Spell.TwinDrakeBlade when followThrough => "TwinDrakeFollow",
            Spell.GreatFireBall or Spell.FireBounce or Spell.MeteorShower => nameof(Spell.GreatFireBall),
            Spell.SoulShield or Spell.BlessedArmour or Spell.MassHiding or Spell.Curse or Spell.Hallucination or
                Spell.Plague or Spell.PoisonCloud => nameof(Spell.SoulFireBall),
            _ => spell.ToString()
        };
        Draw(frame, sequence, feet, direction, pose, opacity);
    }

    internal static void Draw(SpellParticleFrame frame, string sequence, Vector2 feet, int direction, int pose, float opacity)
    {
        var mask = SpellPoseAtlas.GetPose(sequence, direction, pose);
        if (mask.FirstPlane != 0) frame.PoseMask(feet, mask, opacity);
    }

    internal static bool DrawWarriorCast(SpellParticleFrame frame, SpellVisual visual, Vector2 feet, Vector2 facing, float progress)
    {
        (string name, int count) = (visual.Spell, visual.Phase) switch
        {
            (Spell.LionRoar or Spell.BattleCry, SpellVisualPhase.Impact) => ("LionRoar", 20),
            (Spell.Entrapment, SpellVisualPhase.Prepare) => ("Entrapment", 10),
            (Spell.ProtectionField, SpellVisualPhase.Impact) => ("ProtectionField", 10),
            (Spell.Rage, SpellVisualPhase.Impact) => ("Rage", 10),
            (Spell.Fury, SpellVisualPhase.Impact) => ("Fury", 10),
            (Spell.ImmortalSkin, SpellVisualPhase.Impact) => ("ImmortalSkin", 17),
            (Spell.SlashingBurst, SpellVisualPhase.Swing) => ("SlashingBurst", 9),
            (Spell.CounterAttack, SpellVisualPhase.Swing) => ("CounterAttack", 10),
            _ => (null, 0)
        };
        if (count == 0) return false;
        int direction = visual.Spell is Spell.SlashingBurst or Spell.CounterAttack ? Facing(facing, 8) : 0;
        Draw(frame, name, feet, direction, (int)(progress * count), 1);
        if (visual.Spell == Spell.Fury && progress < .8f)
            Draw(frame, "FuryFlash", feet, 0, (int)(progress * 10), 1);
        if (visual.Spell == Spell.ImmortalSkin && progress < .25f)
            Draw(frame, "SkinFlash", feet, 0, (int)(progress * 20), 1);
        if (visual.Spell == Spell.CounterAttack && progress < .2f)
            Draw(frame, "CounterFlash", feet, 0, (int)(progress * 10), 1);
        return true;
    }
}
