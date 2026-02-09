#ifndef YPIPELINE_UNLIT_THIN_GBUFFER_PASS_INCLUDED
#define YPIPELINE_UNLIT_THIN_GBUFFER_PASS_INCLUDED

#include "../../ShaderLibrary/EncodingLibrary.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float4 tangentWS    : TEXCOORD3;
};

Varyings ThinGBufferVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
    return OUT;
}

float4 ThinGBufferUnlitFrag(Varyings IN, out float depth: SV_DEPTH) : SV_TARGET
{
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
    
    float3 N = normalize(IN.normalWS);
    depth = IN.positionHCS.z;

    return float4(EncodeNormalInto888(N), 1);
}

#endif