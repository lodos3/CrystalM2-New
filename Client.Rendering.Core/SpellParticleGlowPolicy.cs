using System;
using System.Numerics;

namespace CrystalM2.Rendering.Core;

/// <summary>
/// F12 spell VFX compositing controls shared by the coverage-particle renderer.
/// Mode 0: authored; 1: black-keyed; 2: authored + glow; 3: glow only.
/// Coverage particles already have an alpha mask, so black-keying does not
/// change their underlying image. The other mode distinctions still apply.
/// </summary>
public readonly record struct SpellParticleGlowPolicy(int Composite, float Cutoff, float Strength, bool ScreenBlend)
{
    public bool ShowAuthored => Composite != 3;
    public bool ShowGlow => Composite is 2 or 3 && Strength > 0f;

    /// <summary>
    /// Match the legacy spell glow pass's peak-channel threshold and 0.35
    /// feather. The particle atlas contains coverage, not source RGB,
    /// so peak brightness comes from the authored particle tint.
    /// </summary>
    public float GlowAmount(Vector3 colour)
    {
        if (!ShowGlow) return 0f;
        float peak = MathF.Max(colour.X, MathF.Max(colour.Y, colour.Z));
        float t = Math.Clamp((peak - Cutoff) / .35f, 0f, 1f);
        return Strength * t * t * (3f - 2f * t);
    }
}
