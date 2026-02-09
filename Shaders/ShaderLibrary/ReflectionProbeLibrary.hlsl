#ifndef YPIPELINE_REFLECTION_PROBE_LIBRARY_INCLUDED
#define YPIPELINE_REFLECTION_PROBE_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "IntersectionTestLibrary.hlsl"

// TODO: 里面有些许重复运算的代码，比如 AABBMinMax，目前影响不大，之后有时间再更改。

// ----------------------------------------------------------------------------------------------------
// Tiled Culling -- Reflection Probe
// ----------------------------------------------------------------------------------------------------

struct ReflectionProbeTile
{
    int tileIndex;
    int headerIndex;
    int probeCount;
    int lastProbeIndex;
};

inline void InitializeReflectionProbeTile(out ReflectionProbeTile tile, float2 screenUV)
{
    uint2 tileCoord = floor(screenUV / _TileParams.zw);
    tile.tileIndex = tileCoord.y * (int) _TileParams.x + tileCoord.x;
    tile.headerIndex = tile.tileIndex * (MAX_REFLECTION_PROBE_COUNT_PER_TILE + 1);
    tile.probeCount = _TileReflectionProbeIndicesBuffer[tile.headerIndex];
    tile.lastProbeIndex = tile.headerIndex + tile.probeCount;
}

// 根据优先级、离像素点距离来找到最合适的一个 Reflection Probe
int FindBestReflectionProbe(float2 screenUV, float3 positionWS)
{
    ReflectionProbeTile tile = (ReflectionProbeTile) 0;
    InitializeReflectionProbeTile(tile, screenUV);
    
    int bestIndex = -1;
    int bestImportance = -1;
    float bestDistSqr = FLT_MAX;
    
    for (int i = tile.headerIndex + 1; i <= tile.lastProbeIndex; i++)
    {
        // 判断像素点是否在 probe 范围内
        uint idx = _TileReflectionProbeIndicesBuffer[i];
        AABBMinMax aabb = BuildAABBMinMax(GetReflectionProbeBoxCenter(idx), GetReflectionProbeBoxExtent(idx));
        bool isInProbe = AABB_Point_Intersect(aabb, positionWS);
        if (!isInProbe) continue;
        
        // 先判断优先级，再判断像素点离 probe 中心距离
        int importance = (int) GetReflectionProbeImportance(idx);
        float3 dir = positionWS - GetReflectionProbeBoxCenter(idx);
        float distSqr = dot(dir, dir);
        
        if (importance > bestImportance)
        {
            bestIndex = idx;
            bestImportance = importance;
            bestDistSqr = distSqr;
        }
        else if (importance == bestImportance)
        {
            if (distSqr < bestDistSqr)
            {
                bestIndex = idx;
                bestDistSqr = distSqr;
            }
        }
    }
    return bestIndex;
}

bool IsGreater(int importanceA, float distSqrA, int importanceB, float distSqrB)
{
    if (importanceA > importanceB) return true;
    if (importanceA < importanceB) return false;
    return distSqrA < distSqrB;  // importance 相等时, 比较 distSqr
}

// 根据优先级、离像素点距离来找到最合适的二个 Reflection Probe，用于后续混合
int2 FindBestTwoReflectionProbe(float2 screenUV, float3 positionWS)
{
    ReflectionProbeTile tile = (ReflectionProbeTile) 0;
    InitializeReflectionProbeTile(tile, screenUV);
    
    int bestIndex = -1, secondBestIndex = -1;
    int bestImportance = -1, secondBestImportance = -1;
    float bestDistSqr = FLT_MAX, secondBestDistSqr = FLT_MAX;
    
    for (int i = tile.headerIndex + 1; i <= tile.lastProbeIndex; i++)
    {
        // 判断像素点是否在 probe 范围内
        uint idx = _TileReflectionProbeIndicesBuffer[i];
        AABBMinMax aabb = BuildAABBMinMax(GetReflectionProbeBoxCenter(idx), GetReflectionProbeBoxExtent(idx));
        bool isInProbe = AABB_Point_Intersect(aabb, positionWS);
        if (!isInProbe) continue;
        
        // 先判断优先级，再判断像素点离 probe 中心距离
        int importance = (int) GetReflectionProbeImportance(idx);
        float3 dir = positionWS - GetReflectionProbeBoxCenter(idx);
        float distSqr = dot(dir, dir);
        
        if (IsGreater(importance, distSqr, bestImportance, bestDistSqr))
        {
            secondBestIndex = bestIndex;
            bestIndex = idx;
            secondBestImportance = bestImportance;
            bestImportance = importance;
            secondBestDistSqr = bestDistSqr;
            bestDistSqr = distSqr;
        }
        else if (IsGreater(importance, distSqr, secondBestImportance, secondBestDistSqr))
        {
            secondBestIndex = idx;
            secondBestImportance = importance;
            secondBestDistSqr = distSqr;
        }
    }
    return int2(bestIndex, secondBestIndex);
}

// ----------------------------------------------------------------------------------------------------
// Sample Reflection Probe Atlas
// ----------------------------------------------------------------------------------------------------

float3 SampleReflectionProbeAtlas(int probeIndex, float3 dir, float mipmap)
{
    float2 leftBottomCoord = GetReflectionProbeAtlasCoord(probeIndex);
    float size = GetReflectionProbeMapSize(probeIndex);
    float2 uv = PackNormalOctQuadEncode(dir);
    uv = saturate(uv * 0.5 + 0.5);
    float2 indent = 0.5 * _ReflectionProbeAtlas_TexelSize.xy;
    
    int mip0 = (int) floor(mipmap);
    int right0 = (mip0 + 1) >> 1;
    int up0 = mip0 >> 1;
    float2 coord0 = leftBottomCoord + size * (float2((1.0 - pow(0.25, right0)) * 4.0 / 3.0, (1.0 - pow(0.25, up0)) * 2.0 / 3.0) + uv * exp2(-mip0)); // 等比数列求和
    // float3 color0 = LOAD_TEXTURE2D_LOD(_ReflectionProbeAtlas, coord0, 0).rgb;
    float2 uv0 = coord0 * _ReflectionProbeAtlas_TexelSize.xy;
    uv0 = lerp(uv0 + indent, uv0 - indent, uv);
    float3 color0 = SAMPLE_TEXTURE2D_LOD(_ReflectionProbeAtlas, sampler_LinearClamp, uv0, 0).rgb;
    
    int mip1 = mip0 + 1;
    int right1 = (mip1 + 1) >> 1;
    int up1 = right0;
    float2 coord1 = leftBottomCoord + size * (float2((1.0 - pow(0.25, right1)) * 4.0 / 3.0, (1.0 - pow(0.25, up1)) * 2.0 / 3.0) + uv * exp2(-mip1)); // 等比数列求和
    // float3 color1 = LOAD_TEXTURE2D_LOD(_ReflectionProbeAtlas, coord1, 0).rgb;
    float2 uv1 = coord1 * _ReflectionProbeAtlas_TexelSize.xy;
    uv1 = lerp(uv1 + indent, uv1 - indent, uv);
    float3 color1 = SAMPLE_TEXTURE2D_LOD(_ReflectionProbeAtlas, sampler_LinearClamp, uv1, 0).rgb;
    
    float mipBlend = mipmap - mip0;
    return lerp(color0, color1, mipBlend);
}

// ----------------------------------------------------------------------------------------------------
// Parallax Correction
// ----------------------------------------------------------------------------------------------------

float3 GetParallaxCorrectionDirection(int probeIndex, float3 R, float3 positionWS)
{
    if (IsReflectionProbeBoxProjection(probeIndex))
    {
        float3 invR = rcp(R + 1e-6);
        float3 boxCenter = GetReflectionProbeBoxCenter(probeIndex);
        float3 boxExtent = GetReflectionProbeBoxExtent(probeIndex);
        AABBMinMax aabb = BuildAABBMinMax(boxCenter, boxExtent);
        float3 t1 = (aabb.min - positionWS) * invR;
        float3 t2 = (aabb.max - positionWS) * invR;
        
        float3 tMax = max(t1, t2);
        float t = min(tMax.x, min(tMax.y, tMax.z));
        t = max(t, 0.0);
        float3 intersection = positionWS + R * t;
        return normalize(intersection - boxCenter);
    }
    else
    {
        return R;
    }
}

// ----------------------------------------------------------------------------------------------------
// Reflection Probe Normalization
// ----------------------------------------------------------------------------------------------------

float3 ReflectionProbeNormalization(float3 rawReflection, float3 irradiance, float3 R, int probeIndex)
{
    float4 SH[7];
    GetReflectionProbeSH(probeIndex, SH);
    float3 probeSH = SampleSphericalHarmonics(R, SH);
    float3 normalizedReflection = rawReflection / (probeSH + 1e-6);
    return normalizedReflection * irradiance;
}

float3 ReflectionProbeNormalization_Luminance(float3 rawReflection, float3 irradiance, float3 R, int probeIndex)
{
    float4 SH[7];
    GetReflectionProbeSH(probeIndex, SH);
    float probeSHLuma = Luminance(SampleSphericalHarmonics(R, SH));
    float3 normalizedReflection = rawReflection / probeSHLuma;
    return normalizedReflection * Luminance(irradiance);
}

// Only For Global Reflection Probe
float3 ReflectionProbeNormalization(float3 rawReflection, float3 irradiance, float3 R)
{
    float3 probeSH = EvaluateAmbientProbe(R);
    float3 normalizedReflection = rawReflection / (probeSH + 1e-6);
    return normalizedReflection * irradiance;
}

// Only For Global Reflection Probe
float3 ReflectionProbeNormalization_Luminance(float3 rawReflection, float3 irradiance, float3 R)
{
    float probeSHLuma = Luminance(EvaluateAmbientProbe(R));
    float3 normalizedReflection = rawReflection / probeSHLuma;
    return normalizedReflection * Luminance(irradiance);
}

// ----------------------------------------------------------------------------------------------------
// Get Pre-filtered Environment Color -- After Parallax Correction or Normalization
// ----------------------------------------------------------------------------------------------------

// No Normalization Version, Only For Global Reflection Probe
inline float3 GetGlobalPrefilteredEnvColor(float mipmap, float3 R)
{
    return SampleGlobalReflectionProbe(R, mipmap);
}

// Normalization Version, Only For Global Reflection Probe
inline float3 GetGlobalPrefilteredEnvColor(float mipmap, float3 R, float3 irradiance)
{
    float3 rawReflection = SampleGlobalReflectionProbe(R, mipmap);
    return ReflectionProbeNormalization(rawReflection, irradiance, R);
}

// No Parallax Correction & No Normalization Version, For Local Reflection Probe
inline float3 GetPrefilteredEnvColor(int probeIndex, float mipmap, float3 R)
{
    float intensity = GetReflectionProbeIntensity(probeIndex);
    return SampleReflectionProbeAtlas(probeIndex, R, mipmap) * intensity;
}

// Parallax Correction & No Normalization Version, For Local Reflection Probe
inline float3 GetPrefilteredEnvColor(int probeIndex, float mipmap, float3 R, float3 positionWS)
{
    float intensity = GetReflectionProbeIntensity(probeIndex);
    float3 correctedR = GetParallaxCorrectionDirection(probeIndex, R, positionWS);
    return SampleReflectionProbeAtlas(probeIndex, correctedR, mipmap) * intensity;
}

// Parallax Correction & Normalization Version, For Local Reflection Probe
inline float3 GetPrefilteredEnvColor(int probeIndex, float mipmap, float3 R, float3 positionWS, float3 irradiance)
{
    float intensity = GetReflectionProbeIntensity(probeIndex);
    float3 correctedR = GetParallaxCorrectionDirection(probeIndex, R, positionWS);
    float3 rawReflection = SampleReflectionProbeAtlas(probeIndex, correctedR, mipmap);
    return ReflectionProbeNormalization(rawReflection, irradiance, correctedR, probeIndex) * intensity;
}

// ----------------------------------------------------------------------------------------------------
// Reflection Probe Blending
// ----------------------------------------------------------------------------------------------------

float3 EvaluateSingleReflectionProbe(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float3 irradiance)
{
    int bestReflectionProbeIndex = FindBestReflectionProbe(geometryParams.screenUV, geometryParams.positionWS);
    float mipmap = RoughnessToMipmapLevel(standardPBRParams.roughness, 6.0);
    
    if (bestReflectionProbeIndex == -1) return GetGlobalPrefilteredEnvColor(mipmap, standardPBRParams.R, irradiance);
    else return GetPrefilteredEnvColor(bestReflectionProbeIndex, mipmap, standardPBRParams.R, geometryParams.positionWS, irradiance);
}

float CalculateProbeWeight(int probeIndex, float3 positionWS)
{
    float blendDistance = GetReflectionProbeBlendDistance(probeIndex);
    float3 boxCenter = GetReflectionProbeBoxCenter(probeIndex);
    float3 boxExtent = GetReflectionProbeBoxExtent(probeIndex);
    AABBMinMax aabb = BuildAABBMinMax(boxCenter, boxExtent);
    float3 weights = min(positionWS - aabb.min, aabb.max - positionWS) / blendDistance;
    return saturate(min(weights.x, min(weights.y, weights.z)));
}

float3 EvaluateAndBlendingTwoReflectionProbes(in GeometryParams geometryParams, in StandardPBRParams standardPBRParams, float3 irradiance)
{
    int2 bestReflectionProbeIndices = FindBestTwoReflectionProbe(geometryParams.screenUV, geometryParams.positionWS);
    float mipmap = RoughnessToMipmapLevel(standardPBRParams.roughness, 6.0);
    
    if (bestReflectionProbeIndices.x == -1) return GetGlobalPrefilteredEnvColor(mipmap, standardPBRParams.R, irradiance);
    else if (bestReflectionProbeIndices.y == -1)
    {
        float weight = CalculateProbeWeight(bestReflectionProbeIndices.x, geometryParams.positionWS);
        float3 secondProbe = GetGlobalPrefilteredEnvColor(mipmap, standardPBRParams.R, irradiance);
        float3 firstProbe = GetPrefilteredEnvColor(bestReflectionProbeIndices.x, mipmap, standardPBRParams.R, geometryParams.positionWS, irradiance);
        return lerp(secondProbe, firstProbe, weight);
    }
    else
    {
        float weight0 = CalculateProbeWeight(bestReflectionProbeIndices.x, geometryParams.positionWS);
        float weight1 = CalculateProbeWeight(bestReflectionProbeIndices.y, geometryParams.positionWS);
        float weight = weight0 / (weight0 + weight1);
        float3 firstProbe = GetPrefilteredEnvColor(bestReflectionProbeIndices.x, mipmap, standardPBRParams.R, geometryParams.positionWS, irradiance);
        float3 secondProbe = GetPrefilteredEnvColor(bestReflectionProbeIndices.y, mipmap, standardPBRParams.R, geometryParams.positionWS, irradiance);
        return lerp(secondProbe, firstProbe, weight);
    }
}

#endif