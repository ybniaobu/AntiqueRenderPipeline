Shader "YPipeline/EditorTool/EnvMapPrefilter"
{
    Properties
    {
        [NoScaleOffset] _Cubemap ("Cubemap (HDR)", Cube) = "grey" {}
        _Rotation ("Rotation", Range(0, 360)) = 270
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Roughness ("Roughness", Range(0, 1)) = 0
        _SampleNumber ("Sample Number", Int) = 2048
        _ResolutionPerFace ("Resolution Per Source Cubemap Face", Int) = 2048
    }
    
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "../../../../Shaders/ShaderLibrary/Core/YPipelineCore.hlsl"
            #include "../../../../Shaders/ShaderLibrary/IBLLibrary.hlsl"
            #include "../../../../Shaders/ShaderLibrary/ToneMappingLibrary.hlsl"

            TextureCube _Cubemap; SamplerState trilinear_repeat_sampler_Cubemap;

            CBUFFER_START(UnityPerMaterial)
                float4 _Cubemap_HDR;
                float _Rotation;
                float _Exposure;
                float _Roughness;
                float _SampleNumber;
                float _ResolutionPerFace;
            CBUFFER_END

            float3 RotateAroundYInDegrees (float3 positionOS, float degrees)
            {
                float alpha = degrees * PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, positionOS.xz), positionOS.y).xzy;
            }

            float GammaToLinearSpaceExact (float value)
            {
                if (value <= 0.04045F)
                    return value / 12.92F;
                else if (value < 1.0F)
                    return pow((value + 0.055F)/1.055F, 2.4F);
                else
                    return pow(value, 2.2F);
            }
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 sampleDir : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 rotated = RotateAroundYInDegrees(IN.positionOS.xyz, _Rotation);
                OUT.positionHCS = TransformObjectToHClip(rotated);
                OUT.sampleDir = IN.positionOS.xyz;
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float3 prefilteredColor = PrefilterEnvMap_GGX(_Cubemap, trilinear_repeat_sampler_Cubemap, _SampleNumber, _ResolutionPerFace, _Roughness, IN.sampleDir);
                float3 color = prefilteredColor.rgb * _Exposure;
                return float4(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b), 1.0f); //RenderToCubemap API will do gamma 0.45
            }
            ENDHLSL
        }
    }
}
