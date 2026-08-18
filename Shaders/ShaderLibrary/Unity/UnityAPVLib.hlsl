#ifndef YPIPELINE_UNITY_APV_LIBRARY_INCLUDED
#define YPIPELINE_UNITY_APV_LIBRARY_INCLUDED

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl" // ImageBasedLightingLib 改写了 EvaluateAmbientProbe 函数
#define __AMBIENTPROBE_HLSL__
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

// ----------------------------------------------------------------------------------------------------
// APV
// ----------------------------------------------------------------------------------------------------

float3 AddNoiseToSamplingPosition_YPipeline(float3 positionWS, float2 pixelCoord, float3 direction)
{
    // float3 right = mul((float3x3)GetViewToWorldMatrix(), float3(1.0, 0.0, 0.0));
    // float3 top = mul((float3x3)GetViewToWorldMatrix(), float3(0.0, 1.0, 0.0));
    
    uint3 dimensions;
    _BlueNoise3D.GetDimensions(dimensions.x, dimensions.y, dimensions.z);
    int3 sampleCoord = int3(pixelCoord % dimensions.xy, _APVFrameIndex % dimensions.z);
    float3 noise = LOAD_TEXTURE3D_LOD(_BlueNoise3D, sampleCoord, 0).rgb;
    // direction += top * (noise.y - 0.5) + right * (noise.z - 0.5);
    return positionWS + noise.x * _APVSamplingNoise * direction;
}

void EvaluateAdaptiveProbeVolume_YPipeline(float3 positionWS, float3 normalWS, float3 viewDir, float2 pixelCoord, uint renderingLayer, out float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    positionWS = AddNoiseToSamplingPosition_YPipeline(positionWS, pixelCoord, viewDir);

    APVSample apvSample = SampleAPV(positionWS, normalWS * 1.0001, renderingLayer, viewDir);
    EvaluateAdaptiveProbeVolume(apvSample, normalWS, bakeDiffuseLighting);
}

float3 SampleProbeVolume(float3 positionWS, float3 normalWS, float3 viewDir, float2 pixelCoord)
{
    float3 irradiance;
    EvaluateAdaptiveProbeVolume_YPipeline(positionWS, normalWS, viewDir, pixelCoord, 0, irradiance);
    return irradiance;
}

float3 CalculateIndirectDiffuse_ProbeVolume(in GeometryParams geoParams, in StandardMaterialParams stdMatParams, float envBRDF_Diffuse)
{
    float3 irradiance = SampleProbeVolume(geoParams.positionWS, stdMatParams.N, stdMatParams.V, geoParams.pixelCoord);
    float3 envBRDFDiffuse = stdMatParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - stdMatParams.metallic;
    float3 Diffuse = irradiance * envBRDFDiffuse * Kd * stdMatParams.ao;
    return Diffuse;
}

#endif