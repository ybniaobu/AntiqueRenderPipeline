#ifndef YPIPELINE_STANDARD_PBR_FOR_TEST_GBUFFER_PASS_INCLUDED
#define YPIPELINE_STANDARD_PBR_FOR_TEST_GBUFFER_PASS_INCLUDED

#include "../../../ShaderLibrary/Core/GBufferCommon.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float4 tangentWS    : TEXCOORD3;
};

Varyings GBufferVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
    OUT.tangentWS = float4(TransformObjectToWorldDir(IN.tangentOS.xyz), IN.tangentOS.w);
    return OUT;
}

GBufferOutput GBufferFrag(Varyings IN)
{
    GBufferOutput OUT;
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
    
    float3 emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv).rgb * _EmissionColor.rgb;
    
    #if _USE_ROUGHNESSTEX
        float roughness = SAMPLE_TEXTURE2D(_RoughnessTex, sampler_RoughnessTex, IN.uv).r;
        roughness *= pow(10, _RoughnessScale);
        roughness = saturate(roughness);
    #else
        float roughness = _Roughness;
    #endif

    #if _USE_METALLICTEX
        float metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, IN.uv).r;
        metallic *= pow(10, _MetallicScale);
        metallic = saturate(metallic);
    #else
        float metallic = _Metallic;
    #endif

    float ao = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, IN.uv).r;
    ao *= pow(0.1, _AOScale);
    ao = saturate(ao);
    
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
    
    OUT.gBuffer0 = float4(albedo.rgb, ao);
    OUT.gBuffer1 = float4(EncodeNormalInto888(N), roughness);
    OUT.gBuffer2 = float4(_Specular, metallic, 0.0, PackMaterialID(MATERIALID_STANDARD_PBR));
    OUT.gBuffer3 = emission;
    
    return OUT;
}

#endif