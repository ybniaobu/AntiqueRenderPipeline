#ifndef YPIPELINE_DEFERRED_LIGHTING_PASS_INCLUDED
#define YPIPELINE_DEFERRED_LIGHTING_PASS_INCLUDED

#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
#include "../../ShaderLibrary/Core/GBufferCommon.hlsl"
#include "../../ShaderLibrary/RenderingEquationLibrary.hlsl"

Texture2D<float4> _GBuffer0; // RGBA8_SRGB: albedo, AO
Texture2D<float4> _GBuffer1; // RGBA8_UNORM: normal, roughness
Texture2D<float4> _GBuffer2; // RGBA8_UNORM: reflectance, metallic, material ID (alpha）
Texture2D<float3> _GBuffer3; // R11G11B10_FLOAT: emission

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

void InitializeGeometryParams(Varyings IN, out GeometryParams geometryParams)
{
    float depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, IN.positionHCS.xy, 0).r;
    float4 NDC = GetNDCFromUVAndDepth(IN.uv, depth);
    geometryParams.positionWS = TransformNDCToWorld(NDC, UNITY_MATRIX_I_VP);
    geometryParams.normalWS = 0.0; // 无需使用，在 standardPBRParams.N 里；
    geometryParams.tangentWS = 0.0; // 无需使用
    geometryParams.uv = IN.uv;
    geometryParams.pixelCoord = IN.positionHCS.xy;
    geometryParams.screenUV = IN.uv;
}

void InitializeStandardPBRParams(in GeometryParams geometryParams, out StandardPBRParams standardPBRParams, out uint materialID)
{
    float4 gBuffer0 = LOAD_TEXTURE2D_LOD(_GBuffer0, geometryParams.pixelCoord, 0);
    float4 gBuffer1 = LOAD_TEXTURE2D_LOD(_GBuffer1, geometryParams.pixelCoord, 0);
    float4 gBuffer2 = LOAD_TEXTURE2D_LOD(_GBuffer2, geometryParams.pixelCoord, 0);
    float3 gBuffer3 = LOAD_TEXTURE2D_LOD(_GBuffer3, geometryParams.pixelCoord, 0);
    materialID = UnpackMaterialID(gBuffer2.a);
    
    standardPBRParams.albedo = gBuffer0.rgb;
    standardPBRParams.ao = gBuffer0.a;
    
    #if _SCREEN_SPACE_AMBIENT_OCCLUSION
    standardPBRParams.ao = min(standardPBRParams.ao, SAMPLE_TEXTURE2D_LOD(_AmbientOcclusionTexture, sampler_PointClamp, geometryParams.screenUV, 0).r);
    #endif
    
    standardPBRParams.alpha = 1.0;
    standardPBRParams.N = DecodeNormalFrom888(gBuffer1.rgb);
    standardPBRParams.roughness = gBuffer1.a;
    standardPBRParams.metallic = gBuffer2.g;
    standardPBRParams.F0 = lerp(gBuffer2.r * gBuffer2.r * float3(0.16, 0.16, 0.16), standardPBRParams.albedo, standardPBRParams.metallic);
    standardPBRParams.F90 = saturate(dot(standardPBRParams.F0, 50.0 * 0.3333));
    standardPBRParams.emission = gBuffer3;
    
    standardPBRParams.V = GetWorldSpaceNormalizedViewDir(geometryParams.positionWS);
    standardPBRParams.R = reflect(-standardPBRParams.V, standardPBRParams.N);
    standardPBRParams.NoV = saturate(dot(standardPBRParams.N, standardPBRParams.V)) + 1e-3; //防止小黑点
}

float4 DeferredLightingFrag(Varyings IN) : SV_TARGET
{
    GeometryParams geometryParams = (GeometryParams) 0;
    InitializeGeometryParams(IN, geometryParams);
    
    uint materialID;
    StandardPBRParams standardPBRParams = (StandardPBRParams) 0;
    InitializeStandardPBRParams(geometryParams, standardPBRParams, materialID);
    
    RenderingEquationContent renderingEquationContent = (RenderingEquationContent) 0;
    
    [forcecase] switch (materialID)
    {
        case MATERIALID_STANDARD_PBR: StandardPBRShading(geometryParams, standardPBRParams, renderingEquationContent);
        break;
        
        default: StandardPBRShading(geometryParams, standardPBRParams, renderingEquationContent);
        break;
    }
    
    
    return float4(CombineRenderingEquationContent(renderingEquationContent), 1.0);
}

#endif