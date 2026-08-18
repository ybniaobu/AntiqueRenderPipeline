#ifndef YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_INDIRECT_LIGHTING_LIBRARY_INCLUDED

#include "ImageBasedLightingLib.hlsl"
#include "ReflectionProbeLib.hlsl"
// #include "../Unity/UnityLightMappingLib.hlsl"
#include "../Unity/UnityAPVLib.hlsl"

// ----------------------------------------------------------------------------------------------------
// Diffuse Indirect Lighting
// ----------------------------------------------------------------------------------------------------

#if defined(_SCREEN_SPACE_IRRADIANCE)
#define DIFFUSE_INDIRECT_LIGHTING(geoParams, stdMatParams, envBRDF_Diffuse, irradiance) DiffuseIndirectLighting_ScreenSpaceIrradiance(geoParams.screenUV, stdMatParams.albedo, stdMatParams.metallic, stdMatParams.ao, envBRDF_Diffuse, irradiance)
#elif defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
#define DIFFUSE_INDIRECT_LIGHTING(geoParams, stdMatParams, envBRDF_Diffuse, irradiance) DiffuseIndirectLighting_APV(geoParams.positionWS, stdMatParams.N, stdMatParams.V, geoParams.pixelCoord, stdMatParams.albedo, stdMatParams.metallic, stdMatParams.ao, envBRDF_Diffuse, irradiance)
// #elif defined(LIGHTMAP_ON)
// #define DIFFUSE_INDIRECT_LIGHTING(geoParams, stdMatParams, envBRDF_Diffuse, irradiance) DiffuseIndirectLighting_LightMap(geoParams.lightMapUV, stdMatParams.albedo, stdMatParams.metallic, stdMatParams.ao, envBRDF_Diffuse, irradiance)
#else
#define DIFFUSE_INDIRECT_LIGHTING(geoParams, stdMatParams, envBRDF_Diffuse, irradiance) DiffuseIndirectLighting_AmbientProbe(stdMatParams.N, stdMatParams.albedo, stdMatParams.metallic, stdMatParams.ao, envBRDF_Diffuse, irradiance)
#endif

// 对 Irradiance 应用漫反射反射方程得到 Radiance
inline float3 ApplyDiffuseBRDF(float3 irradiance, float3 albedo, float metallic, float ao, float envBRDF_Diffuse)
{
    float3 envBRDFDiffuse = albedo * envBRDF_Diffuse;
    float Kd = 1.0 - metallic;
    float3 indirectDiffuse = irradiance * envBRDFDiffuse * Kd * ao;
    return indirectDiffuse;
}

inline float3 DiffuseIndirectLighting_ScreenSpaceIrradiance(float2 screenUV, float3 albedo, float metallic, float ao, float envBRDF_Diffuse, out float3 irradiance)
{
    irradiance = SAMPLE_TEXTURE2D_LOD(_IrradianceTexture, sampler_PointClamp, screenUV, 0).rgb;
    return ApplyDiffuseBRDF(irradiance, albedo, metallic, ao, envBRDF_Diffuse);
}

inline float3 DiffuseIndirectLighting_APV(float3 positionWS, float3 normalWS, float3 viewDir, float2 pixelCoord, float3 albedo, float metallic, float ao, float envBRDF_Diffuse, out float3 irradiance)
{
    irradiance = SampleProbeVolume(positionWS, normalWS, viewDir, pixelCoord);
    return ApplyDiffuseBRDF(irradiance, albedo, metallic, ao, envBRDF_Diffuse);
}

// inline float3 DiffuseIndirectLighting_LightMap(float2 lightMapUV, float3 albedo, float metallic, float ao, float envBRDF_Diffuse, out float3 irradiance)
// {
//     irradiance = SampleLightMap(lightMapUV);
//     return ApplyDiffuseBRDF(irradiance, albedo, metallic, ao, envBRDF_Diffuse);
// }

inline float3 DiffuseIndirectLighting_AmbientProbe(float3 normalWS, float3 albedo, float metallic, float ao, float envBRDF_Diffuse, out float3 irradiance)
{
    irradiance = EvaluateAmbientProbe(normalWS);
    return ApplyDiffuseBRDF(irradiance, albedo, metallic, ao, envBRDF_Diffuse);
}

// ----------------------------------------------------------------------------------------------------
// Specular Indirect Lighting
// ----------------------------------------------------------------------------------------------------

#if defined(_EDITOR_PREVIEW)
#define SPECULAR_INDIRECT_LIGHTING(geoParams, stdMatParams, irradiance, envBRDF_Specular, energyCompensation) SpecularIndirectLighting_EditorPreview(stdMatParams.F0, stdMatParams.F90, stdMatParams.V, stdMatParams.N, stdMatParams.NoV, stdMatParams.ao, stdMatParams.roughness, stdMatParams.alphaRoughness, envBRDF_Specular, energyCompensation)
#else
#define SPECULAR_INDIRECT_LIGHTING(geoParams, stdMatParams, irradiance, envBRDF_Specular, energyCompensation) SpecularIndirectLighting_ReflectionProbe(geoParams.screenUV, geoParams.positionWS, stdMatParams.F0, stdMatParams.F90, stdMatParams.V, stdMatParams.N, stdMatParams.NoV, stdMatParams.ao, stdMatParams.roughness, stdMatParams.alphaRoughness, irradiance, envBRDF_Specular, energyCompensation)
#endif

inline float3 ApplySpecularBRDF(float3 prefilteredColor, float3 F0, float F90, float NoV, float ao, float alphaRoughness, float2 envBRDF_Specular, float3 energyCompensation)
{
    // float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, F0);
    float3 envBRDFSpecular = envBRDF_Specular.xxx * F0 + (float3(F90, F90, F90) - F0) * envBRDF_Specular.yyy;
    float3 indirectSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(NoV, ao, alphaRoughness);
    return indirectSpecular;
}

inline float3 SpecularIndirectLighting_EditorPreview(float3 F0, float F90, float3 V, float3 N, float NoV, float ao, float roughness, float alphaRoughness, float2 envBRDF_Specular, float3 energyCompensation)
{
    float3 R = reflect(-V, N);
    float3 prefilteredColor = SampleCubemap(unity_SpecCube0, samplerunity_SpecCube0, R, RoughnessToMipmapLevel(roughness, 6.0));
    return ApplySpecularBRDF(prefilteredColor, F0, F90, NoV, ao, alphaRoughness, envBRDF_Specular, energyCompensation);
}

inline float3 SpecularIndirectLighting_ReflectionProbe(float2 screenUV, float3 positionWS, float3 F0, float F90, float3 V, float3 N, float NoV, float ao, float roughness, float alphaRoughness, float3 irradiance, float2 envBRDF_Specular, float3 energyCompensation)
{
    float3 R = reflect(-V, N);
    // float3 prefilteredColor = EvaluateSingleReflectionProbe(screenUV, positionWS, roughness, R, irradiance);
    float3 prefilteredColor = EvaluateAndBlendingTwoReflectionProbes(screenUV, positionWS, roughness, R, irradiance);
    return ApplySpecularBRDF(prefilteredColor, F0, F90, NoV, ao, alphaRoughness, envBRDF_Specular, energyCompensation);
}

// ----------------------------------------------------------------------------------------------------
// Anisotropic Specular Indirect Lighting
// ----------------------------------------------------------------------------------------------------

#if defined(_EDITOR_PREVIEW)
#define SPECULAR_INDIRECT_LIGHTING_ANISO(geoParams, advMatParams, irradiance, envBRDF_Specular, energyCompensation) AnisotropicSpecularIndirectLighting_EditorPreview(advMatParams.F0, advMatParams.F90, advMatParams.anisotropy, advMatParams.anisotropicB, advMatParams.V, advMatParams.N, advMatParams.NoV, advMatParams.ao, advMatParams.roughness, advMatParams.alphaRoughness, envBRDF_Specular, energyCompensation)
#else
#define SPECULAR_INDIRECT_LIGHTING_ANISO(geoParams, advMatParams, irradiance, envBRDF_Specular, energyCompensation) AnisotropicSpecularIndirectLighting_ReflectionProbe(geoParams.screenUV, geoParams.positionWS, advMatParams.F0, advMatParams.F90, advMatParams.anisotropy, advMatParams.anisotropicB, advMatParams.V, advMatParams.N, advMatParams.NoV, advMatParams.ao, advMatParams.roughness, advMatParams.alphaRoughness, irradiance, envBRDF_Specular, energyCompensation)
#endif

inline float3 AnisotropicReflectionVector(float anisotropy, float3 anisotropicB, float3 V, float3 N)
{
    float3 bentNormal = cross(anisotropicB, V);
    bentNormal = normalize(cross(bentNormal, anisotropicB));
    bentNormal = normalize(lerp(N, bentNormal, anisotropy));
    float3 R = reflect(-V, bentNormal);
    return R;
}

inline float3 AnisotropicSpecularIndirectLighting_EditorPreview(float3 F0, float F90, float anisotropy, float3 anisotropicB, float3 V, float3 N, float NoV, float ao, float roughness, float alphaRoughness, float2 envBRDF_Specular, float3 energyCompensation)
{
    float3 R = AnisotropicReflectionVector(anisotropy, anisotropicB, V, N);
    float3 prefilteredColor = SampleCubemap(unity_SpecCube0, samplerunity_SpecCube0, R, RoughnessToMipmapLevel(roughness, 6.0));
    return ApplySpecularBRDF(prefilteredColor, F0, F90, NoV, ao, alphaRoughness, envBRDF_Specular, energyCompensation);
}

inline float3 AnisotropicSpecularIndirectLighting_ReflectionProbe(float2 screenUV, float3 positionWS, float3 F0, float F90, float anisotropy, float3 anisotropicB, float3 V, float3 N, float NoV, float ao, float roughness, float alphaRoughness, float3 irradiance, float2 envBRDF_Specular, float3 energyCompensation)
{
    float3 R = AnisotropicReflectionVector(anisotropy, anisotropicB, V, N);
    // float3 prefilteredColor = EvaluateSingleReflectionProbe(screenUV, positionWS, roughness, R, irradiance);
    float3 prefilteredColor = EvaluateAndBlendingTwoReflectionProbes(screenUV, positionWS, roughness, R, irradiance);
    return ApplySpecularBRDF(prefilteredColor, F0, F90, NoV, ao, alphaRoughness, envBRDF_Specular, energyCompensation);
}

// ----------------------------------------------------------------------------------------------------
// Clear Coat Specular Indirect Lighting
// ----------------------------------------------------------------------------------------------------

#if defined(_EDITOR_PREVIEW)
#define SPECULAR_INDIRECT_LIGHTING_CLEARCOAT(geoParams, advMatParams, irradiance) ClearCoatSpecularIndirectLighting_EditorPreview(advMatParams.V, advMatParams.clearCoatN, advMatParams.clearCoatRoughness)
#else 
#define SPECULAR_INDIRECT_LIGHTING_CLEARCOAT(geoParams, advMatParams, irradiance) ClearCoatSpecularIndirectLighting_ReflectionProbe(geoParams.screenUV, geoParams.positionWS, advMatParams.V, advMatParams.clearCoatN, advMatParams.clearCoatRoughness, irradiance)
#endif

inline float3 ClearCoatSpecularIndirectLighting_EditorPreview(float3 V, float3 clearCoatN, float clearCoatRoughness)
{
    float3 clearCoatR = reflect(-V, clearCoatN);
    float3 prefilteredColor = SampleCubemap(unity_SpecCube0, samplerunity_SpecCube0, clearCoatR, RoughnessToMipmapLevel(clearCoatRoughness, 6.0));
    return prefilteredColor;
}

inline float3 ClearCoatSpecularIndirectLighting_ReflectionProbe(float2 screenUV, float3 positionWS, float3 V, float3 clearCoatN, float clearCoatRoughness, float3 irradiance)
{
    float3 clearCoatR = reflect(-V, clearCoatN);
    // float3 prefilteredColor = EvaluateSingleReflectionProbe(screenUV, positionWS, clearCoatRoughness, clearCoatR, irradiance);
    float3 prefilteredColor = EvaluateAndBlendingTwoReflectionProbes(screenUV, positionWS, clearCoatRoughness, clearCoatR, irradiance);
    return prefilteredColor;
}

#endif