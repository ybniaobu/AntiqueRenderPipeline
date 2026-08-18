#ifndef YPIPELINE_SHARED_SHADOW_CASTER_PASS_INCLUDED
#define YPIPELINE_SHARED_SHADOW_CASTER_PASS_INCLUDED

float _ShadowPancaking;

struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float2 uv           : TEXCOORD0;
};

Varyings ShadowCasterVert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

    #if UNITY_REVERSED_Z
    float clamped = min(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
    float clamped = max(OUT.positionHCS.z, OUT.positionHCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    OUT.positionHCS.z = lerp(OUT.positionHCS.z, clamped, _ShadowPancaking);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseTex);
    return OUT;
}

void ShadowCasterFrag(Varyings IN)
{
    #if defined(_CLIPPING)
        float alpha = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv).a * _BaseColor.a;
        clip(alpha - _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(IN.positionHCS.xy);
    #endif
}

#endif