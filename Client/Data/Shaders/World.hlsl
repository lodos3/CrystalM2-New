// Baked BGRA art is sampled without sRGB conversion or inferred relighting.
cbuffer Frame : register(b0) { float2 Viewport; float SourcePremultiplied; float FramePad; };
cbuffer Effect : register(b1) { float4 C[6]; };
Texture2D SpriteTexture : register(t0);
SamplerState SpriteSampler : register(s0);
struct Input { float2 Position : POSITION; float2 Uv : TEXCOORD0; float4 Tint : COLOR0; };
struct Output { float4 Position : SV_POSITION; float2 Uv : TEXCOORD0; float4 Tint : COLOR0; };
Output VSMain(Input input)
{
    Output output;
    output.Position = float4(input.Position.x * 2 / Viewport.x - 1,
                             1 - input.Position.y * 2 / Viewport.y, 0, 1);
    output.Uv = input.Uv;
    output.Tint = input.Tint;
    return output;
}
float4 SampleStraight(float2 uv)
{
    float4 pixel = SpriteTexture.Sample(SpriteSampler, uv);
    if (SourcePremultiplied > 0.5)
        pixel.rgb = pixel.a > 0.00001 ? pixel.rgb / pixel.a : 0;
    return pixel;
}
float4 PSArtwork(Output input) : SV_TARGET { return SampleStraight(input.Uv) * input.Tint; }
float4 PSNormal(Output input) : SV_TARGET
{
    float4 pixel = PSArtwork(input);
    pixel.rgb = lerp(pixel.rgb, pixel.rgb * C[1].rgb, C[0].a);
    return pixel;
}
float4 PSGrayscale(Output input) : SV_TARGET
{
    float4 pixel = PSArtwork(input);
    pixel.rgb = dot(pixel.rgb, float3(0.299, 0.587, 0.114));
    return pixel;
}
float4 PSMagic(Output input) : SV_TARGET
{
    float4 pixel = PSArtwork(input);
    pixel.rgb *= C[1].rgb;
    pixel.a *= C[0].a;
    return pixel;
}
// Atmosphere runs only when compositing the world texture, before the HUD.
float4 PSAtmosphere(Output input) : SV_TARGET
{
    float4 pixel = PSArtwork(input);
    float fog = saturate((C[4].y - input.Uv.y) * C[4].z) * C[3].x;
    pixel.rgb = lerp(pixel.rgb, C[3].yzw, fog);
    pixel.rgb = saturate(pixel.rgb + max(max(pixel.r, pixel.g), pixel.b) * C[4].x * C[4].w);
    float2 centred = input.Uv - C[1].xy;
    float vignette = max(1 - dot(centred, centred) * C[0].y, C[0].w);
    pixel.rgb *= C[0].x * vignette;
    pixel.rgb = lerp(pixel.rgb, pixel.rgb * C[2].rgb, C[2].a);
    pixel.rgb = lerp(pixel.rgb, C[5].yzw, C[5].x);
    return pixel;
}
// Bloom receives the light mask, never the baked world artwork.
float4 PSBloomExtract(Output input) : SV_TARGET
{
    float4 pixel = SampleStraight(input.Uv);
    float value = max(max(pixel.r, pixel.g), pixel.b);
    return float4(pixel.rgb * max(value - C[0].x, 0) * C[0].y, 1);
}
float4 PSBloomBlur(Output input) : SV_TARGET
{
    float3 sum = SampleStraight(input.Uv).rgb;
    sum += SampleStraight(input.Uv + C[0].xy).rgb;
    sum += SampleStraight(input.Uv - C[0].xy).rgb;
    sum += SampleStraight(input.Uv + C[0].zw).rgb;
    sum += SampleStraight(input.Uv - C[0].zw).rgb;
    return float4(sum * C[1].x, 1);
}
