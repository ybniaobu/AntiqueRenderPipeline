#ifndef YPIPELINE_PARTICLES_UNLIT_PASS_INCLUDED
#define YPIPELINE_PARTICLES_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Core/YPipelineCore.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionBlend)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

Texture2D _BaseTex;     SamplerState sampler_Trilinear_Repeat_BaseTex;
Texture2D _DistortionTex;

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color : COLOR;
    
    #if defined(_FLIPBOOK_BLENDING)
        float4 uv : TEXCOORD0;
        float uvBlend : TEXCOORD1;
    #else
        float2 uv : TEXCOORD0;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;

    #if defined(_FLIPBOOK_BLENDING)
        float3 uv2AndBlend : TEXCOORD1;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ParticlesUnlitVert(Attributes IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
    
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.color = IN.color;
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseTex_ST);
    OUT.uv = IN.uv.xy * baseST.xy + baseST.zw;
    #if defined(_FLIPBOOK_BLENDING)
        OUT.uv2AndBlend.xy = IN.uv.zw * baseST.xy + baseST.zw;
        OUT.uv2AndBlend.z = IN.uvBlend;
    #endif
    return OUT;
}

float4 ParticlesUnlitFrag(Varyings IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv).rgba;

    #if defined(_FLIPBOOK_BLENDING)
        albedo = lerp(albedo, SAMPLE_TEXTURE2D(_BaseTex, sampler_Trilinear_Repeat_BaseTex, IN.uv2AndBlend.xy), IN.uv2AndBlend.z);
    #endif

    albedo = albedo * baseColor * IN.color;

    float viewDepth = GetViewDepthFromSVPosition(IN.positionHCS);
    #if defined(_CAMERA_NEAR_FADE)
        float nearFadeDistance = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeDistance);
        float nearFadeRange = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeRange);
        float nearAttenuation = (viewDepth - nearFadeDistance) / nearFadeRange;
        albedo.a *= saturate(nearAttenuation);
    #endif

    float2 screenUV = IN.positionHCS.xy * _CameraBufferSize.xy;
    #if defined(_SOFT_PARTICLES)
        float sampledDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_PointClamp, screenUV, 0);
        float viewSampledDepth = GetViewDepthFromDepthTexture(sampledDepth);
        float depthDelta = abs(viewSampledDepth - viewDepth);
        float softParticlesDistance = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SoftParticlesDistance);
        float softParticlesRange = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SoftParticlesRange);
        float softParticlesAttenuation = (depthDelta - softParticlesDistance) / softParticlesRange;
        albedo.a *= saturate(softParticlesAttenuation);
    #endif
    
    #if defined(_CLIPPING)
        clip(albedo.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif

    #if defined(_DISTORTION)
        float4 packedDistortion = SAMPLE_TEXTURE2D(_DistortionTex, sampler_Trilinear_Repeat_BaseTex, IN.uv);
        #if defined(_FLIPBOOK_BLENDING)
            float4 packedDistortion2 = SAMPLE_TEXTURE2D(_DistortionTex, sampler_Trilinear_Repeat_BaseTex, IN.uv2AndBlend.xy);
            packedDistortion = lerp(packedDistortion, packedDistortion2, IN.uv2AndBlend.z);
        #endif
        float distortionStrength = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DistortionStrength);
        float3 distortion = UnpackNormalScale(packedDistortion, distortionStrength);
    
        float3 sampledColor = SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_LinearClamp, screenUV + distortion * albedo.a, 0).rgb;
        float distortionBlend = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DistortionBlend);
        albedo.rgb = lerp(sampledColor, albedo.rgb, saturate(albedo.a - distortionBlend));
        
    #endif
    
    return float4(albedo.rgb, albedo.a);
}

#endif