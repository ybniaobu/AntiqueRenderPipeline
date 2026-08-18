Shader "YPipeline/Shading Models/Advanced PBR"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseTex("Albedo Texture", 2D) = "white" {}
        
        _Specular("Dielectrics Specular Intensity", Range(0.0, 1.0)) = 0.5
        
        _Roughness("Roughness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Toggle(_USE_HYBRIDTEX)] _UseHybridTex("Use Hybrid Texture?", Float) = 0
        [NoScaleOffset] _HybridTex("Hybrid Texture", 2D) = "white" {}
        _RoughnessScale("Roughness Scale", Range(-1.0, 1.0)) = 0.0
    	_MetallicScale("Metallic Scale", Range(-1.0, 1.0)) = 0.0
        _AOScale("Ambient Occlusion Scale", Range(-1.0, 1.0)) = 0.0
        
        [Toggle(_USE_NORMALTEX)] _UseNormalTex("Use Normal Texture?", Float) = 0
        [NoScaleOffset] [Normal] _NormalTex("Normal Texture", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Float) = 1.0
    	
    	_Anisotropy("Anisotropy", Range(0.0, 1.0)) = 0.0
    	_AnisotropyRotation("Anisotropy Rotation", Range(0.0, 1.0)) = 0.0
    	_ClearCoat("Clear Coat", Range(0.0, 1.0)) = 0.0
    	_ClearCoatRoughness("Clear Coat Roughness", Range(0.0, 1.0)) = 0.05
    	[Toggle(_USE_ADVANCEDTEX)] _UseAdvancedTex("Use Advanced Texture?", Float) = 0
    	[NoScaleOffset] _AdvancedTex("Advanced Texture", 2D) = "white" {}
    	[Toggle(_USE_CLEARCOATNORMALTEX)] _UseClearCoatNormalTex("Use Clear Coat Normal Texture?", Float) = 0
    	[NoScaleOffset] [Normal] _ClearCoatNormalTex("Clear Coat Normal Texture", 2D) = "bump" {}
        
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _EmissionTex("Emission Texture", 2D) = "white" {}
        
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha CutOff", Range(0.0, 1.0)) = 0.5
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    	
    	[HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
        [HideInInspector] _StencilRef ("Stencil Ref", Integer) = 4 // YStencilUsage.AdvancedPBR
    	[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Integer) = 4 // YStencilUsage.AdvancedPBR
    	[HideInInspector] _MotionVectorStencilRef ("Motion Vector Stencil Ref", Integer) = 128 // YStencilUsage.MotionVector
    	[HideInInspector] _MotionVectorStencilWriteMask ("Motion Vector Stencil Write Mask", Integer) = 128 // YStencilUsage.MotionVector
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
            #pragma shader_feature_local_fragment _USE_ADVANCEDTEX
            #pragma shader_feature_local_fragment _USE_CLEARCOATNORMALTEX
            
            // YPipeline keywords
            #pragma multi_compile _ _EDITOR_PREVIEW
            #pragma multi_compile _SHADOW_PCF _SHADOW_PCSS
            #pragma multi_compile _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile _ _SCREEN_SPACE_AMBIENT_OCCLUSION
            #pragma multi_compile _ _TAA

            // Unity defined keywords
            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "AdvancedPBRInput.hlsl"
            #include "AdvancedPBRForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GBuffer"
            
            Tags { "LightMode" = "YPipelineGBuffer" }
            
            ZWrite Off
            ZTest Equal // 使用 depth prepass
            Cull [_Cull]
            
            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex GBufferVert
            #pragma fragment GBufferFrag
            
            // Material Keywords
            #pragma shader_feature_local_fragment _USE_HYBRIDTEX
            #pragma shader_feature_local_fragment _USE_NORMALTEX

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "AdvancedPBRInput.hlsl"
            #include "../SharedGBufferPass.hlsl"
            ENDHLSL
        }

	    Pass
        {
        	Name "Hybrid"
        	
            Tags { "LightMode" = "YPipelineHybrid" }
	        
            Blend One One // emission additive
            ZWrite Off
            ZTest Equal
            Cull [_Cull]
            
	        Stencil
            {
                ReadMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Equal
                Pass Zero
            }
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex HybridVert
            #pragma fragment HybridFrag
            
            // Material Keywords
            #pragma shader_feature_local_fragment _USE_ADVANCEDTEX
            #pragma shader_feature_local_fragment _USE_CLEARCOATNORMALTEX
            
            // YPipeline keywords
            #pragma multi_compile _SHADOW_PCF _SHADOW_PCSS
            #pragma multi_compile _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile _ _SCREEN_SPACE_AMBIENT_OCCLUSION
            #pragma multi_compile _ _TAA
            
            // Unity defined keywords
            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "AdvancedPBRInput.hlsl"
            #include "AdvancedPBRHybridPass.hlsl"
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
			#include "AdvancedPBRInput.hlsl"
			#include "../SharedShadowCasterPass.hlsl"
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
			#include "AdvancedPBRInput.hlsl"
			#include "../SharedDepthPrePass.hlsl"
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
			#include "AdvancedPBRInput.hlsl"
			#include "../SharedThinGBufferPass.hlsl"
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
			#include "AdvancedPBRInput.hlsl"
			#include "../SharedMetaPass.hlsl"
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
                WriteMask [_MotionVectorStencilWriteMask]
                Ref [_MotionVectorStencilRef]
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
			#include "AdvancedPBRInput.hlsl"
			#include "../SharedMotionVectorPass.hlsl"
            ENDHLSL
		}
    }
}