Shader "Hidden/YPipeline/CameraMotionVector"
{
    HLSLINCLUDE
    #include "CameraMotionVectorPass.hlsl"
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
        
        Stencil
        {
            ReadMask 128 // YStencilUsage.MotionVector
            Ref 128 // YStencilUsage.MotionVector
            Comp NotEqual
        }

        Pass
        {
            Name "Motion Vector"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CopyVert
            #pragma fragment CameraMotionVectorFrag
            ENDHLSL
        }
    }
}