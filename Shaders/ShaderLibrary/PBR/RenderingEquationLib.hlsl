#ifndef YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED
#define YPIPELINE_RENDERING_EQUATION_LIBRARY_INCLUDED

struct RenderingEquationContent
{
    float3 emission;
    float3 directSunLight;
    float3 directPunctualLights;
    float3 indirectLightDiffuse;
    float3 indirectLightSpecular;
};

float3 CombineRenderingEquationContent(in RenderingEquationContent content)
{
    float3 directLighting = content.directSunLight + content.directPunctualLights;
    float3 indirectLighting = content.indirectLightDiffuse + content.indirectLightSpecular;
    return directLighting + indirectLighting + content.emission;
}

struct GeometryParams
{
    float3 positionWS;
    float3 normalWS; // 这里存储的是未被 normal map 修改过的几何体自带的 normal
    float4 tangentWS;
    float2 uv;
    float2 pixelCoord; // Screen Pixel Coordinate 屏幕像素坐标
    float2 screenUV;
    // float2 lightMapUV;
};

#include "BRDFModelLib.hlsl"
#include "IndirectLightingLib.hlsl"
#include "DirectLightingLib.hlsl"

// ----------------------------------------------------------------------------------------------------
// Shading Functions
// ----------------------------------------------------------------------------------------------------

void StandardPBRShading(in GeometryParams geoParams, in StandardMaterialParams stdMatParams, inout RenderingEquationContent content)
{
    // ------------------------- Emission -------------------------
    
    content.emission = stdMatParams.emission;
    
    // ------------------------- Indirect Lighting -------------------------
    
    float3 envBRDF = SampleEnvLut(ENVIRONMENT_BRDF_LUT, LUT_SAMPLER, stdMatParams.NoV, stdMatParams.roughness);
    float3 energyCompensation = 1.0 + stdMatParams.F0 * (1.0 / envBRDF.x - 1.0) * 0.5; // 0.5 is a magic number
    
    float3 irradiance;
    content.indirectLightDiffuse += DIFFUSE_INDIRECT_LIGHTING(geoParams, stdMatParams, envBRDF.b, irradiance);

    // content.indirectLightSpecular += CalculateIndirectSpecular_IBL(stdMatParams, unity_SpecCube0, samplerunity_SpecCube0, envBRDF.rg, energyCompensation);
    // content.indirectLightSpecular += CalculateIndirectSpecular_IBL_RemappedMipmap(stdMatParams, unity_SpecCube0,samplerunity_SpecCube0, envBRDF.rg, energyCompensation);
    content.indirectLightSpecular += SPECULAR_INDIRECT_LIGHTING(geoParams, stdMatParams, irradiance, envBRDF.rg, energyCompensation);
    
    // ------------------------- Direct Lighting - Sun Light -------------------------
    
    LightParams sunLightParams = (LightParams) 0;
    InitializeSunLightParams(sunLightParams, stdMatParams.V, stdMatParams.N, geoParams.positionWS, geoParams.pixelCoord);

    content.directSunLight += CalculateLightIrradiance(sunLightParams) * StandardBRDF(stdMatParams, sunLightParams.L, sunLightParams.H, energyCompensation);
    
    // ------------------------- Direct Lighting - Punctual Light -------------------------

    #if defined(_EDITOR_PREVIEW) 
    return;
    #endif
    
    LightTile lightTile = (LightTile) 0;
    InitializeLightTile(lightTile, geoParams.pixelCoord);
    
    for (int i = lightTile.headerIndex + 1; i <= lightTile.lastLightIndex; i++)
    {
        uint lightIndex = _TilesLightIndicesBuffer[i];
        
        LightParams punctualLightParams = (LightParams) 0;
        
        if (GetPunctualLightType(lightIndex) == SPOT_LIGHT) InitializeSpotLightParams(punctualLightParams, lightIndex, stdMatParams.V, stdMatParams.N, geoParams.positionWS, geoParams.pixelCoord);
        else if (GetPunctualLightType(lightIndex) == POINT_LIGHT) InitializePointLightParams(punctualLightParams, lightIndex, stdMatParams.V, stdMatParams.N, geoParams.positionWS, geoParams.pixelCoord);
    
        content.directPunctualLights += CalculateLightIrradiance(punctualLightParams) * StandardBRDF(stdMatParams, punctualLightParams.L, punctualLightParams.H, energyCompensation);
    }
    
    // int punctualLightCount = GetPunctualLightCount();
    //
    // for (int i = 0; i < punctualLightCount; i++)
    // {
    //     LightParams punctualLightParams = (LightParams) 0;
    //     
    //     UNITY_BRANCH
    //     if (GetPunctualLightType(i) == SPOT_LIGHT) InitializeSpotLightParams(punctualLightParams, i, stdMatParams.V, stdMatParams.N, geoParams.positionWS, geoParams.pixelCoord);
    //     else if (GetPunctualLightType(i) == POINT_LIGHT) InitializePointLightParams(punctualLightParams, i, stdMatParams.V, stdMatParams.N, geoParams.positionWS, geoParams.pixelCoord);
    //     
    //     content.directPunctualLights += CalculateLightIrradiance(punctualLightParams) * StandardBRDF(stdMatParams, punctualLightParams.L, punctualLightParams.H, energyCompensation);
    // }
}

void AdvancedPBRShading(in GeometryParams geoParams, in AdvancedMaterialParams advMatParams, inout RenderingEquationContent content)
{
    // ------------------------- Emission -------------------------
    
    content.emission = advMatParams.emission;
    
    // ------------------------- Indirect Lighting -------------------------
    
    float3 envBRDF = SampleEnvLut(ENVIRONMENT_BRDF_LUT, LUT_SAMPLER, advMatParams.NoV, advMatParams.roughness);
    float3 energyCompensation = 1.0 + advMatParams.F0 * (1.0 / envBRDF.x - 1.0) * 0.5; // 0.5 is a magic number
    
    float3 irradiance;
    float Fc = F_Schlick(0.04, advMatParams.clearCoatNoV) * advMatParams.clearCoat;
    float oneMinusFc = 1.0 - Fc;
    content.indirectLightDiffuse += DIFFUSE_INDIRECT_LIGHTING(geoParams, advMatParams, envBRDF.b, irradiance) * oneMinusFc;
    content.indirectLightSpecular += SPECULAR_INDIRECT_LIGHTING_ANISO(geoParams, advMatParams, irradiance, envBRDF.rg, energyCompensation) * oneMinusFc;
    content.indirectLightSpecular += SPECULAR_INDIRECT_LIGHTING_CLEARCOAT(geoParams, advMatParams, irradiance) * Fc;
    
    // ------------------------- Direct Lighting - Sun Light -------------------------
    
    LightParams sunLightParams = (LightParams) 0;
    InitializeSunLightParams(sunLightParams, advMatParams.V, advMatParams.N, geoParams.positionWS, geoParams.pixelCoord);

    content.directSunLight += CalculateLightIrradiance(sunLightParams) * AdvancedBRDF(advMatParams, sunLightParams.L, sunLightParams.H, energyCompensation);
    
    // ------------------------- Direct Lighting - Punctual Light -------------------------
    
    #if defined(_EDITOR_PREVIEW) 
    return;
    #endif
    
    LightTile lightTile = (LightTile) 0;
    InitializeLightTile(lightTile, geoParams.pixelCoord);
    
    for (int i = lightTile.headerIndex + 1; i <= lightTile.lastLightIndex; i++)
    {
        uint lightIndex = _TilesLightIndicesBuffer[i];
        
        LightParams punctualLightParams = (LightParams) 0;
        
        if (GetPunctualLightType(lightIndex) == SPOT_LIGHT) InitializeSpotLightParams(punctualLightParams, lightIndex, advMatParams.V, advMatParams.N, geoParams.positionWS, geoParams.pixelCoord);
        else if (GetPunctualLightType(lightIndex) == POINT_LIGHT) InitializePointLightParams(punctualLightParams, lightIndex, advMatParams.V, advMatParams.N, geoParams.positionWS, geoParams.pixelCoord);
        
        content.directPunctualLights += CalculateLightIrradiance(punctualLightParams) * AdvancedBRDF(advMatParams, punctualLightParams.L, punctualLightParams.H, energyCompensation);
    }
}

#endif