#ifndef YPIPELINE_COLOR_GRADING_LUT_PASS_INCLUDED
#define YPIPELINE_COLOR_GRADING_LUT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "../ShaderLibrary/Core/UnityInput.hlsl"
#include "../ShaderLibrary/ToneMappingLibrary.hlsl"

#include "CopyPass.hlsl"

float4 _ColorGradingLUTParams; // x: lut_height, y: 0.5 / lut_width, z: 0.5 / lut_height, w: lut_height / lut_height - 1

float4 _WhiteBalance;
float4 _ColorAdjustmentsParams; // x: hue, y: exposure, z: contrast, w: saturation
float4 _ColorFilter;
float4 _SMHShadows;
float4 _SMHMidtones;
float4 _SMHHighlights;
float4 _SMHRange;
float4 _LGGLift;
float4 _LGGGamma;
float4 _LGGGain;

float4 _ToneMappingParams; // x: minWhite or exposureBias

TEXTURE2D(_CurveMaster);
TEXTURE2D(_CurveRed);
TEXTURE2D(_CurveGreen);
TEXTURE2D(_CurveBlue);

TEXTURE2D(_CurveHueVsHue);
TEXTURE2D(_CurveHueVsSat);
TEXTURE2D(_CurveSatVsSat);
TEXTURE2D(_CurveLumVsSat);

// ----------------------------------------------------------------------------------------------------
// White Balance
// ----------------------------------------------------------------------------------------------------

float3 WhiteBalance(float3 color)
{
    color = LinearToLMS(color);
    color *= _WhiteBalance.rgb;
    return LMSToLinear(color);
}

// ----------------------------------------------------------------------------------------------------
// Color Adjustments
// ----------------------------------------------------------------------------------------------------

float3 ColorFilter(float3 color)
{
    return color * _ColorFilter.rgb;
}

float3 Hue(float3 color)
{
    color = RgbToHsv(color);
    float hue = color.x + _ColorAdjustmentsParams.x;
    color.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(color);
}

float3 Exposure(float3 color)
{
    return color * _ColorAdjustmentsParams.y;
}

float3 Contrast(float3 color)
{
    color = LinearToLogC(color);
    color = lerp(float3(0.5, 0.5, 0.5), color, _ColorAdjustmentsParams.z);
    return LogCToLinear(color);
}

float3 Contrast_ACES(float3 color)
{
    color = ACES_to_ACEScc(ACEScg_to_ACES(color));
    color = lerp(ACEScc_MIDGRAY, color, _ColorAdjustmentsParams.z);
    return ACES_to_ACEScg(ACEScc_to_ACES(color));
}

float3 Saturation(float3 color)
{
    float luminance = Luminance(color);
    return lerp(float3(luminance, luminance, luminance), color, _ColorAdjustmentsParams.w);
}

float3 Saturation_ACES(float3 color)
{
    float luminance = AcesLuminance(color);
    return lerp(float3(luminance, luminance, luminance), color, _ColorAdjustmentsParams.w);
}

float3 ColorAdjustments(float3 color)
{
    color = ColorFilter(color);
    color = Hue(color);
    color = Exposure(color);
    color = Saturation(color);
    color = Contrast(color);
    return max(color, 0.0);
}

float3 ColorAdjustments_ACES(float3 color)
{
    color = ColorFilter(color);
    color = Hue(color);
    color = Exposure(color);
    color = Saturation_ACES(color);
    color = Contrast_ACES(color);
    return max(color, 0.0);
}

// ----------------------------------------------------------------------------------------------------
// Color Curve
// ----------------------------------------------------------------------------------------------------
float EvaluateCurve(TEXTURE2D(curve), float t)
{
    float x = SAMPLE_TEXTURE2D(curve, sampler_LinearClamp, float2(t, 0.0)).x;
    return saturate(x);
}

float3 HSVColorCurves(float3 color)
{
    float satMult;
    float3 hsv = RgbToHsv(color);
    
    // Hue Vs Sat
    satMult = EvaluateCurve(_CurveHueVsSat, hsv.x) * 2.0;

    // Sat Vs Sat
    satMult *= EvaluateCurve(_CurveSatVsSat, hsv.y) * 2.0;

    // Lum Vs Sat
    satMult *= EvaluateCurve(_CurveLumVsSat, Luminance(color)) * 2.0;

    // Hue Vs Hue
    float hue = hsv.x;
    float offset = EvaluateCurve(_CurveHueVsHue, hue) - 0.5;
    hue += offset;
    hsv.x = RotateHue(hue, 0.0, 1.0);
    
    color = HsvToRgb(hsv);
    float luminance = Luminance(color);
    return lerp(float3(luminance, luminance, luminance), color, satMult);
}

float3 HSVColorCurves_ACES(float3 color)
{
    float satMult;
    float3 hsv = RgbToHsv(color);
    
    // Hue Vs Sat
    satMult = EvaluateCurve(_CurveHueVsSat, hsv.x) * 2.0;

    // Sat Vs Sat
    satMult *= EvaluateCurve(_CurveSatVsSat, hsv.y) * 2.0;

    // Lum Vs Sat
    satMult *= EvaluateCurve(_CurveLumVsSat, Luminance(color)) * 2.0;

    // Hue Vs Hue
    float hue = hsv.x;
    float offset = EvaluateCurve(_CurveHueVsHue, hue) - 0.5;
    hue += offset;
    hsv.x = RotateHue(hue, 0.0, 1.0);
    
    color = HsvToRgb(hsv);
    float luminance = AcesLuminance(color);
    return lerp(float3(luminance, luminance, luminance), color, satMult);
}

float3 YRGBColorCurves(float3 color)
{
    color = FastTonemap(color);
    
    const float kHalfPixel = (1.0 / 128.0) / 2.0;
    float3 c = color;

    // Y (master)
    c += kHalfPixel.xxx;
    float mr = EvaluateCurve(_CurveMaster, c.r);
    float mg = EvaluateCurve(_CurveMaster, c.g);
    float mb = EvaluateCurve(_CurveMaster, c.b);
    c = float3(mr, mg, mb);

    // RGB
    c += kHalfPixel.xxx;
    float r = EvaluateCurve(_CurveRed, c.r);
    float g = EvaluateCurve(_CurveGreen, c.g);
    float b = EvaluateCurve(_CurveBlue, c.b);
    color = float3(r, g, b);
    
    color = FastTonemapInvert(color);

    return color;
}

// ----------------------------------------------------------------------------------------------------
// Shadows Midtones Highlights
// ----------------------------------------------------------------------------------------------------

float3 ShadowsMidtonesHighlights(float3 color)
{
    float luminance = Luminance(color);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
    float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
    return color * _SMHShadows.rgb * shadowsWeight + color * _SMHMidtones.rgb * midtonesWeight + color * _SMHHighlights.rgb * highlightsWeight;
}

float3 ShadowsMidtonesHighlights_ACES(float3 color)
{
    float luminance = AcesLuminance(color);
    float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
    float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
    float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
    return color * _SMHShadows.rgb * shadowsWeight + color * _SMHMidtones.rgb * midtonesWeight + color * _SMHHighlights.rgb * highlightsWeight;
}

// ----------------------------------------------------------------------------------------------------
// Lift Gamma Gain
// ----------------------------------------------------------------------------------------------------

float3 LiftGammaGain(float3 color)
{
    color = color * _LGGGain.xyz + _LGGLift.xyz;
    color = sign(color) * pow(abs(color), _LGGGamma.xyz);
    return color;
}

// ----------------------------------------------------------------------------------------------------
// Overall Color Grading
// ----------------------------------------------------------------------------------------------------

float3 ColorGrading(float3 color)
{
    color = WhiteBalance(color);
    color = ColorAdjustments(color);
    color = YRGBColorCurves(color);
    color = HSVColorCurves(color);
    color = ShadowsMidtonesHighlights(color);
    color = LiftGammaGain(color);
    return max(color, 0.0);
}

float3 ColorGrading_ACES(float3 color)
{
    color = WhiteBalance(color);

    color = unity_to_ACEScg(color);
    color = ColorAdjustments_ACES(color);
    color = YRGBColorCurves(color);
    color = HSVColorCurves_ACES(color);
    color = ShadowsMidtonesHighlights_ACES(color);
    color = LiftGammaGain(color);
    return max(color, 0.0);
}

// ----------------------------------------------------------------------------------------------------
// Fragment
// ----------------------------------------------------------------------------------------------------

float4 ColorGradingNoneFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(color, 1.0);
}

float4 ColorGradingReinhardSimpleFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Reinhard(color), 1.0);
}

float4 ColorGradingReinhardExtendedFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Reinhard_Extended(color, _ToneMappingParams.x), 1.0);
}

float4 ColorGradingReinhardLuminanceFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Reinhard_ExtendedLuminance(color, _ToneMappingParams.x), 1.0);
}

float4 ColorGradingUncharted2FilmicFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(Uncharted2Filmic(color, _ToneMappingParams.x), 1.0);
}

float4 ColorGradingKhronosPBRNeutralFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(KhronosPBRNeutral(color), 1.0);
}

float4 ColorGradingACESFullFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading_ACES(LogCToLinear(color));
    return float4(AcesTonemap(ACEScg_to_ACES(color)), 1.0);
}

float4 ColorGradingACESStephenHillFitFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading_ACES(LogCToLinear(color));
    return float4(ACESStephenHillFit(ACEScg_to_unity(color)), 1.0);
}

float4 ColorGradingACESApproxFitFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading_ACES(LogCToLinear(color));
    return float4(ACESApproxFit(ACEScg_to_unity(color)), 1.0);
}

float4 ColorGradingAgXDefaultFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(AgXApprox_Default(color), 1.0);
}

float4 ColorGradingAgXGoldenFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(AgXApprox_Golden(color), 1.0);
}

float4 ColorGradingAgXPunchyFrag(Varyings IN) : SV_TARGET
{
    float3 color = GetLutStripValue(IN.uv, _ColorGradingLUTParams);
    color = ColorGrading(LogCToLinear(color));
    return float4(AgXApprox_Punchy(color), 1.0);
}

#endif