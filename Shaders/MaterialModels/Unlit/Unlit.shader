Shader "YPipeline/Shading Models/Unlit"
{
    Properties
    {
        [Header(Base Color Settings)] [Space(8)]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseTex("Albedo Texture", 2D) = "white" {}
        
        [Header(Transparency Settings)] [Space(8)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
    	[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    	[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha CutOff", Range(0.0, 1.0)) = 0.5
    	// [Enum(Off, 0, On, 1)] _AlphaToCoverage ("Alpha To Coverage", Float) = 0
        
        [Header(Emission Settings)] [Space(8)]
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 1.0)
        [NoScaleOffset] _EmissionTex("Emission Texture", 2D) = "white" {}
        
        [Header(Other Settings)] [Space(8)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
    	
    	[HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
    	[HideInInspector] _MaterialID ("Material ID", Float) = 0 // See YPipeline.MaterialID, 0 is unlit.
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
            
            Blend [_SrcBlend] [_DstBlend]
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
			
			#pragma shader_feature_local_fragment _CLIPPING

			#pragma multi_compile _ LOD_FADE_CROSSFADE

			#include "../../ShaderLibrary/Core/YPipelineCore.hlsl"
			#include "UnlitInput.hlsl"
			#include "../DepthPrePassCommon.hlsl"
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
                WriteMask 1
                Ref 1
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
			#include "../MotionVectorCommon.hlsl"
            ENDHLSL
		}
    }
    
    CustomEditor "YPipeline.Editor.UnlitShaderGUI"
}
