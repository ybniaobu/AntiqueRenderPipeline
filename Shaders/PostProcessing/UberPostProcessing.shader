Shader "Hidden/YPipeline/UberPostProcessing"
{
    HLSLINCLUDE
    #include "UberPostProcessingPass.hlsl"
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
            Name "Uber Post Processing" // 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment UberPostProcessingFrag

            #pragma multi_compile_local_fragment _ _CHROMATIC_ABERRATION
            #pragma multi_compile_local_fragment _ _BLOOM
            #pragma multi_compile_local_fragment _ _BLOOM_BICUBIC_UPSAMPLING
            #pragma multi_compile_local_fragment _ _VIGNETTE
            #pragma multi_compile_local_fragment _ _EXTRA_LUT
            
            ENDHLSL
        }
    }
}