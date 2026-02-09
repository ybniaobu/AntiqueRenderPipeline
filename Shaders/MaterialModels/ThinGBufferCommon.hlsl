#ifndef YPIPELINE_THIN_GBUFFER_COMMON_INCLUDED
#define YPIPELINE_THIN_GBUFFER_COMMON_INCLUDED

#include "../ShaderLibrary/EncodingLibrary.hlsl"

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

float4 ThinGBufferFrag(Varyings IN, out float depth: SV_DEPTH) : SV_TARGET
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv) * _BaseColor;

    #if defined(_CLIPPING)
        clip(albedo.a - _Cutoff);
    #endif
        
    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif

    #if _USE_ROUGHNESSTEX
        float roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_RoughnessTex, IN.uv).r;
        roughness *= pow(10, _RoughnessScale);
        roughness = saturate(roughness);
    #else
        float roughness = _Roughness;
    #endif

    #if _USE_HYBRIDTEX
        float4 hybrid = SAMPLE_TEXTURE2D(_HybridTex, sampler_HybridTex, IN.uv).rgba;
        roughness = saturate(hybrid.r * pow(10, _RoughnessScale));
    #endif

    #if _USE_NORMALTEX
        float4 packedNormal = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, IN.uv);
        float3 normalTS = UnpackNormalScale(packedNormal, _NormalIntensity);
        float3 n = normalize(IN.normalWS);
        float3 t = normalize(IN.tangentWS.xyz);
        float3 b = normalize(cross(n, t) * IN.tangentWS.w);
        float3x3 tbn = float3x3(t, b, n);
        float3 N = normalize(mul(normalTS, tbn));
    #else
        float3 N = normalize(IN.normalWS);
    #endif

    depth = IN.positionHCS.z;
    float3 encodedNormal = EncodeNormalInto888(N);
    return float4(encodedNormal, roughness);
}

#endif