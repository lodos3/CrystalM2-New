// Baked BGRA art is sampled without sRGB conversion or inferred relighting.
cbuffer Frame : register(b0)
{
    float2 Viewport;
    float SourcePremultiplied;
    float RenderStyle;
    float4 TextureAdjustment;
};
cbuffer Effect : register(b1) { float4 C[8]; };
Texture2D SpriteTexture : register(t0);
Texture2D AuxiliaryTexture : register(t1);
SamplerState SpriteSampler : register(s0);
SamplerState AuxiliarySampler : register(s1);
struct Input { float2 Position : POSITION; float2 Uv : TEXCOORD0; float4 Tint : COLOR0; float4 Bounds : TEXCOORD1; };
struct Output
{
    float4 Position : SV_POSITION;
    float2 Uv : TEXCOORD0;
    float4 Tint : COLOR0;
    nointerpolation float4 Bounds : TEXCOORD1;
};
Output VSMain(Input input)
{
    Output output;
    output.Position = float4(input.Position.x * 2 / Viewport.x - 1,
                             1 - input.Position.y * 2 / Viewport.y, 0, 1);
    output.Uv = input.Uv;
    output.Tint = input.Tint;
    output.Bounds = input.Bounds;
    return output;
}
float4 SampleStraight(float2 uv)
{
    float4 pixel = SpriteTexture.Sample(SpriteSampler, uv);
    if (SourcePremultiplied > 0.5)
        pixel.rgb = pixel.a > 0.00001 ? pixel.rgb / pixel.a : 0;
    return pixel;
}
float4 LoadPremultiplied(int2 texel, int2 first, int2 last)
{
    float4 pixel = SpriteTexture.Load(int3(clamp(texel, first, last), 0));
    if (SourcePremultiplied < 0.5) pixel.rgb *= pixel.a;
    return pixel;
}

bool Similar(float4 a, float4 b)
{
    float4 delta = abs(a - b);
    return max(max(delta.r, delta.g), max(delta.b, delta.a)) < TextureAdjustment.x;
}

float4 CubicWeights(float t)
{
    float t2 = t * t, t3 = t2 * t;
    return float4(-0.5 * t + t2 - 0.5 * t3, 1 - 2.5 * t2 + 1.5 * t3,
                  0.5 * t + 2 * t2 - 1.5 * t3, -0.5 * t2 + 0.5 * t3);
}

float4 SampleArtwork(Output input)
{
    int filter = (int)RenderStyle / 16;
    float mix = TextureAdjustment.y;
    float detail = SourcePremultiplied > 0.5 ? 0 : TextureAdjustment.z;
    if ((filter == 0 || mix <= 0) && detail <= 0) return SampleStraight(input.Uv);

    uint width, height;
    SpriteTexture.GetDimensions(width, height);
    float2 size = float2(width, height);
    float2 position = input.Uv * size;
    float2 footprint = fwidth(position);
    // Clamp every tap to the source rectangle to keep atlas entries isolated.
    int2 extent = int2(width, height) - 1;
    int2 first = clamp(int2(round(input.Bounds.xy * size)), 0, extent);
    int2 last = clamp(int2(round(input.Bounds.zw * size)) - 1, first, extent);
    int2 nearest = int2(floor(position));
    float4 pixel;
    if (filter == 0 || mix <= 0)
        pixel = LoadPremultiplied(nearest, first, last);
    else if ((filter == 2 || filter == 3) && max(footprint.x, footprint.y) < 1.01 &&
        (filter == 3 || min(footprint.x, footprint.y) < 0.99))
    {
        int2 texel = nearest;
        float2 quadrant = frac(position);
        float4 left = LoadPremultiplied(texel + int2(-1, 0), first, last);
        float4 right = LoadPremultiplied(texel + int2(1, 0), first, last);
        float4 top = LoadPremultiplied(texel + int2(0, -1), first, last);
        float4 bottom = LoadPremultiplied(texel + int2(0, 1), first, last);
        pixel = LoadPremultiplied(texel, first, last);
        if (!Similar(left, right) && !Similar(top, bottom))
        {
            float4 h = quadrant.x < 0.5 ? left : right;
            float4 v = quadrant.y < 0.5 ? top : bottom;
            if (filter == 3 && min(footprint.x, footprint.y) > 0.99)
            {
                if (SourcePremultiplied > 0.5) return SampleStraight(input.Uv);
                // Resolve the four virtual 2x samples at native size.
                float4 a = Similar(left, top) ? (left + top) * 0.5 : pixel;
                float4 b = Similar(right, top) ? (right + top) * 0.5 : pixel;
                float4 c = Similar(left, bottom) ? (left + bottom) * 0.5 : pixel;
                float4 d = Similar(right, bottom) ? (right + bottom) * 0.5 : pixel;
                pixel = (a + b + c + d) * 0.25;
            }
            else if (Similar(h, v)) pixel = (h + v) * 0.5;
        }
    }
    else
    {
        float2 fromCentre = position - 0.5;
        int2 texel = int2(floor(fromCentre));
        float2 fraction = frac(fromCentre);
        if (max(fraction.x, fraction.y) < 0.001 && detail <= 0) return SampleStraight(input.Uv);
        if (filter == 4 && max(fraction.x, fraction.y) >= 0.001)
        {
            float4 wx = CubicWeights(fraction.x), wy = CubicWeights(fraction.y);
            pixel = 0;
            [unroll] for (int y = 0; y < 4; y++)
            [unroll] for (int x = 0; x < 4; x++)
                pixel += wx[x] * wy[y] * LoadPremultiplied(texel + int2(x - 1, y - 1), first, last);
            pixel.a = saturate(pixel.a);
            pixel.rgb = clamp(pixel.rgb, 0, pixel.a);
        }
        else
        {
            float4 top = lerp(LoadPremultiplied(texel, first, last),
                              LoadPremultiplied(texel + int2(1, 0), first, last), fraction.x);
            float4 bottom = lerp(LoadPremultiplied(texel + int2(0, 1), first, last),
                                 LoadPremultiplied(texel + int2(1, 1), first, last), fraction.x);
            pixel = lerp(top, bottom, fraction.y);
        }
    }
    if (mix < 1 && mix > 0 && filter != 0)
        pixel = lerp(LoadPremultiplied(nearest, first, last), pixel, mix);
    pixel.rgb = pixel.a > 0.00001 ? pixel.rgb / pixel.a : 0;
    if (detail > 0 && pixel.a > 0)
    {
        float4 surround = LoadPremultiplied(nearest + int2(-1, 0), first, last)
                        + LoadPremultiplied(nearest + int2(1, 0), first, last)
                        + LoadPremultiplied(nearest + int2(0, -1), first, last)
                        + LoadPremultiplied(nearest + int2(0, 1), first, last);
        if (surround.a > 0.001)
            pixel.rgb = saturate(pixel.rgb + detail * 0.45 * (pixel.rgb - surround.rgb / surround.a));
    }
    return pixel;
}

float4 PrepareBlend(float4 pixel)
{
    int blend = (int)RenderStyle % 16;
    if (blend == 4 || blend == 7) pixel.rgb *= pixel.a; // Screen and channel maximum.
    if (blend == 5 || blend == 8) pixel.rgb = lerp(1, pixel.rgb, pixel.a); // Multiply and channel minimum.
    return pixel;
}

float4 PSArtwork(Output input) : SV_TARGET { return PrepareBlend(SampleArtwork(input) * input.Tint); }
float4 PSNormal(Output input) : SV_TARGET
{
    float4 pixel = SampleArtwork(input) * input.Tint;
    pixel.rgb = lerp(pixel.rgb, pixel.rgb * C[1].rgb, C[0].a);
    return PrepareBlend(pixel);
}
float4 PSGrayscale(Output input) : SV_TARGET
{
    float4 pixel = SampleArtwork(input) * input.Tint;
    pixel.rgb = dot(pixel.rgb, float3(0.299, 0.587, 0.114));
    return PrepareBlend(pixel);
}
float4 PSMagic(Output input) : SV_TARGET
{
    float4 pixel = SampleArtwork(input) * input.Tint;
    pixel.rgb *= C[1].rgb;
    pixel.a *= C[0].a;
    return PrepareBlend(pixel);
}
float4 PSSpellArtwork(Output input) : SV_TARGET
{
    float4 pixel = SampleArtwork(input);
    float peak = max(pixel.r, max(pixel.g, pixel.b));
    if (C[1].x > 0)
        pixel.a *= smoothstep(C[0].x, C[0].x + C[0].y, peak);
    pixel *= input.Tint;
    float luma = dot(pixel.rgb, float3(0.299, 0.587, 0.114));
    pixel.rgb = max(luma + (pixel.rgb - luma) * C[0].w, 0) * C[0].z;
    if (C[1].y > 0)
    {
        pixel.a *= smoothstep(C[1].z, C[1].z + 0.35, peak);
        pixel.rgb *= C[1].w;
    }
    return PrepareBlend(pixel);
}
// The emission texture contains no ambient colour. Add ambient exactly once
// during multiplicative world composition; bloom sees only emitted light.
float4 PSLightComposite(Output input) : SV_TARGET
{
    return float4(saturate(SampleStraight(input.Uv).rgb + C[0].rgb), 1);
}
// A separate repaired alpha mask uses linear filtering. The original sprite
// continues to use point sampling, with its baked RGB and alpha untouched.
float4 PSShadowMask(Output input) : SV_TARGET
{
    return float4(0, 0, 0, SampleStraight(input.Uv).a * input.Tint.a);
}
float ProjectedShadowAlpha(float2 uv)
{
    // The source texture ends at the sprite edges. Treat the padded area as clear.
    return SpriteTexture.Sample(SpriteSampler, uv).a * all(uv >= 0) * all(uv <= 1);
}
float4 PSProjectedShadow(Output input) : SV_TARGET
{
    float2 dx = float2(C[0].x, 0);
    float2 dy = float2(0, C[0].y);
    float2 uv = input.Uv;
    float alpha = 4 * ProjectedShadowAlpha(uv);
    alpha += 2 * (ProjectedShadowAlpha(uv - dx) + ProjectedShadowAlpha(uv + dx) +
                  ProjectedShadowAlpha(uv - dy) + ProjectedShadowAlpha(uv + dy));
    alpha += ProjectedShadowAlpha(uv - dx - dy) + ProjectedShadowAlpha(uv + dx - dy) +
             ProjectedShadowAlpha(uv - dx + dy) + ProjectedShadowAlpha(uv + dx + dy);
    return float4(0, 0, 0, alpha * input.Tint.a / 16);
}
float4 PSShapedLight(Output input) : SV_TARGET
{
    float4 pixel = SampleStraight(input.Uv);
    float3 source = C[0].x > 0 ? max(pixel.r, max(pixel.g, pixel.b)).xxx : pixel.rgb;
    return float4(source * pixel.a * input.Tint.rgb * input.Tint.a, 1);
}

float4 PSGaussianLight(Output input) : SV_TARGET
{
    float3 value = SpriteTexture.SampleLevel(SpriteSampler, input.Uv, 0).rgb;
    float weights = 1;
    int radius = min(8, (int)C[0].z);
    [loop] for (int i = 1; i <= radius; i++)
    {
        float w = exp(-i * i * C[0].w);
        float2 stepUv = C[0].xy * i;
        value += w * (SpriteTexture.SampleLevel(SpriteSampler, input.Uv - stepUv, 0).rgb +
            SpriteTexture.SampleLevel(SpriteSampler, input.Uv + stepUv, 0).rgb);
        weights += 2 * w;
    }
    return float4(value / weights, 1);
}

float4 PSConeLight(Output input) : SV_TARGET
{
    float2 ray = input.Uv * 2 - 1;
    float distance = length(ray);
    float direction = dot(ray, C[0].xy) / max(distance, 0.001);
    float beam = lerp(1, smoothstep(C[0].z, C[0].w, direction), smoothstep(0, 0.1, distance));
    float4 pixel = SampleStraight(input.Uv) * input.Tint;
    pixel.a *= beam;
    return pixel;
}
float WeatherNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    float4 h = frac(sin(dot(i, float2(127.1, 311.7)) + float4(0, 127.1, 311.7, 438.8)) * 43758.5453);
    return lerp(lerp(h.x, h.y, f.x), lerp(h.z, h.w, f.x), f.y);
}

float WeatherVisibility(float2 screen)
{
    return 1 - AuxiliaryTexture.Sample(AuxiliarySampler, screen / Viewport).a;
}

float4 PSWeatherOcclusion(Output input) : SV_TARGET
{
    float4 pixel = SampleStraight(input.Uv);
    float brightest = max(pixel.r, max(pixel.g, pixel.b));
    // Reconstructed baked shadows are translucent black. Solid dark artwork
    // still occludes weather, as does the coloured fringe of a sprite.
    float shadow = (1 - step(1.5 / 255, brightest)) * (1 - step(254.5 / 255, pixel.a));
    return float4(0, 0, 0, pixel.a * input.Tint.a * (1 - shadow));
}

float4 PSWeatherGround(Output input) : SV_TARGET
{
    float visibility = WeatherVisibility(input.Position.xy);
    if (visibility < 0.001) return 0;
    float2 world = input.Position.xy - C[0].xy + C[2].xx;
    float snow = 0, wet = 0, sheen = 0, fog = 0;
    if (C[0].z > 0.001 && input.Tint.r > 0)
    {
        float basins = WeatherNoise(world / float2(41, 24)) * 0.72 + WeatherNoise(world / 11) * 0.28;
        float pool = smoothstep(0.18, 0.5, basins + saturate(C[0].z) * 0.38 - 0.28);
        wet = saturate(C[0].z * input.Tint.r * (0.08 + pool * 0.40));
        float2 drops = world / float2(28, 18);
        float2 cell = floor(drops);
        float2 centre = 0.24 + 0.52 * float2(WeatherNoise(cell + 0.27), WeatherNoise(cell + 19.13));
        float phase = frac(C[1].w * (1.2 + centre.x) + centre.y * 2);
        float distance = length(frac(drops) - centre);
        float ring = (1 - smoothstep(0.024, 0.067, abs(distance - phase * 0.29))) *
            smoothstep(0, 0.1, phase);
        sheen = pool * saturate(C[0].z * input.Tint.r) * (0.15 + C[2].y * ring * (1 - phase) * 0.38);
    }
    if (C[0].w > 0.001 && input.Tint.g > 0)
    {
        float grain = WeatherNoise(world / 2.5);
        float pattern = WeatherNoise(world / 33) * 0.7 + grain * 0.3;
        float cover = saturate(C[0].w * input.Tint.g);
        snow = smoothstep(0.27, 0.7, pattern + cover * 0.67 - 0.46) * cover * (0.58 + grain * 0.38);
    }
    if (C[1].z > 0.001)
    {
        float2 p = input.Position.xy - C[1].xy + C[2].xx;
        float mist = WeatherNoise(p / float2(135, 38)) * 0.73 + WeatherNoise(p / float2(46, 17)) * 0.27;
        fog = saturate(smoothstep(0.19, 0.89, mist) * C[1].z * 0.68);
    }
    // Compose the weather contributions in premultiplied space before returning straight alpha.
    float3 colour = float3(0.13, 0.22, 0.32) * wet;
    colour = lerp(colour, float3(0.62, 0.78, 0.96), sheen);
    float alpha = lerp(wet, 1, sheen);
    colour = lerp(colour, float3(0.81, 0.89, 0.99), snow);
    alpha = lerp(alpha, 1, snow);
    colour = lerp(colour, float3(0.73, 0.82, 0.88), fog);
    alpha = lerp(alpha, 1, fog);
    return float4(colour / max(alpha, 0.0001), alpha * visibility);
}

float4 PSWeatherParticle(Output input) : SV_TARGET
{
    float2 local = (input.Uv - input.Bounds.xy) / (input.Bounds.zw - input.Bounds.xy);
    float2 centre = (local - 0.5) * 2;
    float3 cold = lerp(float3(0.53, 0.68, 0.82), float3(0.85, 0.94, 1), input.Tint.g);
    float shape;
    if (input.Tint.r < 0.002)
    {
        float width = lerp(0.24, 0.78, local.y);
        shape = (1 - smoothstep(width * 0.2, width, abs(centre.x))) *
            smoothstep(0, 0.75, local.y) * (1 - smoothstep(0.87, 1, local.y));
    }
    else if (input.Tint.r < 0.006)
    {
        shape = 1 - smoothstep(0.04, 1, length(centre));
        cold = lerp(cold, 1, 0.5);
    }
    else
    {
        shape = 1 - smoothstep(0.08, 0.39, abs(length(centre) - 0.67));
    }
    return PrepareBlend(float4(cold, shape * input.Tint.a * WeatherVisibility(input.Position.xy)));
}

// Atmosphere runs only when compositing the world texture, before the HUD.
float4 PSAtmosphere(Output input) : SV_TARGET
{
    float4 pixel = SampleStraight(input.Uv) * input.Tint;
    int style = (int)C[6].w;
    int shader = style / 4;
    int tone = style % 4;
    float fx = C[7].w;
    if ((shader == 3 || shader == 4) && fx > 0)
    {
        float spread = shader == 3 ? 1 : 1 + fx;
        float2 dx = float2(spread / Viewport.x, 0), dy = float2(0, spread / Viewport.y);
        float3 neighbours = SampleStraight(input.Uv - dx).rgb + SampleStraight(input.Uv + dx).rgb
                          + SampleStraight(input.Uv - dy).rgb + SampleStraight(input.Uv + dy).rgb;
        float3 average = neighbours * input.Tint.rgb * 0.25;
        pixel.rgb = shader == 3 ? saturate(pixel.rgb + 0.72 * fx * (pixel.rgb - average)) :
                                  lerp(pixel.rgb, average, saturate(fx * 0.5));
    }
    if (shader == 5 && fx > 0)
    {
        float2 drift = (input.Uv - 0.5) * fx * 5 / Viewport;
        pixel.r = SampleStraight(input.Uv + drift).r * input.Tint.r;
        pixel.b = SampleStraight(input.Uv - drift).b * input.Tint.b;
    }
    float fog = saturate((C[4].y - input.Uv.y) * C[4].z) * C[3].x;
    pixel.rgb = lerp(pixel.rgb, C[3].yzw, fog);
    pixel.rgb = saturate(pixel.rgb + max(max(pixel.r, pixel.g), pixel.b) * C[4].x * C[4].w);
    float2 centred = input.Uv - C[1].xy;
    float vignette = max(1 - dot(centred, centred) * C[0].y, C[0].w);
    pixel.rgb *= C[0].x * vignette;
    pixel.rgb = lerp(pixel.rgb, pixel.rgb * C[2].rgb, C[2].a);
    pixel.rgb = lerp(pixel.rgb, C[5].yzw, C[5].x);
    pixel.rgb *= C[6].rgb;
    float luminance = dot(pixel.rgb, float3(0.299, 0.587, 0.114));
    float chroma = max(pixel.r, max(pixel.g, pixel.b)) - min(pixel.r, min(pixel.g, pixel.b));
    pixel.rgb = luminance + (pixel.rgb - luminance) * (1 + C[1].w + C[7].z * (1 - saturate(chroma)));
    pixel.rgb = (pixel.rgb - 0.5) * (1 + C[1].z) + 0.5;
    pixel.rgb += (1 - saturate(luminance)) * C[7].x + max(pixel.rgb - 0.45, 0) * (C[7].y - 1);
    if (tone == 1) pixel.rgb = max(pixel.rgb, 0) / (1 + max(pixel.rgb, 0) * 0.3);
    if (tone == 2) pixel.rgb = smoothstep(0, 1, pixel.rgb);
    if (C[0].z != 1) pixel.rgb = pow(max(pixel.rgb, 0), C[0].z);
    if (shader == 1) pixel.rgb = lerp(pixel.rgb, dot(pixel.rgb, float3(0.299, 0.587, 0.114)), saturate(fx));
    if (shader == 2) pixel.rgb = lerp(pixel.rgb, round(saturate(pixel.rgb) * 7) / 7, saturate(fx));
    if (shader == 6) pixel.rgb += (WeatherNoise(floor(input.Position.xy) + 19.7) - 0.5) * 0.16 * fx;
    if (shader == 7) pixel.rgb = lerp(pixel.rgb, 1 - saturate(pixel.rgb), saturate(fx));
    if (shader == 8) pixel.rgb *= 1 - (floor(input.Position.y) % 2) * 0.25 * saturate(fx);
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
    // Separable nine-tap Gaussian expressed as five bilinear samples. C0.xy
    // supplies horizontal or vertical texel direction; C1.x is pass intensity.
    float2 direction = C[0].xy;
    float3 sum = SampleStraight(input.Uv).rgb * 0.2270270270;
    sum += SampleStraight(input.Uv + direction * 1.3846153846).rgb * 0.3162162162;
    sum += SampleStraight(input.Uv - direction * 1.3846153846).rgb * 0.3162162162;
    sum += SampleStraight(input.Uv + direction * 3.2307692308).rgb * 0.0702702703;
    sum += SampleStraight(input.Uv - direction * 3.2307692308).rgb * 0.0702702703;
    return float4(sum * C[1].x, 1);
}
