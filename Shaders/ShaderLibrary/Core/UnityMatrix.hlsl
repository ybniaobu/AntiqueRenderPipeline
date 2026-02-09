#ifndef YPIPELINE_UNITY_MATRIX_INCLUDED
#define YPIPELINE_UNITY_MATRIX_INCLUDED

// ----------------------------------------------------------------------------------------------------
// Non-Builtin Camera Matrices
// ----------------------------------------------------------------------------------------------------

CBUFFER_START(MatricesPerFrame)
    float4x4 _MatrixIP; // influenced by jitter
    float4x4 _MatrixIVP; // influenced by jitter
    float4x4 _MatrixNonJitteredVP; // non-jittered
    float4x4 _MatrixNonJitteredIVP; // non-jittered
    float4x4 _MatrixPreviousVP; // influenced by jitter
    float4x4 _MatrixPreviousIVP; // influenced by jitter
    float4x4 _MatrixNonJitteredPreviousVP; // non-jittered
    float4x4 _MatrixNonJitteredPreviousIVP; // non-jittered
CBUFFER_END

// ----------------------------------------------------------------------------------------------------
// for SpaceTransforms.hlsl compatibility
// ----------------------------------------------------------------------------------------------------

#define UNITY_MATRIX_M          unity_ObjectToWorld
#define UNITY_MATRIX_I_M        unity_WorldToObject
#define UNITY_MATRIX_V          unity_MatrixV
#define UNITY_MATRIX_I_V        unity_MatrixInvV
#define UNITY_MATRIX_P          glstate_matrix_projection
//#define UNITY_MATRIX_I_P        unity_MatrixInvP
#define UNITY_MATRIX_VP         unity_MatrixVP
//#define UNITY_MATRIX_I_VP       unity_MatrixInvVP
#define UNITY_MATRIX_MV         mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV       transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV      transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP        mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#define UNITY_PREV_MATRIX_M     unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M   unity_MatrixPreviousMI

// ----------------------------------------------------------------------------------------------------
// Non Unity Builtin Matrix Macros
// ----------------------------------------------------------------------------------------------------

#define UNITY_MATRIX_I_P                    _MatrixIP 
#define UNITY_MATRIX_I_VP                   _MatrixIVP
#define UNITY_MATRIX_NONJITTERED_VP         _MatrixNonJitteredVP
#define UNITY_MATRIX_NONJITTERED_I_VP       _MatrixNonJitteredIVP
#define UNITY_PREV_MATRIX_VP                _MatrixPreviousVP
#define UNITY_PREV_MATRIX_I_VP              _MatrixPreviousIVP
#define UNITY_PREV_MATRIX_NONJITTERED_VP    _MatrixNonJitteredPreviousVP
#define UNITY_PREV_MATRIX_NONJITTERED_I_VP  _MatrixNonJitteredPreviousIVP

#endif