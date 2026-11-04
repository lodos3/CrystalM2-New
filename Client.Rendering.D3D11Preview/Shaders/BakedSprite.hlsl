// Direct3D 11 / Shader Model 4, intentionally matching Mir's baked art.
// No automatic normals, diffuse lighting, RGB checker repair or new shadows.
// Source .Lib BGRA is uploaded byte-for-byte, sampled with PointClamp and
// composited as straight (NON-premultiplied) alpha.
cbuffer Frame : register(b0)
{
    float2 ViewportSize;
    float2 Padding;
};
Texture2D SpriteTexture : register(t0);
SamplerState SpriteSampler : register(s0);

struct SpriteInput
{
    float2 Position : POSITION;
    float2 Uv : TEXCOORD0;
    float4 Tint : COLOR0;
};

struct SpriteOutput
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
    float4 Tint : COLOR0;
};

SpriteOutput VSMain(SpriteInput input)
{
    SpriteOutput output;
    output.Position = float4(input.Position.x * (2.0 / ViewportSize.x) - 1.0,
                             1.0 - input.Position.y * (2.0 / ViewportSize.y),
                             0.0, 1.0);
    output.Uv = input.Uv;
    output.Tint = input.Tint;
    return output;
}

float4 PSMain(SpriteOutput input) : SV_TARGET
{
    return SpriteTexture.Sample(SpriteSampler, input.Uv) * input.Tint;
}
