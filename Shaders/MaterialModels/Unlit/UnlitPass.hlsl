#ifndef YPIPELINE_UNLIT_PASS_INCLUDED
#define YPIPELINE_UNLIT_PASS_INCLUDED

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

float4 UnlitOpaqueFrag(Varyings IN) : SV_Target
{
    float3 emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv).rgb * _EmissionColor.rgb;
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv).rgba * _BaseColor;

    return float4(albedo.rgb + emission, 1.0);
}

float4 UnlitTransparencyFrag(Varyings IN) : SV_Target
{
    float3 emission = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, IN.uv).rgb * _EmissionColor.rgb;
    float4 albedo = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv).rgba * _BaseColor;
    
    #if defined(_CLIPPING)
        clip(albedo.a - _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        float dither = InterleavedGradientNoise(IN.positionHCS.xy, 0);
        float isNextLodLevel = step(unity_LODFade.x, 0);
        dither = lerp(-dither, dither, isNextLodLevel);
        clip(unity_LODFade.x + dither);
    #endif

    return float4(albedo.rgb + emission, albedo.a);
}

#endif