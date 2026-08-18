#ifndef YPIPELINE_ENCODING_LIBRARY_INCLUDED
#define YPIPELINE_ENCODING_LIBRARY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// ----------------------------------------------------------------------------------------------------
// Normal Packing
// ----------------------------------------------------------------------------------------------------

// R8G8B8A8
float3 EncodeNormalInto888(float3 normalWS)
{
    // From HDRP
    // The sign of the Z component of the normal MUST round-trip through the G-Buffer, otherwise
    // the reconstruction of the tangent frame for anisotropic GGX creates a seam along the Z axis.
    // The constant was eye-balled to not cause artifacts.
    // TODO: find a proper solution. E.g. we could re-shuffle the faces of the octahedron
    // s.t. the sign of the Z component round-trips.
    const float seamThreshold = 1.0 / 1024.0;
    normalWS.z = CopySign(max(seamThreshold, abs(normalWS.z)), normalWS.z);
    
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    return packNormalWS;
}

// R8G8B8A8
float3 DecodeNormalFrom888(float3 packNormalWS)
{
    float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
    float3 normalWS = UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);
    return normalWS;
}

// ----------------------------------------------------------------------------------------------------
// HDR Packing
// From https://github.com/Microsoft/DirectX-Graphics-Samples/blob/master/MiniEngine/Core/Shaders/PixelPacking_R11G11B10.hlsli
// Original license included:
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
// Developed by Minigraph
//
// Author:  James Stanard 
// ----------------------------------------------------------------------------------------------------

// The standard 32-bit HDR color format.  Each float has a 5-bit exponent and no sign bit.
uint PackR11G11B10(float3 rgb)
{
    // Clamp upper bound so that it doesn't accidentally round up to INF 
    // Exponent=15, Mantissa=1.11111
    rgb = min(rgb, asfloat(0x477C0000));  
    uint r = ((f32tof16(rgb.x) + 8) >> 4) & 0x000007FF;
    uint g = ((f32tof16(rgb.y) + 8) << 7) & 0x003FF800;
    uint b = ((f32tof16(rgb.z) + 16) << 17) & 0xFFC00000;
    return r | g | b;
}

float3 UnpackR11G11B10(uint rgb)
{
    float r = f16tof32((rgb << 4 ) & 0x7FF0);
    float g = f16tof32((rgb >> 7 ) & 0x7FF0);
    float b = f16tof32((rgb >> 17) & 0x7FE0);
    return float3(r, g, b);
}

// An improvement to float is to store the mantissa in logarithmic form.  This causes a
// smooth and continuous change in precision rather than having jumps in precision every
// time the exponent increases by whole amounts.
uint PackR11G11B10Log(float3 rgb)
{
    float3 flat_mantissa = asfloat((asuint(rgb) & 0x7FFFFF) | 0x3F800000);
    float3 curved_mantissa = min(log2(flat_mantissa) + 1.0, asfloat(0x3FFFFFFF));
    rgb = asfloat((asuint(rgb) & 0xFF800000) | (asuint(curved_mantissa) & 0x7FFFFF));

    uint r = ((f32tof16(rgb.x) + 8) >>  4) & 0x000007FF;
    uint g = ((f32tof16(rgb.y) + 8) <<  7) & 0x003FF800;
    uint b = ((f32tof16(rgb.z) + 16) << 17) & 0xFFC00000;
    return r | g | b;
}

float3 UnpackR11G11B10Log(uint p)
{
    float3 rgb = f16tof32(uint3(p << 4, p >> 7, p >> 17) & uint3(0x7FF0, 0x7FF0, 0x7FE0));
    float3 curved_mantissa = asfloat((asuint(rgb) & 0x7FFFFF) | 0x3F800000);
    float3 flat_mantissa = exp2(curved_mantissa - 1.0);
    return asfloat((asuint(rgb) & 0xFF800000) | (asuint(flat_mantissa) & 0x7FFFFF));
}



#endif