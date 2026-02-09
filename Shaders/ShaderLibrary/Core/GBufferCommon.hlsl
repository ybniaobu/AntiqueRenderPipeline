#ifndef YPIPELINE_GBUFFER_COMMON_INCLUDED
#define YPIPELINE_GBUFFER_COMMON_INCLUDED

#include "../../../Runtime/PipelinePasses/DeferredPasses/MaterialID.cs.hlsl"
#include "../EncodingLibrary.hlsl"

struct GBufferOutput
{
    float4 gBuffer0     : SV_Target0; // RGBA8_SRGB: albedo, AO 
    float4 gBuffer1     : SV_Target1; // RGBA8_UNORM: normal, roughness
    float4 gBuffer2     : SV_Target2; // RGBA8_UNORM: reflectance, metallic, material ID (alpha）
    float3 gBuffer3     : SV_Target3; // R11G11B10_FLOAT: emission
};

inline float PackMaterialID(uint materialID)
{
    return float(materialID) / 255.0;
}

inline uint UnpackMaterialID(float packedMaterialID)
{
    return uint(packedMaterialID * 255.0 + 0.5); // 四舍五入
}

#endif