#ifndef YPIPELINE_SHADOWS_LIBRARY_INCLUDED
#define YPIPELINE_SHADOWS_LIBRARY_INCLUDED

#include "../Utilities/RandomLib.hlsl"
#include "../Utilities/SamplingLib.hlsl"
#include "../Utilities/CubemapLib.hlsl"

#define SHADOW_NOISE_TEX _BlueNoise
#define SHADOW_NOISE_TEX_SIZE _BlueNoise_TexelSize
#define SHADOW_SAMPLE_SEQUENCE k_SobolDisk
#define ROTATION_JITTER_SCALE 1

// ----------------------------------------------------------------------------------------------------
// Sample Shadow Map or Array
// ----------------------------------------------------------------------------------------------------

inline float SampleShadowMap_Compare(float3 positionSS, TEXTURE2D_SHADOW(shadowMap), SAMPLER_CMP(shadowMapSampler))
{
    float shadowAttenuation = SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowMapSampler, positionSS);
    return shadowAttenuation;
}

inline float SampleShadowMap_Depth(float2 uv, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_LOD(shadowMap, shadowMapSampler, uv, 0).r;
    return depth;
}

inline float SampleShadowMap_DepthCompare(float3 positionSS, TEXTURE2D(shadowMap), SAMPLER(shadowMapSampler))
{
    float depth = SAMPLE_TEXTURE2D_LOD(shadowMap, shadowMapSampler, positionSS.xy, 0).r;
    return step(depth, positionSS.z);
}

// ----------------------------------------------------------------------------------------------------
// Light/Shadow Space Transform
// ----------------------------------------------------------------------------------------------------

inline float3 TransformWorldToSunLightSpaceUV(float3 positionWS, int cascadeIndex)
{
    // SS: shadow space
    float3 positionSS = mul(GetSunLightShadowMatrix(cascadeIndex), float4(positionWS, 1.0)).xyz;
    return positionSS;
}

inline float3 TransformWorldToPunctualLightSpaceUV(float3 positionWS, int sliceIndex)
{
    // SS: shadow space
    float4 positionSS_BeforeDivision = mul(GetPunctualLightShadowMatrix(sliceIndex), float4(positionWS, 1.0));
    float3 positionSS = positionSS_BeforeDivision.xyz / positionSS_BeforeDivision.w;
    return positionSS;
}

inline float2 PunctualLightSliceUVToAtlasUV(int sliceIndex, float2 inverseAtlasSize, float2 sliceUV)
{
    float4 sampleParams = GetPunctualLightSliceSampleParams(sliceIndex);
    return float2((sampleParams.xy + sliceUV * sampleParams.z) * inverseAtlasSize);
}

// Return min/max UV
inline float4 PunctualLightSliceUVToAtlasUV(int sliceIndex, float2 inverseAtlasSize, inout float3 positionSS)
{
    float4 sampleParams = GetPunctualLightSliceSampleParams(sliceIndex);
    positionSS = float3((sampleParams.xy + positionSS.xy * sampleParams.z) * inverseAtlasSize, positionSS.z);
    float2 minUV = sampleParams.xy * inverseAtlasSize;
    float2 maxUV = (sampleParams.xy + sampleParams.z) * inverseAtlasSize;
    return float4(minUV, maxUV);
}

// ----------------------------------------------------------------------------------------------------
// Cascade Shadow Related Functions
// ----------------------------------------------------------------------------------------------------

int ComputeCascadeIndex(float3 positionWS)
{
    float4 sphere0 = GetCascadeCullingSphere(0);
    float4 sphere1 = GetCascadeCullingSphere(1);
    float4 sphere2 = GetCascadeCullingSphere(2);
    float4 sphere3 = GetCascadeCullingSphere(3);
    
    float3 vector0 = positionWS - sphere0.xyz;
    float3 vector1 = positionWS - sphere1.xyz;
    float3 vector2 = positionWS - sphere2.xyz;
    float3 vector3 = positionWS - sphere3.xyz;
    float4 distanceSquare = float4(dot(vector0, vector0), dot(vector1, vector1), dot(vector2, vector2), dot(vector3, vector3));
    float4 radiusSquare = float4(sphere0.w * sphere0.w, sphere1.w * sphere1.w, sphere2.w * sphere2.w, sphere3.w * sphere3.w);

    int4 indexes = int4(distanceSquare < radiusSquare);
    indexes.yzw = saturate(indexes.yzw - indexes.xyz);
    return 4 - dot(indexes, int4(4, 3, 2, 1));
}

float ComputeDistanceFade(float3 positionWS, float maxDistance, float distanceFade)
{
    float depth = -TransformWorldToView(positionWS).z;
    return saturate((1 - depth / maxDistance) / distanceFade);
}

// ----------------------------------------------------------------------------------------------------
// Shadow Bias Related Functions
// ----------------------------------------------------------------------------------------------------

float ComputeTanHalfFOV(int spotLightIndex)
{
    float cosHalfFOV = GetSpotLightCosOuterAngle(spotLightIndex);
    float cosHalfFOVSquare = cosHalfFOV * cosHalfFOV;
    float sinHalfFOVSquare = 1.0 - cosHalfFOVSquare;
    float tanHalfFOVSquare = sinHalfFOVSquare / cosHalfFOVSquare;
    return sqrt(tanHalfFOVSquare);
}

// normalWS must be normalized
float3 ApplyShadowBias(float3 positionWS, float4 shadowBias, float texelSize, float penumbraWS, float3 normalWS, float3 L)
{
    float cosTheta = saturate(dot(normalWS, L));
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta);
    float tanTheta = clamp(sinTheta / cosTheta, 0.0, 100.0); // maxBias

    float offset = 0.5 * (texelSize + penumbraWS);
    float3 depthBias = offset * shadowBias.x * L;
    float3 scaledDepthBias = offset * tanTheta * shadowBias.y * L;
    float3 normalBias = offset * shadowBias.z * normalWS;
    float3 scaledNormalBias = offset * sinTheta * shadowBias.w * normalWS;

    // float3 depthBias = texelSize * (1.0 + penumbraTexel) * shadowBias.x * L;
    // float3 scaledDepthBias = texelSize * (1.0 + penumbraTexel) * tanTheta * shadowBias.y * L;
    // float3 normalBias = texelSize * (1.0 + penumbraTexel) * shadowBias.z * normalWS;
    // float3 scaledNormalBias = texelSize * (1.0 + penumbraTexel) * sinTheta * shadowBias.w * normalWS;
    
    return positionWS + depthBias + scaledDepthBias + normalBias + scaledNormalBias;
}

// ----------------------------------------------------------------------------------------------------
// Shadow and Penumbra Color Function
// ----------------------------------------------------------------------------------------------------

float3 ApplyShadowAndPenumbraColor(float shadowAttenuation, float3 shadowColor, float3 penumbraColor)
{
    penumbraColor = lerp(shadowColor, penumbraColor, shadowAttenuation);
    shadowColor = lerp(penumbraColor, 1, shadowAttenuation);
    return shadowColor;
}

// ----------------------------------------------------------------------------------------------------
// Randomization
// ----------------------------------------------------------------------------------------------------

float2x2 GetRandomRotation(float2 pixelCoord)
{
    #ifdef _TAA
    float randomRadian = (LOAD_TEXTURE2D_LOD(SHADOW_NOISE_TEX, pixelCoord % SHADOW_NOISE_TEX_SIZE.w, 0).r + _Jitter.w * ROTATION_JITTER_SCALE) * TWO_PI;
    #else
    float randomRadian = (LOAD_TEXTURE2D_LOD(SHADOW_NOISE_TEX, pixelCoord % SHADOW_NOISE_TEX_SIZE.w, 0).r) * TWO_PI;
    #endif
    return float2x2(cos(randomRadian), -sin(randomRadian), sin(randomRadian), cos(randomRadian));
}

// ----------------------------------------------------------------------------------------------------
// PCF Related Functions
// ----------------------------------------------------------------------------------------------------

float ApplyPCF_SunLight(int cascadeIndex, float3 positionWS_Bias, float penumbraTexel, float2x2 rotation)
{
    float3 positionSS = TransformWorldToSunLightSpaceUV(positionWS_Bias, cascadeIndex);
    
    int sampleCount = GetSunLightFilterSampleCount();
    float shadowAttenuation = 0.0;
    for (int i = 0; i < sampleCount; i++)
    {
        float2 offset = mul(rotation, SHADOW_SAMPLE_SEQUENCE[i + 1] * 0.5);
        float2 atlasUV = positionSS.xy + offset * penumbraTexel * GetCascadeInverseAtlasSize();
        shadowAttenuation += SampleShadowMap_Compare(float3(atlasUV, positionSS.z), SUN_LIGHT_SHADOW_MAP, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleCount;
}

float ApplyPCF_SpotLight(int lightIndex, int sliceIndex, float3 positionWS_Bias, float penumbraTexel, float2x2 rotation)
{
    float3 positionSS = TransformWorldToPunctualLightSpaceUV(positionWS_Bias, sliceIndex);
    float2 inverseAtlasSize = GetPunctualLightInverseAtlasSize();
    float4 minMaxUV = PunctualLightSliceUVToAtlasUV(sliceIndex, inverseAtlasSize, positionSS);
    
    int sampleCount = GetPunctualLightFilterSampleCount(lightIndex);
    float shadowAttenuation = 0.0;
    for (int i = 0; i < sampleCount; i++)
    {
        float2 offset = mul(rotation, SHADOW_SAMPLE_SEQUENCE[i + 1] * 0.5);
        float2 atlasUV = positionSS.xy + offset * penumbraTexel * inverseAtlasSize;
        atlasUV = clamp(atlasUV, minMaxUV.xy + inverseAtlasSize, minMaxUV.zw - inverseAtlasSize); // indent, make sure there is no shadow seam at the edge of the spot cone
        shadowAttenuation += SampleShadowMap_Compare(float3(atlasUV, positionSS.z), PUNCTUAL_LIGHT_SHADOW_MAP, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleCount;
}

float ApplyPCF_PointLight(int lightIndex, int firstSliceIndex, int faceIndex, float3 positionWS_Bias, float penumbraTexel, float2x2 rotation)
{
    int sliceIndex = firstSliceIndex + faceIndex;
    float3 positionSS = TransformWorldToPunctualLightSpaceUV(positionWS_Bias, sliceIndex);
    float2 inverseAtlasSize = GetPunctualLightInverseAtlasSize();
    float reverseSliceRes = rcp(GetPunctualLightSliceSize(firstSliceIndex));
    
    int sampleCount = GetPunctualLightFilterSampleCount(lightIndex);
    float shadowAttenuation = 0.0;
    for (int i = 0; i < sampleCount; i++)
    {
        float2 offset = mul(rotation, SHADOW_SAMPLE_SEQUENCE[i + 1] * 0.5);
        float2 faceUV = positionSS.xy + offset * penumbraTexel * reverseSliceRes;
        float3 dir = CubemapFaceUVToDirFast(faceIndex, faceUV);
        int realFaceID;
        faceUV = CubemapDirToFaceUVFast(dir, realFaceID);
        float2 atlasUV = PunctualLightSliceUVToAtlasUV(firstSliceIndex + realFaceID, inverseAtlasSize, faceUV);
        shadowAttenuation += SampleShadowMap_Compare(float3(atlasUV, positionSS.z), PUNCTUAL_LIGHT_SHADOW_MAP, SHADOW_SAMPLER_COMPARE);
    }
    return shadowAttenuation / sampleCount;
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions -- PCF
// ----------------------------------------------------------------------------------------------------

float3 GetSunLightShadowAttenuation_PCF(float3 positionWS, float3 normalWS, float3 L, float2 pixelCoord)
{
    int cascadeIndex = ComputeCascadeIndex(positionWS);
    if (cascadeIndex >= GetSunLightCascadeCount()) return 1.0;
    
    float texelSize = GetSunLightFrustumSize(cascadeIndex) / GetSunLightShadowSliceSize();
    float penumbra = GetSunLightPCFPenumbraWidth();
    float penumbraTexel = penumbra / texelSize;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, penumbra, normalWS, L);
    float2x2 rotation = GetRandomRotation(pixelCoord);
    float shadowAttenuation = ApplyPCF_SunLight(cascadeIndex, positionWS_Bias, penumbraTexel, rotation);
    
    float3 shadowColor = ApplyShadowAndPenumbraColor(shadowAttenuation, GetSunLightShadowColor(), GetSunLightPenumbraColor());
    float shadowFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    return lerp(1.0, shadowColor, GetSunLightShadowStrength() * shadowFade);
}

float3 GetSpotLightShadowAttenuation_PCF(int lightIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float2 pixelCoord)
{
    int sliceIndex = GetPunctualLightSliceIndex(lightIndex);
    
    // float linearDepth = mul(GetPunctualLightShadowMatrix(sliceIndex), float4(positionWS, 1.0)).w;
    float perspective = 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth;
    float texelSize = perspective / GetPunctualLightSliceSize(sliceIndex);
    float penumbra = GetPunctualLightPCFPenumbraWidth(lightIndex);
    float penumbraTexel = penumbra / texelSize;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetPunctualLightShadowBias(lightIndex), texelSize, penumbra, normalWS, L);
    float2x2 rotation = GetRandomRotation(pixelCoord);
    float shadowAttenuation = ApplyPCF_SpotLight(lightIndex, sliceIndex, positionWS_Bias, penumbraTexel, rotation);
    
    float3 shadowColor = ApplyShadowAndPenumbraColor(shadowAttenuation, GetPunctualLightShadowColor(lightIndex), GetPunctualLightPenumbraColor(lightIndex));
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    return lerp(1.0, shadowColor, GetPunctualLightShadowStrength(lightIndex) * distanceFade);
}

float3 GetPointLightShadowAttenuation_PCF(int lightIndex, int faceIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float2 pixelCoord)
{
    int firstSliceIndex = GetPunctualLightSliceIndex(lightIndex);
    
    //float linearDepth = mul(GetPunctualLightShadowMatrix(sliceIndex), float4(positionWS, 1.0)).w;
    float perspective = 2.0 * linearDepth;
    float texelSize = perspective / GetPunctualLightSliceSize(firstSliceIndex);
    float penumbra = GetPunctualLightPCFPenumbraWidth(lightIndex);
    float penumbraTexel = penumbra / texelSize;
    
    float3 positionWS_Bias = ApplyShadowBias(positionWS, GetPunctualLightShadowBias(lightIndex), texelSize, penumbra, normalWS, L);
    float2x2 rotation = GetRandomRotation(pixelCoord);
    float shadowAttenuation = ApplyPCF_PointLight(lightIndex, firstSliceIndex, faceIndex, positionWS_Bias, penumbraTexel, rotation);
    
    float3 shadowColor = ApplyShadowAndPenumbraColor(shadowAttenuation, GetPunctualLightShadowColor(lightIndex), GetPunctualLightPenumbraColor(lightIndex));
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    return lerp(1.0, shadowColor, GetPunctualLightShadowStrength(lightIndex) * distanceFade);
}

// ----------------------------------------------------------------------------------------------------
// PCSS Related Functions
// ----------------------------------------------------------------------------------------------------

inline float NonLinearToLinearDepth_Ortho(float4 depthParams, float nonLinearDepth)
{
    return (depthParams.y - 2.0 * nonLinearDepth + 1.0) / depthParams.x;
}

inline float NonLinearToLinearDepth_Persp(float4 depthParams, float nonLinearDepth)
{
    return depthParams.y / (2.0 * nonLinearDepth - 1.0 + depthParams.x);
}

float3 ComputeAverageBlockerDepth_SunLight(int cascadeIndex, float3 positionWS_Bias, float searchWidthTexel, float2x2 rotation)
{
    float3 positionSS = TransformWorldToSunLightSpaceUV(positionWS_Bias, cascadeIndex);
    
    float4 depthParams = GetSunLightDepthParams(cascadeIndex);
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Ortho(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-6; // avoid division by zero

    int sampleCount = GetSunLightBlockerSampleCount();
    for (int i = 0; i < sampleCount; i++)
    {
        float2 offset = mul(rotation, SHADOW_SAMPLE_SEQUENCE[i + 1] * 0.5);
        float2 uv = positionSS.xy + offset * searchWidthTexel * GetCascadeInverseAtlasSize();
        float d_Blocker = SampleShadowMap_Depth(uv, SUN_LIGHT_SHADOW_MAP, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Ortho(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

float3 ComputeAverageBlockerDepth_SpotLight(int lightIndex, int sliceIndex, float3 positionWS_Bias, float searchWidthTexel, float2x2 rotation)
{
    float3 positionSS = TransformWorldToPunctualLightSpaceUV(positionWS_Bias, sliceIndex);
    float2 inverseAtlasSize = GetPunctualLightInverseAtlasSize();
    float4 minMaxUV = PunctualLightSliceUVToAtlasUV(sliceIndex, inverseAtlasSize, positionSS);
    
    float4 depthParams = GetPunctualLightDepthParams(sliceIndex);
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Persp(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-6; // avoid division by zero

    int sampleCount = GetPunctualLightBlockerSampleCount(lightIndex);
    for (int i = 0; i < sampleCount; i++)
    {
        float2 offset = mul(rotation, SHADOW_SAMPLE_SEQUENCE[i + 1] * 0.5);
        float2 uv = positionSS.xy + offset * searchWidthTexel * inverseAtlasSize;
        uv = clamp(uv, minMaxUV.xy + inverseAtlasSize, minMaxUV.zw - inverseAtlasSize);
        float d_Blocker = SampleShadowMap_Depth(uv, PUNCTUAL_LIGHT_SHADOW_MAP, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Persp(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

float3 ComputeAverageBlockerDepth_PointLight(int lightIndex, int firstSliceIndex, float faceIndex, float3 positionWS_Bias, float searchWidthTexel, float2x2 rotation)
{
    int sliceIndex = firstSliceIndex + faceIndex;
    float3 positionSS = TransformWorldToPunctualLightSpaceUV(positionWS_Bias, sliceIndex);
    float2 inverseAtlasSize = GetPunctualLightInverseAtlasSize();
    float reverseSliceRes = rcp(GetPunctualLightSliceSize(firstSliceIndex));
    
    float4 depthParams = GetPunctualLightDepthParams(sliceIndex);
    float d_Shading = positionSS.z;
    float ld_Shading = NonLinearToLinearDepth_Persp(depthParams, d_Shading);
    float ald_Blocker = 0.0;
    float count = 1e-6; // avoid division by zero

    int sampleCount = GetPunctualLightBlockerSampleCount(lightIndex);
    for (int i = 0; i < sampleCount; i++)
    {
        float2 offset = mul(rotation, SHADOW_SAMPLE_SEQUENCE[i + 1] * 0.5);
        float2 faceUV = positionSS.xy + offset * searchWidthTexel * reverseSliceRes;
        float3 dir = CubemapFaceUVToDirFast(faceIndex, faceUV);
        int realFaceID;
        faceUV = CubemapDirToFaceUVFast(dir, realFaceID);
        float2 atlasUV = PunctualLightSliceUVToAtlasUV(firstSliceIndex + realFaceID, inverseAtlasSize, faceUV);
        float d_Blocker = SampleShadowMap_Depth(atlasUV, PUNCTUAL_LIGHT_SHADOW_MAP, SHADOW_SAMPLER);
        float ld_Blocker = NonLinearToLinearDepth_Persp(depthParams, d_Blocker);
        
        if (ld_Blocker < ld_Shading)
        {
            ald_Blocker += ld_Blocker;
            count += 1.0;
        }
    }
    ald_Blocker = ald_Blocker / count;
    return float3(ald_Blocker, count, ld_Shading);
}

// ----------------------------------------------------------------------------------------------------
// Shadow Attenuation Functions -- PCSS
// ----------------------------------------------------------------------------------------------------

float3 GetSunLightShadowAttenuation_PCSS(float3 positionWS, float3 normalWS, float3 L, float2 pixelCoord)
{
    int cascadeIndex = ComputeCascadeIndex(positionWS);
    if (cascadeIndex >= GetSunLightCascadeCount()) return 1.0;
    
    float size = GetSunLightAngularDiameter();
    float texelSize = GetSunLightFrustumSize(cascadeIndex) / GetSunLightShadowSliceSize();
    float searchWidthWS = GetSunLightBlockerSearchScale() * GetSunLightPCSSPenumbraScale() * size * 2.0; // 2.0 is a magic number, which assumes the average blocker height is 2
    float searchWidthTexel = searchWidthWS / texelSize;

    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, searchWidthWS, normalWS, L);
    float2x2 rotation = GetRandomRotation(pixelCoord);
    float3 blocker = ComputeAverageBlockerDepth_SunLight(cascadeIndex, positionWS_SearchBias, searchWidthTexel, rotation);
    if (blocker.y < 1.0) return 1.0;
    
    float penumbraWS = GetSunLightPCSSPenumbraScale() * (blocker.z - blocker.x) * size;
    penumbraWS = max(penumbraWS, GetSunLightMinPenumbraWidth());
    float penumbraTexel = penumbraWS / texelSize;
    
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetSunLightShadowBias(), texelSize, penumbraWS, normalWS, L);
    float shadowAttenuation = ApplyPCF_SunLight(cascadeIndex, positionWS_FilterBias, penumbraTexel, rotation);
    
    float3 shadowColor = ApplyShadowAndPenumbraColor(shadowAttenuation, GetSunLightShadowColor(), GetSunLightPenumbraColor());
    float shadowFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    return lerp(1.0, shadowColor, GetSunLightShadowStrength() * shadowFade);
}

float3 GetSpotLightShadowAttenuation_PCSS(int lightIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float2 pixelCoord)
{
    float sliceIndex = GetPunctualLightSliceIndex(lightIndex);
    
    float perspective = 2.0 * ComputeTanHalfFOV(lightIndex) * linearDepth;
    float texelSize = perspective / GetPunctualLightSliceSize(sliceIndex);
    float size = GetPunctualLightDiameter(lightIndex);
    float searchWidthWS = GetPunctualLightBlockerSearchScale(lightIndex) * GetPunctualLightPCSSPenumbraScale(lightIndex) * size;
    float searchWidthTexel = searchWidthWS / texelSize;
    
    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetPunctualLightShadowBias(lightIndex), texelSize, searchWidthWS, normalWS, L);
    float2x2 rotation = GetRandomRotation(pixelCoord);
    float3 blocker = ComputeAverageBlockerDepth_SpotLight(lightIndex, sliceIndex, positionWS_SearchBias, searchWidthTexel, rotation);
    if (blocker.y < 1.0) return 1.0;

    float penumbraWS = GetPunctualLightPCSSPenumbraScale(lightIndex) * (linearDepth - blocker.x) / blocker.x * size;
    penumbraWS = max(penumbraWS, GetPunctualLightMinPenumbraWidth(lightIndex));
    float penumbraTexel = penumbraWS / texelSize;
    
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetPunctualLightShadowBias(lightIndex), texelSize, penumbraWS, normalWS, L);
    float shadowAttenuation = ApplyPCF_SpotLight(lightIndex, sliceIndex, positionWS_FilterBias, penumbraTexel, rotation);
    
    float3 shadowColor = ApplyShadowAndPenumbraColor(shadowAttenuation, GetPunctualLightShadowColor(lightIndex), GetPunctualLightPenumbraColor(lightIndex));
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    return lerp(1.0, shadowColor, GetPunctualLightShadowStrength(lightIndex) * distanceFade);
}

float3 GetPointLightShadowAttenuation_PCSS(int lightIndex, float faceIndex, float3 positionWS, float3 normalWS, float3 L, float linearDepth, float2 pixelCoord)
{
    float firstSliceIndex = GetPunctualLightSliceIndex(lightIndex);
    
    float perspective = 2.0 * linearDepth;
    float texelSize = perspective / GetPunctualLightSliceSize(firstSliceIndex);
    float size = GetPunctualLightDiameter(lightIndex);
    float searchWidthWS = GetPunctualLightBlockerSearchScale(lightIndex) * GetPunctualLightPCSSPenumbraScale(lightIndex) * size;
    float searchWidthTexel = searchWidthWS / texelSize;
    
    float3 positionWS_SearchBias = ApplyShadowBias(positionWS, GetPunctualLightShadowBias(lightIndex), texelSize, searchWidthWS, normalWS, L);
    float2x2 rotation = GetRandomRotation(pixelCoord);
    float3 blocker = ComputeAverageBlockerDepth_PointLight(lightIndex, firstSliceIndex, faceIndex, positionWS_SearchBias, searchWidthTexel, rotation);
    if (blocker.y < 1.0) return 1.0;
    
    float penumbraWS = GetPunctualLightPCSSPenumbraScale(lightIndex) * (linearDepth - blocker.x) / blocker.x * size;
    penumbraWS = max(penumbraWS, GetPunctualLightMinPenumbraWidth(lightIndex));
    float penumbraTexel = penumbraWS / texelSize;
    
    float3 positionWS_FilterBias = ApplyShadowBias(positionWS, GetPunctualLightShadowBias(lightIndex), texelSize, penumbraWS, normalWS, L);
    float shadowAttenuation = ApplyPCF_PointLight(lightIndex, firstSliceIndex, faceIndex, positionWS_FilterBias, penumbraTexel, rotation);
    
    float3 shadowColor = ApplyShadowAndPenumbraColor(shadowAttenuation, GetPunctualLightShadowColor(lightIndex), GetPunctualLightPenumbraColor(lightIndex));
    float distanceFade = ComputeDistanceFade(positionWS, GetMaxShadowDistance(), GetShadowDistanceFade());
    return lerp(1.0, shadowColor, GetPunctualLightShadowStrength(lightIndex) * distanceFade);
}

#endif