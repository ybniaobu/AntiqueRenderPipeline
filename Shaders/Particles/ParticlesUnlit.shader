Shader "YPipeline/Particles/Unlit"
{
    Properties
    {
        [Header(Base Color Settings)] [Space(8)]
        [MainColor][HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseTex("Albedo Texture", 2D) = "white" {}
        
        [Header(Transparency Settings)] [Space(8)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha CutOff", Range(0.0, 1.0)) = 0.5
        
        [Header(Other Settings)] [Space(8)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
        
        [Header(Particle Settings)] [Space(8)]
        [Toggle(_FLIPBOOK_BLENDING)] _FlipbookBlending ("Flipbook Blending", Float) = 0.0
        [Toggle(_CAMERA_NEAR_FADE)] _CameraFading ("Camera Fading", Float) = 0.0
        _NearFadeDistance ("Near Fade Distance", Range(0.0, 10.0)) = 1
		_NearFadeRange ("Near Fade Range", Range(0.0, 10.0)) = 1
        [Toggle(_SOFT_PARTICLES)] _SoftParticles ("Soft Particles", Float) = 0.0
        _SoftParticlesDistance ("Soft Particles Distance", Range(0.0, 10.0)) = 0
		_SoftParticlesRange ("Soft Particles Range", Range(0.0, 10.0)) = 1
        [Toggle(_DISTORTION)] _Distortion ("Distortion", Float) = 0.0
        [NoScaleOffset] _DistortionTex ("Distortion Texture", 2D) = "bump" {}
        _DistortionStrength ("Distortion Strength", Range(0.0, 1.5)) = 0.1
        _DistortionBlend ("Distortion Blend", Range(0.0, 1.0)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "Particles Unlit"
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex ParticlesUnlitVert
            #pragma fragment ParticlesUnlitFrag

            #pragma shader_feature_local_fragment _CLIPPING
            #pragma shader_feature_local _FLIPBOOK_BLENDING
            #pragma shader_feature_local _CAMERA_NEAR_FADE
            #pragma shader_feature_local _SOFT_PARTICLES
            #pragma shader_feature_local _DISTORTION

            #pragma multi_compile_instancing
            // #pragma instancing_options procedural:ParticleInstancingSetup

            #include "ParticlesUnlitPass.hlsl"
            ENDHLSL
        }
    }
}
