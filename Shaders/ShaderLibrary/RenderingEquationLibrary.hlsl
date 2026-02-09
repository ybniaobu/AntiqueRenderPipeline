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
    
    #if defined(LIGHTMAP_ON)
        float2 lightMapUV;
    #endif
};

#include "../ShaderLibrary/BRDFModelLibrary.hlsl"
#include "../ShaderLibrary/IndirectLightingLibrary.hlsl"
#include "../ShaderLibrary/DirectLightingLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Shading Functions
// ----------------------------------------------------------------------------------------------------

void StandardPBRShading(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, inout RenderingEquationContent content)
{
    content.emission = standardPBRParams.emission;
    
    // ------------------------- Indirect Lighting -------------------------
    
    float3 envBRDF = SampleEnvLut(ENVIRONMENT_BRDF_LUT, LUT_SAMPLER, standardPBRParams.NoV, standardPBRParams.roughness);
    float3 energyCompensation = 1.0 + standardPBRParams.F0 * (1.0 / envBRDF.x - 1) * 0.5; // 0.5 is a magic number
    
    float3 irradiance;
    content.indirectLightDiffuse += DiffuseIndirectLighting(geometryParams, standardPBRParams, envBRDF.b, irradiance);

    // content.indirectLightSpecular += CalculateIndirectSpecular_IBL(standardPBRParams, unity_SpecCube0, samplerunity_SpecCube0, envBRDF.rg, energyCompensation);
    // content.indirectLightSpecular += CalculateIndirectSpecular_IBL_RemappedMipmap(standardPBRParams, unity_SpecCube0,samplerunity_SpecCube0, envBRDF.rg, energyCompensation);
    content.indirectLightSpecular += SpecularIndirectLighting(geometryParams, standardPBRParams, irradiance, envBRDF.rg, energyCompensation);
    
    // ------------------------- Direct Lighting - Sun Light -------------------------
    
    LightParams sunLightParams = (LightParams) 0;
    InitializeSunLightParams(sunLightParams, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);

    BRDFParams sunBRDFParams = (BRDFParams) 0;
    InitializeBRDFParams(sunBRDFParams, standardPBRParams.N, sunLightParams.L, standardPBRParams.V, sunLightParams.H);

    content.directSunLight += CalculateLightIrradiance(sunLightParams) * StandardPBR_EnergyCompensation(sunBRDFParams, standardPBRParams, energyCompensation);
    
    // ------------------------- Direct Lighting - Punctual Light -------------------------

    LightTile lightTile = (LightTile) 0;
    InitializeLightTile(lightTile, geometryParams.pixelCoord);
    
    for (int i = lightTile.headerIndex + 1; i <= lightTile.lastLightIndex; i++)
    {
        uint lightIndex = _TilesLightIndicesBuffer[i];
        
        LightParams punctualLightParams = (LightParams) 0;
        
        UNITY_BRANCH
        if (GetPunctualLightType(lightIndex) == SPOT_LIGHT) InitializeSpotLightParams(punctualLightParams, lightIndex, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
        else if (GetPunctualLightType(lightIndex) == POINT_LIGHT) InitializePointLightParams(punctualLightParams, lightIndex, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
    
        BRDFParams punctualBRDFParams = (BRDFParams) 0;
        InitializeBRDFParams(punctualBRDFParams, standardPBRParams.N, punctualLightParams.L, standardPBRParams.V, punctualLightParams.H);
    
        content.directPunctualLights += CalculateLightIrradiance(punctualLightParams) * StandardPBR_EnergyCompensation(punctualBRDFParams, standardPBRParams, energyCompensation);
    }
    
    // int punctualLightCount = GetPunctualLightCount();
    //
    // for (int i = 0; i < punctualLightCount; i++)
    // {
    //     LightParams punctualLightParams = (LightParams) 0;
    //     
    //     UNITY_BRANCH
    //     if (GetPunctualLightType(i) == SPOT_LIGHT) InitializeSpotLightParams(punctualLightParams, i, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
    //     else if (GetPunctualLightType(i) == POINT_LIGHT) InitializePointLightParams(punctualLightParams, i, standardPBRParams.V, standardPBRParams.N, geometryParams.positionWS, geometryParams.pixelCoord);
    //     
    //     BRDFParams punctualBRDFParams = (BRDFParams) 0;
    //     InitializeBRDFParams(punctualBRDFParams, standardPBRParams.N, punctualLightParams.L, standardPBRParams.V, punctualLightParams.H);
    //     
    //     renderingEquationContent.directPunctualLights += CalculateLightIrradiance(punctualLightParams) * StandardPBR_EnergyCompensation(punctualBRDFParams, standardPBRParams, energyCompensation);
    // }
}

#endif