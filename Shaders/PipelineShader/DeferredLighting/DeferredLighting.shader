Shader "Hidden/YPipeline/DeferredLighting"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            Name "Deferred Lighting"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex FullScreenVert
            #pragma fragment DeferredLightingFrag
            
            // YPipeline keywords
            #pragma multi_compile _SHADOW_PCF _SHADOW_PCSS
            #pragma multi_compile _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile _ _SCREEN_SPACE_AMBIENT_OCCLUSION
            #pragma multi_compile _ _TAA
            
            // Unity defined keywords
            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            
            #include "DeferredLightingPass.hlsl"
            ENDHLSL
        }
    }
}