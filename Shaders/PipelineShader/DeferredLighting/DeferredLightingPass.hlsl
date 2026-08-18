#ifndef YPIPELINE_DEFERRED_LIGHTING_PASS_INCLUDED
#define YPIPELINE_DEFERRED_LIGHTING_PASS_INCLUDED

#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
#include "../../ShaderLibrary/Core/GBufferCommon.hlsl"
#include "../../ShaderLibrary/PBR/RenderingEquationLib.hlsl"

Texture2D<float4> _GBuffer0; // RGBA8_SRGB: albedo, AO
Texture2D<float4> _GBuffer1; // RGBA8_UNORM: normal, roughness
Texture2D<float4> _GBuffer2; // RGBA8_UNORM: reflectance, metallic, material ID (alpha)
Texture2D<float4> _GBuffer3; // RGBA8_UNORM: custom parameters based on project requirements

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings FullScreenVert(uint vertexID : SV_VertexID)
{
    Varyings OUT;
    
    //OUT.positionHCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    //OUT.uv = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    OUT.uv = float2((vertexID << 1) & 2, vertexID & 2);
    OUT.positionHCS = float4(OUT.uv * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
    
    if (_ProjectionParams.x < 0.0) OUT.uv.y = 1.0 - OUT.uv.y;
    
    // #if UNITY_UV_STARTS_AT_TOP
    //     OUT.uv.y = 1.0 - OUT.uv.y;
    // #endif
    
    return OUT;
}

void InitializeGeometryParams(Varyings IN, out GeometryParams geoParams)
{
    float depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, IN.positionHCS.xy, 0).r;
    float4 NDC = GetNDCFromUVAndDepth(IN.uv, depth);
    geoParams.positionWS = TransformNDCToWorld(NDC, UNITY_MATRIX_I_VP);
    geoParams.normalWS = 0.0; // unavailable, but also unnecessary to use
    geoParams.tangentWS = 0.0; // unavailable, but also unnecessary to use
    geoParams.uv = IN.uv;
    geoParams.pixelCoord = IN.positionHCS.xy;
    geoParams.screenUV = IN.uv;
}

void InitializeStandardMaterialParams(in GeometryParams geoParams, out StandardMaterialParams stdMatParams, out uint materialID)
{
    float4 gBuffer0 = LOAD_TEXTURE2D_LOD(_GBuffer0, geoParams.pixelCoord, 0);
    float4 gBuffer1 = LOAD_TEXTURE2D_LOD(_GBuffer1, geoParams.pixelCoord, 0);
    float4 gBuffer2 = LOAD_TEXTURE2D_LOD(_GBuffer2, geoParams.pixelCoord, 0);
    float4 gBuffer3 = LOAD_TEXTURE2D_LOD(_GBuffer3, geoParams.pixelCoord, 0);
    materialID = UnpackMaterialID(gBuffer2.a);
    
    stdMatParams.albedo = gBuffer0.rgb;
    stdMatParams.emission = 0.0;
    stdMatParams.ao = gBuffer0.a;
    
    #if _SCREEN_SPACE_AMBIENT_OCCLUSION
    stdMatParams.ao = min(stdMatParams.ao, SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionTexture, sampler_PointClamp, geoParams.screenUV, 0).r);
    #endif
    
    stdMatParams.alpha = 1.0;
    stdMatParams.N = DecodeNormalFrom888(gBuffer1.rgb);
    stdMatParams.roughness = gBuffer1.a;
    stdMatParams.alphaRoughness = gBuffer1.a * gBuffer1.a;
    stdMatParams.metallic = gBuffer2.g;
    stdMatParams.F0 = lerp(gBuffer2.r * gBuffer2.r * float3(0.16, 0.16, 0.16), stdMatParams.albedo, stdMatParams.metallic);
    stdMatParams.F90 = saturate(dot(stdMatParams.F0, 50.0 * 0.3333));
    
    stdMatParams.V = GetWorldSpaceNormalizedViewDir(geoParams.positionWS);
    stdMatParams.NoV = saturate(dot(stdMatParams.N, stdMatParams.V)) + 1e-3; //防止小黑点
}

float4 DeferredLightingFrag(Varyings IN) : SV_TARGET
{
    GeometryParams geoParams = (GeometryParams) 0;
    InitializeGeometryParams(IN, geoParams);
    
    uint materialID;
    StandardMaterialParams stdMatParams = (StandardMaterialParams) 0;
    InitializeStandardMaterialParams(geoParams, stdMatParams, materialID);
    
    RenderingEquationContent content = (RenderingEquationContent) 0;
    
    // materialID is not used for now, but we can use it to implement different shading models in the future.
    // [forcecase] switch (materialID)
    // {
    //     case MATERIALID_STANDARD_PBR: StandardPBRShading(geoParams, stdMatParams, content);
    //     break;
    //     
    //     default: StandardPBRShading(geoParams, stdMatParams, content);
    //     break;
    // }
    StandardPBRShading(geoParams, stdMatParams, content);
    
    return float4(CombineRenderingEquationContent(content), 1.0);
}

#endif