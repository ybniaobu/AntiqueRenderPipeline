#ifndef YPIPELINE_UNITY_INPUT_INCLUDED
#define YPIPELINE_UNITY_INPUT_INCLUDED

// Unity Engine built-in shader input variables.

// ----------------------------------------------------------------------------------------------------
// SRP Batcher
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    float4 unity_WorldTransformParams;

    // Render Layer block feature
    // Only the first channel (x) contains valid data and the float must be reinterpreted using asuint() to extract the original 32 bits values.
    float4 unity_RenderingLayer;

    // Occlusion Probes
    // float4 unity_ProbesOcclusion;

    // HDR environment map decode instruction
    // float4 unity_SpecCube0_HDR;

    // Lightmap
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    // YPipeline 不支持 Per-Object 的 SH
    // SH
    // float4 unity_SHAr;
    // float4 unity_SHAg;
    // float4 unity_SHAb;
    // float4 unity_SHBr;
    // float4 unity_SHBg;
    // float4 unity_SHBb;
    // float4 unity_SHC;

    // Motion Vector
    float4x4 unity_MatrixPreviousM;
    float4x4 unity_MatrixPreviousMI;
    // x : Use last frame positions (right now skinned meshes are the only objects that use this)
    // y : Force No Motion
    // z : Z bias value
    // w : Camera only
    float4 unity_MotionVectorsParams;
CBUFFER_END

// ----------------------------------------------------------------------------------------------------
// Other variables
// ----------------------------------------------------------------------------------------------------

// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt

float3 _WorldSpaceCameraPos;

// x = 1 or -1 (-1 if projection is flipped)
// y = near plane
// z = far plane
// w = 1/far plane
float4 _ProjectionParams;

// x = width
// y = height
// z = 1 + 1.0/width
// w = 1 + 1.0/height
float4 _ScreenParams;

// x = 1-far/near
// y = far/near
// z = x/far
// w = y/far
// or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
// x = -1+far/near
// y = 1
// z = x/far
// w = 1/far
float4 _ZBufferParams;

// x = orthographic camera's width
// y = orthographic camera's height
// z = unused
// w = 1.0 if camera is ortho, 0.0 if perspective
float4 unity_OrthoParams;

CBUFFER_START(UnityPerFrame)
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;

    // 以下这些 unity 不会设置 !!!!!!!!，需上传这些矩阵，见 UnityMatrix.hlsl
    // float4x4 unity_MatrixInvP;
    // float4x4 unity_MatrixInvVP;
    // float4x4 _PrevViewProjMatrix; // non-jittered.
    // float4x4 _PrevInvViewProjMatrix; // non-jittered
    // float4x4 _NonJitteredViewProjMatrix; // non-jittered.
    // float4x4 _NonJitteredInvViewProjMatrix; // non-jittered.
CBUFFER_END

// ----------------------------------------------------------------------------------------------------
// Textures and Samplers
// ----------------------------------------------------------------------------------------------------

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

// TEXTURE2D(unity_ShadowMask);
// SAMPLER(samplerunity_ShadowMask);

TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

// ----------------------------------------------------------------------------------------------------
// Functions
// ----------------------------------------------------------------------------------------------------

inline uint GetRenderingLayer()
{
    return asuint(unity_RenderingLayer.x);
}

#endif