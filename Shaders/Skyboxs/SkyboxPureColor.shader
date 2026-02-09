Shader "YPipeline/Skybox/PureColor"
{
    Properties
    {
        [MainColor] [HDR] _Color("Skybox Color", Color) = (0.1, 0.1, 0.1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "PreviewType"="Skybox"
        }
        
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "../ShaderLibrary/Core/YPipelineCore.hlsl"

            float4 _Color;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                return float4(_Color.xyz, 1.0);
            }
            ENDHLSL
        }
    }
}