#ifndef YPIPELINE_UPSAMPLE_LIBRARY_INCLUDED
#define YPIPELINE_UPSAMPLE_LIBRARY_INCLUDED

// Gather 的顺序问题可以参考这篇文章：https://wojtsterna.blogspot.com/2018/02/directx-11-hlsl-gatherred.html

// Gather Order (DirectX HLSL)
// -----------    ---------
// | 00 | 10 | -> | w | z | 
// | 01 | 11 | -> | x | y |
// -----------    ---------

static const float4 k_BilinearWeights[4] = 
{
    //     01          11          10          00
    float4(3.0 / 16.0, 1.0 / 16.0, 3.0 / 16.0, 9.0 / 16.0),
    float4(1.0 / 16.0, 3.0 / 16.0, 9.0 / 16.0, 3.0 / 16.0),
    float4(9.0 / 16.0, 3.0 / 16.0, 1.0 / 16.0, 3.0 / 16.0),
    float4(3.0 / 16.0, 9.0 / 16.0, 3.0 / 16.0, 1.0 / 16.0),
};

// ----------------------------------------------------------------------------------------------------
// Depth-Aware Bilateral Upsample Functions
// ----------------------------------------------------------------------------------------------------

// Uniform weight, color3 version (don't consider bilinear weight)
// 使用时注意顺序问题，halfDepths 默认是 Gather 的顺序
float3 DepthAwareBilateralUpsample_Uniform(float depthThreshold, float fullDepth, float4 halfDepths, float3 color01, float3 color11, float3 color10, float3 color00)
{
    float4 weights = abs(1.0 - halfDepths / fullDepth) < depthThreshold;
    float weightSum = weights.x + weights.y + weights.z + weights.w + HALF_MIN;
    float3 weightedColorSum = color01 * weights.x + color11 * weights.y + color10 * weights.z + color00 * weights.w;
    float3 fallBackColor = (color01 + color11 + color10 + color00) / 4;
    return lerp(weightedColorSum / weightSum, fallBackColor, all(weights == 0));
}

// Uniform weight, single channel version (don't consider bilinear weight)
// 使用时注意顺序问题，halfDepths 默认是 Gather 的顺序
float DepthAwareBilateralUpsample_Uniform(float depthThreshold, float fullDepth, float4 halfDepths, float4 values)
{
    float4 weights = abs(1.0 - halfDepths / fullDepth) < depthThreshold;
    float weightSum = weights.x + weights.y + weights.z + weights.w + HALF_MIN;
    float weightedValueSum = dot(values, weights);
    return lerp(weightedValueSum / weightSum, 1, all(weights == 0)); // 无 AO 是 1
}

// Bilinear weight, color3 version
// 使用时注意顺序问题，halfDepths 默认是 Gather 的顺序
float3 DepthAwareBilateralUpsample(float depthThreshold, float fullDepth, float4 halfDepths, float3 color01, float3 color11, float3 color10, float3 color00, int orderIndex)
{
    float4 depthWeights = abs(1.0 - halfDepths / fullDepth) < depthThreshold;
    float4 weights = k_BilinearWeights[orderIndex] * depthWeights;
    float weightSum = weights.x + weights.y + weights.z + weights.w + HALF_MIN;
    float3 weightedColorSum = color01 * weights.x + color11 * weights.y + color10 * weights.z + color00 * weights.w;
    float3 fallBackColor = (color01 + color11 + color10 + color00) / 4;
    return lerp(weightedColorSum / weightSum, fallBackColor, all(weights == 0));
}

// Bilinear weight, single channel version
// 使用时注意顺序问题，halfDepths 默认是 Gather 的顺序
float DepthAwareBilateralUpsample(float depthThreshold, float fullDepth, float4 halfDepths, float4 values, int orderIndex)
{
    float4 depthWeights = abs(1.0 - halfDepths / fullDepth) < depthThreshold;
    float4 weights = k_BilinearWeights[orderIndex] * depthWeights;
    float weightSum = weights.x + weights.y + weights.z + weights.w + HALF_MIN;
    float weightedValueSum = dot(values, weights);
    return lerp(weightedValueSum / weightSum, 1, all(weights == 0)); // 无 AO 是 1
}

// ----------------------------------------------------------------------------------------------------
// Nearest-Depth Upsample Functions
// ----------------------------------------------------------------------------------------------------

// color3 version
float3 NearestDepthUpsample(float fullDepth, float4 halfDepths, float3 color01, float3 color11, float3 color10, float3 color00)
{
    float4 depthDelta = abs(halfDepths - fullDepth);
    float4 nearest = float4(color01, depthDelta.x);
    nearest = lerp(nearest, float4(color11, depthDelta.y), depthDelta.y < nearest.w);
    nearest = lerp(nearest, float4(color10, depthDelta.z), depthDelta.z < nearest.w);
    nearest = lerp(nearest, float4(color00, depthDelta.w), depthDelta.w < nearest.w);
    return nearest.xyz;
}

// single channel version
float NearestDepthUpsample(float fullDepth, float4 halfDepths, float4 values)
{
    float4 depthDelta = abs(halfDepths - fullDepth);
    float2 nearest = float2(values.x, depthDelta.x);
    nearest = lerp(nearest, float2(values.y, depthDelta.y), depthDelta.y < nearest.y);
    nearest = lerp(nearest, float2(values.z, depthDelta.z), depthDelta.z < nearest.y);
    nearest = lerp(nearest, float2(values.w, depthDelta.w), depthDelta.w < nearest.y);
    return nearest.x;
}

#endif