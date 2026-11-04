global using Client.Rendering;
global using System.Numerics;
global using Matrix = System.Numerics.Matrix4x4;

namespace Client.Rendering;

public enum SpriteBlendMode { Alpha, Additive, InverseLight, Multiply }
public enum ShaderEffect { Artwork, Normal, Grayscale, Magic, Atmosphere, BloomExtract, BloomBlur }
