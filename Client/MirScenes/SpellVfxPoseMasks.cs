using System.Numerics;
using CrystalM2.Rendering.Core;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;

namespace Client.MirScenes;

// Directional effects are constructed from geometry. No legacy sprite plane,
// sprite decompression, or per-pose GPU texture is needed to render them.
internal static class SpellVfxPoseMasks
{
    internal static bool IsWeaponSwing(Spell spell) =>
        spell is Spell.Slaying or Spell.Thrusting or Spell.HalfMoon or Spell.CrossHalfMoon;

    internal static int Facing(Vector2 heading, int directions)
    {
        float turn = MathF.Atan2(heading.X, -heading.Y / SpellParticleFrame.GroundAspect);
        return ((int)MathF.Round(turn * directions / MathF.Tau) + directions) % directions;
    }

    internal static void DrawSwing(SpellParticleFrame frame, Spell spell, Vector2 feet,
        int direction, float progress)
    {
        float heading = (direction - 2) * MathF.PI / 4;
        float t = Math.Clamp(progress, 0, 1);
        float fade = .8f * MathF.Sin(MathF.PI * Math.Clamp(t + .12f, 0, 1));
        Vector2 centre = feet - Vector2.UnitY * 23;
        Vector2 forward = new(MathF.Cos(heading), MathF.Sin(heading) * .68f);
        if (spell == Spell.Thrusting)
        {
            Vector2 tip = centre + forward * (27 + 29 * t);
            frame.Thread(centre + forward * 5, tip, 3.3f, .6f, 0x94C9FF, fade, emitter: "Procedural.Swing.Thrust");
            frame.Thread(centre + forward * 13, tip, 1.2f, .3f, 0xF2FAFF, fade, Layer.EmissiveCore, emitter: "Procedural.Swing.ThrustCore");
            return;
        }
        float span = spell == Spell.CrossHalfMoon ? MathF.PI * 1.75f :
            spell == Spell.HalfMoon ? MathF.PI * 1.1f : MathF.PI * .67f;
        float radius = spell == Spell.CrossHalfMoon ? 47 : spell == Spell.HalfMoon ? 40 : 29;
        float leading = heading + span * (t - .42f);
        float trail = span * (.32f + .44f * t);
        const int segments = 9;
        Vector2 previous = centre + SpellParticleFrame.Ellipse(leading - trail, radius, radius * .68f);
        for (int i = 1; i <= segments; i++)
        {
            float u = i / (float)segments;
            Vector2 point = centre + SpellParticleFrame.Ellipse(leading - trail + trail * u, radius, radius * .68f);
            frame.Thread(previous, point, 2.2f * u, 3.1f * u, 0x81ABEB, fade * u,
                emitter: "Procedural.Swing.Wake");
            frame.Thread(previous, point, .7f * u, 1f * u, 0xF0F8FF, fade * u, Layer.EmissiveCore,
                emitter: "Procedural.Swing.Core");
            previous = point;
        }
        frame.Sprite(previous, 10, 8, Shape.Flash, 0xD6EEFF, fade, Layer.EmissiveCore,
            light: true, emitter: "Procedural.Swing.Tip");
    }
}
