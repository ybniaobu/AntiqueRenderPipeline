Shader "Hidden/YPipeline/Copy"
{
    Properties
    {
        _BlitTexture("_BlitTexture", 2D) = "black" {}
        _ScaleOffset("_ScaleOffset", Vector) = (1.0, 1.0, 0.0, 0.0)
    }
    
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
            Name "Copy"
            
            HLSLPROGRAM
            #pragma target 3.5
            
            #pragma vertex CopyVert
            #pragma fragment CopyFrag

            #include "CopyPass.hlsl"
            ENDHLSL
        }
    }
}