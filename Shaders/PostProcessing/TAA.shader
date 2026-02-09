Shader "Hidden/YPipeline/TAA"
{
    HLSLINCLUDE
    #pragma target 3.5
    
    #pragma multi_compile_local_fragment _ _TAA_SAMPLE_3X3
    #pragma multi_compile_local_fragment _ _TAA_YCOCG
    #pragma multi_compile_local_fragment _ _TAA_VARIANCE
    #pragma multi_compile_local_fragment _ _TAA_CURRENT_FILTER
    #pragma multi_compile_local_fragment _ _TAA_HISTORY_FILTER
    
    #include "TAAPass.hlsl"
    
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        
        ZTest Always
        ZWrite Off
        Blend Off
        Cull Off

        Pass
        {
            Name "TAA - AABB Clamp"  // 0
            
            HLSLPROGRAM
            #pragma vertex CopyVert
            #pragma fragment TAAFrag_AABBClamp
            ENDHLSL
        }

        Pass
        {
            Name "TAA - Clip To AABB Center"  // 1
            
            HLSLPROGRAM
            #pragma vertex CopyVert
            #pragma fragment TAAFrag_ClipToAABBCenter
            ENDHLSL
        }

        Pass
        {
            Name "TAA - Clip To Filtered"  // 2
            
            HLSLPROGRAM
            #pragma vertex CopyVert
            #pragma fragment TAAFrag_ClipToFiltered
            ENDHLSL
        }
    }
}