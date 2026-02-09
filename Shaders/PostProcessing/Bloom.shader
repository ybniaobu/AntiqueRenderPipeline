Shader "Hidden/YPipeline/Bloom"
{
    HLSLINCLUDE
    #include "BloomPass.hlsl"
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
            Name "Bloom Prefilter" // 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment BloomPrefilterFrag
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Gaussian Blur Horizontal" // 1
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment BloomGaussianBlurHorizontalFrag
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Gaussian Blur Vertical" // 2
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment BloomGaussianBlurVerticalFrag
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Additive Upsample" // 3
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment BloomAdditiveUpsampleFrag
            #pragma multi_compile_local_fragment _ _BLOOM_BICUBIC_UPSAMPLING
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Scattering Upsample" // 4
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment BloomScatteringUpsampleFrag
            #pragma multi_compile_local_fragment _ _BLOOM_BICUBIC_UPSAMPLING
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Scattering Final Blit" // 5
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment BloomScatteringFinalBlitFrag
            #pragma multi_compile_local_fragment _ _BLOOM_BICUBIC_UPSAMPLING
            ENDHLSL
        }
    }
}
