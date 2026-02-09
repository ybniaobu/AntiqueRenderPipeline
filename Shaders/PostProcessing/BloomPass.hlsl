#ifndef YPIPELINE_BLOOM_PASS_INCLUDED
#define YPIPELINE_BLOOM_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "../ShaderLibrary/Core/UnityInput.hlsl"

#include "CopyPass.hlsl"

float4 _BloomThreshold; // x: threshold, y: -threshold + threshold * thresholdKnee, z: 2 * threshold * thresholdKnee, w: 1 / 4 * threshold * thresholdKnee
float4 _BloomParams; // x: intensity or scatter

TEXTURE2D(_BloomLowerTexture);
float4 _BloomLowerTexture_TexelSize;

float3 ApplyBloomThreshold(float3 color)
{
    float brightness = Max3(color.r, color.g, color.b);
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001);
    return color * contribution;
}

float4 BloomPrefilterFrag(Varyings IN) : SV_TARGET
{
    float3 color = float3(0.0, 0.0, 0.0);
    float weight = 0.0;
    const float2 offsets[5] = { float2(0.0, 0.0), float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)};
    // float2 offsets[9] = { float2(0.0, 0.0), float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0),
    // float2(-1.0, 0.0), float2(1.0, 0.0), float2(0.0, -1.0), float2(0.0, 1.0)};

    UNITY_UNROLL
    for (int i = 0; i < 5; i++)
    {
        float2 offset = offsets[i] * _BlitTexture_TexelSize.xy * 2.0;
        float3 c = ApplyBloomThreshold(SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv + offset, 0).rgb);
        // float w = 1.0 / (Luminance(c) + 1.0);
        float w = 1.0 / (c.r + c.g + c.b + 1.0);
        color += c * w;
        weight += w;
    }
    color /= weight;

    // Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
    color = max(color, 0.0);
    return float4(color, 1.0);
}

float4 BloomGaussianBlurHorizontalFrag(Varyings IN) : SV_TARGET
{
    float3 color = float3(0.0, 0.0, 0.0);
    const float offsets[9] = { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 };
    const float weights[9] = { 0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703, 0.19459459, 0.12162162, 0.05405405, 0.01621622 };

    // 9×9 Gaussian filter
    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        float offset = offsets[i] * _BlitTexture_TexelSize.x * 2.0;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv + float2(offset, 0), 0).rgb * weights[i];
    }
    
    return float4(color, 1.0);
}

float4 BloomGaussianBlurVerticalFrag(Varyings IN) : SV_TARGET
{
    float3 color = float3(0.0, 0.0, 0.0);
    const float offsets[5] = { -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923 };
    const float weights[5] = { 0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027 };

    // 5×5 Gaussian filter
    UNITY_UNROLL
    for (int i = 0; i < 5; i++)
    {
        float offset = offsets[i] * _BlitTexture_TexelSize.y;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv + float2(0, offset), 0).rgb * weights[i];
    }
    
    return float4(color, 1.0);
}

float4 BloomAdditiveUpsampleFrag(Varyings IN) : SV_TARGET
{
    #if _BLOOM_BICUBIC_UPSAMPLING
        float3 lowerTex = SampleTexture2DBicubic(_BloomLowerTexture, sampler_LinearClamp, IN.uv, _BloomLowerTexture_TexelSize.zwxy, (1.0).xx, 0.0).rgb;
    #else
        float3 lowerTex = SAMPLE_TEXTURE2D_LOD(_BloomLowerTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    #endif
    
    float3 higherTex = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(lowerTex * _BloomParams.x + higherTex, 1.0);
}

float4 BloomScatteringUpsampleFrag(Varyings IN) : SV_TARGET
{
    #if _BLOOM_BICUBIC_UPSAMPLING
    float3 lowerTex = SampleTexture2DBicubic(_BloomLowerTexture, sampler_LinearClamp, IN.uv, _BloomLowerTexture_TexelSize.zwxy, (1.0).xx, 0.0).rgb;
    #else
    float3 lowerTex = SAMPLE_TEXTURE2D_LOD(_BloomLowerTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    #endif
    
    float3 higherTex = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    return float4(lerp(higherTex, lowerTex, _BloomParams.x), 1.0);
}

float4 BloomScatteringFinalBlitFrag(Varyings IN) : SV_TARGET
{
    #if _BLOOM_BICUBIC_UPSAMPLING
    float3 lowerTex = SampleTexture2DBicubic(_BloomLowerTexture, sampler_LinearClamp, IN.uv, _BloomLowerTexture_TexelSize.zwxy, (1.0).xx, 0.0).rgb;
    #else
    float3 lowerTex = SAMPLE_TEXTURE2D_LOD(_BloomLowerTexture, sampler_LinearClamp, IN.uv, 0).rgb;
    #endif
    
    float3 higherTex = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0).rgb;

    lowerTex += higherTex - ApplyBloomThreshold(higherTex);
    return float4(lerp(higherTex, lowerTex, _BloomParams.x), 1.0);
}

#endif