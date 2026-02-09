#ifndef YPIPELINE_CAMERA_MOTION_VECTOR_PASS_INCLUDED
#define YPIPELINE_CAMERA_MOTION_VECTOR_PASS_INCLUDED

#include "../../PostProcessing/CopyPass.hlsl"

TEXTURE2D(_CameraDepthTexture);

float4 CameraMotionVectorFrag(Varyings IN) : SV_TARGET
{
    float depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, IN.positionHCS.xy, 0).r;
    float4 NDC = GetNDCFromUVAndDepth(IN.uv, depth);
    float3 currentPositionWS = TransformNDCToWorld(NDC, UNITY_MATRIX_I_VP);

    float4 currentPositionCS = mul(UNITY_MATRIX_NONJITTERED_VP, float4(currentPositionWS.xyz, 1.0));
    float4 previousPositionCS = mul(UNITY_PREV_MATRIX_NONJITTERED_VP, float4(currentPositionWS.xyz, 1.0));
    
    float2 currentPositionNDC = currentPositionCS.xy / currentPositionCS.w;
    float2 previousPositionNDC = previousPositionCS.xy / previousPositionCS.w;
    
    float2 velocity = currentPositionNDC - previousPositionNDC;
    
    #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
    #endif
    
    velocity *= 0.5;

    return float4(velocity, 0, 0);
}

#endif