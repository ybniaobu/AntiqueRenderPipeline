#ifndef YPIPELINE_STANDARD_PBR_FOR_TEST_INPUT_INCLUDED
#define YPIPELINE_STANDARD_PBR_FOR_TEST_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Specular;
    float _Roughness;
    float _RoughnessScale;
    float _Metallic;
    float _MetallicScale;
    float _NormalIntensity;
    float _AOScale;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_BaseTex;
Texture2D _EmissionTex;         SamplerState sampler_EmissionTex;
Texture2D _RoughnessTex;        SamplerState sampler_RoughnessTex;
Texture2D _MetallicTex;         SamplerState sampler_MetallicTex;
Texture2D _NormalTex;           SamplerState sampler_NormalTex;
Texture2D _AOTex;               SamplerState sampler_AOTex;

#endif