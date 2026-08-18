#ifndef YPIPELINE_IMAGE_BASED_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_IMAGE_BASED_LIGHTING_LIBRARY_INCLUDED

#include "BRDFModelLib.hlsl"
#include "SphericalHarmonicsLib.hlsl"

// ----------------------------------------------------------------------------------------------------
// Spherical Harmonics(SH)
// ----------------------------------------------------------------------------------------------------

float3 EvaluateAmbientProbe(float3 N) // 名字不能乱改，该函数覆写掉了 unity 自带的 EvaluateAmbientProbe 函数
{
    float3 L0L1;
    float4 vA = float4(N, 1.0);
    L0L1.r = dot(_AmbientProbe[0], vA);
    L0L1.g = dot(_AmbientProbe[2], vA);
    L0L1.b = dot(_AmbientProbe[4], vA);

    float3 L2;
    float4 vB = N.xyzz * N.yzzx;
    L2.r = dot(_AmbientProbe[1], vB);
    L2.g = dot(_AmbientProbe[3], vB);
    L2.b = dot(_AmbientProbe[5], vB);
    
    float vC = N.x * N.x - N.y * N.y;
    L2 += _AmbientProbe[6].rgb * vC;

    return L0L1 + L2;
}

// float3 SampleSphericalHarmonics(float3 N)
// {
//     float3 L0L1;
//     float4 vA = float4(N, 1.0);
//     L0L1.r = dot(unity_SHAr, vA);
//     L0L1.g = dot(unity_SHAg, vA);
//     L0L1.b = dot(unity_SHAb, vA);
//
//     float3 L2;
//     float4 vB = N.xyzz * N.yzzx;
//     L2.r = dot(unity_SHBr, vB);
//     L2.g = dot(unity_SHBg, vB);
//     L2.b = dot(unity_SHBb, vB);
//     
//     float vC = N.x * N.x - N.y * N.y;
//     L2 += unity_SHC.rgb * vC;
//
//     return L0L1 + L2;
// }

float3 EvaluateRawAmbientProbe(float3 N) // 得到乘上 intensity 前的 SH
{
    float3 L0L1;
    float4 vA = float4(N, 1.0);
    L0L1.r = dot(_AmbientProbe[0], vA);
    L0L1.g = dot(_AmbientProbe[2], vA);
    L0L1.b = dot(_AmbientProbe[4], vA);

    float3 L2;
    float4 vB = N.xyzz * N.yzzx;
    L2.r = dot(_AmbientProbe[1], vB);
    L2.g = dot(_AmbientProbe[3], vB);
    L2.b = dot(_AmbientProbe[5], vB);
    
    float vC = N.x * N.x - N.y * N.y;
    L2 += _AmbientProbe[6].rgb * vC;

    return (L0L1 + L2) / (_AmbientProbe[6].a + 1e-6);
}

// ----------------------------------------------------------------------------------------------------
// IBL Utilities
// ----------------------------------------------------------------------------------------------------

inline float3 SampleEnvLut(Texture2D envLut, SamplerState envLutSampler, float NoV, float roughness)
{
    return SAMPLE_TEXTURE2D(envLut, envLutSampler, float2(NoV, roughness)).rgb;
}

inline float3 DecodeHDR(float4 encoded, float4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    float alpha = max(decodeInstructions.w * (encoded.a - 1.0) + 1.0, 0.0);
    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encoded.rgb;
}

inline float3 SampleCubemap(TextureCube cubemap, SamplerState cubemapSampler, float3 dir, float mipmap)
{
    return SAMPLE_TEXTURECUBE_LOD(cubemap, cubemapSampler, dir, mipmap).rgb;
}

inline float3 SampleHDRCubemap(TextureCube cubemap, SamplerState cubemapSampler, float3 dir, float mipmap, float4 decodeInstructions)
{
    float4 env = SAMPLE_TEXTURECUBE_LOD(cubemap, cubemapSampler, dir, mipmap);
    return DecodeHDR(env, decodeInstructions);
}

inline float3 SampleGlobalReflectionProbe(float3 dir, float mipmap)
{
    float4 env = SAMPLE_TEXTURECUBE_LOD(_GlobalReflectionProbe, sampler_GlobalReflectionProbe, dir, mipmap);
    return DecodeHDR(env, _GlobalReflectionProbe_HDR);
}

inline float RoughnessToMipmapLevel(float roughness, float maxMipLevel)
{
    // roughness = roughness * (1.7 - 0.7 * roughness);
    return roughness * maxMipLevel;
}

// ----------------------------------------------------------------------------------------------------
// IBL Calculation -- Old Version & Deprecated，BUT DO NOT DELETE！！！！！！！！！！
// 下面函数都不再使用了，但不要删除！！！！！！！！！！！
// ----------------------------------------------------------------------------------------------------

float3 CalculateIndirectDiffuse_IBL(in StandardMaterialParams stdMatParams, float envBRDF_Diffuse)
{
    float3 irradiance = EvaluateAmbientProbe(stdMatParams.N);
    float3 envBRDFDiffuse = stdMatParams.albedo * envBRDF_Diffuse;
    float Kd = 1.0 - stdMatParams.metallic;
    float3 IBLDiffuse = irradiance * envBRDFDiffuse * Kd * stdMatParams.ao;
    return IBLDiffuse;
}

float3 CalculateIndirectSpecular_IBL(in StandardMaterialParams stdMatParams, TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler, float2 envBRDF_Specular, float3 energyCompensation)
{
    float3 R = reflect(-stdMatParams.V, stdMatParams.N);
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, R, 6.0 * stdMatParams.roughness).rgb;
    //float3 prefilteredColor = SampleHDRCubemap(prefilteredEnvMap, prefilteredEnvMapSampler, R, 6.0 * stdMatParams.roughness);
    //float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, stdMatParams.F0);
    float3 envBRDFSpecular = envBRDF_Specular.xxx * stdMatParams.F0 + (float3(stdMatParams.F90, stdMatParams.F90, stdMatParams.F90) - stdMatParams.F0) * envBRDF_Specular.yyy;
    float3 IBLSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(stdMatParams.NoV, stdMatParams.ao, stdMatParams.alphaRoughness);
    return IBLSpecular;
}

float3 CalculateIndirectSpecular_IBL_RemappedMipmap(in StandardMaterialParams stdMatParams, TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler, float2 envBRDF_Specular, float3 energyCompensation)
{
    float3 R = reflect(-stdMatParams.V, stdMatParams.N);
    float mipmap = RoughnessToMipmapLevel(stdMatParams.roughness, 6.0);
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, R, mipmap).rgb;
    //float3 prefilteredColor = SampleHDRCubemap(prefilteredEnvMap, prefilteredEnvMapSampler, R, mipmap);
    //float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, stdMatParams.F0);
    float3 envBRDFSpecular = envBRDF_Specular.xxx * stdMatParams.F0 + (float3(stdMatParams.F90, stdMatParams.F90, stdMatParams.F90) - stdMatParams.F0) * envBRDF_Specular.yyy;
    float3 IBLSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(stdMatParams.NoV, stdMatParams.ao, stdMatParams.alphaRoughness);
    return IBLSpecular;
}

float3 CalculateIBL(StandardMaterialParams stdMatParams, TextureCube prefilteredEnvMap, SamplerState prefilteredEnvMapSampler,
    Texture2D envLut, SamplerState envLutSampler, out float3 energyCompensation)
{
    float3 envBRDF = SAMPLE_TEXTURE2D(envLut, envLutSampler, float2(stdMatParams.NoV, stdMatParams.roughness)).rgb;
    
    float3 irradiance = EvaluateAmbientProbe(stdMatParams.N);
    float3 envBRDFDiffuse = stdMatParams.albedo * envBRDF.b;
    float Kd = 1.0 - stdMatParams.metallic;
    float3 IBLDiffuse = irradiance * envBRDFDiffuse * Kd * stdMatParams.ao;
    
    float3 R = reflect(-stdMatParams.V, stdMatParams.N);
    float3 prefilteredColor = SAMPLE_TEXTURECUBE_LOD(prefilteredEnvMap, prefilteredEnvMapSampler, R, 6.0 * stdMatParams.roughness).rgb;
    //float3 envBRDFSpecular = lerp(envBRDF.yyy, envBRDF.xxx, stdMatParams.F0);
    float3 envBRDFSpecular = envBRDF.xxx * stdMatParams.F0 + (float3(stdMatParams.F90, stdMatParams.F90, stdMatParams.F90) - stdMatParams.F0) * envBRDF.yyy;
    energyCompensation = 1.0 + stdMatParams.F0 * (1.0 / envBRDF.x - 1) / 2;
    float3 IBLSpecular = prefilteredColor * envBRDFSpecular * energyCompensation * ComputeSpecularAO(stdMatParams.NoV, stdMatParams.ao, stdMatParams.alphaRoughness);

    float3 IBL = IBLDiffuse + IBLSpecular;
    return IBL;
}

#endif