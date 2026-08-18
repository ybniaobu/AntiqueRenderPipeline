#ifndef YPIPELINE_SSGI_FALLBACK_INCLUDED
#define YPIPELINE_SSGI_FALLBACK_INCLUDED

float4 _AmbientProbe[7]; // YPipeline 上传的全局 Ambient Probe 球谐数据
TEXTURECUBE(_GlobalReflectionProbe); // YPipeline 上传的全局 Reflection Probe 数据
SAMPLER(sampler_GlobalReflectionProbe);
float4 _GlobalReflectionProbe_HDR;

#include "../../ShaderLibrary/PBR/ImageBasedLightingLib.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl" // ImageBasedLightingLib 改写了 EvaluateAmbientProbe 函数
#define __AMBIENTPROBE_HLSL__
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

// ----------------------------------------------------------------------------------------------------
// Diffuse Fallback -- APV
// ----------------------------------------------------------------------------------------------------

void EvaluateAdaptiveProbeVolume(float3 positionWS, float3 normalWS, float3 viewDir, float3 noise, uint renderingLayer, out float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    positionWS = positionWS + noise.x * _APVSamplingNoise * viewDir;
    APVSample apvSample = SampleAPV(positionWS, normalWS * 1.0001, renderingLayer, viewDir);
    EvaluateAdaptiveProbeVolume(apvSample, normalWS, bakeDiffuseLighting);
}

void EvaluateAdaptiveProbeVolume_BentNormal(float3 positionWS, float3 normalWS, float3 bentNormal, float3 noise, uint renderingLayer, out float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    positionWS = positionWS + noise.x * _APVSamplingNoise * bentNormal;
    APVSample apvSample = SampleAPV(positionWS, normalWS * 1.0001, renderingLayer, bentNormal);
    EvaluateAdaptiveProbeVolume(apvSample, bentNormal, bakeDiffuseLighting);
}

float3 SampleProbeVolume(float3 positionWS, float3 normalWS, float3 viewDir, float3 noise)
{
    float3 irradiance;
    EvaluateAdaptiveProbeVolume(positionWS, normalWS, viewDir, noise, 0, irradiance);
    return irradiance;
}

float3 SampleProbeVolume_BentNormal(float3 positionWS, float3 normalWS, float3 bentNormal, float3 noise)
{
    float3 irradiance;
    EvaluateAdaptiveProbeVolume_BentNormal(positionWS, normalWS, bentNormal, noise, 0, irradiance);
    return irradiance;
}

float3 FallbackAmbientProbe(float3 bentNormal)
{
    return EvaluateAmbientProbe(bentNormal);
}

float3 FallbackAPV(float3 positionWS, float3 normalWS, float3 bentNormal,  float3 noise)
{
    #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        return SampleProbeVolume_BentNormal(positionWS, normalWS, bentNormal, noise);
    #else
        return EvaluateAmbientProbe(bentNormal);
    #endif
}

#endif