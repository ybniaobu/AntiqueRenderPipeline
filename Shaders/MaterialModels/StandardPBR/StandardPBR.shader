Shader "YPipeline/Shading Models/Standard PBR"
{
    Properties
    {
        [Header(Base Color Settings)] [Space(8)]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseTex("Albedo Texture", 2D) = "white" {}
        
        [Header(Specular Color Settings)] [Space(8)]
        _Specular("Dielectrics Specular Intensity", Range(0.0, 1.0)) = 0.5
        
        [Header(Hybrid Settings)] [Space(8)]
        _Roughness("Roughness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Toggle(_USE_HYBRIDTEX)] _UseHybridTex("Use Hybrid Texture?", Float) = 0
        [NoScaleOffset] _HybridTex("Hybrid Texture", 2D) = "gray" {}
        _RoughnessScale("Roughness Scale", Range(-1.0, 1.0)) = 0.0
    	_MetallicScale("Metallic Scale", Range(-1.0, 1.0)) = 0.0
        _AOScale("Ambient Occlusion Scale", Range(-1.0, 1.0)) = 0.0
        
        [Header(Normal Settings)] [Space(8)]
        [Toggle(_USE_NORMALTEX)] _UseNormalTex("use normal texture?", Float) = 0
        [NoScaleOffset] [Normal] _NormalTex("Normal Texture", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Float) = 1.0
    	
	    [Header(Emission Settings)] [Space(8)]
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _EmissionTex("Emission Texture", 2D) = "white" {}
        
    	[Header(Alpha Clipping Settings)] [Space(8)]
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha CutOff", Range(0.0, 1.0)) = 0.5
        
        [Header(Other Settings)] [Space(8)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    	
    	[HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "Forward"
            
            Tags { "LightMode" = "YPipelineForward" }
            
            ZWrite Off
            ZTest Equal
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex ForwardVert
            #pragma fragment ForwardFrag
            
            // Material Keywords
            #pragma shader_feature_local_fragment _USE_HYBRIDTEX
            #pragma shader_feature_local_fragment _USE_NORMALTEX
            
            // YPipeline keywords
            #pragma multi_compile _SHADOW_PCF _SHADOW_PCSS
            #pragma multi_compile _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile _ _SCREEN_SPACE_AMBIENT_OCCLUSION
            #pragma multi_compile _ _TAA

            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "StandardPBRInput.hlsl"
            #include "StandardPBRForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GBuffer"
            
            Tags { "LightMode" = "YPipelineGBuffer" }
            
            ZWrite Off
            ZTest Equal // 使用 depth prepass
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex GBufferVert
            #pragma fragment GBufferFrag
            
            // Material Keywords
            #pragma shader_feature_local_fragment _USE_HYBRIDTEX
            #pragma shader_feature_local_fragment _USE_NORMALTEX

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "StandardPBRInput.hlsl"
            #include "StandardPBRGBufferPass.hlsl"
            ENDHLSL
        }

        Pass
        {
        	Name "ShadowCaster"
        	
			Tags { "LightMode" = "ShadowCaster" }

			ColorMask 0
			Cull [_Cull]
			// Cull Off

			HLSLPROGRAM
			#pragma target 4.5
			
			#pragma vertex ShadowCasterVert
			#pragma fragment ShadowCasterFrag

			// Material Keywords
			#pragma shader_feature_local_fragment _CLIPPING

			// Unity defined keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "StandardPBRInput.hlsl"
			#include "../ShadowCasterCommon.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Depth"
			
			Tags { "LightMode" = "Depth" }
			
			ZWrite On
			ColorMask 0
			Cull [_Cull]
			
			HLSLPROGRAM
			#pragma target 4.5

			#pragma vertex DepthVert
			#pragma fragment DepthFrag

			// Material Keywords
			#pragma shader_feature_local_fragment _CLIPPING

			// Unity defined keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "StandardPBRInput.hlsl"
			#include "../DepthPrePassCommon.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "ThinGBuffer"
			
			Tags { "LightMode" = "ThinGBuffer" } // For Forward
			
			ZWrite On
			Cull [_Cull]
			
			HLSLPROGRAM
			#pragma target 4.5

			#pragma vertex ThinGBufferVert
			#pragma fragment ThinGBufferFrag

			// Material Keywords
			#pragma shader_feature_local_fragment _USE_HYBRIDTEX
            #pragma shader_feature_local_fragment _USE_NORMALTEX
			#pragma shader_feature_local_fragment _CLIPPING

			// Unity defined keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "StandardPBRInput.hlsl"
			#include "../ThinGBufferCommon.hlsl"
			ENDHLSL
		}
		
        Pass
        {
        	Name "Meta"
        	
			Tags { "LightMode" = "Meta" }

			Cull Off

			HLSLPROGRAM
			#pragma target 4.5
			
			#pragma vertex MetaVert
			#pragma fragment MetaFrag

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "StandardPBRInput.hlsl"
			#include "../MetaCommon.hlsl"
			ENDHLSL
		}

	    Pass
		{
			Name "MotionVectors"
            Tags { "LightMode" = "MotionVectors" }
            
            ZWrite Off
            ZTest Equal
            ColorMask RG
            Cull [_Cull]
            
            Stencil
            {
                WriteMask 1
                Ref 1
                Comp Always
                Pass Replace
            }
            
            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex MotionVectorVert
			#pragma fragment MotionVectorFrag

            // Material Keywords
            #pragma shader_feature_local_fragment _CLIPPING
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY

            // Unity defined keywords
			#pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "StandardPBRInput.hlsl"
			#include "../MotionVectorCommon.hlsl"
            ENDHLSL
		}
    }

    CustomEditor "YPipeline.Editor.StandardPBRShaderGUI"
}