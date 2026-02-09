#ifndef YPIPELINE_UNITY_APV_LIBRARY_INCLUDED
#define YPIPELINE_UNITY_APV_LIBRARY_INCLUDED

// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl" // IBLLibrary 改写了 EvaluateAmbientProbe 函数
#define __AMBIENTPROBE_HLSL__
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"

// ----------------------------------------------------------------------------------------------------
// APV
// ----------------------------------------------------------------------------------------------------

float3 AddNoiseToSamplingPosition_YPipeline(float3 positionWS, float2 pixelCoord, float3 direction)
{
    float3 right = mul((float3x3)GetViewToWorldMatrix(), float3(1.0, 0.0, 0.0));
    float3 top = mul((float3x3)GetViewToWorldMatrix(), float3(0.0, 1.0, 0.0));
    
    float2 frameMagicScale = k_Halton[_APVFrameIndex % 64 + 1];
    int2 sampleCoord = (pixelCoord + _APVFrameIndex * frameMagicScale) % _STBN128Scalar3_TexelSize.zw;
    float3 noise = LOAD_TEXTURE2D_LOD(_STBN128Scalar3, sampleCoord, 0).rgb;
    direction += top * (noise.y - 0.5) + right * (noise.z - 0.5);
    return positionWS + noise.x * _APVSamplingNoise * direction;
}

void EvaluateAdaptiveProbeVolume_YPipeline(float3 positionWS, float3 normalWS, float3 viewDir, float2 pixelCoord, uint renderingLayer, out float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    positionWS = AddNoiseToSamplingPosition_YPipeline(positionWS, pixelCoord, viewDir);

    APVSample apvSample = SampleAPV(positionWS, normalWS, renderingLayer, viewDir);
    EvaluateAdaptiveProbeVolume(apvSample, normalWS, bakeDiffuseLighting);
}

float3 SampleProbeVolume(float3 positionWS, float3 normalWS, float3 viewDir, float2 pixelCoord)
{
    float3 irradiance;
    EvaluateAdaptiveProbeVolume_YPipeline(positionWS, normalWS, viewDir, pixelCoord, 0, irradiance);
    return irradiance;
}

float3 CalculateIndirectDiffuse_ProbeVolume(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 irradiance = SampleProbeVolume(geometryParams.positionWS, standardPBRParams.N, standardPBRParams.V, geometryParams.pixelCoord);
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 Diffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return Diffuse;
}

#endif