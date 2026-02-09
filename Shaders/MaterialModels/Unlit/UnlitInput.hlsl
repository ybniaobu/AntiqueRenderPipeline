#ifndef YPIPELINE_UNLIT_INPUT_INCLUDED
#define YPIPELINE_UNLIT_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseTex_ST;
    float4 _EmissionColor;
    float _Cutoff;
CBUFFER_END

TEXTURE2D(_BaseTex);        SAMPLER(sampler_BaseTex);
TEXTURE2D(_EmissionTex);    SAMPLER(sampler_EmissionTex);

#endif