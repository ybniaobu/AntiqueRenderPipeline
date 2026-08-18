#ifndef YPIPELINE_SHARED_DEPTH_PREPASS_INCLUDED
#define YPIPELINE_SHARED_DEPTH_PREPASS_INCLUDED

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

Varyings DepthVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    return OUT;
}

float DepthFrag(Varyings IN) : SV_DEPTH
{
    #if defined(_CLIPPING)
        float alpha = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv).a * _BaseColor.a;
        clip(alpha - _Cutoff);
    #endif
        
    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(IN.positionHCS.xy);
    #endif

    return IN.positionHCS.z;
}

#endif