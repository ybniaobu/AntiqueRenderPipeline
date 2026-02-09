Shader "Hidden/YPipeline/CopyDepth"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }
        
        ZTest Always
        ZWrite On
        ColorMask 0
        Cull Off

        Pass
        {
            Name "CopyDepth"
            
            HLSLPROGRAM
            #pragma target 3.5
            
            #pragma vertex CopyVert
            #pragma fragment CopyDepthFrag

            #include "CopyDepthPass.hlsl"
            ENDHLSL
        }
    }
}