#ifndef YPIPELINE_MOTION_VECTOR_COMMON_INCLUDED
#define YPIPELINE_MOTION_VECTOR_COMMON_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    
    float3 previousPositionOS : TEXCOORD4;
    #if _ADD_PRECOMPUTED_VELOCITY
        float3 precomputedVelocity : TEXCOORD5;
    #endif
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nonJitterPositionHCS : TEXCOORD1;
    float4 previousNonJitterPositionHCS : TEXCOORD2;
};

Varyings MotionVectorVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);

    OUT.nonJitterPositionHCS = mul(UNITY_MATRIX_NONJITTERED_VP, mul(UNITY_MATRIX_M, float4(IN.positionOS.xyz, 1.0)));

    // Skin or morph
    float4 previousPositionOS = unity_MotionVectorsParams.x > 0.0 ? float4(IN.previousPositionOS, 1.0) : float4(IN.positionOS.xyz, 1.0);
    
    #if _ADD_PRECOMPUTED_VELOCITY
        previousPositionOS = previousPositionOS - float4(IN.precomputedVelocity, 0);
    #endif
    
    OUT.previousNonJitterPositionHCS = mul(UNITY_PREV_MATRIX_NONJITTERED_VP, mul(UNITY_PREV_MATRIX_M, previousPositionOS));

    return OUT;
}

float4 MotionVectorFrag(Varyings IN) : SV_Target
{
    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion) return float4(0.0, 0.0, 0.0, 0.0);

    #if defined(_CLIPPING)
        float alpha = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv).a * _BaseColor.a;
        clip(alpha - _Cutoff);
    #endif
        
    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif
    
    float2 currentPositionNDC = IN.nonJitterPositionHCS.xy / IN.nonJitterPositionHCS.w;
    float2 previousPositionNDC = IN.previousNonJitterPositionHCS.xy / IN.previousNonJitterPositionHCS.w;

    float2 velocity = currentPositionNDC - previousPositionNDC;

    #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
    #endif
    
    velocity *= 0.5;

    return float4(velocity, 0, 0);
}

#endif