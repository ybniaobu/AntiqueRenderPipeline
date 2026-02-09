Shader "Hidden/YPipeline/FinalPostProcessing"
{
    HLSLINCLUDE
    #include "FinalPostProcessingPass.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Final Post Processing" // 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment FinalPostProcessingFrag
            
            #pragma multi_compile_local_fragment _ _FXAA_QUALITY _FXAA_CONSOLE
            #pragma multi_compile_local_fragment _ _FILM_GRAIN
            
            ENDHLSL
        }
    }
}