#ifndef YPIPELINE_META_COMMON_INCLUDED
#define YPIPELINE_META_COMMON_INCLUDED

#include "../ShaderLibrary/UnityMetaPassLibrary.hlsl"

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
    
    // #if _USE_ROUGHNESSTEX
    //     float roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_RoughnessTex, IN.uv).r;
    //     roughness *= pow(10, _RoughnessScale);
    //     roughness = saturate(roughness);
    // #else
    //     float roughness = _Roughness;
    // #endif
    
    #if _USE_METALLICTEX
        float metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, geometryParams.uv).r;
        float metallic *= pow(10, _MetallicScale);
        float metallic = saturate(standardPBRParams.metallic);
    #else
        float metallic = _Metallic;
    #endif

    #if _USE_HYBRIDTEX
        float4 hybrid = SAMPLE_TEXTURE2D(_HybridTex, sampler_HybridTex, IN.uv).rgba;
        // roughness = saturate(hybrid.r * pow(10, _RoughnessScale));
        metallic = saturate(hybrid.g * pow(10, _MetallicScale));
    #endif
    
    float3 diffuseColor = (1.0 - metallic) * albedo;
    float3 F0 = lerp(_Specular * _Specular * float3(0.16, 0.16, 0.16), albedo, metallic);
    // roughness = clamp(roughness, 0.05, 1.0);
    // float a2 = pow(roughness, 4.0);
    
    UnityMetaParams meta = (UnityMetaParams) 0.0;
    meta.albedo = diffuseColor + F0;
    meta.emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv).rgb * _EmissionColor.rgb;
    return TransportMetaColor(meta);
}

#endif