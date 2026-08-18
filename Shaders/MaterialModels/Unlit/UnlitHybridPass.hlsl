#ifndef YPIPELINE_UNLIT_HYBRID_PASS_INCLUDED
#define YPIPELINE_UNLIT_HYBRID_PASS_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

Varyings UnlitVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    return OUT;
}

float4 UnlitHybridFrag(Varyings IN) : SV_Target
{
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv).rgba * _BaseColor;
    return float4(albedo.rgb, 1.0);
}

#endif