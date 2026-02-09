#ifndef YPIPELINE_TAA_PASS_INCLUDED
#define YPIPELINE_TAA_PASS_INCLUDED

#include "CopyPass.hlsl"
#include "../ShaderLibrary/AntiAliasing/TAA.hlsl"

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_MotionVectorTexture);
TEXTURE2D(_TAAHistory);

float4 _TAAParams; // x: history blend factor, y: variance clipping critical value, z: fixed luma contrast threshold, w: relative luma contrast threshold

float4 TAAFrag_AABBClamp(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------
    
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    float2 velocity = LOAD_TEXTURE2D_LOD(_MotionVectorTexture, velocityPixelCoord, 0).rg;
    
    // ------------------------- Filter Resampled History -------------------------

    float2 historyUV = IN.uv - velocity;
    float3 history = SampleHistory(_TAAHistory, historyUV);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    samples.filteredM = FilterMiddleColor(samples);

    // ------------------------- Build AABB -------------------------
    
    #if _TAA_VARIANCE
    VarianceNeighbourhood(samples, _TAAParams.y);
    #else
    MinMaxNeighbourhood(samples);
    #endif
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    float3 clampedHistory = NeighborhoodAABBClamp(samples, history);

    // ------------------------- Adaptive Blending Factor -------------------------

    float blendFactor = _TAAParams.x + 0.025;

    // Luma Weighted
    float historyLuma = GetLuma(history);
    float minLuma = GetLuma(samples.min);
    float maxLuma = GetLuma(samples.max);
    float accumulatedLumaContrast = GetHistoryAlpha(_TAAHistory, historyUV);
    blendFactor = GetLumaContrastWeightedBlendFactor(blendFactor, minLuma, maxLuma, historyLuma, _TAAParams.zw, accumulatedLumaContrast);

    // Velocity Weighted
    blendFactor = GetVelocityWeightedBlendFactor(blendFactor, velocity);

    // ------------------------- Off Screen(Camera Jump) -------------------------

    blendFactor = lerp(blendFactor, 0, any(abs(historyUV - 0.5) > 0.5));

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, blendFactor);
    
    color = OutputColor(color);
    return float4(color, accumulatedLumaContrast);
}

float4 TAAFrag_ClipToAABBCenter(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------
    
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    float2 velocity = LOAD_TEXTURE2D_LOD(_MotionVectorTexture, velocityPixelCoord, 0).rg;

    // ------------------------- Filter Resampled History -------------------------

    float2 historyUV = IN.uv - velocity;
    float3 history = SampleHistory(_TAAHistory, historyUV);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    samples.filteredM = FilterMiddleColor(samples);
    
    // ------------------------- Build AABB -------------------------
    
    #if _TAA_VARIANCE
    VarianceNeighbourhood(samples, _TAAParams.y);
    #else
    MinMaxNeighbourhood(samples);
    #endif
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    float3 clampedHistory = NeighborhoodClipToAABBCenter(samples, history);

    // ------------------------- Adaptive Blending Factor -------------------------

    float blendFactor = _TAAParams.x + 0.025;

    // Luma Weighted
    float historyLuma = GetLuma(history);
    float minLuma = GetLuma(samples.min);
    float maxLuma = GetLuma(samples.max);
    float accumulatedLumaContrast = GetHistoryAlpha(_TAAHistory, historyUV);
    blendFactor = GetLumaContrastWeightedBlendFactor(blendFactor, minLuma, maxLuma, historyLuma, _TAAParams.zw, accumulatedLumaContrast);

    // Velocity Weighted
    blendFactor = GetVelocityWeightedBlendFactor(blendFactor, velocity);

    // ------------------------- Off Screen(Camera Jump) -------------------------

    blendFactor = lerp(blendFactor, 0, any(abs(historyUV - 0.5) > 0.5));

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, blendFactor);
    
    color = OutputColor(color);
    return float4(color, accumulatedLumaContrast);
}

float4 TAAFrag_ClipToFiltered(Varyings IN) : SV_TARGET
{
    // ------------------------- Get closest motion vector -------------------------
    
    float2 velocityPixelCoord = GetClosestDepthPixelCoord(_CameraDepthTexture, IN.positionHCS.xy);
    float2 velocity = LOAD_TEXTURE2D_LOD(_MotionVectorTexture, velocityPixelCoord, 0).rg;

    // ------------------------- Filter Resampled History -------------------------

    float2 historyUV = IN.uv - velocity;
    float3 history = SampleHistory(_TAAHistory, historyUV);

    // ------------------------- Get Neighbourhood Samples -------------------------

    NeighbourhoodSamples samples = (NeighbourhoodSamples) 0;
    GetNeighbourhoodSamples(samples, _BlitTexture, IN.positionHCS.xy);

    // ------------------------- Filter Current Middle Sample -------------------------
    
    samples.filteredM = FilterMiddleColor(samples);

    // ------------------------- Build AABB -------------------------
    
    #if _TAA_VARIANCE
    VarianceNeighbourhood(samples, _TAAParams.y);
    #else
    MinMaxNeighbourhood(samples);
    #endif
    
    // ------------------------- Rectify History by Neighborhood Clamping/Clipping -------------------------

    float3 clampedHistory = NeighborhoodClipToFiltered(samples, history);

    // ------------------------- Adaptive Blending Factor -------------------------
    
    float blendFactor = _TAAParams.x + 0.025;

    // Luma Weighted
    float historyLuma = GetLuma(history);
    float minLuma = GetLuma(samples.min);
    float maxLuma = GetLuma(samples.max);
    float accumulatedLumaContrast = GetHistoryAlpha(_TAAHistory, historyUV);
    blendFactor = GetLumaContrastWeightedBlendFactor(blendFactor, minLuma, maxLuma, historyLuma, _TAAParams.zw, accumulatedLumaContrast);

    // Velocity Weighted
    blendFactor = GetVelocityWeightedBlendFactor(blendFactor, velocity);
    
    // ------------------------- Off Screen(Camera Jump) -------------------------

    blendFactor = lerp(blendFactor, 0, any(abs(historyUV - 0.5) > 0.5));

    // ------------------------- Exponential Blending -------------------------
    
    float3 color = LumaExponentialAccumulation(clampedHistory, samples.filteredM, blendFactor);
    
    color = OutputColor(color);
    return float4(color, accumulatedLumaContrast);
}

#endif