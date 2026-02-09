#ifndef YPIPELINE_DENOISE_LIBRARY_INCLUDED
#define YPIPELINE_DENOISE_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// ----------------------------------------------------------------------------------------------------
// Bilateral Denoise Functions
// ----------------------------------------------------------------------------------------------------

inline float BilateralWeight(float radius, float depth, float middleDepth, float sigma, float depthThreshold)
{
    bool depthTest = abs(1 - depth / middleDepth) < depthThreshold;
    return exp2(-radius * radius * rcp(2.0 * sigma.x * sigma.x)) * depthTest;
}

inline float NormalWeight(float3 normal, float3 middleNormal)
{
    float normalDelta = max(dot(normal, middleNormal), 0);
    return normalDelta * normalDelta;
}

// ----------------------------------------------------------------------------------------------------
// Temporal Denoise Functions
// ----------------------------------------------------------------------------------------------------

#define YCOCG 1

inline float3 RGB2YCoCg(float3 rgb)
{
    return float3(
             rgb.x/4.0 + rgb.y/2.0 + rgb.z/4.0,
             rgb.x/2.0 - rgb.z/2.0,
            -rgb.x/4.0 + rgb.y/2.0 - rgb.z/4.0
        );
}

inline float3 YCoCg2RGB(float3 YCoCg)
{
    return float3(
            YCoCg.x + YCoCg.y - YCoCg.z,
            YCoCg.x + YCoCg.z,
            YCoCg.x - YCoCg.y - YCoCg.z
        );
}

inline float4 LoadColorAndDepth(TEXTURE2D(tex), int2 pixelCoord)
{
    float4 sample = LOAD_TEXTURE2D_LOD(tex, pixelCoord, 0);
    #if YCOCG
    return float4(RGB2YCoCg(sample.rgb), sample.a);
    #else
    return sample;
    #endif
}

inline float4 SampleColorAndDepth(TEXTURE2D(tex), SAMPLER(samplerTex), float2 screenUV)
{
    float4 sample = SAMPLE_TEXTURE2D_LOD(tex, samplerTex, screenUV, 0);
    #if YCOCG
    return float4(RGB2YCoCg(sample.rgb), sample.a);;
    #else
    return sample;
    #endif
}

inline float GetLuma(float3 color)
{
    #if YCOCG
    return color.r;
    #else
    return Luminance(color);
    #endif
}

inline float3 OutputColor(float3 color)
{
    #if YCOCG
    return max(YCoCg2RGB(color), 0);
    #else
    return max(color, 0);
    #endif
}

float3 BilateralFilterColor(in float4 neighbours[9], float depthThreshold)
{
    const float weights[9] = { 4.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0 };
    
    float3 middleColor = neighbours[0].rgb;
    float middleDepth = neighbours[0].a;
    float weightSum = rcp(GetLuma(middleColor) + 1.0) * 4.0;
    float3 filtered = weightSum * middleColor;

    UNITY_UNROLL
    for (int i = 0; i < 8; i++)
    {
        float3 sampleColor = neighbours[i + 1].rgb;
        float sampleDepth = neighbours[i + 1].a;
        bool occlusionTest = abs(1 - sampleDepth / middleDepth) < depthThreshold;
        float weight = rcp(GetLuma(sampleColor) + 1.0) * weights[i + 1] * occlusionTest;
        weightSum += weight;
        filtered += weight * sampleColor;
    }
    
    filtered *= rcp(weightSum);
    return filtered;
}

float BilateralFilterAO(in float2 neighbours[9], float depthThreshold)
{
    const float weights[9] = { 4.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0 };
    
    float middleAO = neighbours[0].r;
    float middleDepth = neighbours[0].g;
    float weightSum = 4.0;
    float filtered = weightSum * middleAO;

    UNITY_UNROLL
    for (int i = 0; i < 8; i++)
    {
        float sampleAO = neighbours[i + 1].r;
        float sampleDepth = neighbours[i + 1].g;
        bool occlusionTest = abs(1 - sampleDepth / middleDepth) < depthThreshold;
        float weight = weights[i + 1] * occlusionTest;
        weightSum += weight;
        filtered += weight * sampleAO;
    }
    
    filtered *= rcp(weightSum);
    return filtered;
}

// Irradiance Version
void VarianceMinMax(in float4 neighbours[9], float gamma, float3 prefiltered, out float3 minColor, out float3 maxColor)
{
    float3 m1 = 0;
    float3 m2 = 0;

    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        float3 sampleColor = neighbours[i].rgb;
        m1 += sampleColor;
        m2 += sampleColor * sampleColor;
    }

    const int sampleCount = 9;
    m1 *= rcp(sampleCount);
    m2 *= rcp(sampleCount);

    float3 sigma = sqrt(abs(m2 - m1 * m1)); // standard deviation
    float3 neighborMin = m1 - gamma * sigma;
    float3 neighborMax = m1 + gamma * sigma;

    neighborMin = min(neighborMin, prefiltered);
    neighborMax = max(neighborMax, prefiltered);

    minColor = neighborMin;
    maxColor = neighborMax;
}

// AO Version, return float2(minAO, maxAO)
float2 VarianceMinMax(in float2 neighbours[9], float gamma, float prefiltered)
{
    float m1 = 0;
    float m2 = 0;

    UNITY_UNROLL
    for (int i = 0; i < 9; i++)
    {
        float sampleAO = neighbours[i].r;
        m1 += sampleAO;
        m2 += sampleAO * sampleAO;
    }

    const int sampleCount = 9;
    m1 *= rcp(sampleCount);
    m2 *= rcp(sampleCount);

    float sigma = sqrt(abs(m2 - m1 * m1)); // standard deviation
    float neighborMin = m1 - gamma * sigma;
    float neighborMax = m1 + gamma * sigma;

    neighborMin = min(neighborMin, prefiltered);
    neighborMax = max(neighborMax, prefiltered);

    return float2(neighborMin, neighborMax);
}

float3 NeighborhoodClipToFiltered(float3 minColor, float3 maxColor, float3 prefiltered, float3 history)
{
    float3 boxMin = minColor;
    float3 boxMax = maxColor;

    float3 rayOrigin = history;
    float3 rayDir = prefiltered - history;
    rayDir = abs(rayDir) < HALF_MIN ? HALF_MIN : rayDir;
    float3 invDir = rcp(rayDir);
    
    float3 minIntersect = (boxMin - rayOrigin) * invDir;
    float3 maxIntersect = (boxMax - rayOrigin) * invDir;
    float3 enterIntersect = min(minIntersect, maxIntersect);
    float intersect = Max3(enterIntersect.x, enterIntersect.y, enterIntersect.z);
    float historyBlend = saturate(intersect);
    return lerp(history, prefiltered, historyBlend);
}

float3 LumaExponentialAccumulation(float3 history, float3 current, float blendFactor)
{
    float historyLuma = GetLuma(history);
    float currentLuma = GetLuma(current);
    float historyLumaWeight = rcp(historyLuma + 1.0);
    float currentLumaWeight = rcp(currentLuma + 1.0);
    float weightSum = lerp(currentLumaWeight, historyLumaWeight, blendFactor);
    float3 blendColor = lerp(current * currentLumaWeight, history * historyLumaWeight, blendFactor);
    return blendColor / weightSum;
}

#endif