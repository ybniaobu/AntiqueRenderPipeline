//----------------------------------------------------------------------------------
// This file was modified from: https://github.com/hghdev/NVIDIAGameWorks-GraphicsSamples/blob/master/samples/es3-kepler/FXAA/FXAA3_11.h
//----------------------------------------------------------------------------------

//----------------------------------------------------------------------------------
// File:        es3-kepler\FXAA/FXAA3_11.h
// SDK Version: v3.00 
// Email:       gameworks@nvidia.com
// Site:        http://developer.nvidia.com/
//
// Copyright (c) 2014-2015, NVIDIA CORPORATION. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//  * Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
//  * Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
//  * Neither the name of NVIDIA CORPORATION nor the names of its
//    contributors may be used to endorse or promote products derived
//    from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ``AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
// OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//----------------------------------------------------------------------------------

#ifndef YPIPELINE_FXAA_INCLUDED
#define YPIPELINE_FXAA_INCLUDED

// ----------------------------------------------------------------------------------------------------
// FXAA Quality Marcos
// ----------------------------------------------------------------------------------------------------

// #define FXAA_QUALITY_LOW
// #define FXAA_QUALITY_MEDIUM

#if defined(FXAA_QUALITY_LOW)
    #define EXTRA_EDGE_STEPS 5
    #define EDGE_STEP_SIZES 1.0, 1.5, 2.0, 2.0, 2.0
    #define LAST_EDGE_STEP_GUESS 8.0
#elif defined(FXAA_QUALITY_MEDIUM)
    #define EXTRA_EDGE_STEPS 8
    #define EDGE_STEP_SIZES 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 2.0, 4.0
    #define LAST_EDGE_STEP_GUESS 8.0
#else
    #define EXTRA_EDGE_STEPS 12
    #define EDGE_STEP_SIZES 1.0, 1.0, 1.0, 1.0, 1.0, 1.5, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0
    #define LAST_EDGE_STEP_GUESS 8.0
#endif

// 0.333 - too little (faster)
// 0.250 - low quality
// 0.166 - default
// 0.125 - high quality 
// 0.063 - overkill (slower)
#define FXAA_QUALITY_RELATIVE_THRESHOLD 0.125

// 0.0833 - upper limit (default, the start of visible unfiltered edges)
// 0.0625 - high quality (faster)
// 0.0312 - visible limit (slower)
#define FXAA_QUALITY_CONTRAST_THRESHOLD 0.0625

// 1.00 - upper limit (softer)
// 0.75 - default amount of filtering
// 0.50 - lower limit (sharper, less sub-pixel aliasing removal)
// 0.25 - almost off
// 0.00 - completely off
#define FXAA_QUALITY_BLEND_FACTOR 1.00

// ----------------------------------------------------------------------------------------------------
// FXAA Console Marcos
// ----------------------------------------------------------------------------------------------------

// 0.125 leaves less aliasing, but is softer (default!!!)
// 0.25 leaves more aliasing, and is sharper
#define FXAA_CONSOLE_RELATIVE_THRESHOLD 0.125

// 0.06 - faster but more aliasing in darks
// 0.05 - default
// 0.04 - slower and less aliasing in darks
#define FXAA_CONSOLE_CONTRAST_THRESHOLD 0.04

// 0.50 - default
// 0.33 (sharper)
#define FXAA_CONSOLE_SCALE 0.5

// 8.0 is sharper (default!!!)
// 4.0 is softer
// 2.0 is really soft (good only for vector graphics inputs)
#define FXAA_CONSOLE_EDGE_SHARPNESS 4

// ----------------------------------------------------------------------------------------------------
// FXAA Quality and Console Function
// ----------------------------------------------------------------------------------------------------

static const float EdgeStepSizes[EXTRA_EDGE_STEPS] = { EDGE_STEP_SIZES };

//#define TEXEL_SIZE _BlitTexture_TexelSize
#define TEXEL_SIZE 1.0 / _ScreenParams

inline float GetLuma(float3 rgb)
{
    return sqrt(Luminance(rgb));
}

inline float GetLuma(float4 rgba)
{
    return sqrt(Luminance(rgba));
}

inline float4 SampleOffset(float2 uv, float uOffset, float vOffset)
{
    uv += float2(uOffset, vOffset) * TEXEL_SIZE.xy;
    return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
}

inline float4 SampleOffsetZero(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
}

float3 ApplyFXAAQuality(float2 uv, float4 middleColor)
{
    float M  = GetLuma(middleColor);
    float N  = GetLuma(SampleOffset(uv,  0,  1));
    float E  = GetLuma(SampleOffset(uv,  1,  0));
    float S  = GetLuma(SampleOffset(uv,  0, -1));
    float W  = GetLuma(SampleOffset(uv, -1,  0));
    float NW = GetLuma(SampleOffset(uv, -1,  1));
    float NE = GetLuma(SampleOffset(uv,  1,  1));
    float SW = GetLuma(SampleOffset(uv, -1, -1));
    float SE = GetLuma(SampleOffset(uv,  1, -1));

    // ------------------------- Calculate Contrast -------------------------
    
    float maxLuma = max(max(max(N, E), max(W, S)), M);
    float minLuma = min(min(min(N, E), min(W, S)), M);
    float contrast = maxLuma - minLuma;

    // ------------------------- Threshold -------------------------
    
    if (contrast < max(FXAA_QUALITY_CONTRAST_THRESHOLD, maxLuma * FXAA_QUALITY_RELATIVE_THRESHOLD)) return middleColor.rgb;

    // ------------------------- Measure Blend Direction -------------------------
    
    float horizontal = abs(N + S - 2.0 * M) * 2.0 + abs(NE + SE - 2.0 * E) + abs(NW + SW - 2.0 * W);
    float vertical   = abs(E + W - 2.0 * M) * 2.0 + abs(NE + NW - 2.0 * N) + abs(SE + SW - 2.0 * S);
    bool isHorizontal = horizontal > vertical;
    float2 pixelStep = isHorizontal ? float2(0, TEXEL_SIZE.y) : float2(TEXEL_SIZE.x, 0);
    float positive = abs((isHorizontal ? N : E) - M);
    float negative = abs((isHorizontal ? S : W) - M);
    // if (positive < negative) pixelStep = -pixelStep;

    // ------------------------- Reserve Blending Pixel Gradient & Luma -------------------------
    
    float gradient, oppositeLuma;
    if (positive > negative)
    {
        gradient = positive;
        oppositeLuma = isHorizontal ? N : E;
    }
    else
    {
        pixelStep = -pixelStep;
        gradient = negative;
        oppositeLuma = isHorizontal ? S : W;
    }

    // ------------------------- Blend Factor Calculation -------------------------
    
    float filter = 2.0 * (N + E + S + W) + NE + NW + SE + SW;
    filter = filter / 12.0;
    filter = abs(filter - M);
    filter = saturate(filter / contrast);
    float blendFactor = smoothstep(0.0, 1.0, filter);
    blendFactor = blendFactor * blendFactor * FXAA_QUALITY_BLEND_FACTOR;

    // ------------------------- Edge Searching -------------------------
    
    float2 edgeUV = uv;
    edgeUV += pixelStep * 0.5f;
    float2 edgeStep = isHorizontal ? float2(TEXEL_SIZE.x, 0) : float2(0, TEXEL_SIZE.y);

    float edgeLuma = (M + oppositeLuma) * 0.5f;
    float gradientThreshold = gradient * 0.25f;

    float2 uvP = edgeUV;
    float2 uvN = edgeUV;
    float lumaDeltaP, lumaDeltaN, distanceP, distanceN;
    int i;
    
    UNITY_UNROLL
    for (i = 1; i <= EXTRA_EDGE_STEPS; i++)
    {
        uvP += EdgeStepSizes[i - 1] * edgeStep;
        lumaDeltaP = GetLuma(SampleOffsetZero(uvP)) - edgeLuma;
        if (abs(lumaDeltaP) > gradientThreshold) break;
    }
    if (i == EXTRA_EDGE_STEPS + 1)
    {
        uvP += LAST_EDGE_STEP_GUESS * edgeStep;
    }
    
    UNITY_UNROLL
    for (i = 1; i <= EXTRA_EDGE_STEPS; i++)
    {
        uvN -= EdgeStepSizes[i - 1] * edgeStep;
        lumaDeltaN = GetLuma(SampleOffsetZero(uvN)) - edgeLuma;
        if (abs(lumaDeltaN) > gradientThreshold) break;
    }
    if (i == EXTRA_EDGE_STEPS + 1)
    {
        uvP -= LAST_EDGE_STEP_GUESS * edgeStep;
    }

    if (isHorizontal)
    {
        distanceP = uvP.x - uv.x;
        distanceN = uv.x - uvN.x;
    }
    else
    {
        distanceP = uvP.y - uv.y;
        distanceN = uv.y - uvN.y;
    }

    // ------------------------- Edge Factor Calculation -------------------------
    
    float edgeFactor;
    if (distanceP < distanceN)
    {
        if (sign(lumaDeltaP) == sign(M - edgeLuma))
        {
            edgeFactor = 0;
        }
        else
        {
            edgeFactor = 0.5 - distanceP / (distanceP + distanceN);
        }
    }
    else
    {
        if (sign(lumaDeltaN) == sign(M - edgeLuma))
        {
            edgeFactor = 0;
        }
        else
        {
            edgeFactor = 0.5 - distanceN / (distanceP + distanceN);
        }
    }

    float finalFactor = max(blendFactor, edgeFactor);

    return SampleOffsetZero(uv + pixelStep * finalFactor).rgb;
}

float3 ApplyFXAAConsole(float2 uv, float4 middleColor)
{
    float M  = GetLuma(middleColor);
    float NW = GetLuma(SampleOffset(uv, -0.5,  0.5));
    float NE = GetLuma(SampleOffset(uv,  0.5,  0.5));
    float SW = GetLuma(SampleOffset(uv, -0.5, -0.5));
    float SE = GetLuma(SampleOffset(uv,  0.5, -0.5));

    // ------------------------- Calculate Contrast -------------------------
    
    float maxLuma = max(max(NW, NE), max(SW, SE));
    float minLuma = min(min(NW, NE), min(NW, NE));
    float contrast = max(maxLuma, M) - min(minLuma, M);
    
    // ------------------------- Threshold -------------------------
    
    if (contrast < max(FXAA_CONSOLE_CONTRAST_THRESHOLD, maxLuma * FXAA_CONSOLE_RELATIVE_THRESHOLD)) return middleColor.rgb;

    // ------------------------- Determine Blending Direction -------------------------
    
    float2 dir;
    dir.x = -((NW + NE) - (SW + SE));
    dir.y = ((NE + SE) - (NW + SW));
    dir = normalize(dir);

    // ------------------------- Blend -------------------------
    
    float2 dir1 = dir * TEXEL_SIZE.xy * FXAA_CONSOLE_SCALE;
    float4 rgbN1 = SampleOffsetZero(uv - dir1);
    float4 rgbP1 = SampleOffsetZero(uv + dir1);
    float4 rgbA = (rgbN1 + rgbP1) * 0.5;
    
    float dirAbsMinTimesC = min(abs(dir.x), abs(dir.y)) * FXAA_CONSOLE_EDGE_SHARPNESS;
    float2 dir2 = clamp(dir / dirAbsMinTimesC, -2.0, 2.0) * 2.0;
    float4 rgbN2 = SampleOffsetZero(uv - dir2 * TEXEL_SIZE.xy);
    float4 rgbP2 = SampleOffsetZero(uv + dir2 * TEXEL_SIZE.xy);
    float4 rgbB = rgbA * 0.5 + (rgbN2 + rgbP2) * 0.25;
    
    float newLum = GetLuma(rgbB);
    if((newLum > minLuma) && (newLum < maxLuma))
    {
        rgbA = rgbB;
    }
    
    return rgbA.rgb;
}

#endif