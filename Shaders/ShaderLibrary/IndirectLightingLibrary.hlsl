#ifndef YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED

#include "IBLLibrary.hlsl"
#include "ReflectionProbeLibrary.hlsl"

#if defined(LIGHTMAP_ON)
#include "UnityLightMappingLibrary.hlsl"
#endif

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
#include "UnityAPVLibrary.hlsl"
#endif

// ----------------------------------------------------------------------------------------------------
// Light Map Macros
// ----------------------------------------------------------------------------------------------------

#if defined(LIGHTMAP_ON)
    #define LIGHTMAP_UV(index)                      float2 lightMapUV : TEXCOORD##index;
    #define TRANSFER_LIGHTMAP_UV(IN, OUT)           OUT.lightMapUV = IN.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    #define TRANSFER_GEOMETRY_PARAMS_LIGHTMAP_UV    geometryParams.lightMapUV = IN.lightMapUV;
#else
    #define LIGHTMAP_UV(index)
    #define TRANSFER_LIGHTMAP_UV(IN, OUT)
    #define TRANSFER_GEOMETRY_PARAMS_LIGHTMAP_UV
#endif

// ----------------------------------------------------------------------------------------------------
// Diffuse Indirect Lighting
// ----------------------------------------------------------------------------------------------------

// 对 Irradiance 应用漫反射反射方程得到 Radiance，即 Diffuse Indirect Lighting
inline float3 ApplyDiffuseBRDF(float3 irradiance, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 indirectDiffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return indirectDiffuse;
}

// TODO: 改为 Macro 形式
inline float3 DiffuseIndirectLighting(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse, out float3 irradiance)
{
    #if defined(_SCREEN_SPACE_IRRADIANCE)
        irradiance = SAMPLE_TEXTURE2D_LOD(_IrradianceTexture, sampler_LinearClamp, geometryParams.screenUV, 0).rgb;
    #elif defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
        irradiance = SampleProbeVolume(geometryParams.positionWS, standardPBRParams.N, standardPBRParams.V, geometryParams.pixelCoord);
    #elif defined(LIGHTMAP_ON)
        irradiance = SampleLightMap(geometryParams.lightMapUV);
    #else
        irradiance = EvaluateAmbientProbe(standardPBRParams.N);
    #endif
    
    return ApplyDiffuseBRDF(irradiance, standardPBRParams, envBRDF_Diffuse);
}

// ----------------------------------------------------------------------------------------------------
// Specular Indirect Lighting
// ----------------------------------------------------------------------------------------------------

inline float3 ApplySpecularBRDF(float3 prefilteredColor, in StandardPBRParams standardPBRParams, float2 envBRDF_Specular, float3 energyCompensation)
{
    // float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, standardPBRParams.F0);
    float3 envBRDFSpecular = envBRDF_Specular.xxx * standardPBRParams.F0 + (float3(standardPBRParams.F90, standardPBRParams.F90, standardPBRParams.F90) - standardPBRParams.F0) * envBRDF_Specular.yyy;
    float3 indirectSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(standardPBRParams.NoV, standardPBRParams.ao, standardPBRParams.roughness);
    indirectSpecular *= ComputeHorizonSpecularOcclusion(standardPBRParams.R, standardPBRParams.N);
    return indirectSpecular;
}

inline float3 SpecularIndirectLighting(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float3 irradiance, float2 envBRDF_Specular, float3 energyCompensation)
{
    // float3 prefilteredColor = EvaluateSingleReflectionProbe(geometryParams, standardPBRParams, irradiance);
    float3 prefilteredColor = EvaluateAndBlendingTwoReflectionProbes(geometryParams, standardPBRParams, irradiance);
    return ApplySpecularBRDF(prefilteredColor, standardPBRParams, envBRDF_Specular, energyCompensation);
}

#endif