#ifndef YPIPELINE_CLEAR_COAT_META_PASS_INCLUDED
#define YPIPELINE_CLEAR_COAT_META_PASS_INCLUDED

#include "../../ShaderLibrary/Unity/UnityMetaPassLib.hlsl"
#include "../../ShaderLibrary/PBR/BRDFTermsLib.hlsl"

struct Attributes
{
    float4 positionOS           : POSITION;
    float2 uv                   : TEXCOORD0;
    float2 lightMapUV           : TEXCOORD1;
    float2 dynamicLightMapUV    : TEXCOORD2;
};

struct Varyings
{
    float4 positionHCS          : SV_POSITION;
    float2 uv                   : TEXCOORD0;
};

Varyings MetaVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformMetaPosition(IN.positionOS.xyz, IN.lightMapUV, IN.dynamicLightMapUV);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    return OUT;
}

float4 MetaFrag(Varyings IN) : SV_TARGET
{
    float3 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv).rgb * _BaseColor.rgb;
    
    float metallic = _Metallic;
    float coat = _Coat;

    #if _USE_HYBRIDTEX
    float4 hybrid = SAMPLE_TEXTURE2D(_HybridTex, sampler_HybridTex, IN.uv).rgba;
    metallic = hybrid.g;
    #endif
    
    #if _USE_COATTEX
    float2 coatParams = SAMPLE_TEXTURE2D(_CoatTex, sampler_CoatTex, IN.uv).rg;
    coat = coatParams.r;
    #endif
    
    float coatF0 = IORToF0(_CoatIOR);
    float Fc = coat * coatF0;
    float OneMinusFc = 1.0 - Fc;
    float3 diffuseColor = (1.0 - metallic) * albedo * OneMinusFc;
    float3 F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), albedo, metallic) * OneMinusFc;
    
    UnityMetaParams meta = (UnityMetaParams) 0.0;
    meta.albedo = diffuseColor + F0 + Fc;
    meta.emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv).rgb * _EmissionColor.rgb * OneMinusFc;
    return TransportMetaColor(meta);
}

#endif