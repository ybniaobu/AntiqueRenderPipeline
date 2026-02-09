Shader "Hidden/Preview/ReflectionProbePreview"
{
    Properties
    {
        _Cubemap("_Cubemap", Cube) = "white" {}
        _MipLevel("_MipLevel", Range(0.0, 7.0)) = 0.0
        _Exposure("_Exposure", Range(-16.0, 16.0)) = 0.0
    }
    
    SubShader
    {
        ZWrite On
        Cull Back

        Pass
        {
            Name "ReflectionProbePreview"
            
            HLSLPROGRAM
            
            #pragma editor_sync_compilation
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "../../../../Shaders/ShaderLibrary/Core/UnityInput.hlsl"
            #include "../../../../Shaders/ShaderLibrary/Core/UnityMatrix.hlsl"
            #include "../../../../Shaders/ShaderLibrary/Core/YPipelineMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };
            
            TEXTURECUBE(_Cubemap);
            SAMPLER(sampler_Cubemap);
            float4 _Cubemap_HDR;
            float _MipLevel;
            float _Exposure;
            int _SampleByNormal;
            
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }
            
            float4 Frag(Varyings IN) : SV_TARGET
            {
                float4 color = 0;
                
                UNITY_BRANCH
                if (!_SampleByNormal)
                {
                    float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                    float3 R = reflect(-V, normalize(IN.normalWS));
                    color = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, R, _MipLevel).rgba;
                }
                else
                {
                    float3 N = normalize(IN.normalWS);
                    color = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, N, _MipLevel).rgba;
                }
                
                color.rgb = DecodeHDREnvironment(color, _Cubemap_HDR);
                color = color * exp2(_Exposure);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ReflectionProbeSHPreview"
            
            HLSLPROGRAM
            
            #pragma editor_sync_compilation
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "../../../../Shaders/ShaderLibrary/Core/UnityInput.hlsl"
            #include "../../../../Shaders/ShaderLibrary/Core/UnityMatrix.hlsl"
            #include "../../../../Shaders/ShaderLibrary/Core/YPipelineMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "../../../../Shaders/ShaderLibrary/SphericalHarmonicsLibrary.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
            };
            
            float4 _SH[7];
            int _SampleByReflection;
            
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }
            
            float4 Frag(Varyings IN) : SV_TARGET
            {
                float3 color = 0;
                if (_SampleByReflection)
                {
                    float3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                    float3 R = reflect(-V, normalize(IN.normalWS));
                    color = SampleSphericalHarmonics(R, _SH);
                }
                else
                {
                    float3 N = normalize(IN.normalWS);
                    color = SampleSphericalHarmonics(N, _SH);
                }
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}