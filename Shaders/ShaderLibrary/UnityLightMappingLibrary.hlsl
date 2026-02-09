#ifndef YPIPELINE_UNITY_LIGHT_MAPPING_LIBRARY_INCLUDED
#define YPIPELINE_UNITY_LIGHT_MAPPING_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

// ----------------------------------------------------------------------------------------------------
// Light Map
// ----------------------------------------------------------------------------------------------------

float3 SampleLightMap(float2 lightMapUV)
{
    // #if defined(LIGHTMAP_ON)
    //     return SampleSingleLightmap(unity_Lightmap, LIGHTMAP_SAMPLER_NAME, lightMapUV, float4(1, 1, 0, 0), true);
    // #elif defined(DYNAMICLIGHTMAP_ON)
    //     return XXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // #else
    //     return float3(0, 0, 0);
    // #endif
    return SampleSingleLightmap(unity_Lightmap, samplerunity_Lightmap, lightMapUV, float4(1, 1, 0, 0), true);
}

float3 CalculateIndirectDiffuse_LightMap(float2 lightMapUV, in StandardPBRParams standardPBRParams, float envBRDF_Diffuse)
{
    float3 irradiance = SampleLightMap(lightMapUV);
    float3 envBRDFDiffuse = standardPBRParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - standardPBRParams.metallic;
    float3 Diffuse = irradiance * envBRDFDiffuse * Kd * standardPBRParams.ao;
    return Diffuse;
}

// ----------------------------------------------------------------------------------------------------
// Shadowmask - Deprecated （已弃用）
// ----------------------------------------------------------------------------------------------------

// float SampleShadowmask(float2 lightMapUV, float channel)
// {
//     // if (channel < 0) return 1.0;
//     float isNotInShadowmask = channel < 0.0;
//     float attenuation = 1.0;
//     
//     #if defined(LIGHTMAP_ON)
//         attenuation = SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, lightMapUV)[channel];
//     #else
//         attenuation = unity_ProbesOcclusion[channel];
//     #endif
//     
//     return lerp(attenuation, 1.0, isNotInShadowmask);
// }
//
// // For Shadowmask - Distance Shadowmask Mode
// float MixBakedAndRealtimeShadows(float2 lightMapUV, int channel, float realtimeShadowAttenuation, float realtimeShadowFade)
// {
//     float bakedShadowAttenuation = SampleShadowmask(lightMapUV, channel);
//     float shadowAttenuation = lerp(bakedShadowAttenuation, realtimeShadowAttenuation, realtimeShadowFade);
//     return shadowAttenuation;
// }
//
// // For Shadowmask - Shadowmask Mode
// float ChooseBakedAndRealtimeShadows(float2 lightMapUV, int channel, float realtimeShadowAttenuation, float realtimeShadowFade)
// {
//     float bakedShadowAttenuation = SampleShadowmask(lightMapUV, channel);
//     realtimeShadowAttenuation = lerp(1.0, realtimeShadowAttenuation, realtimeShadowFade);
//     return min(bakedShadowAttenuation, realtimeShadowAttenuation);
// }

#endif