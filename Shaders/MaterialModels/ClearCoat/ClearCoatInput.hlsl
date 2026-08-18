#ifndef YPIPELINE_CLEAR_COAT_INPUT_INCLUDED
#define YPIPELINE_CLEAR_COAT_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float4 _CoatColor;
    float _Specular;
    float _Roughness;
    float _Metallic;
    float _NormalIntensity;
    float _Coat;
    float _CoatRoughness;
    float _CoatIOR;
    float _CoatNormalIntensity;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_BaseTex;
Texture2D _EmissionTex;         SamplerState sampler_EmissionTex;
Texture2D _HybridTex;           SamplerState sampler_HybridTex;
Texture2D _NormalTex;           SamplerState sampler_NormalTex;
Texture2D _CoatTex;             SamplerState sampler_CoatTex;
Texture2D _CoatColorTex;        SamplerState sampler_CoatColorTex;
Texture2D _CoatNormalTex;       SamplerState sampler_CoatNormalTex;

#endif