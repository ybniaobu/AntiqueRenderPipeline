#ifndef YPIPELINE_SIMPLE_COPY_PASS_INCLUDED
#define YPIPELINE_SIMPLE_COPY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Core/UnityInput.hlsl"
#include "../ShaderLibrary/Core/UnityMatrix.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "../ShaderLibrary/Core/SpaceTransforms.hlsl"

TEXTURE2D(_BlitTexture);
float4 _BlitTexture_TexelSize;
float4 _ScaleOffset; // xy: scale, zw: offset(pixels)

float4 _CameraBufferSize;

SAMPLER(sampler_LinearClamp);
SAMPLER(sampler_PointClamp);
SAMPLER(sampler_LinearRepeat);

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings CopyVert(uint vertexID : SV_VertexID)
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

float4 CopyFrag(Varyings IN) : SV_TARGET
{
    return LOAD_TEXTURE2D_LOD(_BlitTexture, IN.positionHCS.xy * _ScaleOffset.xy + _ScaleOffset.zw, 0);
    // return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, IN.uv, 0);
}

#endif