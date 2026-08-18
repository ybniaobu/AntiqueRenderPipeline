#ifndef YPIPELINE_ADVANCED_PBR_INPUT_INCLUDED
#define YPIPELINE_ADVANCED_PBR_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Specular;
    float _Roughness;
    float _Metallic;
    float _NormalIntensity;
    float _Anisotropy;
    float _AnisotropyRotation;
    float _Coat;
    float _CoatRoughness;
    // float _CoatNormalIntensity;
    float _Cutoff;
CBUFFER_END

Texture2D _BaseTex;             SamplerState sampler_BaseTex;
Texture2D _EmissionTex;         SamplerState sampler_EmissionTex;
Texture2D _HybridTex;           SamplerState sampler_HybridTex;
Texture2D _NormalTex;           SamplerState sampler_NormalTex;
Texture2D _AdvancedTex;         SamplerState sampler_AdvancedTex;
// Texture2D _CoatNormalTex;       SamplerState sampler_CoatNormalTex;

#endif