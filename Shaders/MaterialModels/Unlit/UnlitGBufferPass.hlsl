#ifndef YPIPELINE_UNLIT_GBUFFER_PASS_INCLUDED
#define YPIPELINE_UNLIT_GBUFFER_PASS_INCLUDED

#include "../../ShaderLibrary/Core/GBufferCommon.hlsl"

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
        LODFadeCrossFade(IN.positionHCS.xy);
    #endif
    
    float3 emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv).rgb * _EmissionColor.rgb;
    float3 N = normalize(IN.normalWS);
    
    OUT.colorAttachment = float4(emission, 1.0);
    OUT.gBuffer0 = float4(albedo.rgb, 1.0);
    OUT.gBuffer1 = float4(EncodeNormalInto888(N), 1.0);
    // OUT.gBuffer2 = float4(0.0, 0.0, 0.0, PackMaterialID(MATERIALID_STANDARD_PBR));
    OUT.gBuffer2 = float4(0.5, 0.0, 0.0, PackMaterialID(0.0)); // materialID is not used for now
    OUT.gBuffer3 = float4(0.0, 0.0, 0.0, 0.0);
    
    return OUT;
}

#endif