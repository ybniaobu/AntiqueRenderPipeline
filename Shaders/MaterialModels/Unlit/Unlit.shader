Shader "YPipeline/Shading Models/Unlit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseTex("Albedo Texture", 2D) = "white" {}
        
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _EmissionTex("Emission Texture", 2D) = "white" {}
        
    	[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha CutOff", Range(0.0, 1.0)) = 0.5
    	
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
    	[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendAlpha ("Src Blend Alpha", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlendAlpha ("Dst Blend Alpha", Float) = 0
    	[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
    	// [Enum(Off, 0, On, 1)] _AlphaToCoverage ("Alpha To Coverage", Float) = 0
    	
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    	[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
        
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    	
    	[HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
    	[HideInInspector] _StencilRef ("Stencil Ref", Integer) = 1 // YStencilUsage.Unlit
    	[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Integer) = 1 // YStencilUsage.Unlit
    	[HideInInspector] _MotionVectorStencilRef ("Motion Vector Stencil Ref", Integer) = 128 // YStencilUsage.MotionVector
    	[HideInInspector] _MotionVectorStencilWriteMask ("Motion Vector Stencil Write Mask", Integer) = 128 // YStencilUsage.MotionVector
    }
    
    SubShader
    {
        Pass
        {
        	Name "Unlit Opaque"
        	
            Tags { "LightMode" = "YPipelineForward" }
	        
            ZWrite Off
            ZTest Equal
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex UnlitVert
            #pragma fragment UnlitOpaqueFrag

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
            #include "UnlitPass.hlsl"
            ENDHLSL
        }

        Pass
        {
        	Name "Unlit Transparency"
        	
            Tags { "LightMode" = "YPipelineTransparency" }
            
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            BlendOp [_BlendOp]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex UnlitVert
            #pragma fragment UnlitTransparencyFrag

            #pragma shader_feature_local_fragment _CLIPPING

            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
            #include "UnlitPass.hlsl"
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

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
            #include "UnlitGBufferPass.hlsl"
            ENDHLSL
        }

	    Pass
        {
        	Name "Unlit Hybrid"
        	
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
            
            #pragma vertex UnlitVert
            #pragma fragment UnlitHybridFrag

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
            #include "UnlitHybridPass.hlsl"
            ENDHLSL
        }

        Pass
        {
        	Name "ShadowCaster"
        	
			Tags { "LightMode" = "ShadowCaster" }

			ColorMask 0
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 4.5
			
			#pragma vertex ShadowCasterVert
			#pragma fragment ShadowCasterFrag

			#pragma shader_feature_local_fragment _CLIPPING

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
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
			
			#pragma shader_feature_local_fragment _CLIPPING

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
			#include "../SharedDepthPrePass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "ThinGBuffer"
			
			Tags { "LightMode" = "ThinGBuffer" }
			
			ZWrite On
			Cull [_Cull]
			
			HLSLPROGRAM
			#pragma target 4.5

			#pragma vertex ThinGBufferVert
			#pragma fragment ThinGBufferUnlitFrag
			
			#pragma shader_feature_local_fragment _CLIPPING

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
			#include "UnlitThinGBufferPass.hlsl"
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
			#include "UnlitInput.hlsl"
			#include "UnlitMetaPass.hlsl"
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

            #pragma shader_feature_local_fragment _CLIPPING
            #pragma shader_feature_local_vertex _ADD_PRECOMPUTED_VELOCITY

			#pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
			#include "../SharedMotionVectorPass.hlsl"
            ENDHLSL
		}
    }
    
    CustomEditor "YPipeline.Editor.UnlitShaderGUI"
}
