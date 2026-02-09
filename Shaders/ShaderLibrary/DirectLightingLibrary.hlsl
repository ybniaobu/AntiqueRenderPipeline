#ifndef YPIPELINE_DIRECT_LIGHTING_LIBRARY_INCLUDED
#define YPIPELINE_DIRECT_LIGHTING_LIBRARY_INCLUDED

#include "../ShaderLibrary/ShadowsLibrary.hlsl"

// ----------------------------------------------------------------------------------------------------
// Tiled Light Culling
// ----------------------------------------------------------------------------------------------------

struct LightTile
{
    int tileIndex;
    int headerIndex;
    int lightCount;
    int lastLightIndex;
};

void InitializeLightTile(out LightTile tile, float2 pixelCoord)
{
    uint2 coord = floor(pixelCoord * _CameraBufferSize.xy / _TileParams.zw);
    tile.tileIndex = coord.y * (int) _TileParams.x + coord.x;
    tile.headerIndex = tile.tileIndex * (MAX_LIGHT_COUNT_PER_TILE + 1);
    tile.lightCount = _TilesLightIndicesBuffer[tile.headerIndex];
    tile.lastLightIndex = tile.headerIndex + tile.lightCount;
}

// ----------------------------------------------------------------------------------------------------
// Light Falloff / Attenuation functions
// ----------------------------------------------------------------------------------------------------

float GetDistanceAttenuation(float3 lightVector, float lightRange, float attenuationScale) // lightVector is unnormalized light direction(L).
{
    float invLightRangeSqr = rcp(lightRange * lightRange);
    float distanceSquare = dot(lightVector, lightVector);
    float factor = distanceSquare * invLightRangeSqr;
    float smoothFactor = saturate(1.0 - factor * factor);

    float isOutRange = step(smoothFactor, 1e-4);
    smoothFactor = lerp(smoothFactor, 1, attenuationScale);
    smoothFactor = lerp(smoothFactor, 0, isOutRange);
    
    return (smoothFactor * smoothFactor) / max(distanceSquare, 1e-4);
}

float GetAngleAttenuation(float3 L, float3 spotDirection, float2 spotAngleParams) // only for spot lights
{
    float SdotL = dot(spotDirection, L);
    float attenuation = saturate(SdotL * spotAngleParams.x - spotAngleParams.y * spotAngleParams.x);
    return attenuation * attenuation;
}

// ----------------------------------------------------------------------------------------------------
// Light Parameters for computing the BRDF Data and the Rendering Equation
// ----------------------------------------------------------------------------------------------------

struct LightParams
{
    float3 color;
    float4 positionWS;
    float3 L;
    float3 H;
    float distanceAttenuation;
    float angleAttenuation;
    bool isShadowing;
    float3 shadowAttenuation;
    // uint layerMask;
};

float3 CalculateLightIrradiance(in LightParams lightParams)
{
    float3 irradiance = lightParams.color * lightParams.shadowAttenuation * lightParams.distanceAttenuation * lightParams.angleAttenuation;
    return irradiance;
}

// ----------------------------------------------------------------------------------------------------
// Initialize Directional Light Parameters
// ----------------------------------------------------------------------------------------------------

void InitializeSunLightParams(out LightParams sunLightParams, float3 V, float3 normalWS, float3 positionWS, float2 pixelCoord)
{
    sunLightParams.color = GetSunLightColor();
    sunLightParams.positionWS = float4(GetSunLightDirection(), 0.0); //Directional Light has no position
    sunLightParams.L = normalize(sunLightParams.positionWS.xyz);
    sunLightParams.H = normalize(sunLightParams.L + V);
    sunLightParams.distanceAttenuation = 1.0;
    sunLightParams.angleAttenuation = 1.0;
    sunLightParams.isShadowing = IsSunLightShadowing();

    UNITY_BRANCH
    if (sunLightParams.isShadowing)
    {
        #if defined(_SHADOW_PCSS)
        sunLightParams.shadowAttenuation = GetSunLightShadowAttenuation_PCSS(positionWS, normalWS, sunLightParams.L, pixelCoord);
        #elif defined(_SHADOW_PCF)
        sunLightParams.shadowAttenuation = GetSunLightShadowAttenuation_PCF(positionWS, normalWS, sunLightParams.L, pixelCoord);
        #endif
    }
    else
    {
        sunLightParams.shadowAttenuation = float3(1, 1, 1);
    }
}

// ----------------------------------------------------------------------------------------------------
// Initialize Punctual Lights Parameters
// ----------------------------------------------------------------------------------------------------

void InitializeSpotLightParams(out LightParams spotLightParams, int lightIndex, float3 V, float3 normalWS, float3 positionWS, float2 pixelCoord)
{
    spotLightParams.color = GetPunctualLightColor(lightIndex);
    spotLightParams.positionWS = float4(GetPunctualLightPosition(lightIndex), 1.0);
    
    float3 lightVector = spotLightParams.positionWS.xyz - positionWS;
    spotLightParams.L = normalize(lightVector);
    spotLightParams.H = normalize(spotLightParams.L + V);
    
    spotLightParams.distanceAttenuation = GetDistanceAttenuation(lightVector, GetPunctualLightRange(lightIndex), GetPunctualLightRangeAttenuationScale(lightIndex));
    float3 spotDirection = GetSpotLightDirection(lightIndex);
    float2 spotAngleParams = GetSpotLightAngleParams(lightIndex);
    spotLightParams.angleAttenuation = GetAngleAttenuation(spotLightParams.L, spotDirection, spotAngleParams);
    
    spotLightParams.isShadowing = spotLightParams.distanceAttenuation * spotLightParams.angleAttenuation <= 0.0 || GetShadowingLightIndex(lightIndex) < 0.0;
    
    UNITY_BRANCH
    if (spotLightParams.isShadowing) // 反了，待更改
    {
        spotLightParams.shadowAttenuation = 1.0;
    }
    else
    {
        float linearDepth = abs(dot(lightVector, spotDirection));
        #if defined(_SHADOW_PCSS)
            spotLightParams.shadowAttenuation = GetSpotLightShadowAttenuation_PCSS(lightIndex, positionWS, normalWS, spotLightParams.L, linearDepth, pixelCoord);
        #elif defined(_SHADOW_PCF)
            spotLightParams.shadowAttenuation = GetSpotLightShadowAttenuation_PCF(lightIndex, positionWS, normalWS, spotLightParams.L, linearDepth, pixelCoord);
        #endif
    }
}

void InitializePointLightParams(out LightParams pointLightParams, int lightIndex, float3 V, float3 normalWS, float3 positionWS, float2 pixelCoord)
{
    pointLightParams.color = GetPunctualLightColor(lightIndex);
    pointLightParams.positionWS = float4(GetPunctualLightPosition(lightIndex), 1.0);
    
    float3 lightVector = pointLightParams.positionWS.xyz - positionWS;
    pointLightParams.L = normalize(lightVector);
    pointLightParams.H = normalize(pointLightParams.L + V);
    
    pointLightParams.distanceAttenuation = GetDistanceAttenuation(lightVector, GetPunctualLightRange(lightIndex), GetPunctualLightRangeAttenuationScale(lightIndex));
    pointLightParams.angleAttenuation = 1.0;

    pointLightParams.isShadowing = pointLightParams.distanceAttenuation <= 0.0 || GetShadowingLightIndex(lightIndex) < 0.0;
    
    UNITY_BRANCH
    if (pointLightParams.isShadowing) // 反了，待更改
    {
        pointLightParams.shadowAttenuation = 1.0;
    }
    else
    {
        float faceIndex = CubeMapFaceID(-pointLightParams.L);
        float linearDepth = abs(dot(lightVector, k_CubeMapFaceDir[faceIndex]));
        #if defined(_SHADOW_PCSS)
            pointLightParams.shadowAttenuation = GetPointLightShadowAttenuation_PCSS(lightIndex, faceIndex, positionWS, normalWS, pointLightParams.L, linearDepth, pixelCoord);
        #elif defined(_SHADOW_PCF)
            pointLightParams.shadowAttenuation = GetPointLightShadowAttenuation_PCF(lightIndex, faceIndex, positionWS, normalWS, pointLightParams.L, linearDepth, pixelCoord);
        #endif
    }
}



#endif