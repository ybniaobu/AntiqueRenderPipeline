#ifndef YPIPELINE_INPUT_INCLUDED
#define YPIPELINE_INPUT_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Constant Buffers
// ----------------------------------------------------------------------------------------------------

float4 _CameraSettings; // x: vertical FOV in radian, y: cot(FOV/2)

float4 _CameraBufferSize; // x: 1.0 / bufferSize.x, y: 1.0 / bufferSize.y, z: bufferSize.x, w: bufferSize.y
float4 _Jitter; // Halton (-0.5, 0.5), xy: 1.0 / jitter, zw: jitter
float4 _TimeParams; // x: frameCount, y: 1.0 / frameCount

// TODO: Global Constant buffer 存放一些全局的只需设置一次的 constant buffer
// CBUFFER_START(ParamsPerSetting)
    float4 _CascadeSettings; // x: max shadow distance, y: shadow distance fade, z: sun light cascade count, w: cascade edge fade
    float4 _ShadowMapSizes; // x: sun light shadow map size, y: spot light shadow map size, z: point light shadow map size
// CBUFFER_END

inline float GetMaxShadowDistance()                        { return _CascadeSettings.x; }
inline float GetShadowDistanceFade()                       { return _CascadeSettings.y; }
inline float GetSunLightCascadeCount()                     { return _CascadeSettings.z; }
inline float GetCascadeEdgeFade()                          { return _CascadeSettings.w; }
inline float GetSunLightShadowMapSize()                    { return _ShadowMapSizes.x; }
inline float GetSpotLightShadowMapSize()                   { return _ShadowMapSizes.y; }
inline float GetPointLightShadowMapSize()                  { return _ShadowMapSizes.z; }

// ----------------------------------------------------------------------------------------------------
// Sun Light & Shadow Data
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(SunLightParams)
    float4 _SunLightColor; // xyz: color * intensity
    float4 _SunLightDirection; // xyz: sun light direction, w: whether is shadowing (1 for shadowing)
    float4 _SunLightShadowColor; // xyz: shadow color, w: shadow strengths
    float4 _SunLightPenumbraColor; // xyz: penumbra color
    float4 _SunLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 _SunLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
    float4 _SunLightShadowParams2; // x: light diameter, y: blocker search area size z: blocker search sample number, w: min penumbra(filter) width

    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT]; // xyz: culling sphere center, w: culling sphere radius
    float4x4 _SunLightShadowMatrices[MAX_CASCADE_COUNT];
    float4 _SunLightDepthParams[MAX_CASCADE_COUNT]; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
CBUFFER_END

inline float3 GetSunLightColor()                                   { return _SunLightColor.xyz; }
inline float3 GetSunLightDirection()                               { return _SunLightDirection.xyz; }
inline bool IsSunLightShadowing()                                  { return _SunLightDirection.w > 0.5; }
inline float3 GetSunLightShadowColor()                             { return _SunLightShadowColor.xyz; }
inline float GetSunLightShadowStrength()                           { return _SunLightShadowColor.w; }
inline float3 GetSunLightPenumbraColor()                           { return _SunLightPenumbraColor.xyz; }
inline float4 GetSunLightShadowBias()                              { return _SunLightShadowBias; }
inline float GetSunLightPCFPenumbraWidth()                         { return _SunLightShadowParams.x; }
inline float GetSunLightPCFSampleNumber()                          { return _SunLightShadowParams.y; }
inline float GetSunLightPCSSPenumbraScale()                        { return _SunLightShadowParams.x; }
inline float GetSunLightPCSSSampleNumber()                         { return _SunLightShadowParams.y; }
inline float GetSunLightSize()                                     { return _SunLightShadowParams2.x; }
inline float GetSunLightBlockerSearchScale()                       { return _SunLightShadowParams2.y; }
inline float GetSunLightBlockerSampleNumber()                      { return _SunLightShadowParams2.z; }
inline float GetSunLightMinPenumbraWidth()                         { return _SunLightShadowParams2.w; }

inline float4 GetCascadeCullingSphere(int cascadeIndex)            { return _CascadeCullingSpheres[cascadeIndex]; }
inline float3 GetCascadeCullingSphereCenter(int cascadeIndex)      { return _CascadeCullingSpheres[cascadeIndex].xyz; }
inline float GetCascadeCullingSphereRadius(int cascadeIndex)       { return _CascadeCullingSpheres[cascadeIndex].w; }
inline float4x4 GetSunLightShadowMatrix(int cascadeIndex)          { return _SunLightShadowMatrices[cascadeIndex]; }
inline float4 GetSunLightDepthParams(int cascadeIndex)             { return _SunLightDepthParams[cascadeIndex]; }

// ----------------------------------------------------------------------------------------------------
// Tiled Based Culling - Light / Reflection Probe Indices
// ----------------------------------------------------------------------------------------------------

float4 _TileParams; // xy: tileCountXY, zw: tileUVSizeXY
StructuredBuffer<uint> _TilesLightIndicesBuffer;
StructuredBuffer<uint> _TileReflectionProbeIndicesBuffer;

// ----------------------------------------------------------------------------------------------------
// Punctual Light & Shadow Data
// ----------------------------------------------------------------------------------------------------

float4 _PunctualLightCount; // x: punctual light count, yzw: 暂无
float GetPunctualLightCount()   { return _PunctualLightCount.x; }

struct PunctualLightData
{
    float4 punctualLightColors; // xyz: light color * intensity, w: light type (point 1, spot 2)
    float4 punctualLightPositions; // xyz: light position, w: shadowing spot/point light index (non-shadowing is -1)
    float4 punctualLightDirections; // xyz: spot light direction
    float4 punctualLightParams; // x: light range, y: range attenuation scale, z: invAngleRange, w: cosOuterAngle
};

StructuredBuffer<PunctualLightData> _PunctualLightData;

inline float3 GetPunctualLightColor(int lightIndex)                { return _PunctualLightData[lightIndex].punctualLightColors.xyz; }
inline float GetPunctualLightType(int lightIndex)                  { return _PunctualLightData[lightIndex].punctualLightColors.w; }
inline float3 GetPunctualLightPosition(int lightIndex)             { return _PunctualLightData[lightIndex].punctualLightPositions.xyz; }
inline float GetShadowingLightIndex(int lightIndex)                { return _PunctualLightData[lightIndex].punctualLightPositions.w; }
inline float3 GetSpotLightDirection(int lightIndex)                { return _PunctualLightData[lightIndex].punctualLightDirections.xyz; }
inline float GetPunctualLightRange(int lightIndex)                 { return _PunctualLightData[lightIndex].punctualLightParams.x; }
inline float GetPunctualLightRangeAttenuationScale(int lightIndex) { return _PunctualLightData[lightIndex].punctualLightParams.y; }
inline float2 GetSpotLightAngleParams(int lightIndex)              { return _PunctualLightData[lightIndex].punctualLightParams.zw; }
inline float GetSpotLightInverseAngleRange(int lightIndex)         { return _PunctualLightData[lightIndex].punctualLightParams.z; }
inline float GetSpotLightCosOuterAngle(int lightIndex)             { return _PunctualLightData[lightIndex].punctualLightParams.w; }

struct PointLightShadowData
{
    float4 pointLightShadowColors; // xyz: shadow color, w: shadow strengths
    float4 pointLightPenumbraColors; // xyz: penumbra color
    float4 pointLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 pointLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
    float4 pointLightShadowParams2; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
    float4 pointLightDepthParams; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
};

StructuredBuffer<PointLightShadowData> _PointLightShadowData;
StructuredBuffer<float4x4> _PointLightShadowMatrices; // TODO: 修改进 PointLightShadowData 里

inline float3 GetPointLightShadowColor(int shadowIndex)                     { return _PointLightShadowData[shadowIndex].pointLightShadowColors.xyz; }
inline float GetPointLightShadowStrength(int shadowIndex)                   { return _PointLightShadowData[shadowIndex].pointLightShadowColors.w; }
inline float3 GetPointLightPenumbraColor(int shadowIndex)                   { return _PointLightShadowData[shadowIndex].pointLightPenumbraColors.xyz; }
inline float4 GetPointLightShadowBias(int shadowIndex)                      { return _PointLightShadowData[shadowIndex].pointLightShadowBias; }
inline float GetPointLightPCFPenumbraWidth(int shadowIndex)                 { return _PointLightShadowData[shadowIndex].pointLightShadowParams.x; }
inline float GetPointLightPCFSampleNumber(int shadowIndex)                  { return _PointLightShadowData[shadowIndex].pointLightShadowParams.y; }
inline float GetPointLightPCSSPenumbraScale(int shadowIndex)                { return _PointLightShadowData[shadowIndex].pointLightShadowParams.x; }
inline float GetPointLightPCSSSampleNumber(int shadowIndex)                 { return _PointLightShadowData[shadowIndex].pointLightShadowParams.y; }
inline float GetPointLightSize(int shadowIndex)                             { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.x; }
inline float GetPointLightBlockerSearchScale(int shadowIndex)               { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.y; }
inline float GetPointLightBlockerSampleNumber(int shadowIndex)              { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.z; }
inline float GetPointLightMinPenumbraWidth(int shadowIndex)                 { return _PointLightShadowData[shadowIndex].pointLightShadowParams2.w; }
inline float4 GetPointLightDepthParams(int shadowIndex)                     { return _PointLightShadowData[shadowIndex].pointLightDepthParams; }
inline float4x4 GetPointLightShadowMatrix(int shadowFaceIndex)              { return _PointLightShadowMatrices[shadowFaceIndex]; }

struct SpotLightShadowData
{
    float4 spotLightShadowColors; // xyz: shadow color, w: shadow strengths
    float4 spotLightPenumbraColors; // xyz: penumbra color
    float4 spotLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 spotLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample number
    float4 spotLightShadowParams2; // x: light diameter, y: blocker search scale z: blocker search sample number, w: min penumbra(filter) width
    float4 spotLightDepthParams; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
};

StructuredBuffer<SpotLightShadowData> _SpotLightShadowData;
StructuredBuffer<float4x4> _SpotLightShadowMatrices; // TODO: 修改进 SpotLightShadowData 里

inline float3 GetSpotLightShadowColor(int shadowIndex)                     { return _SpotLightShadowData[shadowIndex].spotLightShadowColors.xyz; }
inline float GetSpotLightShadowStrength(int shadowIndex)                   { return _SpotLightShadowData[shadowIndex].spotLightShadowColors.w; }
inline float3 GetSpotLightPenumbraColor(int shadowIndex)                   { return _SpotLightShadowData[shadowIndex].spotLightPenumbraColors.xyz; }
inline float4 GetSpotLightShadowBias(int shadowIndex)                      { return _SpotLightShadowData[shadowIndex].spotLightShadowBias; }
inline float GetSpotLightPCFPenumbraWidth(int shadowIndex)                 { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.x; }
inline float GetSpotLightPCFSampleNumber(int shadowIndex)                  { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.y; }
inline float GetSpotLightPCSSPenumbraScale(int shadowIndex)                { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.x; }
inline float GetSpotLightPCSSSampleNumber(int shadowIndex)                 { return _SpotLightShadowData[shadowIndex].spotLightShadowParams.y; }
inline float GetSpotLightSize(int shadowIndex)                             { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.x; }
inline float GetSpotLightBlockerSearchScale(int shadowIndex)               { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.y; }
inline float GetSpotLightBlockerSampleNumber(int shadowIndex)              { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.z; }
inline float GetSpotLightMinPenumbraWidth(int shadowIndex)                 { return _SpotLightShadowData[shadowIndex].spotLightShadowParams2.w; }
inline float4 GetSpotLightDepthParams(int shadowIndex)                     { return _SpotLightShadowData[shadowIndex].spotLightDepthParams; }
inline float4x4 GetSpotLightShadowMatrix(int shadowIndex)                  { return _SpotLightShadowMatrices[shadowIndex]; }

// ----------------------------------------------------------------------------------------------------
// Reflection Probe Data
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(ReflectionProbeData)
    float4 _ReflectionProbeCount; // x: reflection probe count, yzw: 暂无
    float4 _ReflectionProbeBoxCenter[MAX_REFLECTION_PROBE_COUNT]; // xyz: box center, w: importance
    float4 _ReflectionProbeBoxExtent[MAX_REFLECTION_PROBE_COUNT]; // xyz: box extent, w: box projection
    float4 _ReflectionProbeSH[MAX_REFLECTION_PROBE_COUNT * 7]; // reflection probe normalization
    float4 _ReflectionProbeSampleParams[MAX_REFLECTION_PROBE_COUNT]; // xy: uv in atlas, z: height
    float4 _ReflectionProbeParams[MAX_REFLECTION_PROBE_COUNT]; // x: intensity, y: blend distance
CBUFFER_END

inline float GetReflectionProbeCount()                    { return _ReflectionProbeCount.x; }
inline float3 GetReflectionProbeBoxCenter(int index)      { return _ReflectionProbeBoxCenter[index].xyz; }
inline float GetReflectionProbeImportance(int index)      { return _ReflectionProbeBoxCenter[index].w; }
inline float3 GetReflectionProbeBoxExtent(int index)      { return _ReflectionProbeBoxExtent[index].xyz; }
inline float IsReflectionProbeBoxProjection(int index)    { return _ReflectionProbeBoxExtent[index].w; }
inline void GetReflectionProbeSH(int index, out float4 SH[7])
{
    int idx = index * 7;
    SH[0] = _ReflectionProbeSH[idx + 0];
    SH[1] = _ReflectionProbeSH[idx + 1];
    SH[2] = _ReflectionProbeSH[idx + 2];
    SH[3] = _ReflectionProbeSH[idx + 3];
    SH[4] = _ReflectionProbeSH[idx + 4];
    SH[5] = _ReflectionProbeSH[idx + 5];
    SH[6] = _ReflectionProbeSH[idx + 6];
}
inline float2 GetReflectionProbeAtlasCoord(int index)     { return _ReflectionProbeSampleParams[index].xy; }
inline float GetReflectionProbeMapSize(int index)         { return _ReflectionProbeSampleParams[index].z; }
inline float GetReflectionProbeIntensity(int index)       { return _ReflectionProbeParams[index].x; }
inline float GetReflectionProbeBlendDistance(int index)   { return _ReflectionProbeParams[index].y; }

// ----------------------------------------------------------------------------------------------------
// Textures and Samplers
// ----------------------------------------------------------------------------------------------------

// Shadow Texture
#define SUN_LIGHT_SHADOW_MAP            _SunLightShadowMap
#define SPOT_LIGHT_SHADOW_MAP           _SpotLightShadowMap
#define POINT_LIGHT_SHADOW_MAP          _PointLightShadowMap
#define SHADOW_SAMPLER_COMPARE          sampler_LinearClampCompare
#define SHADOW_SAMPLER                  sampler_LinearClamp

TEXTURE2D_ARRAY_SHADOW(SUN_LIGHT_SHADOW_MAP);
float4 _SunLightShadowMap_TexelSize;
TEXTURE2D_ARRAY_SHADOW(SPOT_LIGHT_SHADOW_MAP);
float4 _SpotLightShadowMap_TexelSize;
TEXTURECUBE_ARRAY_SHADOW(POINT_LIGHT_SHADOW_MAP);
float4 _PointLightShadowMap_TexelSize;
SAMPLER_CMP(SHADOW_SAMPLER_COMPARE);
SAMPLER(SHADOW_SAMPLER);

// BRDF LUT
#define ENVIRONMENT_BRDF_LUT            _EnvBRDFLut
#define LUT_SAMPLER                     sampler_Point_Clamp_EnvBRDFLut

TEXTURE2D(ENVIRONMENT_BRDF_LUT);
SAMPLER(LUT_SAMPLER);

// Pipeline Textures
TEXTURE2D(_CameraColorTexture);
TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_ThinGBuffer);
TEXTURE2D(_MotionVectorTexture);
TEXTURE2D(_IrradianceTexture);
TEXTURE2D(_AmbientOcclusionTexture);
TEXTURE2D(_ReflectionProbeAtlas);
float4 _ReflectionProbeAtlas_TexelSize;

// Blue Noise
TEXTURE2D(_BlueNoise64);
float4 _BlueNoise64_TexelSize;

// STBN
TEXTURE2D(_STBN128Scalar3);
float4 _STBN128Scalar3_TexelSize;
TEXTURE2D(_STBN128Vec3);
float4 _STBN128Vec3_TexelSize;
TEXTURE2D(_STBN128UnitVec3);
float4 _STBN128UnitVec3_TexelSize;
TEXTURE2D(_STBN128CosineUnitVec3);
float4 _STBN128CosineUnitVec3_TexelSize;

// General Samplers
SAMPLER(sampler_PointRepeat);
SAMPLER(sampler_PointClamp);
SAMPLER(sampler_LinearRepeat);

#endif