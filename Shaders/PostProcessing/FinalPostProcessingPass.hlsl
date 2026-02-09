#ifndef YPIPELINE_FINAL_POST_PROCESSING_PASS_INCLUDED
#define YPIPELINE_FINAL_POST_PROCESSING_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "CopyPass.hlsl"
#include "../ShaderLibrary/AntiAliasing/FXAA.hlsl"

float4 _FilmGrainParams; // x: intensity, y: response
float4 _FilmGrainTexParams; // xy: CameraSize.xy / FilmGrainTexSize.xy, zw: random offset in uv

TEXTURE2D(_FilmGrainTex);

float4 FinalPostProcessingFrag(Varyings IN) : SV_TARGET
{
    float4 inputColor = SampleOffsetZero(IN.uv);
    float3 color = inputColor.rgb;

    // FXAA
    #if defined(_FXAA_QUALITY)
        color = ApplyFXAAQuality(IN.uv, inputColor);
    #elif defined(_FXAA_CONSOLE)
        color = ApplyFXAAConsole(IN.uv, inputColor);
    #endif

    // Film Grain
    #if _FILM_GRAIN
        float grain = SAMPLE_TEXTURE2D(_FilmGrainTex, sampler_LinearRepeat, IN.uv * _FilmGrainTexParams.xy + _FilmGrainTexParams.zw).w;
        grain = (grain - 0.5) * 2.0; // Remap [-1, 1]
        float lum = Luminance(color);
        lum = 1.0 - sqrt(lum);
        lum = lerp(1.0, lum, _FilmGrainParams.y);
        color += color * grain * _FilmGrainParams.x * lum;
    #endif

    color = saturate(color);
    return float4(color, inputColor.a);
}

#endif