global using Client.Rendering;
global using System.Numerics;
global using Matrix = System.Numerics.Matrix4x4;
global using UiSurface = CrystalM2.Rendering.Core.UiSurface;
global using UiVisualState = CrystalM2.Rendering.Core.UiVisualState;
global using UiGlyph = CrystalM2.Rendering.Core.UiGlyph;
global using DarkFantasyPalette = CrystalM2.Rendering.Core.DarkFantasyPalette;

namespace Client.Rendering;

public enum SpriteBlendMode { Alpha, Additive, InverseLight, Multiply }
public enum ShaderEffect { Artwork, Normal, Grayscale, Magic, Atmosphere, BloomExtract, BloomBlur, LightComposite, ShadowMask, ProjectedShadow, ShapedLight }
