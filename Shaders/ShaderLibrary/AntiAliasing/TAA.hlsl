#ifndef YPIPELINE_TAA_INCLUDED
#define YPIPELINE_TAA_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

// ----------------------------------------------------------------------------------------------------
// TAA Utility Functions
// ----------------------------------------------------------------------------------------------------

inline float4 LoadOffset(TEXTURE2D(tex), float2 pixelCoord, int2 offset)
{
    return LOAD_TEXTURE2D_LOD(tex, pixelCoord + offset, 0);
}

inline float4 SampleLinearOffset(TEXTURE2D(tex), float2 uv, int2 offset)
{
    return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, uv + offset * _CameraBufferSize.xy, 0);
}

float3 RGB2YCoCg(float3 rgb)
{
    return float3(
             rgb.x/4.0 + rgb.y/2.0 + rgb.z/4.0,
             rgb.x/2.0 - rgb.z/2.0,
            -rgb.x/4.0 + rgb.y/2.0 - rgb.z/4.0
        );
}

float3 YCoCg2RGB(float3 YCoCg)
{
    return float3(
            YCoCg.x + YCoCg.y - YCoCg.z,
            YCoCg.x + YCoCg.z,
            YCoCg.x - YCoCg.y - YCoCg.z
        );
}

float GetLuma(float3 color)
{
    #if _TAA_YCOCG
    return color.r;
    #else
    return Luminance(color);
    #endif
}

float3 LoadColor(TEXTURE2D(tex), float2 pixelCoord, int2 offset)
{
    #if _TAA_YCOCG
        return RGB2YCoCg(LoadOffset(tex, pixelCoord, offset).xyz);
    #else
        return LoadOffset(tex, pixelCoord, offset).xyz;
    #endif
}

float4 LoadColorAndAlpha(TEXTURE2D(tex), float2 pixelCoord, int2 offset)
{
    float4 color = LoadOffset(tex, pixelCoord, offset);
    #if _TAA_YCOCG
        return float4(RGB2YCoCg(color.rgb), color.a);
    #else
        return color;
    #endif
}

float3 OutputColor(float3 color)
{
    #if _TAA_YCOCG
        return max(YCoCg2RGB(color), 0);
    #else
        return max(color, 0);
    #endif
}

// ----------------------------------------------------------------------------------------------------
// Closest Velocity
// ----------------------------------------------------------------------------------------------------

float2 GetClosestDepthPixelCoord(TEXTURE2D(depthTex), float2 pixelCoord)
{
    float M = LoadOffset(depthTex, pixelCoord, int2(0, 0)).x;
    float N = LoadOffset(depthTex, pixelCoord, int2(0, 1)).x;
    float E = LoadOffset(depthTex, pixelCoord, int2(1, 0)).x;
    float S = LoadOffset(depthTex, pixelCoord, int2(0, -1)).x;
    float W = LoadOffset(depthTex, pixelCoord, int2(-1, 0)).x;
    #if _TAA_SAMPLE_3X3
    float NW = LoadOffset(depthTex, pixelCoord, int2(-1, 1)).x;
    float NE = LoadOffset(depthTex, pixelCoord, int2(1, 1)).x;
    float SW = LoadOffset(depthTex, pixelCoord, int2(-1, -1)).x;
    float SE = LoadOffset(depthTex, pixelCoord, int2(1, -1)).x;
    #endif

    float3 offset = float3(0, 0, M);
    offset = lerp(offset, float3(0, 1, N), COMPARE_DEVICE_DEPTH_CLOSER(N, offset.z));
    offset = lerp(offset, float3(1, 0, E), COMPARE_DEVICE_DEPTH_CLOSER(E, offset.z));
    offset = lerp(offset, float3(0, -1, S), COMPARE_DEVICE_DEPTH_CLOSER(S, offset.z));
    offset = lerp(offset, float3(-1, 0, W), COMPARE_DEVICE_DEPTH_CLOSER(W, offset.z));
    #if _TAA_SAMPLE_3X3
    offset = lerp(offset, float3(-1, 1, NW), COMPARE_DEVICE_DEPTH_CLOSER(NW, offset.z));
    offset = lerp(offset, float3(1, 1, NE), COMPARE_DEVICE_DEPTH_CLOSER(NE, offset.z));
    offset = lerp(offset, float3(-1, -1, SW), COMPARE_DEVICE_DEPTH_CLOSER(SW, offset.z));
    offset = lerp(offset, float3(1, -1, SE), COMPARE_DEVICE_DEPTH_CLOSER(SE, offset.z));
    #endif
    
    return pixelCoord + offset.xy;
}

// ----------------------------------------------------------------------------------------------------
// History Filter
// ----------------------------------------------------------------------------------------------------

float3 SampleHistoryLinear(TEXTURE2D(tex), float2 uv)
{
    #if _TAA_YCOCG
    return RGB2YCoCg(SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, uv, 0).xyz);
    #else
    return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, uv, 0).xyz;
    #endif
}

float3 SampleHistoryBicubic(TEXTURE2D(tex), float2 uv)
{
    float2 samplePos = uv * _CameraBufferSize.zw;
    float2 tc1 = floor(samplePos - 0.5) + 0.5;
    float2 f = samplePos - tc1;
    float2 f2 = f * f;
    float2 f3 = f * f2;

    float c = 0.5; // sharpen factor (0, 1)
    
    float2 w0 = -c         * f3 +  2.0 * c         * f2 - c * f;
    float2 w1 =  (2.0 - c) * f3 - (3.0 - c)        * f2          + 1.0;
    float2 w2 = -(2.0 - c) * f3 + (3.0 - 2.0 * c)  * f2 + c * f;
    float2 w3 = c          * f3 - c                * f2;

    float2 w12 = w1 + w2;
    float2 tc0 = _CameraBufferSize.xy   * (tc1 - 1.0);
    float2 tc3 = _CameraBufferSize.xy   * (tc1 + 2.0);
    float2 tc12 = _CameraBufferSize.xy  * (tc1 + w2 / w12);

    float3 s0 = SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, float2(tc12.x, tc0.y), 0).xyz;
    float3 s1 = SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, float2(tc0.x, tc12.y), 0).xyz;
    float3 s2 = SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, float2(tc12.x, tc12.y), 0).xyz;
    float3 s3 = SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, float2(tc3.x, tc12.y), 0).xyz;
    float3 s4 = SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, float2(tc12.x, tc3.y), 0).xyz;

    float cw0 = (w12.x * w0.y);
    float cw1 = (w0.x * w12.y);
    float cw2 = (w12.x * w12.y);
    float cw3 = (w3.x * w12.y);
    float cw4 = (w12.x *  w3.y);

    float3 minHistory = min(s0, min(s1, min(s2, min(s3, s4))));
    float3 maxHistory = max(s0, max(s1, max(s2, max(s3, s4))));

    s0 *= cw0;
    s1 *= cw1;
    s2 *= cw2;
    s3 *= cw3;
    s4 *= cw4;

    float3 historyFiltered = s0 + s1 + s2 + s3 + s4;
    float weightSum = cw0 + cw1 + cw2 + cw3 + cw4;

    float3 filteredVal = historyFiltered * rcp(weightSum);

    #if _TAA_YCOCG
    return RGB2YCoCg(clamp(filteredVal, minHistory, maxHistory));
    #else
    return clamp(filteredVal, minHistory, maxHistory);
    #endif
}

float3 SampleHistory(TEXTURE2D(tex), float2 uv)
{
    #if _TAA_HISTORY_FILTER
    return SampleHistoryBicubic(tex, uv);
    #else
    return SampleHistoryLinear(tex, uv);
    #endif
}


// ----------------------------------------------------------------------------------------------------
// Neighbourhood Samples Related
// ----------------------------------------------------------------------------------------------------

#ifdef _TAA_SAMPLE_3X3
#define _TAA_SAMPLE_NUMBER 9
#else
#define _TAA_SAMPLE_NUMBER 5
#endif

struct NeighbourhoodSamples
{
    #if _TAA_SAMPLE_3X3
    float3 neighbours[8];
    #else
    float3 neighbours[4];
    #endif
    float3 M;
    float3 filteredM;
    float3 min;
    float3 max;
};

void GetNeighbourhoodSamples(inout NeighbourhoodSamples samples, TEXTURE2D(tex), float2 pixelCoord)
{
    samples.M = LoadColor(tex, pixelCoord, int2(0, 0));
    samples.neighbours[0] = LoadColor(tex, pixelCoord, int2(0, 1));
    samples.neighbours[1] = LoadColor(tex, pixelCoord, int2(1, 0));
    samples.neighbours[2] = LoadColor(tex, pixelCoord, int2(0, -1));
    samples.neighbours[3] = LoadColor(tex, pixelCoord, int2(-1, 0));

    #if _TAA_SAMPLE_3X3
    samples.neighbours[4] = LoadColor(tex, pixelCoord, int2(-1, 1));
    samples.neighbours[5] = LoadColor(tex, pixelCoord, int2(1, 1));
    samples.neighbours[6] = LoadColor(tex, pixelCoord, int2(-1, -1));
    samples.neighbours[7] = LoadColor(tex, pixelCoord, int2(1, -1));
    #endif
}

// ----------------------------------------------------------------------------------------------------
// Prefilter Middle Color
// ----------------------------------------------------------------------------------------------------

// Not recommended, box filter is not suitable for reconstruction or sharpen
float3 BoxFilterMiddleColor(in NeighbourhoodSamples samples)
{
    const float weight = 1.0 / float(_TAA_SAMPLE_NUMBER);
    float weightSum = rcp(GetLuma(samples.M) + 1.0) * weight;
    float3 filtered = weightSum * samples.M;
    
    for (int i = 0; i < _TAA_SAMPLE_NUMBER - 1; i++)
    {
        float lumaWeight = rcp(GetLuma(samples.neighbours[i]) + 1.0) * weight;
        weightSum += lumaWeight;
        filtered += lumaWeight * samples.neighbours[i];
    }
    filtered *= rcp(weightSum);
    return filtered;
}

float3 GaussianApproxFilterMiddleColor(in NeighbourhoodSamples samples)
{
    const float weights[9] = { 4.0, 2.0, 2.0, 2.0, 2.0, 1.0, 1.0, 1.0, 1.0 };
    float weightSum = rcp(GetLuma(samples.M) + 1.0) * 4.0;
    float3 filtered = weightSum * samples.M;

    for (int i = 0; i < _TAA_SAMPLE_NUMBER - 1; i++)
    {
        float lumaWeight = rcp(GetLuma(samples.neighbours[i]) + 1.0) * weights[i + 1];
        weightSum += lumaWeight;
        filtered += lumaWeight * samples.neighbours[i];
    }
    filtered *= rcp(weightSum);
    return filtered;
}

float3 GaussianFilterMiddleColor(in NeighbourhoodSamples samples)
{
    // sigma = 0.8
    // const float weights[9] = { 1.0, 0.4578, 0.4578, 0.4578, 0.4578, 0.2097, 0.2097, 0.2097, 0.2097 };
    
    // sigma = 0.6
    const float weights[9] = { 1.0, 0.2493, 0.2493, 0.2493, 0.2493, 0.0625, 0.0625, 0.0625, 0.0625 };

    // sigma = 0.5
    // const float weights[9] = { 1.0, 0.1353, 0.1353, 0.1353, 0.1353, 0.0183, 0.0183, 0.0183, 0.0183 };
    float weightSum = rcp(GetLuma(samples.M) + 1.0) * 1.0;
    float3 filtered = weightSum * samples.M;

    for (int i = 0; i < _TAA_SAMPLE_NUMBER - 1; i++)
    {
        float lumaWeight = rcp(GetLuma(samples.neighbours[i]) + 1.0) * weights[i + 1];
        weightSum += lumaWeight;
        filtered += lumaWeight * samples.neighbours[i];
    }
    filtered *= rcp(weightSum);
    return filtered;
}

float3 FilterMiddleColor(in NeighbourhoodSamples samples)
{
    #if _TAA_CURRENT_FILTER
    return GaussianFilterMiddleColor(samples);
    #else
    return samples.M;
    #endif
}

// ----------------------------------------------------------------------------------------------------
// Build AABB Box
// ----------------------------------------------------------------------------------------------------

void MinMaxNeighbourhood(inout NeighbourhoodSamples samples)
{
    samples.min = min(samples.M, min(samples.neighbours[0], min(samples.neighbours[1], min(samples.neighbours[2], samples.neighbours[3]))));
    samples.max = max(samples.M, max(samples.neighbours[0], max(samples.neighbours[1], max(samples.neighbours[2], samples.neighbours[3]))));

    #if _TAA_SAMPLE_3X3
    samples.min = min(samples.min, min(samples.neighbours[4], min(samples.neighbours[5], min(samples.neighbours[6], samples.neighbours[7]))));
    samples.max = max(samples.max, max(samples.neighbours[4], max(samples.neighbours[5], max(samples.neighbours[6], samples.neighbours[7]))));
    #endif
}

// From "An Excursion in Temporal Supersampling" at GDC 2016
// https://developer.download.nvidia.com/gameworks/events/GDC2016/msalvi_temporal_supersampling.pdf
void VarianceNeighbourhood(inout NeighbourhoodSamples samples, float gamma = 1.25)
{
    float3 m1 = 0;
    float3 m2 = 0;
    for (int i = 0; i < _TAA_SAMPLE_NUMBER - 1; i++)
    {
        float3 sampleColor = samples.neighbours[i];
        m1 += sampleColor;
        m2 += sampleColor * sampleColor;
    }

    m1 += samples.M;
    m2 += samples.M * samples.M;

    const int sampleCount = _TAA_SAMPLE_NUMBER;
    m1 *= rcp(sampleCount);
    m2 *= rcp(sampleCount);

    float3 sigma = sqrt(abs(m2 - m1 * m1)); // standard deviation
    float3 neighborMin = m1 - gamma * sigma;
    float3 neighborMax = m1 + gamma * sigma;

    neighborMin = min(neighborMin, samples.filteredM);
    neighborMax = max(neighborMax, samples.filteredM);
    
    samples.min = neighborMin;
    samples.max = neighborMax;
}

// ----------------------------------------------------------------------------------------------------
// Neighborhood AABB Clamp/AABB Clip/Variance Clip (Color Rejection)
// ----------------------------------------------------------------------------------------------------

float3 NeighborhoodAABBClamp(in NeighbourhoodSamples samples, float3 history)
{
    history = clamp(history, samples.min, samples.max);
    return history;
}

// From "Temporal Reprojection Antialiasing in INSIDE (Playdead Studio)" at GDC 2016
// https://github.com/playdeadgames/temporal/blob/4795aa0007d464371abe60b7b28a1cf893a4e349/Assets/Shaders/TemporalReprojection.shader
// https://www.gdcvault.com/play/1022970/Temporal-Reprojection-Anti-Aliasing-in
float3 NeighborhoodClipToAABBCenter(in NeighbourhoodSamples samples, float3 history)
{
    // note: only clips towards aabb center (but fast!)
    float3 center  = 0.5 * (samples.max + samples.min);
    float3 extents = 0.5 * (samples.max - samples.min) + HALF_MIN;
    
    float3 vOffset = history - center;
    float3 vUnit = vOffset / extents;
    float3 absUnit = abs(vUnit);
    float maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);

    if (maxUnit > 1.0)
        return center + (vOffset / maxUnit);
    else
        return history;
}

// UE4 Version
// Here the ray referenced goes from history to (filtered) center color
float3 NeighborhoodClipToFiltered(in NeighbourhoodSamples samples, float3 history)
{
    float3 boxMin = samples.min;
    float3 boxMax = samples.max;

    float3 rayOrigin = history;
    float3 rayDir = samples.filteredM - history;
    rayDir = abs(rayDir) < HALF_MIN ? HALF_MIN : rayDir;
    float3 invDir = rcp(rayDir);
    
    float3 minIntersect = (boxMin - rayOrigin) * invDir;
    float3 maxIntersect = (boxMax - rayOrigin) * invDir;
    float3 enterIntersect = min(minIntersect, maxIntersect);
    float intersect = Max3(enterIntersect.x, enterIntersect.y, enterIntersect.z);
    float historyBlend = saturate(intersect);
    return lerp(history, samples.filteredM, historyBlend);
}

// ----------------------------------------------------------------------------------------------------
// Adaptive Blending Factor
// ----------------------------------------------------------------------------------------------------

float GetHistoryAlpha(TEXTURE2D(tex), float2 historyUV)
{
    return SAMPLE_TEXTURE2D_LOD(tex, sampler_LinearClamp, historyUV, 0).a;
}

float GetLumaContrastWeightedBlendFactor(float blendFactor, float minNeighbourLuma, float maxNeighbourLuma, float historyLuma, float2 contrastThreshold, inout float accumulatedLumaContrast)
{
    float lumaContrast = max(maxNeighbourLuma - minNeighbourLuma, 0);
    accumulatedLumaContrast = lerp(lumaContrast, accumulatedLumaContrast, 0.95);
    float threshold = max(contrastThreshold.x, historyLuma * contrastThreshold.y);
    float lumaFactor = saturate(accumulatedLumaContrast - threshold);
    blendFactor = lerp(blendFactor, 0.98, lumaFactor);
    return blendFactor;
}

float GetVelocityWeightedBlendFactor(float blendFactor, float2 velocity)
{
    float velocityFactor = dot(velocity, velocity);
    blendFactor = lerp(blendFactor, 0, saturate(velocityFactor * 10));
    return blendFactor;
}

// ----------------------------------------------------------------------------------------------------
// Exponential Moving Average
// ----------------------------------------------------------------------------------------------------

float3 SimpleExponentialAccumulation(float3 history, float3 current, float blendFactor)
{
    return lerp(current, history, blendFactor);
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