Shader "Hidden/YPipeline/Debug/LightCullingDebug"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Light Culling Debug"
            
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma vertex Vert
            #pragma fragment Frag

            #include "LightCullingDebugPass.hlsl"
            ENDHLSL
        }
    }
}
