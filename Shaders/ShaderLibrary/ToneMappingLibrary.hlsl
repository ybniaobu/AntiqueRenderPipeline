#ifndef YPIPELINE_TONEMAPPING_LIBRARY_INCLUDED
#define YPIPELINE_TONEMAPPING_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// TODO: 以后对直接控制 tone mapping 曲线有需求可以看这两篇文章：
// http://filmicworlds.com/blog/filmic-tonemapping-with-piecewise-power-curves/
// https://dev.epicgames.com/documentation/en-us/unreal-engine/color-grading-and-the-filmic-tonemapper-in-unreal-engine

// ----------------------------------------------------------------------------------------------------
// Reinhard tone mapping
// ----------------------------------------------------------------------------------------------------

// From https://www-old.cs.utah.edu/docs/techreports/2002/pdf/UUCS-02-001.pdf
float3 Reinhard(float3 color)
{
    return color / (color + 1.0);
}

float3 Reinhard_Extended(float3 color, float minWhite)
{
    float minWhite2 = minWhite * minWhite;
    float3 numerator = color * (1.0 + color / minWhite2);
    return numerator / (1.0 + color);
}

float3 Reinhard_ExtendedLuminance(float3 color, float minWhite)
{
    float l_in = Luminance(color);
    float minWhite2 = minWhite * minWhite;
    // float numerator = l_in * (1.0 + l_in / minWhite2);
    // float l_out = numerator / (1.0 + l_in);
    // return color * l_out / l_in;
    float numerator = 1.0 + l_in / minWhite2;
    float l_simplified = numerator / (1.0 + l_in);
    return color * l_simplified;
}

// ----------------------------------------------------------------------------------------------------
// Uncharted 2 Filmic tone mapping
// ----------------------------------------------------------------------------------------------------

// From http://filmicworlds.com/blog/filmic-tonemapping-operators/ By John Hable
float3 Uncharted2(float3 x)
{
    const float A = 0.15;
    const float B = 0.50;
    const float C = 0.10;
    const float D = 0.20;
    const float E = 0.02;
    const float F = 0.30;
    return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

float3 Uncharted2Filmic(float3 color, float exposureBias)
{
    float3 curr = Uncharted2(color * exposureBias);
    const float W = 11.2;
    float3 whiteScale = 1.0 / Uncharted2(W);
    return whiteScale * curr;
}

// ----------------------------------------------------------------------------------------------------
// Khronos PBR Neutral tone mapping
// ----------------------------------------------------------------------------------------------------

// From https://github.com/KhronosGroup/ToneMapping/tree/main/PBR_Neutral
float3 KhronosPBRNeutral(float3 color)
{
    const float startCompression = 0.76;
    const float desaturation = 0.15;

    float x = min(color.r, min(color.g, color.b));
    float offset = x < 0.08 ? x - 6.25 * x * x : 0.04;
    color -= offset;

    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return color;

    const float d = 0.24;
    float newPeak = 1.0 - d * d / (peak + d - startCompression);
    color *= newPeak / peak;

    float g = 1.0 - 1.0 / (desaturation * (peak - newPeak) + 1.);
    return lerp(color, newPeak * float3(1.0, 1.0, 1.0), g);
}

// ----------------------------------------------------------------------------------------------------
// ACES tone mapping
// ----------------------------------------------------------------------------------------------------

// https://github.com/TheRealMJP/BakingLab/blob/master/BakingLab/ACES.hlsl

static const float3x3 ACESInputMat =
{
    {0.59719, 0.35458, 0.04823},
    {0.07600, 0.90834, 0.01566},
    {0.02840, 0.13383, 0.83777}
};

static const float3x3 ACESOutputMat =
{
    { 1.60475, -0.53108, -0.07367},
    {-0.10208,  1.10813, -0.00605},
    {-0.00327, -0.07276,  1.07602}
};

float3 RRTAndODTFit(float3 v)
{
    float3 a = v * (v + 0.0245786f) - 0.000090537f;
    float3 b = v * (0.983729f * v + 0.4329510f) + 0.238081f;
    return a / b;
}

float3 ACESStephenHillFit(float3 color)
{
    color = mul(ACESInputMat, color);

    // Apply RRT and ODT
    color = RRTAndODTFit(color);

    color = mul(ACESOutputMat, color);

    // Clamp to [0, 1]
    color = saturate(color);
    return color;
}

// https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/
float3 ACESApproxFit(float3 color)
{
    color *= 0.6;
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return saturate(color * (a * color + b)/(color * (c * color + d) + e));
}

// ----------------------------------------------------------------------------------------------------
// AgX Approximation tone mapping
// ----------------------------------------------------------------------------------------------------

// From https://github.com/sobotka/AgX
// https://www.shadertoy.com/view/McG3WW , https://www.shadertoy.com/view/cd3XWr
// Some more details: https://iolite-engine.com/blog_posts/minimal_agx_implementation

float3 AgXDefaultContrastApprox(float3 x)
{
    float3 x2 = x * x;
    float3 x4 = x2 * x2;
  
    return + 15.5     * x4 * x2
           - 40.14    * x4 * x
           + 31.96    * x4
           - 6.868    * x2 * x
           + 0.4298   * x2
           + 0.1191   * x
           - 0.00232;
}

float3 AgX(float3 val)
{
    const float3x3 agx_mat = float3x3( 0.842479062253094,   0.0784335999999992,  0.0792237451477643,
                                       0.0423282422610123,  0.878468636469772,   0.0791661274605434,
                                       0.0423756549057051,  0.0784336,           0.879142973793104);
        
    const float min_ev = -12.47393f;
    const float max_ev = 4.026069f;
    
    // Input transform
    val = mul(agx_mat, val);
      
    // Log2 space encoding
    val = clamp(log2(val), min_ev, max_ev);
    val = (val - min_ev) / (max_ev - min_ev);
      
    // Apply sigmoid function approximation
    val = AgXDefaultContrastApprox(val);
    
    return val;
}

float3 AgXEotf(float3 val)
{
    const float3x3 agx_mat_inv = float3x3(  1.19687900512017,    -0.0980208811401368,  -0.0990297440797205,
                                           -0.0528968517574562,   1.15190312990417,    -0.0989611768448433,
                                           -0.0529716355144438,  -0.0980434501171241,   1.15107367264116);
        
    // Undo input transform
    val = mul(agx_mat_inv, val);
    
    // sRGB IEC 61966-2-1 2.2 Exponent Reference EOTF Display
    // NOTE: We're linearizing the output here. Comment/adjust when not using a sRGB render target
    val = pow(val, 2.2);
    
    return val;
}

float3 AgXLook_Default(float3 val)
{
    const float3 lw = float3(0.2126, 0.7152, 0.0722);
    float luma = dot(val, lw);
    
    // Default look
    float3 offset = float3(0.0, 0.0, 0.0);
    float3 slope = float3(1.0, 1.0, 1.0);
    float3 power = float3(1.0, 1.0, 1.0);
    float sat = 1.0;
    
    // ASC CDL
    val = pow(val * slope + offset, power);
    return luma + sat * (val - luma);
}

float3 AgXLook_Golden(float3 val)
{
    const float3 lw = float3(0.2126, 0.7152, 0.0722);
    float luma = dot(val, lw);
    
    float3 offset = float3(0.0, 0.0, 0.0);
    float3 slope = float3(1.0, 0.9, 0.5);
    float3 power = float3(0.8, 0.8, 0.8);
    float sat = 0.8;
    
    // ASC CDL
    val = pow(val * slope + offset, power);
    return luma + sat * (val - luma);
}

float3 AgXLook_Punchy(float3 val)
{
    const float3 lw = float3(0.2126, 0.7152, 0.0722);
    float luma = dot(val, lw);
    
    float3 offset = float3(0.0, 0.0, 0.0);
    float3 slope = float3(1.0, 1.0, 1.0);
    float3 power = float3(1.35, 1.35, 1.35);
    float sat = 1.4;
    
    // ASC CDL
    val = pow(val * slope + offset, power);
    return luma + sat * (val - luma);
}

float3 AgXApprox_Default(float3 color)
{
    color = AgX(color);
    color = AgXLook_Default(color);
    color = AgXEotf(color);
    return color;
}

float3 AgXApprox_Golden(float3 color)
{
    color = AgX(color);
    color = AgXLook_Golden(color);
    color = AgXEotf(color);
    return color;
}

float3 AgXApprox_Punchy(float3 color)
{
    color = AgX(color);
    color = AgXLook_Punchy(color);
    color = AgXEotf(color);
    return color;
}

#endif