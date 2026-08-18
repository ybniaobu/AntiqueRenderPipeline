#ifndef YPIPELINE_INPUT_INCLUDED
#define YPIPELINE_INPUT_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Textures and Samplers
// ----------------------------------------------------------------------------------------------------

// Shadow Texture
#define SUN_LIGHT_SHADOW_MAP            _SunLightShadowAtlas
#define PUNCTUAL_LIGHT_SHADOW_MAP       _PunctualLightShadowAtlas
#define SHADOW_SAMPLER_COMPARE          sampler_LinearClampCompare
#define SHADOW_SAMPLER                  sampler_LinearClamp

TEXTURE2D_SHADOW(SUN_LIGHT_SHADOW_MAP);
float4 _SunLightShadowAtlas_TexelSize;
TEXTURE2D_SHADOW(PUNCTUAL_LIGHT_SHADOW_MAP);
float4 _PunctualLightShadowAtlas_TexelSize;
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
TEXTURE2D(_BlueNoise);
float4 _BlueNoise_TexelSize;
TEXTURE3D(_BlueNoise3D);

// General Samplers
SAMPLER(sampler_PointRepeat);
SAMPLER(sampler_PointClamp);
SAMPLER(sampler_LinearRepeat);

// ----------------------------------------------------------------------------------------------------
// Global Illumination Fallback
// ----------------------------------------------------------------------------------------------------

float4 _AmbientProbe[7]; // YPipeline 上传的全局 Ambient Probe 球谐数据
TEXTURECUBE(_GlobalReflectionProbe); // YPipeline 上传的全局 Reflection Probe 数据
SAMPLER(sampler_GlobalReflectionProbe);
float4 _GlobalReflectionProbe_HDR;

// ----------------------------------------------------------------------------------------------------
// Camera / Time Related Data
// ----------------------------------------------------------------------------------------------------

float4 _CameraSettings; // x: vertical FOV in radian, y: cot(FOV/2)
float4 _CameraBufferSize; // x: 1.0 / bufferSize.x, y: 1.0 / bufferSize.y, z: bufferSize.x, w: bufferSize.y
float4 _Jitter; // Halton (-0.5, 0.5), xy: 1.0 / jitter, zw: jitter
float4 _TimeParams; // x: frameCount, y: 1.0 / frameCount

// ----------------------------------------------------------------------------------------------------
// Sun Light Data
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(SunLightData)
    float4 _CascadeParams; // x: maxShadowDistance, y: distanceFade, z: cascadeCount, w: slice size
    float4 _SunLightColor; // xyz: color * intensity
    float4 _SunLightDirection; // xyz: sun light direction, w: whether is shadowing (1 for shadowing)
    float4 _SunLightShadowColor; // xyz: shadow color, w: shadow strengths
    float4 _SunLightPenumbraColor; // xyz: penumbra color
    float4 _SunLightShadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 _SunLightShadowParams; // x: penumbra(filter) width or scale, y: filter sample count
    float4 _SunLightShadowParams2; // x: light angular diameter, y: blocker search area size z: blocker search sample count, w: min penumbra(filter) width
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT]; // xyz: culling sphere center, w: culling sphere radius
    float4x4 _SunLightShadowMatrices[MAX_CASCADE_COUNT];
    float4 _SunLightDepthParams[MAX_CASCADE_COUNT]; // z: frustum size, x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
CBUFFER_END

inline float2 GetCascadeAtlasSize()                                { return _SunLightShadowAtlas_TexelSize.zw; }
inline float2 GetCascadeInverseAtlasSize()                         { return _SunLightShadowAtlas_TexelSize.xy; }
inline float GetMaxShadowDistance()                                { return _CascadeParams.x; }
inline float GetShadowDistanceFade()                               { return _CascadeParams.y; }
inline float GetSunLightCascadeCount()                             { return _CascadeParams.z; }
inline float GetSunLightShadowSliceSize()                          { return _CascadeParams.w; }
inline float3 GetSunLightColor()                                   { return _SunLightColor.xyz; }
inline float3 GetSunLightDirection()                               { return _SunLightDirection.xyz; }
inline bool IsSunLightShadowing()                                  { return _SunLightDirection.w > 0.5; }
inline float3 GetSunLightShadowColor()                             { return _SunLightShadowColor.xyz; }
inline float GetSunLightShadowStrength()                           { return _SunLightShadowColor.w; }
inline float3 GetSunLightPenumbraColor()                           { return _SunLightPenumbraColor.xyz; }
inline float4 GetSunLightShadowBias()                              { return _SunLightShadowBias; }
inline float GetSunLightPCFPenumbraWidth()                         { return _SunLightShadowParams.x; }
inline float GetSunLightPCSSPenumbraScale()                        { return _SunLightShadowParams.x; }
inline float GetSunLightFilterSampleCount()                        { return _SunLightShadowParams.y; }
inline float GetSunLightAngularDiameter()                          { return _SunLightShadowParams2.x; }
inline float GetSunLightBlockerSearchScale()                       { return _SunLightShadowParams2.y; }
inline float GetSunLightBlockerSampleCount()                       { return _SunLightShadowParams2.z; }
inline float GetSunLightMinPenumbraWidth()                         { return _SunLightShadowParams2.w; }

inline float4 GetCascadeCullingSphere(int cascadeIndex)            { return _CascadeCullingSpheres[cascadeIndex]; }
inline float3 GetCascadeCullingSphereCenter(int cascadeIndex)      { return _CascadeCullingSpheres[cascadeIndex].xyz; }
inline float GetCascadeCullingSphereRadius(int cascadeIndex)       { return _CascadeCullingSpheres[cascadeIndex].w; }
inline float4x4 GetSunLightShadowMatrix(int cascadeIndex)          { return _SunLightShadowMatrices[cascadeIndex]; }
inline float4 GetSunLightDepthParams(int cascadeIndex)             { return _SunLightDepthParams[cascadeIndex]; }
inline float GetSunLightFrustumSize(int cascadeIndex)              { return _SunLightDepthParams[cascadeIndex].z; }

// ----------------------------------------------------------------------------------------------------
// Tiled Based Culling - Light / Reflection Probe Indices
// ----------------------------------------------------------------------------------------------------

float4 _TileParams; // xy: tileCountXY, zw: tileUVSizeXY
StructuredBuffer<uint> _TilesLightIndicesBuffer;
StructuredBuffer<uint> _TileReflectionProbeIndicesBuffer;

// ----------------------------------------------------------------------------------------------------
// Punctual Light Data
// ----------------------------------------------------------------------------------------------------

float4 _PunctualLightCount; // x: punctual light count, yzw: 暂无
inline float GetPunctualLightCount()                { return _PunctualLightCount.x; }
inline float2 GetPunctualLightAtlasSize()           { return _PunctualLightShadowAtlas_TexelSize.zw; }
inline float2 GetPunctualLightInverseAtlasSize()    { return _PunctualLightShadowAtlas_TexelSize.xy; }

struct PunctualLightData
{
    float4 colors; // xyz: light color * intensity, w: light type (point 1, spot 2)
    float4 positions; // xyz: light position, w: slice index (non-shadowing is -1)
    float4 directions; // xyz: spot light direction
    float4 lightParams; // x: light range, y: range attenuation scale, z: invAngleRange, w: cosOuterAngle
    float4 shadowColor; // xyz: shadow color, w: shadow strengths
    float4 penumbraColor; // xyz: penumbra color
    float4 shadowBias; // x: depth bias, y: slope scaled depth bias, z: normal bias, w: slope scaled normal bias
    float4 shadowParams; // x: penumbra(filter) width or scale, y: filter sample count
    float4 shadowParams2; // x: light diameter, y: blocker search scale z: blocker search sample count, w: min penumbra(filter) width
};

StructuredBuffer<PunctualLightData> _PunctualLightsData;

inline PunctualLightData GetPunctualLightData(int lightIndex)       { return _PunctualLightsData[lightIndex]; }
inline float3 GetPunctualLightColor(int lightIndex)                 { return _PunctualLightsData[lightIndex].colors.xyz; }
inline float GetPunctualLightType(int lightIndex)                   { return _PunctualLightsData[lightIndex].colors.w; }
inline float3 GetPunctualLightPosition(int lightIndex)              { return _PunctualLightsData[lightIndex].positions.xyz; }
inline float GetPunctualLightSliceIndex(int lightIndex)             { return _PunctualLightsData[lightIndex].positions.w; }
inline float3 GetSpotLightDirection(int lightIndex)                 { return _PunctualLightsData[lightIndex].directions.xyz; }
inline float GetPunctualLightRange(int lightIndex)                  { return _PunctualLightsData[lightIndex].lightParams.x; }
inline float GetPunctualLightRangeAttenuationScale(int lightIndex)  { return _PunctualLightsData[lightIndex].lightParams.y; }
inline float2 GetSpotLightAngleParams(int lightIndex)               { return _PunctualLightsData[lightIndex].lightParams.zw; }
inline float GetSpotLightInverseAngleRange(int lightIndex)          { return _PunctualLightsData[lightIndex].lightParams.z; }
inline float GetSpotLightCosOuterAngle(int lightIndex)              { return _PunctualLightsData[lightIndex].lightParams.w; }
inline float3 GetPunctualLightShadowColor(int lightIndex)           { return _PunctualLightsData[lightIndex].shadowColor.xyz; }
inline float GetPunctualLightShadowStrength(int lightIndex)         { return _PunctualLightsData[lightIndex].shadowColor.w; }
inline float3 GetPunctualLightPenumbraColor(int lightIndex)         { return _PunctualLightsData[lightIndex].penumbraColor.xyz; }
inline float4 GetPunctualLightShadowBias(int lightIndex)            { return _PunctualLightsData[lightIndex].shadowBias; }
inline float GetPunctualLightPCFPenumbraWidth(int lightIndex)       { return _PunctualLightsData[lightIndex].shadowParams.x; }
inline float GetPunctualLightPCSSPenumbraScale(int lightIndex)      { return _PunctualLightsData[lightIndex].shadowParams.x; }
inline float GetPunctualLightFilterSampleCount(int lightIndex)      { return _PunctualLightsData[lightIndex].shadowParams.y; }
inline float GetPunctualLightDiameter(int lightIndex)               { return _PunctualLightsData[lightIndex].shadowParams2.x; }
inline float GetPunctualLightBlockerSearchScale(int lightIndex)     { return _PunctualLightsData[lightIndex].shadowParams2.y; }
inline float GetPunctualLightBlockerSampleCount(int lightIndex)     { return _PunctualLightsData[lightIndex].shadowParams2.z; }
inline float GetPunctualLightMinPenumbraWidth(int lightIndex)       { return _PunctualLightsData[lightIndex].shadowParams2.w; }


struct PunctualLightSliceData
{
    float4 sampleParams; // xy: pixel coordinate in atlas, z: shadow slice size, w: pack failed = -1 (暂未使用)
    float4 depthParams; // x: (f + n) / (f - n), y: -2 * f * n / (f - n); [if UNITY_REVERSED_Z] x: (f + n) / (n - f), y: -2 * f * n / (n - f)
    float4x4 shadowMatrix; // shadow matrix for the punctual light shadow slice
};

StructuredBuffer<PunctualLightSliceData> _PunctualLightSlicesData;

inline PunctualLightSliceData GetPunctualLightSliceData(int sliceIndex) { return _PunctualLightSlicesData[sliceIndex]; }
inline float4 GetPunctualLightSliceSampleParams(int sliceIndex)         { return _PunctualLightSlicesData[sliceIndex].sampleParams; }
inline float2 GetPunctualLightSliceCoord(int sliceIndex)                { return _PunctualLightSlicesData[sliceIndex].sampleParams.xy; }
inline float GetPunctualLightSliceSize(int sliceIndex)                  { return _PunctualLightSlicesData[sliceIndex].sampleParams.z; }
inline float4 GetPunctualLightDepthParams(int sliceIndex)               { return _PunctualLightSlicesData[sliceIndex].depthParams; }
inline float4x4 GetPunctualLightShadowMatrix(int sliceIndex)            { return _PunctualLightSlicesData[sliceIndex].shadowMatrix; }

// ----------------------------------------------------------------------------------------------------
// Reflection Probe Data
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(ReflectionProbeData)
    float4 _ReflectionProbeCount; // x: reflection probe count, yzw: 暂无
    float4 _ReflectionProbePositions[MAX_REFLECTION_PROBE_COUNT]; // xyz: probe position
    float4 _ReflectionProbeBoxCenter[MAX_REFLECTION_PROBE_COUNT]; // xyz: box center, w: importance
    float4 _ReflectionProbeBoxExtent[MAX_REFLECTION_PROBE_COUNT]; // xyz: box extent, w: box projection
    float4 _ReflectionProbeSH[MAX_REFLECTION_PROBE_COUNT * 7]; // reflection probe normalization
    float4 _ReflectionProbeSampleParams[MAX_REFLECTION_PROBE_COUNT]; // xy: pixel coordinate in atlas, z: height
    float4 _ReflectionProbeParams[MAX_REFLECTION_PROBE_COUNT]; // x: intensity, y: blend distance
    float4x4 _ReflectionProbeMatrices[MAX_REFLECTION_PROBE_COUNT]; // world to local matrix
CBUFFER_END

inline float GetReflectionProbeCount()                          { return _ReflectionProbeCount.x; }
inline float3 GetReflectionProbePosition(int index)             { return _ReflectionProbePositions[index].xyz; }
inline float3 GetReflectionProbeBoxCenter(int index)            { return _ReflectionProbeBoxCenter[index].xyz; }
inline float GetReflectionProbeImportance(int index)            { return _ReflectionProbeBoxCenter[index].w; }
inline float3 GetReflectionProbeBoxExtent(int index)            { return _ReflectionProbeBoxExtent[index].xyz; }
inline float IsReflectionProbeBoxProjection(int index)          { return _ReflectionProbeBoxExtent[index].w; }
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
inline float2 GetReflectionProbeAtlasCoord(int index)           { return _ReflectionProbeSampleParams[index].xy; }
inline float GetReflectionProbeMapSize(int index)               { return _ReflectionProbeSampleParams[index].z; }
inline float GetReflectionProbeIntensity(int index)             { return _ReflectionProbeParams[index].x; }
inline float GetReflectionProbeBlendDistance(int index)         { return _ReflectionProbeParams[index].y; }
inline float4x4 GetReflectionProbeMatrix(int index)             { return _ReflectionProbeMatrices[index]; }

#endif